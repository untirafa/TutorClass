using System.Net;
using System.Net.Http.Json;
using System.Text;
using TutorClass.Mobile.Models;
using TutorClass.Mobile.Services;
using TutorClass.Mobile.ViewModels;

namespace TutorClass.Mobile.Tests;

public sealed class TutorIaStreamingTests
{
    private const string Delta = "event: delta\ndata: {\"text\":\"ação **correta**\"}\n\n";
    private const string Complete = "event: complete\ndata: {\"nome\":\"TutorIA\",\"resposta\":\"ação **correta**\",\"plano\":\"Foco\",\"possuiAcesso\":true,\"usadasHoje\":1,\"restantesHoje\":6}\n\n";

    [Fact]
    public async Task ViewModel_atualiza_texto_antes_do_final_e_usa_quota_confirmada()
    {
        using var stream = new GateStream(Delta, Complete);
        using var http = Client(stream);
        var session = new SessionService(http, new SessionTests.Store { Token="fake" });
        var vm = new TutorIaViewModel(new(session), new SessionTests.Navigation()) { Mensagem="Oi" };
        var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.Mensagens.CollectionChanged += (_, e) =>
        {
            foreach (MensagemIaItem item in e.NewItems ?? Array.Empty<MensagemIaItem>())
                if (item.Autor == "TutorIA") item.PropertyChanged += (_, _) => first.TrySetResult();
        };
        await vm.CarregarAsync();
        var task = vm.EnviarAsync();
        await first.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(task.IsCompleted); Assert.True(vm.IsBusy); Assert.False(vm.PodeEnviar);
        Assert.Equal("ação **correta**", vm.Mensagens[1].Texto);
        Assert.Equal("ação correta", vm.Mensagens[1].TextoApresentacao);
        Assert.Null(vm.Status);
        stream.Finish.SetResult(); await task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, vm.Status!.UsadasHoje); Assert.Equal(6, vm.Status.RestantesHoje);
        Assert.Equal("", vm.Mensagem); Assert.False(vm.Mensagens[1].Interrompida);
    }

    [Fact]
    public async Task Stream_cortado_marca_resposta_incompleta_e_nao_reenvia()
    {
        using var stream = new GateStream(Delta, ""); stream.Finish.SetResult();
        using var http = Client(stream);
        var vm = new TutorIaViewModel(new(new SessionService(http, new SessionTests.Store {Token="fake"})), new SessionTests.Navigation()) {Mensagem="Oi"};
        await vm.CarregarAsync(); await vm.EnviarAsync();
        Assert.True(vm.HasError); Assert.Null(vm.Status); Assert.False(vm.PodeEnviar);
        Assert.True(vm.Mensagens[1].Interrompida); Assert.Contains("Resposta incompleta", vm.Mensagens[1].TextoApresentacao);
        await vm.EnviarAsync(); Assert.Equal(2, vm.Mensagens.Count);
    }

    [Fact]
    public async Task Stream_401_limpa_token()
    {
        var store=new SessionTests.Store { Token="expired" };
        using var http=new HttpClient(new Handler(_=>new(HttpStatusCode.Unauthorized))) {BaseAddress=new("https://example.test/")};
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>new SessionService(http,store).PostStreamAuthenticatedAsync<TutorIaResponse>("stream",new {},_=>{},default));
        Assert.Null(store.Token);
    }

    [Theory]
    [InlineData("### Frações\n**Atenção**: \\(\\frac{1}{2}\\)", "Frações\nAtenção: (1)/(2)")]
    [InlineData("\\frac{1}{\\frac{2}{3}}", "(1)/((2)/(3))")]
    [InlineData("\\sqrt{x} + x^{2} \\times \\pi", "√(x) + x^(2) × π")]
    [InlineData("R$ 10 e R$ 20", "R$ 10 e R$ 20")]
    [InlineData("```js\nconst x = a ** 2;\n```", "const x = a ** 2;\n")]
    public void Formatacao_preserva_significado_e_codigo(string source,string expected)
        => Assert.Equal(expected,TutorIaTexto.Formatar(source));

    private static HttpClient Client(Stream stream) => new(new Handler(request =>
    {
        Assert.Equal("Bearer",request.Headers.Authorization?.Scheme);
        if(request.Method==HttpMethod.Get)return new(HttpStatusCode.OK){Content=JsonContent.Create(new TutorIaResponse{PossuiAcesso=true,Plano="Foco"})};
        Assert.Equal("/api/tutoria/chat/stream",request.RequestUri!.AbsolutePath);
        var content=new StreamContent(stream);content.Headers.ContentType=new("text/event-stream");
        return new(HttpStatusCode.OK){Content=content};
    })){BaseAddress=new("https://example.test/")};
    private sealed class Handler(Func<HttpRequestMessage,HttpResponseMessage> handle):HttpMessageHandler
    {protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req,CancellationToken ct)=>Task.FromResult(handle(req));}
    private sealed class GateStream(string initial,string ending):Stream
    {
        private readonly MemoryStream first=new(Encoding.UTF8.GetBytes(initial));
        private readonly MemoryStream last=new(Encoding.UTF8.GetBytes(ending));
        public TaskCompletionSource Finish {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken ct=default)
        {var count=await first.ReadAsync(buffer,ct);if(count>0)return count;await Finish.Task.WaitAsync(ct);return await last.ReadAsync(buffer,ct);}
        public override bool CanRead=>true;public override bool CanSeek=>false;public override bool CanWrite=>false;
        public override long Length=>throw new NotSupportedException();
        public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}
        public override int Read(byte[] b,int o,int c)=>throw new NotSupportedException();
        public override void Flush(){}public override long Seek(long o,SeekOrigin s)=>throw new NotSupportedException();
        public override void SetLength(long v)=>throw new NotSupportedException();public override void Write(byte[] b,int o,int c)=>throw new NotSupportedException();
        protected override void Dispose(bool disposing){if(disposing){first.Dispose();last.Dispose();}base.Dispose(disposing);}
    }
}
