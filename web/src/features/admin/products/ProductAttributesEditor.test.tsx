import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { adminSession, product } from '@/test/admin'
import { json, lastBody, mockApi, problem } from '@/test/api'
import { renderApp } from '@/test/render'

vi.mock('@/features/auth/session', () => import('@/test/session'))

const definition = (id: string, name: string, type: string, extra: Record<string, unknown> = {}) => ({
  id,
  name,
  type,
  isFilterable: false,
  allowsMultiple: false,
  createdAt: '2026-11-16T10:00:00Z',
  options: [],
  ...extra,
})

const definitions = [
  definition('d-text', 'Material', 'Text'),
  definition('d-number', 'Power', 'Number'),
  definition('d-bool', 'Cordless', 'Boolean'),
  definition('d-date', 'Released', 'Date'),
  definition('d-single', 'Color', 'OptionList', { options: [{ id: 'red', value: 'Red', position: 0 }, { id: 'blue', value: 'Blue', position: 1 }] }),
  definition('d-multi', 'Sizes', 'OptionList', {
    allowsMultiple: true,
    options: [{ id: 's', value: 'S', position: 0 }, { id: 'm', value: 'M', position: 1 }, { id: 'l', value: 'L', position: 2 }],
  }),
]

function editorApi(attributes: unknown[] = [], put: () => Response = () => json(product())) {
  mockApi({
    ...adminSession,
    'GET /api/admin/products/p1': product({ attributes }),
    'GET /api/admin/attribute-definitions': definitions,
    'PUT /api/admin/products/p1/attributes': put,
  })
}

const section = () => screen.findByRole('region', { name: 'Attributes' })

describe('product attribute values editor', () => {
  it('sends a typed value for each input and omits empty ones', async () => {
    editorApi()
    renderApp('/admin/products/p1')
    const editor = await section()

    await userEvent.type(within(editor).getByLabelText('Material'), 'Steel')
    await userEvent.type(within(editor).getByLabelText('Power'), '500.5')
    await userEvent.selectOptions(within(editor).getByLabelText('Cordless'), 'No')
    await userEvent.type(within(editor).getByLabelText('Released'), '2026-01-31')
    await userEvent.selectOptions(within(editor).getByLabelText('Color'), 'Blue')
    await userEvent.click(within(editor).getByLabelText('S'))
    await userEvent.click(within(editor).getByLabelText('L'))
    await userEvent.click(within(editor).getByRole('button', { name: 'Save attributes' }))

    expect(await within(editor).findByText('Attributes saved')).toBeInTheDocument()
    expect(await lastBody('/api/admin/products/p1/attributes', 'PUT')).toEqual({
      values: { 'd-text': 'Steel', 'd-number': 500.5, 'd-bool': false, 'd-date': '2026-01-31', 'd-single': 'blue', 'd-multi': ['s', 'l'] },
    })
  })

  it('shows the stored values and drops the ones that are cleared', async () => {
    editorApi([
      { definitionId: 'd-text', name: 'Material', type: 'Text', value: 'Steel' },
      { definitionId: 'd-bool', name: 'Cordless', type: 'Boolean', value: true },
      { definitionId: 'd-multi', name: 'Sizes', type: 'OptionList', value: ['m'] },
    ])
    renderApp('/admin/products/p1')
    const editor = await section()

    expect(within(editor).getByLabelText('Material')).toHaveValue('Steel')
    expect(within(editor).getByLabelText('Cordless')).toHaveValue('true')
    expect(within(editor).getByLabelText('M')).toBeChecked()
    await userEvent.clear(within(editor).getByLabelText('Material'))
    await userEvent.click(within(editor).getByLabelText('M'))
    await userEvent.click(within(editor).getByRole('button', { name: 'Save attributes' }))

    await within(editor).findByText('Attributes saved')
    expect(await lastBody('/api/admin/products/p1/attributes', 'PUT')).toEqual({ values: { 'd-bool': true } })
  })

  it('shows API validation errors on the right attribute', async () => {
    editorApi([], () => problem(400, { 'd-number': ['The value must be a number.'] }))
    renderApp('/admin/products/p1')
    const editor = await section()

    await userEvent.type(within(editor).getByLabelText('Power'), '12')
    await userEvent.click(within(editor).getByRole('button', { name: 'Save attributes' }))

    expect(await within(editor).findByLabelText('Power')).toHaveAccessibleDescription('The value must be a number.')
    expect(within(editor).getByLabelText('Material')).not.toHaveAccessibleDescription()
  })
})
