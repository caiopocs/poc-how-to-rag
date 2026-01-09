# LocalRagSystem

## Overview
The LocalRagSystem is a .NET 9 Web API designed for a Retrieval-Augmented Generation (RAG) system that operates locally. It focuses on document ingestion, specifically handling PDF files, and provides chat functionality that allows users to interact with the ingested documents.

## Project Structure
The solution consists of the following projects:

- **RagApi**: The main Web API project that handles document ingestion and chat functionalities.
- **RagApi.Tests**: A project containing unit tests for the API controllers and services.
- **RagApi.IntegrationTests**: Integration tests that validate the complete flow with real Qdrant and Ollama instances.
- **RagApi.E2ETests**: End-to-end tests that validate the HTTP API endpoints.

## Features
- **Document Ingestion**: Upload PDF documents to the system for processing.
- **Chat Functionality**: Ask questions related to the ingested documents and receive AI-generated responses.

## Technologies Used
- .NET 9
- Microsoft.KernelMemory.Core for document processing
- Docker for containerization
- Qdrant for vector storage
- Ollama for AI model serving

## Setup Instructions

### Prerequisites
- .NET 9 SDK
- Docker and Docker Compose

### Running the Application
1. Clone the repository:
   ```
   git clone <repository-url>
   cd LocalRagSystem
   ```

2. Build and run the Docker containers:
   ```
   docker-compose up --build
   ```

3. The API will be available at `http://localhost:5000`.

### API Endpoints
- **Ingest Document**: 
  - `POST /api/ingest`
  - Request Body: `DocumentUploadRequest`
  
- **Ask Question**: 
  - `POST /api/ask`
  - Request Body: `ChatRequest`

## Testing

O projeto possui três níveis de testes para garantir a qualidade e confiabilidade do sistema RAG:

### 1. Testes Unitários (RagApi.Tests)
Testes rápidos que validam a lógica de negócio usando mocks.

```bash
cd tests/RagApi.Tests
dotnet test
```

**O que é testado:**
- Lógica dos controllers
- Validação de entrada
- Tratamento de erros
- Comportamento dos serviços com dependências mockadas

### 2. Testes de Integração (RagApi.IntegrationTests)
Testes que validam a integração COMPLETA com Qdrant e Ollama usando TestContainers.

```bash
cd tests/RagApi.IntegrationTests
dotnet test
```

**O que é testado:**
- Fluxo completo de ingestão: documento → chunking → embeddings → armazenamento
- Fluxo completo de recuperação: pergunta → embedding → busca vetorial → geração LLM
- Integração real com Kernel Memory, Qdrant e Ollama
- Processamento de múltiplos documentos
- Validação de respostas e fontes

**⚠️ IMPORTANTE:**
- Estes testes são **LENTOS** (podem levar vários minutos)
- Inicializam containers Docker automaticamente (Qdrant + Ollama)
- Baixam modelos de IA na primeira execução (~2GB)
- Requerem Docker Desktop rodando

**Modelos baixados automaticamente:**
- `llama3` - LLM para geração de respostas
- `nomic-embed-text` - Modelo de embeddings

### 3. Testes End-to-End (RagApi.E2ETests)
Testes que validam os endpoints HTTP da API completa.

**PRÉ-REQUISITO:** Qdrant e Ollama devem estar rodando:
```bash
docker-compose up -d
```

Aguarde os modelos serem baixados (primeira execução):
```bash
# Verificar logs do Ollama
docker logs -f localragsystem-ollama-1

# Quando estiver pronto, rodar os testes
cd tests/RagApi.E2ETests
dotnet test
```

**O que é testado:**
- Endpoints HTTP: POST /api/ingest, POST /api/chat/ask
- Upload de arquivos (TXT, PDF)
- Validação de requests inválidos (null, vazio, malformado)
- Respostas HTTP corretas (200, 400, 500)
- Fluxo completo: upload → processamento → query → resposta

### Executar Todos os Testes
```bash
# Da raiz do projeto
dotnet test

# Com coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Estratégia de Testes

| Tipo | Velocidade | Escopo | Dependências Externas | Quando Executar |
|------|-----------|--------|----------------------|-----------------|
| **Unitários** | ⚡ Rápido | Lógica isolada | ❌ Nenhuma | A cada commit |
| **Integração** | 🐌 Lento | Pipeline completo | ✅ Qdrant + Ollama (via containers) | Antes de PR/Deploy |
| **E2E** | 🕐 Moderado | API HTTP | ✅ Serviços rodando externamente | Validação final |

### Troubleshooting

**Testes de Integração falhando:**
```bash
# Verificar se Docker Desktop está rodando
docker ps

# Limpar containers órfãos
docker system prune -a --volumes

# Reexecutar
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

**Testes E2E falhando:**
```bash
# Garantir que os serviços estão rodando
docker-compose up -d

# Verificar saúde dos serviços
curl http://localhost:6333
curl http://localhost:11434

# Aguardar modelos serem baixados
docker logs -f localragsystem-ollama-1

# Reexecutar
dotnet test --filter "FullyQualifiedName~E2ETests"
```

**Modelos não baixam:**
```bash
# Entrar no container do Ollama
docker exec -it localragsystem-ollama-1 bash

# Baixar manualmente
ollama pull llama3
ollama pull nomic-embed-text

# Sair
exit
```

## Contributing
Contributions are welcome! Please submit a pull request or open an issue for any enhancements or bug fixes.

## License
This project is licensed under the MIT License. See the LICENSE file for details.