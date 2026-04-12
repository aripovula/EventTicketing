import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'

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

export default function EventList() {
  const [events, setEvents] = useState<Event[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    fetch('/api/events')
      .then(res => res.json())
      .then((data: Event[]) => {
        setEvents(data)
        setLoading(false)
      })
  }, [])

  if (loading) return <p>Loading events...</p>

  return (
    <ul aria-label="events">
      {events.map(event => (
        <li key={event.id}>
          <h2><Link to={`/events/${event.id}`}>{event.title}</Link></h2>
          <p>{event.venue} — {new Date(event.date).toLocaleDateString()}</p>
          <p>${event.price} · {event.availableSeats} seats available</p>
        </li>
      ))}
    </ul>
  )
}
