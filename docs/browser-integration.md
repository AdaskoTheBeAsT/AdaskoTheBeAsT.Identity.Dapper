# Browser integration: keep authentication state explicit

Start with the [runnable samples](../samples/JwtSpa/README.md) or pick a framework: [Angular](angular-jwt-mfa-recipe.md) · [React](react-jwt-mfa-recipe.md).

## Model the flow as states, not a boolean

| State | Keep in transient memory | Next action |
| --- | --- | --- |
| Signed out | Nothing beyond the current form | Sign in or register. Clear the password after submit. |
| MFA challenge | Challenge token, allowed methods, default method | Verify a code, request an eligible resend, or restart sign-in. |
| MFA enrollment | Enrollment challenge if present, pending setup fields | Confirm; discard the QR/key immediately when finished or cancelled. |
| Signed in | Current access and refresh pair | Call authorized endpoints, rotate the pair, or revoke the session. |

Use server-provided method names and error codes. You can show or hide navigation based on a token claim, but the API makes the authorization decision. Use [the endpoint contract](auth-api.md) to distinguish `401`, `403`, and resend `429`.

## Refresh once, then retry carefully

The samples serialize UI operations and expose a manual **Rotate refresh token** button so the exchange is visible. In an app with background requests:

1. Attach the bearer token only to your trusted API origin.
2. On an eligible protected-request `401`, first check whether the token you used is already out of date because another request refreshed it. If so, use the current token.
3. Otherwise, let requests await one shared in-flight refresh promise (or shared observable). Store the new token pair together before waking callers.
4. Retry at most once and only where replaying the original request is safe. Do not intercept login, refresh, revoke, registration, or MFA requests with this logic. Do not refresh for a `403`.
5. If refresh fails or its outcome is uncertain, discard the local pair and sign in again. A network error can arrive after the server consumed the old token. Do not let a late response repopulate state after logout; use an abort signal or session generation check.

A mutex in one tab cannot coordinate separate tabs that share tokens. The samples avoid that by keeping tokens in memory and signing in separately per tab. Avoid putting bearer tokens, recovery codes, or authenticator keys in `localStorage` or `sessionStorage`. For browser apps that need persistent sessions, consider a same-origin backend-for-frontend (BFF) with `HttpOnly`, `Secure` cookies, session handling on the server, and CSRF protection. The library's JSON token endpoints do not implement a BFF for you.

## Handle setup and navigation safely

- Render the server QR as an encoded image, or generate a QR locally from `authenticatorUri`. Never inject raw SVG HTML or forward the URI to a service. Show the manual key as a fallback.
- Don't include passwords, tokens, OTPs, or recovery codes in logs, analytics, error reports, URLs, or screenshots. The initial setup-link URL is an exception: strip its token immediately, keep it in memory, then exchange it once.
- Prefer named internal routes. If accepting a user-supplied `returnUrl`, parse it relative to your origin and verify the parsed origin and allowed path before navigating; a leading slash alone is insufficient.
- Use HTTPS in production and narrow CORS to known origins if an API must be cross-origin. For resend countdowns across origins, expose `Retry-After`, or read the `retryAfterSeconds` problem field.
