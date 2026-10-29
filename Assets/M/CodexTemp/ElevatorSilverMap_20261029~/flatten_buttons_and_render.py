"""Final requested shallow buttons, then final high-quality views."""
import bpy
from pathlib import Path
WORK=Path(__file__).resolve().parent
S=bpy.context.scene
def depth(o,value):
    # Cylinders were created along local Z, then rotated onto their panel plane.
    span=max(v.co.z for v in o.data.vertices)-min(v.co.z for v in o.data.vertices)
    for v in o.data.vertices:v.co.z*=value/span
    for m in o.modifiers:
        if m.type=='BEVEL':m.width=.0015
for o in S.objects:
    n=o.name
    if n.startswith('Cabin · 측면 '):
        if o.type=='MESH' and ('층 링' in n or '비상 호출 링' in n or '문 개폐 링' in n):
            depth(o,.008);o.location.x=-.406
        elif o.type=='MESH' and ('층 면' in n or '비상 버튼 [' in n or '문 개폐 면' in n):
            depth(o,.006);o.location.x=-.412
        elif o.type=='FONT' and ('층 숫자' in n or '비상 버튼 문자' in n):o.location.x=-.418
        elif o.type=='MESH' and '개폐 삼각 아이콘' in n:
            for v in o.data.vertices:v.co.x+=.029
    elif n.startswith('Cabin · 전면 '):
        if o.type=='MESH' and ('층 링' in n or '비상 호출 링' in n):
            depth(o,.008);o.location.y=3.683
        elif o.type=='MESH' and ('층 면' in n or '비상 버튼 [' in n):
            depth(o,.006);o.location.y=3.689
        elif o.type=='MESH' and n.startswith('Cabin · 전면 문 개폐'):
            depth(o,.008);o.location.y=3.685
        elif o.type=='FONT' and ('층 숫자' in n or '비상 버튼 문자' in n):o.location.y=3.695
        elif o.type=='MESH' and '개폐 삼각 아이콘' in n:
            for v in o.data.vertices:v.co.y-=.030
S['버튼 돌출']='링 두께 8 mm, 버튼 면 두께 6 mm / 패널에서 약 1 cm 돌출'
readme=bpy.data.texts['읽어주세요.txt'];body=readme.as_string()+'\n버튼은 링 두께 8 mm, 버튼 면 두께 6 mm로 얕게 만들었습니다.\n'
readme.clear();readme.write(body);(WORK/'읽어주세요.txt').write_text(body,encoding='utf-8')
S.frame_set(1);S.camera=bpy.data.objects['02 · 엘리베이터 내부'];S.render.filepath='//02_내부.png'
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(WORK/'엘베_맵_실버.blend'),compress=True)
print('SHALLOW_BUTTONS_SAVED',flush=True)
pref=bpy.context.preferences.addons['cycles'].preferences;pref.compute_device_type='OPTIX';pref.get_devices()
for d in pref.devices:d.use=d.type!='CPU'
S.cycles.device='GPU';S.cycles.samples=64;S.render.resolution_percentage=100
for cam,fr,out in [
    ('02 · 엘리베이터 내부',1,'02_내부.png'),
    ('05 · 전면과 측면 버튼 높이 비교',1,'05_버튼패널_높이비교.png'),
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
print('FINAL_DELIVERY_READY',flush=True)
