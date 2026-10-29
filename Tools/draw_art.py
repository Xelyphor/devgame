"""Original pixel artwork. Rebuild with Python 3 + Pillow; never needed to run the game."""
from PIL import Image, ImageDraw
from pathlib import Path
import random
ROOT = Path(__file__).resolve().parents[1] / 'Assets/OneMoreBuild/Resources/Art'
ROOT.mkdir(parents=True, exist_ok=True)
rng = random.Random(23)
P = dict(ink='#171d35', wall='#343855', edge='#555270', wood='#805d69', light='#f5cd9a', mint='#8ce3c0', pink='#e9a6b5')
im = Image.new('RGBA', (384,216), '#20283f'); d=ImageDraw.Draw(im)
def rect(box,c): d.rectangle(box,fill=P.get(c,c))
def line(points,c,w=1): d.line(points,fill=P.get(c,c),width=w)
# back wall, left return, floor and skirting
rect((12,12,371,119),'wall'); d.polygon([(12,12),(36,31),(36,133),(12,118)],fill='#292e48')
rect((36,31,371,34),'edge'); rect((36,115,371,122),'#252940')
d.polygon([(12,120),(36,131),(371,131),(371,203),(12,203)],fill='#75596a')
for y in range(133,204,9):
    line([(13,y),(371,y)],'#55445c')
    for x in range(15+(y%3)*27,372,63): line([(x,y),(x,y+8)],'#624f64')
for _ in range(130):
    x,y=rng.randrange(16,368),rng.randrange(134,201); line([(x,y),(x+3,y)],'#896879')
rect((12,204,371,207),'ink'); rect((372,13,375,207),'ink')
# large city window
rect((221,43,339,108),'ink'); rect((225,47,335,104),'#1e2849')
rect((226,48,334,70),'#293457')
for _ in range(27):
    x,y=rng.randrange(228,333),rng.randrange(49,79); rect((x,y,x,y),'#a6b2c9')
d.ellipse((302,51,314,63),fill='#e7d6b3'); d.ellipse((299,49,310,60),fill='#293457')
for x in range(228,336,13):
    y=rng.randrange(74,90); rect((x,y,x+10,103),'#151c35')
    for yy in range(y+4,101,6):
        for xx in range(x+2,x+9,4):
            if rng.random()<.65: rect((xx,yy,xx+1,yy+1),'#bc9b93')
rect((278,47,281,105),'edge'); rect((224,75,336,77),'edge'); rect((218,107,342,111),'#81758c')
# wall poster
rect((63,45,98,86),'ink'); rect((66,48,95,83),'#d69eac'); rect((69,51,92,80),'#4d5679')
d.polygon([(72,72),(80,57),(89,72)],fill='#f2c98e'); rect((77,72,84,75),'mint')
# fairy light cable
line([(110,40),(145,48),(179,43),(209,47)],'#202840')
for x,y in [(114,42),(130,46),(148,48),(165,46),(183,45),(202,47)]:
    rect((x-1,y-1,x+2,y+3),'#b58b86'); rect((x,y,x+1,y+1),'light')
# shelf books and plant
rect((57,102,119,107),'#ac7d80'); rect((60,108,63,114),'ink');rect((113,108,116,114),'ink')
for x,h,c in [(62,17,'#8ca8a6'),(69,20,'#d69eac'),(77,15,'#e1bd91'),(84,18,'#8294ba')]:
    rect((x,101-h,x+5,101),c); line([(x+1,99-h),(x+4,99-h)],'light')
