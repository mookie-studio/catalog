// The API client asks this module for the bearer token of every request.
// The auth feature registers the provider when it loads, so public pages
// never pull in the Supabase code.
type AccessTokenProvider = () => Promise<string | null>

let provider: AccessTokenProvider | null = null

export function setAccessTokenProvider(next: AccessTokenProvider | null) {
  provider = next
}

export async function getAccessToken(): Promise<string | null> {
  return provider ? provider() : null
}
