import { useState } from 'react'
import { Link } from 'react-router-dom'

type Order = {
  id: number
  eventId: number
  email: string
  price: number
  bookedAt: string
}

type Event = {
  id: number
  title: string
  date: string
  venue: string
}

type OrderWithEvent = Order & { event: Event | null }

export default function OrdersList() {
  const [email, setEmail] = useState('')
  const [orders, setOrders] = useState<OrderWithEvent[] | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setLoading(true)
    setError(false)
    setOrders(null)

    try {
      const res = await fetch(`/api/events/orders?email=${encodeURIComponent(email)}`)
      if (!res.ok) throw new Error()
      const rawOrders: Order[] = await res.json()

      const ordersWithEvents = await Promise.all(
        rawOrders.map(async (order) => {
          const evRes = await fetch(`/api/events/${order.eventId}`)
          const event: Event | null = evRes.ok ? await evRes.json() : null
          return { ...order, event }
        })
      )

      setOrders(ordersWithEvents)
    } catch {
      setError(true)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="max-w-lg mx-auto">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">My orders</h1>

      <form onSubmit={handleSubmit} className="flex gap-2 mb-8">
        <input
          type="email"
          aria-label="Email"
          placeholder="Enter your email"
          required
          value={email}
          onChange={e => setEmail(e.target.value)}
          className="flex-1 border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400"
        />
        <button
          type="submit"
          disabled={loading}
          className="bg-indigo-600 text-white text-sm font-medium px-4 py-2 rounded-lg hover:bg-indigo-700 disabled:opacity-50"
        >
          {loading ? 'Looking up…' : 'Look up'}
        </button>
      </form>

      {error && (
        <p className="text-center text-red-500 text-sm">Something went wrong. Please try again.</p>
      )}

      {orders !== null && orders.length === 0 && (
        <p className="text-center text-gray-500 text-sm">No orders found for this email.</p>
      )}

      {orders !== null && orders.length > 0 && (
        <ul className="space-y-4">
          {orders.map(order => (
            <li key={order.id} className="bg-white rounded-xl border border-gray-200 p-5">
              <p className="font-semibold text-gray-900 mb-1">
                {order.event ? order.event.title : `Event #${order.eventId}`}
              </p>
              {order.event && (
                <p className="text-sm text-gray-500 mb-2">
                  {order.event.venue} · {new Date(order.event.date).toLocaleDateString(undefined, { dateStyle: 'long' })}
                </p>
              )}
              <p className="text-sm text-gray-500">
                Order #{order.id} · <span className="font-medium text-gray-700">${order.price}</span>
              </p>
              <Link
                to={`/orders/${order.id}`}
                className="text-xs text-indigo-600 hover:text-indigo-800 transition-colors no-underline mt-2 inline-block"
              >
                View confirmation →
              </Link>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
