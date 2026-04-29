import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

type OrderWithEvent = {
  id: number
  eventId: number
  email: string
  price: number
  bookedAt: string
  event: { id: number; title: string; startTime: string; venue: string } | null
}

export default function OrdersList() {
  const { user, isLoading: authLoading } = useAuth()
  const [email, setEmail] = useState('')
  const [orders, setOrders] = useState<OrderWithEvent[] | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(false)

  useEffect(() => {
    if (!user) return
    setLoading(true)
    setError(false)
    fetch('/api/auth/me/orders')
      .then(res => { if (!res.ok) throw new Error(); return res.json() })
      .then((data: OrderWithEvent[]) => { setOrders(data); setLoading(false) })
      .catch(() => { setError(true); setLoading(false) })
  }, [user])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setLoading(true)
    setError(false)
    setOrders(null)
    try {
      const res = await fetch(`/api/events/orders?email=${encodeURIComponent(email)}`)
      if (!res.ok) throw new Error()
      const rawOrders: Omit<OrderWithEvent, 'event'>[] = await res.json()
      const ordersWithEvents = await Promise.all(
        rawOrders.map(async (order) => {
          const evRes = await fetch(`/api/events/${order.eventId}`)
          const event = evRes.ok ? await evRes.json() : null
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

  if (authLoading) return <p className="text-gray-500 text-center py-12">Loading...</p>

  return (
    <div className="max-w-lg mx-auto">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">My orders</h1>

      {!user && (
        <form onSubmit={handleSubmit} className="flex gap-2 mb-8">
          <input
            type="email"
            aria-label="Email"
            placeholder="Enter your email"
            required
            value={email}
            onChange={e => setEmail(e.target.value)}
            className="flex-1 border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-cyan-400"
          />
          <button
            type="submit"
            disabled={loading}
            className="bg-cyan-600 text-white text-sm font-medium px-4 py-2 rounded-lg hover:bg-cyan-700 disabled:opacity-50"
          >
            {loading ? 'Looking up…' : 'Look up'}
          </button>
        </form>
      )}

      {loading && <p className="text-gray-500 text-center py-8">Loading...</p>}

      {error && (
        <p className="text-center text-red-500 text-sm">Something went wrong. Please try again.</p>
      )}

      {!loading && orders !== null && orders.length === 0 && (
        <p className="text-center text-gray-500 text-sm">No orders found.</p>
      )}

      {!loading && orders !== null && orders.length > 0 && (
        <ul className="space-y-4">
          {orders.map(order => (
            <li key={order.id} className="bg-white rounded-xl border border-gray-200 p-5">
              <p className="font-semibold text-gray-900 mb-1">
                {order.event ? order.event.title : `Event #${order.eventId}`}
              </p>
              {order.event && (
                <p className="text-sm text-gray-500 mb-2">
                  {order.event.venue} · {new Date(order.event.startTime).toLocaleDateString(undefined, { dateStyle: 'long' })}
                </p>
              )}
              <p className="text-sm text-gray-500">
                Order #{order.id} · <span className="font-medium text-gray-700">${order.price}</span>
              </p>
              <Link
                to={`/orders/${order.id}`}
                className="text-xs text-cyan-600 hover:text-cyan-800 transition-colors no-underline mt-2 inline-block"
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
