using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class SwitchProyectoFkToProjectLegacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop FKs viejas que apuntan a "projects" (plural). IF EXISTS por si alguna ya no estaba.
            migrationBuilder.Sql(@"
                ALTER TABLE project_sub_contractor      DROP CONSTRAINT IF EXISTS fk_project_sub_contractor_projects_project_id;
                ALTER TABLE resident_report_incidence   DROP CONSTRAINT IF EXISTS fk_resident_report_incidence_projects_project_id;
                ALTER TABLE ss_empresa_contratista      DROP CONSTRAINT IF EXISTS fk_ss_empresa_contratista_projects_proyecto_id;
                ALTER TABLE ss_empresa_proyecto         DROP CONSTRAINT IF EXISTS fk_ss_empresa_proyecto_projects_proyecto_id;
                ALTER TABLE ss_equipo                   DROP CONSTRAINT IF EXISTS fk_ss_equipo_projects_proyecto_id;
                ALTER TABLE ss_eval_supervisor          DROP CONSTRAINT IF EXISTS fk_ss_eval_supervisor_projects_proyecto_id;
                ALTER TABLE ss_hab_empresa              DROP CONSTRAINT IF EXISTS fk_ss_hab_empresa_projects_proyecto_id;
                ALTER TABLE ss_induccion                DROP CONSTRAINT IF EXISTS fk_ss_induccion_projects_proyecto_id;
                ALTER TABLE ss_sctr_vidaley             DROP CONSTRAINT IF EXISTS fk_ss_sctr_vidaley_projects_proyecto_id;
                ALTER TABLE worker_vinculaciones        DROP CONSTRAINT IF EXISTS fk_worker_vinculaciones_projects_proyecto_id;
            ");

            // Drop tabla projects (plural). CASCADE para eliminar FKs dependientes que el snapshot no listaba.
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS projects CASCADE;");

            // Add FKs nuevas apuntando a project (singular). Drop previo IF EXISTS por si ya existían.
            migrationBuilder.Sql(@"
                ALTER TABLE project_sub_contractor      DROP CONSTRAINT IF EXISTS fk_project_sub_contractor_project_project_id;
                ALTER TABLE project_sub_contractor      ADD CONSTRAINT fk_project_sub_contractor_project_project_id
                    FOREIGN KEY (project_id) REFERENCES project(project_id) ON DELETE CASCADE;

                ALTER TABLE resident_report_incidence   DROP CONSTRAINT IF EXISTS fk_resident_report_incidence_project_project_id;
                ALTER TABLE resident_report_incidence   ADD CONSTRAINT fk_resident_report_incidence_project_project_id
                    FOREIGN KEY (project_id) REFERENCES project(project_id) ON DELETE CASCADE;

                ALTER TABLE ss_empresa_contratista      DROP CONSTRAINT IF EXISTS fk_ss_empresa_contratista_project_proyecto_id;
                ALTER TABLE ss_empresa_contratista      ADD CONSTRAINT fk_ss_empresa_contratista_project_proyecto_id
                    FOREIGN KEY (proyecto_id) REFERENCES project(project_id);

                ALTER TABLE ss_empresa_proyecto         DROP CONSTRAINT IF EXISTS fk_ss_empresa_proyecto_project_proyecto_id;
                ALTER TABLE ss_empresa_proyecto         ADD CONSTRAINT fk_ss_empresa_proyecto_project_proyecto_id
                    FOREIGN KEY (proyecto_id) REFERENCES project(project_id) ON DELETE CASCADE;

                ALTER TABLE ss_equipo                   DROP CONSTRAINT IF EXISTS fk_ss_equipo_project_proyecto_id;
                ALTER TABLE ss_equipo                   ADD CONSTRAINT fk_ss_equipo_project_proyecto_id
                    FOREIGN KEY (proyecto_id) REFERENCES project(project_id) ON DELETE CASCADE;

                ALTER TABLE ss_eval_supervisor          DROP CONSTRAINT IF EXISTS fk_ss_eval_supervisor_project_proyecto_id;
                ALTER TABLE ss_eval_supervisor          ADD CONSTRAINT fk_ss_eval_supervisor_project_proyecto_id
                    FOREIGN KEY (proyecto_id) REFERENCES project(project_id) ON DELETE CASCADE;

                ALTER TABLE ss_hab_empresa              DROP CONSTRAINT IF EXISTS fk_ss_hab_empresa_project_proyecto_id;
                ALTER TABLE ss_hab_empresa              ADD CONSTRAINT fk_ss_hab_empresa_project_proyecto_id
                    FOREIGN KEY (proyecto_id) REFERENCES project(project_id) ON DELETE CASCADE;

                ALTER TABLE ss_induccion                DROP CONSTRAINT IF EXISTS fk_ss_induccion_project_proyecto_id;
                ALTER TABLE ss_induccion                ADD CONSTRAINT fk_ss_induccion_project_proyecto_id
                    FOREIGN KEY (proyecto_id) REFERENCES project(project_id) ON DELETE CASCADE;

                ALTER TABLE ss_sctr_vidaley             DROP CONSTRAINT IF EXISTS fk_ss_sctr_vidaley_project_proyecto_id;
                ALTER TABLE ss_sctr_vidaley             ADD CONSTRAINT fk_ss_sctr_vidaley_project_proyecto_id
                    FOREIGN KEY (proyecto_id) REFERENCES project(project_id) ON DELETE CASCADE;

                ALTER TABLE worker_vinculaciones        DROP CONSTRAINT IF EXISTS fk_worker_vinculaciones_project_proyecto_id;
                ALTER TABLE worker_vinculaciones        ADD CONSTRAINT fk_worker_vinculaciones_project_proyecto_id
                    FOREIGN KEY (proyecto_id) REFERENCES project(project_id);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE project_sub_contractor      DROP CONSTRAINT IF EXISTS fk_project_sub_contractor_project_project_id;
                ALTER TABLE resident_report_incidence   DROP CONSTRAINT IF EXISTS fk_resident_report_incidence_project_project_id;
                ALTER TABLE ss_empresa_contratista      DROP CONSTRAINT IF EXISTS fk_ss_empresa_contratista_project_proyecto_id;
                ALTER TABLE ss_empresa_proyecto         DROP CONSTRAINT IF EXISTS fk_ss_empresa_proyecto_project_proyecto_id;
                ALTER TABLE ss_equipo                   DROP CONSTRAINT IF EXISTS fk_ss_equipo_project_proyecto_id;
                ALTER TABLE ss_eval_supervisor          DROP CONSTRAINT IF EXISTS fk_ss_eval_supervisor_project_proyecto_id;
                ALTER TABLE ss_hab_empresa              DROP CONSTRAINT IF EXISTS fk_ss_hab_empresa_project_proyecto_id;
                ALTER TABLE ss_induccion                DROP CONSTRAINT IF EXISTS fk_ss_induccion_project_proyecto_id;
                ALTER TABLE ss_sctr_vidaley             DROP CONSTRAINT IF EXISTS fk_ss_sctr_vidaley_project_proyecto_id;
                ALTER TABLE worker_vinculaciones        DROP CONSTRAINT IF EXISTS fk_worker_vinculaciones_project_proyecto_id;
            ");
        }
    }
}
