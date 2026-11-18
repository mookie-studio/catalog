import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { LanguageSwitcher } from './LanguageSwitcher'
import { landingCopy } from './landing-copy'
import { LANDING_LANGUAGE_KEY, pickLandingLanguage, useLandingLanguage } from './landing-language'

describe('pickLandingLanguage', () => {
  it.each([
    [null, ['es-ES'], 'es'],
    [null, ['es-MX', 'en'], 'es'],
    [null, ['en-GB', 'es'], 'en'],
    [null, ['fr-FR', 'es'], 'es'],
    [null, ['fr-FR'], 'en'],
    [null, [], 'en'],
    ['en', ['es-ES'], 'en'],
    ['es', ['en-US'], 'es'],
    ['de', ['es-ES'], 'es'],
  ] as const)('(%s, %j) is %s', (saved, languages, expected) => {
    expect(pickLandingLanguage(saved, languages)).toBe(expected)
  })
})

describe('landing copy', () => {
  it('has the agreed texts in both languages', () => {
    expect(landingCopy.en.hero.tryDemo).toBe('Try the demo')
    expect(landingCopy.es.hero.tryDemo).toBe('Probar la demo')
    expect(landingCopy.en.tag.stamp).toBe('Just looking')
    expect(landingCopy.es.tag.stamp).toBe('Solo mirando')
    const headline = (language: 'en' | 'es') => {
      const hero = landingCopy[language].hero
      return hero.headlineBefore + hero.headlineHighlight + hero.headlineAfter
    }
    expect(headline('en')).toBe('Let them look at everything they want.')
    expect(headline('es')).toBe('Que miren todo lo que quieran.')
  })
})

function LandingHarness() {
  const { language, copy, setLanguage } = useLandingLanguage()
  return (
    <>
      <LanguageSwitcher language={language} copy={copy} onChange={setLanguage} />
      <a href="/forja-hardware">{copy.hero.tryDemo}</a>
    </>
  )
}

describe('language switcher', () => {
  let languages: readonly string[] = ['en-US']

  beforeEach(() => {
    vi.spyOn(navigator, 'languages', 'get').mockImplementation(() => languages)
    window.localStorage.clear()
    document.documentElement.setAttribute('lang', 'en')
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('switches the texts to Spanish, marks ES as pressed and saves the choice', async () => {
    languages = ['en-US']
    render(<LandingHarness />)
    expect(screen.getByRole('link', { name: 'Try the demo' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'English' })).toHaveAttribute('aria-pressed', 'true')

    await userEvent.click(screen.getByRole('button', { name: 'Español' }))

    expect(screen.getByRole('link', { name: 'Probar la demo' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Español' })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByRole('button', { name: 'English' })).toHaveAttribute('aria-pressed', 'false')
    expect(window.localStorage.getItem(LANDING_LANGUAGE_KEY)).toBe('es')
    expect(document.documentElement).toHaveAttribute('lang', 'es')
  })

  it('follows a Spanish browser when nothing is saved', () => {
    languages = ['es-ES']
    render(<LandingHarness />)
    expect(screen.getByRole('link', { name: 'Probar la demo' })).toBeInTheDocument()
  })

  it('keeps a saved English choice over a Spanish browser after remounting', async () => {
    languages = ['es-ES']
    const first = render(<LandingHarness />)
    await userEvent.click(screen.getByRole('button', { name: 'English' }))
    first.unmount()

    render(<LandingHarness />)
    expect(screen.getByRole('link', { name: 'Try the demo' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'English' })).toHaveAttribute('aria-pressed', 'true')
  })

  it('restores the previous html lang on unmount', () => {
    languages = ['es-ES']
    const view = render(<LandingHarness />)
    expect(document.documentElement).toHaveAttribute('lang', 'es')
    view.unmount()
    expect(document.documentElement).toHaveAttribute('lang', 'en')
  })

  it('falls back to the browser rule when storage is blocked', () => {
    languages = ['es-MX']
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('blocked')
    })
    render(<LandingHarness />)
    expect(screen.getByRole('link', { name: 'Probar la demo' })).toBeInTheDocument()
  })
})
