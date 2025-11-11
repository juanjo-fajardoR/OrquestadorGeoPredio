using System;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using OrquestadorGeoPredio.DTOs;

namespace OrquestadorGeoPredio.Services.Factories
{
    
    /// Convierte el DTO CrTerrenoCreateDto en una Geometry + WKT + SRID validada.
    /// Esta clase no persiste nada: sólo transforma y valida la geometría.
    public class CrTerrenoFactory
    {
        // SRID por defecto según tu código Java
        public const int DefaultSrid = 9377;

      
        /// Convierte y valida la geometría desde el DTO.
        /// Retorna: (geometry, wkt, srid)
        /// Lanza excepciones si la geometría es inválida.
        public (Geometry geometry, string wkt, int srid) CreateGeometryAndWktFromDto(CrTerrenoCreateDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(dto.GeometriaGeoJson))
                throw new InvalidOperationException("No se recibió geometría GeoJSON válida.");

            // Leer GeoJSON -> Geometry
            var geoJsonReader = new GeoJsonReader();
            Geometry geom;
            try
            {
                geom = geoJsonReader.Read<Geometry>(dto.GeometriaGeoJson);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("GeoJSON inválido: " + ex.Message, ex);
            }

            if (geom == null || geom.IsEmpty)
                throw new InvalidOperationException("La geometría está vacía o nula.");

            // Reparar geometría si no es válida o contiene NaN/Infinity
            if (!geom.IsValid || ContainsInvalidCoordinate(geom))
            {
                geom = geom.Buffer(0); // intento de "fix"
            }

            if (!geom.IsValid || ContainsInvalidCoordinate(geom))
            {
                throw new InvalidOperationException("Geometría inválida o con coordenadas no válidas tras intentar repararla.");
            }

            // SRID: usar dto.Srid si viene; si no, usar DefaultSrid (9377)
            int srid = dto.Srid ?? DefaultSrid;
            geom.SRID = srid;

            // Convertir a WKT
            var wktWriter = new WKTWriter();
            string wkt = wktWriter.Write(geom);

            return (geom, wkt, srid);
        }

        private bool ContainsInvalidCoordinate(Geometry geom)
        {
            foreach (var c in geom.Coordinates)
            {
                if (double.IsNaN(c.X) || double.IsNaN(c.Y) || double.IsInfinity(c.X) || double.IsInfinity(c.Y))
                    return true;
            }
            return false;
        }
    }
}
