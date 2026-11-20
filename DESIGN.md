# Just Looking design system

The single source of truth for how Just Looking looks: colours, type, space, shape, motion, the logo and the components. Every value here is exact; the SPA's tokens (`web/src/index.css`) must match it. A browsable version with live previews: https://claude.ai/artifact/B5V29B7Dvx4w8C1ikSi46V. Decision record: [ADR 0015](docs/adr/0015-just-looking-brand-and-design-system.md).

## The idea

"Just looking" is what a shopper says when browsing without buying, and the product has no prices, cart or checkout. The brand plays that line straight: product data printed on card-stock **hang tags** hanging from a string, with the price covered by a rubber **stamp** that says "Just looking" ("Solo mirando" in Spanish).

## Where it applies

- **Landing page** (`/`): the full brand. Plum `landing-ground` in both themes, mustard and lilac HangTags, the plum-mustard wordmark, `display-xl` headline, the tag Button for "Try the demo". English and Spanish.
- **Sign-in, admin and platform** (`/login`, `/accept-invite`, `/admin`, `/platform`): the app theme, light and dark. `surface` grounds, `ink` text, `accent` for the one primary action, StatusBadges, Fields. The wordmark sits in the header; the brand shows in details (the Stamp on the 404 page, mustard for Featured, a decorative tag on the sign-in panel), never as large colour fields.
- **A Tenant's public catalog** (`/{tenantSlug}`): the Tenant leads. Its Logo replaces ours in the header; its Brand color, when set, replaces `accent` and `focus-ring` for its header and buttons, with black or white text by contrast. Our neutrals, type and components stay. Just Looking appears only as a footer link "Made with Just Looking".

## Content

- Write plainly: short sentences, active voice. Controls name their action ("Publish", "Export CSV", "Try the demo"); errors name the problem and the fix.
- Use the glossary terms (Product, Category, Attribute, Inquiry, Tenant profile). Never "item", "listing" or "customer".
- Never show or imply a price, cart, order or stock. Never invent customers, figures or testimonials.
- No emoji. Codes (SKU, slugs, ids) in the `data` style.

## Colour

Two themes for the app, Light and Dark. The landing commits to plum in both. Aliases in braces point to another token.

| Token | Light | Dark | Usage |
| --- | --- | --- | --- |
| `plum-900` | `#25122f` | `#25122f` | Deepest plum. Ink on light grounds and on mustard; the landing's tag string. |
| `plum-700` | `#3b1f4a` | `#3b1f4a` | Brand plum. The landing ground and the app icon; primary button fill in the light theme. |
| `plum-500` | `#7a3e96` | `#7a3e96` | Mid plum. Focus ring in the light theme; links on light grounds. |
| `mustard-400` | `#f2b134` | `#f2b134` | Brand mustard. Hang tags, the highlight word, the primary button on plum grounds and in the dark theme. Never text on light grounds. |
| `lilac-100` | `#eadcf5` | `#eadcf5` | Lilac card stock. Secondary hang tags and selected rows on light grounds. |
| `lilac-50` | `#f7f0fb` | `#f7f0fb` | Near-white lilac. Text and the reversed logo on plum grounds. |
| `surface` | `#fbf8fd` | `#1a0f21` | Page background of app screens (admin, platform, public catalog). |
| `surface-raised` | `#ffffff` | `#24162d` | Panels, cards, menus and inputs that sit on surface. |
| `surface-sunken` | `#f2ebf7` | `#140a1a` | Table headers, wells, the code/SKU chip ground. |
| `ink` | `#25122f` | `#f7f0fb` | Body text and headings on surface, surface-raised and surface-sunken. |
| `ink-muted` | `#5f4d6b` | `#c2b0cf` | Secondary text, help text, placeholders, on surface, surface-raised and surface-sunken. |
| `border` | `#e2d6ea` | `#3a2a45` | Hairlines between rows and around panels (decorative, carries no meaning). |
| `border-strong` | `#8c7a99` | `#7d6a8b` | Input and control borders; meets 3:1 on surface and surface-raised. |
| `accent` | `{plum-700}` | `{mustard-400}` | Primary action fill (Button primary, active nav). Pair with on-accent. |
| `on-accent` | `#ffffff` | `{plum-900}` | Text and icons on accent. |
| `accent-hover` | `#2e1839` | `#f5c25e` | accent while hovered or pressed. |
| `highlight` | `{mustard-400}` | `{mustard-400}` | Hang tags, the stamp ground and emphasis fills; never body text colour on light grounds. Pair with on-highlight. |
| `on-highlight` | `{plum-900}` | `{plum-900}` | Text on highlight. |
| `link` | `{plum-500}` | `#d9b8ee` | Inline links on surface and surface-raised. |
| `focus-ring` | `{plum-500}` | `{mustard-400}` | Solid 2px focus ring, 2px offset, on every interactive element. |
| `success` | `#1f6b45` | `#7fd6a4` | Published status, success messages; text on surface and surface-raised. |
| `success-soft` | `#ddf2e6` | `#173a28` | Ground of the Published badge; success text reads on it. |
| `warning` | `#8a5300` | `#f2b134` | Warnings and the Draft badge text. |
| `warning-soft` | `#fbeccc` | `#3d2c0c` | Ground of the Draft badge. |
| `danger` | `#b3261e` | `#ff8f85` | Errors, destructive actions, invalid inputs. |
| `danger-soft` | `#fbe3e1` | `#43181a` | Ground of error alerts. |
| `neutral-soft` | `#ece6f0` | `#2e2338` | Ground of the Archived and Handled badges; ink-muted reads on it. |
| `landing-ground` | `{plum-700}` | `{plum-700}` | The landing page ground in both themes: the landing commits to plum. |
| `landing-ink` | `{lilac-50}` | `{lilac-50}` | Text on landing-ground. |
| `landing-ink-muted` | `#d3c2e0` | `#d3c2e0` | Secondary text on landing-ground. |

