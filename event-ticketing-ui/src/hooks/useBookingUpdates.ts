import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { useEffect, useRef } from 'react'

/**
 * Connects to the TicketingHub and calls `onBookingMade(eventId)` whenever
 * the server broadcasts a BookingMade message.
 *
 * The connection is established once on mount and torn down on unmount.
 * Reconnects automatically on transient network failures.
 */
export function useBookingUpdates(onBookingMade: (eventId: number) => void): void {
  // Keep a ref so the latest callback is always used without restarting the connection
  const callbackRef = useRef(onBookingMade)
  callbackRef.current = onBookingMade

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/ticketing')
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on('BookingMade', (eventId: number) => callbackRef.current(eventId))

    connection.start().catch(err =>
      console.error('SignalR connection failed:', err)
    )

    return () => {
      connection.stop()
    }
  }, []) // connection setup only once; callback changes handled via ref
}
