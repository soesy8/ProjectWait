"""Editable, self-contained modern elevator lobby; Blender 5.1, metres, Z up.
All build/QA output stays beside this script. Delivery is a separate copy step.
"""
import bpy
import math
import random
import json
from pathlib import Path
from mathutils import Vector

WORK = Path(__file__).resolve().parent
WORK.mkdir(parents=True, exist_ok=True)
OUTPUT = WORK / '엘베_맵_실버.blend'
random.seed(291026)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.preferences.filepaths.save_version = 0
S = bpy.context.scene
S.name = '실버 아트리움 · 엘리베이터 맵'
S.unit_settings.system = 'METRIC'
S.unit_settings.length_unit = 'METERS'
S.render.engine = 'CYCLES'
S.cycles.samples = 48
S.cycles.use_denoising = True
S.cycles.max_bounces = 8
S.cycles.transparent_max_bounces = 8
S.render.resolution_x = 1600
S.render.resolution_y = 1000
S.render.resolution_percentage = 100
S.render.image_settings.file_format = 'PNG'
S.render.image_settings.color_mode = 'RGB'
S.render.film_transparent = False
S.view_settings.view_transform = 'AgX'
S.view_settings.look = 'AgX - Medium High Contrast'
S.render.fps = 24
S.frame_start = 1
S.frame_end = 144
S.world = bpy.data.worlds.new('Soft daylight')
S.world.use_nodes = True
S.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.72,.82,1,1)
S.world.node_tree.nodes['Background'].inputs['Strength'].default_value = .32

# Prefer an installed GPU; this is a scene setting, not a saved system preference.
device_info=[]
try:
    pref=bpy.context.preferences.addons['cycles'].preferences
    for backend in ['OPTIX','CUDA','HIP','ONEAPI','METAL']:
        try:
            pref.compute_device_type=backend
            pref.get_devices()
            gpu=[d for d in pref.devices if d.type!='CPU']
            if gpu:
                for d in pref.devices: d.use = d.type!='CPU'
                S.cycles.device='GPU'
                device_info=[d.name for d in gpu]
                break
        except Exception: pass
except Exception: pass
print('RENDER_DEVICES',device_info or ['CPU'],flush=True)

COL={}
def collection(key,name):
    c=bpy.data.collections.new(name)
    S.collection.children.link(c)
    COL[key]=c
    return c
for key,name in [
    ('floor','01 · 로비 바닥 / 석재'),('walls','02 · 로비 벽 / 패널'),
    ('windows','03 · 왼쪽 통창'),('elevator','04 · 엘리베이터 외장'),
    ('cabin','05 · 엘리베이터 내부'),('doors','06 · 엘리베이터 슬라이딩 문'),
    ('stairs','07 · 오른쪽 올라가는 계단'),('rear','08 · 뒤쪽 자동문 / 진입부'),
    ('furniture','09 · 가구 / 화분 / 안내'),('ceiling','10 · 로비 천장 [분리 가능]'),
    ('upper','11 · 계단실 상부 / 난간'),('outside','12 · 창밖 풍경'),
    ('lights','13 · 조명'),('cameras','14 · 카메라'),('helpers','15 · 편집 가이드')]:
    collection(key,name)
ACTIVE='floor'
def move(obj,key=None):
    key=key or ACTIVE
    for c in list(obj.users_collection): c.objects.unlink(obj)
    COL[key].objects.link(obj)
    return obj
def rgba(h):
    h=h.lstrip('#')
    v=[int(h[i:i+2],16)/255 for i in (0,2,4)]
    return tuple(a/12.92 if a<=.04045 else ((a+.055)/1.055)**2.4 for a in v)+(1,)
def material(name,color,rough=.5,metal=0,noise=0,scale=5):
    m=bpy.data.materials.new(name)
    m.diffuse_color=rgba(color)
    m.use_nodes=True
    n=m.node_tree.nodes; l=m.node_tree.links
    p=n.get('Principled BSDF')
    p.inputs['Base Color'].default_value=rgba(color)
    p.inputs['Roughness'].default_value=rough
    p.inputs['Metallic'].default_value=metal
    if noise:
        tex=n.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=scale
        tex.inputs['Detail'].default_value=3
        ramp=n.new('ShaderNodeValToRGB')
        base=rgba(color)
        ramp.color_ramp.elements[0].position=.18
        ramp.color_ramp.elements[0].color=tuple(v*.82 for v in base[:3])+(1,)
        ramp.color_ramp.elements[1].position=.82
        ramp.color_ramp.elements[1].color=tuple(min(1,v*1.08) for v in base[:3])+(1,)
        l.new(tex.outputs['Fac'],ramp.inputs[0]);l.new(ramp.outputs[0],p.inputs['Base Color'])
        bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=noise
        bump.inputs['Distance'].default_value=.015
        l.new(tex.outputs['Fac'],bump.inputs['Height']);l.new(bump.outputs[0],p.inputs['Normal'])
    return m
def emit(name,color,strength=3):
    m=material(name,color,.25)
    p=m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Emission Color'].default_value=rgba(color)
    p.inputs['Emission Strength'].default_value=strength
    return m
