"""Preserve the ground-floor map, add empty 2F structure and an actual elevated 3F."""
import bpy,math,json,random
from pathlib import Path
from mathutils import Vector
WORK=Path(__file__).resolve().parent
S=bpy.context.scene;S.frame_set(1)
bpy.context.preferences.filepaths.save_version=0
H=3.96;Z3=H*2
OUTPUT=WORK/'엘베_맵_체육센터_3층.blend'
source_objects=list(S.objects)
source_cols={int(c.name[:2]):c for c in bpy.data.collections if c.name[:2].isdigit()}
source_texts={o.name:o.data.body for o in source_objects if o.type=='FONT'}
M={k:next(m for m in bpy.data.materials if m.name.startswith(p)) for k,p in [('stone','01'),('wall','02'),('silver','03'),('brushed','04'),('charcoal','05'),('black','06'),('white','07'),('rail','08'),('glass','09'),('frost','10'),('leaf','13'),('leaf2','14'),('led','17'),('active','19'),('step','21'),('building','23'),('building2','24')]}
FONT=next(f for f in bpy.data.fonts if 'Malgun' in f.name and 'Bold' not in f.name)
BOLD=next(f for f in bpy.data.fonts if 'Malgun' in f.name and 'Bold' in f.name)
def col(name,parent=None):
    c=bpy.data.collections.new(name);(parent or S.collection).children.link(c);return c
F1=col('10 · 1층 · 기존 로비 보존')
for i,c in source_cols.items():
    if i in list(range(1,12))+[13,14]:
        S.collection.children.unlink(c);F1.children.link(c)
F2=col('20 · 2층 · 비워둔 구조')
F3=col('30 · 3층 · 체험 시작 구역')
STRUCT=col('40 · 건물 외장 · 기둥')
STAIR=col('41 · 층간 연결 계단')
COURT=col('32 · 3층 농구 연습장',F3)
HALL=col('31 · 3층 탈의 · 보관 복도',F3)
CAMS=col('50 · 전체 건물 검토 카메라')
ACTIVE=STRUCT
def move(o,c=None):
    for cc in list(o.users_collection):cc.objects.unlink(o)
    (c or ACTIVE).objects.link(o);return o
def finish(o,name,mat):
    o.name=name;move(o)
    if mat:o.data.materials.append(M[mat] if isinstance(mat,str) else mat)
    return o
def box(name,loc,size,mat='wall',bev=.012):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=finish(bpy.context.object,name,mat);o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bev:
        m=o.modifiers.new('얕은 모서리 베벨','BEVEL');m.width=bev;m.segments=2
        o.modifiers.new('면 노멀','WEIGHTED_NORMAL')
    return o
def rod(name,a,b,r=.025,mat='rail'):
    a,b=Vector(a),Vector(b)
    bpy.ops.mesh.primitive_cylinder_add(vertices=20,radius=r,depth=(b-a).length,location=(a+b)/2)
    o=finish(bpy.context.object,name,mat);o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    for p in o.data.polygons:p.use_smooth=True
    return o
def text(name,body,loc,size=.1,mat='charcoal',facing='front',bold=False):
    d=bpy.data.curves.new(name,'FONT');d.body=body;d.font=BOLD if bold else FONT;d.size=size
    d.align_x='CENTER';d.align_y='CENTER';d.extrude=.0003;d.materials.append(M[mat])
    o=bpy.data.objects.new(name,d);ACTIVE.objects.link(o);o.location=loc
    o.rotation_euler=(math.pi/2,0,{'front':0,'back':math.pi,'left':math.pi/2,'right':-math.pi/2}.get(facing,0));return o
def line(name,points,r=.015,mat='white',closed=False):
    d=bpy.data.curves.new(name,'CURVE');d.dimensions='3D';d.bevel_depth=r;d.bevel_resolution=1
    sp=d.splines.new('POLY');sp.points.add(len(points)-1)
    for p,co in zip(sp.points,points):p.co=(*co,1)
    sp.use_cyclic_u=closed
    o=bpy.data.objects.new(name,d);ACTIVE.objects.link(o);d.materials.append(M[mat]);return o
