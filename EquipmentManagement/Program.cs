using Microsoft.Azure.Cosmos;
using Microsoft.SemanticKernel;
using EquipmentManagement.Services;
using EquipmentManagement.Agents;
using EquipmentManagement.Plugins;

DotNetEnv.Env.Load();
var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure environment variables
var gpt4_endpoint = Environment.GetEnvironmentVariable("AzureOpenAI__GPT4__Endpoint") ?? 
    throw new InvalidOperationException("AzureOpenAI__GPT4__Endpoint environment variable is required");
var gpt4_deploymentName = Environment.GetEnvironmentVariable("AzureOpenAI__GPT4__DeploymentName") ?? 
    throw new InvalidOperationException("AzureOpenAI__GPT4__DeploymentName environment variable is required");
var gpt4_apiKey = Environment.GetEnvironmentVariable("AzureOpenAI__GPT4__ApiKey") ?? 
    throw new InvalidOperationException("AzureOpenAI__GPT4__ApiKey environment variable is required");
var embeddings_endpoint = Environment.GetEnvironmentVariable("AzureOpenAI__Embeddings__Endpoint") ?? 
    throw new InvalidOperationException("AzureOpenAI__Embeddings__Endpoint environment variable is required");
var embeddings_deploymentName = Environment.GetEnvironmentVariable("AzureOpenAI__Embeddings__DeploymentName") ?? 
    throw new InvalidOperationException("AzureOpenAI__Embeddings__DeploymentName environment variable is required");
var embeddings_apiKey = Environment.GetEnvironmentVariable("AzureOpenAI__Embeddings__ApiKey") ?? 
    throw new InvalidOperationException("AzureOpenAI__Embeddings__ApiKey environment variable is required");
var cosmosConnectionString = Environment.GetEnvironmentVariable("CosmosDB__ConnectionString") ?? 
    throw new InvalidOperationException("CosmosDB__ConnectionString environment variable is required");

Console.WriteLine($"Connection Strings Check:\nGPT4 Endpoint: {gpt4_endpoint}\nEmbeddings Endpoint: {embeddings_endpoint}\nCosmos Connection: {cosmosConnectionString.Substring(0, Math.Min(20, cosmosConnectionString.Length))}...");

// Register CosmosClient as a singleton
builder.Services.AddSingleton<CosmosClient>((_) => new CosmosClient(cosmosConnectionString));

// Create and register IKernelBuilder
builder.Services.AddSingleton<IKernelBuilder>(sp =>
{
    var kernelBuilder = Kernel.CreateBuilder();
    
    kernelBuilder.AddAzureOpenAIChatCompletion(
        deploymentName: gpt4_deploymentName,
        endpoint: gpt4_endpoint,
        apiKey: gpt4_apiKey
    );

#pragma warning disable SKEXP0010
    kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(
        deploymentName: embeddings_deploymentName,
        endpoint: embeddings_endpoint,
        apiKey: embeddings_apiKey
    );
#pragma warning restore SKEXP0010

    return kernelBuilder;
});

// Register repository service
builder.Services.AddSingleton<IEquipmentRepository, CosmosDbEquipmentRepository>();
// Register embedding service
builder.Services.AddSingleton<IVectorizationService, VectorizationService>();
// Register EquipmentRequestPlugin
builder.Services.AddSingleton<EquipmentRequestPlugin>();

// Register Kernel after repository
builder.Services.AddSingleton<Kernel>(sp =>
{
    var kernelBuilder = sp.GetRequiredService<IKernelBuilder>();
    var repository = sp.GetRequiredService<IEquipmentRepository>();
    var equipmentPlugin = sp.GetRequiredService<EquipmentRequestPlugin>();
    
    var kernel = kernelBuilder.Build();
    
    kernel.Plugins.AddFromObject(repository, "Equipment");
    kernel.Plugins.AddFromObject(equipmentPlugin, "EquipmentCopilot");

    return kernel;
});

// Register EquipmentCopilot
builder.Services.AddSingleton<EquipmentCopilot>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
