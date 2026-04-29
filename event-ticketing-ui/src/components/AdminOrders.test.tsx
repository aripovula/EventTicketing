import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { vi } from 'vitest'
import AdminOrders from './AdminOrders'

vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: function () {
    return {
      withUrl:               function () { return this },
      withAutomaticReconnect: function () { return this },
      configureLogging:      function () { return this },
      build: function () {
        return { on: function () {}, start: function () { return Promise.resolve() }, stop: function () { return Promise.resolve() } }
      },
    }
  },
  LogLevel: { Warning: 2 },
}))

const mockOrders = [
  {
    id: 10,
    userId: 4,
    email: 'alice@example.com',
    price: 25,
    bookedAt: '2026-04-18T10:00:00Z',
    event: { title: 'Jazz Night' },
  },
  {
    id: 11,
    userId: 5,
    email: 'bob@example.com',
    price: 149,
    bookedAt: '2026-04-17T09:00:00Z',
    event: { title: 'Tech Conference 2026' },
  },
]

function renderComponent() {
  return render(
    <MemoryRouter initialEntries={['/admin/orders']}>
      <Routes>
        <Route path="/admin/orders" element={<AdminOrders />} />
        <Route path="/admin" element={<div>Admin list</div>} />
      </Routes>
    </MemoryRouter>
  )
}

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn())
})

afterEach(() => {
  vi.unstubAllGlobals()
})

test('shows loading state initially', () => {
  vi.mocked(fetch).mockResolvedValue({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
  renderComponent()
  expect(screen.getByText('Loading...')).toBeInTheDocument()
})

test('shows all orders after fetch', async () => {
  vi.mocked(fetch).mockResolvedValue({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
  renderComponent()
  await waitFor(() => screen.getByText('alice@example.com'))
  expect(screen.getByText('bob@example.com')).toBeInTheDocument()
})

test('shows userId for each order', async () => {
  vi.mocked(fetch).mockResolvedValue({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
  renderComponent()
  await waitFor(() => screen.getByText('4'))
  expect(screen.getByText('4')).toBeInTheDocument()
})

test('shows email for each order', async () => {
  vi.mocked(fetch).mockResolvedValue({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
  renderComponent()
  await waitFor(() => screen.getByText('alice@example.com'))
  expect(screen.getByText('bob@example.com')).toBeInTheDocument()
})

test('shows price for each order', async () => {
  vi.mocked(fetch).mockResolvedValue({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
  renderComponent()
  await waitFor(() => screen.getByText('$25'))
  expect(screen.getByText('$149')).toBeInTheDocument()
})

test('shows order ids', async () => {
  vi.mocked(fetch).mockResolvedValue({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
  renderComponent()
  await waitFor(() => expect(screen.getByText('#10')).toBeInTheDocument())
  expect(screen.getByText('#11')).toBeInTheDocument()
})

test('shows event filter dropdown with all event options', async () => {
  vi.mocked(fetch).mockResolvedValue({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
  renderComponent()
  await waitFor(() => screen.getByText('alice@example.com'))

  const dropdown = screen.getByRole('combobox', { name: /filter by event/i })
  expect(dropdown).toBeInTheDocument()
  expect(screen.getByRole('option', { name: 'All events' })).toBeInTheDocument()
  expect(screen.getByRole('option', { name: 'Jazz Night' })).toBeInTheDocument()
  expect(screen.getByRole('option', { name: 'Tech Conference 2026' })).toBeInTheDocument()
})

test('selecting an event filters the table', async () => {
  const user = (await import('@testing-library/user-event')).default.setup()
  vi.mocked(fetch).mockResolvedValue({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
  renderComponent()
  await waitFor(() => screen.getByText('alice@example.com'))

  await user.selectOptions(screen.getByRole('combobox', { name: /filter by event/i }), 'Jazz Night')

  expect(screen.getByText('alice@example.com')).toBeInTheDocument()
  expect(screen.queryByText('bob@example.com')).not.toBeInTheDocument()
})

test('selecting All events shows all orders', async () => {
  const user = (await import('@testing-library/user-event')).default.setup()
  vi.mocked(fetch).mockResolvedValue({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
  renderComponent()
  await waitFor(() => screen.getByText('alice@example.com'))

  await user.selectOptions(screen.getByRole('combobox', { name: /filter by event/i }), 'Jazz Night')
  await user.selectOptions(screen.getByRole('combobox', { name: /filter by event/i }), '')

  expect(screen.getByText('alice@example.com')).toBeInTheDocument()
  expect(screen.getByText('bob@example.com')).toBeInTheDocument()
})

test('shows empty state when no orders', async () => {
  vi.mocked(fetch).mockResolvedValue({ ok: true, json: () => Promise.resolve([]) } as Response)
  renderComponent()
  await waitFor(() => expect(screen.getByText('No orders yet.')).toBeInTheDocument())
})

test('shows error state on failed fetch', async () => {
  vi.mocked(fetch).mockResolvedValue({ ok: false } as Response)
  renderComponent()
  await waitFor(() => expect(screen.getByText('Failed to load orders.')).toBeInTheDocument())
})

test('shows back to admin link', async () => {
  vi.mocked(fetch).mockResolvedValue({ ok: true, json: () => Promise.resolve(mockOrders) } as Response)
  renderComponent()
  expect(screen.getByRole('link', { name: /back to admin/i })).toBeInTheDocument()
})

test('fetches from /api/admin/orders', async () => {
  const fetchMock = vi.mocked(fetch)
  fetchMock.mockResolvedValue({ ok: true, json: () => Promise.resolve([]) } as Response)
  renderComponent()
  await waitFor(() => screen.getByText('No orders yet.'))
  expect(fetchMock).toHaveBeenCalledWith('/api/admin/orders')
})
