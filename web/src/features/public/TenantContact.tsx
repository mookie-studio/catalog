import type { components } from '@/api/client'

type PublicTenant = components['schemas']['GetPublicTenantResponse']

/** About and contact details of the tenant; hidden when the Tenant profile is empty. */
export function TenantContact({ tenant }: { tenant: PublicTenant }) {
  const { description, contactEmail, phone, address, websiteUrl } = tenant
  if (!description && !contactEmail && !phone && !address && !websiteUrl) return null

  return (
    <footer aria-label={`About ${tenant.name}`} className="mt-12 border-t px-4 py-8 sm:px-6">
      <div className="mx-auto grid max-w-6xl gap-6 md:grid-cols-2">
        {description && (
          <div>
            <h2 className="mb-2 font-semibold">About {tenant.name}</h2>
            <p className="whitespace-pre-line text-sm text-muted-foreground">{description}</p>
          </div>
        )}
        {(contactEmail || phone || address || websiteUrl) && (
          <div>
            <h2 className="mb-2 font-semibold">Contact</h2>
            <ul className="space-y-1 text-sm">
              {contactEmail && (
                <li>
                  <a className="underline-offset-4 hover:underline" href={`mailto:${contactEmail}`}>
                    {contactEmail}
                  </a>
                </li>
              )}
              {phone && (
                <li>
                  <a className="underline-offset-4 hover:underline" href={`tel:${phone.replace(/[^\d+]/g, '')}`}>
                    {phone}
                  </a>
                </li>
              )}
              {address && <li className="text-muted-foreground">{address}</li>}
              {websiteUrl && (
                <li>
                  <a className="underline-offset-4 hover:underline" href={websiteUrl} target="_blank" rel="noopener noreferrer">
                    {websiteUrl.replace(/^https?:\/\//, '')}
                  </a>
                </li>
              )}
            </ul>
          </div>
        )}
      </div>
    </footer>
  )
}
