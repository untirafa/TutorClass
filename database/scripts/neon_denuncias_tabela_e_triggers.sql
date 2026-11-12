-- =====================================================================
-- TutorClass - Tabela de denuncias + triggers de integridade (aplicar no Neon)
-- Use quando a tabela "denuncias" ainda nao existe no banco (a API publicada
-- nao aplica migrations automaticamente). Idempotente: pode ser repetido.
-- Pre-requisito: a tabela "usuarios" ja existe.
-- =====================================================================

CREATE TABLE IF NOT EXISTS denuncias (
    id uuid NOT NULL,
    denunciante_id uuid NOT NULL,
    alvo_usuario_id uuid NOT NULL,
    tipo_alvo character varying(30) NOT NULL,
    alvo_id character varying(64) NOT NULL,
    motivo character varying(40) NOT NULL,
    descricao character varying(500),
    resumo_alvo character varying(500) NOT NULL,
    vinculo_mentoria_id uuid,
    status character varying(20) NOT NULL,
    sancao character varying(20) NOT NULL,
    observacao_moderacao character varying(500),
    analisada_por_id uuid,
    analisada_em_utc timestamp with time zone,
    conta_reativada_em_utc timestamp with time zone,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone,
    CONSTRAINT pk_denuncias PRIMARY KEY (id),
    CONSTRAINT fk_denuncias_usuarios_alvo_usuario_id FOREIGN KEY (alvo_usuario_id) REFERENCES usuarios (id) ON DELETE RESTRICT,
    CONSTRAINT fk_denuncias_usuarios_denunciante_id FOREIGN KEY (denunciante_id) REFERENCES usuarios (id) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_denuncias_alvo_usuario_id ON denuncias (alvo_usuario_id);
CREATE INDEX IF NOT EXISTS ix_denuncias_denunciante_id_tipo_alvo_alvo_id ON denuncias (denunciante_id, tipo_alvo, alvo_id);
CREATE INDEX IF NOT EXISTS ix_denuncias_status_created_at_utc ON denuncias (status, created_at_utc);

DO $EF$
BEGIN
    IF to_regclass('"__EFMigrationsHistory"') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111140000_AdicionaDenunciasModeracao') THEN
        INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
        VALUES ('20261111140000_AdicionaDenunciasModeracao', '10.0.4');
    END IF;
END $EF$;

-- =====================================================================

-- =====================================================================
-- PARTE 3 - INTEGRIDADE DAS DENUNCIAS E DA MODERACAO
-- Garante, no proprio banco, que uma denuncia decidida tenha administrador
-- e justificativa e que sancao so exista em denuncia procedente.
-- =====================================================================

CREATE OR REPLACE FUNCTION fn_atualizar_updated_at() RETURNS trigger AS $$
BEGIN
    NEW.updated_at_utc := NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Denuncias: updated_at automatico e coerencia da decisao de moderacao.
CREATE OR REPLACE FUNCTION fn_validar_decisao_denuncia() RETURNS trigger AS $$
BEGIN
    IF NEW.status IN ('Procedente', 'Improcedente') THEN
        IF NEW.analisada_por_id IS NULL OR COALESCE(BTRIM(NEW.observacao_moderacao), '') = '' THEN
            RAISE EXCEPTION 'Denuncia decidida exige administrador e justificativa.'
                USING ERRCODE = 'check_violation';
        END IF;
    END IF;
    IF NEW.status <> 'Procedente' AND NEW.sancao <> 'Nenhuma' THEN
        RAISE EXCEPTION 'Sancao so pode ser aplicada a denuncia procedente.'
            USING ERRCODE = 'check_violation';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_denuncias_atualizar_updated_at ON denuncias;
CREATE TRIGGER trg_denuncias_atualizar_updated_at
    BEFORE UPDATE ON denuncias
    FOR EACH ROW EXECUTE FUNCTION fn_atualizar_updated_at();

DROP TRIGGER IF EXISTS trg_validar_decisao_denuncia ON denuncias;
CREATE TRIGGER trg_validar_decisao_denuncia
    BEFORE INSERT OR UPDATE ON denuncias
    FOR EACH ROW EXECUTE FUNCTION fn_validar_decisao_denuncia();
