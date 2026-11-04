"""Preserve the supplied modelling; add game-ready features, skinning and morphs."""
import bpy, bmesh, math, os, json
from mathutils import Vector
from math import sin, cos, pi
ROOT=r'D:\project\ProjectWait\Assets\M'
OUT=ROOT+r'\Arts\Models\FriendImproved'
SRC=OUT+r'\Source~'
QA=ROOT+r'\CodexTemp\FriendImprovement_20261104~'
for path in (OUT,SRC,QA):os.makedirs(path,exist_ok=True)
if bpy.context.object and bpy.context.object.mode!='OBJECT':bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.object.select_all(action='DESELECT')
def active(o):
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
def linear(v):return v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4
palette={'Hoodie':'FF8C2D','Cuff':'E87927','Skin':'FFC08A','Shorts':'3B3E48','Hair':'633B29','Ink':'493327','Paper':'FFF6DB','Sole':'D9D2BE','Bag':'358F88','BagDark':'276E69','BagLight':'58A99A','Cheek':'FF9572','Mouth':'873E35','Tongue':'EF796D','Eye':'302B25','SockWhite':'FFFDF5','ShoeGreen':'358F88'}
palette['ShoeGreen']=palette['Bag']
PALETTE_COLS=8;PALETTE_ROWS=4
SOCK_TOP=.82
mats={}
for name,h in palette.items():
 m=bpy.data.materials.new(name);rgb=tuple(linear(int(h[i:i+2],16)/255) for i in (0,2,4));m.diffuse_color=(*rgb,1);m.use_nodes=True
 nodes=m.node_tree.nodes;nodes.clear();out=nodes.new('ShaderNodeOutputMaterial');diff=nodes.new('ShaderNodeBsdfDiffuse');diff.inputs['Color'].default_value=(1,1,1,1)
 rgbnode=nodes.new('ShaderNodeShaderToRGB');ramp=nodes.new('ShaderNodeValToRGB');ramp.color_ramp.interpolation='CONSTANT'
 ramp.color_ramp.elements.remove(ramp.color_ramp.elements[1]);e=ramp.color_ramp.elements[0];e.position=0;e.color=(*(v*.73 for v in rgb),1)
 e=ramp.color_ramp.elements.new(.38);e.color=(*(v*.9 for v in rgb),1)
 e=ramp.color_ramp.elements.new(.68);e.color=(*rgb,1)
 em=nodes.new('ShaderNodeEmission');m.node_tree.links.new(diff.outputs[0],rgbnode.inputs[0]);m.node_tree.links.new(rgbnode.outputs[0],ramp.inputs[0]);m.node_tree.links.new(ramp.outputs[0],em.inputs[0]);m.node_tree.links.new(em.outputs[0],out.inputs[0]);mats[name]=m
def setmat(o,name):o.data.materials.clear();o.data.materials.append(mats[name])
def smooth(o):
 for p in o.data.polygons:p.use_smooth=True
def subd(o,level=1):
 active(o);mod=o.modifiers.new('Rounded original surfaces','SUBSURF');mod.levels=level;bpy.ops.object.modifier_apply(modifier=mod.name)
def transform(o):active(o);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
body=bpy.data.objects['Cube'];body.name='Friend_Body';head=bpy.data.objects['Sphere'];head.name='Friend_Head';hair=bpy.data.objects['Cube.001'];hair.name='Friend_Hair';tuft=bpy.data.objects['Cube.002'];tuft.name='Friend_HairTuft'
original=[body,head,hair,tuft]
for o in original:transform(o);smooth(o)
adj={v.index:set() for v in body.data.vertices}
for e in body.data.edges:
 a,b=e.vertices;adj[a].add(b);adj[b].add(a)
remaining=set(adj);semantic={}
while remaining:
 stack=[remaining.pop()];indices=[]
 while stack:
  i=stack.pop();indices.append(i)
  for j in adj[i]:
   if j in remaining:remaining.remove(j);stack.append(j)
 pts=[body.data.vertices[i].co for i in indices];center=sum(pts,Vector())/len(pts)
 tag='shoe' if max(p.z for p in pts)<.7 else ('hand' if abs(center.x)>2.3 else ('hood' if max(p.y for p in pts)>1.0 else ('pocket' if min(p.y for p in pts)<-.8 else 'body')))
 for i in indices:semantic[i]=tag
