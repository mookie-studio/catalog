import { json } from '@/test/api'

// API answers every admin page needs: the signed-in Admin and their tenant.
export const adminSession = {
  '/api/me': { userId: 'u1', email: 'admin@test', isPlatformAdmin: false, tenantId: 't1' },
  '/api/admin/tenant': { id: 't1', name: 'Acme Hardware', slug: 'acme' },
}

export function product(overrides: Record<string, unknown> = {}) {
  return {
    id: 'p1',
    name: 'Hammer drill',
    description: 'A powerful drill.',
    sku: 'HD-1',
    slug: 'hammer-drill',
    status: 'Draft',
    createdAt: '2026-11-16T10:00:00Z',
    updatedAt: '2026-11-16T10:00:00Z',
    attributes: [],
    categoryIds: [],
    images: [],
    ...overrides,
  }
}

export const noContent = () => new Response(null, { status: 204 })
export const created = (body: unknown) => json(body, 201)
