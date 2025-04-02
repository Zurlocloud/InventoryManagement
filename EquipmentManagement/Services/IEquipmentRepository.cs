using EquipmentManagement.Models;

namespace EquipmentManagement.Services;

public interface IEquipmentRepository
{
    Task<Equipment> GetEquipmentAsync(string tenantId, string id);
    Task<IEnumerable<Equipment>> GetAllEquipmentAsync(string tenantId);
    Task<Equipment> AddEquipmentAsync(Equipment equipment);
    Task<Equipment> UpdateEquipmentAsync(string tenantId, string id, Equipment equipment);
    Task DeleteEquipmentAsync(string tenantId, string id);
}