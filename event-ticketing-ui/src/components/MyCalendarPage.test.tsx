import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { AuthProvider } from '../context/AuthContext'
import MyCalendarPage from './MyCalendarPage'

vi.mock('@fullcalendar/react', () => ({
  default: ({ events, eventMouseEnter, eventMouseLeave }: {
    events: Array<{ title: string; extendedProps: Record<string, unknown> }>
    eventMouseEnter?: (arg: unknown) => void
    eventMouseLeave?: (arg: unknown) => void
  }) => (
    <div data-testid="fullcalendar">
      {events.map((e, i) => (
        <div
          key={i}
          data-testid="calendar-event"
          onMouseEnter={() => eventMouseEnter?.({
            event: { extendedProps: e.extendedProps },
            el: { getBoundingClientRect: () => ({ top: 100, left: 200, width: 80, bottom: 120 }) },
            jsEvent: {},
          })}
          onMouseLeave={() => eventMouseLeave?.({ event: { extendedProps: e.extendedProps }, el: {} })}
        >
          {e.title}
        </div>
      ))}
    </div>
  ),
}))
vi.mock('@fullcalendar/daygrid', () => ({ default: {} }))
vi.mock('@fullcalendar/interaction', () => ({ default: {} }))

const johnUser = { userId: 1, name: 'John Doe', email: 'john@example.com', role: 'user' }

const mockOrders = [
  {
    id: 10, price: 25, bookedAt: '2026-04-18T10:00:00Z',
    event: {
      id: 1, title: 'Jazz Night', startTime: '2026-05-02T19:00:00', endTime: '2026-05-02T22:00:00',
      venue: 'Blue Note Club', description: 'Live jazz.', eventType: 'Music',
      price: 25, availableSeats: 48, totalSeats: 120,
    },
  },
  {
    id: 11, price: 149, bookedAt: '2026-04-17T10:00:00Z',
    event: {
      id: 2, title: 'Tech Conference', startTime: '2026-05-05T09:00:00', endTime: '2026-05-05T18:00:00',
      venue: 'Convention Centre', description: 'Dev talks.', eventType: 'Tech',
      price: 149, availableSeats: 212, totalSeats: 500,
    },
  },
]

const loggedIn = { ok: true, status: 200, json: () => Promise.resolve(johnUser) } as Response
const notLoggedIn = new Response(null, { status: 401 })

function renderPage() {
  return render(
    <AuthProvider>
      <MemoryRouter initialEntries={['/calendar']}>
        <Routes>
          <Route path="/calendar" element={<MyCalendarPage />} />
          <Route path="/login" element={<div>Login page</div>} />
        </Routes>
      </MemoryRouter>
    </AuthProvider>
  )
}

afterEach(() => vi.restoreAllMocks())

// ── Auth redirect ──────────────────────────────────────────────────────────────

test('redirects to /login when not authenticated', async () => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(notLoggedIn))
  renderPage()
  await waitFor(() => expect(screen.getByText('Login page')).toBeInTheDocument())
})

// ── Layout ─────────────────────────────────────────────────────────────────────

