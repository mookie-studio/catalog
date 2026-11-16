// Reads invitation emails from the local Supabase email testing inbox (Mailpit, or Inbucket on older CLIs).
const mailboxUrl = process.env.E2E_MAILBOX_URL ?? 'http://localhost:54324'

type MailpitSearch = { messages: { ID: string }[] }
type MailpitMessage = { Text: string; HTML: string }

async function latestMessage(email: string): Promise<string | null> {
  const search = await fetch(`${mailboxUrl}/api/v1/search?query=${encodeURIComponent(`to:"${email}"`)}`)
  if (search.ok) {
    const { messages } = (await search.json()) as MailpitSearch
    if (messages.length === 0) return null
    const message = (await (await fetch(`${mailboxUrl}/api/v1/message/${messages[0].ID}`)).json()) as MailpitMessage
    return `${message.Text}\n${message.HTML}`
  }

  const mailbox = email.split('@')[0]
  const list = await fetch(`${mailboxUrl}/api/v1/mailbox/${mailbox}`)
  if (!list.ok) return null
  const headers = (await list.json()) as { id: string }[]
  if (headers.length === 0) return null
  const message = (await (await fetch(`${mailboxUrl}/api/v1/mailbox/${mailbox}/${headers.at(-1)!.id}`)).json()) as {
    body: { text: string; html: string }
  }
  return `${message.body.text}\n${message.body.html}`
}

/** Waits for the invitation email sent to an address and returns its sign-in link. */
export async function invitationLink(email: string, timeoutMs = 30_000): Promise<string> {
  const deadline = Date.now() + timeoutMs
  while (Date.now() < deadline) {
    const body = await latestMessage(email)
    const link = body?.match(/https?:\/\/[^\s"'<>]+\/auth\/v1\/verify[^\s"'<>]*/)?.[0]
    if (link) return link.replaceAll('&amp;', '&')
    await new Promise((resolve) => setTimeout(resolve, 1_000))
  }
  throw new Error(`No invitation email for ${email} in ${mailboxUrl}`)
}