names=['Hoodie','Cuff','Skin','Shorts','Paper','Sole','ShoeGreen','SockWhite']
for n in names:body.data.materials.append(mats[n])
for p in body.data.polygons:
 c=p.center
 # Polygon centers update lazily: calculate directly from the actual vertices.
 c=sum((body.data.vertices[i].co for i in p.vertices),Vector())/len(p.vertices)
 x,y,z=c
 tag=semantic[p.vertices[0]]
 if tag=='shoe':n='ShoeGreen'
 elif tag=='hand':n='Skin'
 elif z<.980:n='Skin'
 elif z<1.84:n='Shorts'
 elif abs(x)>2.224:n='Cuff'
 else:n='Hoodie'
 p.material_index=names.index(n)
for o,n in [(head,'Skin'),(hair,'Hair'),(tuft,'Hair')]:setmat(o,n)
subd(body);subd(head);subd(hair)
# Cut an actual horizontal shoe/sole seam rather than color whole crossing faces.
bm=bmesh.new();bm.from_mesh(body.data)
shoe_faces=[f for f in bm.faces if f.material_index==names.index('ShoeGreen')]
shoe_edges=set(e for f in shoe_faces for e in f.edges);shoe_verts=set(v for f in shoe_faces for v in f.verts)
bmesh.ops.bisect_plane(bm,geom=list(shoe_faces)+list(shoe_edges)+list(shoe_verts),plane_co=(0,0,.145),plane_no=(0,0,1),dist=.000001,clear_inner=False,clear_outer=False)
for f in bm.faces:
 if f.material_index==names.index('ShoeGreen') and f.calc_center_median().z<.145:f.material_index=names.index('Sole')
# A real horizontal edge follows the marked sock height around both round shins.
leg_faces=[f for f in bm.faces if f.material_index==names.index('Skin') and max(v.co.z for v in f.verts)<1.05]
leg_edges=set(e for f in leg_faces for e in f.edges);leg_verts=set(v for f in leg_faces for v in f.verts)
bmesh.ops.bisect_plane(bm,geom=list(leg_faces)+list(leg_edges)+list(leg_verts),plane_co=(0,0,SOCK_TOP),plane_no=(0,0,1),dist=.000001,clear_inner=False,clear_outer=False)
for f in bm.faces:
 if f.material_index==names.index('Skin') and f.calc_center_median().z<SOCK_TOP:f.material_index=names.index('SockWhite')
bm.to_mesh(body.data);bm.free();body.data.update()
# Re-identify loose shells after subdivision; retain exact hand/shoe rigidity.
body_rigid={};adj={v.index:set() for v in body.data.vertices}
for e in body.data.edges:
 a,b=e.vertices;adj[a].add(b);adj[b].add(a)
remaining=set(adj)
while remaining:
 stack=[remaining.pop()];ids=[]
 while stack:
  i=stack.pop();ids.append(i)
  for j in adj[i]:
   if j in remaining:remaining.remove(j);stack.append(j)
 pts=[body.data.vertices[i].co for i in ids];c=sum(pts,Vector())/len(pts);suffix='L' if c.x>=0 else 'R'
 bone=('foot.'+suffix) if max(p.z for p in pts)<.7 else (('hand.'+suffix) if abs(c.x)>2.3 else ('chest' if max(p.y for p in pts)>1 else ('spine' if min(p.y for p in pts)<-.8 else None)))
 if bone:
  for i in ids:body_rigid[i]=bone
active(tuft)
for mod in list(tuft.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
features=[];rigid={head:'head',hair:'head',tuft:'head'}
def sphere(name,loc,scale,mat,bone='chest',segments=24,rings=12):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=loc);o=bpy.context.object;o.name=name;o.scale=scale;transform(o);setmat(o,mat);smooth(o);features.append(o);rigid[o]=bone;return o
def rounded(name,loc,size,bevel,mat,bone='chest'):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=size;transform(o)
 mod=o.modifiers.new('Soft sewn corners','BEVEL');mod.width=bevel;mod.segments=5;bpy.ops.object.modifier_apply(modifier=mod.name);setmat(o,mat);smooth(o);features.append(o);rigid[o]=bone;return o
