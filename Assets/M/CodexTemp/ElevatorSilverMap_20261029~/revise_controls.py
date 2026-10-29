"""User revision: broader front console, high emergency call, low side console."""
import bpy
import math
from pathlib import Path
from mathutils import Vector
WORK=Path(__file__).resolve().parent
S=bpy.context.scene
S.frame_set(1)
CAB=next(c for c in bpy.data.collections if c.name.startswith('05'))
LIFT=next(c for c in bpy.data.collections if c.name.startswith('04'))
M={key:next(m for m in bpy.data.materials if m.name.startswith(prefix)) for key,prefix in [('silver','03'),('brushed','04'),('charcoal','05'),('black','06'),('white','07'),('rail','08'),('display','18'),('active','19'),('amber','20')]}
font=next(f for f in bpy.data.fonts if 'Malgun' in f.name)
latin=next(f for f in bpy.data.fonts if 'Segoe' in f.name)
def finish(o,name,mat,col=CAB):
    o.name=name
    for c in list(o.users_collection):c.objects.unlink(o)
    col.objects.link(o);o.data.materials.append(M[mat]);return o
def box(name,loc,size,mat,bev=.01,col=CAB):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=finish(bpy.context.object,name,mat,col);o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    m=o.modifiers.new('모서리 베벨','BEVEL');m.width=bev;m.segments=3
    o.modifiers.new('면 노멀','WEIGHTED_NORMAL');return o
def cyl(name,loc,radius,depth,mat,axis='Y'):
    bpy.ops.mesh.primitive_cylinder_add(vertices=32,radius=radius,depth=depth,location=loc)
    o=finish(bpy.context.object,name,mat)
    o.rotation_euler.x=math.pi/2
    if axis=='X':o.rotation_euler=(0,math.pi/2,0)
    for p in o.data.polygons:p.use_smooth=True
    b=o.modifiers.new('버튼 베벨','BEVEL');b.width=.003;b.segments=3
    o.modifiers.new('면 노멀','WEIGHTED_NORMAL');return o
def text(name,body,loc,size,mat='white',side=False,lf=False):
    cr=bpy.data.curves.new(name,'FONT');cr.body=body;cr.font=latin if lf else font
    cr.size=size;cr.align_x='CENTER';cr.align_y='CENTER';cr.extrude=.0004;cr.bevel_depth=.00015
    cr.materials.append(M[mat]);o=bpy.data.objects.new(name,cr);CAB.objects.link(o);o.location=loc
    o.rotation_euler=(math.pi/2,0,-math.pi/2 if side else math.pi);return o
def rod(name,a,b,r=.031):
    a,b=Vector(a),Vector(b)
    bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=r,depth=(b-a).length,location=(a+b)/2)
    o=finish(bpy.context.object,name,'rail');o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    for p in o.data.polygons:p.use_smooth=True
    m=o.modifiers.new('레일 끝 베벨','BEVEL');m.width=.008;m.segments=3
    return o
def animate(o,start,delta):
    o.animation_data_clear()
    for frame,factor in [(1,0),(24,0),(52,1),(96,1),(124,0),(144,0)]:
        o.location=Vector(start)+Vector(delta)*factor
        o.keyframe_insert(data_path='location',frame=frame,group='자동문 슬라이드')
    o['slideDistance']=.9

# Remove only the primary cabin's previous skinny control console and its contents.
old_prefixes=('Cabin · 버튼 콘솔','Cabin · 내부 디스플레이','Cabin · 디스플레이','Cabin · 용량 안내','Cabin · 비상','Cabin · 문 열림 닫힘','Cabin · 개폐 아이콘','Cabin · 스피커 구멍')
for o in list(CAB.objects):
    if o.name.startswith(old_prefixes) or ('층 버튼' in o.name or '층 숫자' in o.name):bpy.data.objects.remove(o,do_unlink=True)

