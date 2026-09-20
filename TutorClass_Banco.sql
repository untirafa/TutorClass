CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260823202032_InitialCreate') THEN
    CREATE TABLE materias (
        "Id" uuid NOT NULL,
        "Nome" character varying(80) NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_materias" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260823202032_InitialCreate') THEN
    CREATE TABLE usuarios (
        "Id" uuid NOT NULL,
        "Nome" character varying(150) NOT NULL,
        "Email" character varying(180) NOT NULL,
        "PasswordHash" character varying(500) NOT NULL,
        "Ativo" boolean NOT NULL,
        tipo_usuario character varying(13) NOT NULL,
        "Biografia" character varying(2000),
        "PrecoHora" numeric(10,2),
        "Status" character varying(40),
        "NivelReputacao" character varying(40),
        "IdentidadeVerificada" boolean,
        "MediaAvaliacoes" numeric(3,2),
        "QuantidadeAvaliacoes" integer,
        "AulasConcluidas" integer,
        "DataNascimento" date,
        "Telefone" text,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_usuarios" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260823202032_InitialCreate') THEN
    CREATE TABLE candidaturas_professor (
        "Id" uuid NOT NULL,
        "ProfessorId" uuid NOT NULL,
        "Mensagem" character varying(1000) NOT NULL,
        "Status" character varying(40) NOT NULL,
        "AdministradorAnaliseId" uuid,
        "Motivo" character varying(1000),
        "DataAnaliseUtc" timestamp with time zone,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_candidaturas_professor" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_candidaturas_professor_usuarios_ProfessorId" FOREIGN KEY ("ProfessorId") REFERENCES usuarios ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260823202032_InitialCreate') THEN
    CREATE TABLE disponibilidades_professor (
        "Id" uuid NOT NULL,
        "ProfessorId" uuid NOT NULL,
        "DiaSemana" character varying(20) NOT NULL,
        "Inicio" time without time zone NOT NULL,
        "Fim" time without time zone NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_disponibilidades_professor" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_disponibilidades_professor_usuarios_ProfessorId" FOREIGN KEY ("ProfessorId") REFERENCES usuarios ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260823202032_InitialCreate') THEN
    CREATE TABLE professores_materias (
        "Id" uuid NOT NULL,
        "ProfessorId" uuid NOT NULL,
        "MateriaId" uuid NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_professores_materias" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_professores_materias_materias_MateriaId" FOREIGN KEY ("MateriaId") REFERENCES materias ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_professores_materias_usuarios_ProfessorId" FOREIGN KEY ("ProfessorId") REFERENCES usuarios ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260823202032_InitialCreate') THEN
    CREATE TABLE qualificacoes_professor (
        "Id" uuid NOT NULL,
        "ProfessorId" uuid NOT NULL,
        "Tipo" character varying(40) NOT NULL,
        "Titulo" character varying(180) NOT NULL,
        "Instituicao" character varying(180),
        "Descricao" character varying(2000),
        "DataConclusao" date,
        "DocumentoComprovacao" character varying(500),
        "StatusVerificacao" character varying(40) NOT NULL,
        "DataVerificacaoUtc" timestamp with time zone,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_qualificacoes_professor" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_qualificacoes_professor_usuarios_ProfessorId" FOREIGN KEY ("ProfessorId") REFERENCES usuarios ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260823202032_InitialCreate') THEN
    CREATE TABLE responsaveis_alunos (
        "Id" uuid NOT NULL,
        "ResponsavelId" uuid NOT NULL,
        "AlunoId" uuid NOT NULL,
        "Parentesco" character varying(80) NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_responsaveis_alunos" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_responsaveis_alunos_usuarios_AlunoId" FOREIGN KEY ("AlunoId") REFERENCES usuarios ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_responsaveis_alunos_usuarios_ResponsavelId" FOREIGN KEY ("ResponsavelId") REFERENCES usuarios ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260823202032_InitialCreate') THEN
    INSERT INTO materias ("Id", "CreatedAtUtc", "Nome", "UpdatedAtUtc")
    VALUES ('11111111-1111-1111-1111-111111111111', TIMESTAMPTZ '2026-08-23T00:00:00Z', 'Portugues', NULL);
    INSERT INTO materias ("Id", "CreatedAtUtc", "Nome", "UpdatedAtUtc")
    VALUES ('22222222-2222-2222-2222-222222222222', TIMESTAMPTZ '2026-08-23T00:00:00Z', 'Literatura', NULL);
    INSERT INTO materias ("Id", "CreatedAtUtc", "Nome", "UpdatedAtUtc")
    VALUES ('33333333-3333-3333-3333-333333333333', TIMESTAMPTZ '2026-08-23T00:00:00Z', 'Ingles', NULL);
    INSERT INTO materias ("Id", "CreatedAtUtc", "Nome", "UpdatedAtUtc")
    VALUES ('44444444-4444-4444-4444-444444444444', TIMESTAMPTZ '2026-08-23T00:00:00Z', 'Espanhol', NULL);
    INSERT INTO materias ("Id", "CreatedAtUtc", "Nome", "UpdatedAtUtc")
    VALUES ('55555555-5555-5555-5555-555555555555', TIMESTAMPTZ '2026-08-23T00:00:00Z', 'Artes', NULL);
    INSERT INTO materias ("Id", "CreatedAtUtc", "Nome", "UpdatedAtUtc")
    VALUES ('66666666-6666-6666-6666-666666666666', TIMESTAMPTZ '2026-08-23T00:00:00Z', 'Historia', NULL);
    INSERT INTO materias ("Id", "CreatedAtUtc", "Nome", "UpdatedAtUtc")
    VALUES ('77777777-7777-7777-7777-777777777777', TIMESTAMPTZ '2026-08-23T00:00:00Z', 'Geografia', NULL);
    INSERT INTO materias ("Id", "CreatedAtUtc", "Nome", "UpdatedAtUtc")
    VALUES ('88888888-8888-8888-8888-888888888888', TIMESTAMPTZ '2026-08-23T00:00:00Z', 'Filosofia', NULL);
    INSERT INTO materias ("Id", "CreatedAtUtc", "Nome", "UpdatedAtUtc")
    VALUES ('99999999-9999-9999-9999-999999999999', TIMESTAMPTZ '2026-08-23T00:00:00Z', 'Sociologia', NULL);
    INSERT INTO materias ("Id", "CreatedAtUtc", "Nome", "UpdatedAtUtc")
    VALUES ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', TIMESTAMPTZ '2026-08-23T00:00:00Z', 'Fisica', NULL);
    INSERT INTO materias ("Id", "CreatedAtUtc", "Nome", "UpdatedAtUtc")
    VALUES ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', TIMESTAMPTZ '2026-08-23T00:00:00Z', 'Quimica', NULL);
    INSERT INTO materias ("Id", "CreatedAtUtc", "Nome", "UpdatedAtUtc")
    VALUES ('cccccccc-cccc-cccc-cccc-cccccccccccc', TIMESTAMPTZ '2026-08-23T00:00:00Z', 'Biologia', NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260823202032_InitialCreate') THEN
    CREATE INDEX "IX_candidaturas_professor_ProfessorId_Status" ON candidaturas_professor ("ProfessorId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260823202032_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_disponibilidades_professor_ProfessorId_DiaSemana_Inicio_Fim" ON disponibilidades_professor ("ProfessorId", "DiaSemana", "Inicio", "Fim");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260823202032_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_materias_Nome" ON materias ("Nome");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260823202032_InitialCreate') THEN
    CREATE INDEX "IX_professores_materias_MateriaId" ON professores_materias ("MateriaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260823202032_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_professores_materias_ProfessorId_MateriaId" ON professores_materias ("ProfessorId", "MateriaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260823202032_InitialCreate') THEN
    CREATE INDEX "IX_qualificacoes_professor_ProfessorId_StatusVerificacao" ON qualificacoes_professor ("ProfessorId", "StatusVerificacao");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260823202032_InitialCreate') THEN
    CREATE INDEX "IX_responsaveis_alunos_AlunoId" ON responsaveis_alunos ("AlunoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260823202032_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_responsaveis_alunos_ResponsavelId_AlunoId" ON responsaveis_alunos ("ResponsavelId", "AlunoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260823202032_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_usuarios_Email" ON usuarios ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260823202032_InitialCreate') THEN
    CREATE INDEX "IX_usuarios_Status" ON usuarios ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260823202032_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260823202032_InitialCreate', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE candidaturas_professor DROP CONSTRAINT "FK_candidaturas_professor_usuarios_ProfessorId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE disponibilidades_professor DROP CONSTRAINT "FK_disponibilidades_professor_usuarios_ProfessorId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE professores_materias DROP CONSTRAINT "FK_professores_materias_materias_MateriaId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE professores_materias DROP CONSTRAINT "FK_professores_materias_usuarios_ProfessorId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE qualificacoes_professor DROP CONSTRAINT "FK_qualificacoes_professor_usuarios_ProfessorId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE responsaveis_alunos DROP CONSTRAINT "FK_responsaveis_alunos_usuarios_AlunoId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE responsaveis_alunos DROP CONSTRAINT "FK_responsaveis_alunos_usuarios_ResponsavelId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE usuarios DROP CONSTRAINT "PK_usuarios";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE responsaveis_alunos DROP CONSTRAINT "PK_responsaveis_alunos";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE qualificacoes_professor DROP CONSTRAINT "PK_qualificacoes_professor";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE professores_materias DROP CONSTRAINT "PK_professores_materias";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE materias DROP CONSTRAINT "PK_materias";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE disponibilidades_professor DROP CONSTRAINT "PK_disponibilidades_professor";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE candidaturas_professor DROP CONSTRAINT "PK_candidaturas_professor";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE usuarios RENAME COLUMN "Telefone" TO telefone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE usuarios RENAME COLUMN "Status" TO status;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE usuarios RENAME COLUMN "Nome" TO nome;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE usuarios RENAME COLUMN "Email" TO email;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE usuarios RENAME COLUMN "Biografia" TO biografia;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE usuarios RENAME COLUMN "Ativo" TO ativo;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE usuarios RENAME COLUMN "Id" TO id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE usuarios RENAME COLUMN "UpdatedAtUtc" TO updated_at_utc;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE usuarios RENAME COLUMN "QuantidadeAvaliacoes" TO quantidade_avaliacoes;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE usuarios RENAME COLUMN "PrecoHora" TO preco_hora;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE usuarios RENAME COLUMN "PasswordHash" TO password_hash;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE usuarios RENAME COLUMN "NivelReputacao" TO nivel_reputacao;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE usuarios RENAME COLUMN "MediaAvaliacoes" TO media_avaliacoes;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE usuarios RENAME COLUMN "IdentidadeVerificada" TO identidade_verificada;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE usuarios RENAME COLUMN "DataNascimento" TO data_nascimento;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE usuarios RENAME COLUMN "CreatedAtUtc" TO created_at_utc;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE usuarios RENAME COLUMN "AulasConcluidas" TO aulas_concluidas;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER INDEX "IX_usuarios_Status" RENAME TO ix_usuarios_status;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER INDEX "IX_usuarios_Email" RENAME TO ix_usuarios_email;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE responsaveis_alunos RENAME COLUMN "Parentesco" TO parentesco;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE responsaveis_alunos RENAME COLUMN "Id" TO id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE responsaveis_alunos RENAME COLUMN "UpdatedAtUtc" TO updated_at_utc;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE responsaveis_alunos RENAME COLUMN "ResponsavelId" TO responsavel_id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE responsaveis_alunos RENAME COLUMN "CreatedAtUtc" TO created_at_utc;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE responsaveis_alunos RENAME COLUMN "AlunoId" TO aluno_id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER INDEX "IX_responsaveis_alunos_ResponsavelId_AlunoId" RENAME TO ix_responsaveis_alunos_responsavel_id_aluno_id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER INDEX "IX_responsaveis_alunos_AlunoId" RENAME TO ix_responsaveis_alunos_aluno_id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE qualificacoes_professor RENAME COLUMN "Titulo" TO titulo;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE qualificacoes_professor RENAME COLUMN "Tipo" TO tipo;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE qualificacoes_professor RENAME COLUMN "Instituicao" TO instituicao;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE qualificacoes_professor RENAME COLUMN "Descricao" TO descricao;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE qualificacoes_professor RENAME COLUMN "Id" TO id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE qualificacoes_professor RENAME COLUMN "UpdatedAtUtc" TO updated_at_utc;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE qualificacoes_professor RENAME COLUMN "StatusVerificacao" TO status_verificacao;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE qualificacoes_professor RENAME COLUMN "ProfessorId" TO professor_id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE qualificacoes_professor RENAME COLUMN "DocumentoComprovacao" TO documento_comprovacao;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE qualificacoes_professor RENAME COLUMN "DataVerificacaoUtc" TO data_verificacao_utc;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE qualificacoes_professor RENAME COLUMN "DataConclusao" TO data_conclusao;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE qualificacoes_professor RENAME COLUMN "CreatedAtUtc" TO created_at_utc;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER INDEX "IX_qualificacoes_professor_ProfessorId_StatusVerificacao" RENAME TO ix_qualificacoes_professor_professor_id_status_verificacao;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE professores_materias RENAME COLUMN "Id" TO id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE professores_materias RENAME COLUMN "UpdatedAtUtc" TO updated_at_utc;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE professores_materias RENAME COLUMN "ProfessorId" TO professor_id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE professores_materias RENAME COLUMN "MateriaId" TO materia_id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE professores_materias RENAME COLUMN "CreatedAtUtc" TO created_at_utc;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER INDEX "IX_professores_materias_ProfessorId_MateriaId" RENAME TO ix_professores_materias_professor_id_materia_id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER INDEX "IX_professores_materias_MateriaId" RENAME TO ix_professores_materias_materia_id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE materias RENAME COLUMN "Nome" TO nome;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE materias RENAME COLUMN "Id" TO id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE materias RENAME COLUMN "UpdatedAtUtc" TO updated_at_utc;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE materias RENAME COLUMN "CreatedAtUtc" TO created_at_utc;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER INDEX "IX_materias_Nome" RENAME TO ix_materias_nome;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE disponibilidades_professor RENAME COLUMN "Inicio" TO inicio;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE disponibilidades_professor RENAME COLUMN "Fim" TO fim;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE disponibilidades_professor RENAME COLUMN "Id" TO id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE disponibilidades_professor RENAME COLUMN "UpdatedAtUtc" TO updated_at_utc;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE disponibilidades_professor RENAME COLUMN "ProfessorId" TO professor_id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE disponibilidades_professor RENAME COLUMN "DiaSemana" TO dia_semana;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE disponibilidades_professor RENAME COLUMN "CreatedAtUtc" TO created_at_utc;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER INDEX "IX_disponibilidades_professor_ProfessorId_DiaSemana_Inicio_Fim" RENAME TO ix_disponibilidades_professor_periodo;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE candidaturas_professor RENAME COLUMN "Status" TO status;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE candidaturas_professor RENAME COLUMN "Motivo" TO motivo;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE candidaturas_professor RENAME COLUMN "Mensagem" TO mensagem;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE candidaturas_professor RENAME COLUMN "Id" TO id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE candidaturas_professor RENAME COLUMN "UpdatedAtUtc" TO updated_at_utc;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE candidaturas_professor RENAME COLUMN "ProfessorId" TO professor_id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE candidaturas_professor RENAME COLUMN "DataAnaliseUtc" TO data_analise_utc;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE candidaturas_professor RENAME COLUMN "CreatedAtUtc" TO created_at_utc;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE candidaturas_professor RENAME COLUMN "AdministradorAnaliseId" TO administrador_analise_id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER INDEX "IX_candidaturas_professor_ProfessorId_Status" RENAME TO ix_candidaturas_professor_professor_id_status;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE usuarios ADD CONSTRAINT pk_usuarios PRIMARY KEY (id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE responsaveis_alunos ADD CONSTRAINT pk_responsaveis_alunos PRIMARY KEY (id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE qualificacoes_professor ADD CONSTRAINT pk_qualificacoes_professor PRIMARY KEY (id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE professores_materias ADD CONSTRAINT pk_professores_materias PRIMARY KEY (id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE materias ADD CONSTRAINT pk_materias PRIMARY KEY (id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE disponibilidades_professor ADD CONSTRAINT pk_disponibilidades_professor PRIMARY KEY (id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE candidaturas_professor ADD CONSTRAINT pk_candidaturas_professor PRIMARY KEY (id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE candidaturas_professor ADD CONSTRAINT fk_candidaturas_professor_usuarios_professor_id FOREIGN KEY (professor_id) REFERENCES usuarios (id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE disponibilidades_professor ADD CONSTRAINT fk_disponibilidades_professor_usuarios_professor_id FOREIGN KEY (professor_id) REFERENCES usuarios (id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE professores_materias ADD CONSTRAINT fk_professores_materias_materias_materia_id FOREIGN KEY (materia_id) REFERENCES materias (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE professores_materias ADD CONSTRAINT fk_professores_materias_usuarios_professor_id FOREIGN KEY (professor_id) REFERENCES usuarios (id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE qualificacoes_professor ADD CONSTRAINT fk_qualificacoes_professor_usuarios_professor_id FOREIGN KEY (professor_id) REFERENCES usuarios (id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE responsaveis_alunos ADD CONSTRAINT fk_responsaveis_alunos_usuarios_aluno_id FOREIGN KEY (aluno_id) REFERENCES usuarios (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    ALTER TABLE responsaveis_alunos ADD CONSTRAINT fk_responsaveis_alunos_usuarios_responsavel_id FOREIGN KEY (responsavel_id) REFERENCES usuarios (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824012216_PadronizaSnakeCasePostgreSql') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260824012216_PadronizaSnakeCasePostgreSql', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260829190254_AdicionaAulasEAgendamentos') THEN
    CREATE TABLE aulas (
        id uuid NOT NULL,
        aluno_id uuid NOT NULL,
        professor_id uuid NOT NULL,
        materia_id uuid NOT NULL,
        inicio_utc timestamp with time zone NOT NULL,
        duracao_minutos integer NOT NULL,
        preco_acordado numeric(10,2) NOT NULL,
        status character varying(40) NOT NULL,
        confirmada_em_utc timestamp with time zone,
        cancelada_em_utc timestamp with time zone,
        concluida_em_utc timestamp with time zone,
        cancelada_por_usuario_id uuid,
        motivo_cancelamento character varying(1000),
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_aulas PRIMARY KEY (id),
        CONSTRAINT fk_aulas_materias_materia_id FOREIGN KEY (materia_id) REFERENCES materias (id) ON DELETE RESTRICT,
        CONSTRAINT fk_aulas_usuarios_aluno_id FOREIGN KEY (aluno_id) REFERENCES usuarios (id) ON DELETE RESTRICT,
        CONSTRAINT fk_aulas_usuarios_professor_id FOREIGN KEY (professor_id) REFERENCES usuarios (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260829190254_AdicionaAulasEAgendamentos') THEN
    CREATE INDEX ix_aulas_aluno_id_inicio_utc_status ON aulas (aluno_id, inicio_utc, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260829190254_AdicionaAulasEAgendamentos') THEN
    CREATE INDEX ix_aulas_materia_id ON aulas (materia_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260829190254_AdicionaAulasEAgendamentos') THEN
    CREATE INDEX ix_aulas_professor_id_inicio_utc_status ON aulas (professor_id, inicio_utc, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260829190254_AdicionaAulasEAgendamentos') THEN
    CREATE INDEX ix_aulas_professor_id_materia_id ON aulas (professor_id, materia_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260829190254_AdicionaAulasEAgendamentos') THEN
    DO $$
    DECLARE
        legacy_user record;
    BEGIN
        FOR legacy_user IN
            SELECT id
            FROM usuarios
            WHERE email LIKE '%@tutorclass.local'
              AND nome IN ('Aluno Desenvolvimento', 'Professor Desenvolvimento')
              AND email NOT IN (
                  'admin.dev@tutorclass.local',
                  'ana.clara.dev@tutorclass.local',
                  'bruno.lima.dev@tutorclass.local',
                  'camila.rocha.dev@tutorclass.local',
                  'diego.costa.dev@tutorclass.local',
                  'lara.nunes.dev@tutorclass.local',
                  'mariana.martins.dev@tutorclass.local',
                  'ricardo.nunes.dev@tutorclass.local',
                  'helena.biologia.dev@tutorclass.local',
                  'rafael.fisica.dev@tutorclass.local',
                  'patricia.portugues.dev@tutorclass.local',
                  'sofia.ingles.dev@tutorclass.local',
                  'marcos.historia.dev@tutorclass.local'
              )
        LOOP
            DELETE FROM aulas
            WHERE aluno_id = legacy_user.id
               OR professor_id = legacy_user.id;

            DELETE FROM responsaveis_alunos
            WHERE aluno_id = legacy_user.id
               OR responsavel_id = legacy_user.id;

            DELETE FROM candidaturas_professor
            WHERE professor_id = legacy_user.id;

            DELETE FROM disponibilidades_professor
            WHERE professor_id = legacy_user.id;

            DELETE FROM professores_materias
            WHERE professor_id = legacy_user.id;

            DELETE FROM qualificacoes_professor
            WHERE professor_id = legacy_user.id;

            DELETE FROM usuarios
            WHERE id = legacy_user.id
              AND email LIKE '%@tutorclass.local'
              AND nome IN ('Aluno Desenvolvimento', 'Professor Desenvolvimento');
        END LOOP;
    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260829190254_AdicionaAulasEAgendamentos') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260829190254_AdicionaAulasEAgendamentos', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260829210000_AdicionaAcompanhamentoAvaliacoesReputacao') THEN
    CREATE TABLE acompanhamentos_aula (
        id uuid NOT NULL,
        aula_id uuid NOT NULL,
        aluno_id uuid NOT NULL,
        professor_id uuid NOT NULL,
        resumo character varying(2000) NOT NULL,
        pontos_fortes character varying(1000),
        pontos_melhorar character varying(1000),
        proximos_passos character varying(1000),
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_acompanhamentos_aula PRIMARY KEY (id),
        CONSTRAINT fk_acompanhamentos_aula_aulas_aula_id FOREIGN KEY (aula_id) REFERENCES aulas (id) ON DELETE CASCADE,
        CONSTRAINT fk_acompanhamentos_aula_usuarios_aluno_id FOREIGN KEY (aluno_id) REFERENCES usuarios (id) ON DELETE RESTRICT,
        CONSTRAINT fk_acompanhamentos_aula_usuarios_professor_id FOREIGN KEY (professor_id) REFERENCES usuarios (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260829210000_AdicionaAcompanhamentoAvaliacoesReputacao') THEN
    CREATE TABLE avaliacoes_professor (
        id uuid NOT NULL,
        aula_id uuid NOT NULL,
        aluno_id uuid NOT NULL,
        professor_id uuid NOT NULL,
        nota integer NOT NULL,
        comentario character varying(1500),
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_avaliacoes_professor PRIMARY KEY (id),
        CONSTRAINT ck_avaliacoes_professor_nota CHECK (nota >= 1 AND nota <= 5),
        CONSTRAINT fk_avaliacoes_professor_aulas_aula_id FOREIGN KEY (aula_id) REFERENCES aulas (id) ON DELETE CASCADE,
        CONSTRAINT fk_avaliacoes_professor_usuarios_aluno_id FOREIGN KEY (aluno_id) REFERENCES usuarios (id) ON DELETE RESTRICT,
        CONSTRAINT fk_avaliacoes_professor_usuarios_professor_id FOREIGN KEY (professor_id) REFERENCES usuarios (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260829210000_AdicionaAcompanhamentoAvaliacoesReputacao') THEN
    CREATE UNIQUE INDEX ix_acompanhamentos_aula_aula_id ON acompanhamentos_aula (aula_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260829210000_AdicionaAcompanhamentoAvaliacoesReputacao') THEN
    CREATE INDEX ix_acompanhamentos_aula_aluno_id_created_at_utc ON acompanhamentos_aula (aluno_id, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260829210000_AdicionaAcompanhamentoAvaliacoesReputacao') THEN
    CREATE INDEX ix_acompanhamentos_aula_professor_id_created_at_utc ON acompanhamentos_aula (professor_id, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260829210000_AdicionaAcompanhamentoAvaliacoesReputacao') THEN
    CREATE UNIQUE INDEX ix_avaliacoes_professor_aula_id ON avaliacoes_professor (aula_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260829210000_AdicionaAcompanhamentoAvaliacoesReputacao') THEN
    CREATE INDEX ix_avaliacoes_professor_aluno_id ON avaliacoes_professor (aluno_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260829210000_AdicionaAcompanhamentoAvaliacoesReputacao') THEN
    CREATE INDEX ix_avaliacoes_professor_professor_id_created_at_utc ON avaliacoes_professor (professor_id, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260829210000_AdicionaAcompanhamentoAvaliacoesReputacao') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260829210000_AdicionaAcompanhamentoAvaliacoesReputacao', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260901181337_AdicionaMateriaisPedagogicos') THEN
    CREATE TABLE materiais (
        id uuid NOT NULL,
        professor_id uuid NOT NULL,
        aluno_id uuid NOT NULL,
        materia_id uuid NOT NULL,
        aula_id uuid,
        titulo character varying(160) NOT NULL,
        descricao character varying(1000),
        tipo character varying(60) NOT NULL,
        nome_arquivo character varying(255),
        url_arquivo character varying(2000) NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_materiais PRIMARY KEY (id),
        CONSTRAINT fk_materiais_aulas_aula_id FOREIGN KEY (aula_id) REFERENCES aulas (id) ON DELETE SET NULL,
        CONSTRAINT fk_materiais_materias_materia_id FOREIGN KEY (materia_id) REFERENCES materias (id) ON DELETE RESTRICT,
        CONSTRAINT fk_materiais_usuarios_aluno_id FOREIGN KEY (aluno_id) REFERENCES usuarios (id) ON DELETE RESTRICT,
        CONSTRAINT fk_materiais_usuarios_professor_id FOREIGN KEY (professor_id) REFERENCES usuarios (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260901181337_AdicionaMateriaisPedagogicos') THEN
    CREATE INDEX ix_materiais_aluno_id_created_at_utc ON materiais (aluno_id, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260901181337_AdicionaMateriaisPedagogicos') THEN
    CREATE INDEX ix_materiais_aula_id ON materiais (aula_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260901181337_AdicionaMateriaisPedagogicos') THEN
    CREATE INDEX ix_materiais_materia_id ON materiais (materia_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260901181337_AdicionaMateriaisPedagogicos') THEN
    CREATE INDEX ix_materiais_professor_id_created_at_utc ON materiais (professor_id, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260901181337_AdicionaMateriaisPedagogicos') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260901181337_AdicionaMateriaisPedagogicos', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902164324_AdicionaPlanosMentoria') THEN
    ALTER TABLE usuarios ADD limite_mentorados integer DEFAULT 2;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902164324_AdicionaPlanosMentoria') THEN
    ALTER TABLE usuarios ADD oferece_mentoria boolean;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902164324_AdicionaPlanosMentoria') THEN
    CREATE TABLE assinaturas_aluno (
        id uuid NOT NULL,
        aluno_id uuid NOT NULL,
        plano character varying(40) NOT NULL,
        data_inicio_utc timestamp with time zone NOT NULL,
        data_fim_utc timestamp with time zone,
        ativa boolean NOT NULL,
        valor_mensal numeric(10,2) NOT NULL,
        valor_cobrado_primeiro_mes numeric(10,2) NOT NULL,
        usou_promocao_primeira_assinatura boolean NOT NULL,
        data_cancelamento_utc timestamp with time zone,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_assinaturas_aluno PRIMARY KEY (id),
        CONSTRAINT fk_assinaturas_aluno_usuarios_aluno_id FOREIGN KEY (aluno_id) REFERENCES usuarios (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902164324_AdicionaPlanosMentoria') THEN
    CREATE TABLE mentorias_aluno_professor (
        id uuid NOT NULL,
        aluno_id uuid NOT NULL,
        professor_id uuid NOT NULL,
        slot integer NOT NULL,
        data_inicio_utc timestamp with time zone NOT NULL,
        data_fim_utc timestamp with time zone,
        ativo boolean NOT NULL,
        data_ultima_troca_utc timestamp with time zone NOT NULL,
        motivo_encerramento character varying(60),
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_mentorias_aluno_professor PRIMARY KEY (id),
        CONSTRAINT fk_mentorias_aluno_professor_usuarios_aluno_id FOREIGN KEY (aluno_id) REFERENCES usuarios (id) ON DELETE RESTRICT,
        CONSTRAINT fk_mentorias_aluno_professor_usuarios_professor_id FOREIGN KEY (professor_id) REFERENCES usuarios (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902164324_AdicionaPlanosMentoria') THEN
    CREATE TABLE usos_ia_aluno (
        id uuid NOT NULL,
        aluno_id uuid NOT NULL,
        data date NOT NULL,
        quantidade integer NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_usos_ia_aluno PRIMARY KEY (id),
        CONSTRAINT fk_usos_ia_aluno_usuarios_aluno_id FOREIGN KEY (aluno_id) REFERENCES usuarios (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902164324_AdicionaPlanosMentoria') THEN
    CREATE TABLE acompanhamentos_mentoria (
        id uuid NOT NULL,
        aula_id uuid NOT NULL,
        aluno_id uuid NOT NULL,
        professor_id uuid NOT NULL,
        vinculo_mentoria_id uuid NOT NULL,
        resumo character varying(2000),
        pontos_fortes character varying(1000),
        pontos_melhorar character varying(1000),
        proximos_passos character varying(1000),
        meta_atual character varying(1000),
        observacoes character varying(1500),
        status character varying(40) NOT NULL,
        data_conclusao_utc timestamp with time zone,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_acompanhamentos_mentoria PRIMARY KEY (id),
        CONSTRAINT fk_acompanhamentos_mentoria_aulas_aula_id FOREIGN KEY (aula_id) REFERENCES aulas (id) ON DELETE CASCADE,
        CONSTRAINT "fk_acompanhamentos_mentoria_mentorias_aluno_professor_vinculo_~" FOREIGN KEY (vinculo_mentoria_id) REFERENCES mentorias_aluno_professor (id) ON DELETE RESTRICT,
        CONSTRAINT fk_acompanhamentos_mentoria_usuarios_aluno_id FOREIGN KEY (aluno_id) REFERENCES usuarios (id) ON DELETE RESTRICT,
        CONSTRAINT fk_acompanhamentos_mentoria_usuarios_professor_id FOREIGN KEY (professor_id) REFERENCES usuarios (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902164324_AdicionaPlanosMentoria') THEN
    CREATE TABLE adicionais_mentoria (
        id uuid NOT NULL,
        professor_id uuid NOT NULL,
        aluno_id uuid NOT NULL,
        aula_id uuid NOT NULL,
        acompanhamento_mentoria_id uuid NOT NULL,
        valor numeric(10,2) NOT NULL,
        data_geracao_utc timestamp with time zone NOT NULL,
        status character varying(40) NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_adicionais_mentoria PRIMARY KEY (id),
        CONSTRAINT "fk_adicionais_mentoria_acompanhamentos_mentoria_acompanhamento~" FOREIGN KEY (acompanhamento_mentoria_id) REFERENCES acompanhamentos_mentoria (id) ON DELETE CASCADE,
        CONSTRAINT fk_adicionais_mentoria_aulas_aula_id FOREIGN KEY (aula_id) REFERENCES aulas (id) ON DELETE RESTRICT,
        CONSTRAINT fk_adicionais_mentoria_usuarios_aluno_id FOREIGN KEY (aluno_id) REFERENCES usuarios (id) ON DELETE RESTRICT,
        CONSTRAINT fk_adicionais_mentoria_usuarios_professor_id FOREIGN KEY (professor_id) REFERENCES usuarios (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902164324_AdicionaPlanosMentoria') THEN
    CREATE INDEX ix_usuarios_oferece_mentoria ON usuarios (oferece_mentoria);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902164324_AdicionaPlanosMentoria') THEN
    CREATE INDEX ix_acompanhamentos_mentoria_aluno_id_created_at_utc ON acompanhamentos_mentoria (aluno_id, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902164324_AdicionaPlanosMentoria') THEN
    CREATE UNIQUE INDEX ix_acompanhamentos_mentoria_aula_id ON acompanhamentos_mentoria (aula_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902164324_AdicionaPlanosMentoria') THEN
    CREATE INDEX ix_acompanhamentos_mentoria_professor_id_created_at_utc ON acompanhamentos_mentoria (professor_id, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902164324_AdicionaPlanosMentoria') THEN
    CREATE INDEX ix_acompanhamentos_mentoria_professor_id_status ON acompanhamentos_mentoria (professor_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902164324_AdicionaPlanosMentoria') THEN
    CREATE INDEX ix_acompanhamentos_mentoria_vinculo_mentoria_id ON acompanhamentos_mentoria (vinculo_mentoria_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902164324_AdicionaPlanosMentoria') THEN
    CREATE UNIQUE INDEX ix_adicionais_mentoria_acompanhamento_mentoria_id ON adicionais_mentoria (acompanhamento_mentoria_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902164324_AdicionaPlanosMentoria') THEN
    CREATE INDEX ix_adicionais_mentoria_aluno_id ON adicionais_mentoria (aluno_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902164324_AdicionaPlanosMentoria') THEN
    CREATE INDEX ix_adicionais_mentoria_aula_id ON adicionais_mentoria (aula_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902164324_AdicionaPlanosMentoria') THEN
    CREATE INDEX ix_adicionais_mentoria_professor_id_aluno_id_data_geracao_utc ON adicionais_mentoria (professor_id, aluno_id, data_geracao_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902164324_AdicionaPlanosMentoria') THEN
    CREATE INDEX ix_assinaturas_aluno_aluno_id_ativa ON assinaturas_aluno (aluno_id, ativa);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902164324_AdicionaPlanosMentoria') THEN
    CREATE INDEX ix_assinaturas_aluno_aluno_id_usou_promocao_primeira_assinatura ON assinaturas_aluno (aluno_id, usou_promocao_primeira_assinatura);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902164324_AdicionaPlanosMentoria') THEN
    CREATE INDEX ix_mentorias_aluno_professor_aluno_id_ativo ON mentorias_aluno_professor (aluno_id, ativo);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902164324_AdicionaPlanosMentoria') THEN
    CREATE INDEX ix_mentorias_aluno_professor_aluno_id_professor_id_ativo ON mentorias_aluno_professor (aluno_id, professor_id, ativo);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902164324_AdicionaPlanosMentoria') THEN
    CREATE INDEX ix_mentorias_aluno_professor_aluno_id_slot_ativo ON mentorias_aluno_professor (aluno_id, slot, ativo);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902164324_AdicionaPlanosMentoria') THEN
    CREATE INDEX ix_mentorias_aluno_professor_professor_id_ativo ON mentorias_aluno_professor (professor_id, ativo);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902164324_AdicionaPlanosMentoria') THEN
    CREATE UNIQUE INDEX ix_usos_ia_aluno_aluno_id_data ON usos_ia_aluno (aluno_id, data);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902164324_AdicionaPlanosMentoria') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260902164324_AdicionaPlanosMentoria', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902181450_AdicionaFinanceiroSimulado') THEN
    ALTER TABLE assinaturas_aluno ADD pagador_usuario_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902181450_AdicionaFinanceiroSimulado') THEN
    UPDATE assinaturas_aluno SET pagador_usuario_id = aluno_id WHERE pagador_usuario_id IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902181450_AdicionaFinanceiroSimulado') THEN
    ALTER TABLE assinaturas_aluno ALTER COLUMN pagador_usuario_id SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902181450_AdicionaFinanceiroSimulado') THEN
    CREATE TABLE pagamentos_aula (
        id uuid NOT NULL,
        aula_id uuid NOT NULL,
        aluno_id uuid NOT NULL,
        pagador_usuario_id uuid NOT NULL,
        professor_id uuid NOT NULL,
        valor_bruto numeric(10,2) NOT NULL,
        percentual_comissao numeric(5,2) NOT NULL,
        valor_comissao numeric(10,2) NOT NULL,
        valor_liquido_professor numeric(10,2) NOT NULL,
        status character varying(40) NOT NULL,
        data_pagamento_utc timestamp with time zone,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_pagamentos_aula PRIMARY KEY (id),
        CONSTRAINT fk_pagamentos_aula_aulas_aula_id FOREIGN KEY (aula_id) REFERENCES aulas (id) ON DELETE CASCADE,
        CONSTRAINT fk_pagamentos_aula_usuarios_aluno_id FOREIGN KEY (aluno_id) REFERENCES usuarios (id) ON DELETE RESTRICT,
        CONSTRAINT fk_pagamentos_aula_usuarios_pagador_usuario_id FOREIGN KEY (pagador_usuario_id) REFERENCES usuarios (id) ON DELETE RESTRICT,
        CONSTRAINT fk_pagamentos_aula_usuarios_professor_id FOREIGN KEY (professor_id) REFERENCES usuarios (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902181450_AdicionaFinanceiroSimulado') THEN
    CREATE INDEX ix_assinaturas_aluno_pagador_usuario_id ON assinaturas_aluno (pagador_usuario_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902181450_AdicionaFinanceiroSimulado') THEN
    CREATE INDEX ix_pagamentos_aula_aluno_id_created_at_utc ON pagamentos_aula (aluno_id, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902181450_AdicionaFinanceiroSimulado') THEN
    CREATE UNIQUE INDEX ix_pagamentos_aula_aula_id ON pagamentos_aula (aula_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902181450_AdicionaFinanceiroSimulado') THEN
    CREATE INDEX ix_pagamentos_aula_pagador_usuario_id_created_at_utc ON pagamentos_aula (pagador_usuario_id, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902181450_AdicionaFinanceiroSimulado') THEN
    CREATE INDEX ix_pagamentos_aula_professor_id_created_at_utc ON pagamentos_aula (professor_id, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902181450_AdicionaFinanceiroSimulado') THEN
    CREATE INDEX ix_pagamentos_aula_status ON pagamentos_aula (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902181450_AdicionaFinanceiroSimulado') THEN
    ALTER TABLE assinaturas_aluno ADD CONSTRAINT fk_assinaturas_aluno_usuarios_pagador_usuario_id FOREIGN KEY (pagador_usuario_id) REFERENCES usuarios (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902181450_AdicionaFinanceiroSimulado') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260902181450_AdicionaFinanceiroSimulado', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902195718_FechaFinanceiroEAdicionaReagendamento') THEN
    ALTER TABLE pagamentos_aula ADD forma_pagamento character varying(40) NOT NULL DEFAULT 'Pix';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902195718_FechaFinanceiroEAdicionaReagendamento') THEN
    CREATE TABLE aulas_reagendamentos (
        id uuid NOT NULL,
        aula_id uuid NOT NULL,
        inicio_anterior_utc timestamp with time zone NOT NULL,
        fim_anterior_utc timestamp with time zone NOT NULL,
        inicio_novo_utc timestamp with time zone NOT NULL,
        fim_novo_utc timestamp with time zone NOT NULL,
        solicitado_por_usuario_id uuid NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_aulas_reagendamentos PRIMARY KEY (id),
        CONSTRAINT fk_aulas_reagendamentos_aulas_aula_id FOREIGN KEY (aula_id) REFERENCES aulas (id) ON DELETE CASCADE,
        CONSTRAINT fk_aulas_reagendamentos_usuarios_solicitado_por_usuario_id FOREIGN KEY (solicitado_por_usuario_id) REFERENCES usuarios (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902195718_FechaFinanceiroEAdicionaReagendamento') THEN
    CREATE TABLE cobrancas_assinatura (
        id uuid NOT NULL,
        assinatura_id uuid NOT NULL,
        pagador_usuario_id uuid NOT NULL,
        ano_competencia integer NOT NULL,
        mes_competencia integer NOT NULL,
        valor numeric(10,2) NOT NULL,
        forma_pagamento character varying(40) NOT NULL,
        status character varying(40) NOT NULL,
        paid_at_utc timestamp with time zone,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_cobrancas_assinatura PRIMARY KEY (id),
        CONSTRAINT fk_cobrancas_assinatura_assinaturas_aluno_assinatura_id FOREIGN KEY (assinatura_id) REFERENCES assinaturas_aluno (id) ON DELETE CASCADE,
        CONSTRAINT fk_cobrancas_assinatura_usuarios_pagador_usuario_id FOREIGN KEY (pagador_usuario_id) REFERENCES usuarios (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902195718_FechaFinanceiroEAdicionaReagendamento') THEN
    CREATE INDEX ix_aulas_reagendamentos_aula_id_created_at_utc ON aulas_reagendamentos (aula_id, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902195718_FechaFinanceiroEAdicionaReagendamento') THEN
    CREATE INDEX ix_aulas_reagendamentos_solicitado_por_usuario_id ON aulas_reagendamentos (solicitado_por_usuario_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902195718_FechaFinanceiroEAdicionaReagendamento') THEN
    CREATE UNIQUE INDEX "ix_cobrancas_assinatura_assinatura_id_ano_competencia_mes_comp~" ON cobrancas_assinatura (assinatura_id, ano_competencia, mes_competencia);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902195718_FechaFinanceiroEAdicionaReagendamento') THEN
    CREATE INDEX ix_cobrancas_assinatura_pagador_usuario_id_created_at_utc ON cobrancas_assinatura (pagador_usuario_id, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902195718_FechaFinanceiroEAdicionaReagendamento') THEN
    CREATE INDEX ix_cobrancas_assinatura_status ON cobrancas_assinatura (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902195718_FechaFinanceiroEAdicionaReagendamento') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260902195718_FechaFinanceiroEAdicionaReagendamento', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260904185135_AdicionaGravacoesAulasMentoria') THEN
    CREATE TABLE gravacoes_aula (
        id uuid NOT NULL,
        aula_id uuid NOT NULL,
        titulo character varying(200) NOT NULL,
        descricao character varying(1000),
        url_video character varying(1000) NOT NULL,
        miniatura_url character varying(1000),
        duracao_segundos integer NOT NULL,
        data_disponibilizacao_utc timestamp with time zone,
        disponivel boolean NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_gravacoes_aula PRIMARY KEY (id),
        CONSTRAINT fk_gravacoes_aula_aulas_aula_id FOREIGN KEY (aula_id) REFERENCES aulas (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260904185135_AdicionaGravacoesAulasMentoria') THEN
    CREATE UNIQUE INDEX ix_gravacoes_aula_aula_id ON gravacoes_aula (aula_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260904185135_AdicionaGravacoesAulasMentoria') THEN
    CREATE INDEX ix_gravacoes_aula_disponivel_data_disponibilizacao_utc ON gravacoes_aula (disponivel, data_disponibilizacao_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260904185135_AdicionaGravacoesAulasMentoria') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260904185135_AdicionaGravacoesAulasMentoria', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260904213825_AdicionaFaqChatbot') THEN
    CREATE TABLE perguntas_frequentes (
        id uuid NOT NULL,
        pergunta character varying(240) NOT NULL,
        resposta character varying(2000) NOT NULL,
        categoria character varying(60) NOT NULL,
        palavras_chave character varying(500),
        ativo boolean NOT NULL,
        ordem_exibicao integer NOT NULL,
        texto_acao character varying(120),
        rota_acao character varying(240),
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_perguntas_frequentes PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260904213825_AdicionaFaqChatbot') THEN
    CREATE INDEX ix_perguntas_frequentes_ativo_categoria_ordem_exibicao ON perguntas_frequentes (ativo, categoria, ordem_exibicao);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260904213825_AdicionaFaqChatbot') THEN
    CREATE UNIQUE INDEX ix_perguntas_frequentes_pergunta ON perguntas_frequentes (pergunta);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260904213825_AdicionaFaqChatbot') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260904213825_AdicionaFaqChatbot', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905015053_AdicionaNotificacoesInternas') THEN
    CREATE TABLE notificacoes (
        id uuid NOT NULL,
        usuario_id uuid NOT NULL,
        tipo character varying(80) NOT NULL,
        titulo character varying(140) NOT NULL,
        mensagem character varying(500) NOT NULL,
        lida boolean NOT NULL,
        data_leitura_utc timestamp with time zone,
        rota_acao character varying(240),
        referencia_id uuid,
        chave_evento character varying(160),
        prioridade character varying(30) NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_notificacoes PRIMARY KEY (id),
        CONSTRAINT fk_notificacoes_usuarios_usuario_id FOREIGN KEY (usuario_id) REFERENCES usuarios (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905015053_AdicionaNotificacoesInternas') THEN
    CREATE UNIQUE INDEX ix_notificacoes_chave_evento ON notificacoes (chave_evento) WHERE chave_evento IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905015053_AdicionaNotificacoesInternas') THEN
    CREATE INDEX ix_notificacoes_usuario_id_lida_created_at_utc ON notificacoes (usuario_id, lida, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905015053_AdicionaNotificacoesInternas') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260905015053_AdicionaNotificacoesInternas', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905161235_FortalecePermissoesResponsavel') THEN
    ALTER TABLE aulas ADD solicitada_por_usuario_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905161235_FortalecePermissoesResponsavel') THEN
    UPDATE aulas
    SET solicitada_por_usuario_id = aluno_id
    WHERE solicitada_por_usuario_id IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905161235_FortalecePermissoesResponsavel') THEN
    ALTER TABLE aulas ALTER COLUMN solicitada_por_usuario_id SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905161235_FortalecePermissoesResponsavel') THEN
    CREATE INDEX ix_aulas_solicitada_por_usuario_id ON aulas (solicitada_por_usuario_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905161235_FortalecePermissoesResponsavel') THEN
    ALTER TABLE aulas ADD CONSTRAINT fk_aulas_usuarios_solicitada_por_usuario_id FOREIGN KEY (solicitada_por_usuario_id) REFERENCES usuarios (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905161235_FortalecePermissoesResponsavel') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260905161235_FortalecePermissoesResponsavel', '10.0.4');
    END IF;
END $EF$;
COMMIT;

