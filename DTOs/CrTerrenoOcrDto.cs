using System.Text.Json.Serialization;

namespace OrquestadorGeoPredio.DTOs
{
    public class CrTerrenoOcrDto
    {
        public string? Etiqueta { get; set; }

        public short? RelacionSuperficie { get; set; }

        public int? Srid { get; set; }

        public string? MunicipioId { get; set; }

        public string? Fuente { get; set; }

        [JsonPropertyName("geometriaGeoJson")]
        public string? GeometriaGeoJson { get; set; }

        public string? UsuarioPortal { get; set; }

        public string? LocalId { get; set; }
    }
}