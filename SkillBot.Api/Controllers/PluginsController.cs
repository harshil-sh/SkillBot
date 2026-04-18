using Microsoft.AspNetCore.Mvc;
using SkillBot.Api.Models.Responses;
using SkillBot.Core.Interfaces;

namespace SkillBot.Api.Controllers;

/// <summary>
/// Plugin information endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PluginsController : ControllerBase
{
    private readonly IPluginProvider _pluginProvider;
    private readonly ILogger<PluginsController> _logger;

    public PluginsController(
        IPluginProvider pluginProvider,
        ILogger<PluginsController> logger)
    {
        _pluginProvider = pluginProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get list of all registered plugins
    /// </summary>
    /// <returns>List of available plugins</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<PluginInfoResponse>), StatusCodes.Status200OK)]
    public ActionResult<List<PluginInfoResponse>> GetPlugins()
    {
        try
        {
            var plugins = _pluginProvider.GetRegisteredPlugins();
            
            var response = plugins.Select(p => new PluginInfoResponse
            {
                Name = p.Name,
                Description = p.Description,
                Functions = p.Functions.Select(f => new FunctionInfo
                {
                    Name = f.Name,
                    Description = f.Description,
                    Parameters = f.Parameters.Select(param => new ParameterInfo
                    {
                        Name = param.Name,
                        Type = param.Type.Name,
                        Description = param.Description,
                        IsRequired = param.IsRequired
                    }).ToList()
                }).ToList()
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving plugins");
            return StatusCode(500, new ErrorResponse
            {
                Error = "InternalError",
                Message = "Failed to retrieve plugins"
            });
        }
    }

    /// <summary>
    /// Get specific plugin information
    /// </summary>
    /// <param name="pluginName">Name of the plugin</param>
    /// <returns>Plugin details</returns>
    [HttpGet("{pluginName}")]
    [ProducesResponseType(typeof(PluginInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<PluginInfoResponse> GetPlugin(string pluginName)
    {
        try
        {
            var plugin = _pluginProvider.GetRegisteredPlugins()
                .FirstOrDefault(p => p.Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase));

            if (plugin == null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = "NotFound",
                    Message = $"Plugin '{pluginName}' not found"
                });
            }

            var response = new PluginInfoResponse
            {
                Name = plugin.Name,
                Description = plugin.Description,
                Functions = plugin.Functions.Select(f => new FunctionInfo
                {
                    Name = f.Name,
                    Description = f.Description,
                    Parameters = f.Parameters.Select(param => new ParameterInfo
                    {
                        Name = param.Name,
                        Type = param.Type.Name,
                        Description = param.Description,
                        IsRequired = param.IsRequired
                    }).ToList()
                }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving plugin {PluginName}", pluginName);
            return StatusCode(500, new ErrorResponse
            {
                Error = "InternalError",
                Message = "Failed to retrieve plugin"
            });
        }
    }
}
