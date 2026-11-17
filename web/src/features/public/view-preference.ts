import type { CatalogView } from '@/features/public/catalog-query'

const key = 'catalog.view'

/** The last view a visitor chose, remembered in this browser (storage may be unavailable). */
export function rememberedView(): CatalogView | null {
  try {
    const value = window.localStorage.getItem(key)
    return value === 'list' || value === 'grid' ? value : null
  } catch {
    return null
  }
}

export function rememberView(view: CatalogView) {
  try {
    window.localStorage.setItem(key, view)
  } catch {
    // Not remembered; the URL still carries the view.
  }
}