M={
    'stone':material('01 · 밝은 회색 석재', '#BFC3C1',.57,0,.12,3.5),
    'wall':material('02 · 라이트 그레이 벽 패널','#D5D7D5',.7,0,.09,4),
    'silver':material('03 · 새틴 스테인리스','#B7C0C4',.28,.82,.035,80),
    'brushed':material('04 · 헤어라인 실버','#AEB6B9',.36,.78),
    'charcoal':material('05 · 차콜 패널','#29343A',.43,.15),
    'black':material('06 · 틈 / 고무 / 검정','#101B21',.52),
    'white':material('07 · 깨끗한 오프화이트','#ECEEEA',.45),
    'rail':material('08 · 손잡이 크롬','#D1D9DC',.2,.92),
    'glass':material('09 · 투명 창 유리','#DCEEF3',.08,.05),
    'frost':material('10 · 반투명 안전 띠','#D8E4E6',.38,.05),
    'mirror':material('11 · 엘리베이터 거울','#E1E7EA',.035,1),
    'fabric':material('12 · 벤치 패브릭','#6B787C',.85,0,.3,90),
    'leaf':material('13 · 딥 그린 잎','#31554A',.52),
    'leaf2':material('14 · 세이지 잎','#5E7C63',.6),
    'soil':material('15 · 흙','#292B27',.98,0,.4,15),
    'stem':material('16 · 식물 가지','#5D6351',.78),
    'led':emit('17 · 뉴트럴 화이트 라인 조명','#F4F8F9',5),
    'display':emit('18 · 디스플레이 글자','#BDE5EB',2),
    'active':emit('19 · 선택 버튼 링','#A5D9D6',2.5),
    'amber':emit('20 · 비상 버튼 표시','#E2B16E',1),
    'step':material('21 · 계단 디딤면','#A4AEAF',.66,0,.08,8),
    'sky':emit('22 · 창밖 하늘','#CBDDE5',.55),
    'building':material('23 · 창밖 건물','#A8BBC4',.85),
    'building2':material('24 · 창밖 건물 밝은면','#D4DFE1',.8),
}
M['glass'].node_tree.nodes.get('Principled BSDF').inputs['Transmission Weight'].default_value=1
M['glass'].node_tree.nodes.get('Principled BSDF').inputs['IOR'].default_value=1.45
M['brushed'].node_tree.nodes.get('Principled BSDF').inputs['Anisotropic'].default_value=.55

def finish(o,name,mat=None):
    o.name=name
    move(o)
    if mat: o.data.materials.append(M[mat] if isinstance(mat,str) else mat)
    return o
def box(name,loc,size,mat,bev=.015):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=finish(bpy.context.object,name,mat)
    o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bev:
        mod=o.modifiers.new('얕은 모서리 베벨','BEVEL');mod.width=bev;mod.segments=3
        mod=o.modifiers.new('면 노멀','WEIGHTED_NORMAL')
    return o
def cyl(name,loc,radius,depth,mat,axis='Z',vertices=24,bev=.005):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=loc)
    o=finish(bpy.context.object,name,mat)
    if axis=='Y': o.rotation_euler.x=math.pi/2
    if axis=='X': o.rotation_euler.y=math.pi/2
    for p in o.data.polygons: p.use_smooth=True
    if bev:
        mod=o.modifiers.new('Rim bevel','BEVEL');mod.width=bev;mod.segments=2
        o.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
    return o
def rod(name,a,b,r,mat='rail'):
    a,b=Vector(a),Vector(b)
    o=cyl(name,(a+b)/2,r,(b-a).length,mat)
    o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    return o
def sphere(name,loc,size,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,location=loc)
    o=finish(bpy.context.object,name,mat);o.scale=size
    for p in o.data.polygons:p.use_smooth=True
    return o
FONT=bpy.data.fonts.load('C:/Windows/Fonts/malgun.ttf')
FONT_LATIN=bpy.data.fonts.load('C:/Windows/Fonts/segoeui.ttf')
def text(name,body,loc,size,mat='charcoal',face='front',align='CENTER',latin=False):
    cr=bpy.data.curves.new(name,'FONT');cr.body=body;cr.size=size
    cr.align_x=align;cr.align_y='CENTER';cr.font=FONT_LATIN if latin else FONT
    cr.extrude=.0005;cr.bevel_depth=.0002;cr.space_character=1.1
    o=bpy.data.objects.new(name,cr);COL[ACTIVE].objects.link(o);o.location=loc
    if face=='front':o.rotation_euler=(math.pi/2,0,0) # normal -Y
    elif face=='back':o.rotation_euler=(math.pi/2,0,math.pi) # normal +Y
    elif face=='left':o.rotation_euler=(math.pi/2,0,math.pi/2) # normal +X
    elif face=='right':o.rotation_euler=(math.pi/2,0,-math.pi/2) # normal -X
    cr.materials.append(M[mat]);return o
def area(name,loc,target,power,size,color=(1,1,1),size_y=None):
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='RECTANGLE'
    d.size=size;d.size_y=size_y or size;d.color=color
    o=bpy.data.objects.new(name,d);COL['lights'].objects.link(o);o.location=loc
    o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
    return o
def camera(name,loc,target,lens=24):
    d=bpy.data.cameras.new(name);o=bpy.data.objects.new(name,d);COL['cameras'].objects.link(o)
    o.location=loc;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
    d.lens=lens;d.clip_start=.06;d.clip_end=250;d.passepartout_alpha=1
    return o
def animate_slide(obj,delta):
    start=obj.location.copy()
    for fr,factor in [(1,0),(24,0),(52,1),(96,1),(124,0),(144,0)]:
        obj.location=start+Vector(delta)*factor
        obj.keyframe_insert(data_path='location',frame=fr,group='자동문 슬라이드')
    obj['사용법']='1~24 닫힘 / 52~96 열림 / 124~144 닫힘'
    obj.location=start

