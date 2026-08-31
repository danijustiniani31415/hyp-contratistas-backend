using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abril_Backend.Migrations
{
    public partial class AddSsomaPerformanceIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Tareo casa: búsquedas por proyecto + fecha
                CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_ss_tareo_proyecto_fecha
                    ON ss_tareo (proyecto_id, fecha);

                -- Tareo detalle casa: join con ss_tareo
                CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_ss_tareo_detalle_casa_tareo_id
                    ON ss_tareo_detalle_casa (tareo_id);

                -- Tareo contratista: empresa + tareo
                CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_ss_tareo_detalle_contratista_empresa_tareo
                    ON ss_tareo_detalle_contratista (empresa_id, tareo_id);

                -- RACs: proyecto + fecha + empresa
                CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_ssoma_rac_proyecto_fecha
                    ON ssoma_rac (proyecto_id, fecha_reporte);
                CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_ssoma_rac_empresa_fecha
                    ON ssoma_rac (empresa_reportada_id, fecha_reporte);

                -- OPT: proyecto + fecha
                CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_ssoma_opt_proyecto_fecha
                    ON ssoma_opt (proyecto_id, fecha);

                -- OPT trabajadores: opt_id + trabajador
                CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_ssoma_opt_trabajador_opt_id
                    ON ssoma_opt_trabajador (opt_id, trabajador_id);

                -- ATS/Auditoría: proyecto + fecha + worker
                CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_ssoma_auditoria_ats_proyecto_fecha
                    ON ssoma_auditoria_ats (proyecto_id, fecha, auditado_worker_id);

                -- Inspecciones: proyecto + empresa + fecha
                CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_ssoma_inspeccion_proyecto_fecha
                    ON ssoma_inspeccion (proyecto_id, fecha);

                -- Charlas asistencia: charla + worker
                CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_ss_charla_asistencia_charla_worker
                    ON ss_charla_asistencia (charla_id, worker_id, asistio);

                -- Charla: proyecto + fecha
                CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_ss_charla_proyecto_fecha
                    ON ss_charla (proyecto_id, fecha);

                -- Workers: por empresa (ContributorId)
                CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_worker_contributor_id
                    ON worker (contributor_id);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ix_ss_tareo_proyecto_fecha;
                DROP INDEX IF EXISTS ix_ss_tareo_detalle_casa_tareo_id;
                DROP INDEX IF EXISTS ix_ss_tareo_detalle_contratista_empresa_tareo;
                DROP INDEX IF EXISTS ix_ssoma_rac_proyecto_fecha;
                DROP INDEX IF EXISTS ix_ssoma_rac_empresa_fecha;
                DROP INDEX IF EXISTS ix_ssoma_opt_proyecto_fecha;
                DROP INDEX IF EXISTS ix_ssoma_opt_trabajador_opt_id;
                DROP INDEX IF EXISTS ix_ssoma_auditoria_ats_proyecto_fecha;
                DROP INDEX IF EXISTS ix_ssoma_inspeccion_proyecto_fecha;
                DROP INDEX IF EXISTS ix_ss_charla_asistencia_charla_worker;
                DROP INDEX IF EXISTS ix_ss_charla_proyecto_fecha;
                DROP INDEX IF EXISTS ix_worker_contributor_id;
            ");
        }
    }
}
