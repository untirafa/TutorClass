using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorClass.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaPlanosMentoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "limite_mentorados",
                table: "usuarios",
                type: "integer",
                nullable: true,
                defaultValue: 2);

            migrationBuilder.AddColumn<bool>(
                name: "oferece_mentoria",
                table: "usuarios",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "assinaturas_aluno",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    aluno_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plano = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    data_inicio_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_fim_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ativa = table.Column<bool>(type: "boolean", nullable: false),
                    valor_mensal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    valor_cobrado_primeiro_mes = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    usou_promocao_primeira_assinatura = table.Column<bool>(type: "boolean", nullable: false),
                    data_cancelamento_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assinaturas_aluno", x => x.id);
                    table.ForeignKey(
                        name: "fk_assinaturas_aluno_usuarios_aluno_id",
                        column: x => x.aluno_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mentorias_aluno_professor",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    aluno_id = table.Column<Guid>(type: "uuid", nullable: false),
                    professor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slot = table.Column<int>(type: "integer", nullable: false),
                    data_inicio_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_fim_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    data_ultima_troca_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    motivo_encerramento = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mentorias_aluno_professor", x => x.id);
                    table.ForeignKey(
                        name: "fk_mentorias_aluno_professor_usuarios_aluno_id",
                        column: x => x.aluno_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mentorias_aluno_professor_usuarios_professor_id",
                        column: x => x.professor_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usos_ia_aluno",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    aluno_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usos_ia_aluno", x => x.id);
                    table.ForeignKey(
                        name: "fk_usos_ia_aluno_usuarios_aluno_id",
                        column: x => x.aluno_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "acompanhamentos_mentoria",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    aula_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aluno_id = table.Column<Guid>(type: "uuid", nullable: false),
                    professor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vinculo_mentoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resumo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    pontos_fortes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    pontos_melhorar = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    proximos_passos = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    meta_atual = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    observacoes = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    data_conclusao_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_acompanhamentos_mentoria", x => x.id);
                    table.ForeignKey(
                        name: "fk_acompanhamentos_mentoria_aulas_aula_id",
                        column: x => x.aula_id,
                        principalTable: "aulas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_acompanhamentos_mentoria_mentorias_aluno_professor_vinculo_~",
                        column: x => x.vinculo_mentoria_id,
                        principalTable: "mentorias_aluno_professor",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_acompanhamentos_mentoria_usuarios_aluno_id",
                        column: x => x.aluno_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_acompanhamentos_mentoria_usuarios_professor_id",
                        column: x => x.professor_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "adicionais_mentoria",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    professor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aluno_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aula_id = table.Column<Guid>(type: "uuid", nullable: false),
                    acompanhamento_mentoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    data_geracao_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_adicionais_mentoria", x => x.id);
                    table.ForeignKey(
                        name: "fk_adicionais_mentoria_acompanhamentos_mentoria_acompanhamento~",
                        column: x => x.acompanhamento_mentoria_id,
                        principalTable: "acompanhamentos_mentoria",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_adicionais_mentoria_aulas_aula_id",
                        column: x => x.aula_id,
                        principalTable: "aulas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_adicionais_mentoria_usuarios_aluno_id",
                        column: x => x.aluno_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_adicionais_mentoria_usuarios_professor_id",
                        column: x => x.professor_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_oferece_mentoria",
                table: "usuarios",
                column: "oferece_mentoria");

            migrationBuilder.CreateIndex(
                name: "ix_acompanhamentos_mentoria_aluno_id_created_at_utc",
                table: "acompanhamentos_mentoria",
                columns: new[] { "aluno_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_acompanhamentos_mentoria_aula_id",
                table: "acompanhamentos_mentoria",
                column: "aula_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_acompanhamentos_mentoria_professor_id_created_at_utc",
                table: "acompanhamentos_mentoria",
                columns: new[] { "professor_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_acompanhamentos_mentoria_professor_id_status",
                table: "acompanhamentos_mentoria",
                columns: new[] { "professor_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_acompanhamentos_mentoria_vinculo_mentoria_id",
                table: "acompanhamentos_mentoria",
                column: "vinculo_mentoria_id");

            migrationBuilder.CreateIndex(
                name: "ix_adicionais_mentoria_acompanhamento_mentoria_id",
                table: "adicionais_mentoria",
                column: "acompanhamento_mentoria_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_adicionais_mentoria_aluno_id",
                table: "adicionais_mentoria",
                column: "aluno_id");

            migrationBuilder.CreateIndex(
                name: "ix_adicionais_mentoria_aula_id",
                table: "adicionais_mentoria",
                column: "aula_id");

            migrationBuilder.CreateIndex(
                name: "ix_adicionais_mentoria_professor_id_aluno_id_data_geracao_utc",
                table: "adicionais_mentoria",
                columns: new[] { "professor_id", "aluno_id", "data_geracao_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_assinaturas_aluno_aluno_id_ativa",
                table: "assinaturas_aluno",
                columns: new[] { "aluno_id", "ativa" });

            migrationBuilder.CreateIndex(
                name: "ix_assinaturas_aluno_aluno_id_usou_promocao_primeira_assinatura",
                table: "assinaturas_aluno",
                columns: new[] { "aluno_id", "usou_promocao_primeira_assinatura" });

            migrationBuilder.CreateIndex(
                name: "ix_mentorias_aluno_professor_aluno_id_ativo",
                table: "mentorias_aluno_professor",
                columns: new[] { "aluno_id", "ativo" });

            migrationBuilder.CreateIndex(
                name: "ix_mentorias_aluno_professor_aluno_id_professor_id_ativo",
                table: "mentorias_aluno_professor",
                columns: new[] { "aluno_id", "professor_id", "ativo" });

            migrationBuilder.CreateIndex(
                name: "ix_mentorias_aluno_professor_aluno_id_slot_ativo",
                table: "mentorias_aluno_professor",
                columns: new[] { "aluno_id", "slot", "ativo" });

            migrationBuilder.CreateIndex(
                name: "ix_mentorias_aluno_professor_professor_id_ativo",
                table: "mentorias_aluno_professor",
                columns: new[] { "professor_id", "ativo" });

            migrationBuilder.CreateIndex(
                name: "ix_usos_ia_aluno_aluno_id_data",
                table: "usos_ia_aluno",
                columns: new[] { "aluno_id", "data" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adicionais_mentoria");

            migrationBuilder.DropTable(
                name: "assinaturas_aluno");

            migrationBuilder.DropTable(
                name: "usos_ia_aluno");

            migrationBuilder.DropTable(
                name: "acompanhamentos_mentoria");

            migrationBuilder.DropTable(
                name: "mentorias_aluno_professor");

            migrationBuilder.DropIndex(
                name: "ix_usuarios_oferece_mentoria",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "limite_mentorados",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "oferece_mentoria",
                table: "usuarios");
        }
    }
}
