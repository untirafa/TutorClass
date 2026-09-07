using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorClass.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FechaFinanceiroEAdicionaReagendamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "forma_pagamento",
                table: "pagamentos_aula",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Pix");

            migrationBuilder.CreateTable(
                name: "aulas_reagendamentos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    aula_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inicio_anterior_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fim_anterior_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    inicio_novo_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fim_novo_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    solicitado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_aulas_reagendamentos", x => x.id);
                    table.ForeignKey(
                        name: "fk_aulas_reagendamentos_aulas_aula_id",
                        column: x => x.aula_id,
                        principalTable: "aulas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_aulas_reagendamentos_usuarios_solicitado_por_usuario_id",
                        column: x => x.solicitado_por_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cobrancas_assinatura",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    assinatura_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pagador_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ano_competencia = table.Column<int>(type: "integer", nullable: false),
                    mes_competencia = table.Column<int>(type: "integer", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    forma_pagamento = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    paid_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cobrancas_assinatura", x => x.id);
                    table.ForeignKey(
                        name: "fk_cobrancas_assinatura_assinaturas_aluno_assinatura_id",
                        column: x => x.assinatura_id,
                        principalTable: "assinaturas_aluno",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cobrancas_assinatura_usuarios_pagador_usuario_id",
                        column: x => x.pagador_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_aulas_reagendamentos_aula_id_created_at_utc",
                table: "aulas_reagendamentos",
                columns: new[] { "aula_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_aulas_reagendamentos_solicitado_por_usuario_id",
                table: "aulas_reagendamentos",
                column: "solicitado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_cobrancas_assinatura_assinatura_id_ano_competencia_mes_comp~",
                table: "cobrancas_assinatura",
                columns: new[] { "assinatura_id", "ano_competencia", "mes_competencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cobrancas_assinatura_pagador_usuario_id_created_at_utc",
                table: "cobrancas_assinatura",
                columns: new[] { "pagador_usuario_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_cobrancas_assinatura_status",
                table: "cobrancas_assinatura",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aulas_reagendamentos");

            migrationBuilder.DropTable(
                name: "cobrancas_assinatura");

            migrationBuilder.DropColumn(
                name: "forma_pagamento",
                table: "pagamentos_aula");
        }
    }
}
