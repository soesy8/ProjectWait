"""Finish exterior corner supports and make third-floor height review unambiguous."""
import bpy,math
from pathlib import Path
from mathutils import Vector
WORK=Path(__file__).resolve().parent
S=bpy.context.scene;Z3=7.92
bpy.context.preferences.filepaths.save_version=0
glass=next(m for m in bpy.data.materials if m.name.startswith('09 ·')).copy()
glass.name='33 · 3층 맑은 통창'
p=glass.node_tree.nodes.get('Principled BSDF');p.inputs['Roughness'].default_value=.015
p.inputs['Metallic'].default_value=0;p.inputs['Base Color'].default_value=(.85,.95,1,1)
for o in S.objects:
    if o.name.startswith('3F · 왼쪽 창 유리'):o.data.materials[0]=glass
    if o.name.startswith('건물 · 수직 구조 기둥') and abs(o.location.x-9.48)<.01:
        o.location.y=-2.8 if o.location.y<0 else 5.45
exterior=bpy.data.objects['전체 · 08 건물 외관'];exterior.location=(39,-47,30)
exterior.rotation_euler=(Vector((0,-7,6))-exterior.location).to_track_quat('-Z','Y').to_euler();exterior.data.lens=40
exterior['검토 렌더 노출 보정']=.75
window=bpy.data.objects['3F · 10 창밖 높이'];window.location=(-6.08,1.2,Z3+1.65)
window.rotation_euler=(Vector((-23,-8,5.2))-window.location).to_track_quat('-Z','Y').to_euler();window.data.lens=21
window['검토 렌더 노출 보정']=.45
bpy.data.objects['전체 · 09 층 구성 단면'].data.ortho_scale=32
S.frame_set(1);S.camera=bpy.data.objects['3F · 02 · 엘리베이터 내부']
S.render.filepath='//3층_02_내부.png'
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(WORK/'엘베_맵_체육센터_3층.blend'),compress=True)
print('REVIEW_VIEWS_SAVED',flush=True)
pref=bpy.context.preferences.addons['cycles'].preferences;pref.compute_device_type='OPTIX';pref.get_devices()
for d in pref.devices:d.use=d.type!='CPU'
S.cycles.device='GPU';S.cycles.samples=64;S.render.resolution_x=1600;S.render.resolution_y=1000;S.render.resolution_percentage=100
base_exposure=S.view_settings.exposure
for cam,frame,name,ev in [
    (window,1,'3층_07_창밖높이.png',.45),
    (bpy.data.objects['3F · 01 · 로비 / 엘리베이터와 우측 계단'],72,'3층_01_로비.png',0),
    (exterior,1,'3층_05_건물외관.png',.75)]:
    S.camera=cam;S.frame_set(frame);S.view_settings.exposure=base_exposure+ev;S.render.filepath=str(WORK/name)
    print('RENDER_START',name,flush=True);bpy.ops.render.render(write_still=True);print('RENDER_DONE',name,flush=True)
S.view_settings.exposure=base_exposure
for o in S.objects:
    base=o.name.removeprefix('3F · ');groups=[c.name for c in o.users_collection]
    if any(n.startswith(('12 ·','32 ·','31 ·','10 · 로비 천장','3F · 10 ·')) for n in groups):o.hide_render=True
    if base in ['오른쪽 계단실 벽','계단실 끝 벽','계단실 높은 천장','뒤쪽 왼쪽 벽','뒤쪽 오른쪽 벽']:o.hide_render=True
    if base.startswith(('Cabin · 천장','Cabin · 환기','계단실 상부 창','계단실 높은 라인','계단실 층 안내','계단 하부 층 안내','계단 벽','계단 진입 낮은','입구 벽 아트','입구 아트')):o.hide_render=True
    if o.name.startswith(('계단 증축부 · 바깥','계단 증축부 · 뒤벽','계단 증축부 · 앞벽','계단 증축부 · 지붕','계단 증축부 · 유리창','계단 증축부 · 창틀','체육관 동 ·','2F · 체육관 동','2F · 외곽 유리','2F · 빈 층 외벽')):o.hide_render=True
bpy.data.objects['전체보기 촬영 바닥'].hide_render=False
S.camera=bpy.data.objects['전체 · 09 층 구성 단면'];S.frame_set(72)
S.render.filepath=str(WORK/'3층_06_층구성단면.png')
print('RENDER_START 3층_06_층구성단면.png',flush=True);bpy.ops.render.render(write_still=True)
print('REVIEW_VIEWS_COMPLETE',flush=True)
