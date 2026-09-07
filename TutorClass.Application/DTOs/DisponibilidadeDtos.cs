namespace TutorClass.Application.DTOs;

public sealed record CadastrarDisponibilidadeProfessorRequest(DayOfWeek DiaSemana, TimeOnly Inicio, TimeOnly Fim);

public sealed record AtualizarDisponibilidadeProfessorRequest(DayOfWeek DiaSemana, TimeOnly Inicio, TimeOnly Fim);

public sealed record DisponibilidadeProfessorDto(Guid Id, DayOfWeek DiaSemana, TimeOnly Inicio, TimeOnly Fim);

public sealed record HorarioLivreProfessorDto(DateOnly Data, TimeOnly Inicio, TimeOnly Fim);
