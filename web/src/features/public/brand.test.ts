import { describe, expect, it } from 'vitest'
import { brandStyle, foregroundFor } from '@/features/public/brand'

describe('brand color', () => {
  it.each([
    ['#FFFF00', '#000000'],
    ['#FFFFFF', '#000000'],
    ['#000080', '#FFFFFF'],
    ['#000000', '#FFFFFF'],
    ['#1A2B3C', '#FFFFFF'],
    ['#7FDBFF', '#000000'],
  ])('uses readable text on %s', (color, expected) => {
    expect(foregroundFor(color)).toBe(expected)
  })

  it('keeps the default theme without a valid Brand color', () => {
    expect(brandStyle(null)).toBeUndefined()
    expect(brandStyle('red')).toBeUndefined()
  })

  it('sets the header and primary colors', () => {
    expect(brandStyle('#000080')).toEqual({
      '--brand': '#000080',
      '--brand-foreground': '#FFFFFF',
      '--primary': '#000080',
      '--primary-foreground': '#FFFFFF',
    })
  })
})
