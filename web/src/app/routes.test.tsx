import { render, screen } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it } from 'vitest'
import { routes } from '@/app/routes'
import { NotFoundPage } from '@/components/NotFoundPage'

describe('routes', () => {
  it('renders the landing page at the root, not the not-found page', async () => {
    const router = createMemoryRouter(routes, { initialEntries: ['/'] })

    render(<RouterProvider router={router} />)

    expect(await screen.findByRole('heading', { level: 1, name: /Let them look at everything they want/ })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Page not found' })).not.toBeInTheDocument()
  })

  it('stamps the not-found page and links it to the home page', () => {
    const router = createMemoryRouter([{ path: '*', Component: NotFoundPage }], { initialEntries: ['/nowhere'] })

    render(<RouterProvider router={router} />)

    expect(screen.getByText('Nothing to see here')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Go to the home page' })).toHaveAttribute('href', '/')
  })
})
