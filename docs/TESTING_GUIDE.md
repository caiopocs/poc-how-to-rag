# Guia Completo de Testes - LocalRagSystem

## 📋 Visão Geral

Este documento descreve a estratégia completa de testes para o sistema RAG local, incluindo testes unitários, de integração e end-to-end.

## 🎯 Pirâmide de Testes

```
                    /\
                   /  \
                  / E2E \      ← Poucos, lentos, alta confiança
                 /--------\
                /          \
               / Integração \  ← Moderados, validam integrações reais
              /--------------\
             /                \
            /    Unitários      \ ← Muitos, rápidos, baixo custo
           /--------------------\
```

## 1️⃣ Testes Unitários (RagApi.Tests)

### Objetivo
Validar a lógica de negócio de forma isolada, usando mocks para dependências externas.

### Quando Executar
- A cada commit
- Durante desenvolvimento (TDD)
- Em pipelines de CI/CD

### Como Executar
```bash
cd tests/RagApi.Tests
dotnet test --logger "console;verbosity=detailed"
```

### Cobertura
- ✅ Controllers (validação de entrada, respostas HTTP)
- ✅ Services (lógica de negócio com dependências mockadas)
- ✅ Models (validação de dados)
- ✅ Tratamento de erros

### Exemplo de Teste Unitário
```csharp
[Fact]
public async Task IngestDocument_WithNullRequest_ShouldReturnBadRequest()
{
    // Arrange
    var mockService = new Mock<IDocumentService>();
    var controller = new DocumentsController(mockService.Object);
    
    // Act
    var result = await controller.IngestDocument(null);
    
    // Assert
    result.Should().BeOfType<BadRequestObjectResult>();
}
```

## 2️⃣ Testes de Integração (RagApi.IntegrationTests)

### Objetivo
Validar o fluxo COMPLETO do sistema RAG com dependências reais (Qdrant + Ollama).

### Quando Executar
- Antes de criar Pull Requests
- Antes de deploys
- Periodicamente (nightly builds)

### Pré-requisitos
- Docker Desktop instalado e rodando
- Pelo menos 8GB de RAM disponível
- Conexão com internet (primeira execução)

### Como Executar
```bash
# Garantir que Docker está rodando
docker ps

# Executar testes
cd tests/RagApi.IntegrationTests
dotnet test --logger "console;verbosity=detailed"
```

### ⚠️ Importante: Primeira Execução
Na primeira vez, os testes vão:
1. Baixar imagens Docker (Qdrant + Ollama) - ~2GB
2. Baixar modelos de IA:
   - `llama3` (~4.7GB)
   - `nomic-embed-text` (~274MB)
3. Inicializar containers
4. Executar testes

**Tempo estimado:** 15-30 minutos na primeira execução, 5-10 minutos nas subsequentes.

### Cobertura
- ✅ Ingestão completa: documento → chunking → embeddings → armazenamento
- ✅ Recuperação completa: pergunta → busca vetorial → geração LLM
- ✅ Processamento de múltiplos documentos
- ✅ Validação de respostas e citações de fontes
- ✅ Consistência de resultados

### Fixtures Utilizadas

#### QdrantFixture
```csharp
// Gerencia container do Qdrant
// - Porta: 6333
// - Isolamento: cada teste tem banco limpo
```

#### OllamaFixture
```csharp
// Gerencia container do Ollama
// - Porta: 11434
// - Modelos: llama3, nomic-embed-text
```

#### RagSystemFixture
```csharp
// Combina Qdrant + Ollama + Kernel Memory
// - Configuração completa do sistema RAG
// - Compartilhada entre testes da mesma collection
```

### Exemplo de Teste de Integração
```csharp
[Collection("RagSystem")]
public class DocumentServiceIntegrationTests
{
    private readonly RagSystemFixture _fixture;
    
    [Fact]
    public async Task IngestDocument_ShouldProcessAndStoreInQdrant()
    {
        // Arrange
        var document = File.OpenRead("sample.pdf");
        var service = new DocumentService(_fixture.Memory);
        
        // Act - Fluxo real: chunk → embed → store
        var result = await service.IngestDocumentAsync(document, "sample.pdf");
        await Task.Delay(10000); // Aguardar processamento
        
        // Assert - Verificar se foi armazenado
        var answer = await _fixture.Memory.AskAsync("What is in the document?");
        answer.Should().NotBeNull();
        answer.RelevantSources.Should().NotBeEmpty();
    }
}
```

## 3️⃣ Testes End-to-End (RagApi.E2ETests)

### Objetivo
Validar os endpoints HTTP da API em um ambiente completo.

### Quando Executar
- Antes de releases
- Após mudanças em endpoints
- Validação final de features

### Pré-requisitos
**Serviços devem estar rodando externamente:**

```bash
# Iniciar serviços
docker-compose up -d

# Verificar que estão saudáveis
curl http://localhost:6333/collections
curl http://localhost:11434/api/tags

# Aguardar modelos serem baixados (primeira vez)
docker logs -f localragsystem-ollama-1
```

### Como Executar
```bash
# Com serviços rodando
cd tests/RagApi.E2ETests
dotnet test --logger "console;verbosity=detailed"
```

