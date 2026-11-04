import bpy, json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
import bmesh
WORK=Path(__file__).resolve().parent
OUT=WORK/'Delivery'
S=.32
bpy.ops.wm.open_mainfile(filepath=str(WORK/'BeforeProportions'/'Wait_Rescue_AB.blend'))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene
def clamp(x): return max(0,min(1,x))
def bodyfactor(z): return .95-.08*clamp((z-2.20)/.85)
def armmap(p):
    x,y,z=p; sign=1 if x>=0 else -1
    if z>=2.71:
        t=clamp((3.21-z)/.50); old=.65+.385*t; new=.56+.27*t
    else:
        t=clamp((2.71-z)/.47); old=1.035+.155*t; new=.83+.05*t
    nx=sign*(new+(abs(x)-old)*.92)
    nz=z if z>=3.21 else 3.21+(z-3.21)*1.175
    return Vector((nx,y,nz))
report=[]
for which,factor in [('A',.92),('B',.88)]:
    prefix=which+' | '; rig=bpy.data.objects[prefix+'RIG • Pose Mode']
    for ob in bpy.data.objects:
        if not ob.name.startswith(prefix) or ob.type!='MESH': continue
        binding=ob.get('BindBone',''); part=ob.get('Part','')
        if binding=='head':
            for v in ob.data.vertices:
                v.co.x*=factor; v.co.y*=factor; v.co.z=3.53*S+(v.co.z-3.53*S)*factor
            continue
        if binding=='jacket':
            groupnames={g.index:g.name for g in ob.vertex_groups}
            for v in ob.data.vertices:
                w=sum(g.weight for g in v.groups if groupnames[g.group].startswith(('upper_arm.','forearm.')))
                p=v.co/S; body=Vector((p.x*bodyfactor(p.z),p.y,p.z))
                v.co=body.lerp(armmap(p),w)*S
        elif binding.startswith(('upper_arm.','forearm.','hand.')):
            for v in ob.data.vertices: v.co=armmap(v.co/S)*S
        elif binding in {'chest','neck','pelvis','spine'}:
            for v in ob.data.vertices: v.co.x*=bodyfactor(v.co.z/S)
        ob.data.update()
    # Match the actual FK joint locations to the new longer, closer-hanging arms.
    bpy.context.view_layer.objects.active=rig; bpy.ops.object.mode_set(mode='EDIT')
    for side,sign in [('L',1),('R',-1)]:
        positions=[((sign*.56,0,3.21),(sign*.83,-.03,2.6225)),
                   ((sign*.83,-.03,2.6225),(sign*.88,-.074,2.07025)),
                   ((sign*.88,-.074,2.07025),(sign*.898,-.08,1.60025))]
        for name,(head,tail) in zip(('upper_arm.','forearm.','hand.'),positions):
            b=rig.data.edit_bones[name+side]; b.head=Vector(head)*S; b.tail=Vector(tail)*S
    pivot=(3.53+(3.70-3.53)*factor)*S
    rig.data.edit_bones['neck'].tail.z=pivot
    rig.data.edit_bones['head'].head.z=pivot
    rig.data.edit_bones['head'].tail.z=(3.53+(5.00-3.53)*factor)*S
    bpy.ops.object.mode_set(mode='OBJECT')
    # Retain the tailored reflective band fit after the shoulder deformation.
    jacket=bpy.data.objects[prefix+'Jacket • continuous shoulders and arms']
    bm=bmesh.new(); bm.from_mesh(jacket.data); bvh=BVHTree.FromBMesh(bm)
    for ob in bpy.data.objects:
        part=ob.get('Part','')
        if not ob.name.startswith(prefix) or not ('Sleeve yellow band' in part or 'Sleeve ivory band' in part): continue
        for v in ob.data.vertices:
            co,no,idx,dist=bvh.find_nearest(v.co)
            if co is not None: v.co=co+no*(.019 if 'ivory' in part.lower() else .014)*S
    bm.free()
    report.append({'id':which,'head_scale':factor,'upper_torso_width_scale':.87,'arm_vertical_length_scale':1.175,
        'shoulder_joint_x':.56,'elbow_joint_x':.83,'wrist_joint_x':.88,'height_m':round((3.53+(5.4235-3.53)*factor)*S,3)})
    rig.location.x=-.53 if which=='A' else .53

for ob in bpy.data.objects:
    if ob.name.startswith('Studio contact shadow '): ob.location.x=-.53 if ob.name.endswith('A') else .53
for name in ['CAM_01 • 정면','CAM_02 • 측면','CAM_03 • 후면','CAM_04 • 앞 사선','CAM_05 • 뒤 사선','CAM_00 • 두 구조요원']:
    cam=bpy.data.objects[name]; cam.data.ortho_scale=2.55 if 'CAM_00' in name else 1.98
    target=Vector((0,0,.84)); cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
scene['Proportions']='Shoulders narrower, arms 17.5% longer and closer to body, head A 92% / B 88%'
scene['Model_height_m']='A 1.687 / B 1.663'
text=bpy.data.texts['읽어주세요 • README']; txt=text.as_string().replace('높이 약 1.74m','높이 A 약 1.69m / B 약 1.66m')
txt+='\n비율 수정: 어깨와 위쪽 몸통 폭을 줄이고 팔을 17.5% 길게 했습니다.\n팔이 몸 가까이 내려오도록 조정했습니다. 머리·이목구비·머리카락·모자 전체를\nA는 92%, B는 88%로 함께 줄여 모자와 얼굴의 맞음새를 유지했습니다.\n'
text.clear(); text.write(txt); (OUT/'README_구조요원.txt').write_text(txt,encoding='utf-8-sig')
scene.camera=bpy.data.objects['CAM_00 • 두 구조요원']; scene.render.resolution_x=1800; scene.render.resolution_y=1560; scene.render.resolution_percentage=65
scene.render.filepath='//Preview_00_AB.png'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Wait_Rescue_AB.blend'))
(WORK/'Proportion_Validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
scene.render.filepath=str(WORK/'Proportions_AB.png'); bpy.ops.render.render(write_still=True)
print('PROPORTION_PAIR_DONE',flush=True)
