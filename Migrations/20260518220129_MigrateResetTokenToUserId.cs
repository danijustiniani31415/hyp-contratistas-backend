using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class MigrateResetTokenToUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ss_reset_token DROP CONSTRAINT IF EXISTS ""fk_ss_reset_token_ss_empresa_contratista_empresa_id"";");
            migrationBuilder.Sql(@"ALTER TABLE workers DROP COLUMN IF EXISTS celular;");
            migrationBuilder.Sql(@"ALTER TABLE ss_reset_token ALTER COLUMN empresa_id DROP NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE ss_reset_token ADD COLUMN IF NOT EXISTS user_id integer;");
            migrationBuilder.Sql(@"ALTER TABLE contractor_email ADD COLUMN IF NOT EXISTS user_id integer;");
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_ss_reset_token_user_id ON ss_reset_token(user_id);
                CREATE INDEX IF NOT EXISTS ix_contractor_email_user_id ON contractor_email(user_id);
                DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_contractor_email_user_user_id') THEN
                    ALTER TABLE contractor_email ADD CONSTRAINT fk_contractor_email_user_user_id FOREIGN KEY (user_id) REFERENCES app_user(user_id); END IF; END $$;
                DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_ss_reset_token_ss_empresa_contratista_empresa_id') THEN
                    ALTER TABLE ss_reset_token ADD CONSTRAINT fk_ss_reset_token_ss_empresa_contratista_empresa_id FOREIGN KEY (empresa_id) REFERENCES ss_empresa_contratista(id); END IF; END $$;
                DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_ss_reset_token_user_user_id') THEN
                    ALTER TABLE ss_reset_token ADD CONSTRAINT fk_ss_reset_token_user_user_id FOREIGN KEY (user_id) REFERENCES app_user(user_id); END IF; END $$;
            ");
            // legacy EF calls replaced by SQL above — kept for Down() reference only
            if (false) { migrationBuilder.AddColumn<int>(
                name: "user_id",
                table: "contractor_email",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_ss_reset_token_user_id",
                table: "ss_reset_token",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_contractor_email_user_id",
                table: "contractor_email",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_contractor_email_user_user_id",
                table: "contractor_email",
                column: "user_id",
                principalTable: "app_user",
                principalColumn: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_ss_reset_token_ss_empresa_contratista_empresa_id",
                table: "ss_reset_token",
                column: "empresa_id",
                principalTable: "ss_empresa_contratista",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_ss_reset_token_user_user_id",
                table: "ss_reset_token",
                column: "user_id",
                principalTable: "app_user",
                principalColumn: "user_id");
            } // end if (false)
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_contractor_email_user_user_id",
                table: "contractor_email");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_reset_token_ss_empresa_contratista_empresa_id",
                table: "ss_reset_token");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_reset_token_user_user_id",
                table: "ss_reset_token");

            migrationBuilder.DropIndex(
                name: "ix_ss_reset_token_user_id",
                table: "ss_reset_token");

            migrationBuilder.DropIndex(
                name: "ix_contractor_email_user_id",
                table: "contractor_email");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "ss_reset_token");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "contractor_email");

            migrationBuilder.AddColumn<string>(
                name: "celular",
                table: "workers",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "empresa_id",
                table: "ss_reset_token",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_ss_reset_token_ss_empresa_contratista_empresa_id",
                table: "ss_reset_token",
                column: "empresa_id",
                principalTable: "ss_empresa_contratista",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
