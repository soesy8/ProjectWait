import bpy, bmesh, json
from pathlib import Path
from mathutils import Vector
WORK=Path(__file__).resolve().parent
OUT=WORK/'Delivery'
S=.32
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Wait_Rescue_AB.blend'))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene
outline=bpy.data.collections['00_INK • 갈색 실루엣 대상']
removed=[]
for ob in list(bpy.data.objects):
    part=ob.get('Part','')
    if not part: continue
    if ob.get('BindBone')=='head':
        for v in ob.data.vertices: v.co.z+=.25*S
    if 'reflective' in part.lower() or 'Sleeve yellow band' in part:
        if ob.name in outline.objects: outline.objects.unlink(ob)
    if part.startswith('Shoulder cap.') or part.startswith('Hand •'):
        mod=ob.modifiers.new('Soft rounded finishing','SUBSURF'); mod.levels=1; mod.render_levels=1
    if part=='Standing rescue collar':
        for v in ob.data.vertices: v.co.z-=.16*S
    if part=='Collar split':
        for v in ob.data.vertices: v.co.z-=.16*S
    if part=='Jacket hem':
        # Remove the redundant overlapping strip. The body has a rounded hem already.
        removed.append(ob.name); bpy.data.objects.remove(ob,do_unlink=True)
        continue
    # Degenerate triangles have no visual area; clean before Freestyle tessellation.
    if ob.type=='MESH':
        bm=bmesh.new(); bm.from_mesh(ob.data)
        bmesh.ops.dissolve_degenerate(bm,dist=1e-7,edges=list(bm.edges))
        bm.to_mesh(ob.data); bm.free(); ob.data.update()

for rig in [o for o in bpy.data.objects if o.type=='ARMATURE']:
    bpy.context.view_layer.objects.active=rig
    bpy.ops.object.mode_set(mode='EDIT')
    rig.data.edit_bones['neck'].tail.z=3.70*S
    rig.data.edit_bones['head'].head.z=3.70*S
    rig.data.edit_bones['head'].tail.z=5.00*S
    bpy.ops.object.mode_set(mode='OBJECT')
for name in ('CAM_01 • 정면','CAM_02 • 측면','CAM_03 • 후면','CAM_04 • 앞 사선','CAM_05 • 뒤 사선'):
    cam=bpy.data.objects[name]; cam.data.ortho_scale=2.08
    target=Vector((0,0,.90)); cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
scene['Model_height_m']=1.794
for f in list(bpy.data.fonts):
    if f.name!='Bfont Regular' and f.users==0: bpy.data.fonts.remove(f)
text=bpy.data.texts['읽어주세요 • README']
readme=text.as_string().replace('높이 약 1.72m','높이 약 1.79m')
text.clear(); text.write(readme)
(OUT/'README_구조요원.txt').write_text(readme,encoding='utf-8-sig')
scene.render.resolution_percentage=65
scene.camera=bpy.data.objects['CAM_00 • 두 구조요원']
scene.render.filepath='//Preview_00_AB.png'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Wait_Rescue_AB.blend'))
scene.render.filepath=str(WORK/'Draft_AB_Polished.png')
bpy.ops.render.render(write_still=True)
print('POLISHED_PAIR_DONE',flush=True)

# Isolate each model at the origin for clear turnaround QA.
for which in ('A','B'):
    for ob in bpy.data.objects:
        if ob.name.startswith(('A | ','B | ')): ob.hide_render=not ob.name.startswith(which+' | ')
    for rig in [o for o in bpy.data.objects if o.type=='ARMATURE']: rig.location.x=0
    for ob in bpy.data.objects:
        if ob.name.startswith('Studio contact shadow '): ob.hide_render=True
    scene.render.resolution_x=900; scene.render.resolution_y=1100; scene.render.resolution_percentage=60
    for view,camname in [('Front','CAM_01 • 정면'),('Side','CAM_02 • 측면'),('Back','CAM_03 • 후면')]:
        scene.camera=bpy.data.objects[camname]; scene.render.filepath=str(WORK/('Draft_'+which+'_'+view+'.png'))
        bpy.ops.render.render(write_still=True)
        print('QA_RENDER '+which+' '+view,flush=True)
