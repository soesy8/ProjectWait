"""Reference layout: front panel on image right, side panel on image left."""
import bpy
from pathlib import Path
from mathutils import Vector
WORK=Path(__file__).resolve().parent
# Reuse only construction helpers, before that script's first model mutation.
helper_source=(WORK/'raise_and_expand_controls.py').read_text(encoding='utf-8').split('# Raise every piece')[0]
exec(compile(helper_source,str(WORK/'raise_and_expand_controls.py'),'exec'))
S.frame_set(1)
# From the back of the cabin looking at the door, negative X is image right.
for o in CAB.objects:
    if o.name.startswith('Cabin · 전면') or o.name=='Cabin · 용량 표기':o.location.x-=2.42

prefixes=('Cabin · 측면 전체 패널','Cabin · 측면 패널 구획선','Cabin · 측면 층 선택 제목','Cabin · 측면 비상','Cabin · 측면 문 개폐','Cabin · 측면 개폐')
for o in list(CAB.objects):
    if o.name.startswith(prefixes) or (o.name.startswith('Cabin · 측면 ') and any(s in o.name for s in ['층 링','층 면','층 숫자'])):bpy.data.objects.remove(o,do_unlink=True)

# Compact horizontal side console, closer to the front so it is visible from behind.
wx=-.345;py=4.25
panel=box('Cabin · 측면 전체 패널 실버 테두리',(wx,py,1.42),(.065,1.05,.60),'silver',.024)
panel['용도']='손잡이 바로 위의 가로형 층 선택 및 문 개폐, 낮은 비상 호출'
panel['패널 하단 높이_m']=1.12
box('Cabin · 측면 전체 패널 차콜 면',(wx-.043,py,1.42),(.026,1.009,.558),'charcoal',.018)
box('Cabin · 측면 패널 구획선',(wx-.060,4.13,1.42),(.006,.008,.47),'silver',.002)
text('Cabin · 측면 층 선택 제목','층 선택',(wx-.065,py+.17,1.665),.042)
for floor in range(1,13):
    i=floor-1;row=i//4;col=i%4;yy=py+.35-col*.12;z=1.25+row*.15
    cyl('Cabin · 측면 %02d층 링'%floor,(wx-.071,yy,z),.044,.024,'active' if floor==2 else 'rail')
    cyl('Cabin · 측면 %02d층 면'%floor,(wx-.088,yy,z),.033,.014,'black')
    text('Cabin · 측면 %02d층 숫자'%floor,str(floor),(wx-.102,yy,z),.038,lf=True)
ey=py-.34;ez=1.28
text('Cabin · 측면 비상 호출 안내','비상 호출',(wx-.066,ey,1.425),.042)
cyl('Cabin · 측면 비상 호출 링',(wx-.071,ey,ez),.060,.024,'rail')
call=cyl('Cabin · 측면 비상 버튼 [높이 1.28m]',(wx-.088,ey,ez),.048,.019,'amber')
call['버튼 중심 높이_m']=ez
text('Cabin · 측면 비상 버튼 문자','비상',(wx-.102,ey,ez),.032,'charcoal')
for yy,outward,label in [(ey+.11,True,'열림'),(ey-.11,False,'닫힘')]:
    cyl('Cabin · 측면 문 개폐 링',(wx-.071,yy,1.55),.044,.024,'rail')
    cyl('Cabin · 측면 문 개폐 면',(wx-.088,yy,1.55),.033,.015,'black')
    arrows('Cabin · 측면 개폐 삼각 아이콘',(wx-.102,yy,1.55),outward)
    text('Cabin · 측면 개폐 안내',label,(wx-.067,yy,1.665),.032)

# A straight interior view with the entire side console inside the image frame.
cam=bpy.data.objects['02 · 엘리베이터 내부']
cam.location=(-1.8,6.22,1.65)
cam.rotation_euler=(Vector((-1.8,3.3,1.5))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=17
compare=bpy.data.objects['05 · 전면과 측면 버튼 높이 비교']
compare.location=(-2.45,6.2,1.6)
compare.rotation_euler=(Vector((-1.7,3.6,1.45))-compare.location).to_track_quat('-Z','Y').to_euler();compare.data.lens=18.5
S['버튼 패널 수정']='참고처럼 정면에서 문 오른쪽 전면 패널 / 왼쪽 벽 가로형 측면 패널 / 전면 3열4행, 측면 4열3행 / 양쪽 모두 열림·닫힘'
S['기본 카메라']='02 · 엘리베이터 내부 / 1프레임 문 닫힘'
readme=bpy.data.texts['읽어주세요.txt'];body=readme.as_string().split('\n버튼 패널 개선:')[0].split('\n버튼 패널 개선 (최종):')[0]
body=body.replace('기본 72프레임은 문이 열린 모습입니다.','72프레임은 문이 열린 모습입니다. 파일은 1프레임의 내부 카메라로 열립니다.')
body+='''\n버튼 패널 개선 (최종 참고 반영):\n내부 정면에서 볼 때 문 오른쪽에 높은 전면 패널, 왼쪽 벽에 가로형 측면 패널이 있습니다.\n주 엘리베이터 문 폭 1.8 m, 전면 패널 폭 0.51 m입니다.\n전면 패널 전체를 20 cm 높였고 전면 비상 버튼 중심은 2.15 m입니다.\n측면 패널은 손잡이(중심 1.02 m) 바로 위에 있으며 하단 높이는 1.12 m입니다.\n전면은 12개 층 버튼 3열·4행, 측면은 12개 층 버튼 4열·3행입니다.\n양쪽 모두 문 열림·닫힘 버튼이 있습니다. 측면 비상 버튼 중심은 1.28 m입니다.\n02 내부 정면 및 05 버튼패널 높이 비교 카메라에서 양쪽 패널을 볼 수 있습니다.\n'''
readme.clear();readme.write(body);(WORK/'읽어주세요.txt').write_text(body,encoding='utf-8')
S.camera=cam;S.frame_set(1);S.render.filepath='//02_내부.png'
for o in bpy.context.selected_objects:o.select_set(False)
bpy.ops.wm.save_as_mainfile(filepath=str(WORK/'엘베_맵_실버.blend'),compress=True)
print('REFERENCE_CONTROLS_SAVED',flush=True)
pref=bpy.context.preferences.addons['cycles'].preferences;pref.compute_device_type='OPTIX';pref.get_devices()
for d in pref.devices:d.use=d.type!='CPU'
S.cycles.device='GPU';S.cycles.samples=24;S.render.resolution_percentage=70
S.render.filepath=str(WORK/'02_내부_참고반영_draft.png')
bpy.ops.render.render(write_still=True)
S.camera=compare;S.render.filepath=str(WORK/'05_패널_참고반영_draft.png')
bpy.ops.render.render(write_still=True)
print('REFERENCE_CONTROLS_DRAFT_DONE',flush=True)
