using Microsoft.AspNetCore.Identity;
using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Common;
using TutorClass.Domain.Financial;
using TutorClass.Domain.Help;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Mentoring;
using TutorClass.Domain.Notifications;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Subjects;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;

namespace TutorClass.Application.Services;

public sealed class DevelopmentDataService
{
    public const string DevelopmentPassword = "TutorClass@123";

    private static readonly DevUser Admin = new("Administrador Desenvolvimento", "admin.dev@tutorclass.local");

    private static readonly DevStudent[] Students =
    [
        new("Ana Clara Martins", "ana.clara.dev@tutorclass.local", new DateOnly(2010, 4, 12)),
        new("Bruno Henrique Lima", "bruno.lima.dev@tutorclass.local", new DateOnly(2009, 9, 3)),
        new("Camila Rocha Alves", "camila.rocha.dev@tutorclass.local", new DateOnly(2011, 1, 21)),
        new("Diego Fernandes Costa", "diego.costa.dev@tutorclass.local", new DateOnly(2008, 11, 8)),
        new("Lara Beatriz Nunes", "lara.nunes.dev@tutorclass.local", new DateOnly(2012, 6, 17))
    ];

    private static readonly DevGuardian[] Guardians =
    [
        new("Mariana Martins", "mariana.martins.dev@tutorclass.local", "11988887777"),
        new("Ricardo Nunes", "ricardo.nunes.dev@tutorclass.local", "11977776666")
    ];

    private static readonly DevTeacher[] Teachers =
    [
        new("Helena Duarte", "helena.biologia.dev@tutorclass.local", "Professora de Biologia com foco em ensino medio, vestibulares e aprendizagem visual.", 85, ["Biologia"], ["Licenciatura em Ciencias Biologicas"], TeacherDemoState.Approved),
        new("Rafael Moreira", "rafael.fisica.dev@tutorclass.local", "Professor de Fisica que trabalha resolucao de problemas e preparacao para ENEM.", 95, ["Fisica"], ["Mestrado em Ensino de Fisica"], TeacherDemoState.Approved),
        new("Patricia Azevedo", "patricia.portugues.dev@tutorclass.local", "Professora de Portugues e Literatura com experiencia em redacao e interpretacao de texto.", 78, ["Portugues", "Literatura"], ["Especializacao em Literatura Brasileira"], TeacherDemoState.Approved),
        new("Sofia Almeida", "sofia.ingles.dev@tutorclass.local", "Professora de Ingles para reforco escolar, conversacao inicial e certificacoes basicas.", 72, ["Ingles"], ["Certificacao internacional de Ingles"], TeacherDemoState.InReviewWithPendingQualification),
        new("Marcos Vieira", "marcos.historia.dev@tutorclass.local", "Professor de Historia com abordagem contextual e uso de mapas mentais.", 68, ["Historia", "Sociologia"], ["Curso de Historia Contemporanea"], TeacherDemoState.PendingApplication)
    ];

