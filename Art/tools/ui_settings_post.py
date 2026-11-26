"""Rebuild settings sprites from the retained green-screen image_gen originals.

Run: python Art/tools/ui_settings_post.py
Requires Pillow and numpy. The optional local Pillow install beside the raws is
used only when Pillow is absent from the interpreter. No Unity API is involved.
The generated nine-slice shapes are re-inked to the requested final stroke widths;
their small isolated rivet/gear shapes are recovered from the source palette masks.
"""
from __future__ import annotations

import argparse
import json
import math
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[2]
RAW = ROOT / 'Art/tools/specs/ui_settings_raw'
OUT = ROOT / 'Game/Assets/Resources/UI/Settings'
try:
    from PIL import Image, ImageDraw, ImageFilter, ImageOps
except ImportError:
    sys.path.insert(0, str(RAW / '_deps'))
    from PIL import Image, ImageDraw, ImageFilter, ImageOps
import numpy as np

INK = (0, 0, 0)
GOLD = (242, 178, 58)
PLUM = (30, 16, 24)
CREAM = (243, 226, 192)
LIGHT = (255, 211, 106)
WARM = (58, 26, 30)
GLOW = (255, 194, 74)
PALETTE = (INK, GOLD, PLUM, CREAM)
STATE_PALETTE = (INK, LIGHT, WARM, CREAM, GLOW)
S = 4
SIZES = {
    'tab': (512, 168), 'tab_on': (512, 168),
    'icon_sound': (256, 256), 'icon_display': (256, 256),
    'icon_game': (256, 256), 'icon_keys': (256, 256),
    'keycap': (384, 160), 'keycap_listen': (384, 160),
    'arrow_right': (128, 128), 'arrow_left': (128, 128),
    'row': (1024, 96), 'settings_frame': (1600, 1000),
}
# Unity order: left, bottom, right, top; includes transparent canvas margins.
BORDERS = {
    'tab': (64, 36, 64, 64), 'tab_on': (64, 36, 64, 64),
    'keycap': (48, 48, 48, 48), 'keycap_listen': (48, 48, 48, 48),
    'row': (48, 47, 48, 47), 'settings_frame': (96, 96, 96, 96),
}


def snap(image: Image.Image, palette=PALETTE) -> Image.Image:
    """Palette RGB everywhere with coverage stored exclusively in alpha."""
    a = np.asarray(image.convert('RGBA')).copy()
    flat = a[:, :, :3].reshape(-1, 3)
    pal = np.array(palette, dtype=np.int32)
    for start in range(0, len(flat), 100000):
        block = flat[start:start + 100000].astype(np.int32)
        nearest = ((block[:, None] - pal[None]) ** 2).sum(2).argmin(1)
        flat[start:start + len(block)] = pal[nearest]
    a[a[:, :, 3] == 0, :3] = 0
    return Image.fromarray(a)


def key_raw(name: str) -> Image.Image:
    """Hard green key before resampling: removes spill rather than retaining it."""
    path = RAW / (name + '_raw.png')
    a = np.asarray(Image.open(path).convert('RGBA')).copy()
    r, g, b = (a[:, :, i].astype(np.int16) for i in range(3))
    green = (g > r + 18) & (g > b + 18) & (g > 65)
    a[green] = 0
    image = snap(Image.fromarray(a))
    bbox = image.getbbox()
    if bbox is None:
        raise ValueError(f'No content: {path}')
    return image.crop(bbox)


def components(mask: np.ndarray):
    """Connected gold details, independent of the large continuous border."""
    work = mask.copy()
    h, w = work.shape
    result = []
    for y, x in zip(*np.nonzero(mask)):
        if not work[y, x]:
            continue
        stack = [(int(x), int(y))]
        work[y, x] = False
        coords = []
        while stack:
            px, py = stack.pop()
            coords.append((px, py))
            for nx, ny in ((px-1, py), (px+1, py), (px, py-1), (px, py+1)):
                if 0 <= nx < w and 0 <= ny < h and work[ny, nx]:
                    work[ny, nx] = False
                    stack.append((nx, ny))
        xy = np.array(coords)
        x0, y0 = xy.min(0)
        x1, y1 = xy.max(0) + 1
        if len(coords) >= 3:
            result.append((int(x0), int(y0), int(x1), int(y1), xy))
    return result


