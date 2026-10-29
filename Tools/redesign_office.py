"""Original top-down office tiles, v2. Pillow needed only to rebuild art."""
from PIL import Image, ImageDraw
from pathlib import Path
import random
root=Path(__file__).resolve().parents[1]/'Assets/OneMoreBuild/Resources/Art'
W,H=384,176
im=Image.new('RGBA',(W,H),'#8ea59b');d=ImageDraw.Draw(im)
def r(b,c):d.rectangle(b,fill=c)
def l(p,c,w=1):d.line(p,fill=c,width=w)
def desk(x,y):
 r((x,y+24,x+58,y+29),'#929081');r((x+4,y+18,x+8,y+28),'#594e49');r((x+51,y+18,x+55,y+28),'#594e49')
 r((x,y,x+58,y+19),'#514f4b');r((x+1,y+1,x+57,y+16),'#d6b17d');r((x+2,y+2,x+56,y+3),'#f4d8a0');r((x+2,y+17,x+56,y+19),'#9b794f')
 r((x+20,y-10,x+43,y+4),'#414b50');r((x+22,y-8,x+41,y+1),'#a5d8b5');r((x+25,y-6,x+35,y-5),'#4c817b');r((x+25,y-3,x+39,y-2),'#4c817b');r((x+29,y+5,x+34,y+7),'#57565d')
 r((x+18,y+10,x+43,y+13),'#e8e4c8')
 for xx in range(x+20,x+42,3):l([(xx,y+10),(xx,y+12)],'#a5a393')
def plant(x,y):
 r((x-5,y,x+5,y+9),'#945f46');r((x-6,y,x+6,y+2),'#c58c60');l([(x,y),(x,y-15)],'#486b47',2)
 for xx,yy in [(x-9,y-10),(x,y-17),(x-7,y-19),(x+2,y-8)]:
  d.ellipse((xx,yy,xx+9,yy+6),fill='#597f4f');l([(xx+2,yy+2),(xx+6,yy+2)],'#8eae68')
# A tiled floor with deliberate seams, warm north wall, uncluttered work area.
r((4,4,379,171),'#4c605c');r((8,29,375,167),'#d0c9a4')
for y in range(30,168,16):
 for x in range(9,375,16):
  c='#c5bd98' if ((x//16+y//16)%2) else '#d4cba9'
  r((x,y,x+14,min(y+14,167)),c);l([(x,y),(x+14,y)],'#e4ddbb')
r((8,5,375,28),'#dedfc5');r((8,26,375,29),'#7a897d');r((8,5,375,7),'#f0ecd5')
# Windows: original skyline and divided panes.
for x in (36,239):
 r((x,10,x+62,24),'#637975');r((x+2,11,x+60,21),'#a4c7cb')
 for xx in range(x+4,x+59,9):r((xx,17-(xx%3),xx+6,21),'#7c9a9d')
 l([(x+31,11),(x+31,23)],'#edf0d8',2);r((x-2,24,x+64,26),'#f5e6c6')
# Noticeboard / wall clock / poster.
r((138,9,177,25),'#755b40');r((140,11,175,23),'#bc9660')
for x,y,c in [(143,13,'#fff4c8'),(154,12,'#b6cfce'),(164,15,'#ebc1a0')]:r((x,y,x+7,y+6),c)
d.ellipse((195,9,208,22),fill='#f7f0d5',outline='#6b776c');l([(201,12),(201,16),(205,17)],'#5c6a65')
# Meeting zone.
r((37,60,105,109),'#b1aa8e');r((40,58,102,101),'#71644d');r((42,59,100,98),'#ddbd88');r((44,60,98,62),'#f6dcaa')
for x,y in [(27,62),(104,62),(27,85),(104,85)]:
 r((x,y,x+11,y+14),'#526f6c');r((x+2,y+2,x+9,y+10),'#8bafa0')
r((57,73,72,84),'#ece8cf');r((76,69,88,78),'#c4d5cc');r((60,76,69,77),'#95a69c')
# Main workstation, phone, tablet, chair.
desk(170,67);r((181,94,213,115),'#5e746c');r((183,96,211,111),'#93ac8c');r((187,111,207,114),'#456059')
r((220,73,225,78),'#f7f0d2');r((221,72,224,73),'#79573e')
r((157,68,165,82),'#526466');r((159,69,163,74),'#9dbc9d');r((159,77,163,78),'#d9d4b2')
# Spare desk and bookcase makes it an office rather than a wallpaper.
desk(286,65)
r((335,107,362,145),'#705b45');r((337,109,360,142),'#b18b61')
for yy in (111,122,133):
 for j,c in enumerate(('#81978a','#b9896d','#e0c28d','#749ba2')):r((339+j*5,yy,342+j*5,yy+8),c)
# Break corner.
r((29,131,105,157),'#a59c85');r((30,127,103,152),'#587a76');r((33,129,100,147),'#8eaca0');r((31,131,36,151),'#6c9187');r((98,131,103,151),'#6c9187')
r((121,136,141,153),'#846948');r((122,134,140,149),'#d9b77d');r((126,137,135,142),'#ece4c4')
# Delivery crate at the entrance.
r((276,140,298,158),'#866447');r((277,138,297,154),'#c29b62');r((286,138,289,155),'#ecce8e');l([(278,140),(284,140)],'#9d784e')
plant(20,48);plant(361,49);plant(315,145)
r((188,156,238,170),'#a08461');r((191,158,235,169),'#b39b72')
im.save(root/'room.png')
for idx in range(6):
 im=Image.new('RGBA',(W,H));d=ImageDraw.Draw(im)
 if idx==0:
  r((210,53,225,69),'#384f52');r((213,56,223,57),'#9bcab3');r((213,61,223,62),'#9bcab3')
 elif idx==1:
  r((179,92,215,116),'#3f595d');r((182,94,212,111),'#ba8061');r((184,95,210,97),'#e7b58b')
 elif idx==2:
  r((144,48,166,62),'#4c6266');r((146,50,164,60),'#a7d7ae');r((149,53,153,56),'#558b73');r((157,53,161,56),'#558b73')
 elif idx==3:plant(117,128);r((230,48,233,68),'#6d765b');r((224,45,239,50),'#f2d595')
 elif idx==4:
  r((173,76,188,83),'#536a73');r((175,77,185,81),'#a6c3b9');l([(186,73),(190,79)],'#57596c')
 else:
  r((187,51,218,72),'#334952');r((189,53,216,68),'#92c5c0');r((191,55,214,66),'#50777f');r((194,58,209,59),'#def1cf');r((194,62,205,63),'#def1cf')
 im.save(root/('upgrade'+str(idx)+'.png'))
print('Office tiles redrawn: 384x176')
