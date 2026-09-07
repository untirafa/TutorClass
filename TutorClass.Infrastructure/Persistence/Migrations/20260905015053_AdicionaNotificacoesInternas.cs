using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorClass.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaNotificacoesInternas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notificacoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    titulo = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    mensagem = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    lida = table.Column<bool>(type: "boolean", nullable: false),
                    data_leitura_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rota_acao = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    referencia_id = table.Column<Guid>(type: "uuid", nullable: true),
                    chave_evento = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    prioridade = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notificacoes", x => x.id);
                    table.ForeignKey(
                        name: "fk_notificacoes_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notificacoes_chave_evento",
                table: "notificacoes",
                column: "chave_evento",
                unique: true,
                filter: "chave_evento IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notificacoes_usuario_id_lida_created_at_utc",
                table: "notificacoes",
                columns: new[] { "usuario_id", "lida", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notificacoes");
        }
    }
}
