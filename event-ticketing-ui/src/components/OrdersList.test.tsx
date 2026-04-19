import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { vi } from 'vitest'
import OrdersList from './OrdersList'

const mockOrders = [
  { id: 10, eventId: 1, email: 'user@example.com', price: 25, bookedAt: '2026-04-18T10:00:00Z' },
  { id: 11, eventId: 2, email: 'user@example.com', price: 40, bookedAt: '2026-04-17T10:00:00Z' },
]

const mockEvent1 = { id: 1, title: 'Jazz Night', date: '2026-08-15T20:00:00', venue: 'Blue Note Club' }
const mockEvent2 = { id: 2, title: 'Rock Fest', date: '2026-09-01T18:00:00', venue: 'City Arena' }

function renderComponent() {
  return render(
    <MemoryRouter>
      <OrdersList />
    </MemoryRouter>
  )
}

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn())
})

afterEach(() => {
  vi.unstubAllGlobals()
})

test('shows email input and look up button initially', () => {
  renderComponent()
  expect(screen.getByRole('textbox', { name: /email/i })).toBeInTheDocument()
  expect(screen.getByRole('button', { name: /look up/i })).toBeInTheDocument()
})

test('shows orders after successful lookup', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockEvent1) } as Response)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockEvent2) } as Response)

  renderComponent()
  await userEvent.type(screen.getByRole('textbox', { name: /email/i }), 'user@example.com')
  await userEvent.click(screen.getByRole('button', { name: /look up/i }))

  await waitFor(() => screen.getByText('Jazz Night'))
  expect(screen.getByText('Rock Fest')).toBeInTheDocument()
})

test('shows venue and date for each order', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockEvent1) } as Response)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockEvent2) } as Response)

  renderComponent()
  await userEvent.type(screen.getByRole('textbox', { name: /email/i }), 'user@example.com')
  await userEvent.click(screen.getByRole('button', { name: /look up/i }))

  await waitFor(() => screen.getByText(/Blue Note Club/))
  expect(screen.getByText(/City Arena/)).toBeInTheDocument()
})

test('shows order id and price', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockEvent1) } as Response)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockEvent2) } as Response)

  renderComponent()
  await userEvent.type(screen.getByRole('textbox', { name: /email/i }), 'user@example.com')
  await userEvent.click(screen.getByRole('button', { name: /look up/i }))

  await waitFor(() => expect(screen.getByText(/Order #10/)).toBeInTheDocument())
  expect(screen.getByText(/\$25/)).toBeInTheDocument()
})

test('shows view confirmation link for each order', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockEvent1) } as Response)
    .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockEvent2) } as Response)

  renderComponent()
  await userEvent.type(screen.getByRole('textbox', { name: /email/i }), 'user@example.com')
  await userEvent.click(screen.getByRole('button', { name: /look up/i }))

  await waitFor(() => screen.getByText('Jazz Night'))
  const links = screen.getAllByRole('link', { name: /view confirmation/i })
  expect(links).toHaveLength(2)
  expect(links[0]).toHaveAttribute('href', '/orders/10')
})

test('shows empty state when no orders found', async () => {
  vi.mocked(fetch).mockResolvedValueOnce({ ok: true, json: () => Promise.resolve([]) } as Response)

  renderComponent()
  await userEvent.type(screen.getByRole('textbox', { name: /email/i }), 'nobody@example.com')
  await userEvent.click(screen.getByRole('button', { name: /look up/i }))

  await waitFor(() => expect(screen.getByText(/no orders found/i)).toBeInTheDocument())
})

test('shows error state on failed fetch', async () => {
  vi.mocked(fetch).mockResolvedValueOnce({ ok: false } as Response)

  renderComponent()
  await userEvent.type(screen.getByRole('textbox', { name: /email/i }), 'user@example.com')
  await userEvent.click(screen.getByRole('button', { name: /look up/i }))

  await waitFor(() => expect(screen.getByText(/something went wrong/i)).toBeInTheDocument())
})

test('fetches orders with encoded email as query param', async () => {
  const fetchMock = vi.mocked(fetch)
  fetchMock.mockResolvedValueOnce({ ok: true, json: () => Promise.resolve([]) } as Response)

  renderComponent()
  await userEvent.type(screen.getByRole('textbox', { name: /email/i }), 'user@example.com')
  await userEvent.click(screen.getByRole('button', { name: /look up/i }))

  await waitFor(() => screen.getByText(/no orders found/i))
  expect(fetchMock).toHaveBeenCalledWith('/api/events/orders?email=user%40example.com')
})
