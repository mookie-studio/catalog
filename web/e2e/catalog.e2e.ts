import { expect, test } from '@playwright/test'
import { createTenantWithAdmin, newPage } from './session.ts'

// One flow across the whole stack: a Platform Admin creates a Tenant and invites its first Admin, the
// Admin publishes a Product with a filterable Attribute, a Visitor finds it, and archiving hides it.

const run = Date.now().toString(36)
const tenant = { name: `E2E Hardware ${run}`, slug: `e2e-${run}` }
const adminEmail = `admin-${run}@e2e.test`
const productName = `Hammer drill ${run}`

test('an Admin publishes a Product that a Visitor finds, and archiving hides it', async ({ browser }) => {
  // Platform Admin creates the Tenant and invites its first Admin, who accepts and sets a password.
  const admin = await createTenantWithAdmin(browser, tenant, adminEmail)

  // Admin: a filterable option-list Attribute Definition with two Options.
  await admin.getByRole('navigation', { name: 'Admin' }).getByRole('link', { name: 'Attributes' }).click()
  const newDefinition = admin.getByRole('form', { name: 'New Attribute Definition' })
  await newDefinition.getByLabel('Name').fill('Color')
  await newDefinition.getByLabel('Type').selectOption('OptionList')
  await newDefinition.getByLabel('Filterable').check()
  await newDefinition.getByRole('button', { name: 'Add attribute' }).click()
  const color = admin.getByRole('region', { name: 'Color' })
  for (const option of ['Red', 'Blue']) {
    await color.getByLabel('New Option', { exact: true }).fill(option)
    await color.getByRole('button', { name: 'Add option' }).click()
    await expect(color.getByText(option, { exact: true })).toBeVisible()
  }

  // Admin: create the Product, give it a description and the Attribute, and publish it.
  await admin.getByRole('navigation', { name: 'Admin' }).getByRole('link', { name: 'Products' }).click()
  await admin.getByRole('link', { name: 'New product' }).first().click()
  await admin.getByLabel('Name', { exact: true }).fill(productName)
  await admin.getByLabel('SKU', { exact: true }).fill(`HD-${run}`)
  await admin.getByLabel('Description', { exact: true }).fill('A powerful hammer drill for concrete.')
  await admin.getByRole('button', { name: 'Create product' }).click()
  await expect(admin.getByRole('heading', { name: productName })).toBeVisible()
  const productSlug = await admin.getByLabel('Product slug').inputValue()
  const attributes = admin.getByRole('region', { name: 'Attributes' })
  await attributes.getByLabel('Color').selectOption({ label: 'Red' })
  await attributes.getByRole('button', { name: 'Save attributes' }).click()
  await expect(attributes.getByText('Attributes saved')).toBeVisible()
  await admin.getByRole('button', { name: 'Publish' }).click()
  await expect(admin.getByRole('button', { name: 'Move to draft' })).toBeVisible()

  // Visitor: find it by search and by the filter, and open its detail.
  const visitor = await newPage(browser)
  await visitor.goto(`/${tenant.slug}`)
  await visitor.getByRole('searchbox', { name: 'Search products' }).fill('hammer')
  await visitor.getByRole('button', { name: 'Search' }).click()
  await expect(visitor.getByRole('link', { name: productName })).toBeVisible()
  await visitor.goto(`/${tenant.slug}`)
  // Filters live in the URL, so a checkbox re-renders after navigation: click, then assert its state.
  const toggle = async (name: string, checked: boolean) => {
    const box = visitor.getByRole('checkbox', { name })
    await box.click()
    await expect(box).toBeChecked({ checked })
  }
  await toggle('Blue', true)
  await expect(visitor.getByText('No products found.')).toBeVisible()
  await toggle('Blue', false)
  await toggle('Red', true)
  await visitor.getByRole('link', { name: productName }).click()
  await expect(visitor.getByRole('heading', { name: productName })).toBeVisible()
  await expect(visitor.getByText('A powerful hammer drill for concrete.')).toBeVisible()
  await expect(visitor.getByRole('definition').filter({ hasText: 'Red' })).toBeVisible()

  // Admin archives it; the Visitor no longer sees it.
  await admin.getByRole('button', { name: 'Archive' }).click()
  await expect(admin.getByRole('button', { name: 'Restore' })).toBeVisible()
  await visitor.goto(`/${tenant.slug}/p/${productSlug}`)
  await expect(visitor.getByRole('heading', { name: 'Page not found' })).toBeVisible()
  await visitor.goto(`/${tenant.slug}?q=hammer`)
  await expect(visitor.getByText('No products found.')).toBeVisible()
})
