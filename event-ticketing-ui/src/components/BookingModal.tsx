import { useState } from 'react'

type Props = {
  eventTitle: string
  eventDate: string
  eventVenue: string
  price: number
  savedEmail?: string
  savedCardLast4?: string
  onConfirm: (email: string, cardLast4: string) => Promise<void>
  onClose: () => void
  error: string | null
}

const inputClass = "w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400 transition"
const labelClass = "block text-sm font-medium text-gray-700 mb-1"

export default function BookingModal({ eventTitle, eventDate, eventVenue, price, savedEmail, savedCardLast4, onConfirm, onClose, error }: Props) {
  const [email, setEmail] = useState(savedEmail ?? '')
  const [cardMode, setCardMode] = useState<'saved' | 'new'>(savedCardLast4 ? 'saved' : 'new')
  const [cardNumber, setCardNumber] = useState('')
  const [expiry, setExpiry] = useState('')
  const [cvv, setCvv] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const formattedDate = new Date(eventDate).toLocaleDateString(undefined, { dateStyle: 'long' })

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSubmitting(true)
    const cardLast4 = cardMode === 'saved' ? savedCardLast4! : cardNumber.slice(-4)
    await onConfirm(email, cardLast4)
    setSubmitting(false)
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
              className={inputClass}
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
            <>
              <div>
                <label htmlFor="cardNumber" className={labelClass}>Card number</label>
                <input
                  id="cardNumber"
                  value={cardNumber}
                  onChange={e => setCardNumber(e.target.value.replace(/\D/g, '').slice(0, 16))}
                  required
                  inputMode="numeric"
                  placeholder="1234 5678 9012 3456"
                  pattern="\d{16}"
                  title="16-digit card number"
                  className={inputClass}
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label htmlFor="expiry" className={labelClass}>Expiry (MM/YY)</label>
                  <input
                    id="expiry"
                    value={expiry}
                    onChange={e => {
                      const raw = e.target.value.replace(/\D/g, '').slice(0, 4)
                      setExpiry(raw.length > 2 ? `${raw.slice(0, 2)}/${raw.slice(2)}` : raw)
                    }}
                    required
                    placeholder="MM/YY"
                    pattern="(0[1-9]|1[0-2])\/\d{2}"
                    title="Expiry in MM/YY format"
                    className={inputClass}
                  />
                </div>
                <div>
                  <label htmlFor="cvv" className={labelClass}>CVV</label>
                  <input
                    id="cvv"
                    value={cvv}
                    onChange={e => setCvv(e.target.value.replace(/\D/g, '').slice(0, 4))}
                    required
                    inputMode="numeric"
                    placeholder="123"
                    pattern="\d{3,4}"
                    title="3 or 4 digit CVV"
                    className={inputClass}
                  />
                </div>
              </div>
            </>
          )}

          {error && <p role="alert" className="text-sm text-red-500">{error}</p>}

          <div className="flex gap-3 pt-2">
            <button
              type="submit"
              disabled={submitting}
              className="bg-indigo-600 text-white px-5 py-2 rounded-lg text-sm font-medium hover:bg-indigo-700 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
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
