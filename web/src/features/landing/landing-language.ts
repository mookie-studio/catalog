import { useCallback, useEffect, useState } from 'react'
import { landingCopy, type LandingCopy, type LandingLanguage } from './landing-copy'

/** localStorage key of the visitor's own language choice on the landing page. */
export const LANDING_LANGUAGE_KEY = 'jl.landingLanguage'

function isLandingLanguage(value: string | null): value is LandingLanguage {
  return value === 'en' || value === 'es'
}

/**
 * The landing page language. A saved choice ('en' or 'es') always wins; any other saved value is
 * ignored. Otherwise the browser decides: Spanish when the first of its languages that is English
 * or Spanish is Spanish (es, es-ES, es-MX…), English in every other case, including no languages.
 */
export function pickLandingLanguage(saved: string | null, languages: readonly string[]): LandingLanguage {
  if (isLandingLanguage(saved)) return saved
  for (const language of languages) {
    const primary = language.split('-')[0].toLowerCase()
    if (primary === 'es') return 'es'
    if (primary === 'en') return 'en'
  }
  return 'en'
}

// Storage can throw (private windows, blocked site data); the browser rule is the fallback.
function readSaved(): string | null {
  try {
    return window.localStorage.getItem(LANDING_LANGUAGE_KEY)
  } catch {
    return null
  }
}

function save(language: LandingLanguage) {
  try {
    window.localStorage.setItem(LANDING_LANGUAGE_KEY, language)
  } catch {
    // The choice still applies to this visit.
  }
}

function browserLanguages(): readonly string[] {
  if (typeof navigator === 'undefined') return []
  return navigator.languages?.length ? navigator.languages : navigator.language ? [navigator.language] : []
}

/**
 * The landing page's language and texts. `setLanguage` switches the page at once and saves the
 * choice. While mounted, `<html lang>` follows the language; the previous value comes back on unmount.
 */
export function useLandingLanguage(): {
  language: LandingLanguage
  copy: LandingCopy
  setLanguage: (language: LandingLanguage) => void
} {
  const [language, setLanguageState] = useState<LandingLanguage>(() => pickLandingLanguage(readSaved(), browserLanguages()))

  useEffect(() => {
    const root = document.documentElement
    const previous = root.getAttribute('lang')
    root.setAttribute('lang', language)
    return () => {
      if (previous === null) root.removeAttribute('lang')
      else root.setAttribute('lang', previous)
    }
  }, [language])

  const setLanguage = useCallback((next: LandingLanguage) => {
    save(next)
    setLanguageState(next)
  }, [])

  return { language, copy: landingCopy[language], setLanguage }
}
