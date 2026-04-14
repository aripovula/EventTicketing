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
  const [sortType, setSortType] = useState<'name' | 'date' | 'price'>('name')

  useEffect(() => {
    fetch('/api/events')
      .then(res => res.json())
      .then((data: Event[]) => {
        setEvents(data)
        setLoading(false)
      })
  }, [])

  useEffect(() => {
    const intervalId = setInterval(() => {
      fetch('/api/events')
        .then(res => res.json())
        .then((data: Event[]) => {
          setEvents(data)})
        }, 30_000)
    return () => clearInterval(intervalId)
  }, [])

  const sortEvents = (a, b) => {
    if (sortType == 'name') return a.title.localeCompare(b.title)
    if (sortType == 'date') return Date.parse(a.date) - Date.parse(b.date)
    return a.price - b.price
  }

  if (loading) return <p>Loading events...</p>

  return (
    <div>
      <input
        type="text"
        value={searchTerm}
        onChange={e => setSearchTerm(e.target.value)}
        placeholder='type search term'
      />
      <select value={sortType} onChange={e => setSortType(e.target.value as 'date' | 'price')}>
        <option id='name' value='name'>sort by name</option>
        <option id='date' value='date'>sort by date</option>
        <option id='price' value='price'>sort by price</option>
      </select>
      <ul aria-label="events">
        {events
          .filter(e => e.title.toUpperCase().includes(searchTerm.toUpperCase()))
          .sort((a, b) => sortEvents(a, b))
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
