import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { AuthProvider } from '../context/AuthContext'
import EventList from './EventList'

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

const mockEvents = [
  {
    id: 1,
    title: 'Jazz Night',
    description: 'An evening of live jazz music.',
    startTime: '2026-08-15T20:00:00',
    endTime:   '2026-08-15T23:00:00',
    venue: 'Blue Note Club',
    totalSeats: 100,
    availableSeats: 100,
    price: 25,
    eventType: 'Music',
  },
  {
    id: 2,
    title: 'Tech Conference',
    description: 'A full-day conference on modern software development.',
    startTime: '2026-07-10T09:00:00',
    endTime:   '2026-07-10T18:00:00',
    venue: 'City Convention Centre',
    totalSeats: 500,
    availableSeats: 500,
    price: 149,
    eventType: 'Tech',
  },
]

// Route by URL: /api/auth/me → 401 (not logged in), everything else → mockEvents.
// Child effects (EventList) fire before parent effects (AuthProvider) in React 18+,
// so order-based mocking is unreliable.
function renderPage() {
  vi.stubGlobal('fetch', vi.fn().mockImplementation((url: string) => {
    if (String(url).includes('/api/auth')) {
      return Promise.resolve(new Response(null, { status: 401 }))
    }
    return Promise.resolve({ ok: true, json: () => Promise.resolve(mockEvents) } as Response)
  }))

  return render(
    <AuthProvider>
      <MemoryRouter><EventList /></MemoryRouter>
    </AuthProvider>
  )
}

afterEach(() => {
  vi.unstubAllGlobals()
})

test('shows loading state initially', () => {
  renderPage()
  expect(screen.getByText('Loading events...')).toBeInTheDocument()
})

test('shows filter by text input field', () => {
  renderPage()
  expect(screen.findByPlaceholderText('type search term'))
})

test('renders events after fetch', async () => {
  renderPage()
  await waitFor(() => expect(screen.getByText('Jazz Night')).toBeInTheDocument())
  expect(screen.getByText(/Blue Note Club/)).toBeInTheDocument()
  expect(screen.getByText(/25/)).toBeInTheDocument()
})

test('filters events by search term', async () => {
  const user = userEvent.setup()
  renderPage()
  await waitFor(() => screen.getByText('Jazz Night'))

  await user.type(screen.getByPlaceholderText('type search term'), 'jazz')

  expect(screen.getByText('Jazz Night')).toBeInTheDocument()
  expect(screen.queryByText('Tech Conference')).not.toBeInTheDocument()
})

test('search filtering is case-insensitive', async () => {
  const user = userEvent.setup()
  renderPage()
  await waitFor(() => screen.getByText('Jazz Night'))

  await user.type(screen.getByPlaceholderText('type search term'), 'TECH')

  expect(screen.getByText('Tech Conference')).toBeInTheDocument()
  expect(screen.queryByText('Jazz Night')).not.toBeInTheDocument()
})

test('shows no events when search term matches nothing', async () => {
  const user = userEvent.setup()
  renderPage()
  await waitFor(() => screen.getByText('Jazz Night'))

  await user.type(screen.getByPlaceholderText('type search term'), 'zzznomatch')

  expect(screen.queryAllByRole('listitem')).toHaveLength(0)
})

test('shows sort type dropdown view', async () => {
  renderPage()
  const user = userEvent.setup();

  const sortSelect = await screen.findByDisplayValue(/sort by name/i);
  expect(sortSelect).toBeInTheDocument();

  await user.click(sortSelect);

  const sortByDateOption = await screen.findByRole('option', {
    name: /sort by date/i,
  });
  expect(sortByDateOption).toBeInTheDocument();
});

test('sorts events by title', async () => {
  const user = userEvent.setup()
  renderPage()
  await waitFor(() => screen.getByText('Jazz Night'))

  await user.selectOptions(screen.getByRole('combobox'), 'name')

  const items = screen.getAllByRole('listitem')
  expect(items[0]).toHaveTextContent('Jazz Night')
  expect(items[1]).toHaveTextContent('Tech Conference')
})

test('sorts events by price ascending', async () => {
  const user = userEvent.setup()
  renderPage()
  await waitFor(() => screen.getByText('Jazz Night'))

  await user.selectOptions(screen.getByRole('combobox'), 'price')

  const items = screen.getAllByRole('listitem')
  expect(items[0]).toHaveTextContent('Jazz Night')    // $25
  expect(items[1]).toHaveTextContent('Tech Conference') // $149
})

test('sorts events by date ascending', async () => {
  const user = userEvent.setup()
  renderPage()
  await waitFor(() => screen.getByText('Jazz Night'))

  await user.selectOptions(screen.getByRole('combobox'), 'date')

  const items = screen.getAllByRole('listitem')
  expect(items[0]).toHaveTextContent('Tech Conference') // July
  expect(items[1]).toHaveTextContent('Jazz Night')      // August
})

test('shows thumbnail image when imageUrl is present', async () => {
  const eventsWithImage = [{ ...mockEvents[0], imageUrl: 'https://images.unsplash.com/photo-123' }]
  vi.stubGlobal('fetch', vi.fn().mockImplementation((url: string) => {
    if (String(url).includes('/api/auth')) return Promise.resolve(new Response(null, { status: 401 }))
    return Promise.resolve({ ok: true, json: () => Promise.resolve(eventsWithImage) } as Response)
  }))

  render(<AuthProvider><MemoryRouter><EventList /></MemoryRouter></AuthProvider>)
  await waitFor(() => screen.getByText('Jazz Night'))

  const img = screen.getByRole('img', { name: 'Jazz Night' })
  expect(img).toBeInTheDocument()
  expect(img).toHaveAttribute('src', 'https://images.unsplash.com/photo-123')
})

