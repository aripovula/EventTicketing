import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { AuthProvider, useAuth, type AuthUser } from '../context/AuthContext'
import RequireAdmin from './RequireAdmin'

// Seed the AuthContext with a known user without going through /me
function Wrapper({ user }: { user: AuthUser | null }) {
  return (
    <AuthProvider>
      <AuthSeeder user={user}>
        <MemoryRouter initialEntries={['/admin']}>
          <Routes>
            <Route element={<RequireAdmin />}>
              <Route path="/admin" element={<span>admin content</span>} />
            </Route>
            <Route path="/login" element={<span>login page</span>} />
          </Routes>
        </MemoryRouter>
      </AuthSeeder>
    </AuthProvider>
  )
}

// Helper that calls login() immediately so tests don't need to go through fetch
function AuthSeeder({ user, children }: { user: AuthUser | null; children: React.ReactNode }) {
  const { login, isLoading } = useAuth()
  if (!isLoading && user) login(user)
  return <>{children}</>
}

const adminUser: AuthUser  = { userId: 4, name: 'Admin', email: 'admin@example.com', role: 'admin' }
const regularUser: AuthUser = { userId: 1, name: 'John',  email: 'john@example.com',  role: 'user' }

beforeEach(() => {
  // stub /me to return 401 so AuthProvider settles quickly
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 401 })))
})
afterEach(() => vi.restoreAllMocks())

test('renders protected content for admin users', async () => {
  render(<Wrapper user={adminUser} />)
  await waitFor(() => expect(screen.queryByText('admin content')).toBeInTheDocument())
})

test('redirects to /login when user is not logged in', async () => {
  render(<Wrapper user={null} />)
  await waitFor(() => expect(screen.queryByText('login page')).toBeInTheDocument())
})

test('shows permission-denied message for logged-in non-admin', async () => {
  render(<Wrapper user={regularUser} />)
  await waitFor(() =>
    expect(screen.queryByText(/do not have permission/i)).toBeInTheDocument()
  )
})

test('does not show admin content to regular user', async () => {
  render(<Wrapper user={regularUser} />)
  await waitFor(() => expect(screen.queryByText('admin content')).not.toBeInTheDocument())
})
