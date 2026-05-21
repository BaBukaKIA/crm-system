using Microsoft.AspNetCore.Routing;

namespace EnterpriseAutomation.Services.Plugins;

public interface IEnterprisePlugin
{
    string Name { get; }

    void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
