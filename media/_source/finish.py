"""Finishing pass for the art renders: bloom, vignette, grain, editorial caption.

Usage: python finish.py <raw dir> <out dir>
"""
import sys, os
import numpy as np
from PIL import Image, ImageFilter, ImageDraw, ImageFont, ImageChops

BONE, MUTED, INK, INK_MUTED = (233, 229, 221), (150, 146, 138), (31, 30, 28), (130, 124, 116)

# Set UNO_FX_FONTS to the folder holding Inter (and DejaVu Sans Mono for the poster)
# when they are not installed in one of these places.
FONT_DIRS = [
    os.environ.get('UNO_FX_FONTS', ''),
    '/usr/share/fonts/opentype/inter',
    '/usr/share/fonts/truetype/dejavu',
    os.path.join(os.environ.get('LOCALAPPDATA', ''), 'Microsoft', 'Windows', 'Fonts'),
    os.path.join(os.environ.get('WINDIR', 'C:/Windows'), 'Fonts'),
    os.path.expanduser('~/Library/Fonts'),
]


def font(name, size):
    for folder in FONT_DIRS:
        path = os.path.join(folder, name)
        if folder and os.path.isfile(path):
            return ImageFont.truetype(path, size)
    sys.exit(f'font {name} not found; set UNO_FX_FONTS to the folder that has it')

SHOTS = [
    # file, number, name, description, light surface?
    ('silk-flowfield', 12, 'Flow field', 'Particles carried by a slowly evolving noise field.', False),
    ('attractor-longexp', 40, 'Attractor', 'The Lorenz attractor, plotted in x and z.', False),
    ('vortex-longexp', 44, 'Vortex', 'A drain: particles spiral inwards, faster as they go.', False),
    ('curlnoise-longexp', 13, 'Curl noise', 'Paths following the curl of a noise field.', False),
    ('dipole-longexp', 39, 'Dipole', 'Field lines traced from one pole to the other.', False),
    ('fireworks-longexp', 26, 'Fireworks', 'Shells that burst into a fading shower.', False),
    ('galaxy-longexp', 36, 'Galaxy', 'A two-armed spiral turning faster towards the centre.', False),
    ('orbits-longexp', 28, 'Orbits', 'Nested rings with one body on each.', False),
    ('phyllotaxis-amber', 54, 'Phyllotaxis', 'The sunflower spiral, with a pulse running outwards.', False),
    ('sparkler-longexp', 60, 'Sparkler', 'A hot core throwing short-lived streaks.', False),
    ('constellation', 27, 'Constellation', 'Drifting nodes linked whenever they come close.', False),
    ('arcs', 61, 'Arcs', 'Ragged discharges arcing between two terminals.', False),
    ('plasma', 20, 'Plasma', 'Four interfering sine fields, drawn by a shader.', False),
    ('lissajous-paper', 35, 'Lissajous', 'A lissajous figure drawn by a train of points.', True),
    ('halftone-paper', 46, 'Halftone', 'Rings of dots swelling as a wave passes through them.', True),
    ('metaballs-paper', 23, 'Metaballs', 'Blobs that merge as they pass, shaded from a scalar field.', True),
]


def bloom(im):
    w, h = im.size
    # Only what is brighter than the surface glows, so the blacks stay put.
    bright = Image.eval(im, lambda v: min(255, max(0, v - 48) * 1.4))
    glow = Image.eval(bright.filter(ImageFilter.GaussianBlur(max(w, h) / 220)), lambda v: int(v * 0.55))
    wide = Image.eval(bright.filter(ImageFilter.GaussianBlur(max(w, h) / 60)), lambda v: int(v * 0.30))
    return ImageChops.screen(ImageChops.screen(im, glow), wide)


def scrim(im):
    w, h = im.size
    a = np.asarray(im).astype(np.float32)
    bg = np.median(a.reshape(-1, 3), axis=0) * 0.7
    y = np.linspace(0, 1, h)[:, None, None]
    t = np.clip((y - 0.72) / 0.28, 0, 1) ** 1.3 * 0.88
    return Image.fromarray(np.clip(a * (1 - t) + bg * t, 0, 255).astype(np.uint8))


def vignette(im, strength):
    w, h = im.size
    y, x = np.ogrid[-1:1:h * 1j, -1:1:w * 1j]
    r = np.sqrt(x * x + y * y) / np.sqrt(2)
    mask = 1 - strength * np.clip((r - 0.35) / 0.65, 0, 1) ** 1.6
    a = np.asarray(im).astype(np.float32) * mask[..., None]
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))


def grain(im, amount, seed):
    rng = np.random.default_rng(seed)
    a = np.asarray(im).astype(np.float32)
    n = rng.normal(0, amount, a.shape[:2]).astype(np.float32)
    return Image.fromarray(np.clip(a + n[..., None], 0, 255).astype(np.uint8))


def caption(im, number, name, description, light):
    w, h = im.size
    d = ImageDraw.Draw(im)
    unit = min(w, h)
    big = font('Inter-Regular.otf', int(unit * 0.032))
    small = font('Inter-Regular.otf', int(unit * 0.017))
    eyebrow = font('Inter-Medium.otf', int(unit * 0.0145))
    fg, muted = (INK, INK_MUTED) if light else (BONE, MUTED)
    m = int(unit * 0.06)

    num = f'{number:02d}'
    base = h - m - int(unit * 0.03)
    d.text((m, base), num, font=big, fill=muted, anchor='ls')
    nx = m + d.textlength(num, font=big) + int(unit * 0.018)
    d.text((nx, base), name, font=big, fill=fg, anchor='ls')
    d.text((m, base + int(unit * 0.034)), description, font=small, fill=muted, anchor='ls')

    label = 'U N O . E F F E C T S'
    d.text((w - m, base + int(unit * 0.034)), label, font=eyebrow, fill=muted, anchor='rs')
    return im


def main(raw, out):
    missing = [f for f, *_ in SHOTS if not os.path.isfile(os.path.join(raw, f + '.png'))]
    if missing:
        sys.exit(f'missing raw renders in {raw}: {", ".join(missing)}')

    os.makedirs(os.path.join(out, 'clean'), exist_ok=True)
    for i, (file, number, name, description, light) in enumerate(SHOTS):
        im = Image.open(os.path.join(raw, file + '.png')).convert('RGB')
        if light:
            im = vignette(im, 0.10)
            im = grain(im, 5.0, i)
        else:
            im = bloom(im)
            im = vignette(im, 0.45)
            im = grain(im, 4.0, i)
        slug = f'{number:02d}-{file}'
        im.save(os.path.join(out, 'clean', slug + '.jpg'), quality=95, subsampling=0)
        if not light:
            im = scrim(im)
        caption(im, number, name, description, light).save(os.path.join(out, slug + '.jpg'), quality=95, subsampling=0)
        print('finished', slug, im.size)


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2])
