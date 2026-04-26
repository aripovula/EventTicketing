import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { vi } from 'vitest'
import AdminSummary from './AdminSummary'

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

const mockRows = [
  { eventId: 2, title: 'Tech Conference 2026', openingBalance: 500, soldSeats: 3, remainingSeats: 497, revenue: 447 },
  { eventId: 1, title: 'Jazz Night', openingBalance: 100, soldSeats: 10, remainingSeats: 90, revenue: 250 },
]

function renderComponent() {
  return render(
    <MemoryRouter initialEntries={['/admin/summary']}>
      <Routes>
        <Route path="/admin/summary" element={<AdminSummary />} />
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
  vi.mocked(fetch).mockResolvedValue({ ok: true, json: () => Promise.resolve(mockRows) } as Response)
  renderComponent()
  expect(screen.getByText('Loading...')).toBeInTheDocument()
})

test('shows "as of" timestamp after fetch', async () => {
  vi.mocked(fetch).mockResolvedValue({ ok: true, json: () => Promise.resolve(mockRows) } as Response)
  renderComponent()
  await waitFor(() => screen.getByText('Jazz Night'))
  expect(screen.getByText(/data as of/i)).toBeInTheDocument()
})

test('shows event names after fetch', async () => {
  vi.mocked(fetch).mockResolvedValue({ ok: true, json: () => Promise.resolve(mockRows) } as Response)
  renderComponent()
  await waitFor(() => screen.getByText('Jazz Night'))
  expect(screen.getByText('Tech Conference 2026')).toBeInTheDocument()
})

test('shows opening balance, sold and remaining seats for each event', async () => {
  vi.mocked(fetch).mockResolvedValue({ ok: true, json: () => Promise.resolve(mockRows) } as Response)
  renderComponent()
  await waitFor(() => screen.getByText('Jazz Night'))
  expect(screen.getByText('100')).toBeInTheDocument()  // openingBalance
  expect(screen.getByText('10')).toBeInTheDocument()   // soldSeats
  expect(screen.getByText('90')).toBeInTheDocument()   // remainingSeats
})

test('shows revenue for each event', async () => {
  vi.mocked(fetch).mockResolvedValue({ ok: true, json: () => Promise.resolve(mockRows) } as Response)
  renderComponent()
  await waitFor(() => screen.getByText('Jazz Night'))
  expect(screen.getByText('$250')).toBeInTheDocument()
  expect(screen.getByText('$447')).toBeInTheDocument()
})

test('shows column headers', async () => {
  vi.mocked(fetch).mockResolvedValue({ ok: true, json: () => Promise.resolve(mockRows) } as Response)
  renderComponent()
  await waitFor(() => screen.getByText('Jazz Night'))
  expect(screen.getByText('Opening balance')).toBeInTheDocument()
  expect(screen.getByText('Sold')).toBeInTheDocument()
  expect(screen.getByText('Remaining')).toBeInTheDocument()
  expect(screen.getByText('Revenue')).toBeInTheDocument()
})

test('shows error state on failed fetch', async () => {
  vi.mocked(fetch).mockResolvedValue({ ok: false } as Response)
  renderComponent()
  await waitFor(() => expect(screen.getByText('Failed to load summary.')).toBeInTheDocument())
})

test('shows back to admin link', () => {
  vi.mocked(fetch).mockResolvedValue({ ok: true, json: () => Promise.resolve(mockRows) } as Response)
  renderComponent()
  expect(screen.getByRole('link', { name: /back to admin/i })).toBeInTheDocument()
})

test('fetches from /api/admin/summary', async () => {
  const fetchMock = vi.mocked(fetch)
  fetchMock.mockResolvedValue({ ok: true, json: () => Promise.resolve([]) } as Response)
  renderComponent()
  await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/admin/summary'))
})