def camera(name,loc,target,lens=28,ortho=None):
    d=bpy.data.cameras.new(name);o=bpy.data.objects.new(name,d);CAMS.objects.link(o);o.location=loc
    o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler();d.lens=lens;d.clip_start=.06;d.clip_end=400
    if ortho:d.type='ORTHO';d.ortho_scale=ortho
    return o
def light(name,loc,target,power,size,size_y=None):
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='RECTANGLE';d.size=size;d.size_y=size_y or size
    o=bpy.data.objects.new(name,d);ACTIVE.objects.link(o);o.location=loc;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
    o.visible_camera=False;o.visible_glossy=False;o.visible_transmission=False;return o
def boolean_opening(wall,name,loc,size):
    cutter=box(name,loc,size,'wall',0)
    m=wall.modifiers.new(name,'BOOLEAN');m.operation='DIFFERENCE';m.solver='EXACT';m.object=cutter
    bpy.context.view_layer.objects.active=wall;wall.select_set(True)
    bpy.ops.object.modifier_apply(modifier=m.name);wall.select_set(False)
    bpy.data.objects.remove(cutter,do_unlink=True)

# Every editable 3F mesh/text is independent of 1F; materials and packed poster are shared.
copies={};copycols={}
copy_keys=list(range(1,12))+[13,14]
for i in copy_keys:copycols[i]=col('3F · '+source_cols[i].name,F3)
for o in source_objects:
    groups=[i for i in copy_keys if o.name in source_cols[i].objects]
    if not groups or (o.type=='LIGHT' and o.data.type=='SUN'):continue
    c=o.copy()
    if o.data:c.data=o.data.copy()
    c.name='3F · '+o.name;c.animation_data_clear()
    for i in groups:copycols[i].objects.link(c)
    copies[o]=c
for o,c in copies.items():
    if o.parent in copies:c.parent=copies[o.parent]
    else:c.parent=None;c.location.z+=Z3
    if o.animation_data:
        start=o.location.copy();start.z+=Z3
        if o.name.startswith('Lift 1 · '):delta=(-.9 if '왼쪽' in o.name else .9,0,0)
        else:delta=(-1.3 if '왼쪽' in o.name else 1.3,0,0)
        for fr,k in [(1,0),(24,0),(52,1),(96,1),(124,0),(144,0)]:
            c.location=start+Vector(delta)*k;c.keyframe_insert(data_path='location',frame=fr)
S.frame_set(1);bpy.context.view_layer.update()
def f3(name):return copies[bpy.data.objects[name]]
def setbody(o,body):o.data.body=body

# Display state and spatial programme.
setbody(f3('Lift 1 · 층수'),'3');setbody(f3('Cabin · 전면 층수'),'3')
setbody(f3('층 번호 대형'),'3');setbody(f3('로비 월 그래픽 층수'),'3F')
setbody(f3('Lift 2 · 층수'),'5');setbody(bpy.data.objects['Lift 2 · 층수'],'5')
setbody(f3('계단실 층 안내'),'4F');setbody(f3('계단 하부 층 안내'),'4F ↑')
setbody(f3('출입구 표기'),'농구장 · 탈의실')
facility_lines='1F  로비 · 주출입구\n2F  준비 중\n3F  농구장 · 탈의실\n4F  운동교실\n5F  체력단련실\n6–12F  문화 · 운영'
for o in [bpy.data.objects['안내판 내용'],f3('안내판 내용')]:o.data.body=facility_lines;o.data.size=.045
for plane in ['전면','측면']:
    for floor in [1,3]:
        ring=f3(f'Cabin · {plane} {floor:02d}층 링');ring.data.materials[0]=M['active'] if floor==3 else M['rail']

