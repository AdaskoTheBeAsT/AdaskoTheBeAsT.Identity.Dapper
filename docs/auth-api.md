# JWT and MFA HTTP reference

Paths below assume your backend mounts the endpoints at `/api/auth`. The prefix is configurable. View the [request and response models](../src/AdaskoTheBeAsT.Identity.Dapper.AspNetCore/Models) for the exact C# contracts.

```csharp
app.MapGroup("/api/auth")
    .MapIdentityJwtApi<ApplicationUser>(endpoints =>
    {
        endpoints.EnableRegistration = true;
        endpoints.EnableMfaEndpoints = true;
        endpoints.EnableMfaSetupLinkEndpoints = true;
    });
```

| Method & Path | Auth | Request Body | Success Response | Notes |
| :--- | :--- | :--- | :--- | :--- |
| `POST /api/auth/login` | Anonymous | `{ userName?, email?, password }` | `200` `JwtLoginResponse` | Returns tokens directly, a login MFA challenge (`twoFactorRequired: true`), or a mandatory enrollment challenge (`twoFactorRequired: true, mfaEnrollmentRequired: true`). |
| `POST /api/auth/login/mfa` | Anonymous | `{ mfaChallengeToken, method, code }` | `200` `JwtLoginResponse` | `method` is `'Authenticator' \| 'Email' \| 'Sms' \| 'RecoveryCode'`. Issues a session whose JWT `amr` claim includes `'mfa'`. |
| `POST /api/auth/login/mfa/resend` | Anonymous | `{ mfaChallengeToken, method }` | `200` `{ sent, retryAfterSeconds? }` | `method` is `'Email' \| 'Sms'`. Returns `429` `ProblemDetails` with `Retry-After` header and `retryAfterSeconds` during cooldown. |
| `POST /api/auth/refresh` | Anonymous | `{ refreshToken }` | `200` `JwtLoginResponse` | Single-use rotation bound to `sid` and `SecurityStamp`. **Replaying a consumed token revokes the entire session family.** |
| `POST /api/auth/revoke` | Bearer | `{ scope?, updateSecurityStamp? }` (optional) | `204 No Content` | `scope` is `'Session'` (default), `'Token'`, or `'AllSessions'`. |
| `POST /api/auth/register` | Anonymous | `{ userName?, email, password, phoneNumber? }` | `201` `{ userId }` | Mapped when `EnableRegistration = true`. |
| `GET /api/auth/mfa/status` | Bearer | — | `200` `JwtMfaStatusResponse` | Returns `{ twoFactorEnabled, hasAuthenticator, recoveryCodesLeft, enabledMethods }`. |
| `POST /api/auth/mfa/authenticator/setup` | Bearer *or* bootstrap token | `{ mfaChallengeToken? }` (optional) | `200` `{ sharedKey, authenticatorUri, qrCodeSvg? }` | Stages a pending TOTP secret; does not activate MFA until confirmed. Use Data Protection to encrypt the key at rest. |
| `POST /api/auth/mfa/authenticator/confirm` | Bearer *or* bootstrap token | `{ code, mfaChallengeToken? }` | `200` `{ twoFactorEnabled, recoveryCodes, loginResponse? }` | Activates TOTP, generates initial recovery codes, and issues `loginResponse` when confirming via `mfaChallengeToken`. |
| `POST /api/auth/mfa/methods` | Bearer | `{ method, enabled, challengeToken?, verificationCode? }` | `200` `{ verificationRequired, enabledMethods, challengeToken? }` | Two-step OTP confirmation when enabling `'Email'` or `'Sms'`; requires MFA assurance if 2FA is already active. |
| `POST /api/auth/mfa/recovery-codes/regenerate` | Bearer (`mfa`) | — | `200` `{ recoveryCodes }` | Requires an MFA-assured session (`amr` contains `'mfa'`). Invalidates previous recovery codes. |
| `POST /api/auth/mfa/disable` | Bearer | `{ password?, revokeOtherSessions? }` (optional) | `204 No Content` | Requires MFA assurance or current `password`. Blocked when server policy is `RequiredForAllUsers`. |
| `POST /api/auth/mfa/setup-link` | Bearer | `{ channel? }` (optional) | `200` `{ sent, expiresInSeconds }` | Sends a link based on `PublicSetupBaseUri` with a `token` query parameter via `IMfaCodeSender<TUser>.SendSetupLinkAsync`. |
| `POST /api/auth/mfa/setup-link/exchange` | Anonymous | `{ setupToken }` | `200` `{ mfaChallengeToken, challengeExpiresIn, authenticatorSetup }` | Atomically consumes `setupToken` and returns both a bootstrap `mfaChallengeToken` and staged `authenticatorSetup`. |

## Error responses (`application/problem+json`)

The mapped endpoints use `application/problem+json` for handled validation and domain errors. The package's Bearer handler also writes ProblemDetails for `401` and `403`. A reverse proxy, rate limiter, or network failure might return a different response, so clients need a generic fallback.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Invalid verification code.",
  "error": "invalid_mfa_code",
  "traceId": "00-..."
}
```

On `429 Too Many Requests` (`POST /api/auth/login/mfa/resend`), the response includes both a `Retry-After` HTTP header and a `retryAfterSeconds` extension property:

```json
{
  "type": "https://tools.ietf.org/html/rfc6585#section-4",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Please wait 42 seconds before requesting another verification code.",
  "error": "resend_cooldown_active",
  "retryAfterSeconds": 42
}
```

Common `error` extension codes to handle in UI forms:

- **Login, Refresh & Bearer Auth:** `invalid_request`, `invalid_grant`, `unauthorized`, `invalid_token`, `forbidden`, `mfa_required`
- **MFA Challenge:** `invalid_mfa_code`, `invalid_mfa_challenge`, `mfa_challenge_exhausted`, `method_not_allowed`, `resend_cooldown_active`, `max_resends_exceeded`
- **MFA Management:** `authenticator_setup_not_started`, `email_not_confirmed`, `phone_not_confirmed`, `mfa_assurance_required`, `step_up_required`, `mfa_required_by_policy`, `invalid_setup_token`

See the [MFA guide](mfa.md) for enrollment, setup links, resend, and assurance, and the [JWT guide](jwt-and-sessions.md) for rotation and revocation.
