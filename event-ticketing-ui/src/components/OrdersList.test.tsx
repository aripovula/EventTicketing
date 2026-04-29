import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { vi } from 'vitest'
import OrdersList from './OrdersList'
import { AuthProvider } from '../context/AuthContext'

const mockOrders = [
  { id: 10, eventId: 1, email: 'user@example.com', price: 25, bookedAt: '2026-04-18T10:00:00Z' },
  { id: 11, eventId: 2, email: 'user@example.com', price: 40, bookedAt: '2026-04-17T10:00:00Z' },
]

const mockOrdersWithEvent = [
  { id: 10, eventId: 1, email: 'john@example.com', price: 25, bookedAt: '2026-04-18T10:00:00Z',
    event: { id: 1, title: 'Jazz Night',  startTime: '2026-08-15T20:00:00', venue: 'Blue Note Club' } },
  { id: 11, eventId: 2, email: 'john@example.com', price: 40, bookedAt: '2026-04-17T10:00:00Z',
    event: { id: 2, title: 'Rock Fest', startTime: '2026-09-01T18:00:00', venue: 'City Arena' } },
]

const mockEvent1 = { id: 1, title: 'Jazz Night',  startTime: '2026-08-15T20:00:00', venue: 'Blue Note Club' }
const mockEvent2 = { id: 2, title: 'Rock Fest', startTime: '2026-09-01T18:00:00', venue: 'City Arena' }

const johnUser = { userId: 1, name: 'John Doe', email: 'john@example.com', role: 'user' }

const notLoggedIn = { ok: false, status: 401, json: () => Promise.resolve(null) } as Response

function renderComponent() {
  return render(
    <AuthProvider>
      <MemoryRouter>
        <OrdersList />
      </MemoryRouter>
    </AuthProvider>
  )
}

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn())
})

afterEach(() => {
  vi.unstubAllGlobals()
})

// ── Guest (not logged in) ──────────────────────────────────────────────────────
// Fetch order: AuthProvider → GET /api/auth/me (401), then email form submit

test('shows email input and look up button for guest', async () => {
  vi.mocked(fetch).mockResolvedValue(notLoggedIn)
  renderComponent()
  await waitFor(() => expect(screen.getByRole('textbox', { name: /email/i })).toBeInTheDocument())
  expect(screen.getByRole('button', { name: /look up/i })).toBeInTheDocument()
})

test('shows orders after successful lookup', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce(notLoggedIn)                                                               // /me
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)        // orders by email
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockEvent1) } as Response)        // event 1
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockEvent2) } as Response)        // event 2

  renderComponent()
  await waitFor(() => screen.getByRole('textbox', { name: /email/i }))
  await userEvent.type(screen.getByRole('textbox', { name: /email/i }), 'user@example.com')
  await userEvent.click(screen.getByRole('button', { name: /look up/i }))

  await waitFor(() => screen.getByText('Jazz Night'))
  expect(screen.getByText('Rock Fest')).toBeInTheDocument()
})

test('shows venue and date for each order', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce(notLoggedIn)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockEvent1) } as Response)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockEvent2) } as Response)

  renderComponent()
  await waitFor(() => screen.getByRole('textbox', { name: /email/i }))
  await userEvent.type(screen.getByRole('textbox', { name: /email/i }), 'user@example.com')
  await userEvent.click(screen.getByRole('button', { name: /look up/i }))

  await waitFor(() => screen.getByText(/Blue Note Club/))
  expect(screen.getByText(/City Arena/)).toBeInTheDocument()
})

test('shows order id and price', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce(notLoggedIn)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockEvent1) } as Response)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockEvent2) } as Response)

  renderComponent()
  await waitFor(() => screen.getByRole('textbox', { name: /email/i }))
  await userEvent.type(screen.getByRole('textbox', { name: /email/i }), 'user@example.com')
  await userEvent.click(screen.getByRole('button', { name: /look up/i }))

  await waitFor(() => expect(screen.getByText(/Order #10/)).toBeInTheDocument())
  expect(screen.getByText(/\$25/)).toBeInTheDocument()
})

test('shows view confirmation link for each order', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce(notLoggedIn)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockEvent1) } as Response)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockEvent2) } as Response)

  renderComponent()
  await waitFor(() => screen.getByRole('textbox', { name: /email/i }))
  await userEvent.type(screen.getByRole('textbox', { name: /email/i }), 'user@example.com')
  await userEvent.click(screen.getByRole('button', { name: /look up/i }))

  await waitFor(() => screen.getByText('Jazz Night'))
  const links = screen.getAllByRole('link', { name: /view confirmation/i })
  expect(links).toHaveLength(2)
  expect(links[0]).toHaveAttribute('href', '/orders/10')
})

