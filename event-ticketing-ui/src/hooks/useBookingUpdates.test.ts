import { renderHook, act } from '@testing-library/react'
import { useBookingUpdates } from './useBookingUpdates'

// vi.hoisted() runs before module imports, so vi.fn() is available there.
// The mock connection is shared across all tests via this ref.
const mockConnection = vi.hoisted(() => ({
  on:    vi.fn(),
  start: vi.fn(() => Promise.resolve()),
  stop:  vi.fn(() => Promise.resolve()),
}))

vi.mock('@microsoft/signalr', () => ({
  // Must be a regular function so `new HubConnectionBuilder()` works
  HubConnectionBuilder: function () {
    return {
      withUrl:              function () { return this },
      withAutomaticReconnect: function () { return this },
      configureLogging:     function () { return this },
      build:                function () { return mockConnection },
    }
  },
  LogLevel: { Warning: 2 },
}))

beforeEach(() => vi.clearAllMocks())

// ── Tests ─────────────────────────────────────────────────────────────────────

test('starts the SignalR connection on mount', () => {
  renderHook(() => useBookingUpdates(vi.fn()))
  expect(mockConnection.start).toHaveBeenCalledTimes(1)
})

test('stops the connection on unmount', () => {
  const { unmount } = renderHook(() => useBookingUpdates(vi.fn()))
  unmount()
  expect(mockConnection.stop).toHaveBeenCalledTimes(1)
})

test('registers a BookingMade handler', () => {
  renderHook(() => useBookingUpdates(vi.fn()))
  expect(mockConnection.on).toHaveBeenCalledWith('BookingMade', expect.any(Function))
})

test('calls the callback with eventId when BookingMade is received', () => {
  const callback = vi.fn()
  renderHook(() => useBookingUpdates(callback))

  // Grab the handler that was registered and simulate a server push
  const handler = mockConnection.on.mock.calls[0][1] as (id: number) => void
  act(() => handler(42))

  expect(callback).toHaveBeenCalledWith(42)
})

test('uses the latest callback without restarting the connection', () => {
  const first  = vi.fn()
  const second = vi.fn()

  const { rerender } = renderHook(({ cb }) => useBookingUpdates(cb), {
    initialProps: { cb: first },
  })

  rerender({ cb: second })

  // Connection started only once despite the prop change
  expect(mockConnection.start).toHaveBeenCalledTimes(1)

  // Simulate a push — should invoke the latest callback only
  const handler = mockConnection.on.mock.calls[0][1] as (id: number) => void
  act(() => handler(7))
  expect(second).toHaveBeenCalledWith(7)
  expect(first).not.toHaveBeenCalled()
})
