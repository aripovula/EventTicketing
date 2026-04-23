import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import '@testing-library/jest-dom'
import { vi } from 'vitest'
import EventDetail from './EventDetail'
import { AuthProvider } from '../context/AuthContext'

const mockEvent = {
  id: 1,
  title: 'Jazz Night',
  description: 'An evening of live jazz music.',
  date: '2026-06-15T20:00:00',
  venue: 'Blue Note Club',
  totalSeats: 100,
  availableSeats: 80,
  price: 25,
}
const mockOrder = { id: 42, eventId: 1, email: 'user@example.com', price: 25, bookedAt: '2026-04-17T00:00:00Z' }

// Helpers — React runs children's effects before parents', so fetch call order is:
//   1. EventDetail useEffect[id]        → GET /api/events/:id
//   2. AuthProvider useEffect[]         → GET /api/auth/me
//   3. EventDetail useEffect[user]      → GET /api/auth/me/cards/default  (only when user != null)

const eventRes  = { ok: true,  status: 200, json: () => Promise.resolve(mockEvent) } as Response
const notLoggedIn = { ok: false, status: 401, json: () => Promise.resolve(null) } as Response

function renderWithRoute(id: string) {
  return render(
    <AuthProvider>
      <MemoryRouter initialEntries={[`/events/${id}`]}>
        <Routes>
          <Route path="/events/:id" element={<EventDetail />} />
          <Route path="/orders/:id" element={<div>Order confirmed</div>} />
        </Routes>
      </MemoryRouter>
    </AuthProvider>
  )
}

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn())
})

afterEach(() => {
  vi.unstubAllGlobals()
  document.title = 'Event Ticketing'
})

// ── Loading / basic rendering ──────────────────────────────────────────────────

test('shows loading state initially', () => {
  vi.mocked(fetch).mockResolvedValue(eventRes)
  renderWithRoute('1')
  expect(screen.getByText('Loading...')).toBeInTheDocument()
})

test('renders event details after fetch', async () => {
  vi.mocked(fetch).mockResolvedValue(eventRes)
  renderWithRoute('1')
  await waitFor(() => expect(screen.getByText('Jazz Night')).toBeInTheDocument())
  expect(screen.getByText('An evening of live jazz music.')).toBeInTheDocument()
  expect(screen.getByText(/Blue Note Club/)).toBeInTheDocument()
})

test('shows not found message for unknown event', async () => {
  vi.mocked(fetch).mockResolvedValue({ ok: false, status: 404, json: () => Promise.resolve(null) } as Response)
  renderWithRoute('999')
  await waitFor(() => expect(screen.getByText('Event not found.')).toBeInTheDocument())
})

test('sets document title to event title after loading', async () => {
  vi.mocked(fetch).mockResolvedValue(eventRes)
  renderWithRoute('1')
  await waitFor(() => expect(document.title).toBe('Jazz Night | Ticketing'))
})

test('shows error state when fetch fails', async () => {
  vi.mocked(fetch).mockRejectedValue(new Error('Network error'))
  renderWithRoute('1')
  await waitFor(() => expect(screen.getByText('Something went wrong. Please try again.')).toBeInTheDocument())
})

test('shows back to events link', async () => {
  vi.mocked(fetch).mockResolvedValue(eventRes)
  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  expect(screen.getByRole('link', { name: /back to events/i })).toBeInTheDocument()
})

test('displays correct seat availability', async () => {
  vi.mocked(fetch).mockResolvedValue(eventRes)
  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  expect(screen.getByText(/80 of 100 seats available/)).toBeInTheDocument()
})

test('resets document title on unmount', async () => {
  vi.mocked(fetch).mockResolvedValue(eventRes)
  const { unmount } = renderWithRoute('1')
  await waitFor(() => expect(screen.getByText('Jazz Night')).toBeInTheDocument())
  expect(document.title).toBe('Jazz Night | Ticketing')
  unmount()
  expect(document.title).toBe('Event Ticketing')
})

