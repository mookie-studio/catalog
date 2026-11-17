import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { adminSession, created, noContent } from '@/test/admin'
import { json, lastBody, mockApi, paged, problem, requestsTo } from '@/test/api'
import { renderApp } from '@/test/render'

vi.mock('@/features/auth/session', () => import('@/test/session'))

const color = {
  id: 'd1',
  name: 'Color',
  type: 'OptionList',
  isFilterable: true,
  allowsMultiple: false,
  createdAt: '2026-11-16T10:00:00Z',
  options: [
    { id: 'o1', value: 'Red', position: 0 },
    { id: 'o2', value: 'Blue', position: 1 },
  ],
}
const material = { id: 'd2', name: 'Material', type: 'Text', isFilterable: false, allowsMultiple: false, createdAt: '2026-11-16T10:00:00Z', options: [] }

function attributesApi(routes: Record<string, unknown> = {}) {
  mockApi({ ...adminSession, 'GET /api/admin/attribute-definitions': paged([color, material]), ...routes })
}

const card = (name: string) => screen.findByRole('region', { name })

describe('admin attribute definitions', () => {
  it('creates a multi-select option list', async () => {
    attributesApi({ 'POST /api/admin/attribute-definitions': () => created(color) })
    renderApp('/admin/attributes')
    const form = await screen.findByRole('form', { name: 'New Attribute Definition' })

    await userEvent.type(within(form).getByLabelText('Name'), 'Sizes')
    await userEvent.selectOptions(within(form).getByLabelText('Type'), 'OptionList')
    await userEvent.click(within(form).getByLabelText('Filterable'))
    await userEvent.click(within(form).getByLabelText('Multi-select'))
    await userEvent.click(within(form).getByRole('button', { name: 'Add attribute' }))

    await vi.waitFor(() => expect(requestsTo('/api/admin/attribute-definitions', 'POST')).toHaveLength(1))
    expect(await lastBody('/api/admin/attribute-definitions', 'POST')).toEqual({
      name: 'Sizes',
      type: 'OptionList',
      isFilterable: true,
      allowsMultiple: true,
    })
  })

  it('shows a taken name next to the name field', async () => {
    attributesApi({ 'POST /api/admin/attribute-definitions': () => problem(409, { name: ['An Attribute Definition with this name already exists in this Tenant.'] }) })
    renderApp('/admin/attributes')
    const form = await screen.findByRole('form', { name: 'New Attribute Definition' })

    await userEvent.type(within(form).getByLabelText('Name'), 'Color')
    await userEvent.click(within(form).getByRole('button', { name: 'Add attribute' }))

    expect(await within(form).findByText('An Attribute Definition with this name already exists in this Tenant.')).toBeInTheDocument()
  })

  it('renames and toggles filterable, with the type selector disabled', async () => {
    attributesApi({ 'PUT /api/admin/attribute-definitions/d2': () => json({ ...material, name: 'Materials', isFilterable: true }) })
    renderApp('/admin/attributes')
    const materialCard = await card('Material')

    expect(within(materialCard).getByLabelText('Type')).toBeDisabled()
    await userEvent.clear(within(materialCard).getByLabelText('Name'))
    await userEvent.type(within(materialCard).getByLabelText('Name'), 'Materials')
    await userEvent.click(within(materialCard).getByLabelText('Filterable'))
    await userEvent.click(within(materialCard).getByRole('button', { name: 'Save' }))

    await vi.waitFor(() => expect(requestsTo('/api/admin/attribute-definitions/d2', 'PUT')).toHaveLength(1))
    expect(await lastBody('/api/admin/attribute-definitions/d2', 'PUT')).toEqual({ name: 'Materials', isFilterable: true })
  })

  it('adds an Option to an option list', async () => {
    attributesApi({ 'POST /api/admin/attribute-definitions/d1/options': () => created({ id: 'o3', value: 'Green', position: 2 }) })
    renderApp('/admin/attributes')
    const colorCard = await card('Color')

    await userEvent.type(within(colorCard).getByLabelText('New Option'), 'Green')
    await userEvent.click(within(colorCard).getByRole('button', { name: 'Add option' }))

    await vi.waitFor(() => expect(requestsTo('/api/admin/attribute-definitions/d1/options', 'POST')).toHaveLength(1))
    expect(await lastBody('/api/admin/attribute-definitions/d1/options', 'POST')).toEqual({ value: 'Green' })
  })

  it('asks for confirmation with the usage count before deleting a used Option', async () => {
    attributesApi({
      'DELETE /api/admin/attribute-definitions/d1/options/o1': (url: URL) =>
        url.searchParams.get('confirm') === 'true' ? noContent() : json({ status: 409, detail: '3 product(s) use it.', usageCount: 3 }, 409),
    })
    renderApp('/admin/attributes')
    const colorCard = await card('Color')

    await userEvent.click(within(colorCard).getByRole('button', { name: 'Delete option Red' }))
    const confirmation = await within(colorCard).findByRole('alertdialog', { name: 'Delete Red?' })
    expect(confirmation).toHaveTextContent('3 products use it.')

    await userEvent.click(within(confirmation).getByRole('button', { name: 'Delete anyway' }))

    await vi.waitFor(() => expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument())
    const deletes = requestsTo('/api/admin/attribute-definitions/d1/options/o1', 'DELETE').map((r) => new URL(r.url).searchParams.get('confirm'))
    expect(deletes).toEqual(['false', 'true'])
  })

  it('deletes an unused definition without asking', async () => {
    attributesApi({ 'DELETE /api/admin/attribute-definitions/d2': () => noContent() })
    renderApp('/admin/attributes')

    await userEvent.click(within(await card('Material')).getByRole('button', { name: 'Delete attribute' }))

    await vi.waitFor(() => expect(requestsTo('/api/admin/attribute-definitions/d2', 'DELETE')).toHaveLength(1))
    expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument()
  })
})

