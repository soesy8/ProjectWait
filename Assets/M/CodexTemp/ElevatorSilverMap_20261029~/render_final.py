import bpy
import math
from pathlib import Path
from mathutils import Vector
WORK=Path(__file__).resolve().parent
S=bpy.context.scene
# Keep the actual LED geometry visible; hide lighting rigs from camera and reflections.
for o in S.objects:
    if o.type=='LIGHT' and o.data.type=='AREA':
        o.visible_camera=False;o.visible_glossy=False;o.visible_transmission=False
bpy.data.objects['좌측 통창 자연광'].location.x=-7.1
# Open the upper landing's inner rail towards its side corridor and guard its edges.
upper=next(c for c in bpy.data.collections if c.name.startswith('11'))
for o in list(upper.objects):
    if o.name.startswith('상부 난간') and abs(o.location.x-3.87)<.01:
        if o.name.startswith('상부 난간 기둥') and o.location.y>4.6:
            bpy.data.objects.remove(o,do_unlink=True)
        elif o.name.startswith(('상부 난간 손잡이','상부 난간 하부')):
            o.location.y=(3.63+4.46)/2;o.scale.z=(4.46-3.63)/(5.25-3.63)
def rail(name,a,b,r):
    a,b=Vector(a),Vector(b)
    bpy.ops.mesh.primitive_cylinder_add(vertices=20,radius=r,depth=(b-a).length,location=(a+b)/2)
    o=bpy.context.object;o.name=name;o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    for c in list(o.users_collection):c.objects.unlink(o)
    upper.objects.link(o);o.data.materials.append(bpy.data.materials['08 · 손잡이 크롬'])
    for p in o.data.polygons:p.use_smooth=True
for y in [4.5,5.34]:
    for x in [.52,1.63,2.74,3.86]:rail('상층 복도 난간 기둥',(x,y,3.43),(x,y,4.46),.022)
    for z,r in [(4.46,.032),(3.87,.016)]:rail('상층 복도 연속 손잡이',(.52,y,z),(3.86,y,z),r)
for z,r in [(4.46,.032),(3.87,.016)]:rail('상층 복도 끝 난간',(.52,4.5,z),(.52,5.34,z),r)
pref=bpy.context.preferences.addons['cycles'].preferences
pref.compute_device_type='OPTIX';pref.get_devices()
for d in pref.devices:d.use=d.type!='CPU'
S.cycles.device='GPU';S.cycles.samples=64
S.render.resolution_percentage=100
S.camera=bpy.data.objects['01 · 로비 / 엘리베이터와 우측 계단'];S.frame_set(72)
S.render.filepath='//01_로비.png'
readme=bpy.data.texts['읽어주세요.txt']
body=readme.as_string().replace('상부 난간 일부, 우측·뒤쪽 계단실 벽을 잠시 숨겨 내부 배치를 보여줬습니다.','우측·뒤쪽 계단실 벽과 뒤쪽 로비 벽을 잠시 숨겨 내부 배치를 보여줬습니다.')
body=body.replace('전체 배치 이미지 촬영 때는 컬렉션 10(로비 천장)과 11의 높은 천장 및\n우측','전체 배치 이미지 촬영 때는 로비·계단실·엘리베이터 천장, 창밖 배경,\n우측')
body+='\n미리보기: 01 로비(72프레임), 02 내부(1프레임/문 닫힘),\n03 뒤쪽 자동문(72프레임), 04 전체 배치(72프레임/일부 벽과 천장 숨김).\n'
readme.clear();readme.write(body)
(WORK/'읽어주세요.txt').write_text(body,encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(WORK/'엘베_맵_실버.blend'),compress=True)
for cam,frame,name in [
    ('01 · 로비 / 엘리베이터와 우측 계단',72,'01_로비.png'),
    ('02 · 엘리베이터 내부',1,'02_내부.png'),
    ('03 · 뒤쪽 자동문 / 왼쪽 창문',72,'03_뒤쪽_자동문.png'),
]:
    S.camera=bpy.data.objects[cam];S.frame_set(frame);S.render.filepath=str(WORK/name)
    print('RENDER_START',name,flush=True)
    bpy.ops.render.render(write_still=True)
    print('RENDER_DONE',name,flush=True)
S.camera=bpy.data.objects['04 · 전체 배치 / 천장 숨김'];S.frame_set(72)
for o in S.objects:
    if any(c.name.startswith(('10','12')) for c in o.users_collection) or o.name in ['오른쪽 계단실 벽','계단실 끝 벽','계단실 높은 천장','뒤쪽 왼쪽 벽','뒤쪽 오른쪽 벽'] or o.name.startswith(('Cabin · 천장','Cabin · 환기','계단실 상부 창','계단실 높은 라인','계단실 층 안내','계단 하부 층 안내','계단 벽','계단 진입 낮은','입구 벽 아트','입구 아트')):
        o.hide_render=True
stage=bpy.data.objects['전체보기 촬영 바닥'];stage.hide_render=False
S.render.filepath=str(WORK/'04_전체배치.png')
print('RENDER_START 04_전체배치.png',flush=True)
bpy.ops.render.render(write_still=True)
print('FINAL_RENDERS_COMPLETE',flush=True)
