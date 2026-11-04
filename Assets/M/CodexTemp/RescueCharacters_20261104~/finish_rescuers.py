import bpy, bmesh, json
from pathlib import Path
from mathutils import Vector
WORK=Path(__file__).resolve().parent
OUT=WORK/'Delivery'
S=.32
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Wait_Rescue_AB.blend'))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene
for ob in list(bpy.data.objects):
    part=ob.get('Part','')
    if not part or ob.type!='MESH': continue
    if part.startswith('Shoulder cap.'):
        bpy.data.objects.remove(ob,do_unlink=True); continue
    if part.startswith('Sleeve.'):
        navy=bpy.data.materials['Shoulder panels']
        ob.data.materials.append(navy)
        for p in ob.data.polygons:
            if p.index<64 or (len(p.vertices)>4 and p.center.z>3*S): p.material_index=len(ob.data.materials)-1
    if part=='Hair cap • fitted hairline':
        for v in ob.data.vertices:
            v.co.x*=1.055; v.co.y*=1.075
            v.co.z=4.31*S+(v.co.z-4.31*S)*1.025
    if part in {'Standing rescue collar','Collar split'}:
        for v in ob.data.vertices: v.co.z+=.10*S; v.co.y*=1.35; v.co.x*=1.08
    if part=='Jacket • fitted padded body':
        for v in ob.data.vertices:
            if abs(v.co.z/S-2.035)<.001: v.co.x*=.65/.56; v.co.y*=.373/.32
        bm=bmesh.new(); bm.from_mesh(ob.data)
        faces=[f for f in bm.faces if all(abs(v.co.z/S-2.035)<.001 for v in f.verts)]
        bmesh.ops.delete(bm,geom=faces,context='FACES_ONLY'); bm.to_mesh(ob.data); bm.free()
    if part=='Trousers • pelvis':
        for v in ob.data.vertices:
            w=max(0,min(1,(v.co.z/S-1.95)/.23))
            v.co.x*=1-.075*w; v.co.y*=1-.12*w
    if part.startswith('Cargo trouser leg.'):
        bm=bmesh.new(); bm.from_mesh(ob.data)
        faces=[f for f in bm.faces if all(abs(v.co.z/S-.52)<.001 for v in f.verts)]
        bmesh.ops.delete(bm,geom=faces,context='FACES_ONLY'); bm.to_mesh(ob.data); bm.free()
    ob.data.update()
for im in bpy.data.images:
    if im.packed_file: im.filepath='//'+('Reference_A.png' if 'A안' in im.name else 'Reference_B.png')
scene.render.resolution_x=1800; scene.render.resolution_y=1560; scene.render.resolution_percentage=100
scene.camera=bpy.data.objects['CAM_00 • 두 구조요원']; scene.render.filepath='//Preview_00_AB.png'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Wait_Rescue_AB.blend'))
scene.render.filepath=str(OUT/'Preview_00_AB.png')
bpy.ops.render.render(write_still=True)
print('FINAL_PAIR_RENDER_DONE',flush=True)

# Isolated model files, with the same lights/cameras and only their own packed reference.
for which,stem in [('A','Wait_Rescuer_A_Firefighter'),('B','Wait_Rescuer_B_LiftSpecialist')]:
    bpy.ops.wm.open_mainfile(filepath=str(OUT/'Wait_Rescue_AB.blend'))
    scene=bpy.context.scene; other='B' if which=='A' else 'A'
    for ob in list(bpy.data.objects):
        if ob.name.startswith(other+' | '): bpy.data.objects.remove(ob,do_unlink=True)
    for c in list(bpy.data.collections):
        if c.name.startswith(other+' · '): bpy.data.collections.remove(c)
    for im in list(bpy.data.images):
        if im.packed_file and im.users==0: bpy.data.images.remove(im)
    rig=next(o for o in bpy.data.objects if o.type=='ARMATURE'); rig.location.x=0
    for ob in list(bpy.data.objects):
        if ob.name.startswith('Studio contact shadow '):
            if ob.name.endswith(other): bpy.data.objects.remove(ob,do_unlink=True)
            else: ob.location.x=0
    scene.name='Wait! | '+('A 소방 구조대원' if which=='A' else 'B 승강기 전문 구조요원')
    scene.render.resolution_x=1000; scene.render.resolution_y=1200; scene.render.resolution_percentage=100
    scene.camera=bpy.data.objects['CAM_04 • 앞 사선']; scene.render.filepath='//Preview_'+which+'_Hero.png'
    bpy.ops.object.select_all(action='DESELECT')
    head=bpy.data.objects[which+' | Head • 둥근 얼굴']; head.select_set(True); bpy.context.view_layer.objects.active=head
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type=='VIEW_3D':
                sp=area.spaces.active; sp.region_3d.view_location=(0,0,.9); sp.region_3d.view_distance=2.7
                sp.region_3d.view_rotation=scene.camera.rotation_euler.to_quaternion()
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(stem+'.blend')))
    print('INDIVIDUAL_SAVED '+which,flush=True)
    # FBX carries portable flat-color materials; Blender source keeps the cel node trees.
    mats={}; originals=[]
    for ob in bpy.data.objects:
        if not ob.name.startswith(which+' | ') or ob.type!='MESH': continue
        for slot in ob.material_slots:
            old=slot.material
            if old is None: continue
            if old.name not in mats:
                m=bpy.data.materials.new('PBR | '+old.name); m.diffuse_color=old.diffuse_color; m.use_nodes=True
                bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=old.diffuse_color
                bs.inputs['Roughness'].default_value=.95; bs.inputs['Specular IOR Level'].default_value=.15
                m['BaseColor_sRGB']=old.get('BaseColor_sRGB',''); mats[old.name]=m
            originals.append((slot,old)); slot.material=mats[old.name]
    bpy.ops.object.select_all(action='DESELECT')
    for ob in bpy.data.objects:
        if ob.name.startswith(which+' | ') and ob.type in {'MESH','ARMATURE'}: ob.select_set(True)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.fbx(filepath=str(OUT/(stem+'.fbx')),use_selection=True,object_types={'MESH','ARMATURE'},
        use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,apply_unit_scale=True,
        axis_forward='-Z',axis_up='Y',path_mode='AUTO',use_custom_props=True)
    for slot,old in originals: slot.material=old
    print('FBX_EXPORTED '+which,flush=True)
    for view,cam in [('Hero','CAM_04 • 앞 사선'),('Front','CAM_01 • 정면'),('Side','CAM_02 • 측면'),('Back','CAM_03 • 후면'),('BackHero','CAM_05 • 뒤 사선')]:
        scene.camera=bpy.data.objects[cam]; scene.render.filepath=str(OUT/('Preview_'+which+'_'+view+'.png'))
        bpy.ops.render.render(write_still=True)
        print('FINAL_RENDER '+which+' '+view,flush=True)
print('FINAL_ALL_DONE',flush=True)