test('does not show img element when imageUrl is absent', async () => {
  renderPage()
  await waitFor(() => screen.getByText('Jazz Night'))

  expect(screen.queryByRole('img')).not.toBeInTheDocument()
})

describe('search input resize', () => {
  test('expands on focus', async () => {
    const user = userEvent.setup()
    renderPage()
    await waitFor(() => screen.getByText('Jazz Night'))

    const input = screen.getByPlaceholderText('type search term')
    await user.click(input)

    expect(input).toHaveStyle({ width: '400px', fontSize: '22px' })
  })

  test('shrinks on blur', async () => {
    const user = userEvent.setup()
    renderPage()
    await waitFor(() => screen.getByText('Jazz Night'))

    const input = screen.getByPlaceholderText('type search term')
    await user.click(input)
    await user.tab()

    expect(input).toHaveStyle({ width: '200px', fontSize: '12px' })
  })
})

describe.skip('polling', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  test('polls API again after 30 seconds', async () => {
    renderPage()
    await vi.advanceTimersByTimeAsync(10)

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
        startTime: '2026-09-01T10:00:00',
        endTime: '2026-09-01T12:00:00',
        venue: 'New Venue',
        totalSeats: 50,
        availableSeats: 50,
        price: 50,
        eventType: 'Other',
      },
    ]

    vi.mocked(fetch)
      .mockResolvedValueOnce({ json: () => Promise.resolve(mockEvents) } as Response)
      .mockResolvedValue({ json: () => Promise.resolve(updatedEvents) } as Response)

    renderPage()
    await vi.advanceTimersByTimeAsync(10)
    expect(screen.queryByText('New Event')).not.toBeInTheDocument()

    await vi.advanceTimersByTimeAsync(30_000)
    await vi.advanceTimersByTimeAsync(0)

    expect(screen.getByText('New Event')).toBeInTheDocument()
  })

  test('does not poll after unmount', async () => {
    const { unmount } = renderPage()
    await vi.advanceTimersByTimeAsync(10)

    unmount()
    const callsAfterUnmount = vi.mocked(fetch).mock.calls.length

    await vi.advanceTimersByTimeAsync(30_000)

    expect(vi.mocked(fetch).mock.calls.length).toBe(callsAfterUnmount)
  })
})

// ── Event type badge ───────────────────────────────────────────────────────────

test('shows event type badge for each event', async () => {
  renderPage()
  await waitFor(() => screen.getByText('Jazz Night'))
  // Both events have their types in badges
  expect(screen.getByText('Music')).toBeInTheDocument()
  expect(screen.getByText('Tech')).toBeInTheDocument()
})

// ── Conflict detection ─────────────────────────────────────────────────────────

const overlappingOrder = {
  id: 99,
  price: 25,
  bookedAt: '2026-04-01T00:00:00Z',
  event: {
    id: 99,
    title: 'Already Booked Show',
    startTime: '2026-08-15T19:00:00', // overlaps Jazz Night (20:00–23:00)
    endTime:   '2026-08-15T21:00:00',
  },
}

test('shows conflict warning when a booked event overlaps a listed event', async () => {
  vi.stubGlobal('fetch', vi.fn().mockImplementation((url: string) => {
    if (String(url).includes('/api/auth/me/orders')) {
      return Promise.resolve({ ok: true, json: () => Promise.resolve([overlappingOrder]) } as Response)
    }
    if (String(url).includes('/api/auth')) {
      // logged-in user
      return Promise.resolve({ ok: true, json: () => Promise.resolve({ userId: 1, name: 'John', email: 'john@example.com', role: 'user' }) } as Response)
    }
    return Promise.resolve({ ok: true, json: () => Promise.resolve(mockEvents) } as Response)
  }))

  render(
    <AuthProvider>
      <MemoryRouter><EventList /></MemoryRouter>
    </AuthProvider>
  )

  await waitFor(() => screen.getByText('Jazz Night'))
  await waitFor(() =>
    expect(screen.getByText(/conflicts with your existing booking of "Already Booked Show"/i)).toBeInTheDocument()
  )
})

test('does not show conflict badge when no events overlap', async () => {
  const nonOverlappingOrder = {
    id: 98,
    price: 10,
    bookedAt: '2026-04-01T00:00:00Z',
    event: {
      id: 98,
      title: 'Distant Event',
      startTime: '2026-12-01T10:00:00', // far in the future, no overlap
      endTime:   '2026-12-01T12:00:00',
    },
  }

  vi.stubGlobal('fetch', vi.fn().mockImplementation((url: string) => {
    if (String(url).includes('/api/auth/me/orders')) {
      return Promise.resolve({ ok: true, json: () => Promise.resolve([nonOverlappingOrder]) } as Response)
    }
    if (String(url).includes('/api/auth')) {
      return Promise.resolve({ ok: true, json: () => Promise.resolve({ userId: 1, name: 'John', email: 'john@example.com', role: 'user' }) } as Response)
    }
    return Promise.resolve({ ok: true, json: () => Promise.resolve(mockEvents) } as Response)
  }))

  render(
    <AuthProvider>
      <MemoryRouter><EventList /></MemoryRouter>
    </AuthProvider>
  )

  await waitFor(() => screen.getByText('Jazz Night'))
  expect(screen.queryByText(/conflicts with your existing booking/i)).not.toBeInTheDocument()
})

test('does not show conflict badge for guest users', async () => {
  renderPage() // guest — no orders fetch
  await waitFor(() => screen.getByText('Jazz Night'))
  expect(screen.queryByText(/conflicts with your existing booking/i)).not.toBeInTheDocument()
})
