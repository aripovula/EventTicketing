import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
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
  {
    id: 2,
    title: 'Tech Conference',
    description: 'A full-day conference on modern software development.',
    date: '2026-07-10T09:00:00',
    venue: 'City Convention Centre',
    totalSeats: 500,
    availableSeats: 500,
    price: 149,
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

test('shows filter by text input field', () => {
  render(<MemoryRouter><EventList /></MemoryRouter>)
  expect(screen.findByPlaceholderText('type search term'))
})


test('renders events after fetch', async () => {
  render(<MemoryRouter><EventList /></MemoryRouter>)
  await waitFor(() => expect(screen.getByText('Jazz Night')).toBeInTheDocument())
  expect(screen.getByText(/Blue Note Club/)).toBeInTheDocument()
  expect(screen.getByText(/25/)).toBeInTheDocument()
})

test('filters events by search term', async () => {
  const user = userEvent.setup()
  render(<MemoryRouter><EventList /></MemoryRouter>)
  await waitFor(() => screen.getByText('Jazz Night'))

  await user.type(screen.getByPlaceholderText('type search term'), 'jazz')

  expect(screen.getByText('Jazz Night')).toBeInTheDocument()
  expect(screen.queryByText('Tech Conference')).not.toBeInTheDocument()
})

test('search filtering is case-insensitive', async () => {
  const user = userEvent.setup()
  render(<MemoryRouter><EventList /></MemoryRouter>)
  await waitFor(() => screen.getByText('Jazz Night'))

  await user.type(screen.getByPlaceholderText('type search term'), 'TECH')

  expect(screen.getByText('Tech Conference')).toBeInTheDocument()
  expect(screen.queryByText('Jazz Night')).not.toBeInTheDocument()
})

test('shows no events when search term matches nothing', async () => {
  const user = userEvent.setup()
  render(<MemoryRouter><EventList /></MemoryRouter>)
  await waitFor(() => screen.getByText('Jazz Night'))

  await user.type(screen.getByPlaceholderText('type search term'), 'zzz')

  expect(screen.queryAllByRole('listitem')).toHaveLength(0)
})