# 1.8 m clear entry instead of 2.2 m; keep the generous 3 m cabin unchanged.
cx=-1.8
for side,label in [(-1,'왼쪽'),(1,'오른쪽')]:
    leaf=bpy.data.objects['Lift 1 · %s 문 [슬라이드]'%label]
    xmin=min(v.co.x for v in leaf.data.vertices);xmax=max(v.co.x for v in leaf.data.vertices)
    for v in leaf.data.vertices:v.co.x*=.894/(xmax-xmin)
    animate(leaf,(cx+side*.45,3.23,1.405),(side*.9,0,0))
    ret=bpy.data.objects['Cabin · 출입구 내부 측면 마감 '+label]
    ret.location=(cx+side*1.21,3.41,1.42);ret.dimensions=(.62,.38,2.83)
    box('Lift 1 · 넓어진 실버 문틀 마감 '+label,(cx+side*1.125,3.08,1.405),(.18,.18,2.66),'brushed',.006,LIFT)
for o in LIFT.objects:
    if o.name.startswith('Lift 1 · 실버 문틀'):
        side=-1 if o.location.x<cx else 1;o.location.x=cx+side*.97
    if o.name.startswith('Lift 1 · 실버 상인방'):o.dimensions.x=2.08

# Broad front console: 3 columns / 4 rows instead of the old 2-column vertical strip.
px=cx+1.21
front=box('Cabin · 전면 패널 실버 테두리',(px,3.628,1.7),(.51,.055,1.7),'silver',.023)
front['폭_m']=.51;front['용도']='전면 층 선택 + 높은 비상 호출'
box('Cabin · 전면 패널 차콜 면',(px,3.664,1.7),(.472,.025,1.658),'charcoal',.018)
box('Cabin · 전면 디스플레이',(px,3.685,2.33),(.362,.012,.25),'black',.012)
text('Cabin · 전면 층수','02',(px,3.697,2.33),.155,'display',lf=True)
text('Cabin · 용량 표기','1000 kg  /  13인승',(px,3.684,2.505),.032)
# High emergency button: intentionally well above the low side console.
fz=1.95
fring=cyl('Cabin · 전면 비상 호출 링',(px,3.701,fz),.073,.024,'rail')
front_call=cyl('Cabin · 전면 비상 버튼 [높이 1.95m]',(px,3.719,fz),.059,.019,'amber')
front_call['버튼 중심 높이_m']=fz
text('Cabin · 전면 비상 버튼 문자','비상',(px,3.732,fz),.038,'charcoal')
text('Cabin · 전면 비상 호출 안내','비상 호출',(px,3.694,2.086),.052)
for i in range(5):
    for j in range(2):cyl('Cabin · 전면 스피커',(px-.055+i*.0275,3.691,2.17+j*.022),.0045,.008,'black')
for floor in range(1,13):
    i=floor-1;row=i//3;col=i%3;x=px+.14-col*.14;z=1.12+row*.17
    cyl('Cabin · 전면 %02d층 링'%floor,(x,3.696,z),.048,.024,'active' if floor==2 else 'rail')
    cyl('Cabin · 전면 %02d층 면'%floor,(x,3.713,z),.036,.014,'black')
    text('Cabin · 전면 %02d층 숫자'%floor,str(floor),(x,3.725,z),.043,lf=True)
for x,body in [(px+.11,'◀▶'),(px-.11,'▶◀')]:
    cyl('Cabin · 전면 문 개폐',(x,3.701,.928),.046,.024,'rail')
    text('Cabin · 전면 개폐 문자',body,(x,3.719,.928),.029,'charcoal',lf=True)

# Low horizontal side console; interrupt the handrail here for a clear reach.
for o in list(CAB.objects):
    if o.name.startswith('Cabin · 측벽 손잡이') and o.location.x>cx:
        bpy.data.objects.remove(o,do_unlink=True)
