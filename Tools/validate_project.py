"""Static project and font checks; not a substitute for opening Unity."""
from pathlib import Path
import json,re
from PIL import Image
from fontTools.ttLib import TTFont
ROOT=Path(__file__).resolve().parents[1]
count=0
def check(value,message):
    global count
    count+=1
    if not value:raise AssertionError(message)
for p in list((ROOT/'Assets').rglob('*.asmdef'))+[ROOT/'Packages/manifest.json']:
    json.loads(p.read_text());count+=1
guids={}
for p in (ROOT/'Assets').rglob('*'):
    if p.suffix=='.meta':continue
    meta=Path(str(p)+'.meta');check(meta.exists(),'Missing meta: '+str(p))
    guid=re.search(r'^guid: ([0-9a-f]{32})$',meta.read_text(),re.M)
    check(guid is not None,'Invalid GUID: '+str(meta));g=guid.group(1)
    check(g not in guids,'Duplicate GUID: '+str(meta));guids[g]=p
    if p.suffix=='.png':
        im=Image.open(p);check(im.size in [(384,216),(40,48)],'Unexpected texture size')
        text=meta.read_text();check('filterMode: 0' in text and 'enableMipMap: 0' in text,'Pixel import settings')
for name in ['room']+['upgrade'+str(i) for i in range(6)]+['developer'+str(i) for i in range(4)]:
    check((ROOT/'Assets/OneMoreBuild/Resources/Art'/str(name+'.png')).exists(),'Missing resource '+name)
font=TTFont(ROOT/'Assets/OneMoreBuild/Resources/Fonts/DejaVuSans.ttf')
cmap=font.getBestCmap()
text=''.join(p.read_text() for p in (ROOT/'Assets/OneMoreBuild/Scripts').glob('*.cs'))
missing={c for c in text if ord(c)>127 and ord(c) not in cmap}
check(not missing,'Font missing: '+repr(missing))
scene=ROOT/'Assets/OneMoreBuild/Scenes/Studio.unity'
scene_guid=re.search(r'guid: (\w+)',Path(str(scene)+'.meta').read_text()).group(1)
check(scene_guid in (ROOT/'ProjectSettings/EditorBuildSettings.asset').read_text(),'Scene absent from build settings')
check('6000.6.7f1' in (ROOT/'ProjectSettings/ProjectVersion.txt').read_text(),'Unity version')
check(not list((ROOT/'Assets').rglob('*.csproj')),'Tests leaked into Unity assets')
print('PASS:',count,'static checks. Cyrillic and UI symbols present in bundled font.')
