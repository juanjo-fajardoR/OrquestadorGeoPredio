using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;  
using Microsoft.EntityFrameworkCore;
using OrquestadorGeoPredio.Data;
using OrquestadorGeoPredio.DTOs;
using OrquestadorGeoPredio.Entities;
using OrquestadorGeoPredio.Repositories;
using OrquestadorGeoPredio.Services.Factories;
using System;
using System.Data;
using System.IO;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;


namespace OrquestadorGeoPredio.Services
{
    public class OcrCrTerrenoService
    {
        private readonly ICrTerrenoRepository _repo;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly CrTerrenoFactory _factory;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<OcrCrTerrenoService> _logger;

        public OcrCrTerrenoService(
            ICrTerrenoRepository repo,
            IHttpClientFactory httpClientFactory,
            CrTerrenoFactory factory,
            ApplicationDbContext dbContext,
              ILogger<OcrCrTerrenoService> logger)
        {
            _repo = repo;
            _httpClientFactory = httpClientFactory;
            _factory = factory;
            _dbContext = dbContext;
            _logger = logger;
        }

        /// Lógica principal: envía archivo al OCR, mapea respuesta y crea registro en BD.
        public async Task<CrTerrenoEntity> ProcesarArchivoOcrAsync(IFormFile archivo, string ocrUrl)
        {
            if (archivo == null || archivo.Length == 0) throw new ArgumentException("Archivo nulo o vacío.");

            // Guardar temporal (igual que en Java)
            var tempDir = Path.Combine(Path.GetTempPath(), "ocr_upload_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);
            var tempFilePath = Path.Combine(tempDir, archivo.FileName ?? ("archivo_" + Guid.NewGuid()));

            await using (var fs = new FileStream(tempFilePath, FileMode.Create))
            {
                await archivo.CopyToAsync(fs);
            }

            // Enviar al OCR (multipart/form-data)
            //var client = _httpClientFactory.CreateClient();

            var client = _httpClientFactory.CreateClient("OCR");

            using var form = new MultipartFormDataContent();
            var stream = File.OpenRead(tempFilePath);
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
            form.Add(streamContent, "file", Path.GetFileName(tempFilePath));

            var resp = await client.PostAsync(ocrUrl, form);
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Error consumiendo OCR: {resp.StatusCode}");

            var body = await resp.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
                throw new Exception("OCR no devolvió datos");

            // Mapear JSON OCR -> CrTerrenoOcrDto (puedes mover MapearDesdeJson a otra clase si quieres)
            var ocrDto = MapearDesdeJson(body);

            // Construir create DTO
            var createDto = new CrTerrenoCreateDto
            {
                Etiqueta = ocrDto.Etiqueta,
                RelacionSuperficie = ocrDto.RelacionSuperficie,
                UsuarioPortal = ocrDto.UsuarioPortal,
                CreatedUser = ocrDto.UsuarioPortal,
                LastEditedUser = ocrDto.UsuarioPortal,
                LocalId = ocrDto.LocalId,
                Fuente = ocrDto.Fuente,
                GeometriaGeoJson = ocrDto.GeometriaGeoJson
            };

            // Ahora creamos desde DTO (aquí delegamos la transformación al factory)
            var result = await CreateFromDtoAsync(createDto);
            return result;
        }


        /// Crea el registro persistentemente (replica createFromDTO de Java).
        /// Esta operación abre una transacción SERIALIZABLE para replicar el comportamiento Java.

        public async Task<CrTerrenoEntity> CreateFromDtoAsync(CrTerrenoCreateDto dto)
        {
          
            var (geometry, wkt, srid) = _factory.CreateGeometryAndWktFromDto(dto);

            var localId = !string.IsNullOrWhiteSpace(dto.LocalId)
                ? dto.LocalId
                : $"L-{Guid.NewGuid()}";

            var entityToReturn = new CrTerrenoEntity
            {
                Etiqueta = dto.Etiqueta,
                RelacionSuperficie = dto.RelacionSuperficie,
                GlobalId = Guid.NewGuid(),
                UsuarioPortal = dto.UsuarioPortal,
                CreatedUser = dto.CreatedUser,
                CreatedDate = DateTime.UtcNow,
                LastEditedUser = dto.LastEditedUser,
                LastEditedDate = DateTime.UtcNow,
                LocalId = localId,
                GdbGeomattrData = null
            };

            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                try
                {
                    var nextObjectId = await _repo.GetNextObjectIdAsync();

                    await _repo.InsertCrTerrenoRawAsync(
                        objectId: nextObjectId,
                        etiqueta: dto.Etiqueta,
                        relacionSuperficie: dto.RelacionSuperficie,
                        globalId: entityToReturn.GlobalId?.ToString(),
                        usuarioPortal: dto.UsuarioPortal,
                        createdUser: dto.CreatedUser,
                        lastEditedUser: dto.LastEditedUser,
                        localId: localId,
                        wkt: wkt,
                        srid: srid,
                        gdbGeomattrData: null
                    );

                    var saved = await _repo.GetByIdAsync(nextObjectId);
                    if (saved == null)
                        throw new Exception($"Registro insertado pero no encontrado OBJECTID: {nextObjectId}");

                    saved.Shape = geometry;

                    await tx.CommitAsync();
                    return saved;
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });
        }