def source_details(name: str):
    raw = key_raw(name)
    raw.thumbnail((800, 800), Image.Resampling.LANCZOS)
    a = np.asarray(snap(raw))
    mask = (a[:, :, :3] == GOLD).all(2) & (a[:, :, 3] > 128)
    candidates = []
    for x0, y0, x1, y1, xy in components(mask):
        w, h = x1-x0, y1-y0
        if 3 <= w <= 65 and 3 <= h <= 65 and 0.55 < w/h < 1.8:
            local = np.zeros((h, w), np.uint8)
            local[xy[:, 1]-y0, xy[:, 0]-x0] = 255
            candidates.append((x0/raw.width, y0/raw.height, Image.fromarray(local)))
    return candidates


def detail_mask(name: str, kind='rivet') -> Image.Image:
    candidates = source_details(name)
    if kind == 'gear':
        # Source gear is the largest isolated corner detail below the top band.
        candidates = [c for c in candidates if c[0] < .2 and .015 < c[1] < .25]
        if not candidates:
            raise ValueError('Cannot recover the generated frame corner gear')
        return max(candidates, key=lambda c: c[2].width * c[2].height)[2]
    # Diagonal trim can leave small disconnected gold flecks after palette snap.
    # Real rivet faces are compact filled discs, unlike those sparse slivers.
    candidates = [c for c in candidates if np.asarray(c[2]).mean()/255 > .55]
    if not candidates:
        raise ValueError(f'Cannot recover generated rivet from {name}')
    return min(candidates, key=lambda c: c[0] + c[1])[2]


def stamp(canvas: Image.Image, mask: Image.Image, centre, size, ink, colour=GOLD):
    """Reuse a generated detail, cleaning its outline at final-size resolution."""
    sw, sh = (int(v * S) for v in size)
    inner = mask.resize((sw, sh), Image.Resampling.LANCZOS).point(lambda v: 255 if v >= 128 else 0)
    pad = int(ink * S)
    tile = Image.new('L', (sw+2*pad, sh+2*pad))
    tile.paste(inner, (pad, pad))
    outline = tile.filter(ImageFilter.MaxFilter(2*pad+1))
    x = int(centre[0]*S - tile.width/2)
    y = int(centre[1]*S - tile.height/2)
    canvas.paste((*INK, 255), (x, y, x+tile.width, y+tile.height), outline)
    canvas.paste((*colour, 255), (x, y, x+tile.width, y+tile.height), tile)


def contour(name: str, inset: float, scale=S) -> Image.Image:
    """Parallel nested contours reproduce the generated forms at measured widths."""
    w, h = SIZES[name]
    image = Image.new('L', (w*scale, h*scale))
    d = ImageDraw.Draw(image)
    margin = {'tab': 16, 'keycap': 16, 'row': 2, 'settings_frame': 8}[name]
    x0, y0 = margin+inset, margin+inset
    x1, y1 = w-margin-inset, h-margin-inset
    if name == 'keycap':
        # A thicker lower gold lip, without gradients or extruded shading.
        if inset >= 14:
            y1 -= 7
        d.rounded_rectangle(tuple(round(v*scale) for v in (x0, y0, x1-1/scale, y1-1/scale)),
                            radius=max(1, (26-inset)*scale), fill=255)
    elif name == 'row':
        # Two-pixel apex; both central rows are identical for vertical slicing.
        c = max(1, 25-inset*.55)
        mid = h/2
        points = [(x0, mid-1), (x0+c, y0), (x1-c, y0), (x1, mid-1),
                  (x1, mid+1), (x1-c, y1), (x0+c, y1), (x0, mid+1)]
        d.polygon([(round(x*scale), round(y*scale)) for x,y in points], fill=255)
    else:
        c = max(0, 28 - (2-math.sqrt(2))*inset)
        points = [(x0+c, y0), (x1-c, y0), (x1, y0+c)]
        if name == 'tab':
            points += [(x1, y1), (x0, y1), (x0, y0+c)]
        else:
            points += [(x1, y1-c), (x1-c, y1), (x0+c, y1), (x0, y1-c), (x0, y0+c)]
        d.polygon([(round(x*scale), round(y*scale)) for x,y in points], fill=255)
    return image


