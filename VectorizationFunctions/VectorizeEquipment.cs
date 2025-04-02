using Azure;
using Azure.AI.OpenAI;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OpenAI.Embeddings;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace InventoryManagement
{
    public class VectorizeEquipment
    {
        private readonly ILogger _logger;
        private readonly EmbeddingClient _embeddingClient;

        const string DatabaseName = "InventoryRentalDB";
        const string ContainerName = "equipment";

        public VectorizeEquipment(ILoggerFactory loggerFactory)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var endpointUrl = config["Values:AzureOpenAIEndpoint"];
            if (string.IsNullOrEmpty(endpointUrl))
                throw new ArgumentNullException("AzureOpenAIEndpoint", "AzureOpenAIEndpoint is required to run this function.");

            var azureOpenAIKey = config["Values:AzureOpenAIKey"];
            if (string.IsNullOrEmpty(azureOpenAIKey))
                throw new ArgumentNullException("AzureOpenAIKey", "AzureOpenAIKey is required to run this function.");

            var deploymentName = config["Values:EmbeddingDeploymentName"];
            if (string.IsNullOrEmpty(deploymentName))
                throw new ArgumentNullException("EmbeddingDeploymentName", "EmbeddingDeploymentName is required to run this function.");

            _logger = loggerFactory.CreateLogger<VectorizeEquipment>();
            var oaiEndpoint = new Uri(endpointUrl);
            var credentials = new AzureKeyCredential(azureOpenAIKey);
            var openAIClient = new AzureOpenAIClient(oaiEndpoint, credentials);
            _embeddingClient = openAIClient.GetEmbeddingClient(deploymentName); 
        }

        [Function("VectorizeEquipment")]
        [CosmosDBOutput(DatabaseName, ContainerName, Connection = "inventoryrentalcosmos_DOCUMENTDB")]
        public async Task<IReadOnlyList<Equipment>?> Run([CosmosDBTrigger(
            databaseName: DatabaseName,
            containerName: ContainerName,
            Connection = "inventoryrentalcosmos_DOCUMENTDB",
            LeaseContainerName = "leases",
            CreateLeaseContainerIfNotExists = true)] IReadOnlyList<Equipment> input)
        {
            var documentsToVectorize = input.Where(t => t.Embedding == null);
            if (documentsToVectorize.Count() == 0) return null;

            foreach (var request in documentsToVectorize)
            {
                try
                {
                    // print out the entire request object to the console
                    _logger.LogInformation($"Processing inventory request {request.Id} ");
                    
                    // Combine the store and details fields into a single string for embedding.
                    var request_text = $"Equipment: {request.Name}\n Request Details: {request.Description}";
                    // Generate a vector for the inventory request.
                    var embedding = _embeddingClient.GenerateEmbedding(request_text);
                    var requestVector = embedding.Value.Vector;

                    // Add the vector embeddings to the inventory request and mark it as vectorized.
                    request.Embedding = requestVector.ToArray();
                    _logger.LogInformation($"Generated vector embeddings for inventory request {request.Id}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error generating vector embeddings for inventory request {request.Id}");
                }
            }

            // Write the updated documents back to Cosmos DB.
            return input;
        }
    }

    public class Equipment
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("tenantId")]
        public required string TenantId { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("description")]
        public required string Description { get; set; }

        [JsonPropertyName("category")]
        public required string Category { get; set; }

        [JsonPropertyName("status")]
        public required string Status { get; set; }

        [JsonPropertyName("attributes")]
        public required Dictionary<string, string> Attributes { get; set; }

        [JsonPropertyName("lastMaintenanceDate")]
        public DateTime? LastMaintenanceDate { get; set; }

        [JsonPropertyName("purchaseDate")]
        public DateTime PurchaseDate { get; set; }

        [JsonPropertyName("serialNumber")]
        public string? SerialNumber { get; set; }

        [JsonPropertyName("embedding")]
        public float[]? Embedding { get; set; }

        [JsonPropertyName("searchableText")]
        public string? SearchableText { get; set; }
    }

}
