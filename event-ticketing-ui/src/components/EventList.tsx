import { useCallback, useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { useHubEvents } from '../hooks/useHubEvents'
import { colorForType } from '../utils/eventTypes'

type Event = {
  id: number
  title: string
  description: string
  startTime: string
  endTime: string
  venue: string
  totalSeats: number
  availableSeats: number
  price: number
  imageUrl?: string
  eventType: string
}

type OrderWithEvent = {
  event: { id: number; startTime: string; endTime: string; title: string } | null
}

function overlaps(aStart: string, aEnd: string, bStart: string, bEnd: string): boolean {
  return new Date(aStart) < new Date(bEnd) && new Date(aEnd) > new Date(bStart)
}

export default function EventList() {
  const { user } = useAuth()
  const [events, setEvents] = useState<Event[]>([])
  const [myBookedEvents, setMyBookedEvents] = useState<OrderWithEvent[]>([])
  const [loading, setLoading] = useState(true)
  const [searchTerm, setSearchTerm] = useState("")
  const [sortType, setSortType] = useState<'name' | 'date' | 'price'>('name')
  const searchInputRef = useRef<HTMLInputElement>(null)

  const fetchEvents = useCallback(() => {
    fetch('/api/events')
      .then(res => res.json())
      .then((data: Event[]) => { setEvents(data); setLoading(false) })
  }, [])

  useEffect(() => { fetchEvents() }, [fetchEvents])

  useEffect(() => {
    if (!user) return
    fetch('/api/auth/me/orders')
      .then(res => res.ok ? res.json() : [])
      .then((data: OrderWithEvent[]) => setMyBookedEvents(data))
      .catch(() => {})
  }, [user])

  const displayedBookedEvents = user ? myBookedEvents : []

  useHubEvents({ BookingMade: fetchEvents, EventCreated: fetchEvents, EventUpdated: fetchEvents, EventDeleted: fetchEvents })

  const sortEvents = (a: Event, b: Event) => {
    if (sortType === 'name') return a.title.localeCompare(b.title)
    if (sortType === 'date') return Date.parse(a.startTime) - Date.parse(b.startTime)
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

  function conflictingBooking(event: Event): string | null {
    for (const order of displayedBookedEvents) {
      if (!order.event || order.event.id === event.id) continue
      if (overlaps(event.startTime, event.endTime, order.event.startTime, order.event.endTime)) {
        return order.event.title
      }
    }
    return null
  }

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
          style={{ width: '200px', fontSize: '12px' }}
          className="rounded-lg border border-gray-300 px-4 py-2 focus:outline-none focus:ring-2 focus:ring-cyan-400 transition"
        />
        <select
          value={sortType}
          onChange={e => setSortType(e.target.value as 'name' | 'date' | 'price')}
          className="rounded-lg border border-gray-300 px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-cyan-400 transition"
        >
          <option value="name">Sort by name</option>
          <option value="date">Sort by date</option>
          <option value="price">Sort by price</option>
        </select>
      </div>

      <ul aria-label="events" className="space-y-4 list-none p-0 m-0">
        {filtered.map(event => {
          const conflict = conflictingBooking(event)
          return (
            <li key={event.id} className="bg-white rounded-xl border border-gray-200 overflow-hidden hover:shadow-md transition-shadow flex h-[220px]">
              {event.imageUrl && (
                <img
                  src={event.imageUrl}
                  alt={event.title}
                  className="aspect-[4/3] h-full object-cover shrink-0"
                />
              )}
              <div className="flex flex-col justify-between p-5 flex-1 min-w-0">
                <div>
                  <div className="flex items-center gap-2 mb-1">
                    <span
                      className="inline-block text-white text-xs font-medium px-2 py-0.5 rounded-full"
                      style={{ backgroundColor: colorForType(event.eventType) }}
                    >
                      {event.eventType}
                    </span>
                    {conflict && (
                      <span className="inline-flex items-center gap-1 text-xs font-medium text-amber-700 bg-amber-50 border border-amber-200 px-2 py-0.5 rounded-full">
                        ⚠ conflicts with your existing booking of "{conflict}"
                      </span>
                    )}
                  </div>
                  <h2 className="text-xl font-semibold text-gray-900 mb-1 mt-0">
                    <Link to={`/events/${event.id}`} className="hover:text-cyan-600 transition-colors no-underline">
                      {event.title}
                    </Link>
                  </h2>
                  <p className="text-base text-gray-500">
                    {event.venue} · {new Date(event.startTime).toLocaleDateString(undefined, { dateStyle: 'medium' })}
                    {' '}
                    {new Date(event.startTime).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })}
                    {' – '}
                    {new Date(event.endTime).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })}
                  </p>
                </div>
                <div className="flex items-end justify-between">
                  <p className="text-sm text-gray-400">{event.availableSeats} seats available</p>
                  <div className="text-right shrink-0">
                    <span className="text-xl font-bold text-cyan-600">${event.price}</span>
                    <Link
                      to={`/events/${event.id}`}
                      className="block mt-2 text-sm bg-cyan-600 text-white rounded-lg px-4 py-2 hover:bg-cyan-700 transition-colors no-underline"
                    >
                      View →
                    </Link>
                  </div>
                </div>
              </div>
            </li>
          )
        })}
      </ul>

      {filtered.length === 0 && (
        <p className="text-center text-gray-400 py-12">No events match your search.</p>
      )}
    </div>
  )
}
