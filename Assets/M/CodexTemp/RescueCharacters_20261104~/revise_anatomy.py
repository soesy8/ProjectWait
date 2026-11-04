"""User revision: continuous soft shoulders, reference-led face, fitted helmet."""
import bpy, bmesh, math, json
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from pathlib import Path
from math import sin, cos, pi, sqrt
WORK=Path(__file__).resolve().parent
OUT=WORK/'Delivery'
S=.32
bpy.ops.wm.open_mainfile(filepath=str(WORK/'BeforeRevision'/'Wait_Rescue_AB.blend'))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene
outline=bpy.data.collections['00_INK • 갈색 실루엣 대상']

def sgnpow(v,p): return math.copysign(abs(v)**p,v)
def newmesh(name,vs,fs,mat,collection,part,bone='head',ink=True):
    me=bpy.data.meshes.new(name+' mesh'); me.from_pydata(vs,[],fs); me.update()
    bm=bmesh.new(); bm.from_mesh(me); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(me); bm.free()
    ob=bpy.data.objects.new(name,me); collection.objects.link(ob)
    if ink: outline.objects.link(ob)
    me.materials.append(mat)
    for p in me.polygons: p.use_smooth=True
    ob['Part']=part; ob['BindBone']=bone
    return ob
def bind(ob,rig,bone=None,jacket=False):
    ob.vertex_groups.clear()
    if not jacket:
        vg=ob.vertex_groups.new(name=bone or ob['BindBone']); vg.add(list(range(len(ob.data.vertices))),1,'REPLACE')
    else:
        names=['chest','upper_arm.L','upper_arm.R','forearm.L','forearm.R']
        gs={n:ob.vertex_groups.new(name=n) for n in names}
        for v in ob.data.vertices:
            x,y,z=v.co/S; side='L' if x>=0 else 'R'
            # Smooth shoulder blend and elbow blend over the same continuous surface.
            w=max(0,min(1,(abs(x)-.56)/.29)); w=w*w*(3-2*w)
            fore=max(0,min(1,(2.86-z)/.30)); fore=fore*fore*(3-2*fore)
            weights={'chest':1-w,'upper_arm.'+side:w*(1-fore),'forearm.'+side:w*fore}
            for n,a in weights.items():
                if a>1e-7: gs[n].add([v.index],a,'REPLACE')
    mod=ob.modifiers.new('FK skin • smooth shoulder weights','ARMATURE'); mod.object=rig; ob.parent=rig

def evaluated_mesh(ob,name):
    # Rest-pose local coordinates, independent of the pair layout rig translation.
    arm=[m for m in ob.modifiers if m.type=='ARMATURE']
    for m in arm: m.show_viewport=False
    bpy.context.view_layer.update()
    me=bpy.data.meshes.new_from_object(ob.evaluated_get(bpy.context.evaluated_depsgraph_get()))
    for m in arm: m.show_viewport=True
    clone=bpy.data.objects.new(name,me); scene.collection.objects.link(clone)
    return clone

def sleeve(name,s,mat,c):
    pts=[(s*.49,0,3.15),(s*.69,0,3.14),(s*.89,-.01,2.95),(s*1.035,-.035,2.68),(s*1.14,-.06,2.43),(s*1.18,-.07,2.28)]
    rs=[.21,.265,.276,.254,.225,.195]; n=32
    pts=[Vector(p)*S for p in pts]; vs=[]; fs=[]
    for i,(p,r) in enumerate(zip(pts,rs)):
        t=(pts[min(i+1,len(pts)-1)]-pts[max(i-1,0)]).normalized()
        u=t.cross(Vector((0,1,0))).normalized(); v=t.cross(u).normalized()
        for j in range(n):
            a=j*2*pi/n; vs.append(p+(u*cos(a)+v*sin(a))*r*S)
    for k in range(len(pts)-1):
        for j in range(n): fs.append((k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j))
    fs.extend([tuple(reversed(range(n))),tuple((len(pts)-1)*n+j for j in range(n))])
    ob=newmesh(name,vs,fs,mat,c,'TEMP sleeve','chest',False)
    mod=ob.modifiers.new('Rounded sleeve cage','SUBSURF'); mod.levels=1
    bpy.context.view_layer.objects.active=ob; bpy.ops.object.modifier_apply(modifier=mod.name)
    return ob