# Clear 2F main floor: no furniture, no buttons, no cabin; shaft openings are genuine voids.
ACTIVE=F2
for x,y,w,d in [(-1.47,-1.05,10.26,8.5),(-4.99,4.9,3.22,3.4),(1.75,4.9,3.86,3.4)]:
    box('2F · 빈 층 구조 슬래브',(x,y,H-.06),(w,d,.12),'stone',.01)
for y in [-5.34,3.27]:box('2F · 외곽 보',(-1.47,y,H-.17),(10.34,.17,.22),'charcoal')
for x in [-6.62,3.7]:box('2F · 외곽 보',(x,-1.02,H-.17),(.17,8.7,.22),'charcoal')
for i in range(6):
    yy=-5.2+i*1.7;box('2F · 비어 있는 창 프레임',(-6.62,yy,H+1.75),(.14,.07,3.5),'silver')
for i in range(5):box('2F · 외곽 유리',(-6.65,-4.35+i*1.7,H+1.75),(.025,1.62,3.4),'glass',.002)
for y in [-5.36,3.3]:box('2F · 빈 층 외벽',(-1.46,y,H+1.65),(10.3,.16,3.3),'wall')
for x in [-3.4,-.17,2.96]:box('2F · 승강로 측벽',(x,4.93,H+1.85),(.15,3.38,3.7),'wall')
box('2F · 승강로 뒤벽',(-.2,6.6,H+1.85),(6.55,.15,3.7),'wall')
F2['용도']='장식과 시설 없는 2층 구조만 유지'

# Connect 1F-2F and 3F-4F without changing the existing 19 ground-floor treads.
ACTIVE=STAIR
for offset,prefix,mapfn in [(0,'1F→2F',lambda n:bpy.data.objects[n]),(Z3,'3F→4F',f3)]:
    for j in range(3):
        y=3.6+(j+.5)*.28;z=offset+3.42+(j+1)*.18
        box(prefix+' · 추가 디딤판',(5.18,y,z-.05),(2.34,.294,.10),'step')
        box(prefix+' · 추가 챌판',(5.18,y-.125,z-.105),(2.32,.045,.16),'wall',.004)
        box(prefix+' · 미끄럼 방지',(5.18,y-.10,z+.004),(2.24,.03,.01),'silver',.002)
    landing=mapfn('계단 상부 랜딩');landing.location=(5.18,4.90,offset+H-.09);landing.dimensions=(2.65,.92,.18)
    corridor=mapfn('위층 복도 이어짐');corridor.location.z+=.54
    upper=source_cols[11] if offset==0 else copycols[11]
    for obj in list(upper.objects):
        base=obj.name.removeprefix('3F · ')
        if base.startswith(('상부 난간','상층 복도')):bpy.data.objects.remove(obj,do_unlink=True)
    for xx in [3.87,6.49]:
        rod(prefix+' · 마지막 3단 손잡이',(xx,3.60,offset+3.60+1.03),(xx,4.44,offset+H+1.03),.032)
        for yy in [4.46,5.28]:rod(prefix+' · 랜딩 기둥',(xx,yy,offset+H),(xx,yy,offset+H+1.03),.022)
        # Right-hand landing stays open to the added return stair wing at 2F.
        if offset>0 or xx<5:rod(prefix+' · 랜딩 손잡이',(xx,4.44,offset+H+1.03),(xx,5.3,offset+H+1.03),.032)
    for yy in [4.515,5.355]:
        for xx in [.52,1.63,2.74,3.86]:rod(prefix+' · 복도 난간',(xx,yy,offset+H),(xx,yy,offset+H+1.03),.022)
        rod(prefix+' · 복도 손잡이',(.52,yy,offset+H+1.03),(3.86,yy,offset+H+1.03),.032)

