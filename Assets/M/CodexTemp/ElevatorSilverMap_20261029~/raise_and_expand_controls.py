"""Second requested revision: higher front console and full side controls above rail."""
import bpy
import math
from pathlib import Path
from mathutils import Vector
WORK=Path(__file__).resolve().parent
S=bpy.context.scene
CAB=next(c for c in bpy.data.collections if c.name.startswith('05'))
M={key:next(m for m in bpy.data.materials if m.name.startswith(prefix)) for key,prefix in [('silver','03'),('charcoal','05'),('black','06'),('white','07'),('rail','08'),('active','19'),('amber','20')]}
font=next(f for f in bpy.data.fonts if 'Malgun' in f.name);latin=next(f for f in bpy.data.fonts if 'Segoe' in f.name)
def finish(o,name,mat):
    o.name=name
    for c in list(o.users_collection):c.objects.unlink(o)
    CAB.objects.link(o);o.data.materials.append(M[mat]);return o
def box(name,loc,size,mat,bev=.01):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=finish(bpy.context.object,name,mat);o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    m=o.modifiers.new('모서리 베벨','BEVEL');m.width=bev;m.segments=3;o.modifiers.new('면 노멀','WEIGHTED_NORMAL');return o
def cyl(name,loc,radius,depth,mat):
    bpy.ops.mesh.primitive_cylinder_add(vertices=32,radius=radius,depth=depth,location=loc)
    o=finish(bpy.context.object,name,mat);o.rotation_euler.y=math.pi/2
    for p in o.data.polygons:p.use_smooth=True
    m=o.modifiers.new('버튼 베벨','BEVEL');m.width=.003;m.segments=3;o.modifiers.new('면 노멀','WEIGHTED_NORMAL');return o
def text(name,body,loc,size,mat='white',lf=False):
    cr=bpy.data.curves.new(name,'FONT');cr.body=body;cr.font=latin if lf else font
    cr.size=size;cr.align_x='CENTER';cr.align_y='CENTER';cr.extrude=.0004;cr.bevel_depth=.00015;cr.materials.append(M[mat])
    o=bpy.data.objects.new(name,cr);CAB.objects.link(o);o.location=loc;o.rotation_euler=(math.pi/2,0,-math.pi/2);return o
def arrows(name,loc,outward=True,side=True,mat='white'):
    # Actual mesh triangles, independent of symbol-font availability.
    w=.021;h=.013;gap=.003
    if outward:points=[(-w-gap,0),(-gap,h),(-gap,-h),(w+gap,0),(gap,-h),(gap,h)]
    else:points=[(-gap,0),(-w-gap,h),(-w-gap,-h),(gap,0),(w+gap,-h),(w+gap,h)]
    p=Vector(loc)
    verts=[tuple(p+(Vector((0,-u,v)) if side else Vector((-u,0,v)))) for u,v in points]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],[(0,1,2),(3,4,5)]);mesh.materials.append(M[mat])
    o=bpy.data.objects.new(name,mesh);CAB.objects.link(o);return o

# Raise every piece of the front control assembly by exactly 200 mm.
for o in CAB.objects:
    if o.name.startswith('Cabin · 전면') or o.name=='Cabin · 용량 표기':o.location.z+=.2
front_call=bpy.data.objects['Cabin · 전면 비상 버튼 [높이 1.95m]']
front_call.name='Cabin · 전면 비상 버튼 [높이 2.15m]';front_call['버튼 중심 높이_m']=2.15
for o in list(CAB.objects):
    if o.name.startswith('Cabin · 전면 개폐 문자'):
        p=o.location.copy();p.y+=.003;outward=o.data.body=='◀▶'
        bpy.data.objects.remove(o,do_unlink=True);arrows('Cabin · 전면 개폐 삼각 아이콘',p,outward,False,'charcoal')

# Replace the earlier low console and its split rail, retaining cabin walls and trim.
prefixes=('Cabin · 측면 보조 패널','Cabin · 측면 비상','Cabin · 측면 문 개폐','Cabin · 측면 개폐','Cabin · 측면 낮은')
for o in list(CAB.objects):
    if o.name.startswith(prefixes):bpy.data.objects.remove(o,do_unlink=True)
a,b=Vector((-.42,3.85,1.02)),Vector((-.42,6.12,1.02))
bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=.031,depth=(b-a).length,location=(a+b)/2)
rail=finish(bpy.context.object,'Cabin · 측벽 손잡이 · 우측 연속','rail');rail.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
for p in rail.data.polygons:p.use_smooth=True
mod=rail.modifiers.new('레일 끝 베벨','BEVEL');mod.width=.008;mod.segments=3

