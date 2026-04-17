import { render, screen } from '@testing-library/react'
import { vi } from 'vitest'
import BookingModal from './BookingModal'

const baseProps = {
  eventTitle: 'Jazz Night',
  eventDate: '2026-08-15T20:00:00',
  eventVenue: 'Blue Note Club',
  price: 30,
  onConfirm: vi.fn(),
  onClose: vi.fn(),
  error: null,
}

afterEach(() => {
  vi.clearAllMocks()
})

test('renders Place order heading', () => {
  render(<BookingModal {...baseProps} />)
  expect(screen.getByRole('heading', { name: /place order/i })).toBeInTheDocument()
})

test('shows event title, venue, and date in info line', () => {
  render(<BookingModal {...baseProps} />)
  const info = screen.getByText(/Jazz Night/)
  expect(info).toHaveTextContent('Blue Note Club')
  expect(info).toHaveTextContent('2026')
  expect(info).toHaveTextContent('$30')
})
