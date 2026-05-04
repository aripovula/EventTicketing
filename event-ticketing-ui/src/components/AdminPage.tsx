import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import ConfirmDialog from './ConfirmDialog'

type Event = {
  id: number
  title: string
  venue: string
}

export default function AdminPage() {
  const [events, setEvents] = useState<Event[]>([])
  const [loading, setLoading] = useState(true)
  const [confirmDeleteId, setConfirmDeleteId] = useState<number | null>(null)
  const [deleteError, setDeleteError] = useState<string | null>(null)

  useEffect(() => {
    fetch('/api/events')
      .then(res => res.json())
      .then((data: Event[]) => { setEvents(data); setLoading(false) })
  }, [])

  const handleDeleteConfirmed = async () => {
    if (confirmDeleteId === null) return
    const res = await fetch(`/api/events/${confirmDeleteId}`, { method: 'DELETE' })
    if (!res.ok) {
      setDeleteError(`Failed to delete event (${res.status}). Please try again.`)
      setConfirmDeleteId(null)
      return
    }
    setEvents(prev => prev.filter(ev => ev.id !== confirmDeleteId))
    setConfirmDeleteId(null)
  }

  const confirmingEvent = events.find(ev => ev.id === confirmDeleteId)

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900 m-0">Admin</h1>
        <div className="flex gap-3">
          <Link
            to="/admin/calendar"
            className="border border-gray-300 text-gray-600 px-4 py-2 rounded-lg text-sm font-medium hover:bg-gray-50 transition-colors no-underline"
          >
            Events Calendar
          </Link>
          <Link
            to="/admin/summary"
            className="border border-gray-300 text-gray-600 px-4 py-2 rounded-lg text-sm font-medium hover:bg-gray-50 transition-colors no-underline"
          >
            Seat summary
          </Link>
          <Link
            to="/admin/orders"
            className="border border-gray-300 text-gray-600 px-4 py-2 rounded-lg text-sm font-medium hover:bg-gray-50 transition-colors no-underline"
          >
            All orders
          </Link>
          <Link
            to="/admin/new"
            className="bg-cyan-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-cyan-700 transition-colors no-underline"
          >
            + New event
          </Link>
        </div>
      </div>

      {deleteError && (
        <p className="text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-3 py-2 mb-4">{deleteError}</p>
      )}

      {loading ? (
        <p className="text-gray-500 text-center py-12">Loading...</p>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <ul aria-label="admin events" className="divide-y divide-gray-100 list-none p-0 m-0">
            {events.map(ev => (
              <li key={ev.id} className="flex items-center justify-between px-6 py-4 gap-4">
                <div>
                  <p className="font-medium text-gray-900">{ev.title}</p>
                  <p className="text-sm text-gray-500">{ev.venue}</p>
                </div>
                <div className="flex gap-2 shrink-0">
                  <Link
                    to={`/admin/${ev.id}/edit`}
                    className="text-sm border border-gray-300 text-gray-600 px-3 py-1.5 rounded-lg hover:bg-gray-50 transition-colors no-underline"
                  >
                    Edit
                  </Link>
                  <button
                    onClick={() => setConfirmDeleteId(ev.id)}
                    className="text-sm border border-red-200 text-red-600 px-3 py-1.5 rounded-lg hover:bg-red-50 transition-colors"
                  >
                    Delete
                  </button>
                </div>
              </li>
            ))}
          </ul>
        </div>
      )}

      {confirmDeleteId !== null && (
        <ConfirmDialog
          title="Delete event?"
          message={<>Are you sure you want to delete <span className="font-medium text-gray-900">"{confirmingEvent?.title}"</span>? This cannot be undone.</>}
          confirmLabel="Delete"
          onConfirm={handleDeleteConfirmed}
          onCancel={() => setConfirmDeleteId(null)}
        />
      )}
    </div>
  )
}