revision=[]
for which in ('A','B'):
    prefix=which+' | '; rig=bpy.data.objects[prefix+'RIG • Pose Mode']
    c=next(co for co in bpy.data.collections if co.name.startswith(which+' · '))
    cloth=bpy.data.materials['A · rescue orange | #FF8C2D' if which=='A' else 'B · deep teal']
    # Unified jacket with continuous shoulder and armpit transitions.
    body=bpy.data.objects[prefix+'Jacket • fitted padded body']
    base=evaluated_mesh(body,'TEMP closed jacket')
    bm=bmesh.new(); bm.from_mesh(base.data)
    bmesh.ops.holes_fill(bm,edges=[e for e in bm.edges if e.is_boundary],sides=0)
    bm.to_mesh(base.data); bm.free()
    pieces=[base,sleeve('TEMP sleeve R',-1,cloth,c),sleeve('TEMP sleeve L',1,cloth,c)]
    bpy.ops.object.select_all(action='DESELECT')
    for p in pieces: p.select_set(True)
    bpy.context.view_layer.objects.active=base; bpy.ops.object.join()
    base.modifiers.clear(); base.vertex_groups.clear()
    mod=base.modifiers.new('Continuous shoulder union','REMESH'); mod.mode='VOXEL'; mod.voxel_size=.032*S; mod.use_smooth_shade=True
    bpy.ops.object.modifier_apply(modifier=mod.name)
    mod=base.modifiers.new('Smooth neck shoulder arm transition','SMOOTH'); mod.factor=.75; mod.iterations=7
    bpy.ops.object.modifier_apply(modifier=mod.name)
    mod=base.modifiers.new('Soft continuous jacket surface','SUBSURF'); mod.levels=1
    bpy.ops.object.modifier_apply(modifier=mod.name)
    # An open tailored hem avoids a closed underside cutting through the trousers.
    bm=bmesh.new(); bm.from_mesh(base.data)
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-6,
        plane_co=(0,0,2.105*S),plane_no=(0,0,1),clear_inner=True,clear_outer=False)
    bm.to_mesh(base.data); bm.free()
    for ob in list(bpy.data.objects):
        if ob.name.startswith(prefix) and (ob.get('Part','') in {'Jacket • fitted padded body','Jacket • navy shoulder yoke'} or ob.get('Part','').startswith(('Sleeve.','Shoulder cap.'))):
            bpy.data.objects.remove(ob,do_unlink=True)
    for old in list(base.users_collection): old.objects.unlink(base)
    c.objects.link(base); outline.objects.link(base)
    base.name=prefix+'Jacket • continuous shoulders and arms'
    base['Part']='Jacket • continuous shoulders and arms'; base['BindBone']='jacket'
    base.data.materials.clear(); base.data.materials.append(cloth)
    for p in base.data.polygons: p.material_index=0; p.use_smooth=True
    # One continuous fitted navy yoke; a normal offset prevents coincident surfaces.
    me=base.data.copy(); yoke=bpy.data.objects.new(prefix+'Yoke • fitted continuous shoulder panel',me); c.objects.link(yoke)
    bm=bmesh.new(); bm.from_mesh(me)
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-6,
        plane_co=(0,0,3.065*S),plane_no=(0,0,1),clear_inner=True,clear_outer=False)
    bm.normal_update()
    for v in bm.verts: v.co+=v.normal*.012*S
    bm.to_mesh(me); bm.free()
    me.materials.clear(); me.materials.append(bpy.data.materials['Shoulder panels'])
    yoke['Part']='Yoke • fitted continuous shoulder panel'; yoke['BindBone']='jacket'
    # Fit all reflective strips to the rebuilt continuous coat.
    bm=bmesh.new(); bm.from_mesh(base.data); bvh=BVHTree.FromBMesh(bm)
    for ob in bpy.data.objects:
        p=ob.get('Part','')
        if not ob.name.startswith(prefix) or not ('Sleeve yellow band' in p or 'Sleeve ivory band' in p): continue
        for v in ob.data.vertices:
            co,no,idx,dist=bvh.find_nearest(v.co)
            if co is not None: v.co=co+no*(.019 if 'ivory' in p.lower() else .014)*S
        ob.data.update()
    bm.free()
    bind(base,rig,jacket=True); bind(yoke,rig,jacket=True)
    # Broad, soft jaw and full cheeks from a rounded superellipse.
    old=bpy.data.objects[prefix+'Head • 둥근 얼굴']; bpy.data.objects.remove(old,do_unlink=True)
    n=64; nr=40; exponent=2/2.20; rx=.85; ry=.644; rz=.78; cz=4.28
    vs=[]; fs=[]
    for k in range(nr+1):
        t=.0005+(pi-.001)*k/nr
        for j in range(n):
            a=j*2*pi/n; ring=abs(sin(t))**exponent
            vs.append((rx*ring*sgnpow(cos(a),exponent)*S,ry*ring*sgnpow(sin(a),exponent)*S,(cz+rz*sgnpow(cos(t),exponent))*S))
    for k in range(nr):
        for j in range(n): fs.append((k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j))
    fs.extend([tuple(reversed(range(n))),tuple(nr*n+j for j in range(n))])
    head=newmesh(prefix+'Head • 둥근 얼굴',vs,fs,bpy.data.materials['Skin | #FFC08A'],c,'Head • 둥근 얼굴')
    bind(head,rig)
    def oldfy(x,z): return -.635*sqrt(max(.025,1-(x/.87)**2-((z-4.285)/.79)**2))
    def fy(x,z): return -.644*max(.008,1-abs(x/.85)**2.20-abs((z-4.28)/.78)**2.20)**(1/2.20)
    # Reproject every facial feature to the new face, preserving its physical relief.
    for ob in list(bpy.data.objects):
        if not ob.name.startswith(prefix) or ob.type!='MESH': continue
        p=ob.get('Part','')
        if p.startswith(('Eye.','Eye glint.','Eye small glint.')):
            side=1 if p.endswith('.L') else -1
            for v in ob.data.vertices:
                x,y,z=v.co/S; nx=side*.30+(x-side*.30)*1.24; nz=4.37+(z-4.355)*1.19
                v.co=Vector((nx,y-oldfy(x,z)+fy(nx,nz),nz))*S
        elif p.startswith('Eyebrow.'):
            for v in ob.data.vertices:
                x,y,z=v.co/S; nx=x*1.04; nz=4.635+(z-4.61)*1.15
                v.co=Vector((nx,y-oldfy(x,z)+fy(nx,nz)-.012,nz))*S
        elif p.startswith('Lash.'):
            side=1 if p.endswith('.L') else -1
            for v in ob.data.vertices:
                x,y,z=v.co/S; nx=side*.30+(x-side*.30)*1.24; nz=4.37+(z-4.355)*1.19
                v.co=Vector((nx,y-oldfy(x,z)+fy(nx,nz),nz))*S
        elif p.startswith('Cheek.'):
            side=1 if p.endswith('.L') else -1
            for v in ob.data.vertices:
                x,y,z=v.co/S; nx=side*.55+(x-side*.535)*1.20; nz=4.13+(z-4.13)*1.18
                v.co=Vector((nx,y-oldfy(x,z)+fy(nx,nz)-.004,nz))*S
        elif p=='Nose':
            for v in ob.data.vertices:
                x,y,z=v.co/S; nx=x*.93; nz=z-.04
                v.co=Vector((nx,y-oldfy(x,z)+fy(nx,nz),nz))*S
        elif p.startswith('Smile'):
            for v in ob.data.vertices:
                x,y,z=v.co/S; nx=x*1.13; nz=3.915+(z-3.915)*1.10
                v.co=Vector((nx,y-oldfy(x,z)+fy(nx,nz)-.005,nz))*S
    # Rebuild a clean fitted hair shell; raised hairline leaves both eyebrows readable.
    old=bpy.data.objects[prefix+'Hair cap • fitted hairline']; bpy.data.objects.remove(old,do_unlink=True)
    vs=[]; fs=[]; n=64; nr=22
    for k in range(nr+1):
        for j in range(n):
            a=j*2*pi/n; end=1.12+.54*(sin(a)+1)
            if which=='B': end+=.04*max(0,-cos(a))*max(0,-sin(a))
            t=.003+(end-.003)*k/nr
            ring=abs(sin(t))**exponent
            vs.append((.892*ring*sgnpow(cos(a),exponent)*S,.690*ring*sgnpow(sin(a),exponent)*S,(4.28+.82*sgnpow(cos(t),exponent))*S))
    for k in range(nr):
        for j in range(n): fs.append((k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j))
    hairmat=bpy.data.materials['A · chestnut hair' if which=='A' else 'B · chestnut hair']
    hair=newmesh(prefix+'Hair cap • fitted hairline',vs,fs,hairmat,c,'Hair cap • fitted hairline')
    bind(hair,rig)
    for ob in list(bpy.data.objects):
        if not ob.name.startswith(prefix) or ob.type!='MESH': continue
        p=ob.get('Part','')
        if p.startswith('Hair fringe') or p=='Hair • swept fringe':
            center=sum((v.co for v in ob.data.vertices),Vector())/len(ob.data.vertices)
            for v in ob.data.vertices:
                v.co.z=center.z+(v.co.z-center.z)*(.70 if which=='A' else .77)+.045*S
                v.co.y-=.028*S
        if p.startswith('Helmet') and not ('chin strap' in p.lower() or 'fastener' in p.lower()):
            for v in ob.data.vertices:
                v.co.x*=.94; v.co.y*=.96; v.co.z=4.78*S+(v.co.z-4.89*S)*.90
    # Lift the ear slightly and flatten the existing straps against the wider jaw.
    for ob in bpy.data.objects:
        if not ob.name.startswith(prefix) or ob.type!='MESH': continue
        p=ob.get('Part','')
        if p.startswith(('Ear.','Ear inner.')):
            for v in ob.data.vertices: v.co.z+=.015*S; v.co.x*=1.015
        if p.startswith('Helmet chin strap.'):
            for v in ob.data.vertices:
                z=v.co.z/S; t=max(0,min(1,(z-3.9)/.95))
                v.co.z-=.11*t*S
                v.co.x*=1.035
    revision.append({'id':which,'jacket_vertices':len(base.data.vertices),'head_vertices':len(head.data.vertices),'shoulders':'single continuous mesh with blended FK weights','face':'broad rounded jaw, larger eyes, visible brows, fitted hairline','helmet':'width -6%, depth -4%, crown height -10%, seated 0.11 model units lower'})

