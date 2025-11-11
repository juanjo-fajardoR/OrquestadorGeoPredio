using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OrquestadorGeoPredio.Data;
using OrquestadorGeoPredio.Entities;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace OrquestadorGeoPredio.Repositories
{
    public class CrTerrenoRepository : ICrTerrenoRepository
    {
        private readonly ApplicationDbContext _context;

        public CrTerrenoRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(CrTerrenoEntity entity, CancellationToken cancellationToken = default)
        {
            await _context.CrTerreno.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<CrTerrenoEntity?> GetByIdAsync(int objectId, CancellationToken cancellationToken = default)
        {
            // Lectura directa con EF Core
            return await _context.CrTerreno.FindAsync(new object[] { objectId }, cancellationToken);
        }

        public async Task<int> GetNextObjectIdAsync(CancellationToken cancellationToken = default)
        {
            const string sql = "SELECT ISNULL(MAX(OBJECTID), 0) + 1 AS [Value] FROM EMTEL.CR_TERRENO";
            var next = await _context.Database.SqlQueryRaw<int>(sql).SingleAsync(cancellationToken);
            return next;
        }

        public async Task InsertCrTerrenoRawAsync(
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
            CancellationToken cancellationToken = default)
        {
            string sql = @"
                INSERT INTO EMTEL.CR_TERRENO
                (OBJECTID, etiqueta, relacion_superficie, globalid, usuario_portal,
                 created_user, created_date, last_edited_user, last_edited_date,
                 local_id, shape, GDB_GEOMATTR_DATA)
                VALUES (@objectId, @etiqueta, @relacion, @globalId, @usuarioPortal,
                        @createdUser, SYSUTCDATETIME(), @lastEditedUser, SYSUTCDATETIME(),
                        @localId, geometry::STGeomFromText(@wkt, @srid), @gdb);";

            // ✅ Usa ExecuteSqlRawAsync (mantiene la transacción activa del DbContext)
            await _context.Database.ExecuteSqlRawAsync(sql,
                new SqlParameter("@objectId", objectId),
                new SqlParameter("@etiqueta", (object?)etiqueta ?? DBNull.Value),
                new SqlParameter("@relacion", (object?)relacionSuperficie ?? DBNull.Value),
                new SqlParameter("@globalId", (object?)globalId ?? DBNull.Value),
                new SqlParameter("@usuarioPortal", (object?)usuarioPortal ?? DBNull.Value),
                new SqlParameter("@createdUser", (object?)createdUser ?? DBNull.Value),
                new SqlParameter("@lastEditedUser", (object?)lastEditedUser ?? DBNull.Value),
                new SqlParameter("@localId", (object?)localId ?? DBNull.Value),
                new SqlParameter("@wkt", wkt),
                new SqlParameter("@srid", srid),
                new SqlParameter("@gdb", SqlDbType.VarBinary)
                {
                    Value = (object?)gdbGeomattrData ?? DBNull.Value
                }
            );
        }
    }
}
