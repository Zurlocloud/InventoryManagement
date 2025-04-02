using Newtonsoft.Json;

namespace EquipmentManagement.Models;

public class Equipment
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonProperty("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [JsonProperty("category")]
    public string Category { get; set; } = string.Empty;

    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("attributes")]
    public Dictionary<string, string> Attributes { get; set; } = new();

    [JsonProperty("lastMaintenanceDate")]
    public DateTime? LastMaintenanceDate { get; set; }

    [JsonProperty("purchaseDate")]
    public DateTime PurchaseDate { get; set; }

    [JsonProperty("serialNumber")]
    public string? SerialNumber { get; set; }

    [JsonProperty("embedding")]
    public float[]? Embedding { get; set; }


}