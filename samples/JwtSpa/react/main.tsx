import { StrictMode, useState, useSyncExternalStore } from 'react';
import { createRoot } from 'react-dom/client';
import { AuthDemo, isDeliveryMethod } from '../shared/auth';
import '../shared/styles.css';

const auth = new AuthDemo();

function App() {
  const { screen, busy, error, message, recoveryCodes } = useSyncExternalStore(auth.subscribe, auth.snapshot);
  const [registering, setRegistering] = useState(false);
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [code, setCode] = useState('');
  const [method, setMethod] = useState('');
  const selected = screen.kind === 'challenge' ? method || screen.method : '';
  const cancel = () => { setCode(''); auth.cancel(); };

  return (
    <main>
      <header>
        <p className="eyebrow">Identity Dapper · React</p>
        <h1>JWT &amp; MFA playground</h1>
        <p className="muted">Create a local account, enroll an authenticator, then call a protected API.</p>
      </header>

      {error && <p className="error" role="alert">{error}</p>}
      {message && <p className="notice" role="status">{message}</p>}

      {recoveryCodes.length > 0 && (
        <section className="panel" aria-labelledby="recovery-title">
          <h2 id="recovery-title">Save your recovery codes</h2>
          <p>Each code works once. Save them in a password manager before dismissing.</p>
          <ul>{recoveryCodes.map(recovery => <li key={recovery}><code>{recovery}</code></li>)}</ul>
          <button type="button" onClick={() => auth.forgetRecoveryCodes()}>I saved the codes</button>
        </section>
      )}

      {screen.kind === 'login' && (
        <section className="panel">
          <h2>{registering ? 'Create an account' : 'Sign in'}</h2>
          <form onSubmit={async event => {
            event.preventDefault();
            setPassword('');
            if (registering) {
              await auth.register(email, password);
              if (!auth.snapshot().error) setRegistering(false);
            } else {
              setMethod('');
              await auth.login(email, password);
            }
          }}>
            <label>Email <input type="email" autoComplete="username" value={email} onChange={event => setEmail(event.target.value)} required /></label>
            <label>Password <input type="password" autoComplete={registering ? 'new-password' : 'current-password'} value={password} onChange={event => setPassword(event.target.value)} required /></label>
            <div className="actions">
              <button className="primary" type="submit" disabled={busy}>{registering ? 'Register' : 'Sign in'}</button>
              <button type="button" disabled={busy} onClick={() => setRegistering(!registering)}>{registering ? 'Back to sign in' : 'Create account'}</button>
            </div>
          </form>
        </section>
      )}

      {screen.kind === 'challenge' && (
        <section className="panel">
          <h2>Verify your sign-in</h2>
          <form onSubmit={event => { event.preventDefault(); setCode(''); void auth.verify(selected, code); }}>
            <label>Method
              <select value={selected} onChange={event => { setMethod(event.target.value); setCode(''); }} required>
                {screen.methods.map(choice => <option key={choice} value={choice}>{choice}</option>)}
              </select>
            </label>
            <label>Verification code <input autoComplete="one-time-code" value={code} onChange={event => setCode(event.target.value)} required /></label>
            <div className="actions">
              <button className="primary" type="submit" disabled={busy}>Verify</button>
              {isDeliveryMethod(selected) && <button type="button" disabled={busy} onClick={() => void auth.resend(selected)}>Resend code</button>}
              <button type="button" disabled={busy} onClick={cancel}>Back to sign in</button>
            </div>
          </form>
        </section>
      )}

      {screen.kind === 'setup' && (
        <section className="panel">
          <h2>Set up an authenticator</h2>
          <p>Scan the QR code or enter the manual setup key in your authenticator. Then enter the six-digit code.</p>
          {screen.qrImage && <img className="qr" src={screen.qrImage} alt="Authenticator setup QR code" />}
          <p>Manual setup key: <code data-testid="setup-key">{screen.sharedKey}</code></p>
          <form onSubmit={event => { event.preventDefault(); setCode(''); void auth.confirm(code); }}>
            <label>Authenticator code <input inputMode="numeric" autoComplete="one-time-code" pattern="[0-9]{6}" value={code} onChange={event => setCode(event.target.value)} required /></label>
            <div className="actions">
              <button className="primary" type="submit" disabled={busy}>Confirm authenticator</button>
              <button type="button" disabled={busy} onClick={cancel}>Cancel setup</button>
            </div>
          </form>
        </section>
      )}

      {screen.kind === 'session' && (
        <section className="panel">
          <h2>Signed in</h2>
          <p>Try both endpoints before and after signing in with MFA to see how step-up authorization works.</p>
          {screen.status && <p>MFA: {screen.status.twoFactorEnabled ? 'enabled' : 'disabled'}. Authenticator: {screen.status.hasAuthenticator ? 'configured' : 'not configured'}. Recovery codes left: {screen.status.recoveryCodesLeft}.</p>}
          <div className="actions">
            <button type="button" className="primary" disabled={busy} onClick={() => void auth.protectedRequest(false)}>Call protected API</button>
            <button type="button" disabled={busy} onClick={() => void auth.protectedRequest(true)}>Call MFA-only API</button>
            <button type="button" disabled={busy} onClick={() => void auth.refresh()}>Rotate refresh token</button>
            <button type="button" disabled={busy} onClick={() => void auth.status()}>Check MFA status</button>
            <button type="button" disabled={busy} onClick={() => { setCode(''); void auth.setup(); }}>Set up authenticator</button>
            <button type="button" disabled={busy} onClick={() => void auth.signOut('Session')}>Sign out</button>
            <button type="button" disabled={busy} onClick={() => void auth.signOut('AllSessions')}>Sign out all sessions</button>
          </div>
        </section>
      )}
      <p className="muted">Local demo only. Tokens stay in this tab; restarting the API clears all accounts.</p>
    </main>
  );
}

createRoot(document.getElementById('root')!).render(<StrictMode><App /></StrictMode>);
