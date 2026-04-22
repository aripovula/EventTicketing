import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { AuthProvider } from '../context/AuthContext'
import LoginPage from './LoginPage'

const johnUser = { userId: 1, name: 'John Doe',  email: 'john@example.com', role: 'user' }
const adminUser = { userId: 4, name: 'Admin',     email: 'admin@example.com', role: 'admin' }

// Stub /api/auth/me (called by AuthProvider on mount) to return 401
// and optionally stub the login call
function renderPage(loginResponse: Response = new Response(null, { status: 401 })) {
  const fetchMock = vi.fn()
    .mockResolvedValueOnce(new Response(null, { status: 401 }))  // /me on mount
    .mockResolvedValue(loginResponse)                             // subsequent calls

  vi.stubGlobal('fetch', fetchMock)

  render(
    <AuthProvider>
      <MemoryRouter initialEntries={['/login']}>
        <LoginPage />
      </MemoryRouter>
    </AuthProvider>,
  )

  return { fetchMock }
}

afterEach(() => vi.restoreAllMocks())

// ── Layout ─────────────────────────────────────────────────────────────────────

test('renders user panel and admin panel', () => {
  renderPage()
  expect(screen.getByText('Sign in as a regular user')).toBeInTheDocument()
  expect(screen.getByText('Sign in as admin')).toBeInTheDocument()
})

test('renders all three regular users as radio options', () => {
  renderPage()
  expect(screen.getByLabelText(/john doe/i, { selector: 'input[type="radio"]' })).toBeInTheDocument()
  expect(screen.getByLabelText(/jane doer/i, { selector: 'input[type="radio"]' })).toBeInTheDocument()
  expect(screen.getByLabelText(/alex johnson/i, { selector: 'input[type="radio"]' })).toBeInTheDocument()
})

test('renders admin as radio option', () => {
  renderPage()
  expect(screen.getByLabelText(/admin/i, { selector: 'input[type="radio"]' })).toBeInTheDocument()
})

test('shows the dual-browser tip banner', () => {
  renderPage()
  expect(screen.getByText(/log in here as a regular user/i)).toBeInTheDocument()
})

test('shows SignalR planned note in tip banner', () => {
  renderPage()
  expect(screen.getByText(/signalr/i)).toBeInTheDocument()
})

// ── Pre-fill behaviour ─────────────────────────────────────────────────────────

test('user panel pre-selects John Doe by default', () => {
  renderPage()
  const john = screen.getAllByRole('radio').find(r =>
    (r as HTMLInputElement).value === 'john@example.com'
  ) as HTMLInputElement
  expect(john.checked).toBe(true)
})

test('selecting Jane pre-fills her email in the user panel', async () => {
  renderPage()
  const jane = screen.getAllByRole('radio').find(r =>
    (r as HTMLInputElement).value === 'jane@example.com'
  )!
  await userEvent.click(jane)

  const emailInputs = screen.getAllByLabelText('Email')
  // user panel email input (first one) should show jane's email
  expect((emailInputs[0] as HTMLInputElement).value).toBe('jane@example.com')
})

test('admin panel pre-fills admin email', () => {
  renderPage()
  const emailInputs = screen.getAllByLabelText('Email')
  expect((emailInputs[1] as HTMLInputElement).value).toBe('admin@example.com')
})

test('password fields are pre-filled and read-only', () => {
  renderPage()
  const pwdInputs = screen.getAllByLabelText('Password')
  for (const input of pwdInputs) {
    expect((input as HTMLInputElement).value).toBe('Password')
    expect((input as HTMLInputElement).readOnly).toBe(true)
  }
})

// ── Login success ──────────────────────────────────────────────────────────────

test('successful user login calls POST /api/auth/login with correct body', async () => {
  const { fetchMock } = renderPage(
    new Response(JSON.stringify(johnUser), { status: 200 })
  )

  await userEvent.click(screen.getAllByRole('button', { name: 'Sign in' })[0])

  await waitFor(() =>
    expect(fetchMock).toHaveBeenCalledWith('/api/auth/login', expect.objectContaining({
      method: 'POST',
      body: JSON.stringify({ email: 'john@example.com', password: 'Password' }),
    }))
  )
})

test('successful admin login calls POST /api/auth/login with admin email', async () => {
  const { fetchMock } = renderPage(
    new Response(JSON.stringify(adminUser), { status: 200 })
  )

  await userEvent.click(screen.getAllByRole('button', { name: 'Sign in' })[1])

  await waitFor(() =>
    expect(fetchMock).toHaveBeenCalledWith('/api/auth/login', expect.objectContaining({
      body: JSON.stringify({ email: 'admin@example.com', password: 'Password' }),
    }))
  )
})

// ── Login failure ──────────────────────────────────────────────────────────────

test('failed login shows error message in the correct panel', async () => {
  renderPage(
    new Response(JSON.stringify({ message: 'Invalid email or password.' }), { status: 401 })
  )

  await userEvent.click(screen.getAllByRole('button', { name: 'Sign in' })[0])

  await waitFor(() =>
    expect(screen.getByRole('alert')).toHaveTextContent('Invalid email or password.')
  )
})

test('Sign in button is disabled while submitting', async () => {
  let resolveLogin!: (r: Response) => void
  const fetchMock = vi.fn()
    .mockResolvedValueOnce(new Response(null, { status: 401 }))
    .mockReturnValueOnce(new Promise<Response>(res => { resolveLogin = res }))

  vi.stubGlobal('fetch', fetchMock)
  render(
    <AuthProvider>
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    </AuthProvider>,
  )

  await userEvent.click(screen.getAllByRole('button', { name: 'Sign in' })[0])
  expect(screen.getAllByRole('button', { name: /signing in/i })[0]).toBeDisabled()

  resolveLogin(new Response(null, { status: 401 }))
})
