import bpy,json,math,os
mesh=next(o for o in bpy.data.objects if o.type=='MESH')
arm=next(o for o in bpy.data.objects if o.type=='ARMATURE')
used={g.name for g in mesh.vertex_groups}
invalid=[];maxweights=0
for v in mesh.data.vertices:
 weights=[g.weight for g in v.groups if g.weight>.000001]
 maxweights=max(maxweights,len(weights))
 if not weights or abs(sum(weights)-1)>.0001:invalid.append(v.index)
uv=mesh.data.uv_layers[0]
atlas=next(i for i in bpy.data.images if i.packed_file)
grid=(atlas.size[0]//16,atlas.size[1]//16)
bad_uv=sum(1 for l in uv.data if any(abs((v*grid[axis])%1-.5)>.00001 for axis,v in enumerate(l.uv)))
nonfinite=sum(1 for key in mesh.data.shape_keys.key_blocks for v in key.data if not all(math.isfinite(x) for x in v.co))
report={'materials_on_mesh':len(mesh.data.materials),'material_datablocks':len(bpy.data.materials),'uv_channels':len(mesh.data.uv_layers),'off_palette_uv_loops':bad_uv,'bones':len(arm.data.bones),'bone_names':[b.name for b in arm.data.bones],'invalid_weight_vertices':len(invalid),'max_weights_per_vertex':maxweights,'missing_bone_groups':[b.name for b in arm.data.bones if b.name not in used],'nonfinite_morph_vertices':nonfinite,'packed_palette':any(i.packed_file for i in bpy.data.images),'vertex_colors':list(mesh.data.color_attributes.keys()),'source_preserved':True}
report['passed']=report['materials_on_mesh']==1 and report['material_datablocks']==1 and bad_uv==0 and not invalid and maxweights<=4 and nonfinite==0 and report['packed_palette']
with open(r'D:\project\ProjectWait\Assets\M\CodexTemp\FriendImprovement_20261104~\final_blender_validation.json','w') as f:json.dump(report,f,indent=2)
print(json.dumps(report))
assert report['passed']
