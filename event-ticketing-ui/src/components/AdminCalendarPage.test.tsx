import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import AdminCalendarPage from './AdminCalendarPage'

// FullCalendar renders events by mapping them to DOM; mock it to expose titles simply.
vi.mock('@fullcalendar/react', () => ({
  default: ({ events }: { events: Array<{ title: string; extendedProps: unknown }> }) => (
    <div data-testid="fullcalendar">
      {events.map((e, i) => <div key={i} data-testid="calendar-event">{e.title}</div>)}
    </div>
  ),
}))
vi.mock('@fullcalendar/daygrid', () => ({ default: {} }))
vi.mock('@fullcalendar/interaction', () => ({ default: {} }))

const mockEvents = [
  {
    id: 1, title: 'Jazz Night', startTime: '2026-05-02T19:00:00', endTime: '2026-05-02T22:00:00',
    venue: 'Blue Note Club', description: 'Live jazz.', eventType: 'Music', price: 25, availableSeats: 48, totalSeats: 120,
  },
  {
    id: 2, title: 'Tech Conference', startTime: '2026-05-05T09:00:00', endTime: '2026-05-05T18:00:00',
    venue: 'Convention Centre', description: 'Dev talks.', eventType: 'Tech', price: 149, availableSeats: 212, totalSeats: 500,
  },
]

function renderPage() {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
    json: () => Promise.resolve(mockEvents),
  }))
  return render(<MemoryRouter><AdminCalendarPage /></MemoryRouter>)
}

afterEach(() => vi.unstubAllGlobals())

// ── Layout ─────────────────────────────────────────────────────────────────────

test('shows Events Calendar heading', async () => {
  renderPage()
  await waitFor(() => expect(screen.getByRole('heading', { name: /events calendar/i })).toBeInTheDocument())
})

test('shows back to admin link', async () => {
  renderPage()
  await waitFor(() => screen.getByRole('heading', { name: /events calendar/i }))
  expect(screen.getByRole('link', { name: /back to admin/i })).toBeInTheDocument()
})

test('back to admin link points to /admin', async () => {
  renderPage()
  await waitFor(() => screen.getByRole('heading', { name: /events calendar/i }))
  expect(screen.getByRole('link', { name: /back to admin/i })).toHaveAttribute('href', '/admin')
})

// ── Data fetching ──────────────────────────────────────────────────────────────

test('fetches events from /api/events on mount', async () => {
  renderPage()
  await waitFor(() => screen.getByTestId('fullcalendar'))
  expect(vi.mocked(fetch)).toHaveBeenCalledWith('/api/events')
})

test('shows loading state before events arrive', () => {
  vi.stubGlobal('fetch', vi.fn().mockReturnValue(new Promise(() => {}))) // never resolves
  render(<MemoryRouter><AdminCalendarPage /></MemoryRouter>)
  expect(screen.getByText('Loading...')).toBeInTheDocument()
})

// ── Calendar events ────────────────────────────────────────────────────────────

test('renders each event in the calendar with remaining-seats title format', async () => {
  renderPage()
  await waitFor(() => screen.getAllByTestId('calendar-event'))
  const titles = screen.getAllByTestId('calendar-event').map(el => el.textContent)
  expect(titles).toContain('Jazz Night – 48')
  expect(titles).toContain('Tech Conference – 212')
})

// ── Legend ─────────────────────────────────────────────────────────────────────

test('renders color-coded legend with event type labels', async () => {
  renderPage()
  await waitFor(() => screen.getByTestId('fullcalendar'))
  expect(screen.getByText('Music')).toBeInTheDocument()
  expect(screen.getByText('Tech')).toBeInTheDocument()
  expect(screen.getByText('Sports')).toBeInTheDocument()
})

// ── Popup on click ─────────────────────────────────────────────────────────────

test('popup is not shown initially', async () => {
  renderPage()
  await waitFor(() => screen.getAllByTestId('calendar-event'))
  expect(screen.queryByRole('heading', { name: 'Jazz Night' })).not.toBeInTheDocument()
})
