"""Library poster: a 4x4 grid of the finished pieces, centre-cropped to squares.

Usage: python poster.py <art/clean dir> <output.jpg>
"""
import sys, os
from PIL import Image, ImageDraw
from finish import BONE, MUTED, SHOTS, font

CLEAN, OUT = sys.argv[1], sys.argv[2]
SURFACE, RULE = (20, 20, 18), (59, 58, 54)

# Reading order on the poster; numbers match the catalog.
ORDER = [40, 44, 12, 54, 35, 13, 26, 28, 39, 46, 36, 60, 27, 61, 23, 20]

W, MARGIN, TILE, GAP, PITCH = 2400, 120, 522, 24, 602
GRID_TOP = 520
H = GRID_TOP + 4 * PITCH + 170


def square(path):
    im = Image.open(path).convert('RGB')
    side = min(im.size)
    left, top = (im.width - side) // 2, (im.height - side) // 2
    return im.crop((left, top, left + side, top + side)).resize((TILE, TILE), Image.LANCZOS)


poster = Image.new('RGB', (W, H), SURFACE)
d = ImageDraw.Draw(poster)

d.text((MARGIN - 14, 337), 'Particles', font=font('Inter-Light.otf', 145), fill=BONE, anchor='ls')
d.text((MARGIN, 409), '64 particle and shader effects for Uno Platform. One control, copy-paste XAML.',
       font=font('Inter-Regular.otf', 40), fill=MUTED, anchor='ls')
d.line([(MARGIN, 470), (W - MARGIN, 470)], fill=RULE, width=2)

by_number = {s[1]: s for s in SHOTS}
label = font('Inter-Regular.otf', 26)
for i, number in enumerate(ORDER):
    file, _, name, _, _ = by_number[number]
    x = MARGIN + (i % 4) * (TILE + GAP)
    y = GRID_TOP + (i // 4) * PITCH
    poster.paste(square(os.path.join(CLEAN, f'{number:02d}-{file}.jpg')), (x, y))
    num = f'{number:02d}'
    base = y + TILE + 42
    d.text((x, base), num, font=label, fill=MUTED, anchor='ls')
    d.text((x + d.textlength(num, font=label) + 11, base), name, font=label, fill=BONE, anchor='ls')

footer = H - 150
d.line([(MARGIN, footer), (W - MARGIN, footer)], fill=RULE, width=2)
d.text((MARGIN, footer + 70), '<fx:ParticleView><fx:Snow /></fx:ParticleView>',
       font=font('DejaVuSansMono.ttf', 28), fill=MUTED, anchor='ls')
d.text((W - MARGIN, footer + 70), 'U N O . E F F E C T S', font=font('Inter-Medium.otf', 28), fill=MUTED, anchor='rs')

poster.save(OUT, quality=95, subsampling=0)
print('poster', poster.size, OUT)
