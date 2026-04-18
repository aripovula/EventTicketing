import { render, screen, waitFor, fireEvent } from '@testing-library/react'
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
        <Route path="/orders/:id" element={<div>Order confirmed</div>} />
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

// Buy ticket button

test('shows Buy ticket button when seats are available', async () => {
  vi.mocked(fetch).mockResolvedValue({ status: 200, json: () => Promise.resolve(mockEvent) } as Response)
  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  expect(screen.getByRole('button', { name: /buy ticket/i })).toBeInTheDocument()
})

test('Buy ticket button is disabled when sold out', async () => {
  vi.mocked(fetch).mockResolvedValue({ status: 200, json: () => Promise.resolve({ ...mockEvent, availableSeats: 0 }) } as Response)
  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  expect(screen.getByRole('button', { name: /sold out/i })).toBeDisabled()
})

test('clicking Buy ticket opens the booking modal', async () => {
  vi.mocked(fetch).mockResolvedValue({ status: 200, json: () => Promise.resolve(mockEvent) } as Response)
  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  fireEvent.click(screen.getByRole('button', { name: /buy ticket/i }))
  expect(screen.getByRole('dialog')).toBeInTheDocument()
  expect(screen.getByLabelText(/email/i)).toBeInTheDocument()
})

// Booking via modal

const mockOrder = { id: 42, eventId: 1, email: 'user@example.com', price: 25, bookedAt: '2026-04-17T00:00:00Z' }

async function fillAndSubmitModal() {
  fireEvent.click(screen.getByRole('button', { name: /buy ticket/i }))
  fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'user@example.com' } })
  fireEvent.change(screen.getByLabelText(/card number/i), { target: { value: '1234567890123456' } })
  fireEvent.change(screen.getByLabelText(/expiry/i), { target: { value: '12/27' } })
  fireEvent.change(screen.getByLabelText(/cvv/i), { target: { value: '123' } })
  fireEvent.submit(screen.getByRole('form', { name: /booking form/i }))
}

test('confirming modal posts email to /api/events/:id/book', async () => {
  const fetchMock = vi.mocked(fetch)
  fetchMock
    .mockResolvedValueOnce({ status: 200, json: () => Promise.resolve(mockEvent) } as Response)
    .mockResolvedValueOnce({ ok: true, status: 201, json: () => Promise.resolve(mockOrder) } as Response)

  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  await fillAndSubmitModal()

  await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/events/1/book', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: 'user@example.com' }),
  }))
})

test('navigates to order confirmation page after successful booking', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce({ status: 200, json: () => Promise.resolve(mockEvent) } as Response)
    .mockResolvedValueOnce({ ok: true, status: 201, json: () => Promise.resolve(mockOrder) } as Response)

  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  await fillAndSubmitModal()

  await waitFor(() => expect(screen.getByText('Order confirmed')).toBeInTheDocument())
})

test('shows sold-out error in modal on 409 response', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce({ status: 200, json: () => Promise.resolve(mockEvent) } as Response)
    .mockResolvedValueOnce({ ok: false, status: 409 } as Response)

  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  fireEvent.click(screen.getByRole('button', { name: /buy ticket/i }))
  fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'user@example.com' } })
  fireEvent.change(screen.getByLabelText(/card number/i), { target: { value: '1234567890123456' } })
  fireEvent.change(screen.getByLabelText(/expiry/i), { target: { value: '12/27' } })
  fireEvent.change(screen.getByLabelText(/cvv/i), { target: { value: '123' } })
  fireEvent.submit(screen.getByRole('form', { name: /booking form/i }))

  await waitFor(() => expect(screen.getByText(/sorry, this event just sold out/i)).toBeInTheDocument())
  expect(screen.getByRole('dialog')).toBeInTheDocument()
})

test('shows generic error in modal on non-409 booking failure', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce({ status: 200, json: () => Promise.resolve(mockEvent) } as Response)
    .mockResolvedValueOnce({ ok: false, status: 500 } as Response)

  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  fireEvent.click(screen.getByRole('button', { name: /buy ticket/i }))
  fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'user@example.com' } })
  fireEvent.change(screen.getByLabelText(/card number/i), { target: { value: '1234567890123456' } })
  fireEvent.change(screen.getByLabelText(/expiry/i), { target: { value: '12/27' } })
  fireEvent.change(screen.getByLabelText(/cvv/i), { target: { value: '123' } })
  fireEvent.submit(screen.getByRole('form', { name: /booking form/i }))

  await waitFor(() => expect(screen.getByText(/booking failed/i)).toBeInTheDocument())
})

test('Cancel button closes the modal', async () => {
  vi.mocked(fetch).mockResolvedValue({ status: 200, json: () => Promise.resolve(mockEvent) } as Response)
  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  fireEvent.click(screen.getByRole('button', { name: /buy ticket/i }))
  expect(screen.getByRole('dialog')).toBeInTheDocument()
  fireEvent.click(screen.getByRole('button', { name: /cancel/i }))
  expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
})

// Saved email and card in state after booking

test('modal shows Place order heading', async () => {
  vi.mocked(fetch).mockResolvedValue({ status: 200, json: () => Promise.resolve(mockEvent) } as Response)
  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  fireEvent.click(screen.getByRole('button', { name: /buy ticket/i }))
  expect(screen.getByRole('heading', { name: /place order/i })).toBeInTheDocument()
})

test('modal closes and navigates away on successful booking', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce({ status: 200, json: () => Promise.resolve(mockEvent) } as Response)
    .mockResolvedValueOnce({ ok: true, status: 201, json: () => Promise.resolve(mockOrder) } as Response)

  renderWithRoute('1')
  await waitFor(() => screen.getByText('Jazz Night'))
  await fillAndSubmitModal()

  await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
  expect(screen.getByText('Order confirmed')).toBeInTheDocument()
})