# Side return flight: 2F landing -> 3F landing, physically separate from the original flight.
box('2F · 연결 랜딩',(7.95,4.90,H-.09),(3.0,.92,.18),'step')
box('3F · 연결 랜딩',(7.95,-2.21,Z3-.09),(3.0,.98,.18),'step')
for i in range(22):
    yy=4.44-(i+.5)*.28;z=H+(i+1)*.18
    box('2F→3F · %02d 디딤판'%(i+1),(8.1,yy,z-.05),(2.34,.294,.1),'step')
    box('2F→3F · %02d 챌판'%(i+1),(8.1,yy+.125,z-.105),(2.32,.045,.16),'wall',.004)
    box('2F→3F · %02d 미끄럼방지'%(i+1),(8.1,yy+.10,z+.004),(2.24,.03,.01),'silver',.002)
for xx in [6.99,9.21]:
    rod('2F→3F · 연속 손잡이',(xx,4.40,H+.18+1.03),(xx,-1.70,Z3+1.03),.032)
    rod('2F→3F · 보조 난간',(xx,4.40,H+.18+.45),(xx,-1.70,Z3+.45),.015)
    for i in [0,4,8,12,16,21]:
        yy=4.44-(i+.5)*.28;z=H+(i+1)*.18
        rod('2F→3F · 난간 기둥',(xx,yy,z),(xx,yy,z+1.03),.022)
for yy,zz in [(5.32,H),(-2.67,Z3)]:
    rod('연결부 외곽 손잡이',(6.5,yy,zz+1.03),(9.47,yy,zz+1.03),.032)
    for xx in [6.5,7.5,8.5,9.47]:rod('연결부 외곽 기둥',(xx,yy,zz),(xx,yy,zz+1.03),.022)
boolean_opening(bpy.data.objects['오른쪽 계단실 벽'],'2F · 계단 연결 개구부',(6.69,4.9,H+1.4),(.8,.94,2.8))
boolean_opening(f3('오른쪽 계단실 벽'),'3F · 계단 연결 개구부',(6.69,-2.21,Z3+1.4),(.8,1.01,2.8))

# Building envelope and the visible structural stack between retained 1F and elevated 3F.
ACTIVE=STRUCT
for x in [-6.82,3.77,6.73,9.48]:
    for y in [-5.5,6.64]:box('건물 · 수직 구조 기둥',(x,y,5.85),(.22,.24,11.7),'wall')
for z in [H-.13,Z3-.13,Z3+3.65]:
    for y in [-5.5,6.64]:box('건물 · 층별 수평 띠',(0,y,z),(13.85,.20,.21),'silver')
    box('건물 · 서측 수평 띠',(-6.82,.57,z),(.20,12.3,.21),'silver')
box('계단 증축부 · 바깥 벽',(9.48,1.33,5.95),(.16,8.0,11.9),'wall')
for yy in [-2.6,-.6,1.4,3.4,5.3]:
    box('계단 증축부 · 유리창',(9.38,yy,7.3),(.025,1.82,2.7),'glass',.002)
    box('계단 증축부 · 창틀',(9.34,yy,7.3),(.04,.045,2.76),'silver',.004)
box('계단 증축부 · 뒤벽',(8.05,5.45,5.95),(2.85,.16,11.9),'wall')
box('계단 증축부 · 앞벽',(8.05,-2.8,5.95),(2.85,.16,11.9),'wall')
box('계단 증축부 · 지붕',(8.05,1.33,11.98),(3.0,8.4,.16),'white')
for z in [H,Z3]:box('계단 증축부 · 층 외장',(8.05,-2.9,z-.13),(3.0,.22,.22),'silver')

# 3F automatic door now opens into an indoor sports corridor, never an outdoor drop.
ACTIVE=HALL
terrace=f3('외부 진입 테라스');bpy.data.objects.remove(terrace,do_unlink=True)
box('3F · 체육관 연결 복도 바닥',(0,-7.05,Z3-.09),(13.5,3.5,.18),'stone')
for xx in [-6.68,6.68]:box('3F · 연결 복도 벽',(xx,-7.05,Z3+1.8),(.16,3.5,3.6),'wall')
box('3F · 연결 복도 천장',(0,-7.05,Z3+3.7),(13.5,3.5,.14),'white')
for y in [-6.1,-8.1]:
    box('3F · 복도 라인 조명',(0,y,Z3+3.60),(10.5,.07,.02),'led',.005)
    light('3F · 복도 면광원',(0,y,Z3+3.55),(0,y,Z3),170,9,.25)
