import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import FullCalendar from '@fullcalendar/react'
import dayGridPlugin from '@fullcalendar/daygrid'
import interactionPlugin from '@fullcalendar/interaction'
import type { EventClickArg, EventHoveringArg } from '@fullcalendar/core'
import { EVENT_TYPE_COLORS, EVENT_TYPES, colorForType } from '../utils/eventTypes'

type AdminEvent = {
  id: number
  title: string
  startTime: string
  endTime: string
  venue: string
  description: string
  eventType: string
  price: number
  availableSeats: number
  totalSeats: number
}

export default function AdminCalendarPage() {
  const [events, setEvents] = useState<AdminEvent[]>([])
  const [loading, setLoading] = useState(true)
  const [popup, setPopup] = useState<AdminEvent | null>(null)
  const [popupPos, setPopupPos] = useState({ top: 0, left: 0 })
  const [tooltip, setTooltip] = useState<{ title: string; availableSeats: number; top: number; left: number } | null>(null)
  const popupRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    fetch('/api/events')
      .then(res => res.json())
      .then((data: AdminEvent[]) => { setEvents(data); setLoading(false) })
  }, [])

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (popupRef.current && !popupRef.current.contains(e.target as Node)) {
        setPopup(null)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  const calendarEvents = events.map(ev => ({
    id: String(ev.id),
    title: `${ev.title} – ${ev.availableSeats}`,
    start: ev.startTime,
    end: ev.endTime,
    backgroundColor: colorForType(ev.eventType),
    borderColor: colorForType(ev.eventType),
    extendedProps: { event: ev },
  }))

  function handleEventMouseEnter(arg: EventHoveringArg) {
    const ev = arg.event.extendedProps.event as AdminEvent
    const rect = arg.el.getBoundingClientRect()
    setTooltip({ title: ev.title, availableSeats: ev.availableSeats, top: rect.top + window.scrollY - 8, left: rect.left + window.scrollX + rect.width / 2 })
  }

  function handleEventMouseLeave() {
    setTooltip(null)
  }

  function handleEventClick(arg: EventClickArg) {
    const ev = arg.event.extendedProps.event as AdminEvent
    const rect = arg.el.getBoundingClientRect()
    setPopupPos({ top: rect.bottom + window.scrollY + 6, left: rect.left + window.scrollX })
    setPopup(ev)
  }

  if (loading) return <p className="text-gray-500 text-center py-12">Loading...</p>

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <Link to="/admin" className="text-sm text-cyan-600 hover:text-cyan-800 transition-colors no-underline">
            ← Back to admin
          </Link>
          <h1 className="text-2xl font-bold text-gray-900 m-0">Events Calendar</h1>
        </div>
      </div>

      {/* Legend */}
      <div className="flex flex-wrap gap-3 mb-5">
        {EVENT_TYPES.filter(t => t !== 'Other').map(type => (
          <span key={type} className="flex items-center gap-1.5 text-xs font-medium text-gray-600">
            <span className="w-3 h-3 rounded-full inline-block" style={{ backgroundColor: EVENT_TYPE_COLORS[type] }} />
            {type}
          </span>
        ))}
      </div>

      <p className="text-xs text-gray-400 mb-4">Each event shows: <em>Title – remaining seats</em>. Overlapping events share the same time slot.</p>

      <div className="bg-white rounded-xl border border-gray-200 p-4">
        <FullCalendar
          plugins={[dayGridPlugin, interactionPlugin]}
          initialView="dayGridMonth"
          events={calendarEvents}
          eventClick={handleEventClick}
          eventMouseEnter={handleEventMouseEnter}
          eventMouseLeave={handleEventMouseLeave}
          height="auto"
          headerToolbar={{ left: 'prev,next today', center: 'title', right: '' }}
          eventDisplay="block"
          dayMaxEvents={false}
        />
      </div>

      {/* Tooltip */}
      {tooltip && (
        <div
          role="tooltip"
          className="fixed z-50 pointer-events-none -translate-x-1/2 -translate-y-full bg-gray-900 text-white text-xs rounded-lg px-3 py-2 shadow-lg whitespace-nowrap"
          style={{ top: tooltip.top, left: tooltip.left }}
        >
          <span className="font-medium">{tooltip.title}</span>
          <span className="text-gray-300 ml-2">{tooltip.availableSeats} seats left</span>
          <span className="absolute left-1/2 -translate-x-1/2 top-full border-4 border-transparent border-t-gray-900" />
        </div>
      )}

      {/* Popup */}
      {popup && (
        <div
          ref={popupRef}
          className="fixed z-50 bg-white rounded-xl border border-gray-200 shadow-lg p-4 w-80"
          style={{ top: popupPos.top, left: popupPos.left }}
        >
          <div className="flex items-start justify-between gap-2 mb-2">
            <span
              className="text-xs font-medium text-white px-2 py-0.5 rounded-full"
              style={{ backgroundColor: colorForType(popup.eventType) }}
            >
              {popup.eventType}
            </span>
            <button onClick={() => setPopup(null)} className="text-gray-400 hover:text-gray-600 text-lg leading-none">×</button>
          </div>
          <h3 className="font-semibold text-gray-900 text-sm mb-1">{popup.title}</h3>
          <p className="text-xs text-gray-500 mb-1">{popup.venue}</p>
          <p className="text-xs text-gray-500 mb-1">
            {new Date(popup.startTime).toLocaleDateString(undefined, { dateStyle: 'medium' })}{' '}
            {new Date(popup.startTime).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })}
            {' – '}
            {new Date(popup.endTime).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })}
          </p>
          <p className="text-xs text-gray-600 line-clamp-3 mb-3">{popup.description}</p>
          <div className="flex items-center justify-between text-xs">
            <span className="text-gray-500">{popup.availableSeats} / {popup.totalSeats} seats remaining</span>
            <span className="font-medium text-gray-900">${popup.price}</span>
          </div>
          <div className="flex gap-2 mt-3">
            <Link
              to={`/admin/${popup.id}/edit`}
              className="text-xs border border-gray-300 text-gray-600 px-3 py-1 rounded-lg hover:bg-gray-50 transition-colors no-underline"
            >
              Edit
            </Link>
          </div>
        </div>
      )}
    </div>
  )
}
