import { useState } from 'react'
import './App.css'

const API_URL = import.meta.env.VITE_API_URL

export default function App() {
  const [email, setEmail] = useState('admin@stockroom.co')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [session, setSession] = useState(() => {
    const raw = localStorage.getItem('admin_session')
    return raw ? JSON.parse(raw) : null
  })

  async function handleSubmit(e) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const res = await fetch(`${API_URL}/api/admin/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      })
      const data = await res.json()
      if (!res.ok) {
        setError(data.message || 'Sign in failed.')
        return
      }
      localStorage.setItem('admin_session', JSON.stringify(data))
      setSession(data)
    } catch {
      setError('Could not reach the API. Is it running?')
    } finally {
      setLoading(false)
    }
  }

  function handleSignOut() {
    localStorage.removeItem('admin_session')
    setSession(null)
    setPassword('')
  }

  if (session) {
    return (
      <div className="page">
        <div className="card signed-in">
          <div className="brand">Stockroom Admin</div>
          <h1>You're signed in</h1>
          <p className="sub">Welcome back, {session.displayName}.</p>
          <dl className="details">
            <dt>Email</dt>
            <dd>{session.email}</dd>
            <dt>Role</dt>
            <dd>{session.role}</dd>
          </dl>
          <button className="btn ghost block" onClick={handleSignOut}>Sign out</button>
        </div>
      </div>
    )
  }

  return (
    <div className="page">
      <div className="card">
        <div className="brand">Stockroom Admin</div>
        <h1>Sign in</h1>
        <p className="sub">Access the admin portal.</p>
        <form onSubmit={handleSubmit}>
          <label className="field">
            <span>Email</span>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              autoFocus
            />
          </label>
          <label className="field">
            <span>Password</span>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </label>
          {error && <div className="error">{error}</div>}
          <button className="btn teal block" type="submit" disabled={loading}>
            {loading ? 'Signing in…' : 'Sign in'}
          </button>
        </form>
        <div className="hint">
          <b>Demo:</b> admin@stockroom.co / Passw0rd!
        </div>
      </div>
    </div>
  )
}
