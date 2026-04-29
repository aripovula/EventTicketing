import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { useEffect, useLayoutEffect, useRef } from 'react'

type Handlers = Record<string, (...args: unknown[]) => void>

/**
 * Connects to the TicketingHub once and registers one handler per entry in
 * `handlers`. The connection is torn down on unmount and reconnects
 * automatically on transient network failures.
 *
 * Because all handlers share one WebSocket connection, callers should prefer
 * this hook over multiple `useHubEvents` calls when listening to several
 * hub events on the same component.
 *
 * Handler callbacks are always the latest ones — updates to the `handlers`
 * object do not restart the connection.
 */
export function useHubEvents(handlers: Handlers): void {
  const handlersRef = useRef<Handlers>(handlers)

  // Keep the ref in sync with the latest handlers without restarting the connection.
  useLayoutEffect(() => {
    handlersRef.current = handlers
  })

  // Capture event names at mount time; they must remain stable across renders.
  const eventNamesRef = useRef(Object.keys(handlers))

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/ticketing')
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    for (const name of eventNamesRef.current) {
      connection.on(name, (...args: unknown[]) =>
        handlersRef.current[name]?.(...args)
      )
    }

    connection.start().catch(err =>
      console.error('SignalR connection failed:', err)
    )

    return () => {
      connection.stop()
    }
  }, []) // connection setup only once; callback changes handled via ref
}
