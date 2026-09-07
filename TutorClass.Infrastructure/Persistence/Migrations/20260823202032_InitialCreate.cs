using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TutorClass.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "materias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_materias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    tipo_usuario = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    Biografia = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PrecoHora = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    NivelReputacao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    IdentidadeVerificada = table.Column<bool>(type: "boolean", nullable: true),
                    MediaAvaliacoes = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    QuantidadeAvaliacoes = table.Column<int>(type: "integer", nullable: true),
                    AulasConcluidas = table.Column<int>(type: "integer", nullable: true),
                    DataNascimento = table.Column<DateOnly>(type: "date", nullable: true),
                    Telefone = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "candidaturas_professor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfessorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mensagem = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AdministradorAnaliseId = table.Column<Guid>(type: "uuid", nullable: true),
                    Motivo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DataAnaliseUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidaturas_professor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_candidaturas_professor_usuarios_ProfessorId",
                        column: x => x.ProfessorId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "disponibilidades_professor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfessorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiaSemana = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Fim = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disponibilidades_professor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_disponibilidades_professor_usuarios_ProfessorId",
                        column: x => x.ProfessorId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "professores_materias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfessorId = table.Column<Guid>(type: "uuid", nullable: false),
                    MateriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_professores_materias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_professores_materias_materias_MateriaId",
                        column: x => x.MateriaId,
                        principalTable: "materias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_professores_materias_usuarios_ProfessorId",
                        column: x => x.ProfessorId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "qualificacoes_professor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfessorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Instituicao = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    Descricao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DataConclusao = table.Column<DateOnly>(type: "date", nullable: true),
                    DocumentoComprovacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StatusVerificacao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DataVerificacaoUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qualificacoes_professor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_qualificacoes_professor_usuarios_ProfessorId",
                        column: x => x.ProfessorId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "responsaveis_alunos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponsavelId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlunoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Parentesco = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_responsaveis_alunos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_responsaveis_alunos_usuarios_AlunoId",
                        column: x => x.AlunoId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_responsaveis_alunos_usuarios_ResponsavelId",
                        column: x => x.ResponsavelId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "materias",
                columns: new[] { "Id", "CreatedAtUtc", "Nome", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 8, 23, 0, 0, 0, 0, DateTimeKind.Utc), "Portugues", null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 8, 23, 0, 0, 0, 0, DateTimeKind.Utc), "Literatura", null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 8, 23, 0, 0, 0, 0, DateTimeKind.Utc), "Ingles", null },
                    { new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(2026, 8, 23, 0, 0, 0, 0, DateTimeKind.Utc), "Espanhol", null },
                    { new Guid("55555555-5555-5555-5555-555555555555"), new DateTime(2026, 8, 23, 0, 0, 0, 0, DateTimeKind.Utc), "Artes", null },
                    { new Guid("66666666-6666-6666-6666-666666666666"), new DateTime(2026, 8, 23, 0, 0, 0, 0, DateTimeKind.Utc), "Historia", null },
                    { new Guid("77777777-7777-7777-7777-777777777777"), new DateTime(2026, 8, 23, 0, 0, 0, 0, DateTimeKind.Utc), "Geografia", null },
                    { new Guid("88888888-8888-8888-8888-888888888888"), new DateTime(2026, 8, 23, 0, 0, 0, 0, DateTimeKind.Utc), "Filosofia", null },
                    { new Guid("99999999-9999-9999-9999-999999999999"), new DateTime(2026, 8, 23, 0, 0, 0, 0, DateTimeKind.Utc), "Sociologia", null },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new DateTime(2026, 8, 23, 0, 0, 0, 0, DateTimeKind.Utc), "Fisica", null },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), new DateTime(2026, 8, 23, 0, 0, 0, 0, DateTimeKind.Utc), "Quimica", null },
                    { new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), new DateTime(2026, 8, 23, 0, 0, 0, 0, DateTimeKind.Utc), "Biologia", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_candidaturas_professor_ProfessorId_Status",
                table: "candidaturas_professor",
                columns: new[] { "ProfessorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_disponibilidades_professor_ProfessorId_DiaSemana_Inicio_Fim",
                table: "disponibilidades_professor",
                columns: new[] { "ProfessorId", "DiaSemana", "Inicio", "Fim" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_materias_Nome",
                table: "materias",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_professores_materias_MateriaId",
                table: "professores_materias",
                column: "MateriaId");

            migrationBuilder.CreateIndex(
                name: "IX_professores_materias_ProfessorId_MateriaId",
                table: "professores_materias",
                columns: new[] { "ProfessorId", "MateriaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_qualificacoes_professor_ProfessorId_StatusVerificacao",
                table: "qualificacoes_professor",
                columns: new[] { "ProfessorId", "StatusVerificacao" });

            migrationBuilder.CreateIndex(
                name: "IX_responsaveis_alunos_AlunoId",
                table: "responsaveis_alunos",
                column: "AlunoId");

            migrationBuilder.CreateIndex(
                name: "IX_responsaveis_alunos_ResponsavelId_AlunoId",
                table: "responsaveis_alunos",
                columns: new[] { "ResponsavelId", "AlunoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_Email",
                table: "usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_Status",
                table: "usuarios",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "candidaturas_professor");

            migrationBuilder.DropTable(
                name: "disponibilidades_professor");

            migrationBuilder.DropTable(
                name: "professores_materias");

            migrationBuilder.DropTable(
                name: "qualificacoes_professor");

            migrationBuilder.DropTable(
                name: "responsaveis_alunos");

            migrationBuilder.DropTable(
                name: "materias");

            migrationBuilder.DropTable(
                name: "usuarios");
        }
    }
}