### Cobertura
- ✅ POST /api/ingest (upload de documentos)
- ✅ POST /api/chat/ask (perguntas e respostas)
- ✅ Validação de entrada (null, vazio, inválido)
- ✅ Códigos HTTP corretos (200, 400, 500)
- ✅ Formato de resposta JSON
- ✅ Fluxo completo através da API

### Exemplo de Teste E2E
```csharp
public class DocumentsControllerE2ETests : IClassFixture<RagApiFactory>
{
    private readonly HttpClient _client;
    
    [Fact]
    public async Task IngestDocument_WithValidFile_Returns200()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(fileBytes), "file", "test.pdf");
        
        // Act - HTTP POST real
        var response = await _client.PostAsync("/api/ingest", content);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

## 📊 Comparação dos Níveis de Teste

| Aspecto | Unitários | Integração | E2E |
|---------|-----------|------------|-----|
| **Velocidade** | ⚡ <1s por teste | 🐌 10-30s por teste | 🕐 2-10s por teste |
| **Confiabilidade** | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Custo** | 💰 Baixo | 💰💰💰 Alto | 💰💰 Moderado |
| **Feedback** | Imediato | Lento | Moderado |
| **Dependências** | Mocks | Containers reais | Serviços externos |
| **Complexidade** | Simples | Complexa | Moderada |

## 🚀 Executando Todos os Testes

### Localmente
```bash
# Da raiz do projeto
dotnet test

# Com filtros
dotnet test --filter "FullyQualifiedName~Unit"
dotnet test --filter "FullyQualifiedName~Integration"
dotnet test --filter "FullyQualifiedName~E2E"
```

### Com Coverage
```bash
dotnet test /p:CollectCoverage=true \
            /p:CoverletOutputFormat=opencover \
            /p:CoverletOutput=./coverage/
```

### Em CI/CD
```yaml
# Exemplo: GitHub Actions
- name: Unit Tests
  run: dotnet test --filter "FullyQualifiedName~RagApi.Tests"
  
- name: Integration Tests
  run: |
    docker-compose up -d
    dotnet test --filter "FullyQualifiedName~IntegrationTests"
    docker-compose down
```

## 🔧 Troubleshooting

### Problema: Testes de integração falhando com timeout

**Solução:**
```bash
# Aumentar recursos do Docker Desktop
# Settings → Resources → Memory: 8GB+

# Limpar cache
docker system prune -a --volumes

# Reexecutar
dotnet test
```

### Problema: Modelos não estão sendo baixados

**Solução:**
```bash
# Entrar no container
docker exec -it localragsystem-ollama-1 bash

# Baixar manualmente
ollama pull llama3
ollama pull nomic-embed-text

# Verificar
ollama list
```

### Problema: Qdrant não inicia

**Solução:**
```bash
# Verificar logs
docker logs localragsystem-qdrant-1

# Recriar container
docker-compose down -v
docker-compose up -d qdrant
```

### Problema: Testes E2E retornam 500

**Solução:**
```bash
# Verificar se serviços estão saudáveis
curl http://localhost:6333
curl http://localhost:11434

# Verificar logs da API
docker-compose logs -f ragapi

# Reiniciar serviços
docker-compose restart
```

## 📈 Métricas de Qualidade

### Objetivos de Cobertura
- **Unitários:** >80% de cobertura de código
- **Integração:** Todos os fluxos críticos cobertos
- **E2E:** Todos os endpoints públicos testados

### Tempo de Execução Aceitável
- **Unitários:** <30 segundos total
- **Integração:** <10 minutos total
- **E2E:** <5 minutos total

## 🎓 Melhores Práticas

### 1. Nomenclatura de Testes
```csharp
// Padrão: MethodName_Scenario_ExpectedResult
[Fact]
public async Task IngestDocument_WithNullStream_ShouldReturnFalse()
```

### 2. Arrange-Act-Assert
```csharp
[Fact]
public async Task ExampleTest()
{
    // Arrange - Preparar dados
    var input = "test";
    
    // Act - Executar ação
    var result = await service.Process(input);
    
    // Assert - Verificar resultado
    result.Should().Be("expected");
}
```

### 3. Isolamento de Testes
- Cada teste deve ser independente
- Não compartilhar estado mutável
- Usar fixtures para setup/teardown

### 4. Testes Descritivos
```csharp
// ❌ Ruim
[Fact]
public void Test1() { }

// ✅ Bom
[Fact]
public void AskQuestion_WithValidInput_ReturnsAnswerWithSources() { }
```

### 5. Usar FluentAssertions
```csharp
// ✅ Legível e descritivo
response.Should().NotBeNull();
response.Answer.Should().Contain("RAG");
response.Sources.Should().HaveCountGreaterThan(0);
```

## 📚 Recursos Adicionais

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions](https://fluentassertions.com/)
- [TestContainers](https://dotnet.testcontainers.org/)
- [Microsoft Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/)

## 🤝 Contribuindo com Testes

Ao adicionar nova funcionalidade:
1. ✅ Escrever testes unitários primeiro (TDD)
2. ✅ Adicionar testes de integração para fluxos críticos
3. ✅ Criar testes E2E para novos endpoints
4. ✅ Verificar que todos os testes passam
5. ✅ Atualizar esta documentação se necessário
