"""Latest saved user model -> coherent sports-centre setting, reduced copy, real poster."""
import bpy
import math
import json
from pathlib import Path
from mathutils import Vector
WORK=Path(__file__).resolve().parent
S=bpy.context.scene
S.frame_set(1)
bpy.context.preferences.filepaths.save_version=0
OUTPUT=WORK/'엘베_맵_체육센터.blend'
COL={key:next(c for c in bpy.data.collections if c.name.startswith(prefix)) for key,prefix in [('cabin','05'),('furniture','09'),('camera','14')]}
M={key:next(m for m in bpy.data.materials if m.name.startswith(prefix)) for key,prefix in [('silver','03'),('charcoal','05'),('white','07'),('rail','08'),('active','19')]}
font=next(f for f in bpy.data.fonts if 'Malgun' in f.name)
latin=next(f for f in bpy.data.fonts if 'Segoe' in f.name)

def box(name,loc,size,mat,bev=.008,col='cabin'):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=bpy.context.object;o.name=name;o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for c in list(o.users_collection):c.objects.unlink(o)
    COL[col].objects.link(o);o.data.materials.append(M[mat])
    m=o.modifiers.new('얕은 베벨','BEVEL');m.width=bev;m.segments=3
    o.modifiers.new('면 노멀','WEIGHTED_NORMAL');return o
def text(name,body,loc,size,mat='charcoal',side='front',col='cabin',lf=False):
    d=bpy.data.curves.new(name,'FONT');d.body=body;d.size=size;d.font=latin if lf else font
    d.align_x='CENTER';d.align_y='CENTER';d.extrude=.0003;d.bevel_depth=.0001
    d.materials.append(M[mat]);o=bpy.data.objects.new(name,d);COL[col].objects.link(o);o.location=loc
    o.rotation_euler=(math.pi/2,0,math.pi if side=='back' else 0)
    return o
def set_text(name,body):
    o=bpy.data.objects.get(name)
    if o is not None and o.type=='FONT':o.data.body=body
    return o

# Deliberately retain floor numbers, controls and safety/identity information only.
remove_prefixes=(
    'Cabin · 안내 제목','Cabin · 안내 본문','Cabin · 안내 하단',
    'Cabin · 출구 표기','Cabin · 용량 표기','Cabin · 측면 층 선택 제목',
    '문 내부 안전 안내','자동문 유리 안내','로비 월 그래픽 영문',
    '휴지통 표기','입구 아트 제목','계단실 층 안내 영문','호출 안내')
removed=[]
for o in list(S.objects):
    if o.type=='FONT' and o.name.startswith(remove_prefixes):
        removed.append({'name':o.name,'text':o.data.body});bpy.data.objects.remove(o,do_unlink=True)
# The second lift is an unused, closed visual bank at floor 3; it is not marked faulty.
set_text('Lift 1 · 층수','1')
set_text('Lift 2 · 층수','3')
set_text('층 번호 대형','1')
set_text('Cabin · 전면 층수','1')
set_text('로비 월 그래픽 층수','1F')
set_text('계단실 층 안내','2F')
o=set_text('계단 하부 층 안내','2F ↑')
if o:o.data.size=.19
set_text('출입구 표기','출입구')
second_arrow=bpy.data.objects.get('Lift 2 · 방향')
if second_arrow:
    removed.append({'name':second_arrow.name,'text':second_arrow.data.body});bpy.data.objects.remove(second_arrow,do_unlink=True)
text('센터 이름','한빛 생활체육센터',(-5.01,3.151,1.78),.104,'charcoal',col='furniture')
set_text('안내판 내용','1F  로비 · 농구장\n2F  탈의실 · 보관함\n3F  운동교실\n4–12F  문화 · 운영')
bpy.data.objects['안내판 내용'].data.size=.057
bpy.data.objects['안내판 내용'].location.z=1.095

# Current-floor illumination agrees with the 1F displays, preserving shallow buttons.
for plane in ['전면','측면']:
    for floor in [1,2]:
        ring=bpy.data.objects[f'Cabin · {plane} {floor:02d}층 링']
        ring.data.materials[0]=M['active'] if floor==1 else M['rail']

# Shift the entire side control assembly 45 cm towards the cabin rear.
move_prefixes=('Cabin · 측면 전체 패널','Cabin · 측면 패널 구획선',
    'Cabin · 측면 비상','Cabin · 측면 문 개폐','Cabin · 측면 개폐')
side_moved=[]
for o in S.objects:
    if o.name.startswith(move_prefixes) or (o.name.startswith('Cabin · 측면 ') and any(s in o.name for s in ['층 링','층 면','층 숫자'])):
        o.location.y+=.45;side_moved.append(o.name)