def tube(name,points,radius,mat,bone='chest'):
 c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.resolution_u=10;c.bevel_depth=radius;c.bevel_resolution=3;s=c.splines.new('BEZIER');s.bezier_points.add(len(points)-1)
 for b,p in zip(s.bezier_points,points):b.co=p;b.handle_left_type='AUTO';b.handle_right_type='AUTO'
 o=bpy.data.objects.new(name,c);bpy.context.collection.objects.link(o);active(o);bpy.ops.object.convert(target='MESH');setmat(o,mat);smooth(o);features.append(o);rigid[o]=bone;return o
# The bag sits behind the hoodie; straps cross the shoulders and meet the lower bag.
rounded('Backpack_Main',(0,.97,2.72),(1.43,.75,1.64),.30,'Bag')
rounded('Backpack_FrontPocket',(0,1.379,2.46),(1.13,.22,1.00),.20,'BagLight')
rounded('Backpack_NamePatch',(0,1.508,2.47),(.48,.045,.29),.045,'Paper')
for side in (-1,1):
 rounded('Backpack_SidePocket',(.72*side,1.02,2.34),(.25,.49,.64),.11,'BagDark')
 # Flattened cross section reads as fabric webbing instead of a round hose.
 pts=[(.53*side,1.03,3.30),(.62*side,.62,3.68),(.64*side,.08,3.79),(.67*side,-.41,3.42),(.64*side,-.64,2.88),(.69*side,-.55,2.35),(.70*side,.30,2.12),(.58*side,.78,2.21)]
 strap=tube('Backpack_ShoulderStrap',pts,.104,'BagDark')
 rounded('Backpack_StrapAdjuster',(.65*side,-.688,2.79),(.20,.07,.15),.024,'BagLight')
pts=[(-.48,1.514,2.76),(-.36,1.519,2.94),(0,1.52,2.99),(.36,1.519,2.94),(.48,1.514,2.76)]
tube('Backpack_ZipSeam',pts,.016,'BagDark')
tube('Backpack_TopHandle',[(-.22,.93,3.43),(-.2,.95,3.67),(.2,.95,3.67),(.22,.93,3.43)],.055,'BagDark')
rounded('Backpack_ZipPull',(.43,1.56,2.78),(.055,.045,.15),.02,'Paper')
# Sample the real rounded head surface, so face patches follow its curvature.
def surface_y(x,z):
 hit,loc,normal,index=head.ray_cast(Vector((x,-3,z)),Vector((0,1,0)))
 return loc.y if hit else -.60
states=['Basis','Blink','Smile','Surprised','Worried','Sad']
fv={s:[] for s in states};faces=[];fm=[]
def append_shape(coords,polys,mat):
 start=len(fv['Basis'])
 for s in states:fv[s].extend(coords[s])
 faces.extend([tuple(start+i for i in p) for p in polys]);fm.extend([mat]*len(polys))
def face_oval(cx,cz,rx,rz,mat,depth=.035,kind='fixed',side=1):
 coords={};seg=24;nr=10
 for st in states:
  pts=[];xx,zz,ww,hh=cx,cz,rx,rz
  if kind in ('eye','glint'):
   if st=='Blink':hh*=.055;zz=cz+(4.66-cz)*.94
   if st=='Surprised':hh*=1.12;ww*=1.04
   if st=='Sad':hh*=.86
  for j in range(nr+1):
   a=pi*j/nr
   for i in range(seg):
    t=2*pi*i/seg;x=xx+ww*sin(a)*cos(t);z=zz+hh*cos(a);y=surface_y(x,z)-.025-depth*sin(a)*sin(t)
    # Keep the whole ellipsoid above the skin by compressing the rear hemisphere.
    y=surface_y(x,z)-.025-depth*(.5+.5*sin(a)*sin(t));pts.append((x,y,z))
  coords[st]=pts
 polys=[(j*seg+i,j*seg+(i+1)%seg,(j+1)*seg+(i+1)%seg,(j+1)*seg+i) for j in range(nr) for i in range(seg)]
 append_shape(coords,polys,mat)
