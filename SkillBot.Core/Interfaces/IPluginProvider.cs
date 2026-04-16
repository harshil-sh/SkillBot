// SkillBot.Core/Interfaces/IPluginProvider.cs
using SkillBot.Core.Models;

namespace SkillBot.Core.Interfaces;

/// <summary>
/// Manages plugin/tool discovery, registration, and lifecycle.
/// </summary>
public interface IPluginProvider
{
    /// <summary>
    /// Discover and register all plugins from the specified assembly.
    /// </summary>
    /// <param name="assemblyPath">Path to assembly containing plugins</param>
    Task RegisterPluginsFromAssemblyAsync(string assemblyPath);

    /// <summary>
    /// Register a single plugin instance.
    /// </summary>
    /// <typeparam name="TPlugin">Plugin type</typeparam>
    /// <param name="instance">Plugin instance</param>
    void RegisterPlugin<TPlugin>(TPlugin instance) where TPlugin : class;

    /// <summary>
    /// Get all registered plugin metadata.
    /// </summary>
    IReadOnlyList<PluginMetadata> GetRegisteredPlugins();

    /// <summary>
    /// Get a specific plugin by name.
    /// </summary>
    /// <param name="pluginName">Name of the plugin</param>
    object? GetPlugin(string pluginName);

    /// <summary>
    /// Remove a plugin from the registry.
    /// </summary>
    /// <param name="pluginName">Name of the plugin to unregister</param>
    bool UnregisterPlugin(string pluginName);
}
