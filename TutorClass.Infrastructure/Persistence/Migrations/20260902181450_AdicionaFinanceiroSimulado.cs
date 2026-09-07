using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorClass.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaFinanceiroSimulado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "pagador_usuario_id",
                table: "assinaturas_aluno",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("UPDATE assinaturas_aluno SET pagador_usuario_id = aluno_id WHERE pagador_usuario_id IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "pagador_usuario_id",
                table: "assinaturas_aluno",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "pagamentos_aula",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    aula_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aluno_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pagador_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    professor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valor_bruto = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    percentual_comissao = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    valor_comissao = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    valor_liquido_professor = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    data_pagamento_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pagamentos_aula", x => x.id);
                    table.ForeignKey(
                        name: "fk_pagamentos_aula_aulas_aula_id",
                        column: x => x.aula_id,
                        principalTable: "aulas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_pagamentos_aula_usuarios_aluno_id",
                        column: x => x.aluno_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pagamentos_aula_usuarios_pagador_usuario_id",
                        column: x => x.pagador_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pagamentos_aula_usuarios_professor_id",
                        column: x => x.professor_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_assinaturas_aluno_pagador_usuario_id",
                table: "assinaturas_aluno",
                column: "pagador_usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_pagamentos_aula_aluno_id_created_at_utc",
                table: "pagamentos_aula",
                columns: new[] { "aluno_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_pagamentos_aula_aula_id",
                table: "pagamentos_aula",
                column: "aula_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pagamentos_aula_pagador_usuario_id_created_at_utc",
                table: "pagamentos_aula",
                columns: new[] { "pagador_usuario_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_pagamentos_aula_professor_id_created_at_utc",
                table: "pagamentos_aula",
                columns: new[] { "professor_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_pagamentos_aula_status",
                table: "pagamentos_aula",
                column: "status");

            migrationBuilder.AddForeignKey(
                name: "fk_assinaturas_aluno_usuarios_pagador_usuario_id",
                table: "assinaturas_aluno",
                column: "pagador_usuario_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_assinaturas_aluno_usuarios_pagador_usuario_id",
                table: "assinaturas_aluno");

            migrationBuilder.DropTable(
                name: "pagamentos_aula");

            migrationBuilder.DropIndex(
                name: "ix_assinaturas_aluno_pagador_usuario_id",
                table: "assinaturas_aluno");

            migrationBuilder.DropColumn(
                name: "pagador_usuario_id",
                table: "assinaturas_aluno");
        }
    }
}
