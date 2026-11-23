# Developer guides

Choose a path depending on what you're building. Generated Identity stores and JWT authentication are independent; you can adopt either or both.

| I want to… | Start here |
| --- | --- |
| Use Dapper for ASP.NET Core Identity users, roles, and claims | [Identity quick start](../README.md#-quick-start) |
| Try authentication and MFA without setting up a database server | [Run the Angular or React sample](../samples/JwtSpa/README.md) |
| Issue JWTs, rotate refresh tokens, or validate in a separate API | [JWT issuance, sessions, and resource APIs](jwt-and-sessions.md) |
| Add an authenticator, recovery codes, email/SMS, or step-up authorization | [MFA guide](mfa.md) |
| Look up a route or JSON payload | [HTTP endpoint reference](auth-api.md) |
| Persist sessions and revoked tokens in a database | [JWT persistence and providers](jwt-persistence.md) |
| Keep backend permissions and data scopes out of browser tokens | [Claim catalogs and request enrichment](claim-catalog.md) |
| Add login to an existing SPA | [Browser integration](browser-integration.md) · [Angular](angular-jwt-mfa-recipe.md) · [React](react-jwt-mfa-recipe.md) |
| Upgrade an existing app | [Migration guide](../MIGRATION.md) |
