using Microsoft.AspNetCore.Mvc;
using OrquestadorGeoPredio.Entities;
using OrquestadorGeoPredio.Services;

namespace OrquestadorGeoPredio.Controllers
{
    [ApiController]
    [Route("api/ocr")]
    public class OcrCrTerrenoController : ControllerBase
    {
        private readonly OcrCrTerrenoService _ocrService;
        private readonly ILogger<OcrCrTerrenoController> _logger;

        public OcrCrTerrenoController(OcrCrTerrenoService ocrService, ILogger<OcrCrTerrenoController> logger)
        {
            _ocrService = ocrService;
            _logger = logger;
        }

        
        /// Procesa un archivo PDF enviado al OCR y crea un registro CrTerreno.
      
        [HttpPost("procesarTerreno")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<CrTerrenoEntity>> ProcesarArchivo([FromForm] IFormFile file, [FromHeader(Name = "OCR_URL")] string ocrUrl) {
            if (string.IsNullOrWhiteSpace(ocrUrl))
                return BadRequest("Debe enviar el header OCR_URL con la dirección del servicio OCR.");

            if (file == null || file.Length == 0)
                return BadRequest("Debe enviar un archivo válido.");

            try
            {
                var result = await _ocrService.ProcesarArchivoOcrAsync(file, ocrUrl);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar archivo OCR");
                return StatusCode(500, new
                {
                    mensaje = "Error procesando archivo OCR",
                    detalle = ex.Message
                });
            }
        }
    }
}
