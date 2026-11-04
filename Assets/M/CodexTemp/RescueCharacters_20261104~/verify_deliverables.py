import bpy, bmesh, json, math
from pathlib import Path
from mathutils import Vector
WORK=Path(__file__).resolve().parent
OUT=WORK/'Delivery'
report={'blender_version':bpy.app.version_string,'blend_files':[],'fbx_roundtrip':[],'issues':[]}
dimension_file=OUT/'Dimensions_구조요원.json'
dimension_specs={c['id']:c for c in json.loads(dimension_file.read_text(encoding='utf-8'))['characters']} if dimension_file.exists() else {}
def axis_sizes(parts):
    coords=[v.co for ob in parts for v in ob.data.vertices]
    return [max(p[i] for p in coords)-min(p[i] for p in coords) for i in range(3)]
files=[('Wait_Rescue_AB.blend',2),('Wait_Rescuer_A_Firefighter.blend',1),('Wait_Rescuer_B_LiftSpecialist.blend',1)]
for filename,count in files:
    bpy.ops.wm.open_mainfile(filepath=str(OUT/filename))
    rigs=[o for o in bpy.data.objects if o.type=='ARMATURE']
    meshes=[o for o in bpy.data.objects if o.type=='MESH' and o.get('Part')]
    item={'file':filename,'rigs':len(rigs),'bones':[len(o.data.bones) for o in rigs],'meshes':len(meshes),
        'packed_references':len([im for im in bpy.data.images if im.packed_file]),'characters':[]}
    if len(rigs)!=count: report['issues'].append(filename+': rig count')
    if item['packed_references']!=count: report['issues'].append(filename+': packed reference count')
    deps=bpy.context.evaluated_depsgraph_get()
    for rig in rigs:
        prefix=rig.name[0]+' | '
        parts=[o for o in meshes if o.name.startswith(prefix)]
        if filename!='Wait_Rescue_AB.blend' and rig.location.length>1e-5: report['issues'].append(filename+': rig not at origin')
        unweighted=0; invalid_weights=0; missing=0; verts=0; tris=0
        for ob in parts:
            for v in ob.data.vertices:
                weights=[g.weight for g in v.groups]
                if not weights: unweighted+=1
                elif abs(sum(weights)-1)>1e-4: invalid_weights+=1
                if not all(math.isfinite(n) for n in v.co): report['issues'].append(ob.name+': nonfinite vertex')
            if any(g.name not in rig.data.bones for g in ob.vertex_groups): missing+=1
            if not any(m.type=='ARMATURE' and m.object==rig for m in ob.modifiers): missing+=1
            if any(slot.material is None for slot in ob.material_slots): missing+=1
            ev=ob.evaluated_get(deps); me=ev.to_mesh(); me.calc_loop_triangles()
            verts+=len(me.vertices); tris+=len(me.loop_triangles); ev.to_mesh_clear()
        jacket=bpy.data.objects[prefix+'Jacket • continuous shoulders and arms']
        bm=bmesh.new(); bm.from_mesh(jacket.data)
        unseen=set(bm.verts); components=0
        while unseen:
            components+=1; stack=[unseen.pop()]
            while stack:
                v=stack.pop()
                for e in v.link_edges:
                    other=e.other_vert(v)
                    if other in unseen: unseen.remove(other); stack.append(other)
        bm.free()
        char={'id':rig.name[0],'evaluated_vertices':verts,'evaluated_triangles':tris,'unweighted_vertices':unweighted,
            'invalid_weight_sums':invalid_weights,'missing_bindings_or_materials':missing,'jacket_connected_components':components}
        if rig.name[0] in dimension_specs:
            spec=dimension_specs[rig.name[0]]
            body_size=axis_sizes([ob for ob in parts if ob.get('BindBone')!='head'])
            head_size=axis_sizes([ob for ob in parts if ob.get('BindBone')=='head'])
            ratios=[body_size[i]/spec['before_body_bounds_m']['size'][i] for i in range(3)]
            char['saved_body_axis_ratios']=ratios
            char['head_size_unchanged']=all(abs(a-b)<1e-6 for a,b in zip(head_size,spec['head_bounds_before_m']['size']))
            if any(abs(a-b)>1e-5 for a,b in zip(ratios,spec['requested_axis_ratios'])) or not char['head_size_unchanged']:
                report['issues'].append(filename+': saved dimension mismatch for '+rig.name[0])
        if unweighted or invalid_weights or missing or components!=1: report['issues'].append(filename+': '+json.dumps(char))
        # Exercise real FK deformation, check that the continuous shoulder remains finite and moves.
        pb=rig.pose.bones['upper_arm.L']; pb.rotation_mode='XYZ'; pb.rotation_euler.z=.30
        bpy.context.view_layer.update()
        ev=jacket.evaluated_get(bpy.context.evaluated_depsgraph_get()); me=ev.to_mesh()
        moved=sum((a.co-b.co).length>.001 for a,b in zip(jacket.data.vertices,me.vertices))
        char['fk_pose_vertices_moved']=moved
        if not moved or not all(math.isfinite(x) for v in me.vertices for x in v.co): report['issues'].append(filename+': FK pose')
        ev.to_mesh_clear(); pb.rotation_euler.z=0; bpy.context.view_layer.update()
        item['characters'].append(char)
    report['blend_files'].append(item)
for which,stem in [('A','Wait_Rescuer_A_Firefighter'),('B','Wait_Rescuer_B_LiftSpecialist')]:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(OUT/(stem+'.fbx')),use_anim=False)
    rigs=[o for o in bpy.data.objects if o.type=='ARMATURE']; meshes=[o for o in bpy.data.objects if o.type=='MESH']
    item={'file':stem+'.fbx','rigs':len(rigs),'bones':len(rigs[0].data.bones) if rigs else 0,'meshes':len(meshes)}
    if len(rigs)!=1 or item['bones']!=18 or not meshes: report['issues'].append(stem+': FBX roundtrip')
    report['fbx_roundtrip'].append(item)
report['status']='PASS' if not report['issues'] else 'FAIL'
(OUT/'Validation_구조요원.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('VALIDATION '+json.dumps(report,ensure_ascii=True),flush=True)
