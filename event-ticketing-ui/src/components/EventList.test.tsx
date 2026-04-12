import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import EventList from './EventList'

const mockEvents = [
  {
    id: 1,
    title: 'Jazz Night',
    description: 'An evening of live jazz music.',
    date: '2026-06-15T20:00:00',
    venue: 'Blue Note Club',
    totalSeats: 100,
    availableSeats: 100,
    price: 25,
  },
]

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
    json: () => Promise.resolve(mockEvents),
  }))
})

afterEach(() => {
  vi.unstubAllGlobals()
})

test('shows loading state initially', () => {
  render(<MemoryRouter><EventList /></MemoryRouter>)
  expect(screen.getByText('Loading events...')).toBeInTheDocument()
})

test('renders events after fetch', async () => {
  render(<MemoryRouter><EventList /></MemoryRouter>)
  await waitFor(() => expect(screen.getByText('Jazz Night')).toBeInTheDocument())
  expect(screen.getByText(/Blue Note Club/)).toBeInTheDocument()
  expect(screen.getByText(/25/)).toBeInTheDocument()
})
