using ConfigStream.Core.Interfaces;
using ConfigStream.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ConfigStream.Mvc.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly IConfigurationStorage _storage;
    private readonly IConfigurationReader _reader;

    public ConfigurationController(IConfigurationStorage storage, IConfigurationReader reader)
    {
        _storage = storage;
        _reader = reader;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ConfigurationItem>>> GetAll(
        [FromQuery] string applicationName = "ConfigurationLibrary.Mvc.Web")
    {
        try
        {
            var records = await _storage.GetAllAsync(applicationName);
            return Ok(records);
        }
        catch (Exception)
        {
            // Storage failed, try to get from file cache via reader
            // TODO: Implement GetAll method
            return Ok(new List<ConfigurationItem>());
        }
    }

    [HttpGet("{applicationName}/{name}")]
    public async Task<ActionResult<ConfigurationItem>> GetByName(string applicationName, string name)
    {
        try
        {
            var record = await _storage.GetAsync(applicationName, name);

            if (record == null)
                return NotFound();

            return Ok(record);
        }
        catch (Exception)
        {
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
            catch (Exception)
            {
                // File cache also failed
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
            return BadRequest("Failed to create configuration");

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
            return NotFound();

        var updatedItem = await _storage.SetAsync(item);

        if (updatedItem == null)
            return BadRequest("Failed to update configuration");

        return NoContent();
    }

    [HttpDelete("{applicationName}/{name}")]
    public async Task<IActionResult> Delete(string applicationName, string name)
    {
        var record = await _storage.GetAsync(applicationName, name);
        if (record == null)
            return NotFound();

        var success = await _storage.DeleteAsync(applicationName, name);

        if (!success)
            return BadRequest("Failed to delete configuration");

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
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("applications")]
    public async Task<ActionResult<IEnumerable<string>>> GetApplications()
    {
        // TODO: Implement this method
        var applications = new[] { "ConfigurationLibrary.Mvc.Web", "SERVICE-A", "SERVICE-B" };
        return Ok(applications);
    }
}
