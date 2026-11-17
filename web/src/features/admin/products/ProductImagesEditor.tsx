import { useMutation, useQueryClient } from '@tanstack/react-query'
import { type ChangeEvent, useRef, useState } from 'react'
import { api } from '@/api/client'
import { ApiProblem, call } from '@/api/problem'
import { Button } from '@/components/ui/button'
import { checkImage, imageTypes, maxImagesPerProduct, moveImage } from '@/features/admin/products/image-rules'
import { type Product, productQueryKey } from '@/features/admin/products/products-api'

/** Product Images in order; the first one is the Main image. */
export function ProductImagesEditor({ product }: { product: Product }) {
  const queryClient = useQueryClient()
  const input = useRef<HTMLInputElement>(null)
  const [clientError, setClientError] = useState<string | null>(null)
  const images = [...product.images].sort((a, b) => a.position - b.position)
  const id = product.id
  const refresh = () => queryClient.invalidateQueries({ queryKey: productQueryKey(id) })

  const upload = useMutation({
    mutationFn: (file: File) => {
      const body = new FormData()
      body.append('file', file)
      return call(api.POST('/api/admin/products/{id}/images', { params: { path: { id } }, body: body as never }))
    },
    onSettled: refresh,
  })
  const reorder = useMutation({
    mutationFn: (imageIds: string[]) => call(api.PUT('/api/admin/products/{id}/images/order', { params: { path: { id } }, body: { imageIds } })),
    onSettled: refresh,
  })
  const remove = useMutation({
    mutationFn: (imageId: string) => call(api.DELETE('/api/admin/products/{id}/images/{imageId}', { params: { path: { id, imageId } } })),
    onSettled: refresh,
  })
  const busy = upload.isPending || reorder.isPending || remove.isPending
  const serverError = [upload.error, reorder.error, remove.error].find((e) => e)
  const error =
    clientError ?? (serverError instanceof ApiProblem ? (Object.values(serverError.fields)[0] ?? serverError.message) : serverError?.message)

  function handleFile(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) return
    const problem = checkImage(file, images.length)
    setClientError(problem)
    upload.reset()
    if (!problem) upload.mutate(file)
  }

  return (
    <section aria-label="Images" className="flex flex-col gap-4 border-t pt-6">
      <div className="flex items-center justify-between gap-3">
        <h2 className="text-lg font-semibold">Images</h2>
        <span className="text-sm text-muted-foreground">
          {images.length} of {maxImagesPerProduct}
        </span>
      </div>
      {images.length === 0 ? (
        <p className="text-sm text-muted-foreground">No images. Visitors see a default image.</p>
      ) : (
        <ol className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-5">
          {images.map((image, index) => (
            <li key={image.id} aria-label={`Image ${index + 1}`} className="flex flex-col gap-2">
              <div className="relative">
                <img src={image.thumbnailUrl} alt={`Image ${index + 1}`} className="aspect-square w-full rounded-lg border object-cover" />
                {index === 0 && (
                  <span className="absolute left-1 top-1 rounded bg-background/90 px-1.5 py-0.5 text-xs font-medium">Main image</span>
                )}
              </div>
              <div className="flex justify-between gap-1">
                <Button
                  size="sm"
                  variant="ghost"
                  aria-label={`Move image ${index + 1} left`}
                  disabled={busy || index === 0}
                  onClick={() => reorder.mutate(moveImage(images.map((i) => i.id), index, -1))}
                >
                  ←
                </Button>
                <Button size="sm" variant="ghost" aria-label={`Delete image ${index + 1}`} disabled={busy} onClick={() => remove.mutate(image.id)}>
                  Delete
                </Button>
                <Button
                  size="sm"
                  variant="ghost"
                  aria-label={`Move image ${index + 1} right`}
                  disabled={busy || index === images.length - 1}
                  onClick={() => reorder.mutate(moveImage(images.map((i) => i.id), index, 1))}
                >
                  →
                </Button>
              </div>
            </li>
          ))}
        </ol>
      )}
      {error && (
        <p role="alert" className="text-sm text-destructive">
          {error}
        </p>
      )}
      <div>
        <input ref={input} type="file" accept={imageTypes.join(',')} className="sr-only" aria-label="Upload image" onChange={handleFile} />
        <Button variant="outline" disabled={busy} onClick={() => input.current?.click()}>
          Upload image
        </Button>
        <p className="mt-2 text-sm text-muted-foreground">JPG, PNG or WebP, up to 2 MB.</p>
      </div>
    </section>
  )
}
