# Multi-factor authentication (MFA)

Enrollment answers **“Which factors can this account use?”** Sign-in verification answers **“Did this session prove a second factor?”** A user may be enrolled while an older access token still lacks MFA assurance. Keep both concepts separate in UI and API policies.

## Choose an enforcement mode

| `options.Mfa.EnforcementMode` | Behavior |
| --- | --- |
| `PerUserSetting` (default) | Respect the user's Identity two-factor setting. A password-only user can enroll after sign-in. |
| `RequiredForAllUsers` | Users without a usable factor get an enrollment challenge, not tokens. |
| `CustomPolicy` | Register an `IMfaRequirementPolicy<TUser>` via `.AddMfaRequirementPolicy<TUser, TPolicy>()`. |

### TOTP enrollment

1. `POST /mfa/authenticator/setup` with your bearer token or your **enrollment** `mfaChallengeToken`. This stages a pending key but does not enable the factor.
2. Show a QR or the manual `sharedKey`. `.AddMfaQrCodeSvg()` returns `qrCodeSvg`; render it through an encoded SVG image URL, not HTML injection. If you prefer client rendering, turn `authenticatorUri` into a QR locally. Never send it to a third-party QR service. Register a custom renderer using `.AddMfaQrCodeGenerator<TUser,TGenerator>()`.
3. `POST /mfa/authenticator/confirm` with `code` and, for bootstrap enrollment, `mfaChallengeToken`. A successful bootstrap can return a nested `loginResponse`. In settings enrollment, use a fresh sign-in to get an MFA-assured session.
4. Display any returned `recoveryCodes` once and discard the setup secret. Use a shared, protected Data Protection key ring in production. Existing authenticator secrets remain compatible with the TOTP implementation.

### Sign-in branches

| `/login` response | Client action |
| --- | --- |
| `twoFactorRequired: false` | Accept the returned access and refresh pair. |
| `twoFactorRequired: true`, `mfaEnrollmentRequired: false` | Show the returned `availableMethods`, preselect `defaultMethod`, then submit `mfaChallengeToken`, `method`, and `code` to `/login/mfa`. |
| `mfaEnrollmentRequired: true` | Use the returned token with the authenticator setup and confirm endpoints, not the normal `/login/mfa` verification route. |

Challenges are time-limited. Do not persist them or treat them as access tokens. Defaults include a five-minute challenge, five attempts, and a 90-second OTP replay window. On expiration or exhaustion, restart sign-in. An authenticator code used in enrollment can be rejected if used immediately again; wait for the next code.

### Email, SMS, and fallback

- Implement `IMfaCodeSender<TUser>` and register it with `.AddMfaCodeSender<TUser, TSender>()`. Configure `AllowEmailMfa` / `AllowSmsMfa`; a configured sender and a confirmed destination are required. Do not log OTPs or destination details.
- To enable Email or SMS, post `{ "method": "Email", "enabled": true }` to `/mfa/methods`. If it returns `verificationRequired: true`, post again with the returned `challengeToken` and the delivered `verificationCode`. A user with active MFA needs an MFA-assured session to change these methods.
- For a sign-in challenge, `/login/mfa/resend` accepts `Email` or `Sms`. Respect the server's `Retry-After` on a cooldown `429`. The default cooldown is 30 seconds with up to three resends per challenge. Server limits still apply if the user opens another tab.
- Recovery codes are single-use. Regeneration requires an MFA-assured bearer session and replaces all previous codes. Disablement requires an MFA-assured session or current password and is blocked by `RequiredForAllUsers`.

### Out-of-band setup links

Set `options.Mfa.PublicSetupBaseUri` to an HTTPS page you control, implement `SendSetupLinkAsync`, and set `EnableMfaSetupLinkEndpoints = true` on the route mapper. The authenticated `/mfa/setup-link` endpoint requests delivery. In the landing page, remove the `token` query parameter from the URL before starting telemetry or loading third-party resources; submit it exactly once as `setupToken` to `/mfa/setup-link/exchange`. Exchange consumes the link and returns a separate enrollment challenge and staged authenticator setup. Use that challenge for confirmation. Avoid placing the returned challenge or setup secret in the URL.

## Require MFA for a sensitive API operation

Apply a policy registered with `policy.RequireMfaAssurance()` (namespace `AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Authorization`). It requires the validated principal to carry `amr: mfa`. A password-only token gets a `403` for this policy, not a refresh prompt: the user must sign in with a second factor. The policy checks MFA assurance, not how recently MFA was performed. Add a separate freshness check if your operation requires it.

Try the [two runnable frontends](../samples/JwtSpa/README.md) or consult the [endpoint reference](auth-api.md).