Rules:

- `ink` on `surface`, `surface-raised` and `surface-sunken`; `ink-muted` for secondary text on the same grounds.
- `accent` + `on-accent` for primary actions only: plum in light, mustard in dark.
- `highlight` (mustard) + `on-highlight` for tags, Featured and emphasis fills. Mustard is never text on light grounds.
- Statuses always pair colour with a word and a dot shape: `success` Published, `warning` Draft, neutral Archived and Handled, lilac Open, mustard Featured.
- Focus ring: solid 2px `focus-ring`, 2px offset, on every interactive element.

### Contrast (WCAG 2)

| Pair | Where | Light | Dark |
| --- | --- | --- | --- |
| `ink` on `surface` | Body text | 16.5:1 | 16.6:1 |
| `ink-muted` on `surface` | Secondary text | 7.2:1 | 9.2:1 |
| `ink-muted` on `surface-raised` | Secondary text on panels | 7.6:1 | 8.5:1 |
| `ink-muted` on `surface-sunken` | Secondary text in wells | 6.5:1 | 9.6:1 |
| `on-accent` on `accent` | Primary button | 14.2:1 | 9.2:1 |
| `on-highlight` on `highlight` | Text on tags and Featured | 9.2:1 | 9.2:1 |
| `link` on `surface` | Links | 6.7:1 | 10.6:1 |
| `success` on `success-soft` | Published badge | 5.5:1 | 7.2:1 |
| `warning` on `warning-soft` | Draft badge | 5.4:1 | 7.1:1 |
| `danger` on `surface` | Errors | 6.2:1 | 8.4:1 |
| `danger` on `danger-soft` | Error alert | 5.3:1 | 6.9:1 |
| `ink-muted` on `neutral-soft` | Archived and Handled badges | 6.2:1 | 7.4:1 |
| `landing-ink` on `landing-ground` | Landing text | 12.7:1 | 12.7:1 |
| `landing-ink-muted` on `landing-ground` | Landing secondary text | 8.5:1 | 8.5:1 |
| `mustard-400` on `plum-700` | Mustard word and stamp on the landing | 7.5:1 | 7.5:1 |
| `focus-ring` on `surface` | Focus ring (needs 3:1) | 6.7:1 | 9.8:1 |
| `border-strong` on `surface` | Control borders (needs 3:1) | 3.7:1 | 3.8:1 |

Text needs 4.5:1; focus rings, control borders and meaningful marks need 3:1. Every pair above passes in both themes.

## Type

Archivo for everything, using its width axis (`font-stretch`); Martian Mono only for data. Both from Google Fonts: `Archivo:wdth,wght@62..125,400..900` and `Martian+Mono:wght@400;600`, `display=swap`.

