import { render, screen, waitFor } from '@testing-library/react'
import App from './App'
import { AuthProvider } from './context/AuthContext'

// Stripe mock — BookingModal (rendered inside EventDetail routes) uses useStripe /
// useElements. Mocking here prevents "no Elements context" errors if any future
// test navigates to an event detail page.
vi.mock('@stripe/react-stripe-js', () => ({
  CardElement: () => null,
  useStripe: () => ({ createPaymentMethod: vi.fn() }),
  useElements: () => ({ getElement: () => null }),
}))

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

const johnUser  = { userId: 1, name: 'John Doe',  email: 'john@example.com',  role: 'user'  }
const adminUser = { userId: 2, name: 'Admin User', email: 'admin@example.com', role: 'admin' }

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

test('guest nav shows All Events link pointing to /', async () => {
  stubFetch(null)
  render(<AuthProvider><App /></AuthProvider>)
  await waitFor(() => screen.getByRole('link', { name: /sign in/i }))
  const allEvents = screen.getByRole('link', { name: /all events/i })
  expect(allEvents).toBeInTheDocument()
  expect(allEvents).toHaveAttribute('href', '/')
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

test('logged-in nav shows All Events link pointing to /', async () => {
  stubFetch(johnUser)
  render(<AuthProvider><App /></AuthProvider>)
  await waitFor(() => screen.getByRole('link', { name: /my calendar/i }))
  const allEvents = screen.getAllByRole('link', { name: /all events/i })[0]
  expect(allEvents).toBeInTheDocument()
  expect(allEvents).toHaveAttribute('href', '/')
})

test('logged-in nav shows My orders, Profile, and Sign out', async () => {
  stubFetch(johnUser)
  render(<AuthProvider><App /></AuthProvider>)
  await waitFor(() => screen.getByRole('link', { name: /my calendar/i }))
  expect(screen.getByRole('link',   { name: /my orders/i })).toBeInTheDocument()
  expect(screen.getByRole('link',   { name: /profile/i })).toBeInTheDocument()
  expect(screen.getByRole('button', { name: /sign out/i })).toBeInTheDocument()
})

// ── Header nav: admin ──────────────────────────────────────────────────────────

test('admin nav shows Events Calendar link instead of My Calendar', async () => {
  stubFetch(adminUser)
  render(<AuthProvider><App /></AuthProvider>)
  await waitFor(() => expect(screen.getByRole('link', { name: /events calendar/i })).toBeInTheDocument())
  expect(screen.queryByRole('link', { name: /my calendar/i })).not.toBeInTheDocument()
})

test('admin Events Calendar link points to /admin/calendar', async () => {
  stubFetch(adminUser)
  render(<AuthProvider><App /></AuthProvider>)
  await waitFor(() => screen.getByRole('link', { name: /events calendar/i }))
  expect(screen.getByRole('link', { name: /events calendar/i })).toHaveAttribute('href', '/admin/calendar')
})
