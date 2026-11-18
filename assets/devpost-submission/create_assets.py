#!/usr/bin/env python3
"""Rebuild the SIGNSPACE submission graphics. Requires Pillow and the two source illustrations.
Artwork is conceptual, not an in-headset screenshot or ASL instruction chart.
"""
from PIL import Image, ImageDraw, ImageFont, ImageOps, ImageFilter
from pathlib import Path
import math, textwrap

D = Path(__file__).resolve().parent
REG = '/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
BOLD = '/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf'
NAVY='#091627'; INK='#112438'; TEAL='#70dfcf'; WHITE='#f6f8f5'; MUTED='#b4c8cc'; GOLD='#f5bd99'; LINE='#385568'
def font(n,b=False): return ImageFont.truetype(BOLD if b else REG,n)
def draw(im): return ImageDraw.Draw(im)
def txt(im, pos, s, size, color=WHITE, bold=False, spacing=8):
    draw(im).multiline_text(pos,s,font=font(size,bold),fill=color,spacing=spacing)
def cover(path,size):
    return ImageOps.fit(Image.open(D/path).convert('RGB'),size,method=Image.Resampling.LANCZOS,centering=(.5,.5)).convert('RGBA')
def bg(size):
    im=cover('02-background-abstract.png',size)
    layer=Image.new('RGBA',size,(4,14,28,65)); return Image.alpha_composite(im,layer)
