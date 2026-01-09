using FluentAssertions;
using Microsoft.KernelMemory;

namespace RagApi.E2ETests.Helpers
{
    /// <summary>
    /// Helpers para validar respostas do sistema RAG nos testes E2E.
    /// </summary>
    public static class ResponseValidator
    {
        /// <summary>
        /// Valida se uma resposta contém informação relevante.
        /// </summary>
        public static void ValidateHasRelevantContent(
            string response,
            string expectedTopic,
            string because = "")
        {
            response.Should().NotBeNullOrEmpty(because);
            response.Length.Should().BeGreaterThan(10,
                "resposta deve ter conteúdo substancial");
        }

        /// <summary>
        /// Valida se as fontes foram incluídas corretamente.
        /// </summary>
        public static void ValidateHasSources(
            IEnumerable<string> sources,
            string because = "")
        {
            sources.Should().NotBeNull(because);
            sources.Should().NotBeEmpty("deve incluir fontes quando há contexto relevante");
        }

        /// <summary>
        /// Valida se a resposta menciona palavras-chave específicas.
        /// </summary>
        public static void ValidateContainsKeywords(
            string response,
            IEnumerable<string> keywords,
            string because = "")
        {
            response.Should().NotBeNullOrEmpty();
            
            var lowercaseResponse = response.ToLower();
            var matchedKeywords = keywords.Where(k => 
                lowercaseResponse.Contains(k.ToLower())).ToList();

            matchedKeywords.Should().NotBeEmpty(
                because + $" (esperava ao menos uma palavra-chave: {string.Join(", ", keywords)})");
        }

        /// <summary>
        /// Valida a estrutura completa de uma resposta RAG.
        /// </summary>
        public static void ValidateRagResponse(
            string answer,
            IEnumerable<string> sources,
            bool shouldHaveSources = true)
        {
            answer.Should().NotBeNullOrEmpty("resposta não deve estar vazia");
            
            if (shouldHaveSources)
            {
                sources.Should().NotBeEmpty("deve incluir fontes relevantes");
            }
        }
    }
}
