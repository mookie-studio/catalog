import { vi } from 'vitest'
import { setAccessTokenProvider } from '@/api/access-token'

// In-memory stand-in for src/features/auth/session.ts (Supabase). Use with
// vi.mock('@/features/auth/session', () => import('@/test/session')).
export const session = { token: 'token' as string | null }

setAccessTokenProvider(async () => session.token)

export const hasSession = async () => session.token !== null
export const signIn = vi.fn(async () => {
  session.token = 'token'
  return null
})
export const setPassword = vi.fn(async () => null)
export const signOut = vi.fn(async () => {
  session.token = null
})