for side in (-1,1):
 face_oval(.345*side,4.625,.095,.177,'Eye',.07,'eye',side)
 face_oval(.345*side-.024,4.696,.025,.043,'Paper',.104,'glint',side)
 face_oval(.345*side+.025,4.565,.012,.017,'Paper',.099,'glint',side)
 face_oval(.636*side,4.343,.132,.093,'Cheek',.012)
 # Brows are simple curved ribbons, with explicit worried/sad inner lifts.
 coords={}
 for st in states:
  pts=[]
  for i in range(17):
   t=i/16;x=side*(.235+.23*t);z=4.862+.038*sin(pi*t)
   if st in ('Worried','Sad'):z+=.09*(1-t)-.045*t
   if st=='Surprised':z+=.07
   if st=='Smile':z-=.015
   for edge in (-1,1):pts.append((x,surface_y(x,z)-.038,z+edge*.018))
  coords[st]=pts
 append_shape(coords,[(i*2,i*2+1,i*2+3,i*2+2) for i in range(16)],'Hair')
sphere('Face_Nose',(0,surface_y(0,4.39)-.075,4.39),(.107,.105,.075),'Skin','head')
for side in (-1,1):sphere('Ear_Inner',(.999*side,-.044,4.475),(.086,.040,.154),'Cheek','head')
def mouth_dims(st):
 if st=='Surprised':return .125,.145,.16,4.145
 if st=='Worried':return .17,.032,.055,4.13
 if st=='Sad':return .15,.027,.035,4.13
 if st=='Smile':return .26,.055,.175,4.175
 return .222,.036,.147,4.16
coords={};seg=48
for st in states:
 w,up,down,z0=mouth_dims(st);pts=[(0,surface_y(0,z0)-.035,z0)]
 for i in range(seg):
  t=2*pi*i/seg;x=w*cos(t);z=z0+(up if sin(t)>=0 else down)*sin(t)
  if st in ('Worried','Sad'):z-=.04*cos(t)**2
  pts.append((x,surface_y(x,z)-.039,z))
 coords[st]=pts
append_shape(coords,[(0,1+i,1+(i+1)%seg) for i in range(seg)],'Mouth')
# A thin brown border around the mouth, with the same expression topology.
coords={}
for st in states:
 w,up,down,z0=mouth_dims(st);pts=[]
 for i in range(seg):
  t=2*pi*i/seg
  for d in (0,.018):
   x=(w+d)*cos(t);z=z0+((up if sin(t)>=0 else down)+d)*sin(t)
   if st in ('Worried','Sad'):z-=.04*cos(t)**2
   pts.append((x,surface_y(x,z)-.042,z))
 coords[st]=pts
append_shape(coords,[(2*i,2*i+1,2*((i+1)%seg)+1,2*((i+1)%seg)) for i in range(seg)],'Ink')
# Teeth and tongue remain inside the mouth; shrink for the round surprised mouth.
for part,mat in [('teeth','Paper'),('tongue','Tongue')]:
 coords={}
 for st in states:
  w,up,down,z0=mouth_dims(st);pts=[];hide=st in ('Worried','Sad');scale=.001 if hide else 1
  for i in range(25):
   t=-1+2*i/24;x=w*.76*t*scale
   if part=='teeth':z=z0+up*.60;h=.042*(1-t*t)*scale
   else:z=z0-down*.77;h=.042*(1-t*t)*scale
   if st=='Surprised':h*=.35
   for edge in (-1,1):
    zz=z+edge*h*.5;pts.append((x,surface_y(x,zz)-.047,zz))
  coords[st]=pts
 append_shape(coords,[(i*2,i*2+1,i*2+3,i*2+2) for i in range(24)],mat)
for i,p in enumerate(faces):
 a,b,c=[Vector(fv['Basis'][j]) for j in p[:3]]
 if (b-a).cross(c-a).y>0:faces[i]=tuple(reversed(p))
mesh=bpy.data.meshes.new('Face_MorphTopology');mesh.from_pydata(fv['Basis'],[],faces);mesh.update();face=bpy.data.objects.new('Friend_ExpressionMesh',mesh);bpy.context.collection.objects.link(face)
for n in palette:mesh.materials.append(mats[n])
for p,n in zip(mesh.polygons,fm):p.material_index=list(palette).index(n);p.use_smooth=True
for st in states:
 key=face.shape_key_add(name=st)
 for v,co in zip(key.data,fv[st]):v.co=co
