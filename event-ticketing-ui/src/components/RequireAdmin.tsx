import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export default function RequireAdmin() {
  const { user, isLoading } = useAuth()
  const location = useLocation()

  if (isLoading) return null

  if (!user)
    return <Navigate to="/login" state={{ from: location }} replace />

  if (user.role !== 'admin')
    return (
      <div className="text-center py-16">
        <p className="text-gray-500 text-sm">You do not have permission to view this page.</p>
      </div>
    )

  return <Outlet />
}
