# JWT issuance, sessions, and resource APIs

Use `AdaskoTheBeAsT.Identity.Dapper.AspNetCore` when your application owns password sign-in and needs bearer tokens. The package uses ASP.NET Core Identity's `UserManager<TUser>` and `SignInManager<TUser>`; it does not require this repository's generated stores. It is not an OAuth/OIDC authorization server and does not supply discovery, federation, or consent endpoints.

## Add an issuer to an Identity app

Install `AdaskoTheBeAsT.Identity.Dapper.AspNetCore` for .NET 10 or .NET 11. Register your Identity user store, `SignInManager<TUser>`, and token providers first. For example, `AddIdentityCore<TUser>()` needs `.AddSignInManager().AddDefaultTokenProviders()` explicitly. Register Data Protection when you use authenticator enrollment.

```csharp
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Authorization;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Endpoints;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;

builder.Services.AddDataProtection();
var issuer = builder.Services.AddIdentityJwtApi<ApplicationUser>(options =>
{
    options.Issuer = "https://auth.example.com";
    options.Audiences.Add("https://api.example.com");
    options.TokenHashKey = builder.Configuration["Auth:TokenHashKey"];
    options.SigningKeys.Add(new JwtSigningKeyDescriptor
    {
        Kid = "signing-1",
        Algorithm = JwtKeyAlgorithm.ES256,
        Pem = builder.Configuration["Auth:PrivatePem"],
        IsActiveSigningKey = true,
    });
}).AddMfaQrCodeSvg();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("mfa", policy => policy.RequireMfaAssurance());

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapGroup("/api/auth").MapIdentityJwtApi<ApplicationUser>();
app.MapGet("/api/private", () => Results.Ok()).RequireAuthorization();
app.MapPost("/api/sensitive-action", () => Results.NoContent()).RequireAuthorization("mfa");
```

This example uses in-memory JWT stores. Select [shared persistence](jwt-persistence.md) before deploying multiple instances. Public registration and setup-link routes are **off by default**; MFA routes are on. The prefix is yours to choose: the runnable sample uses `/api/auth`; the existing SQL Server Web API example uses `/auth`, plus the legacy form-encoded `/api/token`.

## How sessions work

1. `POST /login` either issues tokens or returns an MFA challenge. [Complete the challenge](mfa.md) before treating the user as signed in.
2. Send `Authorization: Bearer <accessToken>` to protected API endpoints.
3. `POST /refresh` with `{ "refreshToken": "…" }`. Atomically replace both tokens in the client with the response. The `sid` stays the same, but the refresh token is single-use.
4. A known consumed token replay revokes its session family, including current refresh tokens and bearer access through the shared revocation store. A client must not race refresh requests or blindly retry an ambiguous refresh failure.
5. `POST /revoke` with a bearer access token. It returns `204`. Clear local session state when signing out.

| Revoke scope | What it affects |
| --- | --- |
| `Token` | This access token (`jti`). Its refresh session remains valid. |
| `Session` (default) | The current session (`sid`) and its refresh family. |
| `AllSessions` | The user's existing refresh sessions and access tokens issued up to the revocation cutoff (`sub`). `updateSecurityStamp: true` also requests Identity security-stamp rotation. |

By default, access tokens last 15 minutes, refresh tokens 14 days, and the absolute session cap is 30 days. Identity account and security-stamp checks can invalidate refresh before expiry. An access token is not checked against Identity on every resource request; use revocation when immediate invalidation matters.

## Validate in a separate resource API

Install the core ASP.NET Core package, not the Identity generator. Match the issuer, audience and `kid`. Give the resource service public verification keys only, not issuer private keys or token-hash keys.

```csharp
var validator = builder.Services.AddIdentityJwtValidation(options =>
{
    options.Issuer = "https://auth.example.com";
    options.Audiences.Add("https://api.example.com");
    options.SigningKeys.Add(new JwtSigningKeyDescriptor
    {
        Kid = "signing-1",
        Algorithm = JwtKeyAlgorithm.ES256,
        Pem = builder.Configuration["Auth:PublicPem"],
        IsActiveSigningKey = false,
    });
});
```

Add authentication and authorization middleware as in the issuer. Connect the validator to the **same revocation backend** as the issuer; the default in-memory validator cannot observe revocations from another process. `FailureMode = RevocationFailureMode.FailClosed` (default) rejects tokens when the revocation backend cannot be checked. `FailOpen` preserves availability at the cost of potentially accepting a revoked token during an outage. Claim transformation is separate and opt-in; see [claim catalogs](claim-catalog.md).

## Before production

- Use TLS and restrict origins. Keep signing keys, token-hash keys, and Data Protection keys private. Use at least 32 bytes of random key material for token hashing.
- Prefer asymmetric signing (`ES*`, `RS*`, `PS*`, or `EdDSA`) when services validate independently. Symmetric `HS*` keys can also sign. For signing rotation, publish the new verification key before switching the issuer, keep the old key for the access-token lifetime plus skew, then remove it. Assign distinct `kid` values and one active issuer key.
- Token hashes use a separate HMAC key. If rotating it, retain prior keys in `PreviousTokenHashKeys` under their former key IDs while records signed with those keys still need to be recognized.
- Apply rate limits and Identity lockout appropriate to your app; MFA resend limits are not a substitute for global or per-account controls. Public signup is opt-in and does not send an email confirmation by itself.
- Use a shared Data Protection key ring with consistent application isolation across issuer replicas. Run schema migrations before rollout and check your readiness and outage behavior.

Next: [MFA](mfa.md) · [API reference](auth-api.md) · [browser integration](browser-integration.md).
