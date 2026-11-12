-- =====================================================================
-- TutorClass - Triggers de integridade das denuncias (aplicar no Neon)
-- Pre-requisito: a tabela "denuncias" ja existe (criada pela migration
-- 20261111140000_AdicionaDenunciasModeracao, aplicada no deploy da API).
-- Idempotente: pode ser executado mais de uma vez.
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
