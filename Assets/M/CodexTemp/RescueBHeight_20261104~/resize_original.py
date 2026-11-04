"""Edit the original B mesh and skeleton; maintain round heads and exact overall height."""
import bpy, json, math, hashlib, struct
from pathlib import Path
from mathutils import Matrix, Vector
WORK=Path(__file__).resolve().parent
OUT=WORK/'Delivery'
M=Path('D:/project/ProjectWait/Assets/M')
assert OUT.is_relative_to(M)
bpy.context.preferences.filepaths.save_version=0
FILENAME='Wait_Rescuer_B_LiftSpecialist.blend'
HEIGHT=.89
HEAD=.96

def parts(which):
    return [o for o in bpy.data.objects if o.type=='MESH' and o.name.startswith(which+' | ') and o.get('Part')]
def bounds(objects,evaluated=False):
    points=[]
    deps=bpy.context.evaluated_depsgraph_get()
    for ob in objects:
        if evaluated:
            ev=ob.evaluated_get(deps);mesh=ev.to_mesh();points.extend(tuple(v.co) for v in mesh.vertices);ev.to_mesh_clear()
        else: points.extend(tuple(v.co) for v in ob.data.vertices)
    low=[min(p[i] for p in points) for i in range(3)];high=[max(p[i] for p in points) for i in range(3)]
    return {'min':low,'max':high,'size':[high[i]-low[i] for i in range(3)]}
def geometry_digest(objects):
    h=hashlib.sha256()
    for ob in sorted(objects,key=lambda o:o.name):
        h.update(ob.name.encode('utf-8'))
        for v in ob.data.vertices: h.update(struct.pack('<fff',*v.co))
    return h.hexdigest()

bpy.ops.wm.open_mainfile(filepath=str(WORK/'Before'/FILENAME))
rig=bpy.data.objects['B | RIG • Pose Mode']
old_pose=rig.data.pose_position;rig.data.pose_position='REST';bpy.context.view_layer.update()
before=bounds(parts('B'),True)
head_before=bounds([o for o in parts('B') if o['BindBone']=='head'])
body_before=bounds([o for o in parts('B') if o['BindBone']!='head'])
pivot=rig.data.bones['head'].head_local.copy()
floor=before['min'][2];top=before['max'][2]
body_scale=(HEIGHT*(top-floor)-HEAD*(top-pivot.z))/(pivot.z-floor)
assert .5<body_scale<1
head_new_pivot=Vector((pivot.x,pivot.y,pivot.z*body_scale))
head_matrix=Matrix.Translation(head_new_pivot)@Matrix.Scale(HEAD,4)@Matrix.Translation(-pivot)
body_matrix=Matrix.Diagonal((1.,1.,body_scale,1.))
report={'request':{'overall_height_ratio':HEIGHT,'head_uniform_ratio':HEAD},
        'body_vertical_ratio':body_scale,'before':before,'head_before':head_before,'body_before':body_before,
        'body_width_and_depth_preserved':True,'files':[]}
note='\nB 원본 추가 수정 (2026-11-04): 전체 키를 직전 원본 대비 11% 줄였습니다.\n머리·이목구비·머리카락·모자는 균일하게 4% 줄였으며, 몸통과 팔다리의 높이를 조정해 전체 키 비율을 맞췄습니다.\n몸의 좌우 폭과 앞뒤 두께는 유지했습니다. 원본 메시와 뼈대를 함께 수정했습니다.\n'

