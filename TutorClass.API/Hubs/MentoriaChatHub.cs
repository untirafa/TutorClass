using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TutorClass.API.Security;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;

namespace TutorClass.API.Hubs;

[Authorize]
public sealed class MentoriaChatHub : Hub
{
    private readonly MentoriaChatService _service;

    public MentoriaChatHub(MentoriaChatService service)
    {
        _service = service;
    }

    public static string GroupName(Guid vinculoMentoriaId) => $"mentoria-chat-{vinculoMentoriaId:N}";

    public async Task EntrarNoChat(Guid vinculoMentoriaId)
    {
        await _service.ObterChatAsync(vinculoMentoriaId, Context.User!.GetUserId(), Context.ConnectionAborted);
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(vinculoMentoriaId), Context.ConnectionAborted);
    }

    public async Task<MensagemMentoriaResponse> EnviarMensagem(Guid vinculoMentoriaId, string conteudo)
    {
        var mensagem = await _service.EnviarMensagemAsync(
            vinculoMentoriaId,
            Context.User!.GetUserId(),
            new EnviarMensagemMentoriaRequest(conteudo),
            Context.ConnectionAborted);

        await Clients.Group(GroupName(vinculoMentoriaId)).SendAsync("MensagemRecebida", mensagem, Context.ConnectionAborted);
        return mensagem;
    }

    public async Task<MensagensLidasResponse> MarcarComoLidas(Guid vinculoMentoriaId)
    {
        return await _service.MarcarComoLidasAsync(vinculoMentoriaId, Context.User!.GetUserId(), Context.ConnectionAborted);
    }
}
