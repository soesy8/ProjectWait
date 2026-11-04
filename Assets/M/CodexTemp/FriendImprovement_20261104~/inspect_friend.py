import bpy, json, os
from mathutils import Vector
OUT = r'D:\project\ProjectWait\Assets\M\CodexTemp\FriendImprovement_20261104~'
os.makedirs(OUT, exist_ok=True)
report = {'version':bpy.app.version_string, 'objects':[],'materials':[]}
for o in bpy.data.objects:
    d={'name':o.name,'type':o.type,'location':list(o.location),'scale':list(o.scale),'rotation':list(o.rotation_euler),'parent':o.parent.name if o.parent else None,'modifiers':[{'name':m.name,'type':m.type} for m in o.modifiers]}
    if o.type=='MESH':
        pts=[o.matrix_world@Vector(v) for v in o.bound_box]
        d.update(vertices=len(o.data.vertices),faces=len(o.data.polygons),bounds=[[min(p[i] for p in pts),max(p[i] for p in pts)] for i in range(3)],materials=[m.name if m else None for m in o.data.materials],groups=[g.name for g in o.vertex_groups])
    report['objects'].append(d)
for m in bpy.data.materials:
    report['materials'].append({'name':m.name,'color':list(m.diffuse_color),'nodes':[(n.name,n.type) for n in m.node_tree.nodes] if m.use_nodes else []})
with open(os.path.join(OUT,'source_inspection.json'),'w',encoding='utf8') as f:json.dump(report,f,ensure_ascii=False,indent=2)
scene=bpy.context.scene
if bpy.context.object and bpy.context.object.mode!='OBJECT': bpy.ops.object.mode_set(mode='OBJECT')
scene.render.engine='BLENDER_WORKBENCH'
scene.render.resolution_x=1000;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.world.color=(.8,.8,.8)
scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL'
scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
scene.display.shading.background_type='WORLD'
meshes=[o for o in bpy.data.objects if o.type=='MESH']
pts=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
lo=Vector([min(p[i] for p in pts) for i in range(3)]);hi=Vector([max(p[i] for p in pts) for i in range(3)]);center=(hi+lo)/2
bpy.ops.object.camera_add(location=center+Vector((0,-max(hi-lo)*2,0)))
cam=bpy.context.object;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=max(hi-lo)*1.18;scene.camera=cam
scene.render.filepath=os.path.join(OUT,'source_front.png');bpy.ops.render.render(write_still=True)
print('INSPECT_DONE',len(meshes),list(lo),list(hi))
