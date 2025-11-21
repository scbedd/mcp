// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Azure.Mcp.Core.Areas.Server.Options;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Core.Services.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Azure.Mcp.Core.UnitTests.Services.Http;

public class HttpClientServiceTests
{
    [Fact]
    public void Constructor_WithNullFactory_ThrowsArgumentNullException()
    {
        var options = Options.Create(new HttpClientOptions());
        Assert.Throws<ArgumentNullException>(() => new HttpClientService(null!, options));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        Assert.Throws<ArgumentNullException>(() => new HttpClientService(factory, null!));
    }

    [Fact]
    public void DefaultClient_ReturnsConfiguredHttpClient()
    {
        using var provider = CreateServiceProvider(options => options.DefaultTimeout = TimeSpan.FromSeconds(30));
        var service = provider.GetRequiredService<IHttpClientService>();

        var client = service.DefaultClient;

        Assert.NotNull(client);
        Assert.Equal(TimeSpan.FromSeconds(30), client.Timeout);
    }

    [Fact]
    public void CreateClient_WithBaseAddress_ReturnsClientWithBaseAddress()
    {
        using var provider = CreateServiceProvider();
        var service = provider.GetRequiredService<IHttpClientService>();
        var baseAddress = new Uri("https://example.com");

        using var client = service.CreateClient(baseAddress);

        Assert.NotNull(client);
        Assert.Equal(baseAddress, client.BaseAddress);
    }

    [Fact]
    public void CreateClient_WithConfigureAction_AppliesConfiguration()
    {
        using var provider = CreateServiceProvider();
        var service = provider.GetRequiredService<IHttpClientService>();
        var baseAddress = new Uri("https://example.com");

        using var client = service.CreateClient(baseAddress, c => c.DefaultRequestHeaders.Add("Custom-Header", "CustomValue"));

        Assert.NotNull(client);
        Assert.Equal(baseAddress, client.BaseAddress);
        Assert.True(client.DefaultRequestHeaders.Contains("Custom-Header"));
    }

    [Fact]
    public void CreateClient_WithProxyConfiguration_CreatesProxyEnabledClient()
    {
        using var provider = CreateServiceProvider(options =>
        {
            options.AllProxy = "http://proxy.example.com:8080";
            options.NoProxy = "localhost,127.0.0.1";
        });

        var service = provider.GetRequiredService<IHttpClientService>();

        using var client = service.CreateClient();

        Assert.NotNull(client);
    }

    [Fact]
    public void Dispose_DisposesDefaultClient()
    {
        using var provider = CreateServiceProvider();
        var service = (HttpClientService)provider.GetRequiredService<IHttpClientService>();
        _ = service.DefaultClient;

        service.Dispose();

        Assert.Throws<ObjectDisposedException>(() => service.CreateClient());
    }

    [Fact]
    public void UserAgent_IsSetCorrectly()
    {
        var serviceStartOptions = new ServiceStartOptions
        {
            Transport = TransportTypes.Http
        };

        using var provider = CreateServiceProvider(
            configure: null,
            serviceStartOptions: serviceStartOptions);

        var service = provider.GetRequiredService<IHttpClientService>();
        var userAgent = service.DefaultClient.DefaultRequestHeaders.UserAgent.ToString();

        Assert.Contains("azmcp-http/", userAgent);
    }

    [Fact]
    public void UserAgent_IgnoresHttpClientOptionsDefaultUserAgent()
    {
        var serviceStartOptions = new ServiceStartOptions
        {
            Transport = TransportTypes.Http
        };

        using var provider = CreateServiceProvider(
            options => options.DefaultUserAgent = "CustomAgent/1.0",
            serviceStartOptions: serviceStartOptions);

        var service = provider.GetRequiredService<IHttpClientService>();
        var userAgent = service.DefaultClient.DefaultRequestHeaders.UserAgent.ToString();

        Assert.Contains("azmcp-http/", userAgent);
        Assert.DoesNotContain("CustomAgent/1.0", userAgent);
    }

    private static ServiceProvider CreateServiceProvider(
        Action<HttpClientOptions>? configure = null,
        ServiceStartOptions? serviceStartOptions = null)
    {
        var services = new ServiceCollection();
        services.AddHttpClientServices(options => configure?.Invoke(options));

        if (serviceStartOptions != null)
        {
            services.AddSingleton<IOptions<ServiceStartOptions>>(Options.Create(serviceStartOptions));
        }

        return services.BuildServiceProvider();
    }
}