# Continuous base, individual large-format porcelain tiles and graphite borders.
ACTIVE='floor'
box('로비 구조 바닥',(0,-.9,-.16),(13.6,9.2,.3),'charcoal',.04)
for ix in range(11):
    for iy in range(7):
        box('로비 석재 타일 %02d-%02d'%(ix,iy),(-6+ix*1.2,-4.7+iy*1.2,.005),(1.192,1.192,.05),'stone',.006)
for x in [-6.43,6.43]:box('바닥 외곽 차콜 띠',(x,-1.1,.033),(.12,8.5,.015),'charcoal',.004)
for y in [-5.0,3.08]:box('바닥 경계선',(0,y,.035),(12.9,.08,.016),'charcoal',.003)
# Tactile steel dots in front of both elevator thresholds.
for cx in [-1.8,1.55]:
    for i in range(10):
        for j in range(3):cyl('점형 유도 블록 · 스테인리스',(cx-.495+i*.11,2.48+j*.11,.046),.014,.012,'silver',vertices=12,bev=.002)

# Facade built around true openings; the open lift has a real walk-in cabin.
ACTIVE='walls'
wall_y=3.32
for x,w in [(-5.18,2.84),(-.13,1.1),(3.13,1.1)]:
    box('엘리베이터 전면 벽체',(x,wall_y,1.8),(w,.28,3.6),'wall',.02)
for cx in [-1.8,1.55]:box('엘리베이터 상부 벽체',(cx,wall_y,3.18),(2.25,.28,.84),'wall',.015)
for x in [-6.59,-5.25,-3.82,-.71,.46,2.72,3.68]:
    box('석재 패널 세로 줄눈',(x,3.164,1.83),(.009,.012,3.51),'charcoal',.001)
for z in [.2,2.97]:box('석재 패널 가로 줄눈',(-1.47,3.165,z),(10.2,.01,.008),'charcoal',.001)
box('오른쪽 계단실 벽',(6.69,.12,3.6),(.24,11,7.2),'wall',.02)
for y in [-4,-2,0,2,4]:box('계단 벽 패널 이음',(6.56,y,3.5),(.014,.012,7),'stone',.001)
box('계단실 끝 벽',(5.13,5.48,3.6),(3.35,.22,7.2),'wall',.025)
box('계단 진입 낮은 걸레받이',(6.54,-2.95,.11),(.035,4.1,.18),'charcoal',.008)
box('전면 걸레받이 왼쪽',(-5.15,3.15,.12),(2.9,.045,.18),'charcoal',.008)

ACTIVE='elevator'
for index,cx in enumerate([-1.8,1.55],1):
    # Flat charcoal inset with satin steel portal, crisp reveal and threshold grooves.
    for dx in [-1.3,1.3]:box('Lift %d · 차콜 포털 옆판'%index,(cx+dx,3.145,1.48),(.18,.09,2.94),'charcoal',.012)
    box('Lift %d · 차콜 포털 윗판'%index,(cx,3.145,2.87),(2.78,.09,.16),'charcoal',.012)
    for dx in [-1.16,1.16]:box('Lift %d · 실버 문틀'%index,(cx+dx,3.065,1.39),(.14,.19,2.78),'brushed',.015)
    box('Lift %d · 실버 상인방'%index,(cx,3.06,2.79),(2.46,.2,.15),'silver',.014)
    box('Lift %d · 문턱'%index,(cx,3.2,.052),(2.48,.44,.055),'silver',.008)
    for y in [3.05,3.18,3.29]:box('문턱 슬라이드 레일',(cx,y,.083),(2.35,.009,.004),'black',.001)
    box('Lift %d · 층 표시창'%index,(cx,3.04,3.025),(.86,.045,.22),'black',.012)
    text('Lift %d · 층수'%index,'02',(cx-.06,3.011,3.03),.147,'display',latin=True)
    text('Lift %d · 방향'%index,'▲' if index==1 else '▼',(cx+.25,3.01,3.03),.083,'display',latin=True)
    text('Lift %d · 호기 표기'%index,'0%d'%index,(cx-1.01,2.954,2.8),.062,'charcoal',latin=True)
    ACTIVE='doors'
    for side in [-1,1]:
        leaf=box('Lift %d · %s 문 [슬라이드]'%(index,'왼쪽' if side<0 else '오른쪽'),(cx+side*.55,3.23,1.405),(1.094,.07,2.64),'brushed',.006)
        leaf['문 역할']='독립 편집 가능한 센터 오픈 문짝'
        if index==1:animate_slide(leaf,(side*1.1,0,0))
    ACTIVE='elevator'

# Single, accessible call station centered between the banks.
box('층 안내 차콜 패널',(-.12,3.135,2.34),(.69,.075,.69),'charcoal',.01)
text('층 번호 대형','2',(-.12,3.09,2.35),.46,'white',latin=True)
box('공용 호출 패널',(-.12,3.08,1.28),(.2,.08,.53),'silver',.02)
for z,txt in [(1.4,'▲'),(1.18,'▼')]:
    cyl('호출 버튼 외곽',(-.12,3.025,z),.058,.022,'rail',axis='Y',vertices=32)
    cyl('호출 버튼 면',(-.12,3.008,z),.044,.012,'black',axis='Y',vertices=32)
    text('호출 버튼 문자',txt,(-.12,2.998,z),.045,'display',latin=True)
text('호출 안내','엘리베이터',(-.12,3.092,1.82),.075,'charcoal')

