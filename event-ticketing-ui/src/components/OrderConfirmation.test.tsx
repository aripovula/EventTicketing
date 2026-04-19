import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { vi } from 'vitest'
import OrderConfirmation from './OrderConfirmation'

const mockOrder = {
  id: 42,
  eventId: 1,
  email: 'user@example.com',
  price: 25,
  bookedAt: '2026-04-18T10:00:00Z',
}

const mockEvent = {
  id: 1,
  title: 'Jazz Night',
  date: '2026-08-15T20:00:00',
  venue: 'Blue Note Club',
}

function renderWithRoute(orderId = '42') {
  return render(
    <MemoryRouter initialEntries={[`/orders/${orderId}`]}>
      <Routes>
        <Route path="/orders/:id" element={<OrderConfirmation />} />
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
  vi.mocked(fetch)
    .mockResolvedValueOnce({ status: 200, json: () => Promise.resolve(mockOrder) } as Response)
    .mockResolvedValueOnce({ status: 200, json: () => Promise.resolve(mockEvent) } as Response)
  renderWithRoute()
  expect(screen.getByText('Loading…')).toBeInTheDocument()
})

test('shows confirmation heading after successful fetch', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce({ status: 200, json: () => Promise.resolve(mockOrder) } as Response)
    .mockResolvedValueOnce({ status: 200, json: () => Promise.resolve(mockEvent) } as Response)
  renderWithRoute()
  await waitFor(() => expect(screen.getByRole('heading', { name: /you're going/i })).toBeInTheDocument())
})

test('shows event title and venue', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce({ status: 200, json: () => Promise.resolve(mockOrder) } as Response)
    .mockResolvedValueOnce({ status: 200, json: () => Promise.resolve(mockEvent) } as Response)
  renderWithRoute()
  await waitFor(() => screen.getByText('Jazz Night'))
  expect(screen.getByText(/Blue Note Club/)).toBeInTheDocument()
})

test('shows confirmation email', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce({ status: 200, json: () => Promise.resolve(mockOrder) } as Response)
    .mockResolvedValueOnce({ status: 200, json: () => Promise.resolve(mockEvent) } as Response)
  renderWithRoute()
  await waitFor(() => screen.getByText('user@example.com'))
})

test('shows order id and price', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce({ status: 200, json: () => Promise.resolve(mockOrder) } as Response)
    .mockResolvedValueOnce({ status: 200, json: () => Promise.resolve(mockEvent) } as Response)
  renderWithRoute()
  await waitFor(() => expect(screen.getByText(/Order #42/)).toBeInTheDocument())
  expect(screen.getByText(/\$25/)).toBeInTheDocument()
})

test('fetches order then event using eventId', async () => {
  const fetchMock = vi.mocked(fetch)
  fetchMock
    .mockResolvedValueOnce({ status: 200, json: () => Promise.resolve(mockOrder) } as Response)
    .mockResolvedValueOnce({ status: 200, json: () => Promise.resolve(mockEvent) } as Response)
  renderWithRoute()
  await waitFor(() => screen.getByText('Jazz Night'))
  expect(fetchMock).toHaveBeenNthCalledWith(1, '/api/events/orders/42')
  expect(fetchMock).toHaveBeenNthCalledWith(2, '/api/events/1')
})

test('shows not found message on 404', async () => {
  vi.mocked(fetch).mockResolvedValueOnce({ status: 404 } as Response)
  renderWithRoute('999')
  await waitFor(() => expect(screen.getByText('Order not found.')).toBeInTheDocument())
})

test('shows back to events link', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce({ status: 200, json: () => Promise.resolve(mockOrder) } as Response)
    .mockResolvedValueOnce({ status: 200, json: () => Promise.resolve(mockEvent) } as Response)
  renderWithRoute()
  await waitFor(() => screen.getByText('Jazz Night'))
  expect(screen.getByRole('link', { name: /browse more events/i })).toBeInTheDocument()
})
