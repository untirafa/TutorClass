-- =====================================================================
-- TutorClass - Solicitacoes de vinculo responsavel-aluno (aplicar no Neon)
-- Cria a tabela caso a migration 20261116200000_AdicionaSolicitacoesVinculoResponsavel
-- ainda nao tenha sido aplicada. Idempotente: pode ser executado mais de uma vez.
-- =====================================================================

CREATE TABLE IF NOT EXISTS solicitacoes_vinculo (
    id uuid NOT NULL,
    responsavel_id uuid NOT NULL,
    nome_aluno_informado character varying(200) NOT NULL,
    parentesco character varying(80) NOT NULL,
    status character varying(20) NOT NULL,
    aluno_id uuid NULL,
    analisada_por_id uuid NULL,
    analisada_em_utc timestamp with time zone NULL,
    motivo character varying(500) NULL,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NULL,
    CONSTRAINT pk_solicitacoes_vinculo PRIMARY KEY (id),
    CONSTRAINT fk_solicitacoes_vinculo_usuarios_aluno_id FOREIGN KEY (aluno_id) REFERENCES usuarios (id) ON DELETE RESTRICT,
    CONSTRAINT fk_solicitacoes_vinculo_usuarios_responsavel_id FOREIGN KEY (responsavel_id) REFERENCES usuarios (id) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_solicitacoes_vinculo_aluno_id ON solicitacoes_vinculo (aluno_id);
CREATE INDEX IF NOT EXISTS ix_solicitacoes_vinculo_responsavel_id_status ON solicitacoes_vinculo (responsavel_id, status);
CREATE INDEX IF NOT EXISTS ix_solicitacoes_vinculo_status_created_at_utc ON solicitacoes_vinculo (status, created_at_utc);

-- Registra a migration para o historico do EF ficar coerente com o banco.
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20261116200000_AdicionaSolicitacoesVinculoResponsavel', '10.0.4'
WHERE NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116200000_AdicionaSolicitacoesVinculoResponsavel');