for xx in [-5.1,5.1]:
    for i in range(5):
        yy=-8.12+i*.42
        box('3F · 개인 보관함',(xx,yy,Z3+1.0),(.66,.40,1.9),'frost')
        box('3F · 보관함 문',(xx+(.34 if xx<0 else -.34),yy,Z3+1.0),(.025,.355,1.81),'charcoal',.007)
        rod('3F · 보관함 손잡이',(xx+(.365 if xx<0 else -.365),yy-.1,Z3+.85),(xx+(.365 if xx<0 else -.365),yy-.1,Z3+1.13),.012)
    box('3F · 운동화 벤치',(xx+(.95 if xx<0 else -.95),-7.2,Z3+.46),(.62,2.4,.16),'step',.045)
    for yy in [-8.1,-6.3]:box('3F · 벤치 다리',(xx+(.95 if xx<0 else -.95),yy,Z3+.22),(.48,.08,.44),'silver')
text('3F · 탈의 보관 동선','탈의실 · 보관함',(0,-5.58,Z3+3.12),.17,'charcoal','front',True)

# A compact practice half court gives the basketball story a visible origin.
ACTIVE=COURT
wood=bpy.data.materials.new('30 · 밝은 스포츠 마루');wood.use_nodes=True
wood.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.48,.34,.19,1)
wood.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.52
courtmark=bpy.data.materials.new('31 · 코트 청록 라인');courtmark.use_nodes=True
courtmark.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.025,.16,.15,1)
M['courtmark']=courtmark
box('3F · 농구장 바닥 구조',(0,-15.85,Z3-.15),(14.3,14.1,.3),'charcoal')
box('3F · 농구 연습장 마루',(0,-15.85,Z3+.015),(13.8,13.6,.07),wood)
for i in range(23):box('3F · 마루 판재 이음',(0,-22.4+i*.6,Z3+.052),(13.7,.006,.003),'stone',0)
zc=Z3+.063
line('3F · 코트 경계',[(-6.2,-22.0,zc),(6.2,-22.0,zc),(6.2,-9.7,zc),(-6.2,-9.7,zc)],.018,'courtmark',True)
line('3F · 자유투 구역',[(-2.4,-22,zc),(-2.4,-17.4,zc),(2.4,-17.4,zc),(2.4,-22,zc)],.018,'courtmark')
line('3F · 자유투 원',[(1.7*math.cos(t),-17.4+1.7*math.sin(t),zc) for t in [i*math.tau/80 for i in range(80)]],.018,'courtmark',True)
line('3F · 외곽 슈팅 호',[(4.9*math.cos(t),-21.1+4.9*math.sin(t),zc) for t in [i*math.pi/80 for i in range(81)]],.018,'courtmark')
for xx in [-7.03,7.03]:
    box('3F · 농구장 창 아래 벽',(xx,-15.85,Z3+1.375),(.16,14.1,2.75),'wall')
    box('3F · 농구장 창 위 벽',(xx,-15.85,Z3+6.525),(.16,14.1,.95),'wall')
    for ya,yb in [(-22.9,-22.625),(-19.775,-19.425),(-16.575,-16.225),(-13.375,-13.025),(-10.175,-8.8)]:
        box('3F · 농구장 창 사이 벽',(xx,(ya+yb)/2,Z3+4.4),(.16,yb-ya,3.3),'wall')
    for yy in [-21.2,-18,-14.8,-11.6]:
        box('3F · 농구장 높은 창',(xx+(.10 if xx<0 else -.10),yy,Z3+4.4),(.025,2.85,3.3),'glass',.002)
        for zz in [Z3+2.77,Z3+6.03]:box('3F · 농구장 창 가로틀',(xx+(.12 if xx<0 else -.12),yy,zz),(.045,2.9,.055),'silver',.004)
