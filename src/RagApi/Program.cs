using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.Ollama;
using Microsoft.KernelMemory.Configuration;
using RagApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Kernel Memory with Qdrant and Ollama
// Use real persistence for production-ready setup
var qdrantEndpoint = builder.Configuration["Qdrant:Host"] ?? "http://localhost:6333";
var ollamaEndpoint = builder.Configuration["Ollama:ServiceUrl"] ?? "http://localhost:11434";
var ollamaModel = builder.Configuration["Ollama:Model"] ?? "llama3";
var embeddingModel = builder.Configuration["Ollama:EmbeddingModel"] ?? "nomic-embed-text";

var memoryBuilder = new KernelMemoryBuilder()
    .WithQdrantMemoryDb(new QdrantConfig
    {
        Endpoint = qdrantEndpoint,
        APIKey = ""
    })
    .WithSimpleFileStorage("/app/data")
    .WithOpenAITextGeneration(new OpenAIConfig
    {
        APIKey = "not-used",
        Endpoint = $"{ollamaEndpoint}/v1",
        TextModel = ollamaModel
    })
    .WithOpenAITextEmbeddingGeneration(new OpenAIConfig
    {
        APIKey = "not-used",
        Endpoint = $"{ollamaEndpoint}/v1",
        EmbeddingModel = embeddingModel
    });

// Build serverless - processes synchronously inline
var memory = memoryBuilder.Build<MemoryServerless>();

builder.Services.AddSingleton<IKernelMemory>(memory);

// Register application services
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IChatService, ChatService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();

// Make Program accessible for testing
public partial class Program { }