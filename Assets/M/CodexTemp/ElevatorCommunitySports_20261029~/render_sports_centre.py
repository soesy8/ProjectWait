import bpy
from pathlib import Path
WORK=Path(__file__).resolve().parent
S=bpy.context.scene
pref=bpy.context.preferences.addons['cycles'].preferences;pref.compute_device_type='OPTIX';pref.get_devices()
for d in pref.devices:d.use=d.type!='CPU'
S.cycles.device='GPU';S.cycles.samples=64;S.render.resolution_percentage=100
for cam,frame,name in [
    ('02 · 엘리베이터 내부',1,'체육센터_02_내부.png'),
    ('06 · 승강기 식별 정보와 안전 포스터',1,'체육센터_06_식별정보_포스터.png'),
    ('01 · 로비 / 엘리베이터와 우측 계단',72,'체육센터_01_로비.png'),
    ('05 · 전면과 측면 버튼 높이 비교',1,'체육센터_05_버튼패널.png'),
    ('03 · 뒤쪽 자동문 / 왼쪽 창문',72,'체육센터_03_뒤쪽_자동문.png')]:
    S.frame_set(frame);S.camera=bpy.data.objects[cam]
    S.render.resolution_x=1800 if name.startswith('체육센터_06') else 1600
    S.render.resolution_y=1125 if name.startswith('체육센터_06') else 1000
    S.render.filepath=str(WORK/name)
    print('RENDER_START',name,flush=True);bpy.ops.render.render(write_still=True);print('RENDER_DONE',name,flush=True)
S.frame_set(72);S.camera=bpy.data.objects['04 · 전체 배치 / 천장 숨김']
for o in S.objects:
    if any(c.name.startswith(('10','12')) for c in o.users_collection) or o.name in ['오른쪽 계단실 벽','계단실 끝 벽','계단실 높은 천장','뒤쪽 왼쪽 벽','뒤쪽 오른쪽 벽'] or o.name.startswith(('Cabin · 천장','Cabin · 환기','계단실 상부 창','계단실 높은 라인','계단실 층 안내','계단 하부 층 안내','계단 벽','계단 진입 낮은','입구 벽 아트','입구 아트')):o.hide_render=True
bpy.data.objects['전체보기 촬영 바닥'].hide_render=False
S.render.resolution_x=1600;S.render.resolution_y=1000;S.render.filepath=str(WORK/'체육센터_04_전체배치.png')
print('RENDER_START 체육센터_04_전체배치.png',flush=True);bpy.ops.render.render(write_still=True)
print('SPORTS_CENTRE_FINAL_RENDERS_COMPLETE',flush=True)