scene['Revision']='User feedback: seamless shoulders / concept face / fitted helmet'
scene['Model_height_m']=round(5.424*S,3)
readme=bpy.data.texts['읽어주세요 • README']
txt=readme.as_string().replace('높이 약 1.79m','높이 약 1.74m')
txt+='\n수정 반영: 몸통·어깨·팔을 연속 메시로 재구성하고 어깨 가중치를 연결했습니다.\n넓고 둥근 턱·볼, 큰 눈과 보이는 눈썹, 머리에 맞는 앞머리·안전모 비율로 수정했습니다.\n'
readme.clear(); readme.write(txt); (OUT/'README_구조요원.txt').write_text(txt,encoding='utf-8-sig')
scene.render.resolution_x=1800; scene.render.resolution_y=1560; scene.render.resolution_percentage=65
scene.camera=bpy.data.objects['CAM_00 • 두 구조요원']; scene.render.filepath='//Preview_00_AB.png'
bpy.ops.object.select_all(action='DESELECT'); head=bpy.data.objects['A | Head • 둥근 얼굴']; head.select_set(True); bpy.context.view_layer.objects.active=head
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Wait_Rescue_AB.blend'))
(WORK/'Revision_Validation.json').write_text(json.dumps(revision,ensure_ascii=False,indent=2),encoding='utf-8')
scene.render.filepath=str(WORK/'Revision_AB.png'); bpy.ops.render.render(write_still=True)
print('REVISION_PAIR_DONE',flush=True)
for ob in bpy.data.objects:
    if ob.name.startswith('B | '): ob.hide_render=True
    if ob.name.startswith('Studio contact shadow '): ob.hide_render=True
    if ob.type=='ARMATURE': ob.location.x=0
scene.render.resolution_x=1000; scene.render.resolution_y=1200; scene.render.resolution_percentage=65
for view,cam in [('Front','CAM_01 • 정면'),('Side','CAM_02 • 측면')]:
    scene.camera=bpy.data.objects[cam]; scene.render.filepath=str(WORK/('Revision_A_'+view+'.png'))
    bpy.ops.render.render(write_still=True); print('REVISION_VIEW '+view,flush=True)