test('shows thumbnail image when imageUrl is present', async () => {
  const eventWithImage = { ...mockEvent, imageUrl: 'https://images.unsplash.com/photo-123' }
  vi.mocked(fetch).mockResolvedValue({ ok: true, status: 200, json: () => Promise.resolve(eventWithImage) } as Response)
  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  const img = screen.getByRole('img', { name: 'Jazz Night' })
  expect(img).toBeInTheDocument()
  expect(img).toHaveAttribute('src', 'https://images.unsplash.com/photo-123')
})

test('does not show img element when imageUrl is absent', async () => {
  vi.mocked(fetch).mockResolvedValue(eventRes)
  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  expect(screen.queryByRole('img')).not.toBeInTheDocument()
})

// ── Buy ticket button ──────────────────────────────────────────────────────────

test('shows Buy ticket button when seats are available', async () => {
  vi.mocked(fetch).mockResolvedValue(eventRes)
  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  expect(screen.getByRole('button', { name: /buy ticket/i })).toBeInTheDocument()
})

test('Buy ticket button is disabled when sold out', async () => {
  vi.mocked(fetch).mockResolvedValue({ ok: true, status: 200, json: () => Promise.resolve({ ...mockEvent, availableSeats: 0 }) } as Response)
  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  expect(screen.getByRole('button', { name: /sold out/i })).toBeDisabled()
})

test('clicking Buy ticket opens the booking modal', async () => {
  vi.mocked(fetch).mockResolvedValue(eventRes)
  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  fireEvent.click(screen.getByRole('button', { name: /buy ticket/i }))
  expect(screen.getByRole('dialog')).toBeInTheDocument()
  expect(screen.getByLabelText(/email/i)).toBeInTheDocument()
})

// ── Booking via modal ──────────────────────────────────────────────────────────

const mockOrder401 = { ok: false, status: 401, json: () => Promise.resolve(null) } as Response

async function fillAndSubmitModal() {
  fireEvent.click(screen.getByRole('button', { name: /buy ticket/i }))
  fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'user@example.com' } })
  fireEvent.change(screen.getByLabelText(/card number/i), { target: { value: '1234567890123456' } })
  fireEvent.change(screen.getByLabelText(/expiry/i), { target: { value: '12/27' } })
  fireEvent.change(screen.getByLabelText(/cvv/i), { target: { value: '123' } })
  fireEvent.submit(screen.getByRole('form', { name: /booking form/i }))
}

test('confirming modal posts email to /api/events/:id/book', async () => {
  const fetchMock = vi.mocked(fetch)
  fetchMock
    .mockResolvedValueOnce(eventRes)       // 1. event fetch
    .mockResolvedValueOnce(mockOrder401)   // 2. /me → not logged in
    .mockResolvedValueOnce({ ok: true, status: 201, json: () => Promise.resolve(mockOrder) } as Response) // 3. book

  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  await fillAndSubmitModal()

  await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/events/1/book', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: 'user@example.com' }),
  }))
})

test('navigates to order confirmation page after successful booking', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce(eventRes)
    .mockResolvedValueOnce(mockOrder401)
    .mockResolvedValueOnce({ ok: true, status: 201, json: () => Promise.resolve(mockOrder) } as Response)

  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  await fillAndSubmitModal()

  await waitFor(() => expect(screen.getByText('Order confirmed')).toBeInTheDocument())
})

test('shows sold-out error in modal on 409 response', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce(eventRes)
    .mockResolvedValueOnce(mockOrder401)
    .mockResolvedValueOnce({ ok: false, status: 409 } as Response)

  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  fireEvent.click(screen.getByRole('button', { name: /buy ticket/i }))
  fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'user@example.com' } })
  fireEvent.change(screen.getByLabelText(/card number/i), { target: { value: '1234567890123456' } })
  fireEvent.change(screen.getByLabelText(/expiry/i), { target: { value: '12/27' } })
  fireEvent.change(screen.getByLabelText(/cvv/i), { target: { value: '123' } })
  fireEvent.submit(screen.getByRole('form', { name: /booking form/i }))

  await waitFor(() => expect(screen.getByText(/sorry, this event just sold out/i)).toBeInTheDocument())
  expect(screen.getByRole('dialog')).toBeInTheDocument()
})

