import { expect, test } from '@playwright/test'
import { createPublishedProduct, createTenantWithAdmin, newPage } from './session.ts'

// A branded catalog and an Inquiry: the Admin sets the profile and enables Inquiries, a Visitor reads
// the description and asks about a Product, and the Admin handles the Inquiry in the inbox.

const run = Date.now().toString(36)
const tenant = { name: `E2E Brand ${run}`, slug: `e2e-brand-${run}` }
const adminEmail = `brand-admin-${run}@e2e.test`
const productName = `Cordless saw ${run}`
const tenantDescription = `Family hardware store since 1987 (${run}).`
const visitorName = `Visitor ${run}`

test('a Visitor sends an Inquiry from a branded catalog and the Admin handles it', async ({ browser }) => {
  const admin = await createTenantWithAdmin(browser, tenant, adminEmail)

  // Admin: description, Brand color and Inquiries on the Profile page.
  await admin.getByRole('navigation', { name: 'Admin' }).getByRole('link', { name: 'Profile' }).click()
  const profile = admin.getByRole('form', { name: 'Profile' })
  await profile.getByLabel('Description', { exact: true }).fill(tenantDescription)
  await profile.getByLabel('Brand color', { exact: true }).fill('#000080')
  await profile.getByRole('button', { name: 'Save profile' }).click()
  await expect(profile.getByText('Saved.')).toBeVisible()
  const accept = admin.getByRole('region', { name: 'Inquiries' }).getByRole('checkbox', { name: 'Accept Inquiries' })
  // The checkbox shows the saved value, so it turns checked only once the API answers: click, then assert.
  await accept.click()
  await expect(accept).toBeChecked()

  const productSlug = await createPublishedProduct(admin, productName, `CS-${run}`, 'A light cordless saw.')

  // Visitor: the branded catalog shows the description; the product page takes an Inquiry.
  const visitor = await newPage(browser)
  await visitor.goto(`/${tenant.slug}`)
  await expect(visitor.getByText(tenantDescription)).toBeVisible()
  await expect(visitor.locator('header')).toHaveCSS('background-color', 'rgb(0, 0, 128)')
  await visitor.goto(`/${tenant.slug}/p/${productSlug}`)
  await expect(visitor.getByRole('heading', { name: productName })).toBeVisible()
  await visitor.getByRole('button', { name: 'Ask about this product' }).click()
  const inquiry = visitor.getByRole('form', { name: `Ask about ${productName}` })
  await inquiry.getByLabel('Your name').fill(visitorName)
  await inquiry.getByLabel('Your email').fill(`visitor-${run}@e2e.test`)
  await inquiry.getByLabel('Message').fill('Is it available in blue?')
  await inquiry.getByRole('button', { name: 'Send' }).click()
  await expect(visitor.getByText('Thank you, your message was sent.')).toBeVisible()

  // Admin: the Inquiry is in the inbox; marking it handled moves it out of Open.
  await admin.getByRole('navigation', { name: 'Admin' }).getByRole('link', { name: /Inquiries/ }).click()
  const item = admin.getByRole('listitem', { name: `Inquiry from ${visitorName}` })
  await expect(item.getByText('Is it available in blue?')).toBeVisible()
  await item.getByRole('button', { name: 'Mark handled' }).click()
  await expect(item.getByRole('button', { name: 'Reopen' })).toBeVisible()
})
