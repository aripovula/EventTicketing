type Props = {
  eventTitle: string
  eventDate: string
  eventVenue: string
  price: number
  savedEmail?: string
  savedCardLast4?: string
  onConfirm: (email: string, cardLast4: string) => Promise<void>
  onClose: () => void
  error: string | null
}

export default function BookingModal({ eventTitle, eventDate, eventVenue, price }: Props) {

  const formattedDate = new Date(eventDate).toLocaleDateString(undefined, { dateStyle: 'long' })

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="booking-modal-title"
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
    >
      <div className="bg-white rounded-xl border border-gray-200 p-6 w-full max-w-md mx-4 shadow-lg">
        <h2 id="booking-modal-title" className="text-xl font-bold text-gray-900 mb-1">
          Place order
        </h2>
        <p className="text-sm text-gray-500 mb-5">
          {eventTitle} · {eventVenue} · {formattedDate} &mdash; <span className="font-medium text-gray-700">${price}</span>
        </p>
      </div>
    </div>
  )
}
