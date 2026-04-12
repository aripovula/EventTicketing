import { BrowserRouter, Route, Routes } from 'react-router-dom'
import EventList from './components/EventList'

function App() {
  return (
    <BrowserRouter>
      <main>
        <Routes>
          <Route path="/" element={<><h1>Events</h1><EventList /></>} />
        </Routes>
      </main>
    </BrowserRouter>
  )
}

export default App