for filename in [FILENAME,'Wait_Rescue_AB.blend']:
    bpy.ops.wm.open_mainfile(filepath=str(WORK/'Before'/filename))
    rig=bpy.data.objects['B | RIG • Pose Mode'];old_pose=rig.data.pose_position;rig.data.pose_position='REST'
    original_a=geometry_digest(parts('A')) if parts('A') else None
    body=[o for o in parts('B') if o['BindBone']!='head'];head=[o for o in parts('B') if o['BindBone']=='head']
    for ob in body+head:
        if ob.data.users>1: ob.data=ob.data.copy()
        ob.data.transform(head_matrix if ob['BindBone']=='head' else body_matrix,shape_keys=True);ob.data.update()
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
    bpy.ops.object.mode_set(mode='EDIT')
    for bone in rig.data.edit_bones:
        matrix=head_matrix if bone.name=='head' else body_matrix
        bone.head=matrix@bone.head;bone.tail=matrix@bone.tail
    bpy.ops.object.mode_set(mode='OBJECT');bpy.context.view_layer.update()
    after=bounds(parts('B'),True);head_after=bounds(head);body_after=bounds(body)
    ratio=after['size'][2]/before['size'][2]
    assert abs(ratio-HEIGHT)<1e-5,(filename,ratio)
    for i in range(3): assert abs(head_after['size'][i]/head_before['size'][i]-HEAD)<1e-5
    for i in range(2): assert abs(body_after['size'][i]/body_before['size'][i]-1)<1e-6
    if original_a: assert geometry_digest(parts('A'))==original_a
    rig['WaitRescuePoseScaleY']=float(rig.get('WaitRescuePoseScaleY',1))*body_scale
    rig['WaitRescueHeightMeters']=after['size'][2]
    rig['WaitRescueHeadRevision']=HEAD
    rig['ModelRevision']='B overall height 89%, head uniform 96%; original meshes and rest bones edited'
    scene=bpy.context.scene;scene['B_height_m']=after['size'][2]
    scene['Model_height_m']=after['size'][2] if filename==FILENAME else 'A %.6f / B %.6f'%(bounds(parts('A'),True)['size'][2],after['size'][2])
    for text in bpy.data.texts:
        if 'README' in text.name:
            content=text.as_string().replace('B 약 1.66m','B 약 1.48m');text.clear();text.write(content+note)
    rig.data.pose_position=old_pose
    scene.camera=bpy.data.objects['CAM_00 • 두 구조요원' if filename=='Wait_Rescue_AB.blend' else 'CAM_04 • 앞 사선']
    scene.render.filepath='//Preview_00_AB.png' if filename=='Wait_Rescue_AB.blend' else '//Preview_B_Hero.png'
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/filename))
    report['files'].append({'file':filename,'after':after,'head_after':head_after,'body_after':body_after,
                            'actual_height_ratio':ratio,'A_geometry_unchanged':True,'pose_scale_y':rig['WaitRescuePoseScaleY']})

report['new_height_m']=report['files'][0]['after']['size'][2]
report['status']='PASS'
(OUT/'BHeightHead_Validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
readme=(OUT/'README_구조요원.txt').read_text(encoding='utf-8-sig')
(OUT/'README_구조요원.txt').write_text(readme+note+'수정 후 B 키: %.4f m.\n'%report['new_height_m'],encoding='utf-8-sig')
print('ORIGINAL_EDIT_DONE '+json.dumps({'height_before':before['size'][2],'height_after':report['new_height_m'],'height_ratio':HEIGHT,'head_ratio':HEAD,'body_pose_y':body_scale}),flush=True)

# Re-export B alone. A source and exchange files are preserved byte for byte.
bpy.ops.wm.open_mainfile(filepath=str(OUT/FILENAME));scene=bpy.context.scene
rig=bpy.data.objects['B | RIG • Pose Mode'];originals=[];mats={}
for ob in parts('B'):
    for slot in ob.material_slots:
        old=slot.material
        if not old: continue
        if old.name not in mats:
            mat=bpy.data.materials.new('PBR | '+old.name);mat.diffuse_color=old.diffuse_color;mat.use_nodes=True
            bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=old.diffuse_color;bs.inputs['Roughness'].default_value=.95
            mats[old.name]=mat
        originals.append((slot,old));slot.material=mats[old.name]
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for ob in parts('B'): ob.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'Wait_Rescuer_B_LiftSpecialist.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},use_mesh_modifiers=True,
    add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_custom_props=True)
for slot,mat in originals: slot.material=mat
for view,cam in [('Hero','CAM_04 • 앞 사선'),('Front','CAM_01 • 정면'),('Side','CAM_02 • 측면'),('Back','CAM_03 • 후면'),('BackHero','CAM_05 • 뒤 사선')]:
    scene.camera=bpy.data.objects[cam];scene.render.filepath=str(OUT/('Preview_B_'+view+'.png'));scene.render.resolution_percentage=100
    bpy.ops.render.render(write_still=True);print('B_PREVIEW '+view,flush=True)
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Wait_Rescue_AB.blend'));scene=bpy.context.scene
scene.camera=bpy.data.objects['CAM_00 • 두 구조요원'];scene.render.resolution_percentage=100;scene.render.filepath=str(OUT/'Preview_00_AB.png')
bpy.ops.render.render(write_still=True)
print('SOURCE_AND_PREVIEWS_DONE',flush=True)
