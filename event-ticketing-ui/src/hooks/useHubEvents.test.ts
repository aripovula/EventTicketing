import { renderHook, act } from '@testing-library/react'
import { useHubEvents } from './useHubEvents'

// vi.hoisted() runs before module imports, so vi.fn() is available there.
const mockConnection = vi.hoisted(() => ({
  on:    vi.fn(),
  start: vi.fn(() => Promise.resolve()),
  stop:  vi.fn(() => Promise.resolve()),
}))

vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: function () {
    return {
      withUrl:               function () { return this },
      withAutomaticReconnect: function () { return this },
      configureLogging:      function () { return this },
      build:                 function () { return mockConnection },
    }
  },
  LogLevel: { Warning: 2 },
}))

beforeEach(() => vi.clearAllMocks())

// ── Connection lifecycle ───────────────────────────────────────────────────────

test('starts the SignalR connection on mount', () => {
  renderHook(() => useHubEvents({ BookingMade: vi.fn() }))
  expect(mockConnection.start).toHaveBeenCalledTimes(1)
})

test('stops the connection on unmount', () => {
  const { unmount } = renderHook(() => useHubEvents({ BookingMade: vi.fn() }))
  unmount()
  expect(mockConnection.stop).toHaveBeenCalledTimes(1)
})

test('does not restart the connection when handlers reference changes', () => {
  const { rerender } = renderHook(
    ({ cb }: { cb: () => void }) => useHubEvents({ BookingMade: cb }),
    { initialProps: { cb: vi.fn() } },
  )
  rerender({ cb: vi.fn() })
  expect(mockConnection.start).toHaveBeenCalledTimes(1)
})

// ── Handler registration ───────────────────────────────────────────────────────

test('registers a handler for each event name', () => {
  renderHook(() =>
    useHubEvents({ BookingMade: vi.fn(), EventCreated: vi.fn() })
  )
  const registeredNames = mockConnection.on.mock.calls.map(([name]) => name)
  expect(registeredNames).toContain('BookingMade')
  expect(registeredNames).toContain('EventCreated')
})

test('calls the correct callback when BookingMade is received', () => {
  const onBooking = vi.fn()
  const onCreated = vi.fn()
  renderHook(() => useHubEvents({ BookingMade: onBooking, EventCreated: onCreated }))

  const handler = mockConnection.on.mock.calls.find(([n]) => n === 'BookingMade')?.[1] as (...a: unknown[]) => void
  act(() => handler(42))

  expect(onBooking).toHaveBeenCalledWith(42)
  expect(onCreated).not.toHaveBeenCalled()
})

test('calls the correct callback when EventCreated is received', () => {
  const onBooking = vi.fn()
  const onCreated = vi.fn()
  renderHook(() => useHubEvents({ BookingMade: onBooking, EventCreated: onCreated }))

  const handler = mockConnection.on.mock.calls.find(([n]) => n === 'EventCreated')?.[1] as (...a: unknown[]) => void
  act(() => handler(7))

  expect(onCreated).toHaveBeenCalledWith(7)
  expect(onBooking).not.toHaveBeenCalled()
})

test('uses the latest callback without restarting the connection', () => {
  const first  = vi.fn()
  const second = vi.fn()

  const { rerender } = renderHook(
    ({ cb }: { cb: (id: number) => void }) => useHubEvents({ BookingMade: cb }),
    { initialProps: { cb: first } },
  )
  rerender({ cb: second })

  expect(mockConnection.start).toHaveBeenCalledTimes(1)

  const handler = mockConnection.on.mock.calls[0][1] as (...a: unknown[]) => void
  act(() => handler(99))
  expect(second).toHaveBeenCalledWith(99)
  expect(first).not.toHaveBeenCalled()
})
