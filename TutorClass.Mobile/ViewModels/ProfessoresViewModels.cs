using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using TutorClass.Mobile.Models;
using TutorClass.Mobile.Services;

namespace TutorClass.Mobile.ViewModels;

public abstract class ConsultaViewModel : INotifyPropertyChanged
{
    public bool IsBusy { get; private set; }
    public string Erro { get; private set; } = "";
    public bool HasError => Erro.Length > 0;
    public bool CanLoad => !IsBusy;
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    protected void Refresh() => Changed(string.Empty);
    protected async Task LoadAsync(Func<Task> action, CancellationToken ct, string recurso = "os professores")
    {
        if (IsBusy) return;
        IsBusy = true;
        Erro = "";
        Refresh();
        try { await action(); }
        catch (UnauthorizedAccessException) { Erro = "Sua sessão expirou. Entre novamente."; }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (OperationCanceledException) { Erro = "A conexão demorou demais. Tente novamente."; }
        catch { Erro = $"Não foi possível carregar {recurso}. Verifique sua conexão e tente novamente."; }
        finally { IsBusy = false; Refresh(); }
    }
}

public sealed class ProfessorItemViewModel(ProfessorResumoDto professor)
{
    private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");
    public Guid Id => professor.Id;
    public string Nome => professor.Nome;
    public string Materias => professor.Materias is { Length: > 0 } ? string.Join(", ", professor.Materias) : "Matérias não informadas";
    public string Preco => professor.PrecoHora is { } value ? value.ToString("C2", Pt) + " / hora" : "Preço não informado";
    public string Avaliacao => professor.MediaAvaliacoes is { } value
        ? $"{value.ToString("0.0", Pt)} / 5 • {professor.QuantidadeAvaliacoes} avaliações"
        : string.IsNullOrWhiteSpace(professor.TextoAvaliacoes) ? "Sem avaliações" : professor.TextoAvaliacoes;
    public string Reputacao => string.IsNullOrWhiteSpace(professor.NivelReputacao) ? "Reputação não informada" : professor.NivelReputacao;
}

public sealed class ProfessoresViewModel(ProfessorService service, IAppNavigation navigation) : ConsultaViewModel
{
    public ProfessorItemViewModel[] Professores { get; private set; } = [];
    public bool IsEmpty { get; private set; }
    public Task CarregarAsync(CancellationToken ct = default) => LoadAsync(async () =>
    {
        IsEmpty = false;
        Professores = [];
        Refresh();
        var result = await service.ListarAsync(ct);
        ct.ThrowIfCancellationRequested();
        Professores = result.Select(p => new ProfessorItemViewModel(p)).ToArray();
        IsEmpty = Professores.Length == 0;
    }, ct);
    public Task SelecionarAsync(ProfessorItemViewModel professor) => navigation.ShowProfessorAsync(professor.Id);
}

public sealed class ProfessorDetalhesViewModel(ProfessorService service, Guid id) : ConsultaViewModel
{
    private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");
    public ProfessorItemViewModel? Professor { get; private set; }
    public bool NotFound { get; private set; }
    public bool HasProfessor => Professor is not null;
    public string Qualificacoes { get; private set; } = "";
    public string Disponibilidades { get; private set; } = "";
    public Task CarregarAsync(CancellationToken ct = default) => LoadAsync(async () =>
    {
        Professor = null;
        NotFound = false;
        Qualificacoes = Disponibilidades = "";
        Refresh();
        var result = await service.BuscarAsync(id, ct);
        ct.ThrowIfCancellationRequested();
        NotFound = result is null;
        if (result is null) return;
        Professor = new(result);
        Qualificacoes = result.Qualificacoes is { Length: > 0 }
            ? string.Join("\n\n", result.Qualificacoes.Select(q => string.Join("\n",
                new[] { q.Titulo, q.Instituicao, q.Descricao, q.DataConclusao?.ToString("dd/MM/yyyy", Pt) }.Where(s => !string.IsNullOrWhiteSpace(s)))))
            : "Qualificações não informadas";
        Disponibilidades = result.Disponibilidades is { Length: > 0 }
            ? string.Join("\n", result.Disponibilidades.OrderBy(d => d.DiaSemana).ThenBy(d => d.Inicio)
                .Select(d => $"{Pt.DateTimeFormat.GetDayName(d.DiaSemana)}: {d.Inicio:HH:mm}–{d.Fim:HH:mm}"))
            : "Disponibilidades não informadas";
    }, ct);
}


