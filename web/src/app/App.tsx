import { QueryClientProvider } from '@tanstack/react-query'
import { createBrowserRouter, RouterProvider } from 'react-router'
import { queryClient } from '@/app/query-client'
import { routes } from '@/app/routes'

const router = createBrowserRouter(routes)

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  )
}
