import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { vi } from 'vitest'
import BookingModal from './BookingModal'

// Mock Stripe so the component renders without a real Elements provider or
// network calls. useStripe / useElements are replaced with lightweight stubs.
vi.mock('@stripe/react-stripe-js', () => ({
  CardElement: () => <div data-testid="stripe-card-element" />,
  useStripe: () => ({
    createPaymentMethod: vi.fn().mockResolvedValue({
      paymentMethod: { id: 'pm_test_mock' },
      error: undefined,
    }),
  }),
  useElements: () => ({
    getElement: () => ({}),
  }),
}))

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

test('prefills email from savedEmail prop', () => {
  render(<BookingModal {...baseProps} savedEmail="saved@example.com" />)
  expect(screen.getByLabelText(/email/i)).toHaveValue('saved@example.com')
})

test('shows Stripe card element when no savedCardLast4', () => {
  render(<BookingModal {...baseProps} />)
  expect(screen.getByTestId('stripe-card-element')).toBeInTheDocument()
  expect(screen.queryByRole('radio')).not.toBeInTheDocument()
})

test('shows saved card radio selected by default when savedCardLast4 provided', () => {
  render(<BookingModal {...baseProps} savedEmail="saved@example.com" savedCardLast4="4242" />)
  expect(screen.getByRole('radio', { name: /card ending in 4242/i })).toBeChecked()
  expect(screen.queryByTestId('stripe-card-element')).not.toBeInTheDocument()
})

test('selecting new card radio reveals Stripe card element', () => {
  render(<BookingModal {...baseProps} savedEmail="saved@example.com" savedCardLast4="4242" />)
  fireEvent.click(screen.getByRole('radio', { name: /new card/i }))
  expect(screen.getByTestId('stripe-card-element')).toBeInTheDocument()
})

test('saved card with savedPaymentMethodId calls onConfirm with stored ID without tokenising', async () => {
  const onConfirm = vi.fn().mockResolvedValue(undefined)
  render(<BookingModal {...baseProps} savedCardLast4="4242" savedPaymentMethodId="pm_saved_123" onConfirm={onConfirm} />)
  fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'test@example.com' } })
  fireEvent.submit(screen.getByRole('form', { name: /booking form/i }))

  await waitFor(() => expect(onConfirm).toHaveBeenCalledWith('test@example.com', 'pm_saved_123'))
})

test('shows error alert when error prop is set', () => {
  render(<BookingModal {...baseProps} error="Sorry, this event just sold out." />)
  expect(screen.getByRole('alert')).toHaveTextContent('Sorry, this event just sold out.')
})

test('Cancel button calls onClose', () => {
  const onClose = vi.fn()
  render(<BookingModal {...baseProps} onClose={onClose} />)
  fireEvent.click(screen.getByRole('button', { name: /cancel/i }))
  expect(onClose).toHaveBeenCalledOnce()
})
