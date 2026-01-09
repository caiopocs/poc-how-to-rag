using Microsoft.KernelMemory;
using RagApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Kernel Memory with Qdrant and Ollama
// Note: This is a simplified configuration for testing
// In production, configure with actual Qdrant and Ollama endpoints
var memoryBuilder = new KernelMemoryBuilder();
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