def nine_slice_uniform(image: Image.Image, border) -> Image.Image:
    a = np.asarray(image).copy()
    h, w = a.shape[:2]
    left, bottom, right, top = border
    # Copy whole clean columns/rows, including edge coverage and transparent RGB.
    a[:, left:w-right] = a[:, w//2:w//2+1]
    a[top:h-bottom, :] = a[h//2:h//2+1, :]
    return Image.fromarray(a)


def border_sprite(name: str) -> Image.Image:
    # Read each raw even for the clean edge reconstruction, so missing inputs fail.
    key_raw(name)
    image = Image.new('RGBA', tuple(v*S for v in SIZES[name]))
    widths = {'tab': (7,8,2), 'keycap': (7,8,2),
              'row': (4,5,1), 'settings_frame': (10,22,9)}[name]
    a, b, c = widths
    for inset, colour in ((0, INK), (a, GOLD), (a+b, INK)):
        image.paste((*colour,255), (0,0), contour(name, inset))
    inside = contour(name, a+b+c)
    image.paste((0,0,0,0) if name == 'settings_frame' else (*PLUM,255), (0,0), inside)
    rivet = detail_mask(name)
    if name == 'tab':
        centres, size, ink = [(50,49),(462,49)], (9,9), 2
    elif name == 'keycap':
        centres, size, ink = [(40,41),(344,41),(40,118),(344,118)], (7,7), 2
    elif name == 'row':
        centres, size, ink = [(36,48),(988,48)], (5,5), 1
    else:
        centres, size, ink = [(30,30),(1570,30),(30,970),(1570,970)], (7,7), 3
    for centre in centres:
        stamp(image, rivet, centre, size, ink)
    if name == 'settings_frame':
        gear = detail_mask(name, 'gear')
        for centre in [(72,72),(1528,72),(72,928),(1528,928)]:
            stamp(image, gear, centre, (23,23), 2)
    image = snap(image.resize(SIZES[name], Image.Resampling.LANCZOS))
    return nine_slice_uniform(image, BORDERS[name])


def icon_sprite(name: str, margin: int, ink: int) -> Image.Image:
    image = key_raw(name)
    w, h = SIZES[name]
    # Fit and pad the content to the target aspect before LANCZOS resampling.
    # Work at 4x so the strengthened black silhouette has smooth coverage too.
    fitted = ImageOps.contain(image, ((w-2*margin)*S, (h-2*margin)*S), Image.Resampling.LANCZOS)
    canvas = Image.new('RGBA', (w*S,h*S))
    canvas.alpha_composite(fitted, ((w*S-fitted.width)//2, (h*S-fitted.height)//2))
    canvas = snap(canvas)
    # Strengthen only the outside of the original ink by two pixels. Using the
    # silhouette (not the coloured regions) preserves fine interior marks and
    # prevents tiny quantized colour islands from producing contour bumps.
    mask = canvas.getchannel('A').point(lambda v: 255 if v>=128 else 0)
    extra = 2 if ink == 8 else 1
    ink_mask = mask.filter(ImageFilter.MaxFilter(extra*S*2+1))
    ink_mask = ink_mask.filter(ImageFilter.GaussianBlur(S*.65)).point(lambda v: 255 if v>=128 else 0)
    ink_layer = Image.new('RGBA', canvas.size, (*INK,0))
    ink_layer.putalpha(ink_mask)
    result = Image.alpha_composite(ink_layer, canvas)
    # Preserve the original twelve/ten-pixel content margin after re-inking.
    bbox = result.getbbox()
    cropped = result.crop(bbox)
    fitted = ImageOps.contain(cropped, ((w-2*margin)*S,(h-2*margin)*S), Image.Resampling.LANCZOS)
    canvas = Image.new('RGBA', (w*S,h*S))
    canvas.alpha_composite(fitted, ((w*S-fitted.width)//2,(h*S-fitted.height)//2))
    fitted = canvas.resize((w,h),Image.Resampling.LANCZOS)
    canvas = Image.new('RGBA', (w,h))
    canvas.alpha_composite(fitted)
    return snap(canvas)


def selected(normal: Image.Image, name: str) -> Image.Image:
    a = np.asarray(normal).copy()
    a[(a[:,:,:3] == GOLD).all(2), :3] = LIGHT
    a[(a[:,:,:3] == PLUM).all(2), :3] = WARM
    image = Image.fromarray(a)
    # One-pixel cream line at the inner edge of the eight-pixel selected gold band.
    outer = np.asarray(contour(name,14).resize(normal.size,Image.Resampling.LANCZOS))
    inner = np.asarray(contour(name,15).resize(normal.size,Image.Resampling.LANCZOS))
    line = (outer > 128) & (inner <= 128) & (a[:,:,3] == 255)
    a[line,:3] = CREAM
    image = nine_slice_uniform(Image.fromarray(a), BORDERS[name])
    silhouette = normal.getchannel('A')
    # 14px finite-support outer halo, maximum alpha 114/255 (< 0.45).
    blur = silhouette.filter(ImageFilter.MaxFilter(5)).filter(ImageFilter.GaussianBlur(4.2))
    support = np.asarray(silhouette.filter(ImageFilter.MaxFilter(29))) > 0
    halo_alpha = np.minimum(np.asarray(blur,dtype=np.float32)*.45,114).astype(np.uint8)
    halo_alpha[~support] = 0
    halo = Image.new('RGBA', normal.size, (*GLOW,0))
    halo.putalpha(Image.fromarray(halo_alpha))
    image = snap(Image.alpha_composite(halo,image), STATE_PALETTE)
    # Keep glow coverage also exactly repeatable through the stretch regions.
    return nine_slice_uniform(image,BORDERS[name])


def verify(images: dict[str, Image.Image]):
    report = []
    for name, image in images.items():
        a = np.asarray(image)
        assert image.mode == 'RGBA' and image.size == SIZES[name], name
        assert all(a[y,x,3] == 0 for x,y in [(0,0),(image.width-1,0),
                   (0,image.height-1),(image.width-1,image.height-1)]), name
        r,g,b = (a[:,:,i].astype(np.int16) for i in range(3))
        assert not ((g > r+18)&(g > b+18)).any(), name
        palette = STATE_PALETTE if name in ('tab_on','keycap_listen') else PALETTE
        colours = set(map(tuple,a[a[:,:,3]>0,:3]))
        assert colours <= set(palette), (name,colours-set(palette))
        assert (a[:,:,3]==0).any() and (a[:,:,3]==255).any(), name
        if name in BORDERS:
            l,bot,r,t = BORDERS[name]
            assert np.all(a[:,l:image.width-r] == a[:,image.width//2:image.width//2+1]), name+' columns'
            assert np.all(a[t:image.height-bot] == a[image.height//2:image.height//2+1]), name+' rows'
        report.append({'name':name,'size':list(image.size),'mode':image.mode,
                       'transparent_corners':True,'green_pixels':0,'palette_colours':len(colours),
                       'borders_LBRT':list(BORDERS[name]) if name in BORDERS else None})
    for normal,state in [('tab','tab_on'),('keycap','keycap_listen')]:
        a,b = np.asarray(images[normal]),np.asarray(images[state])
        assert np.array_equal(a[:,:,3]==255,b[:,:,3]==255), state+' opaque silhouette'
        mask = a[:,:,3]>0
        assert np.all(b[mask,3]>=a[mask,3]), state+' shifted silhouette'
        assert np.max(b[~mask,3])<=114, state+' glow opacity'
    assert np.array_equal(np.asarray(images['arrow_left']),np.asarray(images['arrow_right'])[:,::-1]), 'mirror'
    frame = np.asarray(images['settings_frame'])
    assert np.all(frame[96:-96,96:-96,3] == 0), 'frame interior'
    print(f"{'sprite':20} {'size':12} {'mode':5} {'corners':11} {'green':6} {'9-slice L,B,R,T'}")
    for row in report:
        print(f"{row['name']:20} {str(tuple(row['size'])):12} RGBA  transparent 0      {row['borders_LBRT'] or '-'}")
    return report


def checker(size):
    yy,xx = np.indices((size[1],size[0]))
    values = np.where(((xx//16+yy//16)%2)[...,None],68,86).astype(np.uint8)
    return Image.fromarray(np.repeat(values,3,axis=2)).convert('RGBA')


def previews(images):
    sheet = Image.new('RGBA',(1200,1240),(43,41,46,255))
    d = ImageDraw.Draw(sheet)
    positions = [('tab',20,40),('tab_on',620,40),
                 ('keycap',20,260),('keycap_listen',620,260),
                 ('icon_sound',20,470),('icon_display',315,470),
                 ('icon_game',610,470),('icon_keys',905,470),
                 ('arrow_left',20,810),('arrow_right',180,810),('row',20,1060)]
    for name,x,y in positions:
        image=images[name]
        bg=checker(image.size)
        bg.alpha_composite(image)
        sheet.alpha_composite(bg,(x,y))
        d.text((x,y-20),name,fill='white')
    sheet.save(RAW/'final_review.png')
    frame=checker(images['settings_frame'].size)
    frame.alpha_composite(images['settings_frame'])
    frame.save(RAW/'frame_review.png')
    # Stretch previews expose seams without creating Unity assets or metadata.
    sheet=Image.new('RGBA',(1500,1000),(60,60,64,255))
    d=ImageDraw.Draw(sheet)
    y=28
    for name in ('tab','tab_on','keycap','keycap_listen','row'):
        im=images[name]
        l,b,r,t=BORDERS[name]
        target=(1400,im.height)
        stretched=Image.new('RGBA',target)
        for sx0,sx1,dx0,dx1 in [(0,l,0,l),(l,im.width-r,l,target[0]-r),
                                (im.width-r,im.width,target[0]-r,target[0])]:
            strip=im.crop((sx0,0,sx1,im.height)).resize((dx1-dx0,im.height),Image.Resampling.NEAREST)
            stretched.paste(strip,(dx0,0))
        bg=checker(target)
        bg.alpha_composite(stretched)
        sheet.alpha_composite(bg,(20,y))
        d.text((20,y-17),name+' horizontal 9-slice',fill='white')
        y+=im.height+32
    sheet.save(RAW/'stretch_review.png')


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--verify-only',action='store_true')
    args=parser.parse_args()
    if args.verify_only:
        images={name:Image.open(OUT/(name+'.png')) for name in SIZES}
    else:
        images={name:border_sprite(name) for name in ('tab','keycap','row','settings_frame')}
        for name in ('icon_sound','icon_display','icon_game','icon_keys'):
            images[name]=icon_sprite(name,12,8)
        images['arrow_right']=icon_sprite('arrow_right',10,5)
        images['arrow_left']=ImageOps.mirror(images['arrow_right'])
        images['tab_on']=selected(images['tab'],'tab')
        images['keycap_listen']=selected(images['keycap'],'keycap')
        images={name:images[name] for name in SIZES}
    report=verify(images)
    if not args.verify_only:
        OUT.mkdir(parents=True,exist_ok=True)
        for name,image in images.items():
            image.save(OUT/(name+'.png'),optimize=True)
        data={'order':'L,B,R,T','units':'px','borders':{k:list(v) for k,v in BORDERS.items()},
              'notes':{'row':'Two identical centre rows; intended primarily for horizontal stretching.',
                       'glow_margin_px':16,'glow_support_px':14,'glow_max_alpha':114/255}}
        (RAW.parent/'ui_settings_borders.json').write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
        (RAW/'validation.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
        previews(images)


if __name__=='__main__':
    main()
