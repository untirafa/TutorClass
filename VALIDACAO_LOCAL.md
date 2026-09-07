# Validacao local - Acompanhamento, Avaliacoes e Reputacao

Esta versao adiciona acompanhamento pos-aula, avaliacao do professor por aula concluida e calculo de reputacao.

## 1. Restaurar, compilar e testar

```powershell
dotnet restore TutorClass.slnx
dotnet build TutorClass.slnx
dotnet test TutorClass.slnx
```

O ambiente usado para preparar este ZIP nao possui o SDK .NET, por isso estes comandos precisam ser executados localmente antes de aplicar a migration.

## 2. Conferir migrations

```powershell
dotnet dotnet-ef migrations list --project TutorClass.Infrastructure --startup-project TutorClass.API
```

Deve aparecer:

```text
20260829210000_AdicionaAcompanhamentoAvaliacoesReputacao
```

## 3. Aplicar no Neon

Com `ConnectionStrings:TutorClassDb` e `Jwt:Secret` configurados em User Secrets:

```powershell
dotnet dotnet-ef database update --project TutorClass.Infrastructure --startup-project TutorClass.API
```

## 4. Novos endpoints

- `PUT /api/aulas/{aulaId}/acompanhamento` - professor da aula concluida cria ou atualiza o acompanhamento.
- `GET /api/aulas/{aulaId}/acompanhamento` - aluno/professor da aula ou administrador consulta.
- `POST /api/aulas/{aulaId}/avaliacao` - aluno da aula concluida avalia uma unica vez.
- `GET /api/professores/{professorId}/avaliacoes` - consulta publica das avaliacoes.

## 5. Regras de reputacao

- Novo: nenhuma avaliacao.
- Verificado: pelo menos 1 avaliacao.
- Experiente: 5 aulas concluidas + 3 avaliacoes + media >= 4,0.
- Destaque: 15 aulas concluidas + 10 avaliacoes + media >= 4,5.
- Referencia: 30 aulas concluidas + 20 avaliacoes + media >= 4,7.

A nota, a media, a quantidade de avaliacoes, as aulas concluidas e o nivel de reputacao sao controlados pelo backend.
