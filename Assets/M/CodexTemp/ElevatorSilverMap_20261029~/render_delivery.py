"""Render the approved-scope latest geometry without modifying the saved model."""
import bpy
from pathlib import Path
WORK=Path(__file__).resolve().parent
S=bpy.context.scene
pref=bpy.context.preferences.addons['cycles'].preferences;pref.compute_device_type='OPTIX';pref.get_devices()
for d in pref.devices:d.use=d.type!='CPU'
S.cycles.device='GPU';S.cycles.samples=64;S.render.resolution_percentage=100
for cam,fr,out in [
    ('05 · 전면과 측면 버튼 높이 비교',1,'05_버튼패널_높이비교.png'),
    ('02 · 엘리베이터 내부',1,'02_내부.png'),
    ('01 · 로비 / 엘리베이터와 우측 계단',72,'01_로비.png'),
]:
    S.frame_set(fr);S.camera=bpy.data.objects[cam];S.render.filepath=str(WORK/out)
    print('RENDER_START',out,flush=True);bpy.ops.render.render(write_still=True);print('RENDER_DONE',out,flush=True)
S.frame_set(72);S.camera=bpy.data.objects['04 · 전체 배치 / 천장 숨김']
for o in S.objects:
    if any(c.name.startswith(('10','12')) for c in o.users_collection) or o.name in ['오른쪽 계단실 벽','계단실 끝 벽','계단실 높은 천장','뒤쪽 왼쪽 벽','뒤쪽 오른쪽 벽'] or o.name.startswith(('Cabin · 천장','Cabin · 환기','계단실 상부 창','계단실 높은 라인','계단실 층 안내','계단 하부 층 안내','계단 벽','계단 진입 낮은','입구 벽 아트','입구 아트')):o.hide_render=True
bpy.data.objects['전체보기 촬영 바닥'].hide_render=False
S.render.filepath=str(WORK/'04_전체배치.png')
print('RENDER_START 04_전체배치.png',flush=True);bpy.ops.render.render(write_still=True)
print('DELIVERY_RENDERS_COMPLETE',flush=True)