# Primary cabin, pale stone, segmented silver walls, mirror, rails, readable buttons.
ACTIVE='cabin'
cx=-1.8
box('Cabin · 하부 구조',(cx,4.82,-.09),(3.14,3.18,.22),'charcoal',.025)
box('Cabin · 석재 중앙 바닥',(cx,4.83,.035),(2.68,2.85,.06),'stone',.01)
for x in [cx-1.43,cx+1.43]:box('Cabin · 바닥 차콜 테두리',(x,4.83,.055),(.18,3.02,.025),'charcoal',.005)
for y in [3.43,6.27]:box('Cabin · 문앞/뒤 바닥 테두리',(cx,y,.055),(2.82,.16,.025),'charcoal',.006)
for x in [cx-1.54,cx+1.54]:
    box('Cabin · 측면 벽 구조',(x,4.87,1.5),(.12,3.15,3),'charcoal',.01)
    for i in range(4):box('Cabin · 실버 측벽 패널',(x+(.071 if x<cx else -.071),3.72+i*.73,1.51),(.024,.715,2.87),'brushed',.005)
    box('Cabin · 측면 걸레받이',(x+(.092 if x<cx else -.092),4.88,.19),(.025,3.03,.22),'charcoal',.007)
box('Cabin · 뒤 벽 구조',(cx,6.44,1.5),(3.18,.14,3),'charcoal',.015)
for i in range(3):box('Cabin · 뒤 벽 실버 패널',(cx-.99+i*.99,6.356,1.5),(.974,.026,2.87),'silver',.004)
box('Cabin · 거울 프레임',(cx,6.32,1.91),(2.66,.05,1.34),'charcoal',.018)
box('Cabin · 뒤쪽 대형 거울',(cx,6.285,1.91),(2.58,.025,1.26),'mirror',.008)
for x in [cx-1.38,cx+1.38]:
    rod('Cabin · 측벽 손잡이',(x,3.85,1.02),(x,6.12,1.02),.031)
    for y in [4.0,5.85]:rod('Cabin · 손잡이 벽 브래킷',(x,y,1.02),(x+(-.08 if x<cx else .08),y,1.02),.018)
rod('Cabin · 뒤쪽 손잡이',(cx-1.3,6.14,1.02),(cx+1.3,6.14,1.02),.032)
for x in [cx-1.12,cx+1.12]:rod('Cabin · 뒤 손잡이 브래킷',(x,6.14,1.02),(x,6.31,1.02),.018)
box('Cabin · 천장',(cx,4.88,3.02),(3.13,3.15,.12),'white',.015)
box('Cabin · 천장 차콜 매입',(cx,4.87,2.949),(2.86,2.92,.04),'charcoal',.012)
for x in [cx-1.25,cx+1.25]:
    box('Cabin · 천장 라인 LED',(x,4.87,2.915),(.09,2.62,.025),'led',.008)
    area('Cabin · 실제 면광원',(x,4.87,2.88),(x,4.87,0),72,.15,size_y=2.5)
for i in range(10):box('Cabin · 환기 슬릿',(cx-.36+i*.08,6.05,2.914),(.032,.28,.01),'black',.002)
# Front-facing control console is beside the cabin entrance; it faces into the cabin.
px=cx+1.3
box('Cabin · 버튼 콘솔 전면판',(px,3.55,1.52),(.32,.09,2.2),'charcoal',.018)
box('Cabin · 버튼 콘솔 실버 테두리',(px,3.602,1.52),(.342,.018,2.23),'silver',.016)
box('Cabin · 버튼 콘솔 면',(px,3.618,1.52),(.305,.022,2.19),'charcoal',.012)
box('Cabin · 내부 디스플레이',(px,3.638,2.34),(.24,.015,.27),'black',.008)
text('Cabin · 디스플레이','02',(px,3.65,2.35),.155,'display',face='back',latin=True)
text('Cabin · 용량 안내','1000 kg\n13인승',(px,3.65,2.06),.04,'white',face='back')
for i in range(12):
    floor=i+1; row=i//2;col=i%2
    x=px+(-.072 if col==0 else .072);z=.93+row*.145
    cyl('Cabin · %02d층 버튼 링'%floor,(x,3.65,z),.048,.025,'active' if floor==2 else 'rail',axis='Y',vertices=24)
    cyl('Cabin · %02d층 버튼 면'%floor,(x,3.668,z),.036,.013,'black',axis='Y',vertices=24)
    text('Cabin · %02d층 숫자'%floor,str(floor),(x,3.68,z),.042,'white',face='back',latin=True)
for x,body in [(px-.072,'◀▶'),(px+.072,'▶◀')]:
    cyl('Cabin · 문 열림 닫힘',(x,3.65,.72),.048,.025,'rail',axis='Y')
    text('Cabin · 개폐 아이콘',body,(x,3.67,.72),.027,'charcoal',face='back',latin=True)
cyl('Cabin · 비상 호출',(px,3.65,.52),.05,.025,'amber',axis='Y')
text('Cabin · 비상 표기','비상',(px,3.676,.52),.029,'charcoal',face='back')
for i in range(3):
    for j in range(3):cyl('Cabin · 스피커 구멍',(px-.045+i*.045,3.65,1.85+j*.035),.006,.009,'black',axis='Y',vertices=8,bev=0)
# Poster on the opposite wall, built with editable type and geometry.
box('Cabin · 안전 안내 프레임',(cx-1.448,4.26,1.94),(.06,.64,.85),'charcoal',.014)
box('Cabin · 안전 안내 종이',(cx-1.41,4.26,1.94),(.018,.575,.78),'white',.007)
text('Cabin · 안내 제목','안전한 이용',(cx-1.394,4.26,2.18),.065,'charcoal',face='left')
text('Cabin · 안내 본문','문이 열린 뒤\n천천히 이동하세요\n\n비상시 호출 버튼',(cx-1.392,4.26,1.95),.04,'charcoal',face='left')
text('Cabin · 안내 하단','SAFETY FIRST',(cx-1.392,4.26,1.67),.032,'charcoal',face='left',latin=True)
# An inner front header and independently editable trim.
box('Cabin · 출입구 내부 상부',(cx,3.52,2.84),(2.98,.1,.2),'silver',.009)
text('Cabin · 출구 표기','02  /  LOBBY',(cx,3.584,2.83),.075,'charcoal',face='back',latin=True)

