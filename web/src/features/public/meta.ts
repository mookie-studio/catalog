const maxLength = 160

/** A one-line description for page metadata: whitespace collapsed, cut at a word boundary to at most 160 characters. */
export function toMetaDescription(text: string | null | undefined): string | undefined {
  const collapsed = (text ?? '').replace(/\s+/g, ' ').trim()
  if (collapsed === '') return undefined
  if (collapsed.length <= maxLength) return collapsed

  const room = collapsed.slice(0, maxLength - 1)
  const lastSpace = room.lastIndexOf(' ')
  const cut = lastSpace > 0 ? room.slice(0, lastSpace) : room
  return `${cut.replace(/[\s.,;:!?-]+$/, '')}…`
}
