import bpy
from pathlib import Path
WORK=Path(__file__).resolve().parent
OUT=WORK/'Delivery'
PAIR=OUT/'Wait_Rescue_AB.blend'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.open_mainfile(filepath=str(PAIR))
scene=bpy.context.scene
scene.render.resolution_x=1800; scene.render.resolution_y=1560; scene.render.resolution_percentage=100
scene.camera=bpy.data.objects['CAM_00 • 두 구조요원']; scene.render.filepath='//Preview_00_AB.png'
bpy.ops.wm.save_as_mainfile(filepath=str(PAIR))
scene.render.filepath=str(OUT/'Preview_00_AB.png'); bpy.ops.render.render(write_still=True)
print('PAIR_FINAL_DONE',flush=True)
for which,stem in [('A','Wait_Rescuer_A_Firefighter'),('B','Wait_Rescuer_B_LiftSpecialist')]:
    bpy.ops.wm.open_mainfile(filepath=str(PAIR))
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
                sp=area.spaces.active; sp.region_3d.view_location=(0,0,.88); sp.region_3d.view_distance=2.6
                sp.region_3d.view_rotation=scene.camera.rotation_euler.to_quaternion()
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(stem+'.blend')))
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
    print('SAVED_AND_EXPORTED '+which,flush=True)
    for view,cam in [('Hero','CAM_04 • 앞 사선'),('Front','CAM_01 • 정면'),('Side','CAM_02 • 측면'),('Back','CAM_03 • 후면'),('BackHero','CAM_05 • 뒤 사선')]:
        scene.camera=bpy.data.objects[cam]; scene.render.filepath=str(OUT/('Preview_'+which+'_'+view+'.png'))
        bpy.ops.render.render(write_still=True); print('FINAL_VIEW '+which+' '+view,flush=True)
print('ALL_REVISED_DELIVERABLES_DONE',flush=True)