# Readable identification directly below the high front emergency call button.
px=bpy.data.objects['Cabin · 전면 비상 버튼 [높이 2.15m]'].location.x
plate=box('Cabin · 승강기 식별 정보판',(px,3.690,1.993),(.438,.016,.178),'white',.006)
plate['건물']='한빛 생활체육센터';plate['설치위치']='본관 1호기';plate['관리번호']='HB-EL-01'
plate['식별정보 용도']='비상통화 시 건물명과 호기·관리번호를 전달하는 게임 내 단서'
text('Cabin · 식별 건물명','한빛 생활체육센터',(px,3.702,2.045),.027,'charcoal','back')
text('Cabin · 식별 호기와 번호','본관 1호기  |  HB-EL-01',(px,3.702,1.994),.024,'charcoal','back')
text('Cabin · 식별 정원 하중','1000 kg  /  13명',(px,3.702,1.945),.022,'charcoal','back')
bold=bpy.data.fonts.load('C:/Windows/Fonts/malgunbd.ttf');bold.pack()
ink=bpy.data.materials.new('26 · 식별판 무광 검정 글자');ink.use_nodes=True
bsdf=ink.node_tree.nodes.get('Principled BSDF')
bsdf.inputs['Base Color'].default_value=(.006,.009,.011,1)
bsdf.inputs['Roughness'].default_value=.95;bsdf.inputs['Metallic'].default_value=0
for name,size in [('Cabin · 식별 건물명',.036),('Cabin · 식별 호기와 번호',.030),('Cabin · 식별 정원 하중',.024)]:
    obj=bpy.data.objects[name];obj.data.font=bold;obj.data.size=size;obj.data.materials[0]=ink

# Larger framed poster, using the original right-of-view safety-sign location.
frame=bpy.data.objects['Cabin · 안전 안내 프레임']
frame.location.z=1.88;frame.dimensions=(.06,.76,1.13)
paper=bpy.data.objects['Cabin · 안전 안내 종이']
paper.location.z=1.88;paper.dimensions=(.018,.715,1.085)
poster_img=bpy.data.images.load(str(WORK/'엘리베이터_안전포스터.png'),check_existing=True)
poster_img.colorspace_settings.name='sRGB';poster_img.pack()
poster_img.filepath='//엘리베이터_안전포스터.png'
mat=bpy.data.materials.new('25 · 생성형 안전 포스터 [내장]');mat.use_nodes=True
n=mat.node_tree.nodes;l=mat.node_tree.links;p=n.get('Principled BSDF')
tex=n.new('ShaderNodeTexImage');tex.image=poster_img
p.inputs['Roughness'].default_value=.8
l.new(tex.outputs['Color'],p.inputs['Base Color'])
l.new(tex.outputs['Color'],p.inputs['Emission Color']);p.inputs['Emission Strength'].default_value=.12
yp=frame.location.y;zp=1.88;xp=paper.location.x+.010
w=.70;h=1.05
mesh=bpy.data.meshes.new('안전포스터_UV')
mesh.from_pydata([(xp,yp-w/2,zp-h/2),(xp,yp+w/2,zp-h/2),(xp,yp+w/2,zp+h/2),(xp,yp-w/2,zp+h/2)],[],[(0,1,2,3)])
mesh.materials.append(mat);uv=mesh.uv_layers.new(name='Poster UV')
for loop,co in zip(uv.data,[(0,0),(1,0),(1,1),(0,1)]):loop.uv=co
poster=bpy.data.objects.new('Cabin · 안전 포스터 이미지 [내장 PNG]',mesh);COL['cabin'].objects.link(poster)
poster['원본 이미지']='엘리베이터_안전포스터.png';poster['제작']='내장 image_gen / 직접 생성한 안전 안내 그림'