wx=-.345;py=4.67
panel=box('Cabin · 측면 전체 패널 실버 테두리',(wx,py,1.56),(.065,1.24,.88),'silver',.024)
panel['용도']='손잡이 위의 전체 층 선택, 문 열림·닫힘, 낮은 비상 호출'
panel['패널 하단 높이_m']=1.12
box('Cabin · 측면 전체 패널 차콜 면',(wx-.043,py,1.56),(.026,1.199,.838),'charcoal',.018)
box('Cabin · 측면 패널 구획선',(wx-.060,4.56,1.56),(.006,.008,.71),'silver',.002)
text('Cabin · 측면 층 선택 제목','층 선택',(wx-.065,py+.23,1.93),.052)
for floor in range(1,13):
    i=floor-1;row=i//3;col=i%3;yy=py+.37-col*.14;z=1.29+row*.17
    cyl('Cabin · 측면 %02d층 링'%floor,(wx-.071,yy,z),.048,.024,'active' if floor==2 else 'rail')
    cyl('Cabin · 측면 %02d층 면'%floor,(wx-.088,yy,z),.036,.014,'black')
    text('Cabin · 측면 %02d층 숫자'%floor,str(floor),(wx-.102,yy,z),.043,lf=True)
ey=py-.39;ez=1.28
text('Cabin · 측면 비상 호출 안내','비상 호출',(wx-.066,ey,1.433),.051)
cyl('Cabin · 측면 비상 호출 링',(wx-.071,ey,ez),.073,.024,'rail')
call=cyl('Cabin · 측면 비상 버튼 [높이 1.28m]',(wx-.088,ey,ez),.059,.019,'amber')
call['버튼 중심 높이_m']=ez
text('Cabin · 측면 비상 버튼 문자','비상',(wx-.102,ey,ez),.038,'charcoal')
text('Cabin · 측면 개폐 제목','문 열림·닫힘',(wx-.066,ey,1.88),.037)
for yy,outward,label in [(ey+.135,True,'열림'),(ey-.135,False,'닫힘')]:
    cyl('Cabin · 측면 문 개폐 링',(wx-.071,yy,1.655),.05,.024,'rail')
    cyl('Cabin · 측면 문 개폐 면',(wx-.088,yy,1.655),.038,.015,'black')
    arrows('Cabin · 측면 개폐 삼각 아이콘',(wx-.102,yy,1.655),outward)
    text('Cabin · 측면 개폐 안내',label,(wx-.067,yy,1.533),.036)

compare=bpy.data.objects['05 · 전면과 측면 버튼 높이 비교']
compare.location=(-2.83,5.6,1.6)
compare.rotation_euler=(Vector((-.65,4,1.65))-compare.location).to_track_quat('-Z','Y').to_euler();compare.data.lens=22
S['전면 비상 버튼 중심 높이_m']=2.15
S['측면 비상 버튼 중심 높이_m']=1.28
S['버튼 패널 수정']='전면 전체 +0.20 m / 측면 손잡이 위 / 두 패널 모두 12개 층 버튼과 문 열림·닫힘 / 전면 비상 2.15 m, 측면 비상 1.28 m'
readme=bpy.data.texts['읽어주세요.txt'];body=readme.as_string().split('\n버튼 패널 개선:')[0]
body+='''\n버튼 패널 개선 (최종):\n주 엘리베이터 문 폭은 1.8 m, 전면 패널 폭은 0.51 m입니다.\n전면 패널 전체를 20 cm 올렸고 전면 비상 버튼 중심은 2.15 m입니다.\n측면 패널은 손잡이(중심 1.02 m) 위에 있습니다. 패널 하단 높이는 1.12 m입니다.\n전면과 측면 모두 12개 층 버튼(3열·4행)과 문 열림·닫힘 버튼이 있습니다.\n측면 비상 버튼 중심은 1.28 m로 전면보다 0.87 m 낮습니다.\n손잡이는 측면 패널 아래를 연속해서 지나갑니다.\n05 카메라 / 05_버튼패널_높이비교.png에서 두 패널을 함께 볼 수 있습니다.\n'''
readme.clear();readme.write(body);(WORK/'읽어주세요.txt').write_text(body,encoding='utf-8')
S.frame_set(72);S.camera=bpy.data.objects['01 · 로비 / 엘리베이터와 우측 계단'];S.render.filepath='//01_로비.png'
for o in bpy.context.selected_objects:o.select_set(False)
bpy.ops.wm.save_as_mainfile(filepath=str(WORK/'엘베_맵_실버.blend'),compress=True)
print('RAISED_CONTROLS_SAVED',flush=True)
pref=bpy.context.preferences.addons['cycles'].preferences;pref.compute_device_type='OPTIX';pref.get_devices()
for d in pref.devices:d.use=d.type!='CPU'
S.cycles.device='GPU';S.cycles.samples=24;S.render.resolution_percentage=70
S.camera=compare;S.frame_set(1);S.render.filepath=str(WORK/'05_버튼패널_높이비교_draft_v3.png')
bpy.ops.render.render(write_still=True)
print('RAISED_CONTROLS_DRAFT_DONE',flush=True)
