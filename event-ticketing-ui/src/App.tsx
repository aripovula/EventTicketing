import { BrowserRouter, Link, Route, Routes, useNavigate } from 'react-router-dom'
import EventList from './components/EventList'
import EventDetail from './components/EventDetail'
import AdminPage from './components/AdminPage'
import AdminEventForm from './components/AdminEventForm'
import OrderConfirmation from './components/OrderConfirmation'
import OrdersList from './components/OrdersList'
import AdminOrders from './components/AdminOrders'
import AdminSummary from './components/AdminSummary'
import LoginPage from './components/LoginPage'
import ProfilePage from './components/ProfilePage'
import RequireAdmin from './components/RequireAdmin'
import { useAuth } from './context/AuthContext'

function HeaderNav() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const handleLogout = async () => {
    await logout()
    navigate('/login')
  }

  if (user) {
    return (
      <nav className="flex items-center gap-4">
        <Link to="/orders" className="text-sm text-gray-500 hover:text-cyan-600 transition-colors">
          My orders
        </Link>
        <Link to="/profile" className="text-sm text-gray-500 hover:text-cyan-600 transition-colors">
          Profile
        </Link>
        {user.role === 'admin' && (
          <Link to="/admin" className="text-sm text-gray-500 hover:text-cyan-600 transition-colors">
            Admin
          </Link>
        )}
        <button
          onClick={handleLogout}
          className="text-sm border border-gray-300 text-gray-600 px-3 py-1 rounded-lg hover:bg-gray-50 transition-colors"
        >
          Sign out
        </button>
      </nav>
    )
  }

  return (
    <nav className="flex items-center gap-4">
      <Link to="/orders" className="text-sm text-gray-500 hover:text-cyan-600 transition-colors">
        My orders
      </Link>
      <Link to="/login" className="text-sm text-gray-500 hover:text-cyan-600 transition-colors">
        Sign in
      </Link>
    </nav>
  )
}

function App() {
  return (
    <BrowserRouter>
      <div className="min-h-screen bg-gray-50 text-gray-800">
        <header className="bg-white border-b border-gray-200 px-6 py-4 flex items-center justify-between">
          <Link to="/" className="text-xl font-semibold text-gray-900 hover:text-cyan-600 transition-colors">
            🎟 Event Ticketing
          </Link>
          <HeaderNav />
        </header>
        <main className="max-w-4xl mx-auto px-4 py-8">
          <Routes>
            <Route path="/" element={<EventList />} />
            <Route path="/events/:id" element={<EventDetail />} />
            <Route path="/orders" element={<OrdersList />} />
            <Route path="/orders/:id" element={<OrderConfirmation />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/profile" element={<ProfilePage />} />
            <Route element={<RequireAdmin />}>
              <Route path="/admin" element={<AdminPage />} />
              <Route path="/admin/orders" element={<AdminOrders />} />
              <Route path="/admin/summary" element={<AdminSummary />} />
              <Route path="/admin/new" element={<AdminEventForm />} />
              <Route path="/admin/:id/edit" element={<AdminEventForm />} />
            </Route>
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  )
}

export default App
