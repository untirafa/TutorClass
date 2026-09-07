using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TutorClass.Infrastructure.Persistence;

#nullable disable

namespace TutorClass.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TutorClassDbContext))]
[Migration("20260829210000_AdicionaAcompanhamentoAvaliacoesReputacao")]
public partial class AdicionaAcompanhamentoAvaliacoesReputacao : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "acompanhamentos_aula",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                aula_id = table.Column<Guid>(type: "uuid", nullable: false),
                aluno_id = table.Column<Guid>(type: "uuid", nullable: false),
                professor_id = table.Column<Guid>(type: "uuid", nullable: false),
                resumo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                pontos_fortes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                pontos_melhorar = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                proximos_passos = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_acompanhamentos_aula", x => x.id);
                table.ForeignKey("fk_acompanhamentos_aula_aulas_aula_id", x => x.aula_id, "aulas", "id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("fk_acompanhamentos_aula_usuarios_aluno_id", x => x.aluno_id, "usuarios", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_acompanhamentos_aula_usuarios_professor_id", x => x.professor_id, "usuarios", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "avaliacoes_professor",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                aula_id = table.Column<Guid>(type: "uuid", nullable: false),
                aluno_id = table.Column<Guid>(type: "uuid", nullable: false),
                professor_id = table.Column<Guid>(type: "uuid", nullable: false),
                nota = table.Column<int>(type: "integer", nullable: false),
                comentario = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_avaliacoes_professor", x => x.id);
                table.ForeignKey("fk_avaliacoes_professor_aulas_aula_id", x => x.aula_id, "aulas", "id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("fk_avaliacoes_professor_usuarios_aluno_id", x => x.aluno_id, "usuarios", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_avaliacoes_professor_usuarios_professor_id", x => x.professor_id, "usuarios", "id", onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("ck_avaliacoes_professor_nota", "nota >= 1 AND nota <= 5");
            });

        migrationBuilder.CreateIndex("ix_acompanhamentos_aula_aula_id", "acompanhamentos_aula", "aula_id", unique: true);
        migrationBuilder.CreateIndex("ix_acompanhamentos_aula_aluno_id_created_at_utc", "acompanhamentos_aula", new[] { "aluno_id", "created_at_utc" });
        migrationBuilder.CreateIndex("ix_acompanhamentos_aula_professor_id_created_at_utc", "acompanhamentos_aula", new[] { "professor_id", "created_at_utc" });
        migrationBuilder.CreateIndex("ix_avaliacoes_professor_aula_id", "avaliacoes_professor", "aula_id", unique: true);
        migrationBuilder.CreateIndex("ix_avaliacoes_professor_aluno_id", "avaliacoes_professor", "aluno_id");
        migrationBuilder.CreateIndex("ix_avaliacoes_professor_professor_id_created_at_utc", "avaliacoes_professor", new[] { "professor_id", "created_at_utc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "acompanhamentos_aula");
        migrationBuilder.DropTable(name: "avaliacoes_professor");
    }
}