box('3F · 농구장 뒤벽',(0,-22.9,Z3+3.5),(14.2,.18,7),'wall')
box('3F · 농구장 출입 상부',(0,-8.86,Z3+5.3),(14.1,.16,3.4),'wall')
for xx in [-4.9,4.9]:box('3F · 농구장 진입 벽',(xx,-8.86,Z3+1.8),(4.4,.16,3.6),'wall')
box('3F · 농구장 지붕',(0,-15.85,Z3+7.07),(14.35,14.3,.18),'white')
for yy in [-20.8,-16,-11.2]:
    box('3F · 체육관 지붕 보',(0,yy,Z3+6.8),(13.9,.20,.28),'charcoal')
    box('3F · 체육관 라인 조명',(0,yy,Z3+6.62),(11.0,.09,.025),'led',.005)
    light('3F · 체육관 면광원',(0,yy,Z3+6.57),(0,yy,Z3),550,10,.5)
box('3F · 농구 골대 지지대',(0,-22.68,Z3+2.48),(.15,.16,4.8),'charcoal')
box('3F · 백보드 테두리',(0,-21.9,Z3+3.55),(1.86,.09,1.10),'charcoal')
box('3F · 농구 백보드',(0,-21.84,Z3+3.55),(1.76,.025,1.0),'frost')
line('3F · 백보드 목표 사각',[(-.29,-21.817,Z3+3.25),(.29,-21.817,Z3+3.25),(.29,-21.817,Z3+3.69),(-.29,-21.817,Z3+3.69)],.015,'white',True)
orange=bpy.data.materials.new('32 · 농구 림 주황');orange.use_nodes=True
orange.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.8,.19,.025,1);M['orange']=orange
line('3F · 농구 림',[(.23*math.cos(i*math.tau/48),-21.48+.23*math.sin(i*math.tau/48),Z3+3.05) for i in range(48)],.019,'orange',True)
for i in range(12):
    a=i*math.tau/12;b=a+.18
    line('3F · 골망',[(.23*math.cos(a),-21.48+.23*math.sin(a),Z3+3.03),(.15*math.cos(b),-21.48+.15*math.sin(b),Z3+2.62)],.005,'white')
text('3F · 농구장 표지','농구 연습장',(0,-8.97,Z3+3.13),.26,'charcoal','front',True)

# Support the raised sports wing; the intermediate annex level also stays empty.
ACTIVE=STRUCT
for xx in [-7.12,7.12]:
    for yy in [-8.95,-15.85,-22.85]:box('체육관 동 · 구조 기둥',(xx,yy,Z3/2),(.3,.3,Z3),'wall')
box('2F · 체육관 동 빈 슬래브',(0,-15.85,H-.09),(14.25,14.2,.18),'stone')
for z in [H-.12,Z3-.12]:
    for xx in [-7.12,7.12]:box('체육관 동 · 층 외장',(xx,-15.85,z),(.20,14.35,.22),'silver')

# Match outdoor elevations: nearby roofs below eye level, taller neighbours in the distance.
ACTIVE=source_cols[12]
for o in list(ACTIVE.objects):
    if o.name.startswith(('창밖 건물','중정 맞은편 건물')):bpy.data.objects.remove(o,do_unlink=True)