        private CrTerrenoOcrDto MapearDesdeJson(string data)
        {
            _logger.LogInformation("OCR JSON recibido: {json}", data);

            using var document = JsonDocument.Parse(data);
            var root = document.RootElement;
            var dto = new CrTerrenoOcrDto();

            if (root.TryGetProperty("resultado_geojson", out var resultadoGeoJson)
                && resultadoGeoJson.ValueKind == JsonValueKind.Array
                && resultadoGeoJson.GetArrayLength() > 0)
            {
                // 🔹 Puede ser que resultado_geojson[0] sea objeto o array
                var first = resultadoGeoJson[0];

                // Si es un array anidado, tomamos el primer elemento interno
                if (first.ValueKind == JsonValueKind.Array && first.GetArrayLength() > 0)
                {
                    first = first[0];
                }

                if (first.ValueKind == JsonValueKind.Object &&
                    first.TryGetProperty("features", out var features) &&
                    features.ValueKind == JsonValueKind.Array &&
                    features.GetArrayLength() > 0)
                {
                    var feature = features[0];

                    // properties
                    if (feature.TryGetProperty("properties", out var props) && props.ValueKind == JsonValueKind.Object)
                    {
                        dto.Etiqueta = props.TryGetProperty("etiqueta", out var e) && e.ValueKind == JsonValueKind.String
                            ? e.GetString()
                            : null;

                        if (props.TryGetProperty("relacion_superficie", out var r) && r.ValueKind == JsonValueKind.Number)
                            dto.RelacionSuperficie = (short?)r.GetInt32();

                        dto.Fuente = (props.TryGetProperty("fuentePredio", out var f) && f.ValueKind == JsonValueKind.String)
                            ? f.GetString()
                            : (props.TryGetProperty("fuente", out var f2) && f2.ValueKind == JsonValueKind.String
                                ? f2.GetString()
                                : null);

                        dto.UsuarioPortal = props.TryGetProperty("usuario_portal", out var u) && u.ValueKind == JsonValueKind.String
                            ? u.GetString()
                            : null;

                        dto.LocalId = props.TryGetProperty("local_id", out var l) && l.ValueKind == JsonValueKind.String
                            ? l.GetString()
                            : null;
                    }

                    // geometry
                    if (feature.TryGetProperty("geometry", out var geom) && geom.ValueKind != JsonValueKind.Null)
                        dto.GeometriaGeoJson = geom.GetRawText();
                }
            }

            return dto;
        }


    }
}