# Floor-to-ceiling left glazing, low sill and finely spaced silver mullions.
ACTIVE='windows'
box('통창 하부 턱',(-6.62,-.94,.13),(.22,8.78,.22),'white',.015)
box('통창 상부 프레임',(-6.62,-.94,3.52),(.18,8.78,.15),'silver',.01)
for i in range(6):
    y=-5.22+i*1.71
    box('왼쪽 통창 세로 프레임 %02d'%i,(-6.62,y,1.83),(.14,.075,3.36),'silver',.01)
for i in range(5):
    y=-4.365+i*1.71
    box('왼쪽 창 유리 %02d'%i,(-6.64,y,1.83),(.025,1.63,3.26),'glass',.002)
    box('통창 가로 분할',(-6.57,y,2.65),(.075,1.64,.04),'silver',.005)
    box('유리 안전 표시 띠',(-6.614,y,1.04),(.012,1.63,.034),'frost',.001)

# Straight staircase, rise 180 mm/run 280 mm, 19 risers and a genuine upper landing.
ACTIVE='stairs'
sx=5.18;start=-1.72;rise=.18;run=.28;n=19
box('계단 하부 진입석',(sx,start-.42,.04),(2.35,.72,.06),'step',.012)
for i in range(n):
    y=start+(i+.5)*run;z=(i+1)*rise
    box('계단 %02d · 디딤판'%(i+1),(sx,y,z-.05),(2.34,run+.014,.1),'step',.012)
    box('계단 %02d · 챌판'%(i+1),(sx,start+i*run+.015,z-.105),(2.32,.045,.16),'wall',.005)
    box('계단 %02d · 미끄럼방지 실버'%(i+1),(sx,start+i*run+.042,z+.004),(2.24,.032,.01),'silver',.003)
# Twin stringers under the steps.
for x in [sx-.96,sx+.96]:
    beam=box('계단 사선 구조 스트링거',(x,start+n*run/2-.1,n*rise/2-.2),(.12,math.hypot(n*run,n*rise),.23),'charcoal',.015)
    beam.rotation_euler.x=math.atan2(n*rise,n*run)
    for i in [0,3,6,9,12,15,18]:
        y=start+(i+.6)*run;z=(i+1)*rise
        rod('계단 난간 포스트',(x+(.15 if x>sx else -.15),y,z),(x+(.15 if x>sx else -.15),y,z+1.03),.022)
        cyl('계단 난간 발판',(x+(.15 if x>sx else -.15),y,z+.012),.065,.025,'silver',vertices=24)
    for h in [.45,1.03]:
        rod('계단 연속 손잡이',(x+(.15 if x>sx else -.15),start+.05,rise+h),(x+(.15 if x>sx else -.15),start+18.85*run,n*rise+h),.032 if h>1 else .016)
    # Thin clear balustrade panels follow the stairs.
    for i in range(6):
        y0=start+(i*3+.6)*run;y1=start+(i*3+3.6)*run
        z0=(i*3+1)*rise+.15;z1=(i*3+4)*rise+.15
        xx=x+(.15 if x>sx else -.15)
        mesh=bpy.data.meshes.new('난간 안전유리 메시')
        mesh.from_pydata([(xx,y0,z0),(xx,y1,z1),(xx,y1,z1+.72),(xx,y0,z0+.72)],[],[(0,1,2,3)])
        o=bpy.data.objects.new('계단 투명 안전유리',mesh);COL[ACTIVE].objects.link(o);mesh.materials.append(M['glass'])
        mod=o.modifiers.new('안전유리 두께','SOLIDIFY');mod.thickness=.012
top=start+n*run
ACTIVE='upper'
box('계단 상부 랜딩',(5.18,4.48,3.33),(2.65,1.76,.18),'step',.02)
box('위층 복도 이어짐',(2.18,4.94,3.33),(3.42,.85,.18),'stone',.02)
for x in [3.87,6.49]:
    for y in [3.7,4.5,5.24]:rod('상부 난간 기둥',(x,y,3.43),(x,y,4.46),.022)
    rod('상부 난간 손잡이',(x,3.63,4.46),(x,5.25,4.46),.032)
    rod('상부 난간 하부',(x,3.63,3.87),(x,5.25,3.87),.015)
box('계단실 상부 창 프레임',(6.54,1.1,5.59),(.1,5.8,1.65),'charcoal',.012)
box('계단실 상부 창',(6.48,1.1,5.59),(.025,5.64,1.49),'glass',.005)
for y in [-.75,1.1,2.95]:box('계단실 상부 창 분할',(6.44,y,5.59),(.06,.055,1.5),'silver',.004)
text('계단실 층 안내','03',(6.555,4.2,4.86),.62,'charcoal',face='right',latin=True)
text('계단실 층 안내 영문','UPPER FLOOR',(6.554,4.2,4.39),.105,'charcoal',face='right',latin=True)
ACTIVE='stairs'
text('계단 하부 층 안내','계단  /  3F ↑',(6.55,-2.28,1.83),.19,'charcoal',face='right')
for z in [.17,3.5]:box('계단 벽 수평 마감',(6.555,.1,z),(.03,10.55,.045),'silver',.004)

