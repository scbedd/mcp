// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using Azure.Mcp.Core.Areas.Server.Options;
using Microsoft.Extensions.Options;

namespace Azure.Mcp.Core.Services.Http;

/// <summary>
/// Implementation of IHttpClientService that provides configured HttpClient instances with proxy support.
/// </summary>
public sealed class HttpClientService : IHttpClientService, IDisposable
{
    internal const string DefaultClientName = "AzureMcp.Http.Default";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Lazy<HttpClient> _defaultClient;
    private bool _disposed;

    public HttpClientService(
        IHttpClientFactory httpClientFactory,
        IOptions<HttpClientOptions> options,
        IOptions<ServiceStartOptions>? serviceStartOptions = null)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        ArgumentNullException.ThrowIfNull(options);
        _ = serviceStartOptions; // intentionally unused; resolved for compatibility with legacy constructors

        _defaultClient = new Lazy<HttpClient>(() => _httpClientFactory.CreateClient(DefaultClientName));
    }

    /// <inheritdoc />
    public HttpClient DefaultClient => _defaultClient.Value;

    /// <inheritdoc />
    public HttpClient CreateClient(Uri? baseAddress = null)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HttpClientService));
        }

        var client = _httpClientFactory.CreateClient(DefaultClientName);
        if (baseAddress != null)
        {
            client.BaseAddress = baseAddress;
        }
        return client;
    }

    /// <inheritdoc />
    public HttpClient CreateClient(Uri? baseAddress, Action<HttpClient> configureClient)
    {
        ArgumentNullException.ThrowIfNull(configureClient);

        var client = CreateClient(baseAddress);
        configureClient(client);
        return client;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_defaultClient.IsValueCreated)
            {
                _defaultClient.Value.Dispose();
            }
            _disposed = true;
        }
    }
}
