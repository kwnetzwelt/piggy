using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Piggy.Tests;

public class AuthTests
{
    [Fact]
    public async Task AllRoutes_RequireAuthentication()
    {
        var client = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Oidc:Authority", "https://localhost:9999");
            builder.UseSetting("Oidc:ClientId", "test");
            builder.UseSetting("Oidc:ClientSecret", "test");
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestChallengeHandler>("Test", null);
                services.AddAuthorization(options =>
                {
                    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
                });
            });
        }).CreateClient();

        var routes = new[]
        {
            "/",
            "/Transactions/Index",
            "/Transactions/Tag",
            "/Import/Index",
            "/Settings/Index",
            "/Accounts/Index",
            "/Splits/Index",
            "/logout"
        };

        foreach (var route in routes)
        {
            var response = await client.GetAsync(route);
            Assert.Equal(302, (int)response.StatusCode);
        }
    }

    [Fact]
    public async Task AuthenticatedRequest_Succeeds()
    {
        var client = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Oidc:Authority", "https://localhost:9999");
            builder.UseSetting("Oidc:ClientId", "test");
            builder.UseSetting("Oidc:ClientSecret", "test");
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
            });
        }).CreateClient();

        var routes = new[]
        {
            "/",
            "/Transactions/Index",
            "/Transactions/Tag",
            "/Import/Index",
            "/Settings/Index",
            "/Accounts/Index",
            "/Splits/Index"
        };

        foreach (var route in routes)
        {
            var response = await client.GetAsync(route);
            Assert.Equal(200, (int)response.StatusCode);
        }
    }
}

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim(ClaimTypes.Name, "test") };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class TestChallengeHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestChallengeHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        return Task.FromResult(AuthenticateResult.NoResult());
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Redirect("/Account/Login");
        return Task.CompletedTask;
    }
}
