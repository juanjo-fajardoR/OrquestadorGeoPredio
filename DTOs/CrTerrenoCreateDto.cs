using System.Text.Json.Serialization;

namespace OrquestadorGeoPredio.DTOs
{
    public class CrTerrenoCreateDto
    {
        public string? Etiqueta { get; set; }

        public short? RelacionSuperficie { get; set; }

        public int? Srid { get; set; }

        public string? UsuarioPortal { get; set; }

        public string? CreatedUser { get; set; }

        public string? LastEditedUser { get; set; }

        public string? LocalId { get; set; }

        public string? Fuente { get; set; }

        [JsonPropertyName("geometriaGeoJson")]
        public string? GeometriaGeoJson { get; set; }
    }
}
