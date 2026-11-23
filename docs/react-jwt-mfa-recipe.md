# Integrate JWT + MFA with React

Start with the [runnable React sample](../samples/JwtSpa/README.md). It uses React, TypeScript, Vite, controlled forms, and `useSyncExternalStore`. The dev server proxies `/api/*` to a real ASP.NET Core issuer.

| Look at | Why |
| --- | --- |
| [React component](../samples/JwtSpa/react/main.tsx) | Shows the login states and dispatches each mutating request from an event handler rather than a mount effect. |
| [Shared controller](../samples/JwtSpa/shared/auth.ts) | Uses native `fetch`, safe QR image rendering, transient token storage, and serialized mutations. |
| [Vite config](../samples/JwtSpa/react/vite.config.ts) | Forwards `/api/*` to the loopback sample backend. |

In a larger app, expose the controller through Context and use a single session instance per tab. Do not put token rotation or a one-time setup-link exchange in an effect that unconditionally reruns. In development, Strict Mode can re-run effects. Put the exchange behind a leader promise or a route loader that deduplicates by token; remove the query parameter using replace navigation and discard it after the exchange. Route guards can improve navigation, but the API owns MFA and permission checks.

If resources refresh automatically, share one in-flight request instead of refreshing independently in each component. See [Browser integration](browser-integration.md) for the retry and failure rules.

[MFA guide](mfa.md) · [HTTP reference](auth-api.md)
