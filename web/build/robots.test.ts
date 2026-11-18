import { describe, expect, it } from 'vitest'
import { robotsTxt } from './robots.ts'

describe('robotsTxt', () => {
  it('disallows the private areas and points to the sitemap index of the API', () => {
    expect(robotsTxt('https://api.example.com')).toBe(
      [
        'User-agent: *',
        'Disallow: /admin',
        'Disallow: /platform',
        'Disallow: /login',
        'Disallow: /accept-invite',
        '',
        'Sitemap: https://api.example.com/api/public-sitemap.xml',
        '',
      ].join('\n'),
    )
  })

  it('strips trailing slashes from the API address', () => {
    expect(robotsTxt('https://api.example.com//')).toContain('Sitemap: https://api.example.com/api/public-sitemap.xml')
  })

  it('omits the Sitemap line when the API address is empty or missing', () => {
    for (const value of ['', '  ', undefined]) {
      expect(robotsTxt(value)).not.toContain('Sitemap')
    }
    expect(robotsTxt('')).toContain('Disallow: /admin')
  })
})
