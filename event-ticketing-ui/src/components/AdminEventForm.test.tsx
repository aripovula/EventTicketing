import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import AdminEventForm from './AdminEventForm'

const mockEvent = {
  id: 1,
  title: 'Jazz Night',
  description: 'An evening of live jazz music.',
  startTime: '2026-08-15T20:00:00',
  endTime:   '2026-08-15T23:00:00',
  eventType: 'Music',
  venue: 'Blue Note Club',
  totalSeats: 100,
  availableSeats: 80,
  price: 25,
  imageUrl: 'https://images.unsplash.com/photo-123',
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
  expect(screen.getByLabelText('Image URL')).toHaveValue('')
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
  fireEvent.change(screen.getByLabelText('Start time'), { target: { value: '2026-09-01T10:00' } })
  fireEvent.change(screen.getByLabelText('End time'), { target: { value: '2026-09-01T12:00' } })

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
  expect(screen.getByLabelText('Image URL')).toHaveValue('https://images.unsplash.com/photo-123')
})

test('edit form shows image preview when imageUrl is present', async () => {
  vi.mocked(fetch).mockResolvedValue({ json: () => Promise.resolve(mockEvent) } as Response)
  renderEditForm()
  await waitFor(() => screen.getByLabelText('Title'))
  const preview = screen.getByRole('img', { name: 'Preview' })
  expect(preview).toHaveAttribute('src', 'https://images.unsplash.com/photo-123')
})

test('does not show image preview when imageUrl is empty', () => {
  renderCreateForm()
  expect(screen.queryByRole('img', { name: 'Preview' })).not.toBeInTheDocument()
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

// ── New fields: Start time, End time, Event type ───────────────────────────────

test('create form renders Start time and End time fields', () => {
  renderCreateForm()
  expect(screen.getByLabelText('Start time')).toBeInTheDocument()
  expect(screen.getByLabelText('End time')).toBeInTheDocument()
})

test('create form renders Event type dropdown', () => {
  renderCreateForm()
  expect(screen.getByLabelText('Event type')).toBeInTheDocument()
})

test('Event type dropdown contains expected options', () => {
  renderCreateForm()
  const select = screen.getByLabelText('Event type') as HTMLSelectElement
  const options = Array.from(select.options).map(o => o.value)
  expect(options).toContain('Music')
  expect(options).toContain('Sports')
  expect(options).toContain('Comedy')
  expect(options).toContain('Tech')
  expect(options).toContain('Art')
})

test('edit form pre-populates startTime and endTime from fetched event', async () => {
  vi.mocked(fetch).mockResolvedValue({ json: () => Promise.resolve(mockEvent) } as Response)
  renderEditForm()
  await waitFor(() => screen.getByLabelText('Title'))
  expect((screen.getByLabelText('Start time') as HTMLInputElement).value).toBe('2026-08-15T20:00')
  expect((screen.getByLabelText('End time') as HTMLInputElement).value).toBe('2026-08-15T23:00')
})

test('edit form pre-populates eventType from fetched event', async () => {
  vi.mocked(fetch).mockResolvedValue({ json: () => Promise.resolve(mockEvent) } as Response)
  renderEditForm()
  await waitFor(() => screen.getByLabelText('Title'))
  expect((screen.getByLabelText('Event type') as HTMLSelectElement).value).toBe('Music')
})

test('submitted create form body includes startTime, endTime, and eventType', async () => {
  vi.mocked(fetch).mockResolvedValue({ json: () => Promise.resolve({ id: 3 }) } as Response)
  const user = userEvent.setup()
  renderCreateForm()

  await user.type(screen.getByLabelText('Title'), 'New Show')
  await user.type(screen.getByLabelText('Venue'), 'Arena')
  await user.type(screen.getByLabelText('Description'), 'Desc.')
  fireEvent.change(screen.getByLabelText('Start time'), { target: { value: '2026-09-01T19:00' } })
  fireEvent.change(screen.getByLabelText('End time'), { target: { value: '2026-09-01T22:00' } })
  await user.selectOptions(screen.getByLabelText('Event type'), 'Sports')

  fireEvent.submit(screen.getByRole('form', { name: /event form/i }))

  await waitFor(() => {
    const postCall = vi.mocked(fetch).mock.calls.find(([, opts]) => opts?.method === 'POST')
    expect(postCall).toBeDefined()
    const body = JSON.parse(postCall![1]!.body as string)
    expect(body.startTime).toBe('2026-09-01T19:00')
    expect(body.endTime).toBe('2026-09-01T22:00')
    expect(body.eventType).toBe('Sports')
  })
})
