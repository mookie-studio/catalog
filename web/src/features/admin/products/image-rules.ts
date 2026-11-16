// Client-side checks that mirror the API's rules, so a wrong file is refused before it is uploaded.
export const maxImageBytes = 2 * 1024 * 1024
export const maxImagesPerProduct = 5
export const imageTypes = ['image/jpeg', 'image/png', 'image/webp']

/** Returns why a file cannot be uploaded, or null when it can. */
export function checkImage(file: File, currentCount: number): string | null {
  if (currentCount >= maxImagesPerProduct) return `A product can have at most ${maxImagesPerProduct} images.`
  if (!imageTypes.includes(file.type)) return 'Only JPG, PNG and WebP images are allowed.'
  if (file.size > maxImageBytes) return 'The image must be at most 2 MB.'
  return null
}

/** The image ids after moving one image a step left (-1) or right (+1). */
export function moveImage(ids: string[], index: number, step: -1 | 1): string[] {
  const target = index + step
  if (target < 0 || target >= ids.length) return ids
  const next = [...ids]
  ;[next[index], next[target]] = [next[target], next[index]]
  return next
}
