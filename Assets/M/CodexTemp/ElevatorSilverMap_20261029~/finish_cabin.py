"""Close the cabin front returns and balance its illumination, then regenerate previews."""
import bpy
from pathlib import Path
WORK=Path(__file__).resolve().parent
S=bpy.context.scene
cabin=next(c for c in bpy.data.collections if c.name.startswith('05'))
for side in [-1,1]:
    bpy.ops.mesh.primitive_cube_add(size=1,location=(-1.8+side*1.31,3.48,1.42))
    o=bpy.context.object;o.name='Cabin · 출입구 내부 측면 마감 '+('왼쪽' if side<0 else '오른쪽')
    o.dimensions=(.42,.19,2.83)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for c in list(o.users_collection):c.objects.unlink(o)
    cabin.objects.link(o);o.data.materials.append(bpy.data.materials['04 · 헤어라인 실버'])
    m=o.modifiers.new('얕은 베벨','BEVEL');m.width=.008;m.segments=3
    o.modifiers.new('면 노멀','WEIGHTED_NORMAL')
for o in S.objects:
    if o.type=='LIGHT' and o.name.startswith('Cabin · 실제 면광원'):o.data.energy=240
S.frame_set(72);S.camera=bpy.data.objects['01 · 로비 / 엘리베이터와 우측 계단']
for o in bpy.context.selected_objects:o.select_set(False)
bpy.ops.wm.save_as_mainfile(filepath=str(WORK/'엘베_맵_실버.blend'),compress=True)
pref=bpy.context.preferences.addons['cycles'].preferences
pref.compute_device_type='OPTIX';pref.get_devices()
for d in pref.devices:d.use=d.type!='CPU'
S.cycles.device='GPU';S.cycles.samples=64;S.render.resolution_percentage=100
for name,frame,out in [('01 · 로비 / 엘리베이터와 우측 계단',72,'01_로비.png'),('02 · 엘리베이터 내부',1,'02_내부.png')]:
    S.camera=bpy.data.objects[name];S.frame_set(frame);S.render.filepath=str(WORK/out)
    print('RENDER_START',out,flush=True);bpy.ops.render.render(write_still=True);print('RENDER_DONE',out,flush=True)
S.frame_set(72);S.camera=bpy.data.objects['04 · 전체 배치 / 천장 숨김']
for o in S.objects:
    if any(c.name.startswith(('10','12')) for c in o.users_collection) or o.name in ['오른쪽 계단실 벽','계단실 끝 벽','계단실 높은 천장','뒤쪽 왼쪽 벽','뒤쪽 오른쪽 벽'] or o.name.startswith(('Cabin · 천장','Cabin · 환기','계단실 상부 창','계단실 높은 라인','계단실 층 안내','계단 하부 층 안내','계단 벽','계단 진입 낮은','입구 벽 아트','입구 아트')):o.hide_render=True
bpy.data.objects['전체보기 촬영 바닥'].hide_render=False
S.render.filepath=str(WORK/'04_전체배치.png')
print('RENDER_START 04_전체배치.png',flush=True);bpy.ops.render.render(write_still=True)
print('CABIN_FINISHED',flush=True)
