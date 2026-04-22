import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

const USERS = [
  { label: 'John Doe',     email: 'john@example.com' },
  { label: 'Jane Doer',    email: 'jane@example.com' },
  { label: 'Alex Johnson', email: 'alex@example.com' },
]

const ADMIN = { label: 'Admin', email: 'admin@example.com' }
const PASSWORD = 'Password'

const inputClass =
  'w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400 transition'
const labelClass = 'block text-sm font-medium text-gray-700 mb-1'

interface PanelProps {
  title: string
  options: { label: string; email: string }[]
  selectedEmail: string
  onSelect: (email: string) => void
  error: string | null
  submitting: boolean
  onSubmit: (e: React.FormEvent) => void
  accent: 'gray' | 'indigo'
}

function Panel({ title, options, selectedEmail, onSelect, error, submitting, onSubmit, accent }: PanelProps) {
  const borderClass = accent === 'indigo'
    ? 'border-indigo-200 bg-indigo-50'
    : 'border-gray-200 bg-white'
  const buttonClass = accent === 'indigo'
    ? 'bg-indigo-600 hover:bg-indigo-700 text-white'
    : 'bg-gray-800 hover:bg-gray-900 text-white'

  return (
    <div className={`rounded-xl border p-8 flex flex-col gap-6 ${borderClass}`}>
      <h2 className="text-lg font-semibold text-gray-900 m-0">{title}</h2>

      <form onSubmit={onSubmit} className="flex flex-col gap-5">
        <fieldset className="flex flex-col gap-2 border-0 p-0 m-0">
          <legend className={labelClass}>Select account</legend>
          {options.map(opt => (
            <label
              key={opt.email}
              className="flex items-center gap-3 text-sm text-gray-700 cursor-pointer"
            >
              <input
                type="radio"
                name={`user-${accent}`}
                value={opt.email}
                checked={selectedEmail === opt.email}
                onChange={() => onSelect(opt.email)}
                className="accent-indigo-600"
              />
              <span className="font-medium">{opt.label}</span>
              <span className="text-gray-400">{opt.email}</span>
            </label>
          ))}
        </fieldset>

        <div>
          <label htmlFor={`email-${accent}`} className={labelClass}>Email</label>
          <input
            id={`email-${accent}`}
            type="email"
            value={selectedEmail}
            readOnly
            className={`${inputClass} bg-gray-50 text-gray-500 cursor-default`}
          />
        </div>

        <div>
          <label htmlFor={`password-${accent}`} className={labelClass}>Password</label>
          <input
            id={`password-${accent}`}
            type="password"
            value={PASSWORD}
            readOnly
            className={`${inputClass} bg-gray-50 text-gray-500 cursor-default`}
          />
        </div>

        {error && <p role="alert" className="text-sm text-red-500">{error}</p>}

        <button
          type="submit"
          disabled={submitting || !selectedEmail}
          className={`${buttonClass} px-5 py-2 rounded-lg text-sm font-medium transition-colors disabled:opacity-40 disabled:cursor-not-allowed`}
        >
          {submitting ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
    </div>
  )
}

export default function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()

  const [userEmail,  setUserEmail]  = useState(USERS[0].email)
  const [adminEmail, setAdminEmail] = useState(ADMIN.email)
  const [userError,  setUserError]  = useState<string | null>(null)
  const [adminError, setAdminError] = useState<string | null>(null)
  const [userBusy,   setUserBusy]   = useState(false)
  const [adminBusy,  setAdminBusy]  = useState(false)

  async function handleLogin(
    email: string,
    setError: (e: string | null) => void,
    setBusy: (b: boolean) => void,
  ) {
    setError(null)
    setBusy(true)
    try {
      const res = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password: PASSWORD }),
      })
      if (!res.ok) {
        const data = await res.json().catch(() => ({}))
        setError(data.message ?? 'Login failed. Please try again.')
        return
      }
      const user = await res.json()
      login(user)
      navigate('/')
    } catch {
      setError('Network error. Is the server running?')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="rounded-xl border border-amber-200 bg-amber-50 px-5 py-4 text-sm text-amber-800 leading-relaxed">
        <strong>Tip:</strong> Log in here as a regular user and open this app in another browser (or
        incognito window) to log in as admin — or vice versa. That way you can see what the admin
        panel reflects in real time based on a regular user's actions, and vice versa.{' '}
        <span className="text-amber-600">
          (Real-time push updates via SignalR are planned for a future step.)
        </span>
      </div>

      <div className="flex gap-6">
        <div className="w-[55%]">
          <Panel
            title="Sign in as a regular user"
            options={USERS}
            selectedEmail={userEmail}
            onSelect={setUserEmail}
            error={userError}
            submitting={userBusy}
            onSubmit={e => { e.preventDefault(); handleLogin(userEmail, setUserError, setUserBusy) }}
            accent="gray"
          />
        </div>

        <div className="w-[45%]">
          <Panel
            title="Sign in as admin"
            options={[ADMIN]}
            selectedEmail={adminEmail}
            onSelect={setAdminEmail}
            error={adminError}
            submitting={adminBusy}
            onSubmit={e => { e.preventDefault(); handleLogin(adminEmail, setAdminError, setAdminBusy) }}
            accent="indigo"
          />
        </div>
      </div>
    </div>
  )
}
