using Microsoft.SemanticKernel;
using System.ComponentModel;
using EquipmentManagement.Models;
using EquipmentManagement.Services;
using Microsoft.Extensions.Logging;

namespace EquipmentManagement.Plugins;

public class EquipmentRequestPlugin
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly ILogger<EquipmentRequestPlugin> _logger;

    public EquipmentRequestPlugin(IEquipmentRepository equipmentRepository, ILogger<EquipmentRequestPlugin> logger)
    {
        _equipmentRepository = equipmentRepository ?? throw new ArgumentNullException(nameof(equipmentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [KernelFunction("create_equipment")]
    [Description("Creates a new equipment entry in the database")]
    public async Task<Equipment> CreateEquipmentAsync(
        [Description("The tenant ID for the equipment")] string tenantId,
        [Description("The name of the equipment")] string name,
        [Description("The description of the equipment")] string? description = null,
        [Description("The category of the equipment")] string? category = null,
        [Description("The status of the equipment")] string? status = null,
        [Description("The attributes of the equipment as JSON string of key-value pairs")] string? attributesJson = null,
        [Description("The last maintenance date in format YYYY-MM-DDTHH:MM:SS.SSSZ")] string? lastMaintenanceDate = null,
        [Description("The purchase date in format YYYY-MM-DDTHH:MM:SS.SSSZ")] string? purchaseDate = null,
        [Description("The serial number of the equipment")] string? serialNumber = null)
    {
        try
        {
            _logger.LogInformation("Creating new equipment {Name} for tenant {TenantId}", name, tenantId);

            var equipment = new Equipment
            {
                TenantId = tenantId,
                Name = name,
                Description = description ?? string.Empty,
                Category = category ?? string.Empty,
                Status = status ?? "Available",
                SerialNumber = serialNumber
            };

            // Parse attributes if provided
            if (!string.IsNullOrEmpty(attributesJson))
            {
                try
                {
                    var attributes = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(attributesJson);
                    if (attributes != null)
                    {
                        equipment.Attributes = attributes;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse attributes JSON, using empty dictionary");
                    equipment.Attributes = new Dictionary<string, string>();
                }
            }

            // Parse dates if provided
            if (!string.IsNullOrEmpty(lastMaintenanceDate))
            {
                if (DateTime.TryParse(lastMaintenanceDate, out var parsedLastMaintenanceDate))
                {
                    equipment.LastMaintenanceDate = parsedLastMaintenanceDate;
                }
                else
                {
                    _logger.LogWarning("Invalid format for lastMaintenanceDate: {LastMaintenanceDate}", lastMaintenanceDate);
                }
            }

            if (!string.IsNullOrEmpty(purchaseDate))
            {
                if (DateTime.TryParse(purchaseDate, out var parsedPurchaseDate))
                {
                    equipment.PurchaseDate = parsedPurchaseDate;
                }
                else
                {
                    _logger.LogWarning("Invalid format for purchaseDate: {PurchaseDate}", purchaseDate);
                    // Default to today if not specified or invalid
                    equipment.PurchaseDate = DateTime.UtcNow;
                }
            }
            else
            {
                // Default to today if not specified
                equipment.PurchaseDate = DateTime.UtcNow;
            }

            // Save to database
            var createdEquipment = await _equipmentRepository.AddEquipmentAsync(equipment);
            _logger.LogInformation("Successfully created equipment with ID {Id}", createdEquipment.Id);
            
            return createdEquipment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating equipment {Name} for tenant {TenantId}", name, tenantId);
            throw new Exception($"Failed to create equipment: {ex.Message}", ex);
        }
    }

 
}