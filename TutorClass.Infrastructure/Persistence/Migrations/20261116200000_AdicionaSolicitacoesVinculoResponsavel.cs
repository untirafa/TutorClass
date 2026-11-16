using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorClass.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(TutorClassDbContext))]
    [Migration("20261116200000_AdicionaSolicitacoesVinculoResponsavel")]
    public partial class AdicionaSolicitacoesVinculoResponsavel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "solicitacoes_vinculo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    responsavel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome_aluno_informado = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    parentesco = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    aluno_id = table.Column<Guid>(type: "uuid", nullable: true),
                    analisada_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    analisada_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_solicitacoes_vinculo", x => x.id);
                    table.ForeignKey(
                        name: "fk_solicitacoes_vinculo_usuarios_aluno_id",
                        column: x => x.aluno_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_solicitacoes_vinculo_usuarios_responsavel_id",
                        column: x => x.responsavel_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_solicitacoes_vinculo_aluno_id",
                table: "solicitacoes_vinculo",
                column: "aluno_id");

            migrationBuilder.CreateIndex(
                name: "ix_solicitacoes_vinculo_responsavel_id_status",
                table: "solicitacoes_vinculo",
                columns: new[] { "responsavel_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_solicitacoes_vinculo_status_created_at_utc",
                table: "solicitacoes_vinculo",
                columns: new[] { "status", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "solicitacoes_vinculo");
        }
    }
}
