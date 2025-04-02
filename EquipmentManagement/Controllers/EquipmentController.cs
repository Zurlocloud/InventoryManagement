using Microsoft.AspNetCore.Mvc;
using EquipmentManagement.Models;
using EquipmentManagement.Services;
using EquipmentManagement.Agents;

namespace EquipmentManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EquipmentController : ControllerBase
{
    private readonly IEquipmentRepository _repository;
    private readonly IVectorizationService _vectorizationService;
    private readonly ILogger<EquipmentController> _logger;
    private readonly EquipmentCopilot _equipmentCopilot;

    public EquipmentController(
        IEquipmentRepository repository, 
        IVectorizationService vectorizationService,
        ILogger<EquipmentController> logger,
        EquipmentCopilot equipmentCopilot)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _vectorizationService = vectorizationService ?? throw new ArgumentNullException(nameof(vectorizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _equipmentCopilot = equipmentCopilot ?? throw new ArgumentNullException(nameof(equipmentCopilot));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Equipment>>> GetAllEquipment([FromHeader(Name = "X-Tenant-ID")] string tenantId)
    {
        try
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }
            var equipment = await _repository.GetAllEquipmentAsync(tenantId);
            return Ok(equipment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all equipment for tenant {TenantId}", tenantId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Equipment>> GetEquipment([FromHeader(Name = "X-Tenant-ID")] string tenantId, string id)
    {
        try
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }
            var equipment = await _repository.GetEquipmentAsync(tenantId, id);
            return Ok(equipment);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving equipment with ID: {Id} for tenant {TenantId}", id, tenantId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<ActionResult<Equipment>> CreateEquipment([FromHeader(Name = "X-Tenant-ID")] string tenantId, Equipment equipment)
    {
        try
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }
            equipment.TenantId = tenantId;
            var created = await _repository.AddEquipmentAsync(equipment);
            return CreatedAtAction(nameof(GetEquipment), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating equipment for tenant {TenantId}", tenantId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEquipment([FromHeader(Name = "X-Tenant-ID")] string tenantId, string id, Equipment equipment)
    {
        try
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }
            if (id != equipment.Id)
            {
                return BadRequest("ID mismatch");
            }
            if (tenantId != equipment.TenantId)
            {
                return BadRequest("Tenant ID mismatch");
            }

            await _repository.UpdateEquipmentAsync(tenantId, id, equipment);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating equipment with ID: {Id} for tenant {TenantId}", id, tenantId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEquipment([FromHeader(Name = "X-Tenant-ID")] string tenantId, string id)
    {
        try
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }
            await _repository.DeleteEquipmentAsync(tenantId, id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting equipment with ID: {Id} for tenant {TenantId}", id, tenantId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<Equipment>>> SearchEquipment([FromHeader(Name = "X-Tenant-ID")] string tenantId, [FromQuery] string query)
    {
        try
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant ID is required");
            }
            var results = await _vectorizationService.VectorSearch(tenantId, query);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching equipment with query: {Query} for tenant {TenantId}", query, tenantId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrEmpty(request.Message))
        {
            return BadRequest("Message cannot be empty");
        }

        if (string.IsNullOrEmpty(request.TenantId))
        {
            return BadRequest("TenantId cannot be empty");
        }

        try
        {
            _logger.LogInformation("Received chat request from tenant {TenantId}", request.TenantId);
            
            var response = await _equipmentCopilot.ProcessMessageAsync(request.Message, request.TenantId);
            
            return Ok(new ChatResponse
            {
                Message = response,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            return StatusCode(500, new ChatResponse
            {
                Message = "An error occurred while processing your request.",
                Success = false
            });
        }
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}

public class ChatResponse
{
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; }
}