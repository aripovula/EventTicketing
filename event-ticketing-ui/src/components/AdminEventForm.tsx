import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'

type EventFormData = {
  title: string
  description: string
  date: string
  venue: string
  totalSeats: number
  availableSeats: number
  price: number
  imageUrl: string
}

const emptyForm: EventFormData = {
  title: '',
  description: '',
  date: '',
  venue: '',
  totalSeats: 0,
  availableSeats: 0,
  price: 0,
  imageUrl: '',
}

const inputClass = "w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-cyan-400 transition"
const labelClass = "block text-sm font-medium text-gray-700 mb-1"

export default function AdminEventForm() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const isEdit = id !== undefined

  const [form, setForm] = useState<EventFormData>(emptyForm)
  const [loading, setLoading] = useState(isEdit)

  useEffect(() => {
    if (!isEdit) return
    fetch(`/api/events/${id}`)
      .then(res => res.json())
      .then(data => {
        setForm({
          title: data.title,
          description: data.description,
          date: data.date.slice(0, 16),
          venue: data.venue,
          totalSeats: data.totalSeats,
          availableSeats: data.availableSeats,
          price: data.price,
          imageUrl: data.imageUrl ?? '',
        })
        setLoading(false)
      })
  }, [id, isEdit])

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target
    setForm(prev => ({
      ...prev,
      [name]: name === 'totalSeats' || name === 'availableSeats' || name === 'price'
        ? Number(value)
        : value,
    }))
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (isEdit) {
      await fetch(`/api/events/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ id: Number(id), ...form }),
      })
    } else {
      await fetch('/api/events', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(form),
      })
    }
    navigate('/admin')
  }

  if (loading) return <p className="text-gray-500 text-center py-12">Loading...</p>

  return (
    <div>
      <Link to="/admin" className="text-sm text-cyan-600 hover:text-cyan-800 transition-colors no-underline">
        ← Back to admin
      </Link>

      <h1 className="text-2xl font-bold text-gray-900 mt-6 mb-6">
        {isEdit ? 'Edit event' : 'New event'}
      </h1>

      <div className="bg-white rounded-xl border border-gray-200 p-6">
        <form onSubmit={handleSubmit} aria-label="event form" className="space-y-4">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div>
              <label htmlFor="title" className={labelClass}>Title</label>
              <input id="title" name="title" value={form.title} onChange={handleChange} required className={inputClass} />
            </div>
            <div>
              <label htmlFor="venue" className={labelClass}>Venue</label>
              <input id="venue" name="venue" value={form.venue} onChange={handleChange} required className={inputClass} />
            </div>
          </div>

          <div>
            <label htmlFor="description" className={labelClass}>Description</label>
            <textarea id="description" name="description" value={form.description} onChange={handleChange} required rows={3} className={inputClass} />
          </div>

          <div>
            <label htmlFor="imageUrl" className={labelClass}>Image URL</label>
            <input id="imageUrl" name="imageUrl" type="url" value={form.imageUrl} onChange={handleChange} placeholder="https://images.unsplash.com/…" className={inputClass} />
            {form.imageUrl && (
              <img src={form.imageUrl} alt="Preview" className="mt-2 h-32 w-auto rounded-lg object-cover" />
            )}
          </div>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <div>
              <label htmlFor="date" className={labelClass}>Date</label>
              <input id="date" name="date" type="datetime-local" value={form.date} onChange={handleChange} required className={inputClass} />
            </div>
            <div>
              <label htmlFor="totalSeats" className={labelClass}>Total seats</label>
              <input id="totalSeats" name="totalSeats" type="number" min="1" value={form.totalSeats} onChange={handleChange} required className={inputClass} />
            </div>
            <div>
              <label htmlFor="availableSeats" className={labelClass}>Available seats</label>
              <input id="availableSeats" name="availableSeats" type="number" min="0" value={form.availableSeats} onChange={handleChange} required className={inputClass} />
            </div>
          </div>

          <div className="sm:w-1/3">
            <label htmlFor="price" className={labelClass}>Price ($)</label>
            <input id="price" name="price" type="number" min="0" step="0.01" value={form.price} onChange={handleChange} required className={inputClass} />
          </div>

          <div className="flex gap-3 pt-2">
            <button type="submit" className="bg-cyan-600 text-white px-5 py-2 rounded-lg text-sm font-medium hover:bg-cyan-700 transition-colors">
              {isEdit ? 'Save changes' : 'Create event'}
            </button>
            <Link to="/admin" className="border border-gray-300 text-gray-600 px-5 py-2 rounded-lg text-sm font-medium hover:bg-gray-50 transition-colors no-underline">
              Cancel
            </Link>
          </div>
        </form>
      </div>
    </div>
  )
}
