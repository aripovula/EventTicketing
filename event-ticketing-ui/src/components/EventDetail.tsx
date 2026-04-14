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

  if (loading) return <p>Loading...</p>
  if (notFound) return <p>Event not found.</p>
  if (!event) return <p>Something went wrong. Please try again.</p>

  return (
    <>
      <Link to="/">← Back to events</Link>
      <h1>{event.title}</h1>
      <p>{event.description}</p>
      <p>{event.venue} — {new Date(event.date).toLocaleDateString()}</p>
      <p>${event.price} · {event.availableSeats} of {event.totalSeats} seats available</p>
    </>
  )
}