rect((101,94,112,101),'#cb8c80'); line([(106,93),(106,78)],'#81af9c',2)
d.ellipse((97,80,105,87),fill='#77a997'); d.ellipse((106,77,115,84),fill='#9bc4a1')
# rug
rect((95,149,287,194),'#433e5c');rect((99,152,283,190),'#a57582');rect((103,155,279,187),'#b9858e')
for x in range(108,278,12): line([(x,156),(x+17,186)],'#a87483')
line([(104,158),(278,158)],'#d6a3a5'); line([(104,184),(278,184)],'#d6a3a5')
# desk shadow and legs
rect((135,150,256,163),'#3c354f');rect((144,127,150,159),'ink');rect((240,127,246,159),'ink')
rect((138,108,253,128),'ink');rect((137,105,253,119),'#c48c83');rect((139,106,251,109),'#efb89b');rect((139,120,252,126),'#956876')
# computer tower, monitor, keyboard
rect((228,84,244,105),'ink');rect((230,86,242,102),'#4e526e');rect((232,88,240,89),'#85899f');rect((238,98,239,99),'mint')
rect((171,76,214,103),'ink');rect((174,79,211,98),'#75b7b3');rect((176,81,209,96),'#315573')
for yy,ww in [(83,19),(86,25),(89,13),(92,22)]:rect((179,yy,179+ww,yy),'#b1e3cd')
rect((190,104,195,108),'#434761');rect((184,108,202,109),'#31354e')
rect((171,113,210,116),'#e4bdac')
for x in range(175,209,4): line([(x,113),(x,115)],'#8e7283')
rect((214,114,219,117),'#ddd0c3')
# coffee mug
rect((151,104,159,112),'light');rect((159,106,162,110),'#c59c90');rect((153,103,157,104),'#4a3347')
# baseline chair
rect((184,136,205,154),'#343950');rect((186,134,203,143),'#60607b');rect((193,154,196,164),'ink');rect((184,164,205,166),'ink')
# beanbag and low table
rect((48,162,79,180),'ink');d.ellipse((45,147,83,177),fill='#82809d');d.ellipse((49,149,79,168),fill='#a198b1');line([(53,173),(74,173)],'#65647f')
rect((303,158,338,162),'#b88b86');rect((306,163,309,181),'ink');rect((332,163,335,181),'ink')
rect((312,153,325,158),'#eee1bd');rect((315,151,323,153),'#c29b9d')
# cat sleeping
for box,c in [((306,187,329,195),'#dda478'),((303,185,313,193),'#efbe92'),((304,182,307,187),'#efbe92'),((311,183,314,187),'#efbe92')]:rect(box,c)
line([(306,189),(308,189)],'ink');line([(327,192),(336,192),(336,187)],'#dda478',3)
im.save(ROOT/'room.png')
# Upgrade overlays use the same room coordinates, transparent background.
for idx in range(6):
    im=Image.new('RGBA',(384,216));d=ImageDraw.Draw(im)
    if idx==0:
        rect((228,82,247,105),'ink');rect((231,85,244,102),'#6c7295');rect((233,88,242,89),'mint');rect((233,93,242,94),'mint')
    elif idx==1:
        rect((181,132,208,157),'ink');rect((184,133,205,153),'#749d9c');rect((188,136,201,149),'#98bcb0');rect((180,145,183,153),'#d0b4a0');rect((206,145,209,153),'#d0b4a0')
    elif idx==2:
        rect((118,82,144,100),'ink');rect((120,84,142,98),'#ddbe91');rect((124,88,137,89),'#537b86');rect((124,92,134,93),'#537b86');rect((128,100,133,105),'ink')
    elif idx==3:
        rect((349,149,363,166),'#cb8c80');rect((348,148,364,151),'#edb19b');line([(356,148),(356,116)],'#7ca68b',2)
        for x,y in [(344,127),(355,119),(345,138),(356,132)]:d.ellipse((x,y,x+12,y+7),fill='#9ac39e')
        rect((262,91,265,117),'#eac48d');d.polygon([(258,85),(270,85),(275,94),(253,94)],fill='#ffdb9c');rect((258,117,269,119),'#eac48d')
    elif idx==4:
        rect((145,114,166,119),'ink');rect((147,115,164,117),'#9b91b4');line([(162,111),(167,115)],'light')
    else:
        rect((169,70,217,104),'ink');rect((172,73,214,99),'#719bba');rect((175,76,211,96),'#34506c')
        for yy in range(79,95,4):rect((178,yy,201+(yy%3)*3,yy),'mint')
        rect((110,61,143,76),'ink');rect((112,63,141,74),'#b2a0b9');line([(116,68),(120,71),(128,65),(138,71)],'#e2ca9c',2)
    im.save(ROOT/('upgrade'+str(idx)+'.png'))
# Character animation: seated developer, rest pose and release celebration.
for frame in range(4):
    im=Image.new('RGBA',(40,48));d=ImageDraw.Draw(im)
    rect((13,29,18,42),'#282f49');rect((24,29,29,42),'#282f49');rect((11,42,18,44),'ink');rect((24,42,31,44),'ink')
    rect((11,17,29,33),'#a9b7c5');rect((14,18,27,32),'#d7c6bd')
    rect((13,5,28,19),'#d9a58c');rect((11,4,28,11),'#44374c');rect((10,8,15,15),'#44374c');rect((15,3,25,5),'#44374c')
    if frame==2: # resting: eyes closed, hands down
        line([(23,13),(26,13)],'ink');rect((8,22,12,31),'#d9a58c');rect((29,22,33,31),'#d9a58c')
    elif frame==3:
        rect((7,8,11,22),'#d9a58c');rect((30,6,34,22),'#d9a58c');rect((24,12,25,13),'ink')
    else:
        rect((7,18,11,25),'#d9a58c');rect((29,17+frame,33,24+frame),'#d9a58c');rect((23,12,24,13),'ink')
    im.save(ROOT/('developer'+str(frame)+'.png'))
print('Generated 11 original textures')

# Office v2 replaces the legacy room; character frames remain shared.
import runpy
runpy.run_path(str(Path(__file__).with_name("redesign_office.py")))
