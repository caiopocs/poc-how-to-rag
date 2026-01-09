using Microsoft.KernelMemory;
using RagApi.Models;

namespace RagApi.Services
{
    public class ChatService : IChatService
    {
        private readonly IKernelMemory _memory;

        public ChatService(IKernelMemory memory)
        {
            _memory = memory;
        }

        public async Task<ChatResponse> AskQuestionAsync(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return new ChatResponse 
                { 
                    Answer = string.Empty, 
                    Sources = new List<string>() 
                };
            }

            try
            {
                var result = await _memory.AskAsync(question);
                
                return new ChatResponse
                {
                    Answer = result.Result,
                    Sources = result.RelevantSources
                        .Select(s => s.SourceName)
                        .Distinct()
                        .ToList()
                };
            }
            catch
            {
                return new ChatResponse 
                { 
                    Answer = string.Empty, 
                    Sources = new List<string>() 
                };
            }
        }
    }
}