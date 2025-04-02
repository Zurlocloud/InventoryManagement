using Newtonsoft.Json;

namespace EquipmentManagement.Models;

public class VectorSearchResults
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonProperty("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [JsonProperty("similarityScore")]
    public required float SimilarityScore { get; set; }
}