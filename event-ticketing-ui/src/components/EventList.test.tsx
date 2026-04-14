import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import EventList from './EventList'

const mockEvents = [
  {
    id: 1,
    title: 'Jazz Night',
    description: 'An evening of live jazz music.',
    date: '2026-08-15T20:00:00',
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

  await user.type(screen.getByPlaceholderText('type search term'), 'zzznomatch')

  expect(screen.queryAllByRole('listitem')).toHaveLength(0)
})

test('shows sort type dropdown view', async () => {
  render(<MemoryRouter><EventList /></MemoryRouter>)
  const user = userEvent.setup();

  const sortSelect = await screen.findByDisplayValue(/sort by name/i);
  expect(sortSelect).toBeInTheDocument();

  await user.click(sortSelect);

  const sortByDateOption = await screen.findByRole('option', {
    name: /sort by date/i,
  });
  expect(sortByDateOption).toBeInTheDocument();
});

test('sorts events by price ascending', async () => {
  const user = userEvent.setup()
  render(<MemoryRouter><EventList /></MemoryRouter>)
  await waitFor(() => screen.getByText('Jazz Night'))

  await user.selectOptions(screen.getByRole('combobox'), 'price')

  const items = screen.getAllByRole('listitem')
  expect(items[0]).toHaveTextContent('Jazz Night')    // $25
  expect(items[1]).toHaveTextContent('Tech Conference') // $149
})

test('sorts events by date ascending', async () => {
  const user = userEvent.setup()
  render(<MemoryRouter><EventList /></MemoryRouter>)
  await waitFor(() => screen.getByText('Jazz Night'))

  await user.selectOptions(screen.getByRole('combobox'), 'date')

  const items = screen.getAllByRole('listitem')
  expect(items[0]).toHaveTextContent('Tech Conference') // July
  expect(items[1]).toHaveTextContent('Jazz Night')      // August
})

describe('polling', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  test('polls API again after 30 seconds', async () => {
    render(<MemoryRouter><EventList /></MemoryRouter>)
    await vi.advanceTimersByTimeAsync(10) // flush initial fetch promises

    const callsAfterMount = vi.mocked(fetch).mock.calls.length

    await vi.advanceTimersByTimeAsync(30_000)

    expect(vi.mocked(fetch).mock.calls.length).toBeGreaterThan(callsAfterMount)
  })

  test('updates displayed events when polling returns new data', async () => {
    const updatedEvents = [
      ...mockEvents,
      {
        id: 3,
        title: 'New Event',
        description: 'Just added.',
        date: '2026-09-01T10:00:00',
        venue: 'New Venue',
        totalSeats: 50,
        availableSeats: 50,
        price: 50,
      },
    ]

    vi.mocked(fetch)
      .mockResolvedValueOnce({ json: () => Promise.resolve(mockEvents) } as Response)
      .mockResolvedValue({ json: () => Promise.resolve(updatedEvents) } as Response)

    render(<MemoryRouter><EventList /></MemoryRouter>)
    await vi.advanceTimersByTimeAsync(10) // flush initial fetch promises
    expect(screen.queryByText('New Event')).not.toBeInTheDocument()

    await vi.advanceTimersByTimeAsync(30_000)
    await vi.advanceTimersByTimeAsync(0) // flush fetch promises from interval callback

    expect(screen.getByText('New Event')).toBeInTheDocument()
  })

  test('does not poll after unmount', async () => {
    const { unmount } = render(<MemoryRouter><EventList /></MemoryRouter>)
    await vi.advanceTimersByTimeAsync(10)

    unmount()
    const callsAfterUnmount = vi.mocked(fetch).mock.calls.length

    await vi.advanceTimersByTimeAsync(30_000)

    expect(vi.mocked(fetch).mock.calls.length).toBe(callsAfterUnmount)
  })
})
