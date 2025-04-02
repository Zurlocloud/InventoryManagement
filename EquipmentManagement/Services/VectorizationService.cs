using Microsoft.Azure.Cosmos;
using System.Globalization;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using EquipmentManagement.Models;

namespace EquipmentManagement.Services;

/// <summary>
/// The vectorization service for generating embeddings and executing vector searches.
/// </summary>
public class VectorizationService : IVectorizationService
{
    private readonly IKernelBuilder _kernelBuilder;
    private readonly CosmosClient _cosmosClient;
    private readonly string _embeddingDeploymentName;
    private readonly Container _equipmentContainer;

    public VectorizationService(IKernelBuilder kernelBuilder, CosmosClient cosmosClient)
    {
        _kernelBuilder = kernelBuilder;
        _cosmosClient = cosmosClient;
        _embeddingDeploymentName = Environment.GetEnvironmentVariable("AzureOpenAI__Embeddings__DeploymentName") ?? "text-embedding-ada-002";
        
        var databaseId = Environment.GetEnvironmentVariable("CosmosDB__DatabaseId") ?? "InventoryRentalDB";
        var containerId = Environment.GetEnvironmentVariable("CosmosDB__ContainerId") ?? "equipment";
        _equipmentContainer = _cosmosClient.GetContainer(databaseId, containerId);
    }

    /// <summary>
    /// Translate a text string into a vector embedding.
    /// This uses the embedding deployment name in your configuration, or defaults to text-embedding-ada-002.
    /// </summary>
    public async Task<float[]> GetEmbeddings(string text)
    {
        try
        {
            // Create a temporary kernel with the builder
            var kernel = _kernelBuilder.Build();
            
            // Generate a vector for the provided text.
#pragma warning disable SKEXP0001 
            var embeddings = await kernel.GetRequiredService<ITextEmbeddingGenerationService>().GenerateEmbeddingAsync(text);
#pragma warning restore SKEXP0001 

            return embeddings.ToArray();
        }
        catch (Exception ex)
        {
            throw new Exception($"An error occurred while generating embeddings: {ex.Message}");
        }
    }

    /// <summary>
    /// Search for equipment using vector similarity search.
    /// </summary>
    public async Task<List<VectorSearchResults>> VectorSearch(string tenantId, string searchText, int maxResults = 5, double minimum_similarity_score = 0.8)
    {
        try
        {
            var queryVector = await GetEmbeddings(searchText);
            
            var vectorString = string.Join(", ", queryVector.Select(v => v.ToString(CultureInfo.InvariantCulture)).ToArray());
            
            var query = $"SELECT c.id, c.tenantId, c.name, c.description, VectorDistance(c.embedding, [{vectorString}]) AS SimilarityScore FROM c";
            query += $" WHERE c.tenantId = '{tenantId}' AND VectorDistance(c.embedding, [{vectorString}]) > {minimum_similarity_score.ToString(CultureInfo.InvariantCulture)}";
            query += $" ORDER BY VectorDistance(c.embedding, [{vectorString}])";

            var options = new QueryRequestOptions { MaxItemCount = maxResults };
            
            var results = new List<VectorSearchResults>();
            var iterator = _equipmentContainer.GetItemQueryIterator<VectorSearchResults>(query, requestOptions: options);
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error performing equipment vector search: {ex.Message}", ex);
        }
    }
}
