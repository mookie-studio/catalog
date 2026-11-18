import { act, renderHook } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { useSelection } from '@/features/admin/products/use-selection'

describe('useSelection', () => {
  it('toggles single ids and clears', () => {
    const { result } = renderHook(() => useSelection('page-1'))

    act(() => result.current.toggle('a'))
    act(() => result.current.toggle('b'))
    expect([...result.current.selected]).toEqual(['a', 'b'])
    act(() => result.current.toggle('a'))
    expect([...result.current.selected]).toEqual(['b'])
    act(() => result.current.clear())

    expect(result.current.selected.size).toBe(0)
  })

  it('selects the whole page, reports a partial state and clears the page when all are selected', () => {
    const { result } = renderHook(() => useSelection('page-1'))
    const page = ['a', 'b', 'c']

    expect(result.current.stateOf(page)).toBe('none')
    act(() => result.current.toggle('a'))
    expect(result.current.stateOf(page)).toBe('some')
    act(() => result.current.togglePage(page))
    expect(result.current.stateOf(page)).toBe('all')
    act(() => result.current.togglePage(page))

    expect(result.current.stateOf(page)).toBe('none')
  })

  it('starts empty again when the page or filters change', () => {
    const { result, rerender } = renderHook(({ scope }) => useSelection(scope), { initialProps: { scope: 'page=1' } })
    act(() => result.current.toggle('a'))
    expect(result.current.isSelected('a')).toBe(true)

    rerender({ scope: 'page=2' })

    expect(result.current.isSelected('a')).toBe(false)
    expect(result.current.selected.size).toBe(0)
    act(() => result.current.toggle('b'))
    expect([...result.current.selected]).toEqual(['b'])
  })
})
