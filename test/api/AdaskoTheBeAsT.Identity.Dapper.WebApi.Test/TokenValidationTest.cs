using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Test;

public sealed class TokenValidationTest : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public TokenValidationTest(ApiFactory factory) => _factory = factory;

    [Theory]
    [InlineData("username", "missing")]
    [InlineData("username", "empty")]
    [InlineData("username", "whitespace")]
    [InlineData("username", "oversized")]
    [InlineData("password", "missing")]
    [InlineData("password", "empty")]
    [InlineData("password", "whitespace")]
    [InlineData("password", "oversized")]
    public async Task InvalidPasswordFormsReturnSanitizedBadRequest(string field, string invalidValue)
    {
        // Use the real application's mapper, mediator, and validation pipeline.
        // Invalid input must be rejected before a handler can access the database.
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["grant_type"] = "password",
            ["username"] = "private-username-marker",
            ["password"] = "private-password-marker",
        };
        if (string.Equals(invalidValue, "missing", StringComparison.Ordinal))
        {
            values.Remove(field);
        }
        else
        {
            values[field] = invalidValue switch
            {
                "empty" => string.Empty,
                "whitespace" => " ",
                "oversized" => new string('x', 257),
                _ => throw new ArgumentOutOfRangeException(nameof(invalidValue)),
            };
        }

        using var form = new FormUrlEncodedContent(values);
        using var response = await client.PostAsync("/api/token", form, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        using var properties = body.RootElement.EnumerateObject();
        var error = properties.Should().ContainSingle().Which;
        error.Name.Should().Be(nameof(error));
        error.Value.GetString().Should().Be("invalid_request");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    [InlineData("client_credentials")]
    public async Task UnsupportedGrantsReturnBadRequest(string? grant)
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        if (grant != null)
        {
            values["grant_type"] = grant;
        }

        using var form = new FormUrlEncodedContent(values);
        using var response = await client.PostAsync("/api/token", form, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        body.RootElement.GetProperty("error").GetString().Should().Be("unsupported_grant_type");
    }
}
