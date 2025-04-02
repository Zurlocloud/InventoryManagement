using EquipmentManagement.Models;

namespace EquipmentManagement.Services;
public interface IVectorizationService
{
    Task<float[]> GetEmbeddings(string text);
    Task<List<VectorSearchResults>> VectorSearch(string tenantId, string searchText, int maxResults = 10, double minimum_similarity_score = 0.8);
}

