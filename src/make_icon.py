from PIL import Image, ImageDraw
import struct, io, os

S = 1024
C1 = (0x46, 0x86, 0xFF)   # 顶部亮蓝
C2 = (0x23, 0x53, 0xD8)   # 底部深蓝


def base():
    img = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    grad = Image.new('RGB', (S, S))
    d = ImageDraw.Draw(grad)
    for y in range(S):
        t = y / (S - 1.0)
        # 轻微曲线，避免死板
        t = t ** 0.9
        c = tuple(int(round(C1[i] + (C2[i] - C1[i]) * t)) for i in range(3))
        d.line([(0, y), (S, y)], fill=c)
    mask = Image.new('L', (S, S), 0)
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, S - 1, S - 1], radius=int(S * 0.225), fill=255)
    img.paste(grad, (0, 0), mask)
    return img


def glyph(img):
    d = ImageDraw.Draw(img)
    # 线缆
    d.rounded_rectangle([int(S * 0.135), int(S * 0.468), int(S * 0.335), int(S * 0.532)],
                        radius=int(S * 0.032), fill=(255, 255, 255, 235))
    # 插头主体
    d.rounded_rectangle([int(S * 0.295), int(S * 0.365), int(S * 0.755), int(S * 0.635)],
                        radius=int(S * 0.075), fill=(255, 255, 255, 255))
    # 插针
    for cx in (0.385, 0.492, 0.599):
        r = S * 0.0285
        x = S * cx
        d.ellipse([x - r, S * 0.5 - r, x + r, S * 0.5 + r], fill=C2 + (255,))
    # 右侧开口（深色小缺口）
    d.rounded_rectangle([int(S * 0.700), int(S * 0.442), int(S * 0.760), int(S * 0.558)],
                        radius=int(S * 0.030), fill=C2 + (255,))
    return img


master = glyph(base())

sizes = [16, 24, 32, 48, 64, 128, 256]
datas = []
for s in sizes:
    buf = io.BytesIO()
    master.resize((s, s), Image.LANCZOS).save(buf, 'PNG', optimize=True)
    datas.append(buf.getvalue())

header = struct.pack('<HHH', 0, 1, len(sizes))
offset = 6 + 16 * len(sizes)
entries = b''
for s, data in zip(sizes, datas):
    w = 0 if s >= 256 else s
    entries += struct.pack('<BBBBHHII', w, w, 0, 0, 1, 32, len(data), offset)
    offset += len(data)

out = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'app.ico')
with open(out, 'wb') as f:
    f.write(header + entries + b''.join(datas))

master.resize((256, 256), Image.LANCZOS).save(os.path.join(os.path.dirname(out), 'icon_preview.png'))
print('ok', out, os.path.getsize(out))
