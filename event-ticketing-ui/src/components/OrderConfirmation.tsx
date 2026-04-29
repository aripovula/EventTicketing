import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'

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

export default function OrderConfirmation() {
  const { id } = useParams<{ id: string }>()
  const [order, setOrder] = useState<Order | null>(null)
  const [event, setEvent] = useState<Event | null>(null)
  const [loading, setLoading] = useState(true)
  const [notFound, setNotFound] = useState(false)

  useEffect(() => {
    fetch(`/api/events/orders/${id}`)
      .then(res => {
        if (res.status === 404) { setNotFound(true); setLoading(false); return null }
        return res.json()
      })
      .then((data: Order | null) => {
        if (!data) return
        setOrder(data)
        return fetch(`/api/events/${data.eventId}`).then(res => res.json())
      })
      .then((data: Event | null) => {
        if (data) setEvent(data)
        setLoading(false)
      })
      .catch(() => setLoading(false))
  }, [id])

  if (loading) return <p className="text-gray-500 text-center py-12">Loading…</p>
  if (notFound) return <p className="text-center text-gray-500 py-12">Order not found.</p>
  if (!order || !event) return <p className="text-center text-red-500 py-12">Something went wrong. Please try again.</p>

  return (
    <div className="max-w-lg mx-auto">
      <div className="bg-white rounded-xl border border-gray-200 p-8 text-center">
        <p className="text-4xl mb-4">🎟</p>
        <h1 className="text-2xl font-bold text-gray-900 mb-1">You're going!</h1>
        <p className="text-gray-500 text-sm mb-6">Confirmation sent to <span className="font-medium text-gray-700">{order.email}</span></p>

        <div className="text-left border-t border-gray-100 pt-5 space-y-2">
          <p className="text-lg font-semibold text-gray-900">{event.title}</p>
          <p className="text-sm text-gray-500">
            {event.venue} · {new Date(event.date).toLocaleDateString(undefined, { dateStyle: 'long' })}
          </p>
          <p className="text-sm text-gray-500">
            Order #{order.id} · <span className="font-medium text-gray-700">${order.price}</span>
          </p>
        </div>
      </div>

      <div className="mt-6 text-center">
        <Link to="/" className="text-sm text-cyan-600 hover:text-cyan-800 transition-colors no-underline">
          ← Browse more events
        </Link>
      </div>
    </div>
  )
}
