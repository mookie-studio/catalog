# Landing page

The landing page presents **Just Looking** (the public name of Catalog) to people who have never seen it, and sends them to the demo. It lives at `/`. Brand and look: [DESIGN.md](../../DESIGN.md) and [ADR 0015](../adr/0015-just-looking-brand-and-design-system.md). The first screen, the sections below it and the footer are described here as they are built (#183, #184).

## Language

The landing page is the only page in two languages, **English** and **Spanish**. The rest of the interface (public catalog, admin, platform, sign-in) stays in English.

How the language is chosen, in order:

1. **The visitor's own choice wins.** If the visitor picked a language with the switcher before, on this browser, the page opens in that language, whatever the browser says.
2. **Otherwise the browser decides.** The page looks at the browser's preferred languages in order and takes the first one that is English or Spanish: Spanish for `es`, `es-ES`, `es-MX` and every other Spanish variant, English for every English variant. Languages that are neither are skipped.
3. **Otherwise English.** When the browser lists no English or Spanish language (or none at all), the page is in English.

Examples:

| Browser languages | Saved choice | Page language |
| --- | --- | --- |
| `es-ES` | none | Spanish |
| `es-MX`, `en` | none | Spanish |
| `en-GB`, `es` | none | English |
| `fr-FR`, `es` | none | Spanish |
| `fr-FR` | none | English |
| none | none | English |
| `es-ES` | English | English |
| `en-US` | Spanish | Spanish |
| `es-ES` | any other value | Spanish (the value is ignored) |

## Language switcher

- Two buttons in the landing's top bar, **EN** and **ES** (screen readers announce them as "English" and "Español"). The current language is the pressed one (`aria-pressed="true"`).
- Pressing one switches every text of the page at once, without reloading, and saves the choice.
- The choice is saved in the browser's local storage under the key `jl.landingLanguage` (value `en` or `es`). It stays on that browser until the visitor clears its site data; it does not follow the visitor to other browsers or devices, and nothing is sent to the API.
- When the browser blocks storage (private windows, blocked site data), the switcher still changes the language for the current visit; the next visit follows the browser rule again.
- While the landing page is open, the page's `<html lang>` is set to the current language (`en` or `es`), so screen readers and translators read it correctly. Leaving the landing page puts the previous value back.

## Texts

All texts live in `web/src/features/landing/landing-copy.ts`, one object per language with the same keys (a missing translation fails the build). Fixed texts agreed with the product owner:

| | English | Spanish |
| --- | --- | --- |
| Primary button | Try the demo | Probar la demo |
| Stamp on the tags | Just looking | Solo mirando |
| Headline | Let them look at everything they want. | Que miren todo lo que quieran. |

The texts state only what Catalog does today. They never mention prices, customers, figures or features that do not exist. Product names on the demo tags are the Demo dataset's own names and are not translated; their attribute labels are.