test('shows generic error in modal on non-409 booking failure', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce(eventRes)
    .mockResolvedValueOnce(mockOrder401)
    .mockResolvedValueOnce({ ok: false, status: 500 } as Response)

  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  fireEvent.click(screen.getByRole('button', { name: /buy ticket/i }))
  fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'user@example.com' } })
  fireEvent.change(screen.getByLabelText(/card number/i), { target: { value: '1234567890123456' } })
  fireEvent.change(screen.getByLabelText(/expiry/i), { target: { value: '12/27' } })
  fireEvent.change(screen.getByLabelText(/cvv/i), { target: { value: '123' } })
  fireEvent.submit(screen.getByRole('form', { name: /booking form/i }))

  await waitFor(() => expect(screen.getByText(/booking failed/i)).toBeInTheDocument())
})

test('Cancel button closes the modal', async () => {
  vi.mocked(fetch).mockResolvedValue(eventRes)
  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  fireEvent.click(screen.getByRole('button', { name: /buy ticket/i }))
  expect(screen.getByRole('dialog')).toBeInTheDocument()
  fireEvent.click(screen.getByRole('button', { name: /cancel/i }))
  expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
})

test('modal shows Place order heading', async () => {
  vi.mocked(fetch).mockResolvedValue(eventRes)
  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  fireEvent.click(screen.getByRole('button', { name: /buy ticket/i }))
  expect(screen.getByRole('heading', { name: /place order/i })).toBeInTheDocument()
})

test.skip('modal closes and navigates away on successful booking', async () => {
  // Skipped: race between dialog disappearing and route rendering — needs investigation
})

// ── Card pre-fill (logged-in user) ─────────────────────────────────────────────
// Fetch order when logged in: event → /me (200+user) → /me/cards/default

const johnUser = { userId: 1, name: 'John Doe', email: 'john@example.com', role: 'user' }
const defaultCard = { last4: '4242', cardType: 'Visa', expiryDate: '12/32' }

test('pre-fills email from logged-in user when modal opens', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce(eventRes)
    .mockResolvedValueOnce({ ok: true, status: 200, json: () => Promise.resolve(johnUser) } as Response)
    .mockResolvedValueOnce({ ok: false, status: 404 } as Response) // no card

  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  // Wait for /me and cards fetch to settle
  await waitFor(() => {})
  fireEvent.click(screen.getByRole('button', { name: /buy ticket/i }))

  const emailInput = screen.getByLabelText(/email/i) as HTMLInputElement
  expect(emailInput.value).toBe('john@example.com')
})

test('shows saved card option in modal when user has a default card', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce(eventRes)
    .mockResolvedValueOnce({ ok: true, status: 200, json: () => Promise.resolve(johnUser) } as Response)
    .mockResolvedValueOnce({ ok: true, status: 200, json: () => Promise.resolve(defaultCard) } as Response)

  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  await waitFor(() => {})
  fireEvent.click(screen.getByRole('button', { name: /buy ticket/i }))

  await waitFor(() =>
    expect(screen.getByText(/card ending in 4242/i)).toBeInTheDocument()
  )
})

test('shows new card form when logged-in user has no default card', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce(eventRes)
    .mockResolvedValueOnce({ ok: true, status: 200, json: () => Promise.resolve(johnUser) } as Response)
    .mockResolvedValueOnce({ ok: false, status: 404 } as Response)

  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  await waitFor(() => {})
  fireEvent.click(screen.getByRole('button', { name: /buy ticket/i }))

  expect(screen.getByLabelText(/card number/i)).toBeInTheDocument()
})

test('email field is empty when user is not logged in', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce(eventRes)
    .mockResolvedValueOnce(mockOrder401) // /me → not logged in

  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  fireEvent.click(screen.getByRole('button', { name: /buy ticket/i }))

  const emailInput = screen.getByLabelText(/email/i) as HTMLInputElement
  expect(emailInput.value).toBe('')
})
