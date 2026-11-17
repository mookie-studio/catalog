import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { lastBody, mockApi, problem } from '@/test/api'
import { renderApp } from '@/test/render'

const drill = { slug: 'hammer-drill', name: 'Hammer drill', description: 'A drill.', images: [], attributes: [] }
const inquiriesPath = '/api/public/acme/products/hammer-drill/inquiries'

function productApi(inquiriesEnabled: boolean, send: () => Response = () => new Response(null, { status: 202 })) {
  mockApi({
    '/api/public/acme': { name: 'Acme Hardware', slug: 'acme', inquiriesEnabled },
    '/api/public/acme/products/hammer-drill': drill,
    [`POST ${inquiriesPath}`]: send,
  })
}

async function fillAndSend() {
  await userEvent.click(await screen.findByRole('button', { name: 'Ask about this product' }))
  await userEvent.type(screen.getByLabelText('Your name'), 'Ana')
  await userEvent.type(screen.getByLabelText('Your email'), 'ana@example.com')
  await userEvent.type(screen.getByLabelText('Message'), 'Is it in stock?')
  await userEvent.click(screen.getByRole('button', { name: 'Send' }))
}

describe('inquiry form', () => {
  it('is hidden when the tenant does not accept Inquiries', async () => {
    productApi(false)
    renderApp('/acme/p/hammer-drill')

    await screen.findByRole('heading', { name: 'Hammer drill' })
    expect(screen.queryByRole('button', { name: 'Ask about this product' })).not.toBeInTheDocument()
  })

  it('sends the Inquiry with an empty honeypot and confirms it', async () => {
    productApi(true)
    renderApp('/acme/p/hammer-drill')

    await fillAndSend()

    expect(await screen.findByRole('status')).toHaveTextContent('Thank you, your message was sent.')
    expect(await lastBody(inquiriesPath, 'POST')).toEqual({ name: 'Ana', email: 'ana@example.com', message: 'Is it in stock?', website: '' })
  })

  it('shows field errors from the API', async () => {
    productApi(true, () => problem(400, { Email: ['Your email is not a valid email address.'] }))
    renderApp('/acme/p/hammer-drill')

    await fillAndSend()

    expect(await screen.findByLabelText('Your email')).toHaveAccessibleDescription('Your email is not a valid email address.')
  })

  it('asks to wait when the rate limit is hit', async () => {
    productApi(true, () => problem(429))
    renderApp('/acme/p/hammer-drill')

    await fillAndSend()

    expect(await screen.findByRole('alert')).toHaveTextContent('Too many messages, try again later.')
  })
})
