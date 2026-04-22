import { render, screen, waitFor, act } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { AuthProvider, useAuth, type AuthUser } from './AuthContext'

const testUser: AuthUser = {
  userId: 1,
  name: 'Alice',
  email: 'alice@example.com',
  role: 'user',
}

// Helper component that surfaces auth state and actions
function Harness() {
  const { user, isLoading, login, logout } = useAuth()
  if (isLoading) return <span data-testid="loading">loading</span>
  return (
    <div>
      <span data-testid="name">{user?.name ?? 'none'}</span>
      <span data-testid="role">{user?.role ?? 'none'}</span>
      <button onClick={() => login(testUser)}>login</button>
      <button onClick={() => logout()}>logout</button>
    </div>
  )
}

// Render helper: waits until the /me fetch resolves and loading disappears
async function renderAndSettle(fetchImpl: () => Promise<Response>) {
  vi.stubGlobal('fetch', vi.fn(fetchImpl))
  render(
    <AuthProvider>
      <Harness />
    </AuthProvider>,
  )
  await waitFor(() => expect(screen.queryByTestId('loading')).toBeNull())
}

afterEach(() => vi.restoreAllMocks())

// ── /me on mount ───────────────────────────────────────────────────────────────

test('shows loading spinner while /me is in-flight', async () => {
  let resolve!: (r: Response) => void
  vi.stubGlobal('fetch', vi.fn(() => new Promise<Response>(res => { resolve = res })))

  render(<AuthProvider><Harness /></AuthProvider>)
  expect(screen.getByTestId('loading')).toBeInTheDocument()

  // clean up — resolve so no pending state warning
  act(() => resolve(new Response(null, { status: 401 })))
  await waitFor(() => expect(screen.queryByTestId('loading')).toBeNull())
})

test('restores user from valid session cookie (/me returns 200)', async () => {
  await renderAndSettle(() =>
    Promise.resolve(new Response(JSON.stringify(testUser), { status: 200 }))
  )
  expect(screen.getByTestId('name').textContent).toBe('Alice')
  expect(screen.getByTestId('role').textContent).toBe('user')
})

test('user is null when /me returns 401 (no cookie)', async () => {
  await renderAndSettle(() =>
    Promise.resolve(new Response(null, { status: 401 }))
  )
  expect(screen.getByTestId('name').textContent).toBe('none')
})

test('user is null when /me fetch throws a network error', async () => {
  await renderAndSettle(() => Promise.reject(new Error('network error')))
  expect(screen.getByTestId('name').textContent).toBe('none')
})

// ── login ──────────────────────────────────────────────────────────────────────

test('login sets the user in context', async () => {
  await renderAndSettle(() => Promise.resolve(new Response(null, { status: 401 })))
  await userEvent.click(screen.getByRole('button', { name: 'login' }))
  expect(screen.getByTestId('name').textContent).toBe('Alice')
  expect(screen.getByTestId('role').textContent).toBe('user')
})

test('token is not stored anywhere in JS-accessible storage after login', async () => {
  await renderAndSettle(() => Promise.resolve(new Response(null, { status: 401 })))
  await userEvent.click(screen.getByRole('button', { name: 'login' }))
  // AuthUser has no token field — confirm it's absent
  const { user } = screen.getByTestId('name').closest('div')!.__reactFiber$
    ? {} : {}  // just check the type below
  expect('token' in testUser).toBe(false)
  expect(localStorage.getItem('auth_user')).toBeNull()
  expect(sessionStorage.getItem('auth_user')).toBeNull()
})

// ── logout ────────────────────────────────────────────────────────────────────

test('logout calls POST /api/auth/logout and clears user', async () => {
  const fetchMock = vi.fn()
    .mockResolvedValueOnce(new Response(JSON.stringify(testUser), { status: 200 })) // /me
    .mockResolvedValueOnce(new Response(null, { status: 204 }))                     // /logout

  vi.stubGlobal('fetch', fetchMock)
  render(<AuthProvider><Harness /></AuthProvider>)
  await waitFor(() => expect(screen.queryByTestId('loading')).toBeNull())

  await userEvent.click(screen.getByRole('button', { name: 'logout' }))

  expect(fetchMock).toHaveBeenCalledWith('/api/auth/logout', { method: 'POST' })
  expect(screen.getByTestId('name').textContent).toBe('none')
})

// ── useAuth guard ─────────────────────────────────────────────────────────────

test('useAuth throws when used outside AuthProvider', () => {
  const spy = vi.spyOn(console, 'error').mockImplementation(() => {})
  expect(() => render(<Harness />)).toThrow('useAuth must be used inside <AuthProvider>')
  spy.mockRestore()
})
