using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorClass.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaFaqChatbot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "perguntas_frequentes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pergunta = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    resposta = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    categoria = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    palavras_chave = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ordem_exibicao = table.Column<int>(type: "integer", nullable: false),
                    texto_acao = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    rota_acao = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_perguntas_frequentes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_perguntas_frequentes_ativo_categoria_ordem_exibicao",
                table: "perguntas_frequentes",
                columns: new[] { "ativo", "categoria", "ordem_exibicao" });

            migrationBuilder.CreateIndex(
                name: "ix_perguntas_frequentes_pergunta",
                table: "perguntas_frequentes",
                column: "pergunta",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "perguntas_frequentes");
        }
    }
}
