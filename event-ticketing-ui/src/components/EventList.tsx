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
    searchInputRef.current!.style.width = `${inputWidth}px`
    searchInputRef.current!.style.fontSize = `${fSize}px`
  }

  if (loading) return <p className="text-gray-500 text-center py-12">Loading events...</p>

  const filtered = events
    .filter(e => e.title.toUpperCase().includes(searchTerm.toUpperCase()))
    .sort((a, b) => sortEvents(a, b))

  return (
    <div>
      <div className="flex gap-3 mb-6">
        <input
          ref={searchInputRef}
          type="text"
          value={searchTerm}
          onChange={e => setSearchTerm(e.target.value)}
          onFocus={() => changeInputSize(400, 22)}
          onBlur={() => changeInputSize(200, 12)}
          placeholder="type search term"
          className="flex-1 rounded-lg border border-gray-300 px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400 transition"
        />
        <select
          value={sortType}
          onChange={e => setSortType(e.target.value as 'name' | 'date' | 'price')}
          className="rounded-lg border border-gray-300 px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-indigo-400 transition"
        >
          <option value="name">Sort by name</option>
          <option value="date">Sort by date</option>
          <option value="price">Sort by price</option>
        </select>
      </div>

      <ul aria-label="events" className="space-y-4 list-none p-0 m-0">
        {filtered.map(event => (
          <li key={event.id} className="bg-white rounded-xl border border-gray-200 p-5 hover:shadow-md transition-shadow">
            <div className="flex items-start justify-between gap-4">
              <div className="flex-1 min-w-0">
                <h2 className="text-lg font-semibold text-gray-900 mb-1 mt-0">
                  <Link to={`/events/${event.id}`} className="hover:text-indigo-600 transition-colors no-underline">
                    {event.title}
                  </Link>
                </h2>
                <p className="text-sm text-gray-500">
                  {event.venue} · {new Date(event.date).toLocaleDateString(undefined, { dateStyle: 'medium' })}
                </p>
                <p className="text-sm text-gray-400 mt-1">{event.availableSeats} seats available</p>
              </div>
              <div className="text-right shrink-0">
                <span className="text-lg font-bold text-indigo-600">${event.price}</span>
                <Link
                  to={`/events/${event.id}`}
                  className="block mt-2 text-xs bg-indigo-600 text-white rounded-lg px-3 py-1.5 hover:bg-indigo-700 transition-colors no-underline"
                >
                  View →
                </Link>
              </div>
            </div>
          </li>
        ))}
      </ul>

      {filtered.length === 0 && (
        <p className="text-center text-gray-400 py-12">No events match your search.</p>
      )}
    </div>
  )
}
