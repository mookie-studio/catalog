import type { Plugin } from 'vite'

/** The robots.txt text: keeps search engines out of the admin and sign-in pages and points them to the sitemap index (ADR 0013). */
export function robotsTxt(apiUrl: string | undefined): string {
  const lines = ['User-agent: *', 'Disallow: /admin', 'Disallow: /platform', 'Disallow: /login', 'Disallow: /accept-invite']
  const base = (apiUrl ?? '').trim().replace(/\/+$/, '')
  if (base !== '') lines.push('', `Sitemap: ${base}/api/public-sitemap.xml`)
  return `${lines.join('\n')}\n`
}

/** Emits robots.txt into the build output, using VITE_API_URL for the sitemap address. */
export function robotsPlugin(): Plugin {
  let apiUrl: string | undefined
  return {
    name: 'catalog-robots-txt',
    apply: 'build',
    configResolved(config) {
      apiUrl = config.env.VITE_API_URL as string | undefined
    },
    generateBundle() {
      this.emitFile({ type: 'asset', fileName: 'robots.txt', source: robotsTxt(apiUrl) })
    },
  }
}
