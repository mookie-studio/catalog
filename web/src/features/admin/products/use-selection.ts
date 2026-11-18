import { useCallback, useState } from 'react'

/**
 * The ids selected in a list. The selection belongs to one `scope` (the current page and filters):
 * when the scope changes, it starts empty again.
 */
export function useSelection(scope: string) {
  const [state, setState] = useState<{ scope: string; ids: ReadonlySet<string> }>({ scope, ids: new Set() })
  const selected = state.scope === scope ? state.ids : new Set<string>()

  const update = useCallback(
    (change: (ids: Set<string>) => void) =>
      setState((current) => {
        const ids = new Set(current.scope === scope ? current.ids : [])
        change(ids)
        return { scope, ids }
      }),
    [scope],
  )

  return {
    selected,
    isSelected: (id: string) => selected.has(id),
    toggle: (id: string) => update((ids) => void (ids.has(id) ? ids.delete(id) : ids.add(id))),
    /** Selects all of the given ids, or clears them when they are all selected already. */
    togglePage: (pageIds: string[]) =>
      update((ids) => {
        const all = pageIds.length > 0 && pageIds.every((id) => ids.has(id))
        for (const id of pageIds) {
          if (all) ids.delete(id)
          else ids.add(id)
        }
      }),
    clear: () => update((ids) => ids.clear()),
    /** 'all', 'some' or 'none' of the given ids are selected. */
    stateOf: (pageIds: string[]): 'all' | 'some' | 'none' => {
      const count = pageIds.filter((id) => selected.has(id)).length
      return count === 0 ? 'none' : count === pageIds.length ? 'all' : 'some'
    },
  }
}
