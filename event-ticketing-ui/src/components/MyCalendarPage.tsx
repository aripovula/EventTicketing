import { useEffect, useRef, useState } from 'react'
import { Navigate } from 'react-router-dom'
import FullCalendar from '@fullcalendar/react'
import dayGridPlugin from '@fullcalendar/daygrid'
import interactionPlugin from '@fullcalendar/interaction'
import type { EventClickArg } from '@fullcalendar/core'
import { useAuth } from '../context/AuthContext'
import { EVENT_TYPE_COLORS, EVENT_TYPES, colorForType } from '../utils/eventTypes'

type BookedEvent = {
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

type OrderWithEvent = {
  id: number
  price: number
  bookedAt: string
  event: BookedEvent | null
}

type PopupEvent = BookedEvent & { orderId: number; paidPrice: number }

export default function MyCalendarPage() {
  const { user, isLoading: authLoading } = useAuth()
  const [orders, setOrders] = useState<OrderWithEvent[]>([])
  const [popup, setPopup] = useState<PopupEvent | null>(null)
  const [popupPos, setPopupPos] = useState({ top: 0, left: 0 })
  const popupRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!user) return
    fetch('/api/auth/me/orders')
      .then(res => res.ok ? res.json() : [])
      .then((data: OrderWithEvent[]) => setOrders(data))
      .catch(() => {})
  }, [user])

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (popupRef.current && !popupRef.current.contains(e.target as Node)) {
        setPopup(null)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  if (authLoading) return null
  if (!user) return <Navigate to="/login" replace />

  const calendarEvents = orders
    .filter(o => o.event !== null)
    .map(o => ({
      id: String(o.id),
      title: o.event!.title,
      start: o.event!.startTime,
      end: o.event!.endTime,
      backgroundColor: colorForType(o.event!.eventType),
      borderColor: colorForType(o.event!.eventType),
      extendedProps: { order: o },
    }))

  function handleEventClick(arg: EventClickArg) {
    const order = arg.event.extendedProps.order as OrderWithEvent
    if (!order.event) return
    const rect = arg.el.getBoundingClientRect()
    setPopupPos({ top: rect.bottom + window.scrollY + 6, left: rect.left + window.scrollX })
    setPopup({ ...order.event, orderId: order.id, paidPrice: order.price })
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900 m-0">My Calendar</h1>
      </div>

      {orders.length === 0 ? (
        <p className="text-center text-gray-400 py-12">
          You have no bookings yet. <a href="/" className="text-cyan-600 hover:underline">Browse events</a>.
        </p>
      ) : (
        <>
          {/* Legend */}
          <div className="flex flex-wrap gap-3 mb-5">
            {EVENT_TYPES.filter(t => t !== 'Other').map(type => (
              <span key={type} className="flex items-center gap-1.5 text-xs font-medium text-gray-600">
                <span className="w-3 h-3 rounded-full inline-block" style={{ backgroundColor: EVENT_TYPE_COLORS[type] }} />
                {type}
              </span>
            ))}
          </div>

          <div className="bg-white rounded-xl border border-gray-200 p-4">
            <FullCalendar
              plugins={[dayGridPlugin, interactionPlugin]}
              initialView="dayGridMonth"
              events={calendarEvents}
              eventClick={handleEventClick}
              height="auto"
              headerToolbar={{ left: 'prev,next today', center: 'title', right: '' }}
              eventDisplay="block"
              dayMaxEvents={3}
            />
          </div>
        </>
      )}

      {/* Popup */}
      {popup && (
        <div
          ref={popupRef}
          className="fixed z-50 bg-white rounded-xl border border-gray-200 shadow-lg p-4 w-72"
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
          <p className="text-xs text-gray-500 mb-2">
            {new Date(popup.startTime).toLocaleDateString(undefined, { dateStyle: 'medium' })}{' '}
            {new Date(popup.startTime).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })}
            {' – '}
            {new Date(popup.endTime).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })}
          </p>
          <p className="text-xs text-gray-600 line-clamp-3 mb-3">{popup.description}</p>
          <div className="flex items-center justify-between text-xs text-gray-500">
            <span>Order #{popup.orderId}</span>
            <span className="font-medium text-gray-900">${popup.paidPrice}</span>
          </div>
        </div>
      )}
    </div>
  )
}
