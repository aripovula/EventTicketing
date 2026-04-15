import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'

type Event = {
  id: number
  title: string
  description: string
  date: string
  venue: string
  totalSeats: number
  availableSeats: number
  price: number
}

export default function EventDetail() {
  const { id } = useParams<{ id: string }>()
  const [event, setEvent] = useState<Event | null>(null)
  const [loading, setLoading] = useState(true)
  const [notFound, setNotFound] = useState(false)

  useEffect(() => {
    fetch(`/api/events/${id}`)
      .then(res => {
        if (res.status === 404) { setNotFound(true); return null }
        return res.json()
      })
      .then((data: Event | null) => {
        if (data) setEvent(data)
        setLoading(false)
      })
      .catch(() => setLoading(false))
  }, [id])

  useEffect(() => {
    document.title = event ? `${event.title} | Ticketing` : `Ticketing`
    return () => { document.title = 'Event Ticketing' }
  }, [event])

  if (loading) return <p className="text-gray-500 text-center py-12">Loading...</p>
  if (notFound) return <p className="text-center text-gray-500 py-12">Event not found.</p>
  if (!event) return <p className="text-center text-red-500 py-12">Something went wrong. Please try again.</p>

  const soldOut = event.availableSeats === 0

  return (
    <div>
      <Link to="/" className="text-sm text-indigo-600 hover:text-indigo-800 transition-colors no-underline">
        ← Back to events
      </Link>

      <div className="mt-6 bg-white rounded-xl border border-gray-200 p-6">
        <h1 className="text-3xl font-bold text-gray-900 mt-0 mb-2">{event.title}</h1>
        <p className="text-gray-500 text-sm mb-6">
          {event.venue} · {new Date(event.date).toLocaleDateString(undefined, { dateStyle: 'long' })}
        </p>

        <p className="text-gray-700 leading-relaxed mb-6">{event.description}</p>

        <div className="flex items-center justify-between border-t border-gray-100 pt-5">
          <div>
            <span className="text-3xl font-bold text-indigo-600">${event.price}</span>
            <p className="text-sm mt-1 text-gray-500">
              {soldOut
                ? <span className="text-red-500 font-medium">Sold out</span>
                : <>{event.availableSeats} of {event.totalSeats} seats available</>
              }
            </p>
          </div>
          <button
            disabled={soldOut}
            className="bg-indigo-600 text-white px-6 py-2.5 rounded-lg font-medium hover:bg-indigo-700 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
          >
            {soldOut ? 'Sold out' : 'Buy ticket'}
          </button>
        </div>
      </div>
    </div>
  )
}
