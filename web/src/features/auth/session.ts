import { createClient, type SupabaseClient } from '@supabase/supabase-js'
import { setAccessTokenProvider } from '@/api/access-token'

// Supabase Authentication only signs people in (ADR 0004); what they may do
// is decided by the API. This module is the only place that talks to Supabase.
let client: SupabaseClient | null = null

function supabase(): SupabaseClient {
  if (!client) {
    const url = import.meta.env.VITE_SUPABASE_URL
    const anonKey = import.meta.env.VITE_SUPABASE_ANON_KEY
    if (!url || !anonKey) {
      throw new Error('VITE_SUPABASE_URL and VITE_SUPABASE_ANON_KEY must be set.')
    }
    client = createClient(url, anonKey)
  }
  return client
}

export async function getAccessToken(): Promise<string | null> {
  const { data } = await supabase().auth.getSession()
  return data.session?.access_token ?? null
}

setAccessTokenProvider(getAccessToken)

export async function hasSession(): Promise<boolean> {
  return (await getAccessToken()) !== null
}

/** Returns an error message, or null when the person is signed in. */
export async function signIn(email: string, password: string): Promise<string | null> {
  const { error } = await supabase().auth.signInWithPassword({ email, password })
  return error ? error.message : null
}

/** Sets the password of the person who opened an invitation link. Returns an error message or null. */
export async function setPassword(password: string): Promise<string | null> {
  const { error } = await supabase().auth.updateUser({ password })
  return error ? error.message : null
}

export async function signOut(): Promise<void> {
  await supabase().auth.signOut()
}
