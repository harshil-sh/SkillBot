// SkillBot.Infrastructure/Plugins/DynamicPluginProvider.cs
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SkillBot.Core.Exceptions;
using SkillBot.Core.Interfaces;
using SkillBot.Core.Models;

namespace SkillBot.Infrastructure.Plugins;

/// <summary>
/// Manages dynamic plugin registration and discovery.
/// Uses reflection to find and register plugin classes.
/// </summary>
public class DynamicPluginProvider : IPluginProvider
{
    private readonly Kernel _kernel;
    private readonly ILogger<DynamicPluginProvider> _logger;
    private readonly ConcurrentDictionary<string, PluginMetadata> _plugins;
    private readonly ConcurrentDictionary<string, object> _instances;

    public DynamicPluginProvider(
        Kernel kernel,
        ILogger<DynamicPluginProvider> logger)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _plugins = new ConcurrentDictionary<string, PluginMetadata>();
        _instances = new ConcurrentDictionary<string, object>();
    }

    public async Task RegisterPluginsFromAssemblyAsync(string assemblyPath)
    {
        try
        {
            _logger.LogInformation("Loading plugins from assembly: {Path}", assemblyPath);

            var assembly = Assembly.LoadFrom(assemblyPath);
            var pluginTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.GetCustomAttribute<PluginAttribute>() != null);

            foreach (var type in pluginTypes)
            {
                try
                {
                    var instance = Activator.CreateInstance(type);
                    if (instance != null)
                    {
                        RegisterPluginInternal(instance, type);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to instantiate plugin type: {TypeName}",
                        type.FullName);
                }
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new PluginException(
                $"Failed to load plugins from assembly: {assemblyPath}",
                ex);
        }
    }

    public void RegisterPlugin<TPlugin>(TPlugin instance) where TPlugin : class
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        RegisterPluginInternal(instance, typeof(TPlugin));
    }

    private void RegisterPluginInternal(object instance, Type type)
    {
        var pluginAttr = type.GetCustomAttribute<PluginAttribute>();
        var pluginName = pluginAttr?.Name ?? type.Name;

        _logger.LogInformation("Registering plugin: {PluginName}", pluginName);

        // Extract metadata
        var metadata = new PluginMetadata
        {
            Name = pluginName,
            Description = pluginAttr?.Description ?? string.Empty,
            PluginType = type,
            Functions = ExtractFunctionMetadata(type)
        };

        // Register with Semantic Kernel
        _kernel.ImportPluginFromObject(instance, pluginName);

        // Store metadata and instance
        _plugins[pluginName] = metadata;
        _instances[pluginName] = instance;

        _logger.LogInformation(
            "Registered plugin '{PluginName}' with {FunctionCount} functions",
            pluginName,
            metadata.Functions.Count);
    }

    private List<FunctionMetadata> ExtractFunctionMetadata(Type type)
    {
        var functions = new List<FunctionMetadata>();

        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<KernelFunctionAttribute>() != null);

        foreach (var method in methods)
        {
            var funcAttr = method.GetCustomAttribute<KernelFunctionAttribute>();
            var descAttr = method.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
            var funcMetadata = new FunctionMetadata
            {
                Name = funcAttr?.Name ?? method.Name,
                Description = descAttr?.Description ?? string.Empty,
                Parameters = ExtractParameterMetadata(method)
            };

            functions.Add(funcMetadata);
        }

        return functions;
    }

    private List<ParameterMetadata> ExtractParameterMetadata(MethodInfo method)
    {
        var parameters = new List<ParameterMetadata>();

        foreach (var param in method.GetParameters())
        {
            var descAttr = param.GetCustomAttribute<DescriptionAttribute>();
            
            parameters.Add(new ParameterMetadata
            {
                Name = param.Name ?? "unknown",
                Type = param.ParameterType,
                Description = descAttr?.Description,
                IsRequired = !param.IsOptional,
                DefaultValue = param.DefaultValue
            });
        }

        return parameters;
    }

    public IReadOnlyList<PluginMetadata> GetRegisteredPlugins()
    {
        return _plugins.Values.ToList();
    }

    public object? GetPlugin(string pluginName)
    {
        return _instances.TryGetValue(pluginName, out var instance) ? instance : null;
    }

    public bool UnregisterPlugin(string pluginName)
    {
        _logger.LogInformation("Unregistering plugin: {PluginName}", pluginName);

        var removed = _plugins.TryRemove(pluginName, out _);
        _instances.TryRemove(pluginName, out _);

        return removed;
    }
}

/// <summary>
/// Attribute to mark a class as a plugin.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class PluginAttribute : Attribute
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}


