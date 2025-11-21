// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Versioning;
using Azure.Mcp.Core.Areas.Server.Options;

namespace Azure.Mcp.Core.Services.Http;

internal static class HttpClientFactoryUtilities
{
    private static readonly string s_version;
    private static readonly string s_framework;
    private static readonly string s_platform;

    static HttpClientFactoryUtilities()
    {
        var assembly = typeof(HttpClientService).Assembly;
        s_version = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "unknown";
        s_framework = assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName ?? "unknown";
        s_platform = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
    }

    public static string BuildUserAgent(string? transport)
    {
        var normalizedTransport = string.IsNullOrWhiteSpace(transport) ? TransportTypes.StdIo : transport;
        return $"azmcp/{s_version} azmcp-{normalizedTransport}/{s_version} ({s_framework}; {s_platform})";
    }

    public static HttpMessageHandler CreatePrimaryHandler(HttpClientOptions options)
    {
        var handler = new HttpClientHandler();
        var proxy = CreateProxy(options);
        if (proxy != null)
        {
            handler.Proxy = proxy;
            handler.UseProxy = true;
        }

        return handler;
    }

    private static WebProxy? CreateProxy(HttpClientOptions options)
    {
        string? proxyAddress = options.AllProxy ?? options.HttpsProxy ?? options.HttpProxy;
        if (string.IsNullOrWhiteSpace(proxyAddress))
        {
            return null;
        }

        if (!Uri.TryCreate(proxyAddress, UriKind.Absolute, out var proxyUri))
        {
            return null;
        }

        var proxy = new WebProxy(proxyUri);

        if (!string.IsNullOrEmpty(options.NoProxy))
        {
            var bypassList = options.NoProxy
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(static s => s.Trim())
                .Where(static s => !string.IsNullOrEmpty(s))
                .Select(ConvertGlobToRegex)
                .ToArray();

            if (bypassList.Length > 0)
            {
                proxy.BypassList = bypassList;
            }
        }

        return proxy;
    }

    private static string ConvertGlobToRegex(string globPattern)
    {
        if (string.IsNullOrEmpty(globPattern))
        {
            return string.Empty;
        }

        var escaped = globPattern
            .Replace("\\", "\\\\")
            .Replace(".", "\\.")
            .Replace("+", "\\+")
            .Replace("$", "\\$")
            .Replace("^", "\\^")
            .Replace("{", "\\{")
            .Replace("}", "\\}")
            .Replace("[", "\\[")
            .Replace("]", "\\]")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("|", "\\|");

        var regex = escaped
            .Replace("*", ".*")
            .Replace("?", ".");

        return $"^{regex}$";
    }
}
