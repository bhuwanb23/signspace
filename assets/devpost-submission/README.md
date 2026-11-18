# SIGNSPACE Devpost asset pack

A coordinated set of ready-to-upload PNG graphics, plus an editable vector mark. The designs use deep navy, sea-glass teal and warm apricot. They are **concept artwork and explanatory graphics**, not screenshots from the Quest app and not verified demonstrations of its behavior.

## Recommended uploads

| File | Dimensions | Suggested use / accessible description |
|---|---:|---|
| `01-hero-illustration.png` | 1376 × 768 | AI-generated concept art: translucent reference hand in a calm room. |
| `02-background-abstract.png` | 1376 × 768 | AI-generated dark abstract background for presentations or video titles. |
| `03-logo-mark-transparent.png` | 1024 × 1024 | Transparent abstract hand-and-orbit logo mark. |
| `03-logo-mark-vector.svg` | Scalable | Editable vector version of the logo mark; the small geometry differs slightly from the PNG. |
| `04-logo-lockup-transparent.png` | 1800 × 500 | Transparent logo and wordmark for dark backgrounds. |
| `05-app-icon.png` | 1024 × 1024 | Square app or profile icon with transparent rounded corners. |
| `06-devpost-cover-1920x1080.png` | 1920 × 1080 | Main cover: “See the shape. Find your angle.” Uses AI concept art and is labelled accordingly. |
| `07-wide-banner-1920x640.png` | 1920 × 640 | Wide header or presentation banner. |
| `08-social-square-1200.png` | 1200 × 1200 | Square social post or project tile. |
| `09-diagram-experience-flow.png` | 1920 × 1080 | Experience diagram: inspect → practice → feedback → shelf. |
| `10-diagram-system-architecture.png` | 1920 × 1080 | Simplified diagram: hand tracking and reference poses → matcher → feedback → shelf. |
| `11-gallery-inspect.png` | 1600 × 900 | Gallery graphic: explore a reference hand from different angles. |
| `12-gallery-practice.png` | 1600 × 900 | Gallery graphic: practice using hand interactions. |
| `13-gallery-feedback.png` | 1600 × 900 | Gallery graphic: constructive progress and near-miss feedback. |
| `14-gallery-shelf.png` | 1600 × 900 | Gallery graphic: completed shapes in a spatial practice area. |
| `15-gallery-learning-with-care.png` | 1600 × 900 | Gallery graphic: practice with care, alongside Deaf teachers. |

## Suggested Devpost order

1. Cover: `06-devpost-cover-1920x1080.png`.
2. Add your **actual in-headset screenshots and gameplay video** if available; they are the evidence of what your build does.
3. Add `09-diagram-experience-flow.png` and the gallery graphics that match the final build.
4. Use `10-diagram-system-architecture.png` for the technical section, if useful.
5. Use `03-logo-mark-transparent.png` or `05-app-icon.png` for the project thumbnail, depending on the field's cropping.

## Important notes

- The two source illustrations (`01` and `02`) were AI-generated. The cover and background-based graphics use those illustrations. The remaining lettering, icons, and layouts were composited programmatically.
- The logo is an **abstract design**, and the AI hand is conceptual. Neither is an ASL teaching reference. Do not use them to teach or verify handshape accuracy.
- These are concept and explanatory assets, **not actual app screenshots, measured results, evidence of signer approval, or a substitute for a demonstration video**. Review the diagrams and copy against your final build before publishing.
- Check the current Devpost upload requirements and any competition restrictions on AI-generated images. Crop or resize for the actual field rather than assuming a particular size is required.
- The transparent lockup uses light lettering and is intended for a dark background. PNG files preserve transparency where appropriate.
- `create_assets.py` contains the graphic layouts and can regenerate the PNGs from the two source illustrations using Python and Pillow. Its font paths use DejaVu Sans installed on the original system; update `REG` and `BOLD` if needed. The supplied SVG is independently editable.
