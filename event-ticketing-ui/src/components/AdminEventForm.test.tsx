import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import AdminEventForm from './AdminEventForm'

const mockEvent = {
  id: 1,
  title: 'Jazz Night',
  description: 'An evening of live jazz music.',
  date: '2026-08-15T20:00:00',
  venue: 'Blue Note Club',
  totalSeats: 100,
  availableSeats: 80,
  price: 25,
}

function renderCreateForm() {
  return render(
    <MemoryRouter initialEntries={['/admin/new']}>
      <Routes>
        <Route path="/admin/new" element={<AdminEventForm />} />
        <Route path="/admin" element={<div>Admin list</div>} />
      </Routes>
    </MemoryRouter>
  )
}

function renderEditForm(id = '1') {
  return render(
    <MemoryRouter initialEntries={[`/admin/${id}/edit`]}>
      <Routes>
        <Route path="/admin/:id/edit" element={<AdminEventForm />} />
        <Route path="/admin" element={<div>Admin list</div>} />
      </Routes>
    </MemoryRouter>
  )
}

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn())
})

afterEach(() => {
  vi.unstubAllGlobals()
})

// Create mode

test('create form shows New event heading', () => {
  renderCreateForm()
  expect(screen.getByRole('heading', { name: /new event/i })).toBeInTheDocument()
})

test('create form shows all fields empty', () => {
  renderCreateForm()
  expect(screen.getByLabelText('Title')).toHaveValue('')
  expect(screen.getByLabelText('Venue')).toHaveValue('')
})

test('create form shows Create event button', () => {
  renderCreateForm()
  expect(screen.getByRole('button', { name: /create event/i })).toBeInTheDocument()
})

test('cancel link points to /admin', () => {
  renderCreateForm()
  expect(screen.getByRole('link', { name: /cancel/i })).toHaveAttribute('href', '/admin')
})

test('submitting create form calls POST and navigates to /admin', async () => {
  vi.mocked(fetch).mockResolvedValue({ json: () => Promise.resolve({ id: 3 }) } as Response)
  const user = userEvent.setup()

  renderCreateForm()

  await user.type(screen.getByLabelText('Title'), 'New Show')
  await user.type(screen.getByLabelText('Venue'), 'Arena')
  await user.type(screen.getByLabelText('Description'), 'Desc.')
  fireEvent.change(screen.getByLabelText('Date'), { target: { value: '2026-09-01T10:00' } })

  fireEvent.submit(screen.getByRole('form', { name: /event form/i }))

  await waitFor(() => {
    const postCall = vi.mocked(fetch).mock.calls.find(([, opts]) => opts?.method === 'POST')
    expect(postCall).toBeDefined()
    expect(postCall![0]).toBe('/api/events')
  })

  await waitFor(() => expect(screen.getByText('Admin list')).toBeInTheDocument())
})

// Edit mode

test('edit form shows loading state initially', () => {
  vi.mocked(fetch).mockResolvedValue({ json: () => Promise.resolve(mockEvent) } as Response)
  renderEditForm()
  expect(screen.getByText('Loading...')).toBeInTheDocument()
})

test('edit form shows Edit event heading after fetch', async () => {
  vi.mocked(fetch).mockResolvedValue({ json: () => Promise.resolve(mockEvent) } as Response)
  renderEditForm()
  await waitFor(() => expect(screen.getByRole('heading', { name: /edit event/i })).toBeInTheDocument())
})

test('edit form pre-populates fields from fetched event', async () => {
  vi.mocked(fetch).mockResolvedValue({ json: () => Promise.resolve(mockEvent) } as Response)
  renderEditForm()
  await waitFor(() => expect(screen.getByLabelText('Title')).toHaveValue('Jazz Night'))
  expect(screen.getByLabelText('Venue')).toHaveValue('Blue Note Club')
})

test('submitting edit form calls PUT and navigates to /admin', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce({ json: () => Promise.resolve(mockEvent) } as Response)
    .mockResolvedValueOnce({ json: () => Promise.resolve({}) } as Response)

  renderEditForm()
  await waitFor(() => screen.getByLabelText('Title'))

  fireEvent.submit(screen.getByRole('form', { name: /event form/i }))

  await waitFor(() => {
    const putCall = vi.mocked(fetch).mock.calls.find(([, opts]) => opts?.method === 'PUT')
    expect(putCall).toBeDefined()
    expect(putCall![0]).toBe('/api/events/1')
  })

  await waitFor(() => expect(screen.getByText('Admin list')).toBeInTheDocument())
})
