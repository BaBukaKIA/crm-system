using System.Reflection;
using System.Runtime.Loader;

namespace EnterpriseAutomation.Services.Plugins;

public static class PluginLoader
{
    public static IReadOnlyList<IEnterprisePlugin> Load(
        string applicationRoot,
        IServiceCollection services,
        IConfiguration configuration,
        ILogger? logger = null)
    {
        var pluginTypes = new List<Type>();

        pluginTypes.AddRange(Assembly.GetExecutingAssembly()
            .ExportedTypes
            .Where(type => typeof(IEnterprisePlugin).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface));

        var pluginDirectory = Path.Combine(applicationRoot, "Plugins");
        if (Directory.Exists(pluginDirectory))
        {
            foreach (var assemblyPath in Directory.GetFiles(pluginDirectory, "*.dll"))
            {
                try
                {
                    var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(assemblyPath));
                    pluginTypes.AddRange(assembly.ExportedTypes.Where(type =>
                        typeof(IEnterprisePlugin).IsAssignableFrom(type) &&
                        !type.IsAbstract &&
                        !type.IsInterface));
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Failed to load plugin assembly {AssemblyPath}", assemblyPath);
                }
            }
        }

        var plugins = new List<IEnterprisePlugin>();
        foreach (var type in pluginTypes.Distinct())
        {
            if (Activator.CreateInstance(type) is not IEnterprisePlugin plugin)
            {
                continue;
            }

            plugin.ConfigureServices(services, configuration);
            plugins.Add(plugin);
        }

        return plugins;
    }
}
