import { useState } from 'react'
import { CardElement, useElements, useStripe } from '@stripe/react-stripe-js'

type Props = {
  eventTitle: string
  eventDate: string
  eventVenue: string
  price: number
  savedEmail?: string
  savedPaymentMethodId?: string
  savedCardLast4?: string
  onConfirm: (email: string, paymentMethodId: string) => Promise<void>
  onClose: () => void
  error: string | null
}

const labelClass = "block text-sm font-medium text-gray-700 mb-1"

// Stripe CardElement appearance to match the app's existing input style.
const CARD_ELEMENT_OPTIONS = {
  style: {
    base: {
      fontSize: '14px',
      color: '#111827',
      fontFamily: 'inherit',
      '::placeholder': { color: '#9ca3af' },
    },
    invalid: { color: '#ef4444' },
  },
}

export default function BookingModal({ eventTitle, eventDate, eventVenue, price, savedEmail, savedPaymentMethodId, savedCardLast4, onConfirm, onClose, error }: Props) {
  const stripe = useStripe()
  const elements = useElements()

  const [email, setEmail] = useState(savedEmail ?? '')
  const [cardMode, setCardMode] = useState<'saved' | 'new'>(savedCardLast4 ? 'saved' : 'new')
  const [submitting, setSubmitting] = useState(false)
  const [stripeError, setStripeError] = useState<string | null>(null)

  const formattedDate = new Date(eventDate).toLocaleDateString(undefined, { dateStyle: 'long' })

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setStripeError(null)
    setSubmitting(true)

    try {
      // Saved card path — re-use the stored PaymentMethod ID without tokenising again.
      if (cardMode === 'saved' && savedPaymentMethodId) {
        await onConfirm(email, savedPaymentMethodId)
        return
      }

      if (!stripe || !elements) {
        setStripeError('Stripe is not ready yet. Please try again.')
        return
      }

      const cardElement = elements.getElement(CardElement)
      if (!cardElement) {
        setStripeError('Card element not found.')
        return
      }

      const { paymentMethod, error: stripeErr } = await stripe.createPaymentMethod({
        type: 'card',
        card: cardElement,
      })

      if (stripeErr || !paymentMethod) {
        setStripeError(stripeErr?.message ?? 'Could not tokenise card.')
        return
      }

      await onConfirm(email, paymentMethod.id)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="booking-modal-title"
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
    >
      <div className="bg-white rounded-xl border border-gray-200 p-6 w-full max-w-md mx-4 shadow-lg">
        <h2 id="booking-modal-title" className="text-xl font-bold text-gray-900 mb-1">
          Place order
        </h2>
        <p className="text-sm text-gray-500 mb-5">
          {eventTitle} · {eventVenue} · {formattedDate} &mdash; <span className="font-medium text-gray-700">${price}</span>
        </p>

        <form onSubmit={handleSubmit} aria-label="booking form" className="space-y-4">
          <div>
            <label htmlFor="email" className={labelClass}>Email</label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={e => setEmail(e.target.value)}
              required
              placeholder="you@example.com"
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-cyan-400 transition"
            />
          </div>

          {savedCardLast4 && (
            <fieldset className="space-y-2">
              <legend className="text-sm font-medium text-gray-700">Payment</legend>
              <label className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
                <input
                  type="radio"
                  name="cardMode"
                  value="saved"
                  checked={cardMode === 'saved'}
                  onChange={() => setCardMode('saved')}
                />
                Card ending in {savedCardLast4}
              </label>
              <label className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
                <input
                  type="radio"
                  name="cardMode"
                  value="new"
                  checked={cardMode === 'new'}
                  onChange={() => setCardMode('new')}
                />
                New card
              </label>
            </fieldset>
          )}

          {cardMode === 'new' && (
            <div>
              <label className={labelClass}>Card details</label>
              <div className="rounded-lg border border-gray-300 px-3 py-2.5 focus-within:ring-2 focus-within:ring-cyan-400 transition">
                <CardElement options={CARD_ELEMENT_OPTIONS} />
              </div>
            </div>
          )}

          {(stripeError || error) && (
            <p role="alert" className="text-sm text-red-500">{stripeError ?? error}</p>
          )}

          <div className="flex gap-3 pt-2">
            <button
              type="submit"
              disabled={submitting || !stripe}
              className="bg-cyan-600 text-white px-5 py-2 rounded-lg text-sm font-medium hover:bg-cyan-700 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
            >
              {submitting ? 'Placing order…' : 'Place order'}
            </button>
            <button
              type="button"
              onClick={onClose}
              disabled={submitting}
              className="border border-gray-300 text-gray-600 px-5 py-2 rounded-lg text-sm font-medium hover:bg-gray-50 transition-colors disabled:opacity-40"
            >
              Cancel
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