# Rear entrance: paired automatic glass leaves, fixed sidelights, sensor and track.
ACTIVE='rear'
back=-5.34;dcx=-.6
box('뒤쪽 왼쪽 벽',(-5.0,back,1.8),(3.4,.25,3.6),'wall',.018)
box('뒤쪽 오른쪽 벽',(4.4,back,1.8),(4.6,.25,3.6),'wall',.018)
for x in [dcx-2.65,dcx+2.65]:box('자동문 외곽 세로 프레임',(x,back,1.64),(.1,.19,3.22),'silver',.009)
for x in [dcx-1.3,dcx+1.3]:box('자동문 고정창 프레임',(x,back,1.58),(.065,.14,3.1),'silver',.006)
for side in [-1,1]:
    x=dcx+side*1.98
    box('자동문 옆 고정 유리',(x,back,1.62),(1.26,.025,3.1),'glass',.002)
    box('자동문 고정창 안전 띠',(x,back+.018,1.05),(1.26,.012,.055),'frost',.001)
box('자동문 상단 구동부',(dcx,back,3.25),(5.43,.28,.27),'charcoal',.012)
box('자동문 구동부 실버 커버',(dcx,back+.15,3.26),(5.33,.025,.22),'silver',.007)
box('자동문 상부 벽',(dcx,back,3.55),(5.44,.25,.3),'wall',.012)
box('자동문 감지 센서',(dcx,back+.18,3.18),(.2,.13,.07),'black',.016)
for side in [-1,1]:
    parent=bpy.data.objects.new('자동문 %s 문짝 [애니메이션]'%('왼쪽' if side<0 else '오른쪽'),None)
    COL['rear'].objects.link(parent)
    parts=[];x=dcx+side*.64
    parts.append(box('자동문 이동 유리',(x,back-.06,1.59),(1.27,.025,3.03),'glass',.002))
    for dx in [-.632,.632]:parts.append(box('자동문 이동 세로틀',(x+dx,back-.06,1.59),(.042,.075,3.08),'silver',.005))
    for z in [.08,3.1]:parts.append(box('자동문 이동 가로틀',(x,back-.06,z),(1.3,.075,.065),'silver',.006))
    parts.append(box('자동문 안전 띠',(x,back-.037,1.05),(1.25,.012,.055),'frost',.001))
    label=text('자동문 유리 안내','자동문',(x,back+.001,1.32),.12,'charcoal',face='back')
    parts.append(label)
    for part in parts:part.parent=parent
    animate_slide(parent,(side*1.3,0,0))
box('자동문 입구 매트',(dcx,-4.6,.048),(3.05,1.15,.025),'charcoal',.025)
for i in range(27):box('입구 매트 리브',(dcx-1.4+i*.108,-4.6,.064),(.025,1.07,.008),'fabric',.002)
box('자동문 문턱',(dcx,back,.04),(5.43,.34,.045),'silver',.008)
box('비상 출구 표지',(dcx,back+.18,3.53),(.84,.06,.18),'charcoal',.008)
text('출입구 표기','출입구  /  EXIT',(dcx,back+.222,3.53),.09,'white',face='back')
box('외부 진입 테라스',(dcx,-6.5,-.03),(6.0,2.22,.1),'stone',.025)

# Furnishings, designed as separate editable parts.
ACTIVE='furniture'
# Window-side bench, length along the glazing.
bx=-5.66;by=-.65
box('벤치 하부 프레임',(bx,by,.38),(.73,2.9,.13),'charcoal',.04)
for yy in [by-.98,by+.98]:
    box('벤치 알루미늄 다리',(bx,yy,.21),(.55,.085,.38),'silver',.025)
    box('벤치 다리 받침',(bx,yy,.054),(.64,.18,.04),'charcoal',.01)
for i in range(3):
    box('벤치 좌방석 %d'%i,(bx,by-.94+i*.94,.51),(.79,.91,.2),'fabric',.075)
    box('벤치 등방석 %d'%i,(bx-.31,by-.94+i*.94,.83),(.17,.91,.55),'fabric',.06)
for yy in [by-1.43,by+1.43]:rod('벤치 팔걸이',(bx-.2,yy,.72),(bx+.35,yy,.72),.027)

def leaf_mesh(name,start,end,width,mat):
    a,b=Vector(start),Vector(end);length=(b-a).length
    verts=[];faces=[]
    for i in range(7):
        t=i/6;w=math.sin(math.pi*t)**.75*width
        for side in [-1,0,1]:verts.append((side*w,t*length,math.sin(math.pi*t)*length*.15-abs(side)*w*.16))
    for i in range(6):
        for j in range(2):faces.append((i*3+j,i*3+j+1,(i+1)*3+j+1,(i+1)*3+j))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    o=bpy.data.objects.new(name,mesh);COL['furniture'].objects.link(o);mesh.materials.append(M[mat])
    o.location=a;o.rotation_euler=(b-a).to_track_quat('Y','Z').to_euler()
    for p in mesh.polygons:p.use_smooth=True
    mod=o.modifiers.new('잎 두께','SOLIDIFY');mod.thickness=.0015
    return o
