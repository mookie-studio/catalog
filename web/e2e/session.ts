import { type Browser, expect, type Page } from '@playwright/test'
import { invitationLink } from './mailbox.ts'

export function requiredEnv(name: string): string {
  const value = process.env[name]
  if (!value) throw new Error(`${name} must be set (see .github/workflows/e2e.yml).`)
  return value
}

export async function newPage(browser: Browser): Promise<Page> {
  return (await browser.newContext()).newPage()
}

export async function signIn(page: Page, email: string, password: string) {
  await page.goto('/login')
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', { name: 'Sign in' }).click()
}

/**
 * As the Platform Admin, creates a Tenant and invites its first Admin; then accepts the invitation as
 * that Admin. Returns the Admin's page, signed in on the admin area.
 */
export async function createTenantWithAdmin(browser: Browser, tenant: { name: string; slug: string }, adminEmail: string) {
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

  const admin = await newPage(browser)
  await admin.goto(await invitationLink(adminEmail))
  await expect(admin.getByRole('heading', { name: 'Choose your password' })).toBeVisible()
  const password = 'e2e-admin-password'
  await admin.getByLabel('Password', { exact: true }).fill(password)
  await admin.getByLabel('Repeat password').fill(password)
  await admin.getByRole('button', { name: 'Save password' }).click()
  await expect(admin).toHaveURL(/\/admin\/products/)
  return admin
}

/** Creates a Product with a description from the admin area and publishes it. Returns its Product slug. */
export async function createPublishedProduct(admin: Page, name: string, sku: string, description: string) {
  await admin.getByRole('navigation', { name: 'Admin' }).getByRole('link', { name: 'Products' }).click()
  await admin.getByRole('link', { name: 'New product' }).click()
  await admin.getByLabel('Name', { exact: true }).fill(name)
  await admin.getByLabel('SKU', { exact: true }).fill(sku)
  await admin.getByLabel('Description', { exact: true }).fill(description)
  await admin.getByRole('button', { name: 'Create product' }).click()
  await expect(admin.getByRole('heading', { name })).toBeVisible()
  await admin.getByRole('button', { name: 'Publish' }).click()
  await expect(admin.getByRole('button', { name: 'Move to draft' })).toBeVisible()
  return admin.getByLabel('Product slug').inputValue()
}
