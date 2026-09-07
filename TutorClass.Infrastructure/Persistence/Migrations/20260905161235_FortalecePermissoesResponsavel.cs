using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorClass.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FortalecePermissoesResponsavel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "solicitada_por_usuario_id",
                table: "aulas",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE aulas
                SET solicitada_por_usuario_id = aluno_id
                WHERE solicitada_por_usuario_id IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "solicitada_por_usuario_id",
                table: "aulas",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_aulas_solicitada_por_usuario_id",
                table: "aulas",
                column: "solicitada_por_usuario_id");

            migrationBuilder.AddForeignKey(
                name: "fk_aulas_usuarios_solicitada_por_usuario_id",
                table: "aulas",
                column: "solicitada_por_usuario_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_aulas_usuarios_solicitada_por_usuario_id",
                table: "aulas");

            migrationBuilder.DropIndex(
                name: "ix_aulas_solicitada_por_usuario_id",
                table: "aulas");

            migrationBuilder.DropColumn(
                name: "solicitada_por_usuario_id",
                table: "aulas");
        }
    }
}