def plant(name,x,y,height=1.8):
    # Tapered ceramic pot with an actual open top, rim, soil and branching foliage.
    bpy.ops.mesh.primitive_cone_add(vertices=48,radius1=.29,radius2=.36,depth=.66,location=(x,y,.38))
    o=finish(bpy.context.object,name+' · 차콜 화분','charcoal')
    be=o.modifiers.new('화분 둥근 모서리','BEVEL');be.width=.025;be.segments=3
    for p in o.data.polygons:p.use_smooth=True
    cyl(name+' · 화분 실버 밴드',(x,y,.72),.365,.035,'silver',vertices=48)
    cyl(name+' · 흙',(x,y,.737),.337,.014,'soil',vertices=48)
    rod(name+' · 중심 가지',(x,y,.74),(x+.025,y,height),.018,'stem')
    for i in range(22):
        angle=i*2.39996;z=.84+(i/22)*(height-.75)
        reach=.29+random.random()*.3
        origin=Vector((x,y,z));tip=Vector((x+math.cos(angle)*reach,y+math.sin(angle)*reach,z+.08+random.random()*.15))
        rod(name+' · 잎 가지',origin,origin.lerp(tip,.38),.005,'stem')
        leaf_mesh(name+' · 잎',origin.lerp(tip,.25),tip,.095+random.random()*.045,'leaf' if i%3 else 'leaf2')
plant('통창 옆 식물',-5.47,2.21,2.14)
plant('계단 옆 식물',3.33,2.48,1.68)
# Large graphic hierarchy at the left wall.
text('로비 월 그래픽 층수','02',(-5.02,3.155,2.38),.91,'charcoal',latin=True)
text('로비 월 그래픽 영문','ELEVATOR LOBBY',(-5.01,3.151,1.72),.112,'charcoal',latin=True)
box('로비 안내판',(-4.99,3.12,1.15),(1.3,.055,.59),'charcoal',.012)
text('안내판 제목','층별 안내',(-5.49,3.081,1.32),.08,'white',align='LEFT')
text('안내판 내용','2F  로비 · 휴게 공간\n3F  계단으로 이동',(-5.49,3.079,1.12),.065,'white',align='LEFT')
# Slim bin alongside the closed lift, kept clear of the walking route.
box('슬림 휴지통',(3.15,2.76,.43),(.38,.38,.8),'charcoal',.035)
box('휴지통 실버 윗면',(3.15,2.76,.84),(.39,.39,.035),'silver',.01)
box('휴지통 투입구',(3.15,2.71,.862),(.25,.12,.012),'black',.016)
text('휴지통 표기','RECYCLE',(3.15,2.558,.59),.038,'white',latin=True)
# Rear-wall art: restrained three-dimensional line composition in the reference palette.
box('입구 벽 아트 프레임',(3.75,-5.18,1.96),(1.6,.055,1.0),'charcoal',.015)
box('입구 벽 아트 바탕',(3.75,-5.14,1.96),(1.52,.02,.92),'white',.006)
for i in range(5):
    ob=box('입구 아트 실버 라인',(3.23+i*.25,-5.12,1.96),(.022,.012,.53+random.random()*.22),'silver',.003)
    ob.rotation_euler.y=.22
text('입구 아트 제목','A T R I U M',(3.75,-5.105,1.62),.065,'charcoal',face='back',latin=True)

# Suspended ceiling covers the main lobby; the staircase bay has full headroom.
ACTIVE='ceiling'
box('로비 천장 구조',(-1.45,-.94,3.72),(10.3,8.78,.17),'white',.018)
for y in [-4.05,-1.85,.4,2.55]:
    box('천장 라인 조명 매입 홈',(-1.45,y,3.614),(9.7,.15,.065),'charcoal',.009)
    box('천장 연속 라인 LED',(-1.45,y,3.574),(9.45,.065,.014),'led',.006)
    area('로비 천장 실제 조명',(-1.45,y,3.55),(-1.45,y,0),160,8.6,size_y=.22)
for x in [-5.9,3.03]:box('천장 측면 그림자 틈',(x,-.94,3.617),(.035,8.5,.02),'charcoal',.002)
box('천장 계단 경계 보',(3.73,-.94,3.66),(.17,8.85,.27),'charcoal',.014)
ACTIVE='upper'
box('계단실 높은 천장',(5.18,.09,7.22),(3.12,10.95,.16),'white',.018)
for y in [-3,0,3.5]:
    box('계단실 높은 라인 LED',(5.17,y,7.118),(2.28,.07,.02),'led',.006)
    area('계단실 상부 조명',(5.17,y,7.08),(5.17,y,1),140,2.1,size_y=.3)

# Quiet skyline beyond the left glazing; no external images or dependencies.
ACTIVE='outside'
box('창밖 하늘 바탕',(-23,0,9),(.1,80,40),'sky',0)
box('창밖 지면',(-15,0,-.72),(25,80,.18),'building2',.015)
for i in range(8):
    y=-18+i*5.4;h=random.uniform(4,12);xx=random.uniform(-15,-11)
    box('창밖 건물 %02d'%i,(xx,y,h/2-.6),(random.uniform(2,3.5),3.5,h),'building' if i%2 else 'building2',.08)
    for z in range(1,int(h)):
        box('창밖 건물 수평 띠',(xx+1.78,y,z-.6),(.03,3.43,.032),'white',.002)
for y in [-3,2.0]:
    rod('외부 조경 나무',(-9,y,-.5),(-9,y,1.7),.08,'stem')
    for i in range(5):sphere('외부 조경 잎',(-9+random.uniform(-.5,.5),y+random.uniform(-.5,.5),1.6+random.random()*.9),(.65,.65,.8),'leaf2')
area('좌측 통창 자연광',(-6.2,-.7,3.0),(-1,.4,1.0),1500,6.8,color=(.85,.93,1),size_y=3)
area('입구 자연광',(-.6,-6.2,2.4),(-.5,1,1.2),750,4.5,color=(.92,.96,1),size_y=3)
area('계단실 상부 창 자연광',(6.2,.8,5.55),(3,.8,1.5),500,4,color=(.87,.94,1),size_y=1.3)
sun_d=bpy.data.lights.new('맑은 오후 햇빛','SUN');sun_d.energy=1.2;sun_d.angle=math.radians(9)
sun=bpy.data.objects.new('맑은 오후 햇빛',sun_d);COL['lights'].objects.link(sun);sun.rotation_euler=(math.radians(29),math.radians(-28),math.radians(-66))

