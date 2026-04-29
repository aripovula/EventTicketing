import { render, screen, waitFor } from '@testing-library/react'
import App from './App'
import { AuthProvider } from './context/AuthContext'

vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: function () {
    return {
      withUrl:                function () { return this },
      withAutomaticReconnect: function () { return this },
      configureLogging:       function () { return this },
      build: function () {
        return { on: function () {}, start: () => Promise.resolve(), stop: () => Promise.resolve() }
      },
    }
  },
  LogLevel: { Warning: 2 },
}))

// Mock FullCalendar to avoid complex DOM requirements in tests
vi.mock('@fullcalendar/react', () => ({ default: () => null }))
vi.mock('@fullcalendar/daygrid', () => ({ default: {} }))
vi.mock('@fullcalendar/interaction', () => ({ default: {} }))

const johnUser = { userId: 1, name: 'John Doe', email: 'john@example.com', role: 'user' }

afterEach(() => vi.restoreAllMocks())

function stubFetch(user: typeof johnUser | null) {
  vi.stubGlobal('fetch', vi.fn().mockImplementation((url: string) => {
    if (String(url).includes('/api/auth/me')) {
      return user
        ? Promise.resolve({ ok: true, json: () => Promise.resolve(user) } as Response)
        : Promise.resolve(new Response(null, { status: 401 }))
    }
    // events list — return empty so EventList renders quickly
    return Promise.resolve({ ok: true, json: () => Promise.resolve([]) } as Response)
  }))
}

// ── Header nav: guest ──────────────────────────────────────────────────────────

test('guest nav shows Sign in link', async () => {
  stubFetch(null)
  render(<AuthProvider><App /></AuthProvider>)
  await waitFor(() => expect(screen.getByRole('link', { name: /sign in/i })).toBeInTheDocument())
})

test('guest nav does not show My Calendar link', async () => {
  stubFetch(null)
  render(<AuthProvider><App /></AuthProvider>)
  await waitFor(() => screen.getByRole('link', { name: /sign in/i }))
  expect(screen.queryByRole('link', { name: /my calendar/i })).not.toBeInTheDocument()
})

// ── Header nav: logged-in user ─────────────────────────────────────────────────

test('logged-in nav shows My Calendar link', async () => {
  stubFetch(johnUser)
  render(<AuthProvider><App /></AuthProvider>)
  await waitFor(() => expect(screen.getByRole('link', { name: /my calendar/i })).toBeInTheDocument())
})

test('My Calendar link points to /calendar', async () => {
  stubFetch(johnUser)
  render(<AuthProvider><App /></AuthProvider>)
  await waitFor(() => screen.getByRole('link', { name: /my calendar/i }))
  expect(screen.getByRole('link', { name: /my calendar/i })).toHaveAttribute('href', '/calendar')
})

test('My Calendar link appears before My orders in the nav', async () => {
  stubFetch(johnUser)
  render(<AuthProvider><App /></AuthProvider>)
  await waitFor(() => screen.getByRole('link', { name: /my calendar/i }))

  const navLinks = screen.getAllByRole('link')
  const calendarIdx = navLinks.findIndex(l => /my calendar/i.test(l.textContent ?? ''))
  const ordersIdx   = navLinks.findIndex(l => /my orders/i.test(l.textContent ?? ''))
  expect(calendarIdx).toBeLessThan(ordersIdx)
})

test('logged-in nav shows My orders, Profile, and Sign out', async () => {
  stubFetch(johnUser)
  render(<AuthProvider><App /></AuthProvider>)
  await waitFor(() => screen.getByRole('link', { name: /my calendar/i }))
  expect(screen.getByRole('link',   { name: /my orders/i })).toBeInTheDocument()
  expect(screen.getByRole('link',   { name: /profile/i })).toBeInTheDocument()
  expect(screen.getByRole('button', { name: /sign out/i })).toBeInTheDocument()
})
