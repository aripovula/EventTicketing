import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import AdminPage from './AdminPage'

const mockEvents = [
  { id: 1, title: 'Jazz Night', description: 'Live jazz.', date: '2026-08-15T20:00:00', venue: 'Blue Note Club', totalSeats: 100, availableSeats: 100, price: 25 },
  { id: 2, title: 'Tech Conference', description: 'Dev talks.', date: '2026-07-10T09:00:00', venue: 'City Convention Centre', totalSeats: 500, availableSeats: 500, price: 149 },
]

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
    json: () => Promise.resolve(mockEvents),
  }))
})

afterEach(() => {
  vi.unstubAllGlobals()
})

function renderAdminPage() {
  return render(<MemoryRouter><AdminPage /></MemoryRouter>)
}

test('shows Admin heading and New event link', async () => {
  renderAdminPage()
  expect(screen.getByRole('heading', { name: 'Admin' })).toBeInTheDocument()
  await waitFor(() =>
    expect(screen.getByRole('link', { name: /new event/i })).toBeInTheDocument()
  )
})

test('renders existing events after fetch', async () => {
  renderAdminPage()
  await waitFor(() => expect(screen.getByText('Jazz Night')).toBeInTheDocument())
  expect(screen.getByText('Tech Conference')).toBeInTheDocument()
})
