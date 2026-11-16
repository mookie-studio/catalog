import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { adminSession, created, noContent } from '@/test/admin'
import { json, lastBody, mockApi, problem, requestsTo } from '@/test/api'
import { renderApp } from '@/test/render'

vi.mock('@/features/auth/session', () => import('@/test/session'))

type Node = { id: string; name: string; children: Node[] }

/** A small fake of the category endpoints that keeps the tree in memory. */
function categoriesApi(initial: Node[], routes: Record<string, unknown> = {}) {
  let tree = initial
  mockApi({
    ...adminSession,
    'GET /api/admin/categories': () => json(tree),
    'POST /api/admin/categories': async (_url: URL, request: Request) => {
      const { name, parentId } = (await request.json()) as { name: string; parentId: string | null }
      const node = { id: `new-${name}`, name, children: [] }
      if (parentId === null) tree = [...tree, node]
      else tree = tree.map((n) => (n.id === parentId ? { ...n, children: [...n.children, node] } : n))
      return created(node)
    },
    ...routes,
  })
  return { setTree: (next: Node[]) => (tree = next) }
}

const tools = { id: 'tools', name: 'Tools', children: [{ id: 'drills', name: 'Drills', children: [] }] }
const garden = { id: 'garden', name: 'Garden', children: [] }

describe('admin category tree', () => {
  it('creates top-level and child Categories and refreshes the tree', async () => {
    categoriesApi([tools])
    renderApp('/admin/categories')

    await userEvent.type(await screen.findByRole('textbox', { name: 'New top-level Category' }), 'Garden')
    await userEvent.click(within(screen.getByRole('form', { name: 'New top-level Category' })).getByRole('button', { name: 'Add' }))
    expect(await screen.findByRole('listitem', { name: 'Garden' })).toBeInTheDocument()

    const toolsItem = screen.getByRole('listitem', { name: 'Tools' })
    await userEvent.click(within(toolsItem).getAllByRole('button', { name: 'Add subcategory' })[0])
    await userEvent.type(within(toolsItem).getByRole('textbox', { name: 'New subcategory of Tools' }), 'Saws')
    await userEvent.click(within(within(toolsItem).getByRole('form', { name: 'New subcategory of Tools' })).getByRole('button', { name: 'Add' }))

    expect(await within(screen.getByRole('listitem', { name: 'Tools' })).findByRole('listitem', { name: 'Saws' })).toBeInTheDocument()
    expect(await lastBody('/api/admin/categories', 'POST')).toEqual({ name: 'Saws', parentId: 'tools' })
  })

  it('moves a Category under another parent, excluding itself and its descendants', async () => {
    const api = categoriesApi([tools, garden], {
      'POST /api/admin/categories/tools/move': () => {
        api.setTree([{ ...garden, children: [tools] }])
        return noContent()
      },
    })
    renderApp('/admin/categories')
    const toolsItem = await screen.findByRole('listitem', { name: 'Tools' })

    await userEvent.click(within(toolsItem).getAllByRole('button', { name: 'Move' })[0])
    const form = within(toolsItem).getByRole('form', { name: 'Move Tools' })
    const parents = within(form).getAllByRole('option').map((o) => o.textContent?.trim())
    expect(parents).toEqual(['Top level', 'Garden'])
    await userEvent.selectOptions(within(form).getByLabelText('New parent'), 'garden')
    await userEvent.click(within(form).getByRole('button', { name: 'Move' }))

    expect(await within(screen.getByRole('listitem', { name: 'Garden' })).findByRole('listitem', { name: 'Tools' })).toBeInTheDocument()
    expect(await lastBody('/api/admin/categories/tools/move', 'POST')).toEqual({ parentId: 'garden', position: 0 })
  })

  it('reorders siblings with up and down', async () => {
    categoriesApi([tools, garden], { 'POST /api/admin/categories/garden/move': () => noContent() })
    renderApp('/admin/categories')

    expect(await screen.findByRole('button', { name: 'Move Tools up' })).toBeDisabled()
    await userEvent.click(screen.getByRole('button', { name: 'Move Garden up' }))

    await vi.waitFor(() => expect(requestsTo('/api/admin/categories/garden/move', 'POST')).toHaveLength(1))
    expect(await lastBody('/api/admin/categories/garden/move', 'POST')).toEqual({ parentId: null, position: 0 })
  })

  it('shows the error when deleting a Category with subcategories', async () => {
    categoriesApi([tools], {
      'DELETE /api/admin/categories/tools': () => problem(409, undefined, 'A Category with subcategories cannot be deleted.'),
    })
    renderApp('/admin/categories')

    await userEvent.click(await screen.findByRole('button', { name: 'Delete Tools' }))

    expect(await screen.findByText('A Category with subcategories cannot be deleted.')).toBeInTheDocument()
  })
})
