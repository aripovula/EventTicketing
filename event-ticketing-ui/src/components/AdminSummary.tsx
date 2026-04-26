import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useBookingUpdates } from '../hooks/useBookingUpdates'

type EventSummary = {
  eventId: number
  title: string
  openingBalance: number
  soldSeats: number
  remainingSeats: number
  revenue: number
}

export default function AdminSummary() {
  const [rows, setRows] = useState<EventSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)
  const [asOf, setAsOf] = useState<Date | null>(null)

  const fetchSummary = useCallback(() => {
    fetch('/api/admin/summary')
      .then(res => {
        if (!res.ok) throw new Error()
        return res.json()
      })
      .then((data: EventSummary[]) => { setRows(data); setAsOf(new Date()); setLoading(false) })
      .catch(() => { setError(true); setLoading(false) })
  }, [])

  useEffect(() => { fetchSummary() }, [fetchSummary])

  useBookingUpdates(fetchSummary)

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900 m-0">Seat summary</h1>
        <Link to="/admin" className="text-sm text-indigo-600 hover:text-indigo-800 transition-colors no-underline">
          ← Back to admin
        </Link>
      </div>

      {loading && <p className="text-gray-500 text-center py-12">Loading...</p>}
      {error && <p className="text-red-500 text-center py-12">Failed to load summary.</p>}

      {!loading && !error && (
        <>
        <p className="text-sm text-gray-500 mb-4">
          Data as of{' '}
          <span className="font-medium text-gray-700">
            {asOf?.toLocaleString(undefined, { dateStyle: 'long', timeStyle: 'medium' })}
          </span>
        </p>
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-6 py-3 font-medium text-gray-500">Event</th>
                <th className="text-right px-6 py-3 font-medium text-gray-500">Opening balance</th>
                <th className="text-right px-6 py-3 font-medium text-gray-500">Sold</th>
                <th className="text-right px-6 py-3 font-medium text-gray-500">Remaining</th>
                <th className="text-right px-6 py-3 font-medium text-gray-500">Revenue</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {rows.map(row => (
                <tr key={row.eventId} className="hover:bg-gray-50 transition-colors">
                  <td className="px-6 py-4 font-medium text-gray-900">{row.title}</td>
                  <td className="px-6 py-4 text-right text-gray-700">{row.openingBalance}</td>
                  <td className="px-6 py-4 text-right text-indigo-600 font-medium">{row.soldSeats}</td>
                  <td className="px-6 py-4 text-right text-gray-700">{row.remainingSeats}</td>
                  <td className="px-6 py-4 text-right font-medium text-green-600">${row.revenue.toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        </>
      )}
    </div>
  )
}