sky=bpy.data.objects['창밖 하늘 바탕'];sky.location=(-50,0,25);sky.dimensions=(.1,150,90)
ground=bpy.data.objects['창밖 지면'];ground.location=(-18,-4,-.72);ground.dimensions=(45,100,.18)
bg_specs=[(-19,-22,9,6,6),(-25,-10,23,8,8),(-21,6,17,6,7),(-29,19,29,8,8),(-35,-30,32,9,10),(-31,36,25,8,8)]
for i,(xx,yy,hh,ww,dd) in enumerate(bg_specs):
    box('배경 · %02d 건물'%i,(xx,yy,hh/2-.6),(ww,dd,hh),'building' if i%2 else 'building2',.06)
    for zz in range(3,int(hh),3):
        box('배경 · 층 수평선',(xx+ww/2+.02,yy,zz-.6),(.025,dd-.12,.045),'white',.002)
        for dy in [-dd*.3,0,dd*.3]:box('배경 · 창',(xx+ww/2+.04,yy+dy,zz+.5),(.02,dd*.19,1.2),'frost',.002)
box('배경 · 맞은편 건물',(-.6,-36,9.0),(24,.25,18),'building2',.05)
for z in [2.5,5.5,8.5,11.5,14.5]:
    for x in [-9,-5.5,-2,1.5,5,8.5]:box('배경 · 맞은편 창',(x,-35.85,z),(2.2,.025,1.8),'frost',.005)

# Facility guide beside the preserved generated safety poster, on the 3F cabin wall.
ACTIVE=copycols[5]
yp=5.20;zp=Z3+1.88;xp=-3.195
box('3F · 층별 시설 안내 프레임',(-3.247,yp,zp),(.06,.55,1.13),'charcoal',.014)
box('3F · 층별 시설 안내 바탕',(-3.209,yp,zp),(.018,.50,1.075),'white',.006)
def walltext(name,body,y,z,size=.038,mat='charcoal',bold=False):
    obj=text(name,body,(xp,y,z),size,mat,'left',bold);return obj
walltext('3F · 시설 안내 제목','층별 시설 안내',yp,Z3+2.31,.052,'charcoal',True)
rows=[('12–6F','문화 · 운영'),('5F','체력단련실'),('4F','운동교실'),('3F','농구장 · 탈의실'),('2F','준비 중'),('1F','로비 · 주출입구')]
for i,(floor,body) in enumerate(rows):
    z=Z3+2.13-i*.125
    if floor=='3F':box('3F · 현재층 안내 강조',(-3.200,yp,z-.012),(.004,.475,.115),'frost',.002)
    walltext('3F · 안내 층 '+floor,floor,yp-.17,z,.040,'charcoal',floor=='3F')
    walltext('3F · 안내 시설 '+floor,body,yp+.065,z,.033,'charcoal',floor=='3F')
walltext('3F · 현위치 표기','현재 3층',yp,Z3+1.365,.042,'charcoal',True)

