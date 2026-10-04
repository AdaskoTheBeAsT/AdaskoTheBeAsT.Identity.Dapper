using System.Net;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Test;

public sealed class StartupTest
{
    [Fact]
    public void PackagedSettingsContainAValidDemoSigningKey()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        using var settings = JsonDocument.Parse(File.ReadAllText(path));
        var signingKey = settings.RootElement.GetProperty("TokenServiceOptions").GetProperty("SigningKey").GetString();
        // Never include configuration values in an assertion or its output.
        string.IsNullOrWhiteSpace(signingKey).Should().BeFalse();
        (Encoding.UTF8.GetByteCount(signingKey!) >= 32).Should().BeTrue();
    }

    [Fact]
    public async Task DemoSigningKeyStartsApplicationWithoutAnOverride()
    {
        using var factory = new StartupFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var form = new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "unknown" });
        using var response = await client.PostAsync("/api/token", form, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void MissingSigningKeyFailsStartup()
    {
        using var factory = new StartupFactory(string.Empty);
        var exception = FluentActions.Invoking(() => factory.CreateClient()).Should().ThrowExactly<InvalidOperationException>().Which;
        exception.Message.Should().Contain("TokenServiceOptions:SigningKey");
    }

    [Theory]
    [InlineData("short")]
    [InlineData("minimum-minus-one")]
    [InlineData("whitespace")]
    public void InvalidSigningKeyFailsStartup(string kind)
    {
        var signingKey = kind switch
        {
            "short" => "short",
            "minimum-minus-one" => new string('a', 31),
            "whitespace" => new string(' ', 32),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
        using var factory = new StartupFactory(signingKey);
        var exception = FluentActions.Invoking(() => factory.CreateClient()).Should().ThrowExactly<InvalidOperationException>().Which;
        exception.Message.Should().Contain("TokenServiceOptions:SigningKey");
    }

    [Fact]
    public async Task PrivateRandomSigningKeyStartsApplication()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var form = new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "unknown" });
        using var response = await client.PostAsync("/api/token", form, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed class StartupFactory : WebApplicationFactory<Program>
    {
        private readonly string? _signingKey;

        public StartupFactory(string? signingKey = null) => _signingKey = signingKey;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            if (_signingKey != null)
            {
                builder.UseSetting("TokenServiceOptions:SigningKey", _signingKey);
            }

            builder.UseEnvironment("Testing");
        }
    }
}
