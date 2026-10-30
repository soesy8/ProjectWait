import bpy
from pathlib import Path
WORK=Path(__file__).resolve().parent
S=bpy.context.scene
pref=bpy.context.preferences.addons['cycles'].preferences;pref.compute_device_type='OPTIX';pref.get_devices()
for d in pref.devices:d.use=d.type!='CPU'
S.cycles.device='GPU';S.cycles.samples=64
S.render.resolution_percentage=100;S.render.resolution_x=1600;S.render.resolution_y=1000
for cam,frame,name in [
    ('3F · 01 · 로비 / 엘리베이터와 우측 계단',72,'3층_01_로비.png'),
    ('3F · 02 · 엘리베이터 내부',1,'3층_02_내부.png'),
    ('3F · 06 · 승강기 식별 정보와 안전 포스터',1,'3층_03_층별시설안내.png'),
    ('3F · 07 농구장과 복도',1,'3층_04_농구장.png'),
    ('전체 · 08 건물 외관',1,'3층_05_건물외관.png'),
    ('3F · 10 창밖 높이',1,'3층_07_창밖높이.png')]:
    S.camera=bpy.data.objects[cam];S.frame_set(frame);S.render.filepath=str(WORK/name)
    print('RENDER_START',name,flush=True);bpy.ops.render.render(write_still=True);print('RENDER_DONE',name,flush=True)
# Section is a render-only visibility setup; the saved file keeps the complete envelope.
for o in S.objects:
    base=o.name.removeprefix('3F · ')
    groups=[c.name for c in o.users_collection]
    if any(n.startswith(('12 ·','32 ·','31 ·','10 · 로비 천장','3F · 10 ·')) for n in groups):o.hide_render=True
    if base in ['오른쪽 계단실 벽','계단실 끝 벽','계단실 높은 천장','뒤쪽 왼쪽 벽','뒤쪽 오른쪽 벽']:o.hide_render=True
    if base.startswith(('Cabin · 천장','Cabin · 환기','계단실 상부 창','계단실 높은 라인','계단실 층 안내','계단 하부 층 안내','계단 벽','계단 진입 낮은','입구 벽 아트','입구 아트')):o.hide_render=True
    if o.name.startswith(('계단 증축부 · 바깥','계단 증축부 · 뒤벽','계단 증축부 · 앞벽','계단 증축부 · 지붕','계단 증축부 · 유리창','계단 증축부 · 창틀','체육관 동 ·','2F · 체육관 동','2F · 외곽 유리','2F · 빈 층 외벽')):o.hide_render=True
bpy.data.objects['전체보기 촬영 바닥'].hide_render=False
S.camera=bpy.data.objects['전체 · 09 층 구성 단면'];S.frame_set(72)
S.render.filepath=str(WORK/'3층_06_층구성단면.png')
print('RENDER_START 3층_06_층구성단면.png',flush=True);bpy.ops.render.render(write_still=True)
print('THREE_FLOOR_FINAL_RENDERS_COMPLETE',flush=True)
