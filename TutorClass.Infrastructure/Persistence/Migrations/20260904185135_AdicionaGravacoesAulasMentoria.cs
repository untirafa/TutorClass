using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorClass.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaGravacoesAulasMentoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gravacoes_aula",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    aula_id = table.Column<Guid>(type: "uuid", nullable: false),
                    titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    url_video = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    miniatura_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    duracao_segundos = table.Column<int>(type: "integer", nullable: false),
                    data_disponibilizacao_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    disponivel = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gravacoes_aula", x => x.id);
                    table.ForeignKey(
                        name: "fk_gravacoes_aula_aulas_aula_id",
                        column: x => x.aula_id,
                        principalTable: "aulas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_gravacoes_aula_aula_id",
                table: "gravacoes_aula",
                column: "aula_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gravacoes_aula_disponivel_data_disponibilizacao_utc",
                table: "gravacoes_aula",
                columns: new[] { "disponivel", "data_disponibilizacao_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gravacoes_aula");
        }
    }
}