features.append(face);rigid[face]='head'
# Deformation skeleton in the exact proportions of the supplied T pose.
armdata=bpy.data.armatures.new('Friend_Skeleton');arm=bpy.data.objects.new('Friend_Rig',armdata);bpy.context.collection.objects.link(arm);active(arm);bpy.ops.object.mode_set(mode='EDIT')
spec=[('root',(0,0,0),(0,0,.5),None),('hips',(0,.06,1.62),(0,.06,2.02),'root'),('spine',(0,.06,2.02),(0,.06,2.65),'hips'),('chest',(0,.06,2.65),(0,.06,3.40),'spine'),('neck',(0,.06,3.40),(0,.06,3.84),'chest'),('head',(0,.06,3.84),(0,.06,5.55),'neck')]
for side,suffix in [(1,'L'),(-1,'R')]:
 s=side
 spec.extend([('shoulder.'+suffix,(s*.15,.06,3.34),(s*.91,.06,3.30),'chest'),('upper_arm.'+suffix,(s*.91,.06,3.30),(s*1.66,.06,3.24),'shoulder.'+suffix),('forearm.'+suffix,(s*1.66,.06,3.24),(s*2.37,.06,3.20),'upper_arm.'+suffix),('hand.'+suffix,(s*2.37,.06,3.20),(s*2.86,.06,3.20),'forearm.'+suffix),('thigh.'+suffix,(s*.405,.06,1.62),(s*.415,.015,1.02),'hips'),('shin.'+suffix,(s*.415,.015,1.02),(s*.415,.06,.43),'thigh.'+suffix),('foot.'+suffix,(s*.415,.06,.43),(s*.415,-.48,.18),'shin.'+suffix),('toe.'+suffix,(s*.415,-.48,.18),(s*.415,-.69,.17),'foot.'+suffix)])
for n,h,t,parent in spec:
 b=armdata.edit_bones.new(n);b.head=h;b.tail=t
 if parent:b.parent=armdata.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT');arm.show_in_front=True;armdata.display_type='OCTAHEDRAL'
def smoothstep(a,b,x):
 t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
def blend(a,b,t):return {a:1-t,b:t}
def weights(p):
 x,y,z=p;ax=abs(x);s='L' if x>=0 else 'R'
 if z<1.75:
  if z<.34:return {'foot.'+s:1}
  if z<.65:return blend('foot.'+s,'shin.'+s,smoothstep(.34,.65,z))
  if z<1.18:return blend('shin.'+s,'thigh.'+s,smoothstep(.87,1.18,z))
  return blend('thigh.'+s,'hips',smoothstep(1.37,1.78,z))
 if z<2.5:torso=blend('hips','spine',smoothstep(1.82,2.40,z))
 elif z<3.22:torso=blend('spine','chest',smoothstep(2.4,2.95,z))
 else:torso={'chest':1}
 amount=smoothstep(.70,1.12,ax)*smoothstep(2.40,2.95,z)
 if amount==0:return torso
 if ax<1.86:armweights=blend('upper_arm.'+s,'forearm.'+s,smoothstep(1.47,1.86,ax))
 else:armweights=blend('forearm.'+s,'hand.'+s,smoothstep(2.27,2.40,ax))
 result={n:w*(1-amount) for n,w in torso.items()}
 for n,w in armweights.items():result[n]=result.get(n,0)+w*amount
 return result
for o in original+features:
 mask=0 if o==face or o.name.startswith(('Face_Nose','Ear_Inner')) else (.75 if o==hair or o==tuft else 1)
 attribute=o.data.color_attributes.new(name='OutlineMask',type='BYTE_COLOR',domain='POINT')
 attribute.data.foreach_set('color',[1,1,1,mask]*len(o.data.vertices))
 for n,_,_,_ in spec:o.vertex_groups.new(name=n)
 for v in o.data.vertices:
  ws={rigid[o]:1} if o in rigid else weights(v.co)
  if o==body and v.index in body_rigid:ws={body_rigid[v.index]:1}
  for n,w in ws.items():
   if w>.00001:o.vertex_groups[n].add([v.index],w,'REPLACE')
 # Bake a 1.48 m height into both geometry and bind pose, never into root scale.
 for v in o.data.vertices:v.co*=.24
 if o.data.shape_keys:
  for k in o.data.shape_keys.key_blocks:
   for v in k.data:v.co*=.24
 o.parent=arm;mod=o.modifiers.new('Friend_Deform','ARMATURE');mod.object=arm
