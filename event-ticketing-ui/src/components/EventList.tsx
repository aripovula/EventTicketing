import { useEffect, useRef, useState } from 'react'
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
  const searchInputRef = useRef<HTMLInputElement>(null)

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

  const sortEvents = (a: Event, b: Event) => {
    if (sortType == 'name') return a.title.localeCompare(b.title)
    if (sortType == 'date') return Date.parse(a.date) - Date.parse(b.date)
    return a.price - b.price
  }

  const changeInputSize = (inputWidth: number, fSize: number) => {
    searchInputRef.current.style.width=`${inputWidth}px`
    searchInputRef.current.style.fontSize=`${fSize}px`
  }

  if (loading) return <p>Loading events...</p>

  return (
    <div>
      <input
        type="text"
        value={searchTerm}
        onChange={e => setSearchTerm(e.target.value)}
        onFocus={() => changeInputSize(400, 22)}
        onBlur={() => changeInputSize(200, 12)}
        placeholder='type search term'
        ref={searchInputRef}
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
