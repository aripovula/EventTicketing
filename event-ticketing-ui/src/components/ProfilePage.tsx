import { useEffect, useState } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

type DefaultCard = {
  last4: string
  cardType: string
  expiryDate: string
}

export default function ProfilePage() {
  const { user, isLoading: authLoading } = useAuth()

  const [card, setCard] = useState<DefaultCard | null>(null)
  const [cardLoading, setCardLoading] = useState(true)

  const [cardNumber, setCardNumber] = useState('')
  const [cardType, setCardType] = useState('Visa')
  const [expiryDate, setExpiryDate] = useState('')

  const [saving, setSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [saveSuccess, setSaveSuccess] = useState(false)

  useEffect(() => {
    if (!user) return
    fetch('/api/auth/me/cards/default')
      .then(res => (res.ok ? res.json() : null))
      .then((data: DefaultCard | null) => {
        if (data) {
          setCard(data)
          setCardType(data.cardType)
          setExpiryDate(data.expiryDate)
        }
        setCardLoading(false)
      })
      .catch(() => setCardLoading(false))
  }, [user])

  if (authLoading) return null
  if (!user) return <Navigate to="/login" replace />

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault()
    setSaving(true)
    setSaveError(null)
    setSaveSuccess(false)

    const res = await fetch('/api/auth/me/cards/default', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ cardNumber, cardType, expiryDate }),
    })

    if (res.ok) {
      const last4 = cardNumber.slice(-4)
      setCard({ last4, cardType, expiryDate })
      setCardNumber('')
      setSaveSuccess(true)
    } else {
      setSaveError('Failed to save card. Please check your details and try again.')
    }
    setSaving(false)
  }

  return (
    <div className="max-w-lg mx-auto space-y-8">

      {/* Account info */}
      <section className="bg-white rounded-xl border border-gray-200 p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Account</h2>
        <dl className="space-y-3 text-sm">
          <div className="flex justify-between">
            <dt className="text-gray-500">Name</dt>
            <dd className="font-medium text-gray-900">{user.name}</dd>
          </div>
          <div className="flex justify-between">
            <dt className="text-gray-500">Email</dt>
            <dd className="font-medium text-gray-900">{user.email}</dd>
          </div>
        </dl>
      </section>

      {/* Payment method */}
      <section className="bg-white rounded-xl border border-gray-200 p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Default payment method</h2>

        {!cardLoading && card && (
          <div className="mb-5 p-4 bg-gray-50 rounded-lg border border-gray-200 text-sm">
            <p className="font-medium text-gray-900">
              {card.cardType} ending in {card.last4}
            </p>
            <p className="text-gray-500 mt-1">Expires {card.expiryDate}</p>
          </div>
        )}

        {!cardLoading && !card && (
          <p className="text-sm text-gray-500 mb-5">No default card saved yet.</p>
        )}

        <form onSubmit={handleSave} aria-label="payment method form" className="space-y-4">
          <div>
            <label htmlFor="cardNumber" className="block text-sm font-medium text-gray-700 mb-1">
              {card ? 'New card number' : 'Card number'}
            </label>
            <input
              id="cardNumber"
              type="text"
              inputMode="numeric"
              placeholder="1234 5678 9012 3456"
              value={cardNumber}
              onChange={e => setCardNumber(e.target.value.replace(/\D/g, ''))}
              required
              maxLength={19}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-cyan-400"
            />
          </div>

          <div className="flex gap-4">
            <div className="flex-1">
              <label htmlFor="cardType" className="block text-sm font-medium text-gray-700 mb-1">
                Card type
              </label>
              <select
                id="cardType"
                value={cardType}
                onChange={e => setCardType(e.target.value)}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-cyan-400"
              >
                <option>Visa</option>
                <option>Mastercard</option>
                <option>Amex</option>
                <option>Discover</option>
              </select>
            </div>

            <div className="w-32">
              <label htmlFor="expiryDate" className="block text-sm font-medium text-gray-700 mb-1">
                Expiry
              </label>
              <input
                id="expiryDate"
                type="text"
                placeholder="MM/YY"
                value={expiryDate}
                onChange={e => setExpiryDate(e.target.value)}
                required
                maxLength={5}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-cyan-400"
              />
            </div>
          </div>

          {saveError && (
            <p className="text-sm text-red-500">{saveError}</p>
          )}

          {saveSuccess && (
            <p className="text-sm text-green-600">Card saved successfully.</p>
          )}

          <button
            type="submit"
            disabled={saving}
            className="bg-cyan-600 text-white px-5 py-2 rounded-lg text-sm font-medium hover:bg-cyan-700 disabled:opacity-50 transition-colors"
          >
            {saving ? 'Saving…' : card ? 'Update card' : 'Save card'}
          </button>
        </form>
      </section>
    </div>
  )
}
