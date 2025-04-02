using Microsoft.Azure.Cosmos;
using EquipmentManagement.Models;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace EquipmentManagement.Services;

public class CosmosDbEquipmentRepository : IEquipmentRepository
{
    private readonly Microsoft.Azure.Cosmos.Container _container;
    private readonly ILogger<CosmosDbEquipmentRepository> _logger;
    private readonly IVectorizationService _vectorizationService;

    public CosmosDbEquipmentRepository(
        CosmosClient cosmosClient,
        ILogger<CosmosDbEquipmentRepository> logger,
        IVectorizationService vectorizationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _vectorizationService = vectorizationService ?? throw new ArgumentNullException(nameof(vectorizationService));
        
        var databaseId = Environment.GetEnvironmentVariable("CosmosDB__DatabaseId") ?? "InventoryRentalDB";
        var containerId = Environment.GetEnvironmentVariable("CosmosDB__ContainerId") ?? "equipment";
            
        _container = cosmosClient.GetContainer(databaseId, containerId);
    }

    [KernelFunction("GetEquipment")]
    [Description("Retrieves a specific piece of equipment by its ID")]
    public async Task<Equipment> GetEquipmentAsync(string tenantId, string id)
    {
        try
        {
            var response = await _container.ReadItemAsync<Equipment>(id, new PartitionKey(tenantId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Equipment with ID {Id} not found for tenant {TenantId}", id, tenantId);
            throw new KeyNotFoundException($"Equipment with ID {id} not found");
        }
    }

    [KernelFunction("GetAllEquipment")]
    [Description("Retrieves all equipment for a specific tenant")]
    public async Task<IEnumerable<Equipment>> GetAllEquipmentAsync(string tenantId)
    {
        var queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.tenantId = @tenantId")
            .WithParameter("@tenantId", tenantId);
        var query = _container.GetItemQueryIterator<Equipment>(queryDefinition);
        
        var results = new List<Equipment>();
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response.ToList());
        }
        return results;
    }

    [KernelFunction("AddEquipment")]
    [Description("Adds a new piece of equipment to the inventory")]
    public async Task<Equipment> AddEquipmentAsync(Equipment equipment)
    {
        equipment.Id = Guid.NewGuid().ToString();
        
        
        return await _container.CreateItemAsync(equipment, new PartitionKey(equipment.TenantId));
    }

    [KernelFunction("UpdateEquipment")]
    [Description("Updates an existing piece of equipment in the inventory")]
    public async Task<Equipment> UpdateEquipmentAsync(string tenantId, string id, Equipment equipment)
    {
        equipment.Id = id;
        equipment.TenantId = tenantId;
        
        return await _container.UpsertItemAsync(equipment, new PartitionKey(tenantId));
    }

    [KernelFunction("DeleteEquipment")]
    [Description("Deletes a piece of equipment from the inventory")]
    public async Task DeleteEquipmentAsync(string tenantId, string id)
    {
        await _container.DeleteItemAsync<Equipment>(id, new PartitionKey(tenantId));
    }


}