import { BrowserRouter, Route, Routes } from 'react-router-dom'
import EventList from './components/EventList'
import EventDetail from './components/EventDetail'

function App() {
  return (
    <BrowserRouter>
      <main>
        <Routes>
          <Route path="/" element={<><h1>Events</h1><EventList /></>} />
          <Route path="/events/:id" element={<EventDetail />} />
        </Routes>
      </main>
    </BrowserRouter>
  )
}

export default App