    private static readonly DevFaq[] Faqs =
    [
        new(
            "Como cancelar uma aula?",
            "Voce pode cancelar uma aula pela area de suas aulas, respeitando as regras de cancelamento existentes.",
            CategoriaFaq.Aulas,
            "cancelar,cancelamento,cancelo,desistir,aula,minhas aulas",
            10,
            "Ver minhas aulas",
            "/minhas-aulas"),
        new(
            "Como funciona o agendamento?",
            "O aluno escolhe um professor aprovado, seleciona uma materia que ele leciona e solicita um horario dentro da disponibilidade cadastrada.",
            CategoriaFaq.Aulas,
            "agendar,agendamento,horario,disponibilidade,aula,professor",
            20,
            "Buscar professores",
            "/professores"),
        new(
            "As aulas sao publicas?",
            "Nao. As aulas do TutorClass sao particulares, contratadas e agendadas pelos alunos. Nao existem aulas publicas, promocionais ou abertas para visitantes.",
            CategoriaFaq.Aulas,
            "publica,publicas,live,visitante,promocional,particular,contratada",
            30),
        new(
            "Qual a diferenca entre Gratuito, Foco e Mentoria?",
            "Todos podem contratar aulas. O Gratuito nao possui TutorIA nem gravacoes. O Foco possui TutorIA com limite de 7 mensagens por dia. A Mentoria possui TutorIA sem limite diario imposto pelo TutorClass, ate 2 mentores ativos e acesso as gravacoes das proprias aulas elegiveis.",
            CategoriaFaq.Planos,
            "gratuito,foco,mentoria,planos,diferenca,assinatura",
            10,
            "Conhecer planos",
            "/planos"),
        new(
            "Como funciona o desconto do primeiro mes?",
            "A promocao do primeiro mes pode ser aplicada uma unica vez por aluno elegivel. Depois de cancelar e assinar novamente, passa a valer o preco mensal normal do plano.",
            CategoriaFaq.Planos,
            "desconto,promocao,primeiro mes,assinatura,preco",
            20,
            "Conhecer planos",
            "/planos"),
        new(
            "Quantos mentores posso ter?",
            "O plano Mentoria permite ate 2 professores mentores ativos ao mesmo tempo.",
            CategoriaFaq.Mentoria,
            "mentor,mentores,mentoria,professor,professores,quantos,limite",
            10,
            "Ver mentoria",
            "/mentoria"),
        new(
            "Posso trocar de mentor?",
            "Sim. A troca de mentor preserva o historico e segue as regras do plano Mentoria.",
            CategoriaFaq.Mentoria,
            "trocar,troca,mentor,mentoria,substituir",
            20,
            "Ver mentoria",
            "/mentoria"),
        new(
            "Nem todo professor oferece Mentoria?",
            "Isso mesmo. Somente professores aprovados que habilitaram Mentoria podem ser escolhidos como mentores.",
            CategoriaFaq.Mentoria,
            "oferece mentoria,professor habilitado,mentor,mentoria",
            30),
        new(
            "O que e a TutorIA?",
            "A TutorIA e um recurso separado do FAQ e serve para apoio educacional nos estudos. Ela nao responde como base administrativa da plataforma.",
            CategoriaFaq.TutorIA,
            "tutoria,ia,estudo,educacional,chat educacional",
            10),
        new(
            "Quantas mensagens posso enviar no plano Foco?",
            "No plano Foco, a TutorIA permite ate 7 mensagens por dia. No plano Mentoria, nao ha limite diario imposto pelo TutorClass.",
            CategoriaFaq.TutorIA,
            "foco,mensagens,limite,sete,7 mensagens,tutoria",
            20),
        new(
            "Quem pode acessar gravacoes?",
            "Alunos com plano Mentoria ativo podem rever as gravacoes disponiveis das proprias aulas concluidas. Gratuito e Foco participam da aula ao vivo contratada, mas nao acessam a gravacao depois.",
            CategoriaFaq.Gravacoes,
            "gravacao,gravacoes,video,rever,assistir,mentoria,proprias aulas",
            10,
            "Ver gravacoes",
            "/gravacoes"),
        new(
            "Quem pode pagar uma aula?",
            "A aula pode ser paga pelo proprio aluno ou por um responsavel vinculado a ele.",
            CategoriaFaq.Pagamentos,
            "pagar,pagamento,aula,responsavel,aluno,pix",
            10),
        new(
            "O plano inclui o valor das aulas?",
            "Nao. A assinatura do plano e a contratacao de aulas sao cobrancas separadas.",
            CategoriaFaq.Pagamentos,
            "plano inclui aula,assinatura,aula,cobranca,separada,pagamento",
            20),
        new(
            "Como me tornar professor?",
            "Crie uma conta como professor, informe seu perfil, vincule materias, cadastre qualificacoes e envie sua candidatura para analise administrativa.",
            CategoriaFaq.Professores,
            "professor,candidatura,qualificacao,materia,cadastro,aprovar",
            10,
            "Cadastrar como professor",
            "/cadastro/professor")
    ];

    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    private readonly IMentoriaChatStore? _chatStore;

    public DevelopmentDataService(IApplicationDbContext db, IPasswordHasher<Usuario> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _chatStore = null;
    }

