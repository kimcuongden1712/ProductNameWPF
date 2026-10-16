using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductName.AppCore.Authentication;
using ProductName.AppCore.Authentication.Interface;
using ProductName.Infrastructure.Authentication;
using ProductName.Infrastructure.Http;

namespace ProductName.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddProductNameInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var apiOptions = new ApiOptions();

            if (!Uri.TryCreate(apiOptions.BaseUrl, UriKind.Absolute, out var baseAddress) ||
                (baseAddress.Scheme != Uri.UriSchemeHttps && baseAddress.Host != "localhost"))
            {
                throw new InvalidOperationException(
                    "Configure Api:BaseUrl as an HTTPS URL (HTTP is allowed only for localhost).");
            }

            services.AddSingleton(apiOptions);
            services.AddSingleton<ITokenStore, DpapiTokenStore>();
            services.AddSingleton<IAuthenticationSessionState, AuthenticationSessionState>();
            services.AddHttpClient<ExecuteHttpClient>(client =>
            {
                client.BaseAddress = baseAddress;
                client.Timeout = TimeSpan.FromSeconds(20);
            });
            services.AddTransient<IAuthenticationApi, AuthenticationApi>();
            return services;
        }
    }
}