# Accommodate the moved side panel in both interior views.
cam=bpy.data.objects['02 · 엘리베이터 내부'];cam.location=(-2.4,6.24,1.65)
cam.rotation_euler=(Vector((-1.55,3.4,1.5))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=16
compare=bpy.data.objects['05 · 전면과 측면 버튼 높이 비교'];compare.location=(-2.7,6.22,1.6)
compare.rotation_euler=(Vector((-1.5,3.85,1.55))-compare.location).to_track_quat('-Z','Y').to_euler();compare.data.lens=18.5
d=bpy.data.cameras.new('06 · 승강기 식별 정보와 안전 포스터')
detail=bpy.data.objects.new('06 · 승강기 식별 정보와 안전 포스터',d);COL['camera'].objects.link(detail)
detail.location=(-.65,5.7,1.85)
detail.rotation_euler=(Vector((-3.04,3.92,1.99))-detail.location).to_track_quat('-Z','Y').to_euler()
d.lens=30;d.clip_start=.05
S.name='한빛 생활체육센터 · 1층 승강기 홀'
S['건물 콘셉트']='한빛 생활체육센터 / 체육관과 문화·교육시설을 함께 사용하는 현대식 복합시설'
S['현재층']=1;S['오른쪽 보조 승강기 현재층']=3
S['오른쪽 보조 승강기 역할']='이번 체험에서 이용하지 않는 닫힌 외장, 3층에 대기'
S['전면 식별정보']='한빛 생활체육센터 / 본관 1호기 / 관리번호 HB-EL-01'
S['측면 패널 이동']='출입문에서 뒤쪽(+Y)으로 0.45 m 이동, 손잡이 위 높이 유지'
S['시나리오 요약']='농구 종료 → 1층 승강기 홀 → 2층 보관함으로 귀가 준비 → 정지 → 낮은 측면 비상벨 → 식별정보 전달 → 안내에 따른 대기·구조'
S['기본 카메라']='02 · 엘리베이터 내부 / 1프레임 문 닫힘 / 1층'
readme='''한빛 생활체육센터 · 엘리베이터 맵

Blender 5.1 / 미터 단위 / Z축 위
이번 수정은 사용자가 마지막으로 저장한 엘베_맵_실버.blend를 기준으로 했습니다.
기존 실버 파일과 사용자 변경은 보존하며 별도의 체육센터 버전으로 저장합니다.

주 엘리베이터와 로비는 1층, 닫힌 오른쪽 보조 엘리베이터는 3층으로 표시합니다.
현재층 1 버튼의 링을 전면·측면에 표시했습니다.
출입문 기준 측면 패널은 뒤쪽으로 45 cm 이동했습니다. 높이와 얕은 버튼은 유지합니다.
전면 비상벨 아래의 정보판: 한빛 생활체육센터 / 본관 1호기 / HB-EL-01.
HB-EL-01은 가상 건물의 관리번호입니다. 실제 승강기 국가 등록번호를 뜻하지 않습니다.
오른쪽 벽의 안전 안내는 새로 생성한 PNG 포스터로 교체했습니다.
사용 텍스처와 한글 폰트를 blend 파일에 포함했습니다.

1프레임 내부 카메라로 열립니다. 72프레임은 문이 열려 있습니다.
문 애니메이션: 1~24 닫힘 → 52~96 열림 → 124~144 닫힘.
01 로비 / 02 내부 / 03 뒤쪽 자동문 / 04 전체 / 05 두 패널 / 06 정보판과 포스터.
전체 미리보기는 일부 벽과 천장을 숨겨 배치를 보여 줍니다. 저장된 모델에는 벽과 천장이 있습니다.

건물콘셉트_시나리오.txt: 건물 설정, 대안 검토, 새 이야기와 대사·상호작용 설계.
이번 시나리오는 기획 문서입니다. 기존 Unity 씬과 런타임 시나리오 코드는 변경하지 않았습니다.
'''
old=bpy.data.texts.get('읽어주세요.txt')
if old:old.clear();old.write(readme)
else:bpy.data.texts.new('읽어주세요.txt').write(readme)
(WORK/'읽어주세요.txt').write_text(readme,encoding='utf-8')
story=(WORK/'건물콘셉트_시나리오.txt').read_text(encoding='utf-8')
bpy.data.texts.new('건물콘셉트_시나리오.txt').write(story)
for o in bpy.context.selected_objects:o.select_set(False)
S.camera=cam;S.frame_set(1);S.render.filepath='//체육센터_02_내부.png'
S.render.resolution_x=1600;S.render.resolution_y=1000;S.render.resolution_percentage=100;S.cycles.samples=64
bpy.ops.file.pack_all();bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT),compress=True)
report={'sourceSnapshot':'원본_사용자최신.blend','output':str(OUTPUT),'removedText':removed,'movedSideObjects':side_moved,'sideShiftMeters':.45,'venue':'한빛 생활체육센터','primaryFloor':1,'secondaryFloor':3,'posterPixels':list(poster_img.size),'posterPacked':poster_img.packed_file is not None,'objects':len(S.objects),'currentFrame':S.frame_current,'camera':S.camera.name}
(WORK/'Build_Validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('SPORTS_CENTRE_SAVED '+json.dumps(report,ensure_ascii=False),flush=True)
# Low-cost previews before final rendering.
pref=bpy.context.preferences.addons['cycles'].preferences;pref.compute_device_type='OPTIX';pref.get_devices()
for dev in pref.devices:dev.use=dev.type!='CPU'
S.cycles.device='GPU';S.cycles.samples=24;S.render.resolution_percentage=70
for camera,out in [(cam,'02_내부_draft.png'),(detail,'06_식별정보_포스터_draft.png')]:
    S.camera=camera;S.render.filepath=str(WORK/out);bpy.ops.render.render(write_still=True)
print('SPORTS_CENTRE_DRAFTS_COMPLETE',flush=True)
