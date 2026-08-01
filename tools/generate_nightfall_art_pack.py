#!/usr/bin/env python3
"""Generate production-ready Nightfall Meadow reference PNGs for Unity and web."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import math
import random

ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "Assets/Art/NightfallMeadow/Generated"
WEB = ROOT / "web/assets/nightfall/generated"
UNITY.mkdir(parents=True, exist_ok=True)
WEB.mkdir(parents=True, exist_ok=True)

P = {
    "night": "#101126", "ink": "#21192B", "field": "#1C302B", "field2": "#29483B",
    "field3": "#3B6851", "path": "#635D4D", "path2": "#81745C", "water": "#173A46",
    "paper": "#F5E4B8", "paper2": "#E7C98E", "cream": "#FFF2C9", "mint": "#8EE3C2",
    "lav": "#BFA7FF", "coral": "#FF758F", "peach": "#FFB98B", "gold": "#FFD56A",
    "wood": "#6F4938", "wood2": "#3D2A2C", "leaf": "#63B36F", "leaf2": "#244E3D",
    "slime": "#7D66B4", "horn": "#B56A5D", "white": "#FFFDF6"
}
R = random.Random(147)


def font(size, bold=False):
    paths = [
        "/usr/share/fonts/truetype/dejavu/DejaVuSansCondensed-Bold.ttf" if bold else "/usr/share/fonts/truetype/dejavu/DejaVuSansCondensed.ttf",
        "/usr/share/fonts/truetype/liberation2/LiberationSans-Bold.ttf" if bold else "/usr/share/fonts/truetype/liberation2/LiberationSans-Regular.ttf",
    ]
    for path in paths:
        try:
            return ImageFont.truetype(path, size)
        except OSError:
            pass
    return ImageFont.load_default()


def panel(draw, box, fill, outline=None, cut=4, width=1):
    x0, y0, x1, y1 = box
    pts = [(x0+cut,y0),(x1-cut,y0),(x1,y0+cut),(x1,y1-cut),(x1-cut,y1),(x0+cut,y1),(x0,y1-cut),(x0,y0+cut)]
    draw.polygon(pts, fill=fill)
    if outline:
        draw.line(pts + [pts[0]], fill=outline, width=width)


def text(draw, xy, value, size, fill, anchor="la", bold=False):
    draw.text(xy, value, font=font(size, bold), fill=fill, anchor=anchor)


def star(draw, x, y, radius, color, core=None):
    pts = []
    for i in range(8):
        a = -math.pi/2 + i*math.pi/4
        r = radius if i % 2 == 0 else radius*0.42
        pts.append((x + math.cos(a)*r, y + math.sin(a)*r))
    draw.polygon(pts, fill=color)
    if core:
        draw.rectangle((x-1,y-1,x+1,y+1), fill=core)


def tree(draw, x, y, scale=1):
    draw.ellipse((x-14*scale,y+6*scale,x+14*scale,y+13*scale), fill=(5,8,13,100))
    draw.rectangle((x-2*scale,y+5*scale,x+2*scale,y+16*scale), fill="#594234")
    for ox, oy, rr, c in [(-8,-3,9,"#183A31"),(1,-9,11,"#245044"),(10,-2,9,"#1B4036"),(-4,6,10,"#2E5C49"),(7,6,9,"#254D40")]:
        draw.ellipse((x+(ox-rr)*scale,y+(oy-rr)*scale,x+(ox+rr)*scale,y+(oy+rr)*scale), fill=c, outline="#102B25")


def stone(draw, x, y, scale=1):
    draw.ellipse((x-7*scale,y+2*scale,x+7*scale,y+6*scale), fill=(5,8,13,85))
    pts=[(x-6*scale,y+2*scale),(x-3*scale,y-4*scale),(x+2*scale,y-6*scale),(x+7*scale,y-1*scale),(x+4*scale,y+4*scale),(x-4*scale,y+5*scale)]
    draw.polygon(pts, fill="#66786F", outline="#293D37")


def flower(draw, x, y, color):
    draw.line((x,y+4,x,y), fill=P["leaf"])
    for dx,dy in ((0,-2),(2,0),(0,2),(-2,0)):
        draw.ellipse((x+dx-1,y+dy-1,x+dx+1,y+dy+1), fill=color)
    draw.point((x,y), fill=P["gold"])


def background():
    img = Image.new("RGBA", (480,270), P["night"])
    d = ImageDraw.Draw(img, "RGBA")
    for y in range(270):
        t=y/269
        c=(int(16+13*t),int(17+31*t),int(38+18*t),255)
        d.line((0,y,480,y), fill=c)
    d.ellipse((370,15,430,75), fill=(191,167,255,25))
    d.ellipse((387,27,413,53), fill=P["cream"])
    d.ellipse((397,21,421,48), fill=P["night"])
    d.polygon([(0,70),(75,49),(145,72),(230,45),(320,73),(400,48),(480,72),(480,105),(0,105)], fill="#16252B")
    panel(d,(10,58,470,264),P["field"],"#415E50",cut=10,width=2)
    d.ellipse((245,82,392,220), fill=P["path"])
    d.ellipse((270,95,384,212), fill=P["field"])
    d.ellipse((292,112,372,201), fill=P["path2"])
    d.ellipse((310,125,360,190), fill=P["field2"])
    d.ellipse((48,155,132,235), fill="#102C36", outline="#416C6F", width=2)
    d.ellipse((56,163,124,227), fill=P["water"])
    d.arc((58,166,121,224),0,180,fill=(142,227,194,80),width=1)
    for _ in range(42):
        x,y=R.randint(25,455),R.randint(75,250)
        if 40<x<140 and 145<y<240: continue
        d.line((x,y,x+R.choice([-2,-1,1,2]),y-R.randint(3,6)), fill=R.choice(["#385F4B","#467058","#315441"]))
    for _ in range(26):
        flower(d,R.randint(25,455),R.randint(80,248),R.choice([P["peach"],P["lav"],P["mint"]]))
    for _ in range(12): stone(d,R.randint(25,455),R.randint(86,245),R.choice([.7,.9,1]))
    for x,y,s in [(22,76,1.2),(65,72,1),(118,69,1.1),(448,82,1.25),(421,135,1),(34,235,1.15),(442,232,1.2)]: tree(d,x,y,s)
    for x,y in [(184,99),(221,213),(403,196)]:
        d.ellipse((x-8,y+3,x+8,y+8), fill=(5,7,12,90)); d.rectangle((x-4,y-12,x+4,y+5), fill=P["wood"]); d.rectangle((x-3,y-17,x+3,y-9), fill=P["gold"])
    d.rectangle((402,90,425,110), fill="#58665F", outline="#273A35")
    d.rectangle((407,79,420,92), fill="#718078", outline="#273A35")
    return img


def courier(d,x,y):
    d.ellipse((x-11,y+10,x+11,y+16), fill=(5,7,12,100))
    d.polygon([(x-11,y+2),(x-15,y+12),(x,y+8),(x+14,y+13),(x+11,y+1)], fill=P["lav"], outline=P["ink"])
    d.rounded_rectangle((x-9,y-3,x+9,y+13),4,fill=P["cream"],outline=P["ink"],width=2)
    d.ellipse((x-10,y-18,x+10,y+2), fill="#C97A64", outline=P["ink"], width=2)
    d.rectangle((x-4,y-10,x-2,y-8), fill=P["ink"]); d.rectangle((x+3,y-10,x+5,y-8), fill=P["ink"])
    d.arc((x-10,y-5,x+10,y+7),10,170,fill=P["coral"],width=3)


def slime(d,x,y):
    d.ellipse((x-10,y+7,x+10,y+12), fill=(5,7,12,100))
    d.polygon([(x-11,y+6),(x-9,y-3),(x-4,y-9),(x+4,y-9),(x+10,y-2),(x+11,y+6),(x+6,y+10),(x-6,y+10)], fill=P["slime"], outline=P["ink"])
    d.point((x-3,y-2),fill=P["white"]); d.point((x+3,y-2),fill=P["white"])


def moonhorn(d,x,y):
    d.ellipse((x-13,y+8,x+13,y+14), fill=(5,7,12,100))
    d.polygon([(x-13,y+6),(x-12,y-4),(x-7,y-7),(x-11,y-15),(x-3,y-9),(x+3,y-9),(x+11,y-15),(x+7,y-7),(x+12,y-3),(x+13,y+6),(x+7,y+11),(x-7,y+11)], fill=P["horn"], outline=P["ink"])
    d.point((x-4,y-2),fill=P["peach"]); d.point((x+4,y-2),fill=P["peach"])


def weapon_icon(d,kind,x,y,scale=1):
    if kind=="wand":
        d.line((x-6*scale,y+7*scale,x+5*scale,y-6*scale),fill=P["paper"],width=max(1,int(2*scale))); star(d,x+6*scale,y-7*scale,5*scale,P["gold"],P["white"])
    elif kind=="notes":
        d.line((x-4*scale,y-6*scale,x-4*scale,y+5*scale),fill=P["lav"],width=max(1,int(2*scale))); d.ellipse((x-8*scale,y+3*scale,x-3*scale,y+8*scale),fill=P["lav"]); d.ellipse((x+1*scale,y,x+6*scale,y+5*scale),fill=P["mint"])
    elif kind=="jar":
        d.rounded_rectangle((x-7*scale,y-5*scale,x+7*scale,y+8*scale),radius=3*scale,fill="#365D55",outline=P["paper"]); star(d,x,y+1*scale,4*scale,P["gold"])
    elif kind=="berry":
        for dx,dy in ((-4,2),(2,3),(0,-3)): d.ellipse((x+(dx-4)*scale,y+(dy-4)*scale,x+(dx+4)*scale,y+(dy+4)*scale),fill=P["coral"],outline=P["ink"])
    elif kind=="needle":
        d.line((x-8*scale,y+7*scale,x+8*scale,y-7*scale),fill=P["white"],width=max(1,int(2*scale))); d.ellipse((x+4*scale,y-9*scale,x+9*scale,y-4*scale),outline=P["mint"])


def entities(img):
    d=ImageDraw.Draw(img,"RGBA")
    courier(d,238,163)
    for x,y in [(170,134),(315,165),(204,215),(354,111),(102,112),(395,230)]: slime(d,x,y)
    for x,y in [(150,225),(330,93)]: moonhorn(d,x,y)
    for x,y in [(210,144),(264,198),(365,151),(82,205)]: star(d,x,y,5,P["mint"],P["white"])
    for x,y in [(230,128),(250,148),(275,174)]: weapon_icon(d,"wand",x,y,.55)
    return img


def hud(img):
    d=ImageDraw.Draw(img,"RGBA")
    panel(d,(10,8,152,45),(25,20,37,225),P["paper2"],cut=6,width=2)
    courier(d,29,29); text(d,(51,20),"MEADOW COURIER",7,P["cream"],bold=True); text(d,(51,36),"♥ ♥ ♥ ♥",11,P["coral"],bold=True)
    panel(d,(205,8,275,42),(25,20,37,225),P["paper2"],cut=6,width=2); text(d,(240,22),"NIGHTFALL",5,P["paper2"],anchor="mm",bold=True); text(d,(240,34),"07:42",13,P["cream"],anchor="mm",bold=True)
    panel(d,(346,8,470,45),(25,20,37,225),P["paper2"],cut=6,width=2); text(d,(358,20),"LEVEL 14",7,P["mint"],bold=True); text(d,(458,20),"724 KILLS",7,P["cream"],anchor="ra",bold=True); text(d,(458,35),"3 CHESTS",6,P["gold"],anchor="ra",bold=True)
    kinds=["wand","notes","jar","berry","needle","wand"]
    start=151
    for i,k in enumerate(kinds):
        x=start+i*31; panel(d,(x,229,x+27,256),(26,21,39,230),P["paper2"],cut=4); weapon_icon(d,k,x+13.5,242.5,.7)
    d.rectangle((10,263,470,268),fill=P["wood2"]); d.rectangle((10,263,289,268),fill=P["mint"]); text(d,(12,261),"LEVEL 14",5,P["cream"],anchor="ls",bold=True); text(d,(468,261),"61%",5,P["cream"],anchor="rs",bold=True)
    return img


def upgrade(img):
    img=entities(img.copy()); img=Image.alpha_composite(img,Image.new("RGBA",img.size,(10,8,27,190))); d=ImageDraw.Draw(img,"RGBA")
    text(d,(240,36),"LEVEL UP",17,P["cream"],anchor="mm",bold=True); text(d,(240,52),"Choose one gift from the night.",6,P["paper2"],anchor="mm",bold=True)
    cards=[("WEAPON • LV. 5","HEARTH NOTES","+1 orbiting note","notes",P["lav"]),("PASSIVE • LV. 2","FIREFLY JAR","Cooldown -15%","jar",P["mint"]),("NEW WEAPON","BERRY BASKET","Splits into 3 seeds","berry",P["coral"])]
    for i,(tag,title,desc,icon,accent) in enumerate(cards):
        x=45+i*148; panel(d,(x+3,71,x+125,224),(5,4,14,180),cut=7); panel(d,(x,68,x+122,221),P["wood"],P["ink"],cut=7,width=2); panel(d,(x+4,72,x+118,217),P["paper"],accent,cut=5,width=2)
        text(d,(x+10,86),tag,5,"#A4496B",bold=True); panel(d,(x+37,95,x+85,143),(38,31,45),accent,cut=6); weapon_icon(d,icon,x+61,119,1.3); text(d,(x+61,159),title,8,P["ink"],anchor="mm",bold=True); text(d,(x+61,177),desc,5,P["wood2"],anchor="mm",bold=True); panel(d,(x+44,195,x+78,211),accent,P["ink"],cut=3); text(d,(x+61,203),str(i+1),7,P["ink"],anchor="mm",bold=True)
    return img


def atlas():
    img=Image.new("RGBA",(512,512),(0,0,0,0)); d=ImageDraw.Draw(img,"RGBA")
    text(d,(16,20),"NIGHTFALL MEADOW / UI ATLAS",11,P["cream"],bold=True)
    panel(d,(16,35,242,99),P["wood"],P["ink"],cut=9,width=2); panel(d,(23,42,235,92),P["paper"],P["gold"],cut=6,width=2); text(d,(42,63),"PARCHMENT PANEL",9,P["ink"],bold=True); text(d,(42,80),"Reusable 9-slice frame",6,P["wood2"])
    for i,(name,c) in enumerate([("NORMAL",P["paper2"]),("HOVER",P["gold"]),("PRESSED",P["coral"]),("DISABLED","#91867A")]):
        x=16+i*119; panel(d,(x,116,x+103,146),P["wood"],P["ink"],cut=5,width=2); panel(d,(x+4,120,x+99,142),c,P["paper"],cut=4); text(d,(x+51,131),name,7,P["ink"],anchor="mm",bold=True)
    for i,k in enumerate(["wand","notes","jar","berry","needle","wand"]):
        x=16+i*57; panel(d,(x,178,x+48,226),(27,21,39,240),P["paper2"],cut=5,width=2); weapon_icon(d,k,x+24,202,1.2)
    for i,(title,c,k) in enumerate([("STAR WAND",P["gold"],"wand"),("FIREFLY JAR",P["mint"],"jar"),("BERRY BASKET",P["coral"],"berry")]):
        x=16+i*162; panel(d,(x,260,x+145,407),P["wood"],P["ink"],cut=8,width=2); panel(d,(x+5,265,x+140,402),P["paper"],c,cut=6,width=2); weapon_icon(d,k,x+72,310,1.8); text(d,(x+72,352),title,9,P["ink"],anchor="mm",bold=True); text(d,(x+72,373),"UPGRADE CARD",6,P["wood2"],anchor="mm",bold=True)
    return img


def save_pair(img,name):
    small=UNITY/f"{name}_480x270.png"; large=UNITY/f"{name}_1920x1080.png"
    img.save(small); img.resize((1920,1080),Image.Resampling.NEAREST).save(large); img.save(WEB/f"{name}_480x270.png")

base=background()
save_pair(base,"background_moonlit_clearing")
save_pair(hud(entities(base.copy())),"gameplay_target")
save_pair(upgrade(base),"upgrade_target")
ui=atlas(); ui.save(UNITY/"ui_atlas_512.png"); ui.resize((2048,2048),Image.Resampling.NEAREST).save(UNITY/"ui_atlas_2048.png"); ui.save(WEB/"ui_atlas_512.png")
ref=ROOT/"Assets/Art/NightfallMeadow/Reference/sprite_sheet_256.png"
if ref.exists():
    sprite=Image.open(ref).convert("RGBA"); sprite.resize((1024,1024),Image.Resampling.NEAREST).save(UNITY/"sprite_sheet_1024.png"); sprite.save(WEB/"sprite_sheet_256.png")
print("Generated Nightfall Meadow PNG assets")
