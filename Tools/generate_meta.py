"""Stable Unity GUIDs. Only creates missing metadata; safe to rerun."""
from pathlib import Path
import uuid
ROOT=Path(__file__).resolve().parents[1]
def guid(path):return uuid.uuid5(uuid.NAMESPACE_URL,'https://github.com/Xelyphor/devgame/'+path).hex
for p in sorted((ROOT/'Assets').rglob('*')):
    if p.suffix=='.meta':continue
    rel=p.relative_to(ROOT).as_posix();meta=Path(str(p)+'.meta')
    if meta.exists():continue
    text='fileFormatVersion: 2\nguid: '+guid(rel)+'\n'
    if p.is_dir():text+='folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n'
    elif p.suffix=='.cs':text+='MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n'
    elif p.suffix=='.png':text+='TextureImporter:\n  externalObjects: {}\n  serializedVersion: 13\n  mipmaps:\n    enableMipMap: 0\n  isReadable: 0\n  textureSettings:\n    serializedVersion: 2\n    filterMode: 0\n    aniso: 0\n    mipBias: 0\n    wrapU: 1\n    wrapV: 1\n    wrapW: 1\n  maxTextureSize: 2048\n  textureFormat: 1\n  textureCompression: 0\n  alphaIsTransparency: 1\n  textureType: 0\n  npotScale: 0\n  sRGBTexture: 1\n'
    elif p.suffix=='.ttf':text+='TrueTypeFontImporter:\n  externalObjects: {}\n  serializedVersion: 4\n  fontSize: 16\n  forceTextureCase: -2\n  characterSpacing: 0\n  characterPadding: 1\n  includeFontData: 1\n  fontName: DejaVu Sans\n  fontNames:\n  - DejaVu Sans\n  fallbackFontReferences: []\n  useLegacyBoundsCalculation: 0\n  shouldRoundAdvanceValue: 1\n'
    elif p.suffix=='.asmdef':text+='AssemblyDefinitionImporter:\n  externalObjects: {}\n'
    else:text+='DefaultImporter:\n  externalObjects: {}\n'
    if p.suffix=='.png':
        text=text.replace('  textureCompression: 0\n','')
        text+='  alphaUsage: 1\n  platformSettings:\n  - serializedVersion: 3\n    buildTarget: DefaultTexturePlatform\n    maxTextureSize: 2048\n    resizeAlgorithm: 0\n    textureFormat: -1\n    textureCompression: 0\n    compressionQuality: 100\n    crunchedCompression: 0\n    allowsAlphaSplitting: 0\n    overridden: 0\n    ignorePlatformSupport: 0\n    androidETC2FallbackOverride: 0\n    forceMaximumCompressionQuality_BC6H_BC7: 0\n'
    meta.write_text(text)
scene='Assets/OneMoreBuild/Scenes/Studio.unity'
(ROOT/'ProjectSettings/EditorBuildSettings.asset').write_text('''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1045 &1
EditorBuildSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Scenes:
  - enabled: 1
    path: '''+scene+'\n    guid: '+guid(scene)+'\n  m_configObjects: {}\n')
print('Metadata ready')
