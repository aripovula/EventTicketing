import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import '@testing-library/jest-dom'
import { vi } from 'vitest'
import EventDetail from './EventDetail'

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

function renderWithRoute(id: string) {
  return render(
    <MemoryRouter initialEntries={[`/events/${id}`]}>
      <Routes>
        <Route path="/events/:id" element={<EventDetail />} />
      </Routes>
    </MemoryRouter>
  )
}

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn())
})

afterEach(() => {
  vi.unstubAllGlobals()
  document.title = 'Event Ticketing'
})

test('shows loading state initially', () => {
  vi.mocked(fetch).mockResolvedValue({ status: 200, json: () => Promise.resolve(mockEvent) } as Response)
  renderWithRoute('1')
  expect(screen.getByText('Loading...')).toBeInTheDocument()
})

test('renders event details after fetch', async () => {
  vi.mocked(fetch).mockResolvedValue({ status: 200, json: () => Promise.resolve(mockEvent) } as Response)
  renderWithRoute('1')
  await waitFor(() => expect(screen.getByText('Jazz Night')).toBeInTheDocument())
  expect(screen.getByText('An evening of live jazz music.')).toBeInTheDocument()
  expect(screen.getByText(/Blue Note Club/)).toBeInTheDocument()
})

test('shows not found message for unknown event', async () => {
  vi.mocked(fetch).mockResolvedValue({ status: 404, json: () => Promise.resolve(null) } as Response)
  renderWithRoute('999')
  await waitFor(() => expect(screen.getByText('Event not found.')).toBeInTheDocument())
  expect(document.title).toBe('Ticketing')
})

test('sets document title to event title after loading', async () => {
  vi.mocked(fetch).mockResolvedValue({ status: 200, json: () => Promise.resolve(mockEvent) } as Response)
  renderWithRoute('1')
  await waitFor(() => expect(document.title).toBe('Jazz Night | Ticketing'))
})

test('shows error state when fetch fails', async () => {
  vi.mocked(fetch).mockRejectedValue(new Error('Network error'))
  renderWithRoute('1')
  await waitFor(() => expect(screen.getByText('Something went wrong. Please try again.')).toBeInTheDocument())
})

test('shows back to events link', async () => {
  vi.mocked(fetch).mockResolvedValue({ status: 200, json: () => Promise.resolve(mockEvent) } as Response)
  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  expect(screen.getByRole('link', { name: /back to events/i })).toBeInTheDocument()
})

test('displays correct seat availability', async () => {
  vi.mocked(fetch).mockResolvedValue({ status: 200, json: () => Promise.resolve(mockEvent) } as Response)
  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  expect(screen.getByText(/80 of 100 seats available/)).toBeInTheDocument()
})

test('resets document title on unmount', async () => {
  vi.mocked(fetch).mockResolvedValue({
    status: 200,
    json: () => Promise.resolve(mockEvent),
  } as Response)

  const { unmount } = renderWithRoute('1')

  await waitFor(() =>
    expect(screen.getByText('Jazz Night')).toBeInTheDocument()
  )

  expect(document.title).toBe('Jazz Night | Ticketing')

  unmount()

  expect(document.title).toBe('Event Ticketing')
})