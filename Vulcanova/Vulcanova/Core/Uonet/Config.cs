using Prism.Ioc;
using Vulcanova.Features.Auth;
using Vulcanova.Features.Shared;
using Vulcanova.Uonet.Api.Common;
using Vulcanova.Uonet.Signing;

namespace Vulcanova.Core.Uonet;

public static class Config
{
    public static void RegisterUonet(this IContainerRegistry container)
    {
        container.RegisterSingleton<IApiClientFactory, ApiClientFactory>();
        container.RegisterSingleton<IInstanceUrlProvider, InstanceUrlProvider>();
        container.RegisterSingleton<AccountContext>();

        container.RegisterScoped<IPeriodService, PeriodService>();
    }
}