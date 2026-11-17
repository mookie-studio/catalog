using System.Threading.RateLimiting;
using Catalog.Api.Features.Public;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Catalog.Api.Infrastructure;

/// <summary>Fixed-window limit per client IP for Inquiries, bound from "RateLimits:Inquiries".</summary>
public sealed class InquiryRateLimitOptions
{
    public const string SectionName = "RateLimits:Inquiries";

    public int PermitLimit { get; set; } = 5;
    public int WindowSeconds { get; set; } = 600;
}

public static class RateLimitingServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        // The API runs behind the host's reverse proxy, whose addresses are not fixed: trust X-Forwarded-*
        // so RemoteIpAddress is the client's IP. Only the first (client-most) hop is used.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        var inquiries = configuration.GetSection(InquiryRateLimitOptions.SectionName).Get<InquiryRateLimitOptions>()
            ?? new InquiryRateLimitOptions();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                }

                await TypedResults.Problem(new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests",
                    Detail = "Too many messages were sent from your address. Try again later.",
                }).ExecuteAsync(context.HttpContext);
            };

            options.AddPolicy(CreateInquiry.RateLimitPolicy, http => RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = inquiries.PermitLimit,
                    Window = TimeSpan.FromSeconds(inquiries.WindowSeconds),
                    QueueLimit = 0,
                }));
        });

        return services;
    }
}