# Named editorial views. Default: standing inside the rear entrance, facing the lifts.
hero=camera('01 · 로비 / 엘리베이터와 우측 계단',(.0,-4.72,1.96),(-.25,2.4,1.63),20)
interior=camera('02 · 엘리베이터 내부',(cx-.3,6.08,1.72),(cx+.04,3.24,1.52),17)
rear=camera('03 · 뒤쪽 자동문 / 왼쪽 창문',(2.8,2.5,2.0),(-1.15,-4.0,1.6),22)
overview=camera('04 · 전체 배치 / 천장 숨김',(16,-20,18),(0,.5,1.1),40)
overview.data.type='ORTHO';overview.data.ortho_scale=23
for label,frame in [('문 닫힘',1),('문 열림',52),('문 열린 상태',72),('문 닫힘 복귀',144)]:S.timeline_markers.new(label,frame=frame)
S.camera=hero;S.frame_set(72)
S['구성']='중앙 엘리베이터 2기 / 주 엘리베이터 실내 / 우측 상승 계단 / 좌측 통창 / 뒤쪽 자동문'
S['색상 테마']='사용자 오른쪽 참고 사진: 라이트 그레이 석재 + 실버 스테인리스 + 차콜'
S['방향']='+Z 위 / 메인 카메라 기준 +X 오른쪽 계단, -X 왼쪽 창문, -Y 뒤쪽 자동문'
S['계단 규격']='19단, 단높이 0.18 m, 디딤깊이 0.28 m, 폭 2.34 m, 상층 높이 3.42 m'
S['참고']='모델 및 문 개폐 시각화. 런타임 상호작용 및 엘리베이터 수직 이동은 별도 구현.'
readme='''실버 아트리움 · 엘리베이터 맵

Blender 5.1 / 미터 단위 / Z축 위 / 모든 재질과 한글 폰트 내장
오른쪽 참고 사진의 밝은 회색, 실버, 차콜로 로비와 내부를 통일했습니다.

배치: 중앙 엘리베이터 2기, 우측 올라가는 계단, 좌측 통창, 뒤쪽 자동문.
왼쪽 01호기에는 직접 들어갈 수 있는 내부가 있습니다.
오른쪽 02호기는 닫힌 외장 모델이며 내부는 없습니다.
로비 약 13.2 x 8.5 m, 주 엘리베이터 실내 약 3.0 x 3.0 x 2.9 m.
계단은 19단, 단높이 0.18 m, 상층 높이 3.42 m.

컬렉션을 부위별로 분리했습니다. 문, 버튼, 난간, 안내판 등을 따로 편집할 수 있습니다.
기본 72프레임은 문이 열린 모습입니다.
타임라인: 1~24 닫힘 → 52~96 열림 → 124~144 닫힘.
주 엘리베이터의 좌우 문짝과 뒤쪽 유리 자동문에 개폐 키프레임이 있습니다.
한글 안내문은 편집 가능한 텍스트입니다. 외부 이미지나 링크 라이브러리가 필요 없습니다.

카메라 01 로비 / 02 내부 / 03 뒤쪽 자동문 / 04 전체 배치.
전체 배치 이미지 촬영 때는 컬렉션 10(로비 천장)과 11의 높은 천장 및
상부 난간 일부, 우측·뒤쪽 계단실 벽을 잠시 숨겨 내부 배치를 보여줬습니다.
기본 파일에는 천장과 모든 벽이 있습니다.

문 개폐는 Blender 시각화용 애니메이션입니다.
게임 상호작용, 충돌체, 엘리베이터 수직 운행은 포함하지 않습니다.
'''
bpy.data.texts.new('읽어주세요.txt').write(readme)
(WORK/'읽어주세요.txt').write_text(readme,encoding='utf-8')
FONT.pack();FONT_LATIN.pack()
bpy.ops.file.pack_all()
for o in bpy.context.selected_objects:o.select_set(False)
for screen in bpy.data.screens:
    for a in screen.areas:
        if a.type=='VIEW_3D':
            a.spaces.active.clip_end=250
            a.spaces.active.region_3d.view_perspective='CAMERA'
            a.spaces.active.shading.type='MATERIAL'
            a.spaces.active.overlay.show_extras=False
bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT),compress=True)
print('SAVED',str(OUTPUT),flush=True)
validation={'blender':bpy.app.version_string,'file':str(OUTPUT),'objects':len(S.objects),'meshes':sum(o.type=='MESH' for o in S.objects),'materials':len(bpy.data.materials),'fontsPacked':all(f.packed_file for f in [FONT,FONT_LATIN]),'renderDevices':device_info,'unit':'metres','defaultFrame':72,'externalLibraries':len(bpy.data.libraries)}
(WORK/'Build_Validation.json').write_text(json.dumps(validation,ensure_ascii=False,indent=2),encoding='utf-8')
# Draft previews: retain the final file's higher resolution / Cycles configuration.
S.render.resolution_percentage=70
S.cycles.samples=24
for cam,name in [(hero,'01_로비'),(interior,'02_내부'),(rear,'03_뒤쪽_자동문')]:
    S.camera=cam;S.render.filepath=str(WORK/(name+'_draft.png'))
    bpy.ops.render.render(write_still=True)
print('DRAFTS_COMPLETE',flush=True)
