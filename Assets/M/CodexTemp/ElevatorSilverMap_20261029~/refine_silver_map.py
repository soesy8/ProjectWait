import bpy
import math
import random
from pathlib import Path
from mathutils import Vector
WORK=Path(__file__).resolve().parent
S=bpy.context.scene
S.view_settings.exposure=-1.05
hero=bpy.data.objects['01 · 로비 / 엘리베이터와 우측 계단']
hero.location=(5.55,-4.8,2.15)
hero.rotation_euler=(Vector((-.4,1.6,1.55))-hero.location).to_track_quat('-Z','Y').to_euler()
hero.data.lens=19

def box(name,loc,size,mat,collection='outside',bev=.012):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=bpy.context.object;o.name=name;o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for c in list(o.users_collection):c.objects.unlink(o)
    col=next(c for c in bpy.data.collections if c.name.startswith({'outside':'12','doors':'06','helpers':'15'}[collection]))
    col.objects.link(o);o.data.materials.append(bpy.data.materials[mat])
    if bev:
        m=o.modifiers.new('모서리 베벨','BEVEL');m.width=bev;m.segments=3
        o.modifiers.new('면 노멀','WEIGHTED_NORMAL')
    return o
# Add a shallow courtyard so the open rear door has a modelled destination.
box('진입부 외부 중정 바닥',(-.6,-9.1,-.06),(10.4,7.3,.12),'01 · 밝은 회색 석재')
for y in [-7,-8.2,-9.4,-10.6,-11.8]:
    box('중정 석재 줄눈',(-.6,y,.006),(10.3,.013,.007),'05 · 차콜 패널',bev=.001)
for x in [-4.5,-2.5,-.5,1.5,3.5]:
    box('중정 석재 세로 줄눈',(x,-9.1,.006),(.013,7.2,.007),'05 · 차콜 패널',bev=.001)
box('중정 뒤쪽 낮은 화단',(-.6,-11.9,.31),(10.4,.75,.62),'05 · 차콜 패널',bev=.04)
box('중정 화단 실버 마감',(-.6,-11.9,.65),(10.42,.78,.06),'03 · 새틴 스테인리스')
random.seed(429)
outside=next(c for c in bpy.data.collections if c.name.startswith('12'))
for i in range(18):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,location=(-5.3+i*.55,-11.9,.83+random.random()*.17))
    o=bpy.context.object;o.name='중정 조경 관목';o.scale=(.4,.32,.35+random.random()*.16)
    for c in list(o.users_collection):c.objects.unlink(o)
    outside.objects.link(o);o.data.materials.append(bpy.data.materials['14 · 세이지 잎'])
    for p in o.data.polygons:p.use_smooth=True
box('중정 맞은편 건물 외벽',(-.6,-15,3),(22,.18,6),'02 · 라이트 그레이 벽 패널')
for z in [.6,1.8,3,4.2,5.4]:
    box('중정 맞은편 건물 수평 패널',(-.6,-14.895,z),(22,.02,.012),'01 · 밝은 회색 석재',bev=.001)
for x in [-7,-3.8,2.6,5.8]:
    box('중정 맞은편 건물 창틀',(x,-14.87,3.15),(2.0,.04,2.55),'05 · 차콜 패널')
    box('중정 맞은편 건물 유리',(x,-14.83,3.15),(1.85,.025,2.4),'10 · 반투명 안전 띠')
# Door safety text follows the primary leaves, with correct facing into the cabin.
S.frame_set(1)
font=next(f for f in bpy.data.fonts if 'Malgun' in f.name)
for side,name,body in [(-1,'왼쪽','손 끼임 주의'),(1,'오른쪽','기대지 마세요')]:
    leaf=bpy.data.objects['Lift 1 · %s 문 [슬라이드]'%name]
    cr=bpy.data.curves.new('문 안전 안내','FONT');cr.body=body;cr.font=font;cr.size=.065;cr.align_x='CENTER';cr.align_y='CENTER'
    cr.materials.append(bpy.data.materials['05 · 차콜 패널'])
    o=bpy.data.objects.new('문 내부 안전 안내 · '+name,cr)
    next(c for c in bpy.data.collections if c.name.startswith('06')).objects.link(o)
    o.parent=leaf;o.location=(0,.043,.43);o.rotation_euler=(math.pi/2,0,math.pi)
    # Local coordinates: leaf mesh is centred at z 1.405, text is at z 1.835.
stage=box('전체보기 촬영 바닥',(0,0,-.43),(200,200,.1),'02 · 라이트 그레이 벽 패널','helpers',bev=0)
stage.hide_render=True;stage.hide_set(True)
S.camera=hero;S.frame_set(72)
S.render.resolution_percentage=100;S.cycles.samples=64
for o in bpy.context.selected_objects:o.select_set(False)
S.render.filepath=str(WORK/'01_로비.png')
bpy.ops.wm.save_as_mainfile(filepath=str(WORK/'엘베_맵_실버.blend'),compress=True)

# Configure only this rendering process; no preference-file writes.
pref=bpy.context.preferences.addons['cycles'].preferences
pref.compute_device_type='OPTIX';pref.get_devices()
for d in pref.devices:d.use=d.type!='CPU'
S.cycles.device='GPU';S.cycles.samples=24;S.render.resolution_percentage=70
S.render.filepath=str(WORK/'01_로비_refined.png')
bpy.ops.render.render(write_still=True)

# A roofless overview for spatial QA, without changing saved visibility.
S.camera=bpy.data.objects['04 · 전체 배치 / 천장 숨김']
hidden=[]
for o in S.objects:
    if any(c.name.startswith(('10','12')) for c in o.users_collection) or o.name in ['오른쪽 계단실 벽','계단실 끝 벽','계단실 높은 천장','뒤쪽 왼쪽 벽','뒤쪽 오른쪽 벽'] or o.name.startswith(('Cabin · 천장','Cabin · 환기')):
        if not o.hide_render:hidden.append(o);o.hide_render=True
stage.hide_render=False
S.render.filepath=str(WORK/'04_전체배치_draft.png')
bpy.ops.render.render(write_still=True)
print('REFINEMENT_DRAFTS_COMPLETE',flush=True)
