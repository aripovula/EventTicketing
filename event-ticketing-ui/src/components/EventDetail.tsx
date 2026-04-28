import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import BookingModal from './BookingModal'
import { useAuth } from '../context/AuthContext'
import { useBookingUpdates } from '../hooks/useBookingUpdates'

type Event = {
  id: number
  title: string
  description: string
  date: string
  venue: string
  totalSeats: number
  availableSeats: number
  price: number
  imageUrl?: string
}

type User = {
  email: string
  cardLast4: string
}

export default function EventDetail() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { user } = useAuth()
  const [event, setEvent] = useState<Event | null>(null)
  const [loading, setLoading] = useState(true)
  const [notFound, setNotFound] = useState(false)
  const [modalOpen, setModalOpen] = useState(false)
  const [bookingError, setBookingError] = useState<string | null>(null)
  const [userData, setUserDate] = useState<User | null>(null)
  const [defaultCardLast4, setDefaultCardLast4] = useState<string | undefined>()

  const fetchEvent = useCallback(() => {
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

  useEffect(() => { fetchEvent() }, [fetchEvent])

  useBookingUpdates((bookedEventId) => {
    if (bookedEventId === Number(id)) fetchEvent()
  })

  useEffect(() => {
    if (!user) return
    fetch('/api/auth/me/cards/default')
      .then(res => (res.ok ? res.json() : null))
      .then((card: { last4: string } | null) => {
        if (card) setDefaultCardLast4(card.last4)
      })
      .catch(() => {})
  }, [user])

  useEffect(() => {
    document.title = event ? `${event.title} | Ticketing` : `Ticketing`
    return () => { document.title = 'Event Ticketing' }
  }, [event])

  if (loading) return <p className="text-gray-500 text-center py-12">Loading...</p>
  if (notFound) return <p className="text-center text-gray-500 py-12">Event not found.</p>
  if (!event) return <p className="text-center text-red-500 py-12">Something went wrong. Please try again.</p>

  const soldOut = event.availableSeats === 0

  const handleConfirmBooking = async (email: string, cardLast4: string) => {
    setBookingError(null)
    const res = await fetch(`/api/events/${id}/book`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email }),
    })
    if (res.ok) {
      const order = await res.json()
      setUserDate({ email, cardLast4 })
      setModalOpen(false)
      navigate(`/orders/${order.id}`)
    } else if (res.status === 409) {
      setBookingError('Sorry, this event just sold out.')
    } else {
      setBookingError('Booking failed. Please try again.')
    }
  }

  return (
    <div>
      <Link to="/" className="text-sm text-indigo-600 hover:text-indigo-800 transition-colors no-underline">
        ← Back to events
      </Link>

      <div className="mt-6 bg-white rounded-xl border border-gray-200 overflow-hidden">
        {event.imageUrl && (
          <img
            src={event.imageUrl}
            alt={event.title}
            className="w-full h-64 object-cover"
          />
        )}
        <div className="p-6">
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
            onClick={() => setModalOpen(true)}
            disabled={soldOut}
            className="bg-indigo-600 text-white px-6 py-2.5 rounded-lg font-medium hover:bg-indigo-700 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
          >
            {soldOut ? 'Sold out' : 'Buy ticket'}
          </button>
        </div>
        </div>
      </div>

      {modalOpen && (
        <BookingModal
          eventTitle={event.title}
          eventDate={event.date}
          eventVenue={event.venue}
          price={event.price}
          savedEmail={userData?.email ?? user?.email}
          savedCardLast4={userData?.cardLast4 ?? defaultCardLast4}
          onConfirm={handleConfirmBooking}
          onClose={() => { setModalOpen(false); setBookingError(null) }}
          error={bookingError}
        />
      )}
    </div>
  )
}
