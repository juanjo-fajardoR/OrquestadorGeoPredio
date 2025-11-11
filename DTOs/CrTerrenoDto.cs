using System.Text.Json.Serialization;

namespace OrquestadorGeoPredio.DTOs
{
    public class CrTerrenoDto
    {
        [JsonPropertyName("objectId")]
        public long? ObjectId { get; set; }

        public string? Etiqueta { get; set; }

        public short? RelacionSuperficie { get; set; }

        public string? GlobalId { get; set; }

        public string? UsuarioPortal { get; set; }

        public string? CreatedUser { get; set; }

        public string? LastEditedUser { get; set; }

        public string? LocalId { get; set; }

        public string? Fuente { get; set; }

        [JsonPropertyName("geometriaGeoJson")]
        public string? GeometriaGeoJson { get; set; }
    }
}