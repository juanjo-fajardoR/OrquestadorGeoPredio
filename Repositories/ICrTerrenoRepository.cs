using System.Threading;
using System.Threading.Tasks;
using OrquestadorGeoPredio.Entities;

namespace OrquestadorGeoPredio.Repositories
{
    public interface ICrTerrenoRepository
    {
        Task AddAsync(CrTerrenoEntity entity, CancellationToken cancellationToken = default);
        Task<CrTerrenoEntity?> GetByIdAsync(int objectId, CancellationToken cancellationToken = default);
        /// Ejecuta la inserción "raw" que utiliza geometry::STGeomFromText(wkt, srid)
        /// y permite pasar gdbGeomattrData como varbinary (o null).
        /// Esta función no inicia ni hace commit/rollback de transacción: se espera
        /// que el caller abra una transacción si se requiere aislamiento SERIALIZABLE.
        Task InsertCrTerrenoRawAsync(
            int objectId,
            string? etiqueta,
            short? relacionSuperficie,
            string? globalId,
            string? usuarioPortal,
            string? createdUser,
            string? lastEditedUser,
            string? localId,
            string wkt,
            int srid,
            byte[]? gdbGeomattrData,
            CancellationToken cancellationToken = default);

      
        /// Devuelve ISNULL(MAX(OBJECTID), 0) + 1 — usado si mantienes la estrategia actual.
        Task<int> GetNextObjectIdAsync(CancellationToken cancellationToken = default);
    }
}