rod('Cabin · 측면 낮은 패널 앞 손잡이',(cx+1.38,3.85,1.02),(cx+1.38,4.15,1.02))
rod('Cabin · 측면 낮은 패널 뒤 손잡이',(cx+1.38,5.2,1.02),(cx+1.38,6.12,1.02))
wallx=cx+1.455;py=4.67
sidepanel=box('Cabin · 측면 보조 패널 실버 테두리',(wallx,py,.88),(.065,.96,.47),'silver',.024)
sidepanel['용도']='아이 손이 닿는 낮은 비상 호출 / 문 개폐'
box('Cabin · 측면 보조 패널 차콜 면',(wallx-.043,py,.88),(.026,.919,.427),'charcoal',.018)
text('Cabin · 측면 비상 호출 안내','비상 호출',(wallx-.063,py,1.035),.052,side=True)
sz=.85
cyl('Cabin · 측면 비상 호출 링',(wallx-.071,py,sz),.072,.024,'rail',axis='X')
side_call=cyl('Cabin · 측면 비상 버튼 [높이 0.85m]',(wallx-.088,py,sz),.058,.019,'amber',axis='X')
side_call['버튼 중심 높이_m']=sz
text('Cabin · 측면 비상 버튼 문자','비상',(wallx-.102,py,sz),.038,'charcoal',side=True)
for yy,body,label in [(py+.3,'◀▶','열림'),(py-.3,'▶◀','닫힘')]:
    cyl('Cabin · 측면 문 개폐 링',(wallx-.07,yy,sz),.05,.023,'rail',axis='X')
    cyl('Cabin · 측면 문 개폐 면',(wallx-.086,yy,sz),.039,.015,'black',axis='X')
    text('Cabin · 측면 개폐 아이콘',body,(wallx-.1,yy,sz),.029,side=True,lf=True)
    text('Cabin · 측면 개폐 안내',label,(wallx-.068,yy,.71),.035,side=True)
text('Cabin · 측면 낮은 위치 안내','낮은 위치',(wallx-.069,py,.71),.034,side=True)

# The interior shot deliberately includes both control locations in one frame.
cam=bpy.data.objects['02 · 엘리베이터 내부']
cam.location=(cx-.93,6.05,1.65)
cam.rotation_euler=(Vector((cx+.22,3.63,1.42))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=19.3
cd=bpy.data.cameras.new('05 · 전면과 측면 버튼 높이 비교')
compare=bpy.data.objects.new('05 · 전면과 측면 버튼 높이 비교',cd)
next(c for c in bpy.data.collections if c.name.startswith('14')).objects.link(compare)
compare.location=(cx-1.03,5.6,1.35)
compare.rotation_euler=(Vector((-.65,4.0,1.43))-compare.location).to_track_quat('-Z','Y').to_euler();cd.lens=24;cd.clip_start=.05
S['주 엘리베이터 출입구 폭_m']=1.8
S['전면 비상 버튼 중심 높이_m']=fz
S['측면 비상 버튼 중심 높이_m']=sz
S['버튼 패널 수정']='전면 폭 0.51 m / 3열 4행 / 높은 비상 호출 1.95 m / 측면 낮은 비상 호출 0.85 m'
readme=bpy.data.texts['읽어주세요.txt'];body=readme.as_string()
body+='''\n버튼 패널 개선:\n주 엘리베이터의 문 폭을 2.2 m에서 1.8 m로 조정했습니다.\n전면 패널은 폭 0.51 m, 층 버튼 3열 4행으로 넓게 재배치했습니다.\n전면 비상 호출 버튼의 중심은 바닥에서 1.95 m입니다.\n측면에는 중심 높이 0.85 m의 낮은 비상 호출 버튼과 문 개폐 버튼을 추가했습니다.\n측면 패널 앞의 손잡이를 나누어 손이 접근할 공간을 확보했습니다.\n05 카메라 / 05_버튼패널_높이비교.png에서 두 위치를 함께 볼 수 있습니다.\n'''
readme.clear();readme.write(body);(WORK/'읽어주세요.txt').write_text(body,encoding='utf-8')
S.frame_set(72);S.camera=bpy.data.objects['01 · 로비 / 엘리베이터와 우측 계단'];S.render.filepath='//01_로비.png'
for o in bpy.context.selected_objects:o.select_set(False)
bpy.ops.wm.save_as_mainfile(filepath=str(WORK/'엘베_맵_실버.blend'),compress=True)
print('CONTROL_REVISION_SAVED',flush=True)
# Fast control-focused preview before final rendering.
pref=bpy.context.preferences.addons['cycles'].preferences;pref.compute_device_type='OPTIX';pref.get_devices()
for d in pref.devices:d.use=d.type!='CPU'
S.cycles.device='GPU';S.cycles.samples=24;S.render.resolution_percentage=70
S.camera=compare;S.frame_set(1);S.render.filepath=str(WORK/'05_버튼패널_높이비교_draft.png')
bpy.ops.render.render(write_still=True)
print('CONTROL_REVISION_DRAFT_DONE',flush=True)
