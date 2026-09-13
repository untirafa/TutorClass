using System.Globalization;
using TutorClass.Mobile.Models;
using TutorClass.Mobile.Services;

namespace TutorClass.Mobile.ViewModels;

public abstract class ListaConsultaViewModel<T> : ConsultaViewModel
{
    public T[] Itens { get; private set; } = [];
    public bool IsEmpty { get; private set; }
    protected abstract string Recurso { get; }
    protected abstract Task<T[]> ConsultarAsync(CancellationToken ct);
    public Task CarregarAsync(CancellationToken ct = default) => LoadAsync(async () =>
    {
        Itens = [];
        IsEmpty = false;
        Refresh();
        var result = await ConsultarAsync(ct);
        ct.ThrowIfCancellationRequested();
        Itens = result;
        IsEmpty = result.Length == 0;
    }, ct, Recurso);
}

public sealed class MateriasViewModel(MateriaService service) : ListaConsultaViewModel<MateriaDto>
{
    protected override string Recurso => "as matérias";
    protected override Task<MateriaDto[]> ConsultarAsync(CancellationToken ct) => service.ListarAsync(ct);
}

public sealed class AulaItemViewModel(AulaResumoDto aula)
{
    private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");
    public string Materia => aula.Materia ?? "Matéria não informada";
    public string Professor => aula.Professor ?? "Professor não informado";
    public string Horario => aula.Inicio is { } inicio
        ? inicio.ToLocalTime().ToString("dd/MM/yyyy 'às' HH:mm", Pt) + " (horário do dispositivo)"
        : "Horário não informado";
    public string Duracao => aula.DuracaoMinutos is { } minutos ? $"{minutos} minutos" : "Duração não informada";
    public string Status => aula.Status ?? "Status não informado";
    public string Valor => aula.PrecoAcordado is { } valor ? valor.ToString("C2", Pt) : "Valor não informado";
}

public sealed class MinhasAulasViewModel(AulaService service, IAppNavigation navigation) : ListaConsultaViewModel<AulaItemViewModel>
{
    protected override string Recurso => "suas aulas";
    protected override async Task<AulaItemViewModel[]> ConsultarAsync(CancellationToken ct)
    {
        try { return (await service.ListarMinhasAsync(ct)).Select(a => new AulaItemViewModel(a)).ToArray(); }
        catch (UnauthorizedAccessException)
        {
            navigation.ShowLogin();
            // The protected screen is replaced by Login; do not display an empty result.
            throw;
        }
    }
}

