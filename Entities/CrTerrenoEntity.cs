
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;
    using NetTopologySuite.Geometries;
    using NetTopologySuite.IO;     

    namespace OrquestadorGeoPredio.Entities
    {
        [Table("CR_TERRENO", Schema = "EMTEL")]
        public class CrTerrenoEntity
        {
            [Key]
            [Column("OBJECTID")]
            public int ObjectId { get; set; }

            [Column("etiqueta", TypeName = "nvarchar(255)")]
            public string? Etiqueta { get; set; }

            [Column("relacion_superficie")]
            public short? RelacionSuperficie { get; set; }

            [Column("globalid")]
            public Guid? GlobalId { get; set; }

            [Column("usuario_portal", TypeName = "nvarchar(60)")]
            public string? UsuarioPortal { get; set; }

            [Column("created_user", TypeName = "nvarchar(255)")]
            public string? CreatedUser { get; set; }

            [Column("created_date", TypeName = "datetime2")]
            public DateTime? CreatedDate { get; set; }

            [Column("last_edited_user", TypeName = "nvarchar(255)")]
            public string? LastEditedUser { get; set; }

            [Column("last_edited_date", TypeName = "datetime2")]
            public DateTime? LastEditedDate { get; set; }

            [Column("local_id", TypeName = "nvarchar(255)")]
            public string? LocalId { get; set; }

          
            [NotMapped]
            [JsonIgnore]
            public Geometry? Shape { get; set; }

            [Column("GDB_GEOMATTR_DATA", TypeName = "varbinary(max)")]
            public byte[]? GdbGeomattrData { get; set; }

            [NotMapped]
            [JsonPropertyName("geometriaGeoJson")]
            public string? GeometriaGeoJson
            {
                get
                {
                    if (Shape == null) return null;
                    var writer = new GeoJsonWriter();
                    return writer.Write(Shape);
                }
            }

            public Geometry? GetShapeFromWkb(byte[]? wkb)
            {
                if (wkb == null) return null;
                try
                {
                    var reader = new WKBReader();
                    return reader.Read(wkb);
                }
                catch (Exception e)
                {
                    throw new Exception("Error convirtiendo geometría desde WKB", e);
                }
            }
        }
    }

