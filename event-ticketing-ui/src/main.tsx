import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { loadStripe } from '@stripe/stripe-js'
import { Elements } from '@stripe/react-stripe-js'
import './index.css'
import App from './App.tsx'
import { AuthProvider } from './context/AuthContext.tsx'

// loadStripe is called once at module level so the Stripe.js script is fetched
// eagerly and the same promise is reused everywhere — React Stripe requires a
// stable reference, as recreating it on every render remounts the iframe.
const stripePromise = loadStripe(import.meta.env.VITE_STRIPE_PUBLISHABLE_KEY ?? '')

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider>
      <Elements stripe={stripePromise}>
        <App />
      </Elements>
    </AuthProvider>
  </StrictMode>,
)