def rounded(d,xy,rad,fill,outline=None,width=1): d.rounded_rectangle(xy,radius=rad,fill=fill,outline=outline,width=width)
def mark(im, x, y, s, color=TEAL):
    # Abstract palm made of rounded strokes and an orbital arc: not a letter or a real ASL pose.
    d=draw(im); w=max(3,int(s*.048))
    def pts(a): return [(int(x+u*s),int(y+v*s)) for u,v in a]
    strokes=[[(.28,.62),(.28,.34)],[(.40,.59),(.40,.22)],[(.52,.59),(.52,.17)],[(.64,.61),(.64,.26)],[(.28,.58),(.26,.68),(.36,.80),(.61,.80),(.69,.71),(.69,.60)],[(.28,.60),(.18,.52),(.14,.54),(.15,.59),(.27,.72)]]
    for path in strokes:
        p=pts(path); d.line(p,fill=color,width=w,joint='curve')
        for a,b in (p[0],p[-1]): d.ellipse((a-w//2,b-w//2,a+w//2,b+w//2),fill=color)
    d.arc((x+s*.04,y+s*.04,x+s*.90,y+s*.90), 218, 496, fill=color,width=max(2,int(s*.015)))
    d.ellipse((x+s*.77,y+s*.19,x+s*.81,y+s*.23), fill=GOLD)
def label(im,x,y,s):
    txt(im,(x,y),s,19,TEAL,True)
def footer(im, W, H, small=False):
    d=draw(im); d.line((90,H-80,W-90,H-80),fill='#345162',width=2)
    mark(im,88,H-64,39);txt(im,(137,H-56),'SIGNSPACE',17,WHITE,True)
    d.text((W-95,H-54),'CONCEPT VISUAL',font=font(14),fill=MUTED,anchor='ra')
def fitlines(im, x,y, lines, size=34, color=MUTED, gap=12):
    for line in lines:
        txt(im,(x,y),line,size,color);y+=size+gap

def save(im,name):
    if im.mode != 'RGBA': im=im.convert('RGBA')
    im.save(D/name,optimize=True)

# Transparent logo assets.
im=Image.new('RGBA',(1024,1024)); mark(im,112,80,800); save(im,'03-logo-mark-transparent.png')
im=Image.new('RGBA',(1800,500));mark(im,40,35,400)
txt(im,(440,125),'SIGNSPACE',130,WHITE,True);txt(im,(446,295),'PRACTICE IN THREE DIMENSIONS',30,TEAL,True)
save(im,'04-logo-lockup-transparent.png')
im=Image.new('RGBA',(1024,1024)); d=draw(im);rounded(d,(0,0,1023,1023),200,NAVY);rounded(d,(24,24,999,999),180,None,'#28485b',3);mark(im,137,117,750);save(im,'05-app-icon.png')

# Main promo graphic: original AI artwork is deliberately labelled in the asset guide.
im=cover('01-hero-illustration.png',(1920,1080)); d=draw(im)
# Darken the text side for legibility while retaining the artwork.
d.rectangle((0,0,820,1080),fill=(6,18,33,255))
mark(im,110,90,105);txt(im,(225,119),'SIGNSPACE',38,WHITE,True)
label(im,125,330,'A HANDS-FIRST MIXED REALITY EXPERIENCE')
txt(im,(118,402),'See the shape.\nFind your angle.',70,WHITE,True,spacing=14)
txt(im,(125,630),'Explore ASL handshapes in 3D,\none thoughtful movement at a time.',30,'#c4d8d7',spacing=12)
rounded(d,(125,804,425,856),26,'#173d47');txt(im,(148,816),'EXPLORE  ·  PRACTICE',19,TEAL,True)
txt(im,(125,1009),'CONCEPT ART  /  NOT AN IN-APP SCREENSHOT',16,'#91afaf')
save(im,'06-devpost-cover-1920x1080.png')

# Wide cover and square social.
im=bg((1920,640));mark(im,100,100,125);txt(im,(245,133),'SIGNSPACE',46,WHITE,True)
txt(im,(112,292),'A new angle on handshape practice.',60,WHITE,True)
txt(im,(115,405),'Explore ASL handshapes in mixed reality.',30,MUTED)
save(im,'07-wide-banner-1920x640.png')
im=bg((1200,1200));d=draw(im);rounded(d,(62,64,1138,1136),40,(9,25,40,180),'#315463',2)
mark(im,104,90,125);txt(im,(250,122),'SIGNSPACE',40,WHITE,True)
txt(im,(115,345),'See the shape.\nFind your angle.',78,WHITE,True,spacing=18)
txt(im,(120,655),'A hands-first way to explore\nASL handshapes in 3D.',31,MUTED,spacing=15)
d.line((120,940,1070,940),fill='#3a5963',width=2);txt(im,(120,985),'MIXED REALITY  /  HAND TRACKING',21,TEAL,True)
save(im,'08-social-square-1200.png')

# Diagram tiles.
def card(im,box,num,title,lines):
    d=draw(im);x,y,x2,y2=box
    rounded(d,box,28,(13,33,48,235),'#365867',2)
    d.ellipse((x+30,y+30,x+86,y+86),fill='#234e52');d.text((x+58,y+58),num,font=font(21,True),fill=TEAL,anchor='mm')
    txt(im,(x+30,y+112),title,30,WHITE,True)
    fitlines(im,x+30,y+165,lines,21,MUTED,9)
def arrow(im,x1,y,x2):
    d=draw(im);d.line((x1,y,x2,y),fill=TEAL,width=3);d.polygon([(x2,y),(x2-12,y-7),(x2-12,y+7)],fill=TEAL)

im=bg((1920,1080));label(im,100,80,'THE EXPERIENCE');txt(im,(95,125),'From curiosity to practice',62,WHITE,True)
txt(im,(100,220),'A simple, hands-first learning loop.',29,MUTED)
cards=[('01','INSPECT',['Pinch and turn the','reference hand.']),('02','PRACTICE',['Shape your hand and','compare with the model.']),('03','GET FEEDBACK',['See progress and helpful','near-miss cues.']),('04','BUILD A SHELF',['Add completed shapes','to your practice space.'])]
for i,(n,t,lines) in enumerate(cards):
    x=100+i*450; card(im,(x,380,x+400,690),n,t,lines)
    if i<3:arrow(im,x+406,530,x+443)
footer(im,1920,1080);save(im,'09-diagram-experience-flow.png')

im=bg((1920,1080));label(im,100,75,'HOW IT WORKS');txt(im,(95,120),'From tracked joints to feedback',60,WHITE,True)
txt(im,(100,215),'A simplified view of the system architecture.',29,MUTED)
for i,(n,t,lines) in enumerate([('01','HAND TRACKING',['Joint rotations from','Meta hand input.']),('02','POSE MATCHER',['Compare against the','selected reference pose.']),('03','FEEDBACK',['Progress, hold, and','near-miss cues.']),('04','SPATIAL SHELF',['Completed shapes in','a room or on a plinth.'])]):
    x=100+i*450;card(im,(x,345,x+400,650),n,t,lines)
    if i<3:arrow(im,x+406,495,x+443)
d=draw(im);rounded(d,(490,745,890,865),22,(21,52,60,235),'#3b7073',2)
txt(im,(520,772),'REFERENCE POSES',23,TEAL,True);txt(im,(520,815),'ASL handshape data',20,MUTED)
d.line((690,745,690,670),fill=TEAL,width=3);d.polygon([(690,660),(683,674),(697,674)],fill=TEAL)
footer(im,1920,1080);save(im,'10-diagram-system-architecture.png')

# Four distinct gallery explainer assets, illustrated symbols instead of fake screenshots.
def simple_icon(im,kind,x,y):
    d=draw(im); c=TEAL
    if kind=='inspect':
        mark(im,x,y,440)
    elif kind=='practice':
        mark(im,x+20,y+10,360)
        d.arc((x+20,y+10,x+405,y+395),35,145,fill=GOLD,width=9)
        d.polygon([(x+38,y+146),(x+23,y+169),(x+53,y+164)],fill=GOLD)
    elif kind=='feedback':
        rounded(d,(x+30,y+80,x+420,y+320),35,'#173844','#4f9c98',3)
        for i,w in enumerate((200,300,240)):rounded(d,(x+65,y+125+i*48,x+65+w,y+138+i*48),7,TEAL if i==0 else '#6b8d95')
        d.ellipse((x+330,y+245,x+380,y+295),fill=GOLD)
    elif kind=='shelf':
        d.line((x+15,y+320,x+425,y+320),fill=TEAL,width=10)
        for i in range(3):
            xx=x+75+i*120;d.ellipse((xx,y+150,xx+68,y+218),outline=GOLD,width=7);rounded(d,(xx+17,y+218,xx+51,y+290),16,'#4ea7a0')
        d.arc((x+20,y+36,x+420,y+350),190,350,fill='#45747e',width=3)
def gallery(num,eyebrow,title,body,kind):
    im=bg((1600,900));d=draw(im);label(im,90,75,eyebrow)
    txt(im,(86,145),title,59,WHITE,True,spacing=10)
    txt(im,(90,365),body,27,MUTED,spacing=15)
    rounded(d,(930,160,1490,720),40,(11,34,47,220),'#315b68',2)
    simple_icon(im,kind,985,213)
    footer(im,1600,900);save(im,f'{num:02d}-gallery-{kind}.png')
gallery(11,'01  /  INSPECT','A different\npoint of view.','Turn a 3D reference hand\nto explore the shape from\nthe angle that helps you.', 'inspect')
gallery(12,'02  /  PRACTICE','Your hands are\nthe interface.','Form a handshape and compare it\nwith the reference—without\na controller in the way.', 'practice')
gallery(13,'03  /  FEEDBACK','Helpful, not\njudgmental.','Progress and near-miss cues\nmake room for curiosity\ninstead of a score.', 'feedback')
gallery(14,'04  /  YOUR SPACE','A shelf for\nyour progress.','Completed shapes become objects\nin a practice space that can\nuse your real room.', 'shelf')

# Story card supports authenticity without claiming approvals.
im=bg((1600,900));label(im,100,90,'OUR APPROACH');txt(im,(95,155),'Practice with care.',65,WHITE,True)
txt(im,(100,310),'SIGNSPACE is a companion for\nexploring handshapes—not a\nreplacement for Deaf teachers.',32,MUTED,spacing=18)
mark(im,1040,230,370);footer(im,1600,900);save(im,'15-gallery-learning-with-care.png')

# Clean editable vector mark.
svg='''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1024 1024"><title>SIGNSPACE abstract hand mark</title><g fill="none" stroke="#70dfcf" stroke-linecap="round" stroke-linejoin="round"><g stroke-width="38"><path d="M336 576V352 M432 552V256 M528 552V216 M624 568V288"/><path d="M336 544 L320 624 L400 720 L600 720 L664 648 L664 560"/><path d="M336 560 L256 496 L224 512 L232 552 L328 656"/></g><path stroke-width="12" d="M179 701 A344 344 0 1 1 776 276"/></g><circle cx="744" cy="248" r="16" fill="#f5bd99"/></svg>'''
(D/'03-logo-mark-vector.svg').write_text(svg,encoding='utf-8')
print('Created graphics in',D)
