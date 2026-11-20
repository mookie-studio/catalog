import { render, screen } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { LandingPage } from './LandingPage'
import { LANDING_LANGUAGE_KEY } from './landing-language'

function renderLanding() {
  const router = createMemoryRouter([{ path: '/', Component: LandingPage }], { initialEntries: ['/'] })
  render(<RouterProvider router={router} />)
}

describe('LandingPage', () => {
  let languages: readonly string[] = ['en-US']

  beforeEach(() => {
    window.localStorage.removeItem(LANDING_LANGUAGE_KEY)
    vi.spyOn(navigator, 'languages', 'get').mockImplementation(() => languages)
  })

  afterEach(() => {
    vi.restoreAllMocks()
    languages = ['en-US']
  })

  it('shows the headline and a Try the demo link to the forja-hardware demo', () => {
    renderLanding()

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Let them look at everything they want.')
    expect(screen.getByRole('link', { name: 'Try the demo' })).toHaveAttribute('href', '/forja-hardware')
    expect(screen.getByText('3 example shops · no sign-up')).toBeInTheDocument()
  })

  it('links each of the three tags to its demo catalog', () => {
    renderLanding()

    expect(screen.getByRole('link', { name: 'Open the Forja · hardware demo' })).toHaveAttribute('href', '/forja-hardware')
    expect(screen.getByRole('link', { name: 'Open the Lino · apparel demo' })).toHaveAttribute('href', '/lino-apparel')
    expect(screen.getByRole('link', { name: 'Open the Folio · books demo' })).toHaveAttribute('href', '/folio-books')
  })

  it('links the logo to the home page and Sign in to the sign-in page', () => {
    renderLanding()

    expect(screen.getByRole('link', { name: 'Just Looking' })).toHaveAttribute('href', '/')
    expect(screen.getByRole('link', { name: 'Sign in' })).toHaveAttribute('href', '/login')
  })

  it('is in Spanish when the browser prefers Spanish', () => {
    languages = ['es-ES']

    renderLanding()

    expect(screen.getByRole('link', { name: 'Probar la demo' })).toHaveAttribute('href', '/forja-hardware')
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Que miren todo lo que quieran.')
    expect(screen.getByRole('link', { name: 'Abrir la demo de Lino · ropa' })).toHaveAttribute('href', '/lino-apparel')
  })
})
