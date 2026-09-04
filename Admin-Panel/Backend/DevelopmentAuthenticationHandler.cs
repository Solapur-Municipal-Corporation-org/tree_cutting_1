using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AdminPanel.Api;

public sealed class DevelopmentAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var employeeId = Request.Cookies["smc_admin_test_employee"];
        var role = Request.Cookies["smc_admin_test_role"];
        if (string.IsNullOrWhiteSpace(employeeId) || string.IsNullOrWhiteSpace(role)) return Task.FromResult(AuthenticateResult.NoResult());
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, employeeId), new Claim("employee_id", employeeId), new Claim(ClaimTypes.Role, role), new Claim("name", $"Test {role}") };
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
    }
}