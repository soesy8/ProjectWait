import bpy
from pathlib import Path
WORK=Path(__file__).resolve().parent
S=bpy.context.scene
bpy.context.preferences.filepaths.save_version=0
ground=bpy.data.objects['창밖 지면'];ground.location=(-8,-12,-.72);ground.dimensions=(100,130,.18)
guideink=next(m for m in bpy.data.materials if m.name.startswith('26 ·'))
for o in S.objects:
    if o.type=='FONT' and o.name.startswith(('3F · 안내 ','3F · 시설 안내 제목','3F · 현위치 표기')):o.data.materials[0]=guideink
floorlabel=bpy.data.objects.get('3F · 안내 층 12–6F')
if floorlabel:floorlabel.data.body='6–12F'
regular=next(f for f in bpy.data.fonts if 'Malgun' in f.name and 'Bold' not in f.name)
for name in ['3F · 안내 층 3F','3F · 안내 시설 3F']:bpy.data.objects[name].data.font=regular
for name in ['3F · 현재층 안내 강조','3F · 현위치 표기']:
    obj=bpy.data.objects.get(name)
    if obj:bpy.data.objects.remove(obj,do_unlink=True)
struct=next(c for c in bpy.data.collections if c.name.startswith('40 ·'))
if not bpy.data.objects.get('계단 증축부 · 지상 기초'):
    bpy.ops.mesh.primitive_cube_add(size=1,location=(8.05,1.33,-.15))
    o=bpy.context.object;o.name='계단 증축부 · 지상 기초';o.dimensions=(3.2,8.5,.3)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for c in list(o.users_collection):c.objects.unlink(o)
    struct.objects.link(o);o.data.materials.append(next(m for m in bpy.data.materials if m.name.startswith('05 ·')))
S['층별 안내 스타일']='모든 층 동일한 색과 굵기. 현재 위치 강조와 별도 현위치 문구 없음.'
readme=bpy.data.texts['읽어주세요.txt']
body=readme.as_string().replace('포스터 옆에 층별 시설 안내가 있으며 3층을 표시했습니다.','포스터 옆에 층별 시설 안내가 있으며 모든 층을 같은 색과 굵기로 표시합니다.')
readme.clear();readme.write(body);(WORK/'읽어주세요_3층.txt').write_text(body,encoding='utf-8')
for o in bpy.context.selected_objects:o.select_set(False)
S.frame_set(1);S.camera=bpy.data.objects['3F · 02 · 엘리베이터 내부']
S.render.filepath='//3층_02_내부.png'
bpy.context.view_layer.update();bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(WORK/'엘베_맵_체육센터_3층.blend'),compress=True)
print('THREE_FLOORS_FINISHED',flush=True)
