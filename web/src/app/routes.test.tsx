import { render, screen } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it } from 'vitest'
import { routes } from '@/app/routes'

describe('routes', () => {
  it('renders the not-found page for an unknown address', async () => {
    const router = createMemoryRouter(routes, { initialEntries: ['/'] })

    render(<RouterProvider router={router} />)

    expect(await screen.findByRole('heading', { name: 'Page not found' })).toBeInTheDocument()
  })

  it('stamps the not-found page and links it to the home page', async () => {
    const router = createMemoryRouter(routes, { initialEntries: ['/'] })

    render(<RouterProvider router={router} />)

    expect(await screen.findByText('Nothing to see here')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Go to the home page' })).toHaveAttribute('href', '/')
  })
})
