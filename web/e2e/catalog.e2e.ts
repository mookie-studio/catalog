import { type Browser, expect, type Page, test } from '@playwright/test'
import { invitationLink } from './mailbox.ts'

// One flow across the whole stack: a Platform Admin creates a Tenant and invites its first Admin, the
// Admin publishes a Product with a filterable Attribute, a Visitor finds it, and archiving hides it.

const run = Date.now().toString(36)
const tenant = { name: `E2E Hardware ${run}`, slug: `e2e-${run}` }
const adminEmail = `admin-${run}@e2e.test`
const adminPassword = 'e2e-admin-password'
const productName = `Hammer drill ${run}`

function requiredEnv(name: string): string {
  const value = process.env[name]
  if (!value) throw new Error(`${name} must be set (see .github/workflows/e2e.yml).`)
  return value
}

async function newPage(browser: Browser): Promise<Page> {
  return (await browser.newContext()).newPage()
}

async function signIn(page: Page, email: string, password: string) {
  await page.goto('/login')
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', { name: 'Sign in' }).click()
}

test('an Admin publishes a Product that a Visitor finds, and archiving hides it', async ({ browser }) => {
  // Platform Admin: create the Tenant and invite its first Admin.
  const platform = await newPage(browser)
  await signIn(platform, requiredEnv('E2E_PLATFORM_ADMIN_EMAIL'), requiredEnv('E2E_PLATFORM_ADMIN_PASSWORD'))
  await expect(platform).toHaveURL(/\/platform\/tenants/)
  const newTenant = platform.getByRole('form', { name: 'New Tenant' })
  await newTenant.getByLabel('Name').fill(tenant.name)
  await newTenant.getByLabel('Tenant slug').fill(tenant.slug)
  await newTenant.getByRole('button', { name: 'Create tenant' }).click()
  const tenantRow = platform.getByRole('listitem', { name: tenant.name })
  await tenantRow.getByRole('button', { name: 'Manage' }).click()
  const admins = tenantRow.getByRole('region', { name: 'Admins' })
  await admins.getByLabel('Admin email').fill(adminEmail)
  await admins.getByRole('button', { name: 'Invite' }).click()
  await expect(admins.getByText(adminEmail)).toBeVisible()

  // Admin: accept the invitation and set a password.
  const admin = await newPage(browser)
  await admin.goto(await invitationLink(adminEmail))
  await expect(admin.getByRole('heading', { name: 'Choose your password' })).toBeVisible()
  await admin.getByLabel('Password', { exact: true }).fill(adminPassword)
  await admin.getByLabel('Repeat password').fill(adminPassword)
  await admin.getByRole('button', { name: 'Save password' }).click()
  await expect(admin).toHaveURL(/\/admin\/products/)

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
  await admin.getByRole('link', { name: 'New product' }).click()
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
  await visitor.getByRole('checkbox', { name: 'Blue' }).check()
  await expect(visitor.getByText('No products found.')).toBeVisible()
  await visitor.getByRole('checkbox', { name: 'Blue' }).uncheck()
  await visitor.getByRole('checkbox', { name: 'Red' }).check()
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
