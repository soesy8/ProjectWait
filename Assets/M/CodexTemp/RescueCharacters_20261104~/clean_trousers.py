import bpy, bmesh
from pathlib import Path
WORK=Path(__file__).resolve().parent; OUT=WORK/'Delivery'; S=.32
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Wait_Rescue_AB.blend'))
bpy.context.preferences.filepaths.save_version=0
outline=bpy.data.collections['00_INK • 갈색 실루엣 대상']
for which in ('A','B'):
    prefix=which+' | '; rig=bpy.data.objects[prefix+'RIG • Pose Mode']
    collection=next(c for c in bpy.data.collections if c.name.startswith(which+' · '))
    old=[o for o in bpy.data.objects if o.name.startswith(prefix) and (o.get('Part')=='Trousers • pelvis' or o.get('Part','').startswith('Cargo trouser leg.'))]
    clones=[]
    for ob in old:
        for mod in ob.modifiers:
            if mod.type=='ARMATURE': mod.show_viewport=False
        bpy.context.view_layer.update()
        me=bpy.data.meshes.new_from_object(ob.evaluated_get(bpy.context.evaluated_depsgraph_get()))
        bm=bmesh.new(); bm.from_mesh(me); bmesh.ops.holes_fill(bm,edges=[e for e in bm.edges if e.is_boundary],sides=0); bm.to_mesh(me); bm.free()
        clone=bpy.data.objects.new('TEMP trousers',me); collection.objects.link(clone); clones.append(clone)
    bpy.ops.object.select_all(action='DESELECT')
    for clone in clones: clone.select_set(True)
    pants=clones[0]; bpy.context.view_layer.objects.active=pants; bpy.ops.object.join()
    pants.vertex_groups.clear()
    mod=pants.modifiers.new('Continuous hips and legs','REMESH'); mod.mode='VOXEL'; mod.voxel_size=.035*S; mod.use_smooth_shade=True
    bpy.ops.object.modifier_apply(modifier=mod.name)
    mod=pants.modifiers.new('Soft tailored hips','SMOOTH'); mod.factor=.65; mod.iterations=4; bpy.ops.object.modifier_apply(modifier=mod.name)
    mod=pants.modifiers.new('Rounded trousers surface','SUBSURF'); mod.levels=1; bpy.ops.object.modifier_apply(modifier=mod.name)
    bm=bmesh.new(); bm.from_mesh(pants.data)
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-6,plane_co=(0,0,.54*S),plane_no=(0,0,1),clear_inner=True)
    bm.to_mesh(pants.data); bm.free()
    for ob in old: bpy.data.objects.remove(ob,do_unlink=True)
    pants.name=prefix+'Trousers • continuous tailored hips'; pants['Part']='Trousers • continuous tailored hips'; pants['BindBone']='trousers'
    pants.data.materials.clear(); pants.data.materials.append(bpy.data.materials['Cargo navy | #3B3E48'])
    for p in pants.data.polygons: p.material_index=0; p.use_smooth=True
    outline.objects.link(pants)
    gs={n:pants.vertex_groups.new(name=n) for n in ['pelvis','thigh.L','thigh.R','shin.L','shin.R']}
    for v in pants.data.vertices:
        side='L' if v.co.x>0 else 'R'; z=v.co.z/S
        pelvis=max(0,min(1,(z-1.55)/.40)); shin=max(0,min(1,(1.29-z)/.30))
        for n,w in [('pelvis',pelvis),('thigh.'+side,(1-pelvis)*(1-shin)),('shin.'+side,(1-pelvis)*shin)]:
            if w: gs[n].add([v.index],w,'REPLACE')
    mod=pants.modifiers.new('FK skin • tailored hips','ARMATURE'); mod.object=rig; pants.parent=rig
bpy.ops.object.select_all(action='DESELECT'); head=bpy.data.objects['A | Head • 둥근 얼굴']; head.select_set(True); bpy.context.view_layer.objects.active=head
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Wait_Rescue_AB.blend'))
print('TROUSERS_CLEAN_DONE',flush=True)