test('shows empty state when no orders found', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce(notLoggedIn)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve([]) } as Response)

  renderComponent()
  await waitFor(() => screen.getByRole('textbox', { name: /email/i }))
  await userEvent.type(screen.getByRole('textbox', { name: /email/i }), 'nobody@example.com')
  await userEvent.click(screen.getByRole('button', { name: /look up/i }))

  await waitFor(() => expect(screen.getByText(/no orders found/i)).toBeInTheDocument())
})

test('shows error state on failed fetch', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce(notLoggedIn)
    .mockResolvedValueOnce({ ok: false } as Response)

  renderComponent()
  await waitFor(() => screen.getByRole('textbox', { name: /email/i }))
  await userEvent.type(screen.getByRole('textbox', { name: /email/i }), 'user@example.com')
  await userEvent.click(screen.getByRole('button', { name: /look up/i }))

  await waitFor(() => expect(screen.getByText(/something went wrong/i)).toBeInTheDocument())
})

test('fetches orders with encoded email as query param', async () => {
  const fetchMock = vi.mocked(fetch)
  fetchMock
    .mockResolvedValueOnce(notLoggedIn)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve([]) } as Response)

  renderComponent()
  await waitFor(() => screen.getByRole('textbox', { name: /email/i }))
  await userEvent.type(screen.getByRole('textbox', { name: /email/i }), 'user@example.com')
  await userEvent.click(screen.getByRole('button', { name: /look up/i }))

  await waitFor(() => screen.getByText(/no orders found/i))
  expect(fetchMock).toHaveBeenCalledWith('/api/events/orders?email=user%40example.com')
})

// ── Logged-in user ─────────────────────────────────────────────────────────────
// Fetch order: AuthProvider → GET /api/auth/me (200+user), then auto-fetch /api/auth/me/orders

test('hides email form for logged-in user', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce({ ok: true, status: 200, json: () => Promise.resolve(johnUser) } as Response)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockOrdersWithEvent) } as Response)

  renderComponent()
  await waitFor(() => screen.getByText('Jazz Night'))

  expect(screen.queryByRole('textbox', { name: /email/i })).not.toBeInTheDocument()
  expect(screen.queryByRole('button', { name: /look up/i })).not.toBeInTheDocument()
})

test('auto-fetches orders from /api/auth/me/orders for logged-in user', async () => {
  const fetchMock = vi.mocked(fetch)
  fetchMock
    .mockResolvedValueOnce({ ok: true, status: 200, json: () => Promise.resolve(johnUser) } as Response)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockOrdersWithEvent) } as Response)

  renderComponent()
  await waitFor(() => screen.getByText('Jazz Night'))

  expect(fetchMock).toHaveBeenCalledWith('/api/auth/me/orders')
})

test('shows orders with event details for logged-in user', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce({ ok: true, status: 200, json: () => Promise.resolve(johnUser) } as Response)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockOrdersWithEvent) } as Response)

  renderComponent()
  await waitFor(() => screen.getByText('Jazz Night'))

  expect(screen.getByText('Rock Fest')).toBeInTheDocument()
  expect(screen.getByText(/Blue Note Club/)).toBeInTheDocument()
  expect(screen.getByText(/Order #10/)).toBeInTheDocument()
})

test('shows empty state when logged-in user has no orders', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce({ ok: true, status: 200, json: () => Promise.resolve(johnUser) } as Response)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve([]) } as Response)

  renderComponent()
  await waitFor(() => expect(screen.getByText(/no orders found/i)).toBeInTheDocument())
})

// ── startTime display ──────────────────────────────────────────────────────────

test('displays event date formatted from startTime for logged-in user', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce({ ok: true, status: 200, json: () => Promise.resolve(johnUser) } as Response)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockOrdersWithEvent) } as Response)

  renderComponent()
  await waitFor(() => screen.getByText('Jazz Night'))
  // Aug 15 2026 should appear somewhere in the order card
  const items = screen.getAllByRole('listitem')
  expect(items[0].textContent).toMatch(/Aug|August/)
})

test('displays event date formatted from startTime for guest lookup', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce(notLoggedIn)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockEvent1) } as Response)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockEvent2) } as Response)

  renderComponent()
  await waitFor(() => screen.getByRole('textbox', { name: /email/i }))
  await userEvent.type(screen.getByRole('textbox', { name: /email/i }), 'user@example.com')
  await userEvent.click(screen.getByRole('button', { name: /look up/i }))

  await waitFor(() => screen.getByText('Jazz Night'))
  const items = screen.getAllByRole('listitem')
  expect(items[0].textContent).toMatch(/Aug|August/)
})
