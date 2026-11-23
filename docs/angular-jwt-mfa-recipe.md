# Integrate JWT + MFA with Angular

Start with the [runnable Angular sample](../samples/JwtSpa/README.md). It uses a standalone component, signals, and template-driven forms. The dev server proxies `/api/*` to a real ASP.NET Core issuer, so no CORS configuration is needed for the sample.

| Look at | Why |
| --- | --- |
| [Angular component](../samples/JwtSpa/angular/main.ts) | Adapts the shared controller's snapshots to a signal and unsubscribes on component destruction. |
| [Template](../samples/JwtSpa/angular/app.html) | Switches between credentials, challenge, enrollment, and session. Renders the optional QR as an image. |
| [Shared controller](../samples/JwtSpa/shared/auth.ts) | Posts to the real endpoints and keeps access tokens, refresh tokens, and challenge data out of persistent storage. |
| [Proxy](../samples/JwtSpa/angular/proxy.json) | Forwards `/api/**` to the loopback sample backend. |

In a larger app, put the controller behind an injectable service. If using `HttpClient`, configure a functional interceptor that attaches the bearer token only to your own resource API. Exclude the auth endpoints and share one in-flight rotation across requests. Clear the credential on logout and cancel or ignore late responses. Use route guards for navigation only; apply `RequireMfaAssurance()` or a permission policy in the API.

For mandatory enrollment, carry the returned bootstrap token from `/login` into both authenticator endpoints. For out-of-band setup links, remove the initial URL parameter with a replace navigation and call exchange once, not on every route emission.

[Browser integration](browser-integration.md) · [MFA guide](mfa.md) · [HTTP reference](auth-api.md)
