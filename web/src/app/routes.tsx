import type { RouteObject } from 'react-router'
import { NotFoundPage } from '@/components/NotFoundPage'
import { LandingPage } from '@/features/landing/LandingPage'
import { PublicLayout } from '@/features/public/PublicLayout'

// Admin, platform and sign-in pages are lazy-loaded so they end up in their own
// chunks and public pages never download admin or Supabase code.
export const routes: RouteObject[] = [
  {
    // The landing page is eager, like the public pages (#176).
    path: '/',
    Component: LandingPage,
  },
  {
    path: '/login',
    lazy: async () => ({
      Component: (await import('@/features/auth/LoginPage')).LoginPage,
    }),
  },
  {
    path: '/accept-invite',
    lazy: async () => ({
      Component: (await import('@/features/auth/AcceptInvitePage')).AcceptInvitePage,
    }),
  },
  {
    path: '/admin/*',
    lazy: async () => ({
      Component: (await import('@/features/admin/AdminArea')).AdminArea,
    }),
  },
  {
    path: '/platform/*',
    lazy: async () => ({
      Component: (await import('@/features/platform/PlatformArea')).PlatformArea,
    }),
  },
  {
    path: '/:tenantSlug/*',
    Component: PublicLayout,
  },
  {
    path: '*',
    Component: NotFoundPage,
  },
]