test('shows My Calendar heading for logged-in user', async () => {
  vi.stubGlobal('fetch', vi.fn()
    .mockResolvedValueOnce(loggedIn)
    .mockResolvedValue({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
  )
  renderPage()
  await waitFor(() => expect(screen.getByRole('heading', { name: /my calendar/i })).toBeInTheDocument())
})

// ── Empty state ────────────────────────────────────────────────────────────────

test('shows empty state when user has no bookings', async () => {
  vi.stubGlobal('fetch', vi.fn()
    .mockResolvedValueOnce(loggedIn)
    .mockResolvedValue({ ok: true, json: () => Promise.resolve([]) } as Response)
  )
  renderPage()
  await waitFor(() => expect(screen.getByText(/you have no bookings yet/i)).toBeInTheDocument())
})

test('empty state contains a browse events link', async () => {
  vi.stubGlobal('fetch', vi.fn()
    .mockResolvedValueOnce(loggedIn)
    .mockResolvedValue({ ok: true, json: () => Promise.resolve([]) } as Response)
  )
  renderPage()
  await waitFor(() => screen.getByText(/browse events/i))
  expect(screen.getByRole('link', { name: /browse events/i })).toBeInTheDocument()
})

// ── Calendar events ────────────────────────────────────────────────────────────

test('renders calendar when user has bookings', async () => {
  vi.stubGlobal('fetch', vi.fn()
    .mockResolvedValueOnce(loggedIn)
    .mockResolvedValue({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
  )
  renderPage()
  await waitFor(() => screen.getByTestId('fullcalendar'))
})

test('shows each booked event title in the calendar', async () => {
  vi.stubGlobal('fetch', vi.fn()
    .mockResolvedValueOnce(loggedIn)
    .mockResolvedValue({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
  )
  renderPage()
  await waitFor(() => screen.getAllByTestId('calendar-event'))
  const titles = screen.getAllByTestId('calendar-event').map(el => el.textContent)
  expect(titles).toContain('Jazz Night')
  expect(titles).toContain('Tech Conference')
})

test('fetches orders from /api/auth/me/orders', async () => {
  const fetchMock = vi.fn()
    .mockResolvedValueOnce(loggedIn)
    .mockResolvedValue({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
  vi.stubGlobal('fetch', fetchMock)

  renderPage()
  await waitFor(() => screen.getByTestId('fullcalendar'))
  expect(fetchMock).toHaveBeenCalledWith('/api/auth/me/orders')
})

// ── Legend ─────────────────────────────────────────────────────────────────────

test('renders color-coded legend with event type names', async () => {
  vi.stubGlobal('fetch', vi.fn()
    .mockResolvedValueOnce(loggedIn)
    .mockResolvedValue({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
  )
  renderPage()
  await waitFor(() => screen.getByTestId('fullcalendar'))
  expect(screen.getByText('Music')).toBeInTheDocument()
  expect(screen.getByText('Tech')).toBeInTheDocument()
})

// ── Tooltip on hover ───────────────────────────────────────────────────────────

test('tooltip is not shown before hovering', async () => {
  vi.stubGlobal('fetch', vi.fn()
    .mockResolvedValueOnce(loggedIn)
    .mockResolvedValue({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
  )
  renderPage()
  await waitFor(() => screen.getAllByTestId('calendar-event'))
  expect(screen.queryByRole('tooltip')).not.toBeInTheDocument()
})

test('tooltip appears with event name and seats on mouse enter', async () => {
  const user = userEvent.setup()
  vi.stubGlobal('fetch', vi.fn()
    .mockResolvedValueOnce(loggedIn)
    .mockResolvedValue({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
  )
  renderPage()
  await waitFor(() => screen.getAllByTestId('calendar-event'))
  const [firstEvent] = screen.getAllByTestId('calendar-event')
  await user.hover(firstEvent)
  const tooltip = screen.getByRole('tooltip')
  expect(tooltip).toBeInTheDocument()
  expect(tooltip).toHaveTextContent('Jazz Night')
  expect(tooltip).toHaveTextContent('19:00')
  expect(tooltip).toHaveTextContent('22:00')
})

test('tooltip disappears on mouse leave', async () => {
  const user = userEvent.setup()
  vi.stubGlobal('fetch', vi.fn()
    .mockResolvedValueOnce(loggedIn)
    .mockResolvedValue({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
  )
  renderPage()
  await waitFor(() => screen.getAllByTestId('calendar-event'))
  const [firstEvent] = screen.getAllByTestId('calendar-event')
  await user.hover(firstEvent)
  expect(screen.getByRole('tooltip')).toBeInTheDocument()
  await user.unhover(firstEvent)
  expect(screen.queryByRole('tooltip')).not.toBeInTheDocument()
})
