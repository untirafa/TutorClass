using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorClass.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaMateriaisPedagogicos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "materiais",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    professor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aluno_id = table.Column<Guid>(type: "uuid", nullable: false),
                    materia_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aula_id = table.Column<Guid>(type: "uuid", nullable: true),
                    titulo = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    tipo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    nome_arquivo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    url_arquivo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_materiais", x => x.id);
                    table.ForeignKey(
                        name: "fk_materiais_aulas_aula_id",
                        column: x => x.aula_id,
                        principalTable: "aulas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_materiais_materias_materia_id",
                        column: x => x.materia_id,
                        principalTable: "materias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_materiais_usuarios_aluno_id",
                        column: x => x.aluno_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_materiais_usuarios_professor_id",
                        column: x => x.professor_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_materiais_aluno_id_created_at_utc",
                table: "materiais",
                columns: new[] { "aluno_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_materiais_aula_id",
                table: "materiais",
                column: "aula_id");

            migrationBuilder.CreateIndex(
                name: "ix_materiais_materia_id",
                table: "materiais",
                column: "materia_id");

            migrationBuilder.CreateIndex(
                name: "ix_materiais_professor_id_created_at_utc",
                table: "materiais",
                columns: new[] { "professor_id", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "materiais");
        }
    }
}