| Style | Family | Size | Line height | Weight | Tracking | Usage |
| --- | --- | --- | --- | --- | --- | --- |
| `display-xl` | Archivo | 88px | 0.92 | 900 | -0.035em | Landing hero headline only. Set with font-stretch 125%. One per page. |
| `display-l` | Archivo | 56px | 0.95 | 900 | -0.03em | Landing section headings. font-stretch 120%. |
| `display-m` | Archivo | 36px | 1.0 | 800 | -0.02em | Page titles in the app, product name on the product page. font-stretch 110%. |
| `heading` | Archivo | 22px | 1.2 | 700 | 0 | Panel and section headings in the app. |
| `subheading` | Archivo | 17px | 1.35 | 700 | 0 | Form groups, card titles, tag names. |
| `lede` | Archivo | 19px | 1.5 | 400 | 0 | Landing intro paragraphs; max 46ch. |
| `body` | Archivo | 16px | 1.55 | 400 | 0 | Default text everywhere; max 70ch. |
| `body-strong` | Archivo | 16px | 1.55 | 600 | 0 | Emphasis inside body, button labels. |
| `small` | Archivo | 14px | 1.45 | 400 | 0 | Help text, table cells in dense lists, captions. |
| `data` | Martian Mono | 13px | 1.4 | 400 | 0 | SKUs, slugs, attribute values on tags, ids. Only for data, never for prose. |
| `data-label` | Martian Mono | 11px | 1.3 | 600 | 0.06em | Uppercase tenant line on hang tags. |
| `stamp` | Archivo | 15px | 1.1 | 900 | 0.04em | The Stamp component only. font-stretch 75%. |

- Widths: `display-xl` at `font-stretch: 125%`, `display-l` 120%, `display-m` 110%, buttons 108%, `stamp` 75%; text styles at 100%.
- On phones `display-xl` scales down to about 44px and `display-l` to about 34px (`clamp()`).
- Body measure at most 70ch, `lede` at most 46ch. Headings use `text-wrap: balance`. More space above a heading than below it.

## Space, shape, depth

| Spacing | Value | Usage |
| --- | --- | --- |
| `space-1` | 4px | Icon-to-label gaps, chip padding. |
| `space-2` | 8px | Gaps inside controls and between tight items. |
| `space-3` | 12px | Input padding, table cell padding. |
| `space-4` | 16px | Panel padding on phones, gaps between form fields, minimum side gutter. |
| `space-6` | 24px | Panel padding, gaps between cards. |
| `space-8` | 32px | Between groups of a form or page sections in the app. |
| `space-12` | 48px | Above section headings. |
| `space-18` | 72px | Between landing sections. |

| Radius | Value | Usage |
| --- | --- | --- |
| `radius-s` | 4px | Badges, the Stamp, SKU chips. |
| `radius-m` | 8px | Buttons, inputs, menus. |
| `radius-l` | 12px | Panels and product cards. |
| `radius-pill` | 999px | The Sign in pill on the landing, filter chips. |

| Shadow | Light | Dark | Usage |
| --- | --- | --- | --- |
| `shadow-tag` | `0 18px 30px -12px rgba(37,18,47,0.45)` | `0 18px 30px -12px rgba(0,0,0,0.6)` | Hang tags and anything that hangs or floats over a ground. |
| `shadow-raised` | `0 1px 2px rgba(37,18,47,0.08), 0 4px 12px -4px rgba(37,18,47,0.12)` | `0 1px 2px rgba(0,0,0,0.4), 0 4px 12px -4px rgba(0,0,0,0.5)` | Menus, popovers, the sticky bulk-action bar. |

- Hang tags keep their cut top corners (`clip-path: polygon(22% 0, 78% 0, 100% 14%, 100% 100%, 0 100%, 0 14%)`) instead of a radius.
- Panels use a `border` hairline, not a shadow.

## Motion

- One signature movement: hang tags sway slowly (5 to 6 s, ease-in-out, ±2.5deg around the top of the string) and stop on hover and focus.
- Everything else: ease-out transitions of 150 to 300 ms on hover and press; the landing's tag flip at most 400 ms.
- Under `prefers-reduced-motion: reduce` nothing sways or flips with animation. Nothing animates in from invisible.

## Iconography

- Lucide icons (`lucide-react`, already used through shadcn/ui), 16 or 20px, 1.75px stroke, `currentColor`. No emoji or Unicode glyphs as icons.
- The brand symbol (the two eyes) is for brand moments only: favicon, empty states.

## Logo

The wordmark is Archivo at weight 900, semi-expanded, with the two "o" of "Looking" drawn as eyes glancing sideways. The symbol is the two eyes alone. Files in `web/public/brand/`, single-ink outlined SVGs (no font needed):

| File | Ink | Use on |
| --- | --- | --- |
| `just-looking-wordmark.svg` | `#25122f` | Light grounds: surface, surface-raised, lilac, mustard |
| `just-looking-wordmark-reversed.svg` | `#f7f0fb` | Plum and dark grounds |
| `just-looking-wordmark-plum-mustard.svg` | `#f7f0fb`, pupils `#f2b134` | `landing-ground` only |
| `just-looking-symbol.svg` | `#25122f` | Small spaces on light grounds |
| `just-looking-symbol-reversed.svg` | `#f7f0fb` | Small spaces on dark grounds |
| `just-looking-app-icon.svg` | plum square, lilac eyes, mustard pupils | Favicon, app icon, social avatar |

