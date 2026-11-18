import type { CSSProperties } from 'react'

function channel(hex: string) {
  const value = Number.parseInt(hex, 16) / 255
  return value <= 0.03928 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4
}

/** WCAG relative luminance of a "#RRGGBB" color. */
export function relativeLuminance(color: string): number {
  const hex = color.replace('#', '')
  return 0.2126 * channel(hex.slice(0, 2)) + 0.7152 * channel(hex.slice(2, 4)) + 0.0722 * channel(hex.slice(4, 6))
}

const contrast = (a: number, b: number) => (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05)

/** Black or white, whichever has the higher contrast on the Brand color. */
export function foregroundFor(color: string): '#000000' | '#FFFFFF' {
  const luminance = relativeLuminance(color)
  return contrast(luminance, 0) >= contrast(luminance, 1) ? '#000000' : '#FFFFFF'
}

/**
 * CSS custom properties for the public layout: the Brand color drives the header (`--brand`), the
 * primary buttons (`--primary`) and the focus ring (`--ring`). The ring only takes the Brand color
 * when it has at least 3:1 contrast on white (WCAG non-text contrast); a light Brand color would make
 * focus invisible, so the default ring is kept. Without a Brand color the default theme is kept.
 */
export function brandStyle(brandColor: string | null | undefined): CSSProperties | undefined {
  if (!brandColor || !/^#[0-9a-fA-F]{6}$/.test(brandColor)) return undefined
  const foreground = foregroundFor(brandColor)
  return {
    '--brand': brandColor,
    '--brand-foreground': foreground,
    '--primary': brandColor,
    '--primary-foreground': foreground,
    ...(contrast(relativeLuminance(brandColor), 1) >= 3 ? { '--ring': brandColor } : {}),
  } as CSSProperties
}
