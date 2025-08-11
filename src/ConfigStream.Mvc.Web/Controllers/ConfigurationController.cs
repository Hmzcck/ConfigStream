using ConfigStream.Core.Interfaces;
using ConfigStream.Core.Logging;
using ConfigStream.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ConfigStream.Mvc.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigurationController : ControllerBase
{
    private static readonly ILogger<ConfigurationController> _logger = Logging.CreateLogger<ConfigurationController>();
    private readonly IConfigurationStorage _storage;
    private readonly IConfigurationReader _reader;

    public ConfigurationController(IConfigurationStorage storage, IConfigurationReader reader)
    {
        _storage = storage;
        _reader = reader;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ConfigurationItem>>> GetAll(
        [FromQuery] string? applicationName = null)
    {
        try
        {
            if (string.IsNullOrEmpty(applicationName))
            {
                // Get ALL configurations from ALL applications
                var allRecords = await _storage.GetAllConfigurationsAsync();
                return Ok(allRecords);
            }
            else
            {
                // Get configurations for specific application
                var records = await _storage.GetAllAsync(applicationName);
                return Ok(records);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Storage failed for GetAll, application '{ApplicationName}'",
                applicationName ?? "ALL");
            return StatusCode(503,
                new
                {
                    error = "Configuration storage unavailable",
                    message = "Unable to retrieve configurations at this time"
                });
        }
    }

    [HttpGet("{applicationName}/{name}")]
    public async Task<ActionResult<ConfigurationItem>> GetByName(string applicationName, string name)
    {
        try
        {
            var record = await _storage.GetAsync(applicationName, name);

            if (record == null)
            {
                return NotFound();
            }

            return Ok(record);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Storage failed for configuration '{Name}' in application '{ApplicationName}', trying fallback", name,
                applicationName);
            // Storage failed, try to get from file cache via reader
            // Injected reader might be for a different application
            try
            {
                var value = await _reader.GetValueAsync<string>(name);
                if (value != null)
                {
                    return Ok(new ConfigurationItem
                    {
                        Name = name,
                        ApplicationName = applicationName,
                        Value = value,
                        Type = ConfigurationType.String,
                        IsActive = 1
                    });
                }
            }
            catch (Exception readerEx)
            {
                _logger.LogWarning(readerEx, "Fallback reader also failed for configuration '{Name}'", name);
            }

            return NotFound();
        }
    }

    [HttpPost]
    public async Task<ActionResult<ConfigurationItem>> Create([FromBody] ConfigurationItem item)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);


        var savedItem = await _storage.SetAsync(item);

        if (savedItem == null)
        {
            _logger.LogWarning("Failed to create configuration '{Name}' for application '{ApplicationName}'", item.Name,
                item.ApplicationName);
            return BadRequest("Failed to create configuration");
        }

        _logger.LogInformation("Created configuration '{Name}' for application '{ApplicationName}'", item.Name,
            item.ApplicationName);
        return CreatedAtAction(nameof(GetByName),
            new { applicationName = savedItem.ApplicationName, name = savedItem.Name }, savedItem);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] ConfigurationItem item)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);


        var existingRecord = await _storage.GetAsync(item.ApplicationName, item.Name);
        if (existingRecord == null)
        {
            _logger.LogWarning(
                "Attempted to update non-existent configuration '{Name}' for application '{ApplicationName}'",
                item.Name, item.ApplicationName);
            return NotFound();
        }

        var updatedItem = await _storage.SetAsync(item);

        if (updatedItem == null)
        {
            _logger.LogWarning("Failed to update configuration '{Name}' for application '{ApplicationName}'", item.Name,
                item.ApplicationName);
            return BadRequest("Failed to update configuration");
        }

        _logger.LogInformation("Updated configuration '{Name}' for application '{ApplicationName}'", item.Name,
            item.ApplicationName);
        return NoContent();
    }

    [HttpDelete("{applicationName}/{name}")]
    public async Task<IActionResult> Delete(string applicationName, string name)
    {
        var record = await _storage.GetAsync(applicationName, name);
        if (record == null)
        {
            _logger.LogWarning(
                "Attempted to delete non-existent configuration '{Name}' for application '{ApplicationName}'", name,
                applicationName);
            return NotFound();
        }

        var success = await _storage.DeleteAsync(applicationName, name);

        if (!success)
        {
            _logger.LogWarning("Failed to delete configuration '{Name}' for application '{ApplicationName}'", name,
                applicationName);
            return BadRequest("Failed to delete configuration");
        }

        _logger.LogInformation("Deleted configuration '{Name}' for application '{ApplicationName}'", name,
            applicationName);
        return NoContent();
    }

    [HttpGet("test-reader/{key}")]
    public ActionResult<object> TestReader(string key)
    {
        try
        {
            var value = _reader.GetValue<string>(key);

            return Ok(new { key, value, type = "string" });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reader test failed for key '{Key}'", key);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("applications")]
    public async Task<ActionResult<IEnumerable<string>>> GetApplications()
    {
        try
        {
            var applications = await _storage.GetApplicationsAsync();

            _logger.LogInformation(applications.ToString());

            return Ok(applications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve applications list");
            return StatusCode(500, new { error = "Failed to retrieve applications", message = ex.Message });
        }
    }
}