# Cameras for the elevated hall, the new directory, the sports wing and stacked construction.
cam=f3('02 · 엘리베이터 내부');S.camera=cam
guidecam=f3('06 · 승강기 식별 정보와 안전 포스터')
guidecam.location=(-.65,5.95,Z3+1.91)
guidecam.rotation_euler=(Vector((-3.04,4.62,Z3+1.95))-guidecam.location).to_track_quat('-Z','Y').to_euler();guidecam.data.lens=25
courtcam=camera('3F · 07 농구장과 복도',(5.8,-10.4,Z3+2.0),(-.5,-20,Z3+2),23)
exterior=camera('전체 · 08 건물 외관',(29,-35,22),(0,-6.5,6),37)
section=camera('전체 · 09 층 구성 단면',(-25,-24,20),(0,.0,5.8),38,29)
window=camera('3F · 10 창밖 높이',(-3.3,-1.9,Z3+1.6),(-20,2,5.8),25)
S.name='한빛 생활체육센터 · 3층에서 1층으로'
S['현재층']=3;S['오른쪽 보조 승강기 현재층']=5;S['층간 높이_m']=H;S['3층 바닥 기준 높이_m']=Z3
S['1층 보존']='기존 1층 로비/내부/가구 보존. 연결을 위해 계단 상부 3단, 2층 랜딩과 상부 벽 개구부 조정.'
S['2층']='가구·시설 없는 빈 구조층. 슬래브, 외벽, 계단 연결만 존재.'
S['3층']='승강기 홀, 농구 연습장, 탈의·보관 복도. 현재 3층, 목적지 1층.'
S['시나리오 요약']='3층 농구 종료 → 보관함에서 가방 챙김 → 1호기로 1층 귀가 → 정지 → 측면 비상벨 → 본관 1호기 HB-EL-01 전달 → 구조 안내 → 1층 출구'
readme='''한빛 생활체육센터 · 3층 시작 버전

층간 높이 3.96 m / 1층 기준 0 m / 2층 3.96 m / 3층 7.92 m.
1층의 기존 로비·캐빈·가구를 보존하고 3층에 별도의 편집 가능한 구역을 만들었습니다.
2층에는 장식·가구·시설을 배치하지 않았습니다. 층 구조와 연결 계단만 남겼습니다.
기존 계단 19단 뒤에 3단을 더하고 측면 귀환 계단을 만들어 1→2→3층으로 연결했습니다.
3층에는 농구 연습장과 탈의·보관 복도를 추가했습니다. 뒤 자동문이 실내 복도로 연결됩니다.
주 승강기와 실내는 3층, 닫힌 오른쪽 승강기는 5층으로 표시합니다.
포스터 옆에 층별 시설 안내가 있으며 3층을 표시했습니다.
주변 건물은 지상 기준 9~32 m로 조정했고 나무·지면은 지상에 둡니다.

3층과 1층의 캐빈은 각 장면의 편집·체험을 위한 고정 모델입니다.
수직 운행 및 Unity 게임 진행은 구현하지 않았습니다. 문 개폐 애니메이션은 유지합니다.
1프레임 닫힘, 72프레임 열림. 기본 카메라는 3층 내부입니다.
텍스처와 폰트는 모두 blend 내부에 포함했습니다.
시나리오는 별도 문서 및 Blender 텍스트에 저장했습니다.
'''
old=bpy.data.texts.get('읽어주세요.txt');old.clear();old.write(readme)
(WORK/'읽어주세요_3층.txt').write_text(readme,encoding='utf-8')
story=(WORK/'시나리오_3층에서_귀가.txt').read_text(encoding='utf-8')
oldstory=bpy.data.texts.get('건물콘셉트_시나리오.txt')
if oldstory:oldstory.clear();oldstory.write(story)
else:bpy.data.texts.new('건물콘셉트_시나리오.txt').write(story)
S.frame_set(1);S.camera=cam;S.render.filepath='//3층_02_내부.png'
S.render.resolution_x=1600;S.render.resolution_y=1000;S.render.resolution_percentage=100;S.cycles.samples=64
for o in bpy.context.selected_objects:o.select_set(False)
bpy.ops.file.pack_all();bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT),compress=True)
report={'file':str(OUTPUT),'floorHeight':H,'thirdFloorZ':Z3,'sourceObjects':len(source_objects),'thirdFloorCopies':len(copies),'objects':len(S.objects),'currentFloor':3,'unusedElevatorFloor':5,'facilities':[list(r) for r in rows],'backgroundHeights':[v[2] for v in bg_specs]}
(WORK/'Build_Validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('THREE_FLOORS_SAVED '+json.dumps(report,ensure_ascii=False),flush=True)
pref=bpy.context.preferences.addons['cycles'].preferences;pref.compute_device_type='OPTIX';pref.get_devices()
for d in pref.devices:d.use=d.type!='CPU'
S.cycles.device='GPU';S.cycles.samples=24;S.render.resolution_percentage=60
for c,name,frame in [(f3('01 · 로비 / 엘리베이터와 우측 계단'),'Draft_3F_Lobby.png',72),(guidecam,'Draft_3F_Directory.png',1),(courtcam,'Draft_3F_Court.png',1)]:
    S.camera=c;S.frame_set(frame);S.render.filepath=str(WORK/name)
    print('DRAFT_START',name,flush=True);bpy.ops.render.render(write_still=True)
print('THREE_FLOOR_DRAFTS_COMPLETE',flush=True)
