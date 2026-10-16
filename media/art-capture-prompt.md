# Prompt — artsy captures of any app

Paste into a session that has the project checked out. Fill in the `<…>` parts or delete them.

---

Make a set of gallery-quality, art-directed images of what this project draws: <its UI, components, charts, effects, game scenes, generated output>. They're for <social posts / landing page / launch / README>. They should read as prints or editorial images, not raw screenshots. Every pixel has to come from the project's own rendering. Don't paint over it, generate images with AI, or mock up fake content.

**1. Find the rendering seam**

- Read the code and work out the lowest-level place where a visual gets drawn: a render or draw method, a component in isolation, a canvas or shader pass, a storybook story, a headless test harness, an export function.
- Prefer driving that seam offscreen over screenshotting the running app. You get any resolution, a clean background and no chrome.
- If something animates or simulates, step it with a fixed timestep and a fixed seed, so a shot can be reproduced exactly.
- If there's no seam, run the app in a large window at 2× or 3× device scale and capture just the element. Hide the dev overlays and use a Release or production build.
- Put any capture-only code (a CLI flag, a script, a story) in a scratch copy or a clearly separate tool. Leave the product code unchanged unless I say otherwise.

**2. Survey, then curate**

- Render everything once at small size and build a contact sheet.
- Pick the 10–16 subjects with the strongest silhouette, texture or composition.
- Leave out things that only make sense in motion or in context, and say which ones you skipped and why.

**3. Art direct from the project's own identity**

- Pull the palette, type and surfaces from the project's design tokens, theme or CSS. Use only those, plus one or two deliberate variations, for example inverting to a light "paper" surface with dark ink.
- Vary the framing: wide 16:9 hero shots, squares, and 4:5 portraits.
- Scale content up crisply. Render at a small logical size and multiply the scale, so stroke widths and spacing keep the proportions they were designed with.
- For anything that moves, try a long exposure: blend the last N frames together (lighten on dark backgrounds, darken on light). For static UI, try tight detail crops, a repeated grid of states, or a single component isolated on a large empty field.
- Tune the subject's own parameters (density, size, seed, data) for composition. Never draw anything new.

**4. Finishing pass** (a script, so it's repeatable)

- **Bloom:** apply it to bright pixels only, so the blacks stay black.
- **Vignette and grain:** keep both subtle.
- **Captions:** small and editorial, in the project's own typeface. Use real metadata (the item's name, number and description from the code), plus a small wordmark.
- **Legibility:** wherever a caption would sit on busy content, add a soft scrim behind it.
- **Variants:** export each piece with and without its caption, as high-quality JPEG (or PNG when there's no grain).
- **Poster:** add one overview poster, a grid of the best pieces with a title and a one-line description of the project.

**5. Review loop**

After each batch, look at downscaled thumbnails and fix what's wrong:
- washed-out blacks
- captions colliding with the subject
- subjects too small or off-centre
- banding, blocky or aliased edges
- anything that misrepresents what the product actually does

Re-render only the shots that need it.

**6. Honesty and delivery**

- Don't show performance numbers, data or states the product wouldn't really produce.
- Note anything you tuned beyond the defaults.
- Save the outputs in `<media/art/>` and `<media/art/clean/>`, plus `<media/_source/>` with the shot list, the capture code, the finishing script, and a README saying how each image was made and how to regenerate it.
- Commit only to the branch I name.
- Run slow renders in the background.

---

## Optional add-ons

- **Regular screenshots:** also capture 4–6 honest UI screenshots of the real app at a fixed window size, showing the main flows.
- **Recording:** also record one 30–60 s real-time walkthrough of the real app with a scripted pointer. Don't speed it up.