- Always an `<img>` of these files with `alt="Just Looking"`; never retype the wordmark in a font or recolour it.
- Minimum height 20px (wordmark), 16px (symbol). Clear space on every side: the height of one eye.

## mookie studio credit

Just Looking is a product by mookie studio. The maker's credit sits in the landing footer, on the right of its `plum-900` band (stacked below the rest on phones): the label "A product by" (Spanish "Un producto de"), 12px 600 uppercase, 0.06em, `landing-ink-muted`, above the mookie studio wordmark.

- Component `MookieStudioCredit` in `web/src/components/brand/`; files in `web/public/brand/mookie-studio/` (see its README), copied unchanged from mookie studio's logo pack.
- `wordmark-on-dark.svg` on plum and dark grounds, `wordmark-on-light.svg` (prop `onLight`) on light grounds. Never recoloured, redrawn or cropped; its own colours (red `#E0202F`) are the only place that red appears.
- Shown 64px high (the file includes its clear space, so the red mark is about 52px). Keep at least 24px between the credit and any other footer item.
- The wordmark links to mookie studio on GitHub (`https://github.com/mookie-studio`), in a new tab, with the accessible name "mookie studio on GitHub" and a visible focus outline.
- One credit per page, only in a footer. It never replaces the Just Looking logo.

## Components

Shared components live in `web/src/components/` (`ui/` for shadcn primitives, `brand/` for Logo, HangTag, Stamp and MookieStudioCredit). They never import admin, platform or Supabase code.

### Button

- `default` (primary): `accent` fill, `on-accent` text, `accent-hover` on hover. One per view.
- `outline` (secondary): transparent, 2px `border-strong`, `ink` text.
- `ghost`: toolbar and row actions. `destructive`: `danger`, always behind a confirmation panel.
- Tag button (landing only, on `landing-ground`): `highlight` fill, `on-highlight` text, an eyelet dot at the left, `shadow-tag`; tilts -1.5deg and lifts 2px on hover. Used for "Try the demo".
- Archivo 800 at 108%, 16px, padding 12px 20px (`sm`: 14px, 8px 14px), `radius-m`. Disabled: 45% opacity, `not-allowed` cursor.

### Field

- Label 14px 600 `ink`; control on `surface-raised`, 1.5px `border-strong`, `radius-m`, padding 10px 12px; placeholder `ink-muted` with an example, never the label.
- Help text 14px `ink-muted`; error 14px 600 `danger`, linked with `aria-describedby`, and `aria-invalid="true"` turns the border `danger`.
- SKU, slug and other codes use the mono variant (Martian Mono 14px).

### StatusBadge

- 13px 600, padding 5px 9px, `radius-s`, a 7px dot before the word.
- Published: `success` on `success-soft`. Draft: `warning` on `warning-soft`. Archived, Handled: `ink-muted` on `neutral-soft`, hollow dot. Open: `plum-900` on `lilac-100`. Featured: `on-highlight` on `highlight`.

### HangTag

- A 2px `plum-900` string of 24 to 120px, then the card: `highlight` (mustard) or `lilac-100`, cut top corners, an eyelet hole in the ground colour, padding 46px 18px 18px, width 220px.
- Inside: tenant line (Martian Mono 11px 600 uppercase, 0.06em), product name (Archivo 800 20px), up to three attributes (Martian Mono 11px), a 1.5px dashed rule with "Price" and the Stamp, optional link label.
- Used on the landing (hero rail, demo picker, how it works) and the sign-in panel. Never for product lists: those use ProductCard. At most three in one rail, alternating colours.
- As a link the whole tag is one `<a>` named "Open the {tenant} demo", with a visible focus outline around the card.

### Stamp

- Archivo 900 at 75%, 15px, uppercase, 0.04em, 2.5px border in `currentColor`, `radius-s`, padding 2px 8px, rotated -6deg.
- Ink `plum-700` on mustard and lilac; `mustard-400` on plum. At most one per tag or card. Never a status badge.

### ProductCard

- `surface-raised`, 1px `border`, `radius-l`; image 4:3 on `surface-sunken` ("No image" when there is none); body padding `space-4`: name 17px 700, SKU in `data` `ink-muted`, up to two attributes 14px `ink-muted`, Featured badge.
- Hover: `shadow-raised` and a 2px lift. No price, ever.