active(arm);bpy.ops.object.mode_set(mode='EDIT')
for b in armdata.edit_bones:b.head*=.24;b.tail*=.24
bpy.ops.object.mode_set(mode='OBJECT')
# One skinned renderer keeps expressions and existing game animation lookup coherent.
active(face)
for o in original+features:o.select_set(True)
bpy.ops.object.join();face.name='Friend_Mesh';face.data.name='Friend_SkinnedMesh';smooth(face)
# One small palette atlas and one surface material for the entire character.
texdir=ROOT+r'\Arts\Textures';os.makedirs(texdir,exist_ok=True)
atlas_width=PALETTE_COLS*16;atlas_height=PALETTE_ROWS*16
atlas=bpy.data.images.new('Friend_Palette',width=atlas_width,height=atlas_height,alpha=True)
pixels=[0.0]*(atlas_width*atlas_height*4)
for j,(name,h) in enumerate(palette.items()):
 color=[int(h[i:i+2],16)/255 for i in (0,2,4)]+[1]
 for yy in range((j//PALETTE_COLS)*16,(j//PALETTE_COLS+1)*16):
  for xx in range((j%PALETTE_COLS)*16,(j%PALETTE_COLS+1)*16):pixels[(yy*atlas_width+xx)*4:(yy*atlas_width+xx)*4+4]=color
atlas.pixels=pixels;atlas.filepath_raw=os.path.join(texdir,'FriendImprovedPalette.png');atlas.file_format='PNG';atlas.save()
atlas.pack()
for layer in list(face.data.uv_layers):face.data.uv_layers.remove(layer)
uv=face.data.uv_layers.new(name='PaletteUV')
for p in face.data.polygons:
 name=face.data.materials[p.material_index].name;j=list(palette).index(name)
 for loop in p.loop_indices:uv.data[loop].uv=((j%PALETTE_COLS+.5)/PALETTE_COLS,(j//PALETTE_COLS+.5)/PALETTE_ROWS)
 p.material_index=0
single=bpy.data.materials.new('Friend_Atlas_Toon');single.use_nodes=True;single.diffuse_color=(1,1,1,1)
nodes=single.node_tree.nodes;nodes.clear();out=nodes.new('ShaderNodeOutputMaterial');diff=nodes.new('ShaderNodeBsdfDiffuse');diff.inputs['Color'].default_value=(1,1,1,1)
lighting=nodes.new('ShaderNodeShaderToRGB');bands=nodes.new('ShaderNodeValToRGB');bands.color_ramp.interpolation='CONSTANT';bands.color_ramp.elements[0].position=.15;bands.color_ramp.elements[0].color=(.73,.78,.76,1);bands.color_ramp.elements[1].position=.68;bands.color_ramp.elements[1].color=(1,1,1,1);bands.color_ramp.elements.new(.38).color=(.90,.90,.90,1)
tex=nodes.new('ShaderNodeTexImage');tex.image=atlas;tex.interpolation='Closest';multiply=nodes.new('ShaderNodeMixRGB');multiply.blend_type='MULTIPLY';multiply.inputs[0].default_value=1;em=nodes.new('ShaderNodeEmission')
links=single.node_tree.links
for a,b in [(diff.outputs[0],lighting.inputs[0]),(lighting.outputs[0],bands.inputs[0]),(bands.outputs[0],multiply.inputs[1]),(tex.outputs['Color'],multiply.inputs[2]),(multiply.outputs[0],em.inputs[0]),(em.outputs[0],out.inputs[0])]:links.new(a,b)
face.data.materials.clear();face.data.materials.append(single)
for material in list(bpy.data.materials):
 if material!=single:bpy.data.materials.remove(material,do_unlink=True)
# Preserve the bone name used by the game's pelvis and phone interactions.
armdata.bones['hips'].name='pelvis'
for o in list(bpy.data.objects):
 if o.type in ('CAMERA','LIGHT'):bpy.data.objects.remove(o,do_unlink=True)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
scene.render.engine='BLENDER_EEVEE'
scene.render.resolution_x=1200;scene.render.resolution_y=1200;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.film_transparent=False
scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.view_settings.exposure=0;scene.view_settings.gamma=1
world=bpy.data.worlds.new('Warm studio');scene.world=world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.32,.40,.36,1);world.node_tree.nodes['Background'].inputs[1].default_value=.65
bpy.ops.object.light_add(type='SUN',location=(-3,-4,6));light=bpy.context.object;light.name='Preview_Key';light.rotation_euler=(math.radians(25),math.radians(-25),math.radians(-25));light.data.energy=2.0;light.data.angle=.2
bpy.ops.object.camera_add(location=(2.5,-4.5,2.2));cam=bpy.context.object;cam.name='Preview_Camera';cam.data.type='ORTHO';cam.data.ortho_scale=1.80;scene.camera=cam
target=Vector((0,0,.76))
def camera(loc):cam.location=loc;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
def reset():
 for p in arm.pose.bones:p.rotation_mode='XYZ';p.rotation_euler=(0,0,0)
 for k in face.data.shape_keys.key_blocks:k.value=0
def relaxed():
 reset()
 for s,sign in [('L',1),('R',-1)]:arm.pose.bones['upper_arm.'+s].rotation_euler.x=math.radians(-62);arm.pose.bones['forearm.'+s].rotation_euler.z=math.radians(9*sign)
def render(name,loc):camera(loc);scene.render.filepath=os.path.join(QA,name+'.png');bpy.context.view_layer.update();bpy.ops.render.render(write_still=True)
relaxed();render('improved_front',(0,-4,1.06));render('improved_threequarter',(2.5,-4,1.85));render('improved_back',(2.2,4,1.65))
reset();arm.pose.bones['upper_arm.L'].rotation_euler.x=math.radians(-55);arm.pose.bones['forearm.L'].rotation_euler.z=math.radians(-65);arm.pose.bones['thigh.R'].rotation_euler.x=math.radians(-22);arm.pose.bones['shin.R'].rotation_euler.x=math.radians(45);render('rig_bend_test',(2.5,-4,1.85))
relaxed();face.data.shape_keys.key_blocks['Surprised'].value=1;render('expression_surprised',(0,-4,1.06))
relaxed();face.data.shape_keys.key_blocks['Worried'].value=1;render('expression_worried',(0,-4,1.06))
relaxed();face.data.shape_keys.key_blocks['Blink'].value=1;render('expression_blink',(0,-4,1.06))
reset()
active(arm);face.select_set(True)
fbx=os.path.join(OUT,'Wait_Friend_Improved.fbx')
bpy.ops.export_scene.fbx(filepath=fbx,use_selection=True,object_types={'ARMATURE','MESH'},use_mesh_modifiers=False,add_leaf_bones=False,use_armature_deform_only=True,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',use_custom_props=True)
relaxed();camera((2.5,-4,1.85));active(arm)
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':area.spaces.active.region_3d.view_distance=2.5;area.spaces.active.region_3d.view_location=(0,0,.8);area.spaces.active.shading.type='MATERIAL'
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(SRC,'친구_개선.blend'))
report={'source':r'E:\ProjectWait\Blender\직접모델링\친구.blend','height_m':1.48,'vertices':len(face.data.vertices),'triangles':sum(len(p.vertices)-2 for p in face.data.polygons),'bones':[b.name for b in armdata.bones],'blendshapes':[k.name for k in face.data.shape_keys.key_blocks],'unweighted_vertices':sum(1 for v in face.data.vertices if not v.groups),'materials':[{ 'name':m.name,'hex':palette.get(m.name,'')} for m in face.data.materials],'fbx':fbx}
with open(os.path.join(QA,'blender_validation.json'),'w',encoding='utf8') as f:json.dump(report,f,ensure_ascii=False,indent=2)
print('FRIEND_COMPLETE',json.dumps({k:v for k,v in report.items() if k not in ('materials','bones')},ensure_ascii=False))
