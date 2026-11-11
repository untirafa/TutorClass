using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorClass.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(TutorClassDbContext))]
    [Migration("20261111140000_AdicionaDenunciasModeracao")]
    public partial class AdicionaDenunciasModeracao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "denuncias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    denunciante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alvo_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_alvo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    alvo_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    motivo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    resumo_alvo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    vinculo_mentoria_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sancao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    observacao_moderacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    analisada_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    analisada_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    conta_reativada_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_denuncias", x => x.id);
                    table.ForeignKey(
                        name: "fk_denuncias_usuarios_alvo_usuario_id",
                        column: x => x.alvo_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_denuncias_usuarios_denunciante_id",
                        column: x => x.denunciante_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_denuncias_alvo_usuario_id",
                table: "denuncias",
                column: "alvo_usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_denuncias_denunciante_id_tipo_alvo_alvo_id",
                table: "denuncias",
                columns: new[] { "denunciante_id", "tipo_alvo", "alvo_id" });

            migrationBuilder.CreateIndex(
                name: "ix_denuncias_status_created_at_utc",
                table: "denuncias",
                columns: new[] { "status", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "denuncias");
        }
    }
}
