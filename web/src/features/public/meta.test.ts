import { describe, expect, it } from 'vitest'
import { toMetaDescription } from '@/features/public/meta'

describe('toMetaDescription', () => {
  it('leaves a short text unchanged and returns nothing for an empty one', () => {
    expect(toMetaDescription('A powerful drill.')).toBe('A powerful drill.')
    expect(toMetaDescription('   ')).toBeUndefined()
    expect(toMetaDescription(null)).toBeUndefined()
  })

  it('collapses newlines and repeated spaces', () => {
    expect(toMetaDescription('Line one.\n\n  Line   two.\t')).toBe('Line one. Line two.')
  })

  it('cuts a long text at a word boundary, ends with an ellipsis and never exceeds 160 characters', () => {
    const text = Array.from({ length: 60 }, (_, i) => `word${i}`).join(' ')

    const result = toMetaDescription(text)!

    expect(result.length).toBeLessThanOrEqual(160)
    expect(result.endsWith('…')).toBe(true)
    expect(text.startsWith(result.slice(0, -1))).toBe(true)
    expect(result.slice(0, -1)).toMatch(/word\d+$/)
  })

  it('keeps a text of exactly 160 characters and cuts a single long word', () => {
    expect(toMetaDescription('a'.repeat(160))).toBe('a'.repeat(160))
    expect(toMetaDescription('a'.repeat(300))).toHaveLength(160)
  })
})
