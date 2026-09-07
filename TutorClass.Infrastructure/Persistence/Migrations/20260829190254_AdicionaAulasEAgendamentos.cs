using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorClass.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaAulasEAgendamentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "aulas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    aluno_id = table.Column<Guid>(type: "uuid", nullable: false),
                    professor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    materia_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inicio_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    duracao_minutos = table.Column<int>(type: "integer", nullable: false),
                    preco_acordado = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    confirmada_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelada_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    concluida_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelada_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    motivo_cancelamento = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_aulas", x => x.id);
                    table.ForeignKey(
                        name: "fk_aulas_materias_materia_id",
                        column: x => x.materia_id,
                        principalTable: "materias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_aulas_usuarios_aluno_id",
                        column: x => x.aluno_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_aulas_usuarios_professor_id",
                        column: x => x.professor_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_aulas_aluno_id_inicio_utc_status",
                table: "aulas",
                columns: new[] { "aluno_id", "inicio_utc", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_aulas_materia_id",
                table: "aulas",
                column: "materia_id");

            migrationBuilder.CreateIndex(
                name: "ix_aulas_professor_id_inicio_utc_status",
                table: "aulas",
                columns: new[] { "professor_id", "inicio_utc", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_aulas_professor_id_materia_id",
                table: "aulas",
                columns: new[] { "professor_id", "materia_id" });

            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    legacy_user record;
                BEGIN
                    FOR legacy_user IN
                        SELECT id
                        FROM usuarios
                        WHERE email LIKE '%@tutorclass.local'
                          AND nome IN ('Aluno Desenvolvimento', 'Professor Desenvolvimento')
                          AND email NOT IN (
                              'admin.dev@tutorclass.local',
                              'ana.clara.dev@tutorclass.local',
                              'bruno.lima.dev@tutorclass.local',
                              'camila.rocha.dev@tutorclass.local',
                              'diego.costa.dev@tutorclass.local',
                              'lara.nunes.dev@tutorclass.local',
                              'mariana.martins.dev@tutorclass.local',
                              'ricardo.nunes.dev@tutorclass.local',
                              'helena.biologia.dev@tutorclass.local',
                              'rafael.fisica.dev@tutorclass.local',
                              'patricia.portugues.dev@tutorclass.local',
                              'sofia.ingles.dev@tutorclass.local',
                              'marcos.historia.dev@tutorclass.local'
                          )
                    LOOP
                        DELETE FROM aulas
                        WHERE aluno_id = legacy_user.id
                           OR professor_id = legacy_user.id;

                        DELETE FROM responsaveis_alunos
                        WHERE aluno_id = legacy_user.id
                           OR responsavel_id = legacy_user.id;

                        DELETE FROM candidaturas_professor
                        WHERE professor_id = legacy_user.id;

                        DELETE FROM disponibilidades_professor
                        WHERE professor_id = legacy_user.id;

                        DELETE FROM professores_materias
                        WHERE professor_id = legacy_user.id;

                        DELETE FROM qualificacoes_professor
                        WHERE professor_id = legacy_user.id;

                        DELETE FROM usuarios
                        WHERE id = legacy_user.id
                          AND email LIKE '%@tutorclass.local'
                          AND nome IN ('Aluno Desenvolvimento', 'Professor Desenvolvimento');
                    END LOOP;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aulas");
        }
    }
}
