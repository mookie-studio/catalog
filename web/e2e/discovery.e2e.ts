import { expect, type Page, test } from '@playwright/test'
import { createTenantWithAdmin, newPage } from './session.ts'

// Admin tools and discovery: an Admin bulk-publishes two Drafts and files them under a Category, a Visitor
// opens one and sees the other as a Related product, and the Dashboard counts the view.

const run = Date.now().toString(36)
const tenant = { name: `E2E Discovery ${run}`, slug: `e2e-discovery-${run}` }
const adminEmail = `discovery-admin-${run}@e2e.test`
const categoryName = `Tools ${run}`
const drillName = `Drill ${run}`
const sawName = `Saw ${run}`

/** Creates a Draft with a description from the admin area. Returns its Product slug. */
async function createDraft(admin: Page, name: string, sku: string) {
  await admin.getByRole('navigation', { name: 'Admin' }).getByRole('link', { name: 'Products' }).click()
  await admin.getByRole('link', { name: 'New product' }).click()
  await admin.getByLabel('Name', { exact: true }).fill(name)
  await admin.getByLabel('SKU', { exact: true }).fill(sku)
  await admin.getByLabel('Description', { exact: true }).fill(`${name} for every job.`)
  await admin.getByRole('button', { name: 'Create product' }).click()
  await expect(admin.getByRole('heading', { name })).toBeVisible()
  return admin.getByLabel('Product slug').inputValue()
}

test('bulk publish, a Related product for a Visitor and a view on the Dashboard', async ({ browser }) => {
  const admin = await createTenantWithAdmin(browser, tenant, adminEmail)

  // A Category and two Drafts.
  await admin.getByRole('navigation', { name: 'Admin' }).getByRole('link', { name: 'Categories' }).click()
  const newCategory = admin.getByRole('form', { name: 'New top-level Category' })
  await newCategory.getByLabel('New top-level Category').fill(categoryName)
  await newCategory.getByRole('button', { name: 'Add' }).click()
  await expect(admin.getByRole('list', { name: 'Category tree' }).getByText(categoryName)).toBeVisible()
  const drillSlug = await createDraft(admin, drillName, `DR-${run}`)
  await createDraft(admin, sawName, `SW-${run}`)

  // Bulk: publish both, then add both to the Category.
  await admin.getByRole('navigation', { name: 'Admin' }).getByRole('link', { name: 'Products' }).click()
  await admin.getByRole('checkbox', { name: 'Select all on this page' }).check()
  await expect(admin.getByText('2 selected')).toBeVisible()
  await admin.getByRole('button', { name: 'Publish' }).click()
  await expect(admin.getByRole('status')).toHaveText(/2 updated/)
  await admin.getByRole('button', { name: 'Dismiss' }).click()

  await admin.getByRole('checkbox', { name: 'Select all on this page' }).check()
  await admin.getByRole('button', { name: 'Add to category…' }).click()
  await admin.getByRole('combobox', { name: 'Category to apply' }).selectOption({ label: categoryName })
  await admin.getByRole('button', { name: 'Add', exact: true }).click()
  await expect(admin.getByRole('status')).toHaveText(/2 updated/)

  // Visitor: the Saw is a Related product of the Drill.
  const visitor = await newPage(browser)
  await visitor.goto(`/${tenant.slug}/p/${drillSlug}`)
  await expect(visitor.getByRole('heading', { name: drillName })).toBeVisible()
  const related = visitor.getByRole('list', { name: 'Related products' })
  await expect(related.getByRole('link', { name: sawName })).toBeVisible()

  // Dashboard: two Published products and one view of the Drill.
  await admin.getByRole('navigation', { name: 'Admin' }).getByRole('link', { name: 'Dashboard' }).click()
  await expect(admin.getByRole('link', { name: '2 Published', exact: true })).toBeVisible()
  const mostViewed = admin.getByRole('region', { name: 'Most viewed products' })
  await expect(async () => {
    await admin.reload()
    const row = mostViewed.getByRole('row', { name: new RegExp(drillName) })
    await expect(row.getByRole('cell').nth(2)).toHaveText('1')
  }).toPass()
})
