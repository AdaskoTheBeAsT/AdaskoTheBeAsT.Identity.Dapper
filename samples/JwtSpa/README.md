# Run the Angular and React JWT + MFA samples

Both clients use the same **real ASP.NET Core API**. It uses generated Identity stores and database-backed JWT stores over a shared in-memory SQLite database. You don't need Docker, SQL Server, an existing account, or an email provider.

**Prerequisites:** the repository's .NET SDK (`global.json`), the .NET 10 runtime if using `-f net10.0`, Node 24.15+ LTS (or a [supported Node version](https://angular.dev/reference/versions)), npm, and an authenticator app.

## Start the API

From the repository root:

```bash
dotnet run --project samples/JwtSpa/Api/JwtSpa.Api.csproj -f net11.0 --no-launch-profile -- --environment Development
```

The API listens only at `http://127.0.0.1:5080`. It creates both schemas and generates local signing and token-hash keys at startup. Restarting it clears all accounts and sessions. To try MFA enrollment at first sign-in instead of opting in after sign-in, append `--Sample:RequireMfa=true` to the command.

## Start a frontend

Open a second terminal from the repository root:

```bash
cd samples/JwtSpa
npm ci
npm run dev:angular  # http://127.0.0.1:4200
# Or, in this or another terminal:
npm run dev:react    # http://127.0.0.1:5173
```

Both dev servers proxy `/api/*` to the loopback API. Open either URL, then:

1. **Create account** with a throwaway email (for example `developer@example.test`) and a unique password that satisfies the Identity policy.
2. **Sign in**. Try **Call protected API** (success) and **Call MFA-only API** (forbidden before MFA).
3. **Rotate refresh token**. The successful response replaces both tokens without changing the session ID.
4. **Set up authenticator**. Scan the QR code or enter the manual key, then confirm with a six-digit code. Save the recovery codes. The sample sends you back to sign-in after signed-in enrollment because it changes the account's security state.
5. **Sign in again**. Choose `Authenticator` or `RecoveryCode` and finish verification. An authenticator code used for enrollment may be rejected as a replay; wait for the next code. The MFA-only API now succeeds.
6. Use **Sign out** to revoke the current session or **Sign out all sessions** to revoke all of that account's sessions. To see all-session revocation, sign in to the same account in both clients first and then call the protected API from the other client.

Run `npm run build` from `samples/JwtSpa` to build both frontends.

## Read the code

| File | What to look for |
| --- | --- |
| [API startup](Api/Program.cs) | Identity registration, SQLite setup, JWT issuer, registration, MFA and step-up policy |
| [Shared client](shared/auth.ts) | Login response branches, API errors, memory-only state, refresh guard, revocation |
| [Angular component](angular/main.ts) and [template](angular/app.html) | Signals, template-driven forms, and conditional screens |
| [React component](react/main.tsx) | Controlled forms, `useSyncExternalStore`, and event-driven transitions |

These samples deliberately do not configure email/SMS delivery, setup links, or a production session strategy. They use plain HTTP on loopback, public signup, and disposable data. Do not deploy the API as-is. For production integration, start with the [JWT guide](../../docs/jwt-and-sessions.md), [MFA guide](../../docs/mfa.md), and [browser integration guide](../../docs/browser-integration.md).