    public DevelopmentDataService(IApplicationDbContext db, IPasswordHasher<Usuario> passwordHasher, IMentoriaChatStore chatStore)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _chatStore = chatStore;
    }

    public async Task<DevelopmentTestDataDto> EnsureTestDataAsync(CancellationToken cancellationToken)
    {
        var admin = EnsureAdministrator(Admin);
        var students = Students.Select(EnsureStudent).ToArray();
        var guardians = Guardians.Select(EnsureGuardian).ToArray();

        EnsureGuardianLink(guardians[0].Id, students[0].Id, "Mae");
        EnsureGuardianLink(guardians[0].Id, students[1].Id, "Mae");
        EnsureGuardianLink(guardians[1].Id, students[2].Id, "Pai");
        EnsureGuardianLink(guardians[1].Id, students[4].Id, "Pai");
        EnsureGuardianLink(guardians[0].Id, students[4].Id, "Responsavel adicional");

        var teachers = Teachers.Select(teacher => EnsureTeacher(teacher, admin.Id)).ToArray();

        await _db.SaveChangesAsync(cancellationToken);

        EnsureDevelopmentPlans(students, guardians);
        var financeiroDemonstracao = EnsureDevelopmentMentoring(students, guardians);

        await _db.SaveChangesAsync(cancellationToken);

        var gravacoesSimuladas = await EnsureDevelopmentRecordingsAsync(students, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        var perguntasFrequentes = EnsureDevelopmentFaqs();
        var notificacoes = EnsureDevelopmentNotifications(students, guardians);
        await EnsureDevelopmentMentoriaChatAsync(cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return new DevelopmentTestDataDto(
            ToUserDto(admin),
            students.Select(ToUserDto).OrderBy(a => a.Nome).ToArray(),
            guardians.Select(ToGuardianDto).OrderBy(r => r.Nome).ToArray(),
            teachers.Where(p => p.StatusProfessor == StatusProfessor.Aprovado.ToString()).OrderBy(p => p.Nome).ToArray(),
            teachers.Where(p => p.StatusProfessor != StatusProfessor.Aprovado.ToString()).OrderBy(p => p.Nome).ToArray(),
            BuildDevelopmentPlans(students),
            BuildDevelopmentMentoring(),
            financeiroDemonstracao,
            gravacoesSimuladas,
            perguntasFrequentes,
            notificacoes,
            DevelopmentPassword);
    }

    private Administrador EnsureAdministrator(DevUser user)
    {
        var admin = _db.Administradores.SingleOrDefault(a => a.Email == user.Email);
        if (admin is null)
        {
            admin = new Administrador(user.Name, user.Email, "development-only-password-hash");
            _db.AddEntity(admin);
        }

        admin.DefinirPasswordHash(_passwordHasher.HashPassword(admin, DevelopmentPassword));
        return admin;
    }

    private Aluno EnsureStudent(DevStudent user)
    {
        var existing = _db.Usuarios.SingleOrDefault(u => u.Email == user.Email);
        if (existing is Aluno aluno)
        {
            aluno.DefinirPasswordHash(_passwordHasher.HashPassword(aluno, DevelopmentPassword));
            return aluno;
        }

        if (existing is not null)
        {
            throw new DomainException($"Email de desenvolvimento usado por outro perfil: {user.Email}.");
        }

        var newAluno = new Aluno(user.Name, user.Email, "development-only-password-hash", user.BirthDate);
        newAluno.DefinirPasswordHash(_passwordHasher.HashPassword(newAluno, DevelopmentPassword));
        _db.AddEntity(newAluno);
        return newAluno;
    }

    private Responsavel EnsureGuardian(DevGuardian user)
    {
        var existing = _db.Usuarios.SingleOrDefault(u => u.Email == user.Email);
        if (existing is Responsavel responsavel)
        {
            responsavel.DefinirPasswordHash(_passwordHasher.HashPassword(responsavel, DevelopmentPassword));
            return responsavel;
        }

        if (existing is not null)
        {
            throw new DomainException($"Email de desenvolvimento usado por outro perfil: {user.Email}.");
        }

        var newResponsavel = new Responsavel(user.Name, user.Email, "development-only-password-hash", user.Phone);
        newResponsavel.DefinirPasswordHash(_passwordHasher.HashPassword(newResponsavel, DevelopmentPassword));
        _db.AddEntity(newResponsavel);
        return newResponsavel;
    }

    private DevelopmentProfessorDto EnsureTeacher(DevTeacher teacher, Guid adminId)
    {
        var existing = _db.Usuarios.SingleOrDefault(u => u.Email == teacher.Email);
        Professor professor;
        if (existing is Professor found)
        {
            professor = found;
        }
        else if (existing is null)
        {
            professor = new Professor(teacher.Name, teacher.Email, "development-only-password-hash");
            _db.AddEntity(professor);
        }
        else
        {
            throw new DomainException($"Email de desenvolvimento usado por outro perfil: {teacher.Email}.");
        }

        professor.DefinirPasswordHash(_passwordHasher.HashPassword(professor, DevelopmentPassword));
        professor.AtualizarPerfilProfissional(teacher.Bio, teacher.HourlyPrice);
        professor.ConfigurarMentoria(teacher.Name is "Helena Duarte" or "Rafael Moreira" or "Marcos Vieira", teacher.Name == "Rafael Moreira" ? 1 : 2);

        foreach (var subjectName in teacher.Subjects)
        {
            var subject = FindSubject(subjectName);
            if (!_db.ProfessoresMaterias.Any(pm => pm.ProfessorId == professor.Id && pm.MateriaId == subject.Id))
            {
                _db.AddEntity(new ProfessorMateria(professor.Id, subject.Id));
            }
        }

        EnsureAvailability(professor.Id, DayOfWeek.Monday, new TimeOnly(14, 0), new TimeOnly(18, 0));
        EnsureAvailability(professor.Id, DayOfWeek.Wednesday, new TimeOnly(9, 0), new TimeOnly(12, 0));

        foreach (var qualificationTitle in teacher.Qualifications)
        {
            var qualification = _db.QualificacoesProfessor.SingleOrDefault(q =>
                q.ProfessorId == professor.Id &&
                q.Titulo == qualificationTitle);

            if (qualification is null)
            {
                qualification = new QualificacaoProfessor(professor.Id, TipoQualificacaoProfessor.Certificacao, qualificationTitle, "Instituicao de Desenvolvimento", "Qualificacao ficticia usada apenas para demonstracao local.", new DateOnly(2024, 12, 10), "https://example.com/documento-dev.pdf");
                _db.AddEntity(qualification);
            }

            if (teacher.State != TeacherDemoState.InReviewWithPendingQualification &&
                qualification.StatusVerificacao == StatusVerificacaoQualificacao.Pendente)
            {
                qualification.Verificar();
            }
        }

        var candidatureStatus = EnsureApplication(professor, teacher, adminId);

        return new DevelopmentProfessorDto(professor.Id, professor.Nome, professor.Email, professor.Status.ToString(), candidatureStatus, teacher.Subjects, teacher.Qualifications, professor.PrecoHora);
    }

    private string EnsureApplication(Professor professor, DevTeacher teacher, Guid adminId)
    {
        var application = _db.CandidaturasProfessor
            .Where(c => c.ProfessorId == professor.Id)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefault();

        if (application is null)
        {
            application = new CandidaturaProfessor(professor.Id, "Candidatura de desenvolvimento para demonstracao.");
            _db.AddEntity(application);
        }

        if (teacher.State == TeacherDemoState.PendingApplication)
        {
            return application.Status.ToString();
        }

        if (application.Status == StatusCandidaturaProfessor.Pendente)
        {
            application.IniciarAnalise(adminId);
        }

        if (teacher.State == TeacherDemoState.InReviewWithPendingQualification)
        {
            return application.Status.ToString();
        }

        var hasSubject = teacher.Subjects.Length > 0;
        var hasVerifiedQualification = teacher.Qualifications.Length > 0;
        professor.ValidarElegibilidadeParaAprovacao(hasSubject, hasVerifiedQualification);

        if (application.Status == StatusCandidaturaProfessor.EmAnalise)
        {
            application.Aprovar(adminId);
        }

        professor.Aprovar();
        return application.Status.ToString();
    }

    private Materia FindSubject(string name)
    {
        return _db.Materias.SingleOrDefault(m => m.Nome == name)
            ?? throw new DomainException($"Materia de desenvolvimento nao encontrada: {name}.");
    }

    private void EnsureAvailability(Guid professorId, DayOfWeek day, TimeOnly start, TimeOnly end)
    {
        if (_db.DisponibilidadesProfessor.Any(d => d.ProfessorId == professorId && d.DiaSemana == day && d.Inicio == start && d.Fim == end))
        {
            return;
        }

        _db.AddEntity(new DisponibilidadeProfessor(professorId, day, start, end));
    }

    private void EnsureGuardianLink(Guid guardianId, Guid studentId, string relationship)
    {
        if (_db.ResponsaveisAlunos.Any(ra => ra.ResponsavelId == guardianId && ra.AlunoId == studentId))
        {
            return;
        }

        _db.AddEntity(new ResponsavelAluno(guardianId, studentId, relationship));
    }

    private void EnsureDevelopmentPlans(IReadOnlyCollection<Aluno> students, IReadOnlyCollection<Responsavel> guardians)
    {
        var ana = students.Single(a => a.Email == "ana.clara.dev@tutorclass.local");
        var bruno = students.Single(a => a.Email == "bruno.lima.dev@tutorclass.local");
        var camila = students.Single(a => a.Email == "camila.rocha.dev@tutorclass.local");
        var responsavelCamila = guardians.Single(r => r.Email == "ricardo.nunes.dev@tutorclass.local");

        EnsureNoActivePaidPlan(ana.Id);
        EnsurePaidPlan(bruno.Id, bruno.Id, PlanoAluno.Foco, 29.99m, 19.99m);
        EnsurePaidPlan(camila.Id, responsavelCamila.Id, PlanoAluno.Mentoria, 79.99m, 59.99m);
    }

    private DevelopmentFinanceiroDto EnsureDevelopmentMentoring(IReadOnlyCollection<Aluno> students, IReadOnlyCollection<Responsavel> guardians)
    {
        var camila = students.Single(a => a.Email == "camila.rocha.dev@tutorclass.local");
        var responsavelCamila = guardians.Single(r => r.Email == "ricardo.nunes.dev@tutorclass.local");
        var helena = _db.Professores.Single(p => p.Email == "helena.biologia.dev@tutorclass.local");
        var rafael = _db.Professores.Single(p => p.Email == "rafael.fisica.dev@tutorclass.local");
        var biologia = FindSubject("Biologia");
        var assinaturaCamila = _db.AssinaturasAluno
            .Where(a => a.AlunoId == camila.Id && a.Ativa)
            .OrderByDescending(a => a.CreatedAtUtc)
            .FirstOrDefault();
        if (assinaturaCamila is null)
        {
            assinaturaCamila = new AssinaturaAluno(camila.Id, responsavelCamila.Id, PlanoAluno.Mentoria, 79.99m, 59.99m, usouPromocaoPrimeiraAssinatura: true);
            _db.AddEntity(assinaturaCamila);
        }

        EnsureSubscriptionCharge(assinaturaCamila);

        var vinculoHelena = EnsureMentoringLink(camila.Id, helena.Id, slot: 1);
        EnsureMentoringLink(camila.Id, rafael.Id, slot: 2);

        var aula = _db.Aulas
            .Where(a => a.AlunoId == camila.Id && a.ProfessorId == helena.Id && a.MateriaId == biologia.Id && a.PrecoAcordado == 100m)
            .OrderBy(a => a.CreatedAtUtc)
            .FirstOrDefault();

        if (aula is null)
        {
            aula = new Aula(camila.Id, helena.Id, biologia.Id, DateTime.UtcNow.AddDays(2), 60, 100m);
            _db.AddEntity(aula);
            aula.Confirmar(helena.Id);
            aula.Concluir(helena.Id);
        }

        var pagamento = _db.PagamentosAula.SingleOrDefault(p => p.AulaId == aula.Id);
        if (pagamento is null)
        {
            pagamento = new PagamentoAula(aula.Id, camila.Id, responsavelCamila.Id, helena.Id, aula.PrecoAcordado, FormaPagamento.Pix);
            pagamento.MarcarComoPago();
            _db.AddEntity(pagamento);
        }

        AcompanhamentoMentoria acompanhamento;
        if (!_db.AcompanhamentosMentoria.Any(a => a.AulaId == aula.Id))
        {
            acompanhamento = new AcompanhamentoMentoria(aula.Id, camila.Id, helena.Id, vinculoHelena.Id);
            acompanhamento.Atualizar("Acompanhamento pendente de demonstracao para Mentoria.", "Participacao ativa", "Organizar revisao", "Revisar o conteudo da aula", "Completar lista da semana", "Criado pela massa Development.");
            _db.AddEntity(acompanhamento);
        }
        else
        {
            acompanhamento = _db.AcompanhamentosMentoria.Single(a => a.AulaId == aula.Id);
        }

        if (acompanhamento.Status == StatusAcompanhamentoMentoria.Pendente)
        {
            acompanhamento.Atualizar("Acompanhamento concluido de demonstracao para Mentoria.", "Participacao ativa", "Organizar revisao", "Revisar o conteudo da aula", "Completar lista da semana", "Criado pela massa Development.");
            acompanhamento.Concluir();
        }

        if (!_db.AdicionaisMentoria.Any(a => a.AcompanhamentoMentoriaId == acompanhamento.Id))
        {
            _db.AddEntity(new AdicionalMentoria(helena.Id, camila.Id, aula.Id, acompanhamento.Id, 20m));
        }

        return new DevelopmentFinanceiroDto(
            camila.Id,
            camila.Nome,
            responsavelCamila.Id,
            responsavelCamila.Nome,
            helena.Id,
            helena.Nome,
            aula.Id,
            pagamento.ValorBruto,
            pagamento.ValorComissao,
            pagamento.ValorLiquidoProfessor,
            20m);
    }

    private async Task<IReadOnlyCollection<DevelopmentGravacaoDto>> EnsureDevelopmentRecordingsAsync(IReadOnlyCollection<Aluno> students, CancellationToken cancellationToken)
    {
        const string demoVideoUrl = "https://www.w3schools.com/html/mov_bbb.mp4";

        var ana = students.Single(a => a.Email == "ana.clara.dev@tutorclass.local");
        var bruno = students.Single(a => a.Email == "bruno.lima.dev@tutorclass.local");
        var camila = students.Single(a => a.Email == "camila.rocha.dev@tutorclass.local");
        var helena = _db.Professores.Single(p => p.Email == "helena.biologia.dev@tutorclass.local");
        var rafael = _db.Professores.Single(p => p.Email == "rafael.fisica.dev@tutorclass.local");
        var biologia = FindSubject("Biologia");
        var fisica = FindSubject("Fisica");

        var principal = _db.Aulas
            .Where(a => a.AlunoId == camila.Id && a.ProfessorId == helena.Id && a.MateriaId == biologia.Id && a.Status == StatusAula.Concluida)
            .OrderBy(a => a.CreatedAtUtc)
            .FirstOrDefault()
            ?? EnsureCompletedDemoLesson(camila.Id, helena.Id, biologia.Id, DateTime.UtcNow.AddDays(-14), 100m);

        var gravacoes = new[]
        {
            EnsureRecording(
                principal,
                "Biologia - Sistema Respiratorio",
                demoVideoUrl,
                3120,
                disponivel: true,
                "Gravacao simulada de aula particular concluida. Em producao, a URL seria protegida por storage/CDN privado."),
            EnsureRecording(
                EnsureCompletedDemoLessonForRecording("Gravacao - Revisao de cinematica", camila.Id, rafael.Id, fisica.Id, DateTime.UtcNow.AddDays(-9), 95m),
                "Gravacao - Revisao de cinematica",
                demoVideoUrl,
                2460,
                disponivel: false,
                "Gravacao simulada de aula particular concluida que ainda nao esta disponivel."),
            EnsureRecording(
                EnsureCompletedDemoLessonForRecording("Gravacao - Ecologia basica", ana.Id, helena.Id, biologia.Id, DateTime.UtcNow.AddDays(-8), 85m),
                "Gravacao - Ecologia basica",
                demoVideoUrl,
                1800,
                disponivel: true,
                "Gravacao simulada de aula particular concluida para demonstrar bloqueio de aluno Gratuito."),
            EnsureRecording(
                EnsureCompletedDemoLessonForRecording("Gravacao - Leis de Newton", bruno.Id, rafael.Id, fisica.Id, DateTime.UtcNow.AddDays(-7), 95m),
                "Gravacao - Leis de Newton",
                demoVideoUrl,
                2700,
                disponivel: true,
                "Gravacao simulada de aula particular concluida para demonstrar bloqueio de aluno Foco.")
        };

        await _db.SaveChangesAsync(cancellationToken);

        return gravacoes
            .Select(g =>
            {
                var aula = _db.Aulas.Single(a => a.Id == g.AulaId);
                var aluno = _db.Usuarios.OfType<Aluno>().Single(a => a.Id == aula.AlunoId);
                var professor = _db.Professores.Single(p => p.Id == aula.ProfessorId);
                return new DevelopmentGravacaoDto(
                    aula.Id,
                    g.Id,
                    g.Titulo,
                    aluno.Nome,
                    professor.Nome,
                    GetDevelopmentPlanName(aluno.Id),
                    g.Disponivel,
                    g.UrlVideo);
            })
            .OrderBy(g => g.AlunoNome)
            .ThenBy(g => g.Titulo)
            .ToArray();
    }

    private Aula EnsureCompletedDemoLessonForRecording(string recordingTitle, Guid alunoId, Guid professorId, Guid materiaId, DateTime inicioUtc, decimal precoAcordado)
    {
        var existingRecording = _db.GravacoesAula.SingleOrDefault(g => g.Titulo == recordingTitle);
        if (existingRecording is not null)
        {
            return _db.Aulas.Single(a => a.Id == existingRecording.AulaId);
        }

        return EnsureCompletedDemoLesson(alunoId, professorId, materiaId, inicioUtc, precoAcordado);
    }

    private Aula EnsureCompletedDemoLesson(Guid alunoId, Guid professorId, Guid materiaId, DateTime inicioUtc, decimal precoAcordado)
    {
        var aula = new Aula(alunoId, professorId, materiaId, inicioUtc, 60, precoAcordado);
        _db.AddEntity(aula);
        aula.Confirmar(professorId);
        aula.Concluir(professorId);
        return aula;
    }

    private GravacaoAula EnsureRecording(Aula aula, string titulo, string urlVideo, int duracaoSegundos, bool disponivel, string? descricao)
    {
        var gravacao = _db.GravacoesAula.SingleOrDefault(g => g.AulaId == aula.Id);
        if (gravacao is null)
        {
            gravacao = new GravacaoAula(aula, titulo, urlVideo, duracaoSegundos, descricao);
            _db.AddEntity(gravacao);
        }
        else
        {
            gravacao.AtualizarReferencia(aula, titulo, urlVideo, duracaoSegundos, descricao);
        }

        if (!disponivel)
        {
            gravacao.Desativar();
        }

        return gravacao;
    }

    private string GetDevelopmentPlanName(Guid alunoId)
    {
        return _db.AssinaturasAluno
            .Where(a => a.AlunoId == alunoId && a.Ativa)
            .OrderByDescending(a => a.DataInicioUtc)
            .Select(a => a.Plano)
            .FirstOrDefault()
            .ToString();
    }

    private string GetUsuarioNome(Guid usuarioId)
    {
        return _db.Usuarios.Where(u => u.Id == usuarioId).Select(u => u.Nome).SingleOrDefault() ?? "Usuario";
    }

    private IReadOnlyCollection<DevelopmentFaqDto> EnsureDevelopmentFaqs()
    {
        var ensured = new List<PerguntaFrequente>();

        foreach (var faq in Faqs)
        {
            var existing = _db.PerguntasFrequentes.SingleOrDefault(f => f.Pergunta == faq.Pergunta);
            if (existing is null)
            {
                var created = new PerguntaFrequente(
                    faq.Pergunta,
                    faq.Resposta,
                    faq.Categoria,
                    faq.PalavrasChave,
                    faq.OrdemExibicao,
                    faq.TextoAcao,
                    faq.RotaAcao);
                _db.AddEntity(created);
                ensured.Add(created);
                continue;
            }

            existing.Atualizar(
                faq.Pergunta,
                faq.Resposta,
                faq.Categoria,
                faq.PalavrasChave,
                faq.OrdemExibicao,
                faq.TextoAcao,
                faq.RotaAcao,
                ativo: true);
            ensured.Add(existing);
        }

        return ensured
            .OrderBy(f => f.Categoria)
            .ThenBy(f => f.OrdemExibicao)
            .ThenBy(f => f.Pergunta)
            .Select(f => new DevelopmentFaqDto(f.Id, f.Pergunta, f.Categoria.ToString(), f.Ativo, f.OrdemExibicao))
            .ToArray();
    }

    private IReadOnlyCollection<DevelopmentNotificacaoDto> EnsureDevelopmentNotifications(IReadOnlyCollection<Aluno> students, IReadOnlyCollection<Responsavel> guardians)
    {
        var notificacoes = new NotificacaoService(_db);
        var ensured = new List<Notificacao>();
        var camila = students.Single(a => a.Email == "camila.rocha.dev@tutorclass.local");
        var helena = _db.Professores.Single(p => p.Email == "helena.biologia.dev@tutorclass.local");
        var responsavelCamila = guardians.Single(r => r.Email == "ricardo.nunes.dev@tutorclass.local");

        var aulasCamilaHelena = _db.Aulas
            .Where(a => a.AlunoId == camila.Id && a.ProfessorId == helena.Id)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToArray();

        var aulaCamila = aulasCamilaHelena.FirstOrDefault(a => _db.GravacoesAula.Any(g => g.AulaId == a.Id && g.Disponivel)) ??
            aulasCamilaHelena.FirstOrDefault(a => _db.PagamentosAula.Any(p => p.AulaId == a.Id && p.PagadorUsuarioId == responsavelCamila.Id)) ??
            aulasCamilaHelena.FirstOrDefault();

        if (aulaCamila is not null)
        {
            ensured.Add(notificacoes.CriarSeNaoExistir(
                camila.Id,
                TipoNotificacao.AulaConfirmada,
                "Helena confirmou sua aula",
                "Sua aula com Helena Duarte esta confirmada.",
                "/minhas-aulas",
                aulaCamila.Id,
                $"DEV:AULA_CONFIRMADA:{aulaCamila.Id}:{camila.Id}",
                PrioridadeNotificacao.Importante));

            var gravacao = _db.GravacoesAula.SingleOrDefault(g => g.AulaId == aulaCamila.Id && g.Disponivel);
            if (gravacao is not null)
            {
                ensured.Add(notificacoes.CriarSeNaoExistir(
                    camila.Id,
                    TipoNotificacao.GravacaoDisponivel,
                    "Sua gravacao esta disponivel",
                    "A gravacao da sua aula de Biologia ja esta disponivel para revisao.",
                    "/gravacoes",
                    gravacao.Id,
                    $"DEV:GRAVACAO_DISPONIVEL:{gravacao.Id}:{camila.Id}"));
            }

            var acompanhamento = _db.AcompanhamentosMentoria.SingleOrDefault(a => a.AulaId == aulaCamila.Id && a.Status == StatusAcompanhamentoMentoria.Concluido);
            if (acompanhamento is not null)
            {
                ensured.Add(notificacoes.CriarSeNaoExistir(
                    camila.Id,
                    TipoNotificacao.AcompanhamentoMentoriaConcluido,
                    "Novo acompanhamento da Mentoria",
                    "Seu mentor concluiu o acompanhamento da ultima aula.",
                    "/mentoria",
                    acompanhamento.Id,
                    $"DEV:ACOMPANHAMENTO_CONCLUIDO:{acompanhamento.Id}:{camila.Id}"));
            }

            var pagamento = _db.PagamentosAula
                .Where(p => p.AulaId == aulaCamila.Id && p.PagadorUsuarioId == responsavelCamila.Id)
                .OrderByDescending(p => p.CreatedAtUtc)
                .FirstOrDefault();

            if (pagamento is not null)
            {
                ensured.Add(notificacoes.CriarSeNaoExistir(
                    responsavelCamila.Id,
                    TipoNotificacao.PagamentoConfirmado,
                    "Pagamento confirmado",
                    "Pagamento da aula de Camila com Helena Duarte confirmado.",
                    "/financeiro",
                    pagamento.Id,
                    $"DEV:PAGAMENTO_CONFIRMADO:{pagamento.Id}:{responsavelCamila.Id}"));
            }
        }

        var mentoriaHelena = _db.MentoriasAlunoProfessor
            .Where(m => m.AlunoId == camila.Id && m.ProfessorId == helena.Id && m.Ativo)
            .OrderByDescending(m => m.CreatedAtUtc)
            .FirstOrDefault();

        if (mentoriaHelena is not null)
        {
            ensured.Add(notificacoes.CriarSeNaoExistir(
                helena.Id,
                TipoNotificacao.MentoriaIniciada,
                "Novo aluno de Mentoria",
                "Camila selecionou voce como mentora.",
                "/professor/mentoria",
                mentoriaHelena.Id,
                $"DEV:MENTORIA_INICIADA:{mentoriaHelena.Id}:{helena.Id}",
                PrioridadeNotificacao.Importante));
        }

        return ensured
            .OrderBy(n => GetUsuarioNome(n.UsuarioId))
            .ThenBy(n => n.CreatedAtUtc)
            .Select(n => new DevelopmentNotificacaoDto(n.Id, n.UsuarioId, GetUsuarioNome(n.UsuarioId), n.Tipo.ToString(), n.Titulo, n.Lida))
            .ToArray();
    }

    private async Task EnsureDevelopmentMentoriaChatAsync(CancellationToken cancellationToken)
    {
        if (_chatStore is null || !_chatStore.Configurado)
        {
            return;
        }

        var camila = _db.Usuarios.OfType<Aluno>().SingleOrDefault(a => a.Email == "camila.rocha.dev@tutorclass.local");
        var helena = _db.Professores.SingleOrDefault(p => p.Email == "helena.biologia.dev@tutorclass.local");
        if (camila is null || helena is null)
        {
            return;
        }

        var vinculo = _db.MentoriasAlunoProfessor
            .Where(m => m.AlunoId == camila.Id && m.ProfessorId == helena.Id && m.Ativo)
            .OrderByDescending(m => m.CreatedAtUtc)
            .FirstOrDefault();

        if (vinculo is null)
        {
            return;
        }

        var chat = new MentoriaChatService(_db, _chatStore, new ResponsavelAlunoService(_db));
        try
        {
            var historico = await chat.ListarMensagensAsync(vinculo.Id, camila.Id, 100, null, cancellationToken);
            if (historico.Mensagens.All(m => m.Conteudo != "Professora, posso revisar os exercicios antes da proxima aula?"))
            {
                await chat.EnviarMensagemAsync(
                    vinculo.Id,
                    camila.Id,
                    new EnviarMensagemMentoriaRequest("Professora, posso revisar os exercicios antes da proxima aula?"),
                    cancellationToken);
            }

            historico = await chat.ListarMensagensAsync(vinculo.Id, camila.Id, 100, null, cancellationToken);
            if (historico.Mensagens.All(m => m.Conteudo != "Claro, Camila. Vou deixar uma lista curta e comentamos no acompanhamento."))
            {
                await chat.EnviarMensagemAsync(
                    vinculo.Id,
                    helena.Id,
                    new EnviarMensagemMentoriaRequest("Claro, Camila. Vou deixar uma lista curta e comentamos no acompanhamento."),
                    cancellationToken);
            }
        }
        catch (MentoriaChatUnavailableException)
        {
            // O seed principal e PostgreSQL-first. Se o MongoDB do chat estiver fora, os demais dados de desenvolvimento continuam validos.
        }
    }

    private void EnsureNoActivePaidPlan(Guid alunoId)
    {
        foreach (var assinatura in _db.AssinaturasAluno.Where(a => a.AlunoId == alunoId && a.Ativa).ToArray())
        {
            assinatura.Cancelar();
        }
    }

    private void EnsurePaidPlan(Guid alunoId, Guid pagadorUsuarioId, PlanoAluno plano, decimal valorMensal, decimal valorPrimeiroMes)
    {
        var assinaturasAtivas = _db.AssinaturasAluno.Where(a => a.AlunoId == alunoId && a.Ativa).ToArray();
        if (assinaturasAtivas.Length == 1 && assinaturasAtivas[0].Plano == plano && assinaturasAtivas[0].PagadorUsuarioId == pagadorUsuarioId)
        {
            return;
        }

        foreach (var assinaturaAtiva in assinaturasAtivas)
        {
            assinaturaAtiva.Cancelar();
        }

        _db.AddEntity(new AssinaturaAluno(alunoId, pagadorUsuarioId, plano, valorMensal, valorPrimeiroMes, usouPromocaoPrimeiraAssinatura: true));
    }

    private void EnsureSubscriptionCharge(AssinaturaAluno assinatura)
    {
        var ano = assinatura.DataInicioUtc.Year;
        var mes = assinatura.DataInicioUtc.Month;

        if (_db.CobrancasAssinatura.Any(c => c.AssinaturaId == assinatura.Id && c.AnoCompetencia == ano && c.MesCompetencia == mes))
        {
            return;
        }

        var cobranca = new CobrancaAssinatura(assinatura.Id, assinatura.PagadorUsuarioId, ano, mes, assinatura.ValorCobradoPrimeiroMes, FormaPagamento.Pix);
        cobranca.MarcarComoPago();
        _db.AddEntity(cobranca);
    }

    private MentoriaAlunoProfessor EnsureMentoringLink(Guid alunoId, Guid professorId, int slot)
    {
        var active = _db.MentoriasAlunoProfessor.SingleOrDefault(m => m.AlunoId == alunoId && m.Slot == slot && m.Ativo);
        if (active is not null)
        {
            return active;
        }

        var link = new MentoriaAlunoProfessor(alunoId, professorId, slot);
        _db.AddEntity(link);
        return link;
    }

    private DevelopmentResponsavelDto ToGuardianDto(Responsavel guardian)
    {
        var linkedStudents = _db.ResponsaveisAlunos
            .Where(ra => ra.ResponsavelId == guardian.Id)
            .Join(_db.Usuarios.OfType<Aluno>(), ra => ra.AlunoId, aluno => aluno.Id, (ra, aluno) => new DevelopmentAlunoVinculadoDto(aluno.Id, aluno.Nome, aluno.Email, ra.Parentesco))
            .AsEnumerable()
            .OrderBy(a => a.Nome)
            .ToArray();

        return new DevelopmentResponsavelDto(guardian.Id, guardian.Nome, guardian.Email, linkedStudents);
    }

    private static DevelopmentUsuarioDto ToUserDto(Usuario user)
    {
        return new DevelopmentUsuarioDto(user.Id, user.Nome, user.Email);
    }

    private IReadOnlyCollection<DevelopmentPlanoAlunoDto> BuildDevelopmentPlans(IReadOnlyCollection<Aluno> students)
    {
        return students
            .Select(aluno =>
            {
                var plano = _db.AssinaturasAluno
                    .Where(a => a.AlunoId == aluno.Id && a.Ativa)
                    .OrderByDescending(a => a.DataInicioUtc)
                    .Select(a => a.Plano)
                    .FirstOrDefault();

                return new DevelopmentPlanoAlunoDto(aluno.Id, aluno.Nome, plano.ToString());
            })
            .OrderBy(p => p.AlunoNome)
            .ToArray();
    }

    private IReadOnlyCollection<DevelopmentMentoriaDto> BuildDevelopmentMentoring()
    {
        var mentorias = _db.MentoriasAlunoProfessor
            .Where(m => m.Ativo)
            .ToArray();

        return mentorias
            .Select(m =>
            {
                var aluno = _db.Usuarios.OfType<Aluno>().Single(a => a.Id == m.AlunoId);
                var professor = _db.Professores.Single(p => p.Id == m.ProfessorId);
                return new DevelopmentMentoriaDto(aluno.Id, aluno.Nome, professor.Id, professor.Nome, m.Slot, m.Ativo);
            })
            .OrderBy(m => m.AlunoNome)
            .ThenBy(m => m.Slot)
            .ToArray();
    }

    private sealed record DevUser(string Name, string Email);
    private sealed record DevStudent(string Name, string Email, DateOnly BirthDate);
    private sealed record DevGuardian(string Name, string Email, string Phone);
    private sealed record DevTeacher(string Name, string Email, string Bio, decimal HourlyPrice, string[] Subjects, string[] Qualifications, TeacherDemoState State);
    private sealed record DevFaq(string Pergunta, string Resposta, CategoriaFaq Categoria, string PalavrasChave, int OrdemExibicao, string? TextoAcao = null, string? RotaAcao = null);

    private enum TeacherDemoState
    {
        Approved,
        InReviewWithPendingQualification,
        PendingApplication
    }
}
