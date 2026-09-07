using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorClass.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PadronizaSnakeCasePostgreSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_candidaturas_professor_usuarios_ProfessorId",
                table: "candidaturas_professor");

            migrationBuilder.DropForeignKey(
                name: "FK_disponibilidades_professor_usuarios_ProfessorId",
                table: "disponibilidades_professor");

            migrationBuilder.DropForeignKey(
                name: "FK_professores_materias_materias_MateriaId",
                table: "professores_materias");

            migrationBuilder.DropForeignKey(
                name: "FK_professores_materias_usuarios_ProfessorId",
                table: "professores_materias");

            migrationBuilder.DropForeignKey(
                name: "FK_qualificacoes_professor_usuarios_ProfessorId",
                table: "qualificacoes_professor");

            migrationBuilder.DropForeignKey(
                name: "FK_responsaveis_alunos_usuarios_AlunoId",
                table: "responsaveis_alunos");

            migrationBuilder.DropForeignKey(
                name: "FK_responsaveis_alunos_usuarios_ResponsavelId",
                table: "responsaveis_alunos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_usuarios",
                table: "usuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_responsaveis_alunos",
                table: "responsaveis_alunos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_qualificacoes_professor",
                table: "qualificacoes_professor");

            migrationBuilder.DropPrimaryKey(
                name: "PK_professores_materias",
                table: "professores_materias");

            migrationBuilder.DropPrimaryKey(
                name: "PK_materias",
                table: "materias");

            migrationBuilder.DropPrimaryKey(
                name: "PK_disponibilidades_professor",
                table: "disponibilidades_professor");

            migrationBuilder.DropPrimaryKey(
                name: "PK_candidaturas_professor",
                table: "candidaturas_professor");

            migrationBuilder.RenameColumn(
                name: "Telefone",
                table: "usuarios",
                newName: "telefone");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "usuarios",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "usuarios",
                newName: "nome");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "usuarios",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Biografia",
                table: "usuarios",
                newName: "biografia");

            migrationBuilder.RenameColumn(
                name: "Ativo",
                table: "usuarios",
                newName: "ativo");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "usuarios",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "usuarios",
                newName: "updated_at_utc");

            migrationBuilder.RenameColumn(
                name: "QuantidadeAvaliacoes",
                table: "usuarios",
                newName: "quantidade_avaliacoes");

            migrationBuilder.RenameColumn(
                name: "PrecoHora",
                table: "usuarios",
                newName: "preco_hora");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "usuarios",
                newName: "password_hash");

            migrationBuilder.RenameColumn(
                name: "NivelReputacao",
                table: "usuarios",
                newName: "nivel_reputacao");

            migrationBuilder.RenameColumn(
                name: "MediaAvaliacoes",
                table: "usuarios",
                newName: "media_avaliacoes");

            migrationBuilder.RenameColumn(
                name: "IdentidadeVerificada",
                table: "usuarios",
                newName: "identidade_verificada");

            migrationBuilder.RenameColumn(
                name: "DataNascimento",
                table: "usuarios",
                newName: "data_nascimento");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "usuarios",
                newName: "created_at_utc");

            migrationBuilder.RenameColumn(
                name: "AulasConcluidas",
                table: "usuarios",
                newName: "aulas_concluidas");

            migrationBuilder.RenameIndex(
                name: "IX_usuarios_Status",
                table: "usuarios",
                newName: "ix_usuarios_status");

            migrationBuilder.RenameIndex(
                name: "IX_usuarios_Email",
                table: "usuarios",
                newName: "ix_usuarios_email");

            migrationBuilder.RenameColumn(
                name: "Parentesco",
                table: "responsaveis_alunos",
                newName: "parentesco");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "responsaveis_alunos",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "responsaveis_alunos",
                newName: "updated_at_utc");

            migrationBuilder.RenameColumn(
                name: "ResponsavelId",
                table: "responsaveis_alunos",
                newName: "responsavel_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "responsaveis_alunos",
                newName: "created_at_utc");

            migrationBuilder.RenameColumn(
                name: "AlunoId",
                table: "responsaveis_alunos",
                newName: "aluno_id");

            migrationBuilder.RenameIndex(
                name: "IX_responsaveis_alunos_ResponsavelId_AlunoId",
                table: "responsaveis_alunos",
                newName: "ix_responsaveis_alunos_responsavel_id_aluno_id");

            migrationBuilder.RenameIndex(
                name: "IX_responsaveis_alunos_AlunoId",
                table: "responsaveis_alunos",
                newName: "ix_responsaveis_alunos_aluno_id");

            migrationBuilder.RenameColumn(
                name: "Titulo",
                table: "qualificacoes_professor",
                newName: "titulo");

            migrationBuilder.RenameColumn(
                name: "Tipo",
                table: "qualificacoes_professor",
                newName: "tipo");

            migrationBuilder.RenameColumn(
                name: "Instituicao",
                table: "qualificacoes_professor",
                newName: "instituicao");

            migrationBuilder.RenameColumn(
                name: "Descricao",
                table: "qualificacoes_professor",
                newName: "descricao");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "qualificacoes_professor",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "qualificacoes_professor",
                newName: "updated_at_utc");

            migrationBuilder.RenameColumn(
                name: "StatusVerificacao",
                table: "qualificacoes_professor",
                newName: "status_verificacao");

            migrationBuilder.RenameColumn(
                name: "ProfessorId",
                table: "qualificacoes_professor",
                newName: "professor_id");

            migrationBuilder.RenameColumn(
                name: "DocumentoComprovacao",
                table: "qualificacoes_professor",
                newName: "documento_comprovacao");

            migrationBuilder.RenameColumn(
                name: "DataVerificacaoUtc",
                table: "qualificacoes_professor",
                newName: "data_verificacao_utc");

            migrationBuilder.RenameColumn(
                name: "DataConclusao",
                table: "qualificacoes_professor",
                newName: "data_conclusao");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "qualificacoes_professor",
                newName: "created_at_utc");

            migrationBuilder.RenameIndex(
                name: "IX_qualificacoes_professor_ProfessorId_StatusVerificacao",
                table: "qualificacoes_professor",
                newName: "ix_qualificacoes_professor_professor_id_status_verificacao");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "professores_materias",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "professores_materias",
                newName: "updated_at_utc");

            migrationBuilder.RenameColumn(
                name: "ProfessorId",
                table: "professores_materias",
                newName: "professor_id");

            migrationBuilder.RenameColumn(
                name: "MateriaId",
                table: "professores_materias",
                newName: "materia_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "professores_materias",
                newName: "created_at_utc");

            migrationBuilder.RenameIndex(
                name: "IX_professores_materias_ProfessorId_MateriaId",
                table: "professores_materias",
                newName: "ix_professores_materias_professor_id_materia_id");

            migrationBuilder.RenameIndex(
                name: "IX_professores_materias_MateriaId",
                table: "professores_materias",
                newName: "ix_professores_materias_materia_id");

            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "materias",
                newName: "nome");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "materias",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "materias",
                newName: "updated_at_utc");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "materias",
                newName: "created_at_utc");

            migrationBuilder.RenameIndex(
                name: "IX_materias_Nome",
                table: "materias",
                newName: "ix_materias_nome");

            migrationBuilder.RenameColumn(
                name: "Inicio",
                table: "disponibilidades_professor",
                newName: "inicio");

            migrationBuilder.RenameColumn(
                name: "Fim",
                table: "disponibilidades_professor",
                newName: "fim");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "disponibilidades_professor",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "disponibilidades_professor",
                newName: "updated_at_utc");

            migrationBuilder.RenameColumn(
                name: "ProfessorId",
                table: "disponibilidades_professor",
                newName: "professor_id");

            migrationBuilder.RenameColumn(
                name: "DiaSemana",
                table: "disponibilidades_professor",
                newName: "dia_semana");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "disponibilidades_professor",
                newName: "created_at_utc");

            migrationBuilder.RenameIndex(
                name: "IX_disponibilidades_professor_ProfessorId_DiaSemana_Inicio_Fim",
                table: "disponibilidades_professor",
                newName: "ix_disponibilidades_professor_periodo");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "candidaturas_professor",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Motivo",
                table: "candidaturas_professor",
                newName: "motivo");

            migrationBuilder.RenameColumn(
                name: "Mensagem",
                table: "candidaturas_professor",
                newName: "mensagem");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "candidaturas_professor",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "candidaturas_professor",
                newName: "updated_at_utc");

            migrationBuilder.RenameColumn(
                name: "ProfessorId",
                table: "candidaturas_professor",
                newName: "professor_id");

            migrationBuilder.RenameColumn(
                name: "DataAnaliseUtc",
                table: "candidaturas_professor",
                newName: "data_analise_utc");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "candidaturas_professor",
                newName: "created_at_utc");

            migrationBuilder.RenameColumn(
                name: "AdministradorAnaliseId",
                table: "candidaturas_professor",
                newName: "administrador_analise_id");

            migrationBuilder.RenameIndex(
                name: "IX_candidaturas_professor_ProfessorId_Status",
                table: "candidaturas_professor",
                newName: "ix_candidaturas_professor_professor_id_status");

            migrationBuilder.AddPrimaryKey(
                name: "pk_usuarios",
                table: "usuarios",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_responsaveis_alunos",
                table: "responsaveis_alunos",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_qualificacoes_professor",
                table: "qualificacoes_professor",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_professores_materias",
                table: "professores_materias",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_materias",
                table: "materias",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_disponibilidades_professor",
                table: "disponibilidades_professor",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_candidaturas_professor",
                table: "candidaturas_professor",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_candidaturas_professor_usuarios_professor_id",
                table: "candidaturas_professor",
                column: "professor_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_disponibilidades_professor_usuarios_professor_id",
                table: "disponibilidades_professor",
                column: "professor_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_professores_materias_materias_materia_id",
                table: "professores_materias",
                column: "materia_id",
                principalTable: "materias",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_professores_materias_usuarios_professor_id",
                table: "professores_materias",
                column: "professor_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_qualificacoes_professor_usuarios_professor_id",
                table: "qualificacoes_professor",
                column: "professor_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_responsaveis_alunos_usuarios_aluno_id",
                table: "responsaveis_alunos",
                column: "aluno_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_responsaveis_alunos_usuarios_responsavel_id",
                table: "responsaveis_alunos",
                column: "responsavel_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    legacy_professor_id uuid;
                BEGIN
                    SELECT id
                    INTO legacy_professor_id
                    FROM usuarios
                    WHERE email = 'professor.dev.20260824002024929@tutorclass.local'
                      AND tipo_usuario = 'Professor';

                    IF legacy_professor_id IS NOT NULL THEN
                        DELETE FROM candidaturas_professor
                        WHERE professor_id = legacy_professor_id;

                        DELETE FROM disponibilidades_professor
                        WHERE professor_id = legacy_professor_id;

                        DELETE FROM professores_materias
                        WHERE professor_id = legacy_professor_id;

                        DELETE FROM qualificacoes_professor
                        WHERE professor_id = legacy_professor_id;

                        DELETE FROM usuarios
                        WHERE id = legacy_professor_id
                          AND email = 'professor.dev.20260824002024929@tutorclass.local'
                          AND tipo_usuario = 'Professor';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE VIEW vw_professores AS
                SELECT
                    id,
                    nome,
                    email,
                    ativo,
                    status,
                    biografia,
                    preco_hora,
                    nivel_reputacao,
                    media_avaliacoes,
                    quantidade_avaliacoes,
                    created_at_utc,
                    updated_at_utc
                FROM usuarios
                WHERE tipo_usuario = 'Professor';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_professores;");

            migrationBuilder.DropForeignKey(
                name: "fk_candidaturas_professor_usuarios_professor_id",
                table: "candidaturas_professor");

            migrationBuilder.DropForeignKey(
                name: "fk_disponibilidades_professor_usuarios_professor_id",
                table: "disponibilidades_professor");

            migrationBuilder.DropForeignKey(
                name: "fk_professores_materias_materias_materia_id",
                table: "professores_materias");

            migrationBuilder.DropForeignKey(
                name: "fk_professores_materias_usuarios_professor_id",
                table: "professores_materias");

            migrationBuilder.DropForeignKey(
                name: "fk_qualificacoes_professor_usuarios_professor_id",
                table: "qualificacoes_professor");

            migrationBuilder.DropForeignKey(
                name: "fk_responsaveis_alunos_usuarios_aluno_id",
                table: "responsaveis_alunos");

            migrationBuilder.DropForeignKey(
                name: "fk_responsaveis_alunos_usuarios_responsavel_id",
                table: "responsaveis_alunos");

            migrationBuilder.DropPrimaryKey(
                name: "pk_usuarios",
                table: "usuarios");

            migrationBuilder.DropPrimaryKey(
                name: "pk_responsaveis_alunos",
                table: "responsaveis_alunos");

            migrationBuilder.DropPrimaryKey(
                name: "pk_qualificacoes_professor",
                table: "qualificacoes_professor");

            migrationBuilder.DropPrimaryKey(
                name: "pk_professores_materias",
                table: "professores_materias");

            migrationBuilder.DropPrimaryKey(
                name: "pk_materias",
                table: "materias");

            migrationBuilder.DropPrimaryKey(
                name: "pk_disponibilidades_professor",
                table: "disponibilidades_professor");

            migrationBuilder.DropPrimaryKey(
                name: "pk_candidaturas_professor",
                table: "candidaturas_professor");

            migrationBuilder.RenameColumn(
                name: "telefone",
                table: "usuarios",
                newName: "Telefone");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "usuarios",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "nome",
                table: "usuarios",
                newName: "Nome");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "usuarios",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "biografia",
                table: "usuarios",
                newName: "Biografia");

            migrationBuilder.RenameColumn(
                name: "ativo",
                table: "usuarios",
                newName: "Ativo");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "usuarios",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at_utc",
                table: "usuarios",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "quantidade_avaliacoes",
                table: "usuarios",
                newName: "QuantidadeAvaliacoes");

            migrationBuilder.RenameColumn(
                name: "preco_hora",
                table: "usuarios",
                newName: "PrecoHora");

            migrationBuilder.RenameColumn(
                name: "password_hash",
                table: "usuarios",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "nivel_reputacao",
                table: "usuarios",
                newName: "NivelReputacao");

            migrationBuilder.RenameColumn(
                name: "media_avaliacoes",
                table: "usuarios",
                newName: "MediaAvaliacoes");

            migrationBuilder.RenameColumn(
                name: "identidade_verificada",
                table: "usuarios",
                newName: "IdentidadeVerificada");

            migrationBuilder.RenameColumn(
                name: "data_nascimento",
                table: "usuarios",
                newName: "DataNascimento");

            migrationBuilder.RenameColumn(
                name: "created_at_utc",
                table: "usuarios",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "aulas_concluidas",
                table: "usuarios",
                newName: "AulasConcluidas");

            migrationBuilder.RenameIndex(
                name: "ix_usuarios_status",
                table: "usuarios",
                newName: "IX_usuarios_Status");

            migrationBuilder.RenameIndex(
                name: "ix_usuarios_email",
                table: "usuarios",
                newName: "IX_usuarios_Email");

            migrationBuilder.RenameColumn(
                name: "parentesco",
                table: "responsaveis_alunos",
                newName: "Parentesco");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "responsaveis_alunos",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at_utc",
                table: "responsaveis_alunos",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "responsavel_id",
                table: "responsaveis_alunos",
                newName: "ResponsavelId");

            migrationBuilder.RenameColumn(
                name: "created_at_utc",
                table: "responsaveis_alunos",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "aluno_id",
                table: "responsaveis_alunos",
                newName: "AlunoId");

            migrationBuilder.RenameIndex(
                name: "ix_responsaveis_alunos_responsavel_id_aluno_id",
                table: "responsaveis_alunos",
                newName: "IX_responsaveis_alunos_ResponsavelId_AlunoId");

            migrationBuilder.RenameIndex(
                name: "ix_responsaveis_alunos_aluno_id",
                table: "responsaveis_alunos",
                newName: "IX_responsaveis_alunos_AlunoId");

            migrationBuilder.RenameColumn(
                name: "titulo",
                table: "qualificacoes_professor",
                newName: "Titulo");

            migrationBuilder.RenameColumn(
                name: "tipo",
                table: "qualificacoes_professor",
                newName: "Tipo");

            migrationBuilder.RenameColumn(
                name: "instituicao",
                table: "qualificacoes_professor",
                newName: "Instituicao");

            migrationBuilder.RenameColumn(
                name: "descricao",
                table: "qualificacoes_professor",
                newName: "Descricao");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "qualificacoes_professor",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at_utc",
                table: "qualificacoes_professor",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "status_verificacao",
                table: "qualificacoes_professor",
                newName: "StatusVerificacao");

            migrationBuilder.RenameColumn(
                name: "professor_id",
                table: "qualificacoes_professor",
                newName: "ProfessorId");

            migrationBuilder.RenameColumn(
                name: "documento_comprovacao",
                table: "qualificacoes_professor",
                newName: "DocumentoComprovacao");

            migrationBuilder.RenameColumn(
                name: "data_verificacao_utc",
                table: "qualificacoes_professor",
                newName: "DataVerificacaoUtc");

            migrationBuilder.RenameColumn(
                name: "data_conclusao",
                table: "qualificacoes_professor",
                newName: "DataConclusao");

            migrationBuilder.RenameColumn(
                name: "created_at_utc",
                table: "qualificacoes_professor",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "ix_qualificacoes_professor_professor_id_status_verificacao",
                table: "qualificacoes_professor",
                newName: "IX_qualificacoes_professor_ProfessorId_StatusVerificacao");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "professores_materias",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at_utc",
                table: "professores_materias",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "professor_id",
                table: "professores_materias",
                newName: "ProfessorId");

            migrationBuilder.RenameColumn(
                name: "materia_id",
                table: "professores_materias",
                newName: "MateriaId");

            migrationBuilder.RenameColumn(
                name: "created_at_utc",
                table: "professores_materias",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "ix_professores_materias_professor_id_materia_id",
                table: "professores_materias",
                newName: "IX_professores_materias_ProfessorId_MateriaId");

            migrationBuilder.RenameIndex(
                name: "ix_professores_materias_materia_id",
                table: "professores_materias",
                newName: "IX_professores_materias_MateriaId");

            migrationBuilder.RenameColumn(
                name: "nome",
                table: "materias",
                newName: "Nome");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "materias",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at_utc",
                table: "materias",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "created_at_utc",
                table: "materias",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "ix_materias_nome",
                table: "materias",
                newName: "IX_materias_Nome");

            migrationBuilder.RenameColumn(
                name: "inicio",
                table: "disponibilidades_professor",
                newName: "Inicio");

            migrationBuilder.RenameColumn(
                name: "fim",
                table: "disponibilidades_professor",
                newName: "Fim");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "disponibilidades_professor",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at_utc",
                table: "disponibilidades_professor",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "professor_id",
                table: "disponibilidades_professor",
                newName: "ProfessorId");

            migrationBuilder.RenameColumn(
                name: "dia_semana",
                table: "disponibilidades_professor",
                newName: "DiaSemana");

            migrationBuilder.RenameColumn(
                name: "created_at_utc",
                table: "disponibilidades_professor",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "ix_disponibilidades_professor_periodo",
                table: "disponibilidades_professor",
                newName: "IX_disponibilidades_professor_ProfessorId_DiaSemana_Inicio_Fim");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "candidaturas_professor",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "motivo",
                table: "candidaturas_professor",
                newName: "Motivo");

            migrationBuilder.RenameColumn(
                name: "mensagem",
                table: "candidaturas_professor",
                newName: "Mensagem");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "candidaturas_professor",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at_utc",
                table: "candidaturas_professor",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "professor_id",
                table: "candidaturas_professor",
                newName: "ProfessorId");

            migrationBuilder.RenameColumn(
                name: "data_analise_utc",
                table: "candidaturas_professor",
                newName: "DataAnaliseUtc");

            migrationBuilder.RenameColumn(
                name: "created_at_utc",
                table: "candidaturas_professor",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "administrador_analise_id",
                table: "candidaturas_professor",
                newName: "AdministradorAnaliseId");

            migrationBuilder.RenameIndex(
                name: "ix_candidaturas_professor_professor_id_status",
                table: "candidaturas_professor",
                newName: "IX_candidaturas_professor_ProfessorId_Status");

            migrationBuilder.AddPrimaryKey(
                name: "PK_usuarios",
                table: "usuarios",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_responsaveis_alunos",
                table: "responsaveis_alunos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_qualificacoes_professor",
                table: "qualificacoes_professor",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_professores_materias",
                table: "professores_materias",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_materias",
                table: "materias",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_disponibilidades_professor",
                table: "disponibilidades_professor",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_candidaturas_professor",
                table: "candidaturas_professor",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_candidaturas_professor_usuarios_ProfessorId",
                table: "candidaturas_professor",
                column: "ProfessorId",
                principalTable: "usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_disponibilidades_professor_usuarios_ProfessorId",
                table: "disponibilidades_professor",
                column: "ProfessorId",
                principalTable: "usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_professores_materias_materias_MateriaId",
                table: "professores_materias",
                column: "MateriaId",
                principalTable: "materias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_professores_materias_usuarios_ProfessorId",
                table: "professores_materias",
                column: "ProfessorId",
                principalTable: "usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_qualificacoes_professor_usuarios_ProfessorId",
                table: "qualificacoes_professor",
                column: "ProfessorId",
                principalTable: "usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_responsaveis_alunos_usuarios_AlunoId",
                table: "responsaveis_alunos",
                column: "AlunoId",
                principalTable: "usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_responsaveis_alunos_usuarios_ResponsavelId",
                table: "responsaveis_alunos",
                column: "ResponsavelId",
                principalTable: "usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
