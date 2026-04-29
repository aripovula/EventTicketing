import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { vi } from 'vitest'
import ProfilePage from './ProfilePage'
import { AuthProvider } from '../context/AuthContext'

const johnUser = { userId: 1, name: 'John Doe', email: 'john@example.com', role: 'user' }
const defaultCard = { last4: '4242', cardType: 'Visa', expiryDate: '12/32' }

const notLoggedIn = { ok: false, status: 401, json: () => Promise.resolve(null) } as Response
const loggedIn    = { ok: true,  status: 200, json: () => Promise.resolve(johnUser) } as Response
const cardRes     = { ok: true,  status: 200, json: () => Promise.resolve(defaultCard) } as Response
const noCardRes   = { ok: false, status: 404, json: () => Promise.resolve(null) } as Response

// React runs children's effects before parents', so fetch order is:
//   1. AuthProvider useEffect → GET /api/auth/me
//   2. ProfilePage useEffect[user] → GET /api/auth/me/cards/default (only when user != null)

function renderProfile() {
  return render(
    <AuthProvider>
      <MemoryRouter initialEntries={['/profile']}>
        <Routes>
          <Route path="/profile" element={<ProfilePage />} />
          <Route path="/login" element={<div>Login page</div>} />
        </Routes>
      </MemoryRouter>
    </AuthProvider>
  )
}

beforeEach(() => vi.stubGlobal('fetch', vi.fn()))
afterEach(() => vi.unstubAllGlobals())

// ── Auth redirect ──────────────────────────────────────────────────────────────

test('redirects to /login when not logged in', async () => {
  vi.mocked(fetch).mockResolvedValue(notLoggedIn)
  renderProfile()
  await waitFor(() => expect(screen.getByText('Login page')).toBeInTheDocument())
})

// ── Account section ────────────────────────────────────────────────────────────

test('shows user name and email', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce(loggedIn)
    .mockResolvedValueOnce(noCardRes)
  renderProfile()
  await waitFor(() => expect(screen.getByText('John Doe')).toBeInTheDocument())
  expect(screen.getByText('john@example.com')).toBeInTheDocument()
})

// ── Payment method section ─────────────────────────────────────────────────────

test('shows existing default card details', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce(loggedIn)
    .mockResolvedValueOnce(cardRes)
  renderProfile()
  await waitFor(() => expect(screen.getByText(/Visa ending in 4242/i)).toBeInTheDocument())
  expect(screen.getByText(/Expires 12\/32/i)).toBeInTheDocument()
})

test('shows no card message when user has no default card', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce(loggedIn)
    .mockResolvedValueOnce(noCardRes)
  renderProfile()
  await waitFor(() => expect(screen.getByText(/no default card saved yet/i)).toBeInTheDocument())
})

test('shows Save card button when no card exists', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce(loggedIn)
    .mockResolvedValueOnce(noCardRes)
  renderProfile()
  await waitFor(() => expect(screen.getByRole('button', { name: /save card/i })).toBeInTheDocument())
})

test('shows Update card button when card exists', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce(loggedIn)
    .mockResolvedValueOnce(cardRes)
  renderProfile()
  await waitFor(() => expect(screen.getByRole('button', { name: /update card/i })).toBeInTheDocument())
})

test('pre-fills card type and expiry from existing card', async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce(loggedIn)
    .mockResolvedValueOnce(cardRes)
  renderProfile()
  await waitFor(() => screen.getByText(/Visa ending in 4242/i))
  expect((screen.getByLabelText(/card type/i) as HTMLSelectElement).value).toBe('Visa')
  expect((screen.getByLabelText(/expiry/i) as HTMLInputElement).value).toBe('12/32')
})

// ── Saving a card ──────────────────────────────────────────────────────────────

test('submitting the form calls PUT /api/auth/me/cards/default', async () => {
  const user = userEvent.setup()
  const fetchMock = vi.mocked(fetch)
  fetchMock
    .mockResolvedValueOnce(loggedIn)
    .mockResolvedValueOnce(noCardRes)
    .mockResolvedValueOnce({ ok: true, status: 204 } as Response)

  renderProfile()
  await waitFor(() => screen.getByRole('button', { name: /save card/i }))

  await user.type(screen.getByLabelText(/card number/i), '4111111111114242')
  await user.clear(screen.getByLabelText(/expiry/i))
  await user.type(screen.getByLabelText(/expiry/i), '12/29')
  await user.click(screen.getByRole('button', { name: /save card/i }))

  await waitFor(() =>
    expect(fetchMock).toHaveBeenCalledWith('/api/auth/me/cards/default', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ cardNumber: '4111111111114242', cardType: 'Visa', expiryDate: '12/29' }),
    })
  )
})

test('shows success message after saving', async () => {
  const user = userEvent.setup()
  vi.mocked(fetch)
    .mockResolvedValueOnce(loggedIn)
    .mockResolvedValueOnce(noCardRes)
    .mockResolvedValueOnce({ ok: true, status: 204 } as Response)

  renderProfile()
  await waitFor(() => screen.getByRole('button', { name: /save card/i }))

  await user.type(screen.getByLabelText(/card number/i), '4111111111114242')
  await user.type(screen.getByLabelText(/expiry/i), '12/29')
  await user.click(screen.getByRole('button', { name: /save card/i }))

  await waitFor(() => expect(screen.getByText(/card saved successfully/i)).toBeInTheDocument())
})

test('shows error message when save fails', async () => {
  const user = userEvent.setup()
  vi.mocked(fetch)
    .mockResolvedValueOnce(loggedIn)
    .mockResolvedValueOnce(noCardRes)
    .mockResolvedValueOnce({ ok: false, status: 400 } as Response)

  renderProfile()
  await waitFor(() => screen.getByRole('button', { name: /save card/i }))

  await user.type(screen.getByLabelText(/card number/i), '4111111111114242')
  await user.type(screen.getByLabelText(/expiry/i), '12/29')
  await user.click(screen.getByRole('button', { name: /save card/i }))

  await waitFor(() => expect(screen.getByText(/failed to save card/i)).toBeInTheDocument())
})
