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

test('shows Edit link and Delete button for each event', async () => {
  renderAdminPage()
  await waitFor(() => screen.getByText('Jazz Night'))
  expect(screen.getAllByRole('link', { name: /edit/i })).toHaveLength(2)
  expect(screen.getAllByRole('button', { name: /delete/i })).toHaveLength(2)
})

test('Edit link points to the correct edit route', async () => {
  renderAdminPage()
  await waitFor(() => screen.getByText('Jazz Night'))
  const editLinks = screen.getAllByRole('link', { name: /edit/i })
  expect(editLinks[0]).toHaveAttribute('href', '/admin/1/edit')
  expect(editLinks[1]).toHaveAttribute('href', '/admin/2/edit')
})

test('New event link points to /admin/new', async () => {
  renderAdminPage()
  await waitFor(() => screen.getByRole('link', { name: /new event/i }))
  expect(screen.getByRole('link', { name: /new event/i })).toHaveAttribute('href', '/admin/new')
})

test('clicking delete calls DELETE and removes event from list', async () => {
  const user = userEvent.setup()
  vi.mocked(fetch)
    .mockResolvedValueOnce({ json: () => Promise.resolve(mockEvents) } as Response)
    .mockResolvedValueOnce({ json: () => Promise.resolve({}) } as Response)

  renderAdminPage()
  await waitFor(() => screen.getByText('Jazz Night'))

  await user.click(screen.getAllByRole('button', { name: /delete/i })[0])

  const deleteCall = vi.mocked(fetch).mock.calls.find(([, opts]) => opts?.method === 'DELETE')
  expect(deleteCall).toBeDefined()
  expect(deleteCall![0]).toBe('/api/events/1')
  await waitFor(() => expect(screen.queryByText('Jazz Night')).not.toBeInTheDocument())
})
