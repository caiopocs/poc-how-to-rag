using RagApi.Models;

namespace RagApi.Services
{
    public interface IChatService
    {
        Task<ChatResponse> AskQuestionAsync(string question);
    }
}