describe('paging Attribute Definitions', () => {
  it('requests the page in the URL and links to the neighbouring pages', async () => {
    mockApi({
      ...adminSession,
      'GET /api/admin/attribute-definitions': (url: URL) =>
        json(paged([material], Number(url.searchParams.get('page')), 20, 45)),
    })

    renderApp('/admin/attributes?page=2')

    expect(await screen.findByText('Page 2 of 3')).toBeInTheDocument()
    expect(requestsTo('/api/admin/attribute-definitions').at(-1)!.url).toContain('page=2')
    expect(screen.getByRole('link', { name: 'Previous' })).toHaveAttribute('href', '/admin/attributes')
    expect(screen.getByRole('link', { name: 'Next' })).toHaveAttribute('href', '/admin/attributes?page=3')
  })

  it('goes back one page after the last definition of a page is gone', async () => {
    mockApi({
      ...adminSession,
      'GET /api/admin/attribute-definitions': (url: URL) =>
        url.searchParams.get('page') === '3' ? json(paged([], 3, 20, 40)) : json(paged([material], 2, 20, 40)),
    })

    const router = renderApp('/admin/attributes?page=3')

    expect(await screen.findByRole('region', { name: 'Material' })).toBeInTheDocument()
    expect(router.state.location.search).toBe('?page=2')
  })

  it('confirms a new definition even when it lands on another page', async () => {
    attributesApi({ 'POST /api/admin/attribute-definitions': () => created({ ...material, id: 'd3', name: 'Weight' }) })
    renderApp('/admin/attributes')

    const form = await screen.findByRole('form', { name: 'New Attribute Definition' })
    await userEvent.type(within(form).getByLabelText('Name'), 'Weight')
    await userEvent.click(within(form).getByRole('button', { name: 'Add attribute' }))

    expect(await within(form).findByRole('status')).toHaveTextContent('Weight added.')
  })
})
