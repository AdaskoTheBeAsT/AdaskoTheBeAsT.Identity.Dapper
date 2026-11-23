interface LoginResponse {
  twoFactorRequired: boolean;
  mfaEnrollmentRequired: boolean;
  accessToken?: string;
  refreshToken?: string;
  sessionId?: string;
  mfaChallengeToken?: string;
  defaultMethod?: string;
  availableMethods?: string[];
}

interface Session {
  accessToken: string;
  refreshToken: string;
}

interface Setup {
  sharedKey: string;
  qrCodeSvg?: string;
}

interface MfaStatus {
  twoFactorEnabled: boolean;
  hasAuthenticator: boolean;
  recoveryCodesLeft: number;
}

export type Screen =
  | { kind: 'login' }
  | { kind: 'challenge'; token: string; method: string; methods: string[] }
  | { kind: 'setup'; token?: string; sharedKey: string; qrImage?: string }
  | { kind: 'session'; status?: MfaStatus };

export interface View {
  screen: Screen;
  busy: boolean;
  message: string;
  error: string;
  recoveryCodes: string[];
}

class ApiError extends Error {
  constructor(readonly status: number, message: string) {
    super(message);
  }
}

async function request<T>(path: string, body?: object, accessToken?: string): Promise<T> {
  const headers = new Headers({ Accept: 'application/json, application/problem+json' });
  if (body) headers.set('Content-Type', 'application/json');
  if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`);
  const response = await fetch(`/api/${path}`, {
    method: body ? 'POST' : 'GET',
    headers,
    body: body ? JSON.stringify(body) : undefined,
    cache: 'no-store',
    redirect: 'error',
  });
  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    const detail = problem?.detail || problem?.title || `Request failed (${response.status}).`;
    throw new ApiError(response.status, detail);
  }
  return response.status === 204 ? (undefined as T) : response.json();
}

// One instance per tab. Never persist a token or authenticator secret in browser storage.
export class AuthDemo {
  private session?: Session;
  private view: View = { screen: { kind: 'login' }, busy: false, message: '', error: '', recoveryCodes: [] };
  private readonly listeners = new Set<() => void>();

  readonly snapshot = () => this.view;

  readonly subscribe = (listener: () => void) => {
    this.listeners.add(listener);
    return () => { this.listeners.delete(listener); };
  };

  register(email: string, password: string) {
    return this.run(async () => {
      await request('auth/register', { email, password });
      this.update({ message: 'Account created. Sign in with the same email and password.' });
    });
  }

  login(email: string, password: string) {
    return this.run(async () => {
      this.update({ recoveryCodes: [] });
      await this.handleLogin(await request<LoginResponse>('auth/login', { email, password }));
    });
  }

  verify(method: string, code: string) {
    return this.run(async () => {
      const screen = this.view.screen;
      if (screen.kind !== 'challenge') return;
      const result = await request<LoginResponse>('auth/login/mfa', { mfaChallengeToken: screen.token, method, code });
      await this.handleLogin(result);
    });
  }

  resend(method: string) {
    return this.run(async () => {
      const screen = this.view.screen;
      if (screen.kind !== 'challenge') return;
      await request('auth/login/mfa/resend', { mfaChallengeToken: screen.token, method });
      this.update({ message: 'A new verification code was requested.' });
    });
  }

  setup() {
    return this.run(() => this.openSetup());
  }

  confirm(code: string) {
    return this.run(async () => {
      const screen = this.view.screen;
      if (screen.kind !== 'setup') return;
      const result = await request<{ recoveryCodes: string[]; loginResponse?: LoginResponse }>(
        'auth/mfa/authenticator/confirm',
        { code, mfaChallengeToken: screen.token },
        this.session?.accessToken,
      );
      if (result.loginResponse) {
        await this.handleLogin(result.loginResponse);
      } else {
        // Enrollment changes the Identity security stamp. Obtain a fresh MFA-assured session.
        this.clearSession();
      }
      this.update({
        recoveryCodes: result.recoveryCodes,
        message: result.loginResponse
          ? 'Authenticator enabled.'
          : 'Authenticator enabled. Save your recovery codes, then sign in again.',
      });
    });
  }

  status() {
    return this.run(async () => {
      const status = await request<MfaStatus>('auth/mfa/status', undefined, this.requireSession().accessToken);
      this.update({ screen: { kind: 'session', status } });
    });
  }

  protectedRequest(stepUp: boolean) {
    return this.run(async () => {
      const response = await request<{ message: string }>(
        stepUp ? 'step-up' : 'private', undefined, this.requireSession().accessToken,
      );
      this.update({ message: response.message });
    });
  }

  refresh() {
    return this.run(async () => {
      const { refreshToken } = this.requireSession();
      try {
        this.accept(await request<LoginResponse>('auth/refresh', { refreshToken }));
        this.update({ message: 'Refresh token rotated.' });
      } catch (error) {
        // A network failure can occur after the server committed rotation. Never retry the old token.
        this.clearSession();
        throw error;
      }
    });
  }

  signOut(scope: 'Session' | 'AllSessions') {
    return this.run(async () => {
      await request('auth/revoke', { scope }, this.requireSession().accessToken);
      this.clearSession();
      this.update({ message: scope === 'Session' ? 'Session revoked.' : 'All sessions revoked.' });
    });
  }

  cancel() {
    if (this.view.busy) return;
    this.update({ screen: this.session ? { kind: 'session' } : { kind: 'login' }, error: '', message: '' });
  }

  forgetRecoveryCodes() {
    this.update({ recoveryCodes: [] });
  }

  private update(update: Partial<View>) {
    this.view = { ...this.view, ...update };
    for (const listener of this.listeners) listener();
  }

  private async run(action: () => Promise<void>) {
    // Synchronous guard: even if events race, only one refresh is submitted.
    if (this.view.busy) return;
    this.update({ busy: true, error: '', message: '' });
    try {
      await action();
    } catch (error) {
      this.update({ error: error instanceof ApiError
        ? error.message
        : 'The request could not be completed. Check the API is running and try again.' });
    } finally {
      this.update({ busy: false });
    }
  }

  private async handleLogin(result: LoginResponse) {
    if (!result.twoFactorRequired) {
      this.accept(result);
      return;
    }

    if (!result.mfaChallengeToken) throw new Error('Missing challenge token.');
    if (result.mfaEnrollmentRequired) {
      await this.openSetup(result.mfaChallengeToken);
      return;
    }
    const methods = result.availableMethods ?? [];
    this.update({
      screen: { kind: 'challenge', token: result.mfaChallengeToken, method: result.defaultMethod ?? methods[0] ?? '', methods },
    });
  }

  private async openSetup(token?: string) {
    const setup = await request<Setup>('auth/mfa/authenticator/setup', { mfaChallengeToken: token }, this.session?.accessToken);
    this.update({
      screen: {
        kind: 'setup',
        token,
        sharedKey: setup.sharedKey,
        qrImage: setup.qrCodeSvg ? `data:image/svg+xml,${encodeURIComponent(setup.qrCodeSvg)}` : undefined,
      },
    });
  }

  private accept(response: LoginResponse) {
    if (!response.accessToken || !response.refreshToken) throw new Error('Missing session tokens.');
    this.session = { accessToken: response.accessToken, refreshToken: response.refreshToken };
    this.update({ screen: { kind: 'session' } });
  }

  private clearSession() {
    this.session = undefined;
    this.update({ screen: { kind: 'login' }, recoveryCodes: [] });
  }

  private requireSession(): Session {
    if (!this.session) throw new Error('Sign in first.');
    return this.session;
  }
}

export function isDeliveryMethod(method: string) {
  return ['email', 'sms'].includes(method.toLowerCase());
}
