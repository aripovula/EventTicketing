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
  const [searchTerm, setSearchTerm] = useState("")

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
    <div>
      <input
        type="text"
        value={searchTerm}
        onChange={e => setSearchTerm(e.target.value)}
        placeholder='type search term'
      />
      <ul aria-label="events">
        {events
          .filter(e => e.title.toUpperCase().includes(searchTerm.toUpperCase()))
          .map(event => (
          <li key={event.id}>
            <h2><Link to={`/events/${event.id}`}>{event.title}</Link></h2>
            <p>{event.venue} — {new Date(event.date).toLocaleDateString()}</p>
            <p>${event.price} · {event.availableSeats} seats available</p>
          </li>
        ))}
      </ul>
    </div>
  )
}
