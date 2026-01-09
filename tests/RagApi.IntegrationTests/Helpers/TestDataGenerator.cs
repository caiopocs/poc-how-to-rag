namespace RagApi.IntegrationTests.Helpers
{
    /// <summary>
    /// Helper para criar dados de teste realistas.
    /// Útil para gerar documentos com conteúdo variado para testes.
    /// </summary>
    public static class TestDataGenerator
    {
        /// <summary>
        /// Gera um documento de texto sobre um tópico específico.
        /// </summary>
        public static string GenerateDocument(string topic, int paragraphs = 5)
        {
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine($"# {topic}");
            sb.AppendLine();
            
            for (int i = 0; i < paragraphs; i++)
            {
                sb.AppendLine($"This is paragraph {i + 1} about {topic}. " +
                    $"It contains information that can be used for testing " +
                    $"the RAG system's ability to retrieve and generate answers. " +
                    $"The content is designed to be semantically rich and varied.");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Cria múltiplos documentos relacionados para testar recuperação multi-documento.
        /// </summary>
        public static Dictionary<string, string> GenerateRelatedDocuments(
            string baseTopic, int count = 3)
        {
            var documents = new Dictionary<string, string>();

            for (int i = 0; i < count; i++)
            {
                var subtopic = $"{baseTopic} - Part {i + 1}";
                documents[$"doc_{i + 1}.txt"] = GenerateDocument(subtopic, 3);
            }

            return documents;
        }

        /// <summary>
        /// Gera perguntas esperadas para um tópico.
        /// </summary>
        public static List<string> GenerateQuestionsForTopic(string topic)
        {
            return new List<string>
            {
                $"What is {topic}?",
                $"Explain {topic} in detail",
                $"What are the key aspects of {topic}?",
                $"How does {topic} work?",
                $"Why is {topic} important?"
            };
        }
    }
}
