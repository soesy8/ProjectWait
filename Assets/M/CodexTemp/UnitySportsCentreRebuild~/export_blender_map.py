"""Lossless evaluated geometry -> native Unity meshes. Unity coordinates (x,z,y)."""
import bpy, json, struct, hashlib, shutil, math
from pathlib import Path
from mathutils import Matrix, Vector
ROOT=Path(r'D:/project/ProjectWait/Assets/M')
OUT=ROOT/'Arts/Models/SportsCentre3F';OUT.mkdir(parents=True,exist_ok=True)
TEX=ROOT/'Arts/Textures/SportsCentre3F';TEX.mkdir(parents=True,exist_ok=True)
S=bpy.context.scene;S.frame_set(1)
# Letter extrusions are sub-millimetre. Flat outlines with a moderate curve resolution
# preserve every label while avoiding millions of invisible side-wall triangles in XR.
for o in S.objects:
 if o.type=='FONT':o.data.extrude=0;o.data.bevel_depth=0;o.data.resolution_u=3
D=Matrix(((1,0,0,0),(0,0,1,0),(0,1,0,0),(0,0,0,1)))
mats=[];lookup={}
for m in bpy.data.materials:
 p=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None) if m.use_nodes else None
 def val(n,default):return p.inputs[n].default_value if p and n in p.inputs else default
 c=list(val('Base Color',m.diffuse_color));e=list(val('Emission Color',(0,0,0,1)))
 image=next((n.image for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image),None) if m.use_nodes else None
 texture=''
 if image:
  path=TEX/'ElevatorSafetyPoster.png';image.filepath_raw=str(path);image.file_format='PNG';image.save();texture='Assets/M/Arts/Textures/SportsCentre3F/'+path.name
 lookup[m.name]=len(mats)
 mats.append(dict(name=m.name,color=c,metallic=float(val('Metallic',0)),roughness=float(val('Roughness',.5)),emission=e,emissionStrength=float(val('Emission Strength',0)),transmission=float(val('Transmission Weight',0)),texture=texture))
dg=bpy.context.evaluated_depsgraph_get();objects=[];texts=[];lights=[];stats={'vertices':0,'triangles':0}
binpath=OUT/'SportsCentreGeometry.bytes'
with binpath.open('wb') as f:
 f.write(b'PWSC0001')
 for o in sorted(S.objects,key=lambda o:o.name):
  if o.hide_render or o.type not in ('MESH','FONT','CURVE','LIGHT'):continue
  cols=[c.name for c in o.users_collection]
  if any('가이드' in c for c in cols):continue
  if o.type=='LIGHT':
   lights.append(dict(name=o.name,position=list(D@o.matrix_world.translation),color=list(o.data.color),energy=o.data.energy,type=o.data.type));continue
  eo=o.evaluated_get(dg);me=eo.to_mesh(preserve_all_data_layers=True,depsgraph=dg)
  if not me:continue
  me.calc_loop_triangles()
  if not len(me.loop_triangles):eo.to_mesh_clear();continue
  # World matrix conversion preserves scale/rotation. Local vertex coordinates use the same conversion.
  wm=D@o.matrix_world@D;p,q,scale=wm.decompose()
  uv=me.uv_layers.active.data if me.uv_layers.active else None
  localmats=[lookup.get(m.name,0) if m else 0 for m in me.materials] or [0]
  offset=f.tell(); count=len(me.loop_triangles)*3; f.write(struct.pack('<i',count))
  for tri in me.loop_triangles:
   mat=localmats[min(tri.material_index,len(localmats)-1)]
   for li in (tri.loops[0],tri.loops[2],tri.loops[1]):
    loop=me.loops[li];v=me.vertices[loop.vertex_index].co;n=me.corner_normals[li].vector
    u=uv[li].uv if uv else (0,0)
    f.write(struct.pack('<8fi',v.x,v.z,v.y,n.x,n.z,n.y,u[0],u[1],mat))
  rec=dict(name=o.name,parent=o.parent.name if o.parent else '',collections=cols,position=list(p),rotation=[q.x,q.y,q.z,q.w],scale=list(scale),offset=offset,count=count,bounds=list(o.dimensions),type=o.type,body=o.data.body if o.type=='FONT' else '')
  objects.append(rec);stats['vertices']+=count;stats['triangles']+=count//3
  if o.type=='FONT':texts.append(dict(name=o.name,body=o.data.body,position=list(p)))
  eo.to_mesh_clear()
manifest=dict(version=1,source=bpy.data.filepath,sourceSha256=hashlib.sha256(Path(bpy.data.filepath).read_bytes()).hexdigest(),floorHeight=3.96,materials=mats,objects=objects,texts=texts,lights=lights,stats=stats)
(OUT/'SportsCentreManifest.json').write_text(json.dumps(manifest,ensure_ascii=False,separators=(',',':')),encoding='utf-8')
print('UNITY_MAP_EXPORT',json.dumps(dict(objects=len(objects),materials=len(mats),bytes=binpath.stat().st_size,**stats)))
