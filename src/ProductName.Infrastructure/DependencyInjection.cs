using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductName.AppCore.Authentication;
using ProductName.Infrastructure.Authentication;

namespace ProductName.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddProductNameInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var apiOptions = new ApiOptions
        {
            BaseUrl = configuration["Api:BaseUrl"] ?? "",
            SignInPath = configuration["Api:SignInPath"] ?? "/signin",
            CurrentAccountPath = configuration["Api:CurrentAccountPath"] ?? "/me",
            SignOutPath = configuration["Api:SignOutPath"] ?? ""
        };

        if (!Uri.TryCreate(apiOptions.BaseUrl, UriKind.Absolute, out var baseAddress) ||
            (baseAddress.Scheme != Uri.UriSchemeHttps && baseAddress.Host != "localhost"))
        {
            throw new InvalidOperationException(
                "Configure Api:BaseUrl as an HTTPS URL (HTTP is allowed only for localhost).");
        }

        services.AddSingleton(apiOptions);
        services.AddSingleton<ITokenStore, DpapiTokenStore>();
        services.AddHttpClient<IAuthenticationApi, AuthenticationApi>(client =>
        {
            client.BaseAddress = baseAddress;
            client.Timeout = TimeSpan.FromSeconds(20);
        });
        services.AddTransient<AuthenticatedSessionHandler>();
        services.AddHttpClient("AuthenticatedApi", client =>
        {
            client.BaseAddress = baseAddress;
            client.Timeout = TimeSpan.FromSeconds(20);
        }).AddHttpMessageHandler<AuthenticatedSessionHandler>();

        return services;
    }
}
