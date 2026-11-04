"""Two reference-led, editable 3D rescuers. All generated files stay beside this script."""
import bpy, bmesh, math, json
from math import sin, cos, pi, sqrt
from mathutils import Vector, Quaternion
from pathlib import Path

WORK = Path(__file__).resolve().parent
OUT = WORK / 'Delivery'
OUT.mkdir(parents=True, exist_ok=True)
S = .32
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.preferences.filepaths.save_version = 0
scene = bpy.context.scene
scene.name = 'Wait! | 구조요원 A · B'
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x = 1600
scene.render.resolution_y = 1400
scene.render.resolution_percentage = 65
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
scene.render.fps = 24
scene.view_settings.view_transform = 'Standard'
scene.view_settings.look = 'None'
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
scene.render.use_freestyle = True

def col(name, parent=None):
    c=bpy.data.collections.new(name)
    (parent or scene.collection).children.link(c)
    return c

outline=col('00_INK • 갈색 실루엣 대상')
characters=col('01_CHARACTERS • 부위별 편집')
rigs=col('02_RIGS • 기본 FK 포즈')
studio=col('03_STUDIO • 카메라 · 조명')
refs=col('04_REFERENCES • 첨부 원본 (숨김)')
refs.hide_render=True
refs.hide_viewport=True

def lin(x): return x/12.92 if x<=.04045 else ((x+.055)/1.055)**2.4
def rgba(h): return tuple(lin(int(h.lstrip('#')[i:i+2],16)/255) for i in (0,2,4))+(1,)
M={}
def toon(name,h,flat=False):
    m=bpy.data.materials.new(name)
    m.diffuse_color=rgba(h); m.use_nodes=True
    m['BaseColor_sRGB']=h
    m['Blender_shading']='Geometric 3-band toon / narrow transitions / no textures'
    ns=m.node_tree.nodes; lk=m.node_tree.links; ns.clear()
    out=ns.new('ShaderNodeOutputMaterial'); out.location=(650,0)
    em=ns.new('ShaderNodeEmission'); em.location=(430,0); lk.new(em.outputs[0],out.inputs['Surface'])
    if flat: em.inputs['Color'].default_value=rgba(h)
    else:
        geo=ns.new('ShaderNodeNewGeometry'); geo.location=(-650,0)
        dot=ns.new('ShaderNodeVectorMath'); dot.operation='DOT_PRODUCT'; dot.location=(-450,0)
        dot.inputs[1].default_value=Vector((-.45,-.85,.8)).normalized()
        lk.new(geo.outputs['Normal'],dot.inputs[0])
        ramp=ns.new('ShaderNodeValToRGB'); ramp.location=(-160,0)
        ramp.label='그림자 · 중간톤 · 밝은 면 / 3 bands'
        ramp.color_ramp.interpolation='LINEAR'
        rgb=[int(h.lstrip('#')[i:i+2],16)/255 for i in (0,2,4)]
        def tint(mul): return tuple(lin(rgb[i]*mul[i]) for i in range(3))+(1,)
        low=tint((.741,.784,.765)); mid=tint((.90,.90,.90)); hi=rgba(h)
        stops=[(0,low),(.008,low),(.032,mid),(.608,mid),(.632,hi),(1,hi)]
        for i,(p,c) in enumerate(stops):
            e=ramp.color_ramp.elements[i] if i<2 else ramp.color_ramp.elements.new(p)
            e.position=p; e.color=c
        lk.new(dot.outputs['Value'],ramp.inputs[0]); lk.new(ramp.outputs['Color'],em.inputs['Color'])
    M[name]=m
    return m

skin=toon('Skin | #FFC08A','#FFC08A')
earmat=toon('Ear warmth','#EF9B6D')
blush=toon('Cheek coral','#F99A76',True)
ink=toon('Ink | #493327','#493327',True)
eye=toon('Eyes espresso','#2B211B',True)
cream=toon('Reflective ivory | #FFF0C5','#FFF0C5')
paper=toon('Paper | #FFF6DB','#FFF6DB',True)
yellow=toon('Reflective lemon','#FFE56F')
gold=toon('Badge gold','#F9BE36')
orange=toon('A · rescue orange | #FF8C2D','#FF8C2D')
orange_dark=toon('A · cuff orange | #E87927','#E87927')
orange_line=toon('A · tailored seam','#B85C25')
teal=toon('B · deep teal','#297F7D')
teal_cuff=toon('B · cuff teal','#226A68')
teal_line=toon('B · tailored seam','#205957')
teal_high=toon('B · collar piping','#58A99A')
navy=toon('Cargo navy | #3B3E48','#3B3E48')
yoke=toon('Shoulder panels','#424751')
navyline=toon('Cargo seams','#292D36')
rubber=toon('Boots warm charcoal','#373434')
solemat=toon('Boot soles','#292727')
bootpanel=toon('Boot toe panels','#454142')
strap=toon('Straps and belt','#343233')
steel=toon('Buckle warm steel','#9A9183')
radio_mat=toon('Radio casing','#45494B')
radio_edge=toon('Radio trim','#272D30')
screen=toon('Radio screen','#759C8C')
hairA=toon('A · chestnut hair','#573727')
hairB=toon('B · chestnut hair','#66412E')
hairlight=toon('Hair warm locks','#77503A')
helmetB=toon('B · safety yellow helmet','#FFD679')
helmetBline=toon('B · helmet moulding','#D6A448')
glovemat=toon('Work gloves','#BAAA8F')
gloveline=toon('Work glove seams','#7C7265')
mouth=toon('Smile inside','#76372C',True)
tongue=toon('Smile tongue','#E77D70',True)
white=toon('Eye glints and teeth','#FFF8E8',True)

ALL=[]; CURRENT=None; PREFIX=''; PARTS=[]
def link(ob,c):
    for old in list(ob.users_collection): old.objects.unlink(ob)
    c.objects.link(ob)
def finish(ob,name,mat,bone='chest',outlined=True,smooth=True):
    ob.name=PREFIX+name; link(ob,CURRENT)
    if outlined: outline.objects.link(ob)
    if mat: ob.data.materials.append(mat)
    if ob.type=='MESH':
        for p in ob.data.polygons: p.use_smooth=smooth
    ob['Part']=name; ob['BindBone']=bone
    PARTS.append(ob); ALL.append(ob)
    return ob
def mesh(name,vs,fs,mat,bone='chest',outlined=True,sub=0):
    me=bpy.data.meshes.new(PREFIX+name+'_Mesh'); me.from_pydata(vs,[],fs); me.update()
    bm=bmesh.new(); bm.from_mesh(me); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(me); bm.free()
    ob=bpy.data.objects.new(PREFIX+name,me); CURRENT.objects.link(ob)
    finish(ob,name,mat,bone,outlined)
    if sub:
        mod=ob.modifiers.new('Rounded quad surface','SUBSURF'); mod.levels=sub; mod.render_levels=sub
    return ob
def ell(name,loc,scale,mat,bone='chest',rot=(0,0,0),outlined=True,seg=32,rings=20):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg,ring_count=rings,location=loc)
    ob=bpy.context.object; ob.scale=scale; ob.rotation_euler=rot
    return finish(ob,name,mat,bone,outlined)
def box(name,loc,dims,bev,mat,bone='chest',rot=(0,0,0),outlined=True):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    ob=bpy.context.object; ob.dimensions=dims
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    ob.rotation_euler=rot; finish(ob,name,mat,bone,outlined)
    mod=ob.modifiers.new('Soft tailored bevel','BEVEL'); mod.width=bev; mod.segments=3
    mod=ob.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL'); mod.keep_sharp=True
    return ob
def curve(name,pts,r,mat,bone='chest',cyclic=False,outlined=False):
    cu=bpy.data.curves.new(PREFIX+name,'CURVE'); cu.dimensions='3D'; cu.resolution_u=12
    cu.bevel_depth=r; cu.bevel_resolution=2; cu.use_fill_caps=True
    sp=cu.splines.new('BEZIER'); sp.bezier_points.add(len(pts)-1)
    for b,p in zip(sp.bezier_points,pts): b.co=p; b.handle_left_type='AUTO'; b.handle_right_type='AUTO'
    sp.use_cyclic_u=cyclic
    ob=bpy.data.objects.new(PREFIX+name,cu); CURRENT.objects.link(ob)
    return finish(ob,name,mat,bone,outlined)
def loft(name,rs,mat,bone='chest',n=40,sub=1):
    vs=[]; fs=[]
    for z,x,y,rx,ry in rs:
        for j in range(n):
            a=j*2*pi/n; vs.append((x+rx*cos(a),y+ry*sin(a),z))
    for k in range(len(rs)-1):
        for j in range(n): fs.append((k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j))
    fs.extend([tuple(reversed(range(n))),tuple((len(rs)-1)*n+j for j in range(n))])
    return mesh(name,vs,fs,mat,bone,True,sub)
def tube(name,pts,rs,mat,bone,n=28,sub=1,outlined=True):
    vs=[]; fs=[]; pts=[Vector(p) for p in pts]
    for i,(p,r) in enumerate(zip(pts,rs)):
        t=(pts[min(i+1,len(pts)-1)]-pts[max(0,i-1)]).normalized()
        u=t.cross(Vector((0,1,0))).normalized(); v=t.cross(u).normalized()
        rx,ry=r if isinstance(r,tuple) else (r,r)
        for j in range(n):
            a=j*2*pi/n; vs.append(p+u*rx*cos(a)+v*ry*sin(a))
    for k in range(len(pts)-1):
        for j in range(n): fs.append((k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j))
    fs.extend([tuple(reversed(range(n))),tuple((len(pts)-1)*n+j for j in range(n))])
    return mesh(name,vs,fs,mat,bone,outlined,sub)
def disk(name,loc,r,depth,mat,bone='pelvis',axis=(0,-1,0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=40,radius=r,depth=depth,location=loc)
    ob=bpy.context.object; ob.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler()
    finish(ob,name,mat,bone)
    mod=ob.modifiers.new('Rounded rim','BEVEL'); mod.width=.015; mod.segments=3
    ob.modifiers.new('Rim normals','WEIGHTED_NORMAL')
    return ob
def patch(name,pts,mat,bone='chest',outlined=False):
    # Convex decorative plate; triangles remain independently editable.
    center=sum((Vector(p) for p in pts),Vector())/len(pts)
    return mesh(name,[center]+pts,[(0,i+1,(i+1)%len(pts)+1) for i in range(len(pts))],mat,bone,outlined)

def hand(sign,side):
    objs=[]; x=sign*1.21; y=-.08
    objs.append(ell('Hand palm.'+side,(x,y,2.03),(.18,.125,.22),skin,'hand.'+side))
    for i in range(4):
        fx=x+sign*(-.112+i*.075)
        objs.append(ell('Finger.'+side+'.'+str(i),(fx,y-.015,1.88+(.018 if i in (0,3) else 0)),(.047,.105,.13 if i in (1,2) else .105),skin,'hand.'+side,seg=20,rings=12))
    objs.append(ell('Thumb.'+side,(x-sign*.16,y-.055,2.02),(.075,.105,.13),skin,'hand.'+side,rot=(0,-sign*.4,0),seg=24,rings=16))
    bpy.ops.object.select_all(action='DESELECT')
    for ob in objs: ob.select_set(True)
    bpy.context.view_layer.objects.active=objs[0]; bpy.ops.object.join()
    ob=objs[0]; ob.name=PREFIX+'Hand • rounded fingers.'+side; ob['Part']='Hand • rounded fingers.'+side
    for gone in objs[1:]:
        PARTS.remove(gone); ALL.remove(gone)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    mod=ob.modifiers.new('Unified palm and fingers','REMESH'); mod.mode='VOXEL'; mod.voxel_size=.026; mod.use_smooth_shade=True
    bpy.ops.object.modifier_apply(modifier=mod.name)
    mod=ob.modifiers.new('Soft hand surface','SMOOTH'); mod.factor=.65; mod.iterations=4
    bpy.ops.object.modifier_apply(modifier=mod.name)
    for i in range(3):
        fx=x+sign*(-.075+i*.075)
        curve('Finger crease.'+side+'.'+str(i),[(fx,y-.115,1.86),(fx,y-.129,1.92)],.006,earmat,'hand.'+side)

def make_character(which):
    global CURRENT,PREFIX,PARTS
    A=which=='A'; PREFIX=which+' | '; PARTS=[]
    CURRENT=col(('A · 소방 구조대원' if A else 'B · 승강기 전문 구조요원'),characters)
    cloth=orange if A else teal; cuff=orange_dark if A else teal_cuff
    seam=orange_line if A else teal_line; hair=hairA if A else hairB
    # A broad cheek silhouette, small chin and friendly readable face.
    head=ell('Head • 둥근 얼굴',(0,0,4.035),(.845,.635,.79),skin,'head',seg=56,rings=36)
    for v in head.data.vertices: v.co.x*=1+.04*math.exp(-((v.co.z+.2)/.38)**2)
    def fy(x,z,off=0): return -.635*sqrt(max(.025,1-(x/.87)**2-((z-4.035)/.79)**2))-off
    def fp(name,pts,mat,off=.02):
        return patch(name,[(x,fy(x,z,off),z) for x,z in pts],mat,'head')
    ell('Neck',(0,0,3.38),(.235,.22,.28),skin,'neck')
    for s,side in [(-1,'R'),(1,'L')]:
        ell('Ear.'+side,(s*.825,0,3.98),(.16,.125,.215),skin,'head')
        ell('Ear inner.'+side,(s*.852,-.113,3.98),(.083,.018,.126),earmat,'head',outlined=False)
        x=s*.30; z=4.105
        ell('Eye.'+side,(x,fy(x,z,.022),z),(.073,.038,.133),eye,'head',rot=(0,0,s*.18),outlined=False)
        ell('Eye glint.'+side,(x-.018,fy(x,z,.064),z+.052),(.020,.009,.030),white,'head',outlined=False,seg=20,rings=12)
        ell('Eye small glint.'+side,(x+.014,fy(x,z,.061),z-.045),(.007,.006,.010),white,'head',outlined=False,seg=12,rings=8)
        x=s*.535; z=3.88
        ell('Cheek.'+side,(x,fy(x,z,.018),z),(.117,.017,.077),blush,'head',rot=(0,0,s*.41),outlined=False)
        xs=[s*.18,s*.29,s*.40]; zs=[4.345,4.375,4.34]
        curve('Eyebrow.'+side,[(x,fy(x,z,.027),z) for x,z in zip(xs,zs)],.030 if A else .023,hair,'head')
        if not A:
            curve('Lash.'+side,[(s*.351,fy(s*.351,4.18,.055),4.18),(s*.391,fy(s*.391,4.20,.03),4.20)],.012,eye,'head')
    ell('Nose',(0,fy(0,3.94,.028),3.94),(.084,.071,.065),skin,'head',outlined=False)
    mouthpts=[(-.193,3.754),(-.15,3.773),(0,3.751),(.15,3.773),(.193,3.754),(.165,3.644),(.104,3.565),(0,3.538),(-.104,3.565),(-.165,3.644)]
    fp('Smile interior',mouthpts,mouth,.027)
    curve('Smile warm rim',[(x,fy(x,z,.032),z) for x,z in mouthpts],.018,ink,'head',cyclic=True)
    fp('Smile teeth',[(-.146,3.745),(0,3.727),(.146,3.745),(.133,3.697),(0,3.686),(-.133,3.697)],white,.034)
    fp('Smile tongue',[(-.099,3.576),(-.074,3.613),(0,3.625),(.074,3.613),(.099,3.576),(0,3.557)],tongue,.035)
    # Fitted scalp: high front hairline, low rear hairline, no whole sphere over the face.
    vs=[]; fs=[]; n=56; nr=16
    for k in range(nr+1):
        for j in range(n):
            a=j*2*pi/n; end=1.18+.59*(sin(a)+1); t=.005+(end-.005)*k/nr
            vs.append((.867*sin(t)*cos(a),.661*sin(t)*sin(a),4.06+.79*cos(t)))
    for k in range(nr):
        for j in range(n): fs.append((k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j))
    mesh('Hair cap • fitted hairline',vs,fs,hair,'head')
    if A:
        for i in range(3):
            ell('Hair fringe '+str(i),(-.36+i*.27,-.492,4.48+(i-1)*.035),(.26,.14,.14),hair,'head',rot=(0,.25,0))
    else:
        ell('Hair • swept fringe',(-.26,-.486,4.475),(.49,.152,.19),hair,'head',rot=(0,-.27,-.10))
        ell('Hair • right temple',(.624,-.28,4.27),(.14,.16,.34),hair,'head',rot=(0,-.12,0))
        ell('Hair • ponytail root',(0,.60,3.79),(.26,.23,.24),hair,'head')
        ell('Hair • ponytail main',(0,.845,3.57),(.32,.29,.38),hair,'head',rot=(-.43,.12,0))
        ell('Hair • ponytail curl',(.12,.89,3.35),(.20,.23,.23),hair,'head',rot=(0,.30,0))
        curve('Hair tie',[(-.20,.77,3.81),(0,.83,3.86),(.20,.77,3.81)],.036,teal_high,'head')
        curve('Ponytail sculpt line',[(-.11,1.10,3.73),(-.15,1.14,3.54),(-.05,1.12,3.36)],.009,hairlight,'head')
    for s,side in [(-1,'R'),(1,'L')]:
        curve('Helmet chin strap.'+side,[(s*.79,-.04,4.60),(s*.81,-.16,4.20),(s*.78,-.22,3.84),(s*.57,-.24,3.49),(s*.21,-.24,3.37)],.042,strap,'head',outlined=True)
        box('Chin strap fastener.'+side,(s*.74,-.22,3.72),(.095,.07,.12),.025,radio_mat,'head',rot=(0,s*.38,0))
    # Helmet shell and curved rolled brim.
    hm=orange if A else helmetB; mould=orange_dark if A else helmetBline
    def hp(t,a,offset=0): return ((.959+offset)*sin(t)*cos(a),(.807+offset)*sin(t)*sin(a),4.64+(.715+offset)*cos(t))
    vs=[]; fs=[]; n=72; nr=20
    for k in range(nr+1):
        t=.003+(pi/2-.003)*k/nr
        for j in range(n): vs.append(hp(t,j*2*pi/n))
    for k in range(nr):
        for j in range(n): fs.append((k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j))
    shell=mesh('Helmet • dome shell',vs,fs,hm,'head')
    mod=shell.modifiers.new('Helmet wall thickness','SOLIDIFY'); mod.thickness=.034
    rs=[(.88,0),(.98,.006),(1.04,-.005),(1.055,-.030),(1.04,-.061),(.88,-.045)]
    vs=[]; fs=[]
    for r,dz in rs:
        for j in range(n):
            a=j*2*pi/n; front=max(0,-sin(a))**6
            vs.append((r*cos(a),(.84*r+.13*front)*sin(a),4.64+.035*sin(a)+dz))
    for k in range(len(rs)):
        for j in range(n): fs.append((k*n+j,k*n+(j+1)%n,((k+1)%len(rs))*n+(j+1)%n,((k+1)%len(rs))*n+j))
    mesh('Helmet • rolled brim',vs,fs,hm,'head')
    curve('Helmet brim moulding',[(1.04*cos(j*2*pi/n),(.84*1.04+.13*max(0,-sin(j*2*pi/n))**6)*sin(j*2*pi/n),4.61+.035*sin(j*2*pi/n)) for j in range(n)],.010,mould,'head',cyclic=True)
    # Two longitudinal relief ribs; fitted reflective lateral strips on the firefighter helmet.
    for s in (-1,1):
        x=.32*s
        pts=[]
        for j in range(25):
            t=-pi/2+j*pi/24; cross=sqrt(1-(x/.959)**2)
            pts.append((x,(.807*cross+.009)*sin(t),4.64+(.715*cross+.009)*cos(t)))
        curve('Helmet raised rib '+str(s),pts,.017,mould,'head',outlined=False)
    if A:
        for s in (-1,1):
            vs=[]; fs=[]; na=32
            for t in (1.08,1.22):
                for j in range(na+1):
                    a=(-pi+.12+j*(pi-.24)/na) if s<0 else (.12+j*(pi-.24)/na)
                    vs.append(hp(t,a,.012))
            for j in range(na): fs.append((j,j+1,na+2+j,na+1+j))
            mesh('Helmet reflective side '+str(s),vs,fs,cream,'head',False)
        box('Helmet badge backing',(0,-.775,4.90),(.36,.075,.28),.10,orange_dark,'head')
        # Rounded wing-and-shield insignia.
        for s in (-1,1):
            patch('Helmet gold wing '+str(s),[(s*.07,-.827,4.94),(s*.33,-.79,4.94),(s*.29,-.801,4.86),(s*.15,-.825,4.81),(s*.07,-.84,4.83)],gold,'head')
            for dz in (0,.045): curve('Wing engraved '+str(s)+' '+str(dz),[(s*.14,-.85,4.87+dz),(s*.27,-.822,4.89+dz)],.009,helmetBline,'head')
        ell('Helmet shield',(0,-.858,4.90),(.13,.035,.16),gold,'head',outlined=True)
        patch('Helmet flame',[(-.057,-.902,4.86),(-.064,-.902,4.94),(0,-.902,5.018),(.018,-.902,4.95),(.060,-.902,4.93),(.063,-.902,4.86),(0,-.902,4.83)],yellow,'head')
    else:
        box('Helmet lift badge rim',(0,-.781,4.91),(.36,.065,.32),.08,helmetBline,'head')
        box('Helmet lift badge',(0,-.822,4.91),(.30,.022,.263),.06,paper,'head')
        for s in (-1,1):
            x=s*.065; up=s<0
            z1,z2=(4.83,4.985) if up else (4.985,4.83)
            curve('Helmet arrow shaft '+str(s),[(x,-.843,z1),(x,-.843,z2)],.017,ink,'head')
            z=z2+(-.047 if up else .047)
            curve('Helmet arrow head '+str(s),[(x-.038,-.843,z),(x,-.843,z2),(x+.038,-.843,z)],.017,ink,'head')
    # Padded jacket with quad ring topology. Narrow waist, broad rounded shoulders.
    torso_rs=[(2.035,0,0,.56,.32),(2.07,0,0,.65,.37),(2.17,0,0,.68,.39),(2.48,0,0,.70,.405),(2.84,0,0,.72,.41),(3.13,0,0,.76,.385),(3.31,0,0,.66,.33),(3.43,0,0,.38,.26),(3.44,0,0,.30,.24)]
    loft('Jacket • fitted padded body',torso_rs,cloth,'chest')
    loft('Jacket hem',[(2.04,0,0,.59,.345),(2.08,0,0,.66,.38),(2.17,0,0,.674,.389)],cuff,'chest',sub=0)
    # Upper yoke panel wraps the back and shoulders.
    vs=[]; fs=[]; n=56
    for level in range(4):
        for j in range(n):
            a=j*2*pi/n
            if level==0: z=3.02+.11*max(0,-sin(a)); rx=.755; ry=.405
            elif level==1: z=3.18; rx=.738; ry=.377
            elif level==2: z=3.34; rx=.61; ry=.325
            else: z=3.42; rx=.35; ry=.27
            vs.append((rx*cos(a),ry*sin(a),z))
    for k in range(3):
        for j in range(n): fs.append((k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j))
    mesh('Jacket • navy shoulder yoke',vs,fs,yoke,'chest',False)
    # Reflective bands fully wrap torso, including the back.
    for num,z in enumerate((2.78,2.91) if A else (2.88,)):
        loft('Chest reflective yellow '+str(num),[(z-.055,0,0,.737,.426),(z+.055,0,0,.744,.425)],yellow,'chest',n=56,sub=0)
        loft('Chest reflective ivory '+str(num),[(z-.021,0,0,.740,.430),(z+.021,0,0,.747,.429)],cream,'chest',n=56,sub=0)
    # Center zipper follows the jacket, two clean tailored front edges.
    curve('Jacket center seam',[(0,-.399,2.16),(0,-.427,2.65),(0,-.424,3.03),(0,-.35,3.30),(0,-.275,3.45)],.018,seam)
    box('Zipper pull',(.005,-.445,3.16),(.046,.035,.11),.012,steel)
    if A:
        loft('Standing rescue collar',[(3.37,0,0,.30,.255),(3.39,0,0,.345,.28),(3.60,0,0,.32,.255),(3.62,0,0,.28,.23)],orange_dark,'neck',n=40,sub=0)
        curve('Collar split',[(0,-.287,3.38),(0,-.27,3.59)],.014,orange_line,'neck')
        box('Chest name patch',(.34,-.382,3.12),(.22,.043,.075),.025,orange_dark)
    else:
        for s in (-1,1):
            patch('Folded collar '+str(s),[(s*.04,-.268,3.43),(s*.18,-.28,3.54),(s*.40,-.299,3.35),(s*.19,-.383,3.24)],teal_high,'neck',True)
        for s in (-1,1):
            box('Jacket lower pocket '+str(s),(s*.36,-.374,2.53),(.32,.052,.22),.045,teal_cuff)
            curve('Pocket topstitch '+str(s),[(s*.22,-.413,2.61),(s*.48,-.374,2.61)],.009,teal_high)
        box('Chest lift ID rim',(.34,-.392,3.11),(.245,.045,.235),.045,gold)
        box('Chest lift ID',(.34,-.420,3.11),(.204,.018,.198),.035,paper)
        for s in (-1,1):
            x=.34+s*.043; up=s<0; z1,z2=(3.055,3.173) if up else (3.173,3.055)
            curve('Chest lift arrow '+str(s),[(x,-.435,z1),(x,-.435,z2)],.012,ink)
            z=z2+(-.035 if up else .035)
            curve('Chest lift arrow head '+str(s),[(x-.027,-.435,z),(x,-.435,z2),(x+.027,-.435,z)],.012,ink)
    for s,side in [(-1,'R'),(1,'L')]:
        # One uninterrupted sleeve; local weights blend across the elbow.
        pts=[(s*.62,0,3.24),(s*.78,0,3.20),(s*.94,-.01,2.95),(s*1.06,-.035,2.67),(s*1.14,-.06,2.43),(s*1.18,-.07,2.29)]
        tube('Sleeve.'+side,pts,[.23,.285,.28,.255,.23,.20],cloth,'sleeve.'+side,n=32,sub=1)
        tube('Shoulder cap.'+side,pts[:3],[.24,.29,.282],yoke,'upper_arm.'+side,n=32,sub=0,outlined=False)
        # Bands perpendicular to the sleeve; no floating rings.
        p1=Vector((s*.913,-.008,2.991)); p2=Vector((s*.973,-.019,2.855))
        tube('Sleeve yellow band.'+side,[p1,p2],[.287,.279],yellow,'upper_arm.'+side,n=40,sub=0)
        tube('Sleeve ivory band.'+side,[p1.lerp(p2,.35),p1.lerp(p2,.65)],[.288,.286],cream,'upper_arm.'+side,n=40,sub=0,outlined=False)
        tube('Sleeve cuff.'+side,[(s*1.153,-.066,2.40),(s*1.19,-.074,2.26)],[.235,.204],cuff,'forearm.'+side,n=32,sub=0)
        curve('Cuff stitch.'+side,[(s*1.03,-.195,2.34),(s*1.17,-.275,2.33),(s*1.32,-.195,2.33)],.009,seam,'forearm.'+side)
        hand(s,side)
    # Belt and buckle, repeated loops, then sculpted cargo trousers and rounded work boots.
    loft('Utility belt',[(2.20,0,0,.678,.407),(2.38,0,0,.704,.424)],strap,'pelvis',n=56,sub=0)
    box('Belt buckle outer',(0,-.453,2.294),(.285,.073,.234),.043,steel,'pelvis')
    box('Belt buckle inset',(0,-.497,2.294),(.187,.028,.141),.022,strap,'pelvis')
    for x in (-.5,.5): box('Belt keeper '+str(x),(x,-.332,2.295),(.066,.071,.20),.018,orange_dark if A else teal_cuff,'pelvis',rot=(0,0,x*.55))
    loft('Trousers • pelvis',[(1.82,0,.025,.55,.29),(1.95,0,.025,.60,.32),(2.18,0,.025,.60,.33)],navy,'pelvis',n=40,sub=1)
    for s,side in [(-1,'R'),(1,'L')]:
        x=s*.344
        loft('Cargo trouser leg.'+side,[(.52,x,0,.245,.242),(.56,x,0,.276,.272),(.77,x,0,.286,.278),(1.05,x,.01,.29,.282),(1.42,x,.015,.298,.29),(1.80,x,.018,.31,.306),(1.99,x,.02,.29,.29)],navy,'leg.'+side,n=36,sub=1)
        box('Cargo side pocket.'+side,(s*.57,-.10,1.47),(.22,.33,.40),.06,yoke,'thigh.'+side,rot=(0,0,-s*.30))
        box('Cargo flap.'+side,(s*.582,-.125,1.65),(.232,.346,.12),.035,navy,'thigh.'+side,rot=(0,0,-s*.30))
        box('Knee padded panel.'+side,(x,-.258,1.02),(.35,.052,.29),.08,yoke,'shin.'+side)
        curve('Trouser fold.'+side,[(x-.19,-.202,.69),(x-.04,-.274,.65),(x+.17,-.225,.68)],.008,navyline,'shin.'+side)
        # Rounded boot upper, tall shaft, protective toe and a thick flat outsole.
        box('Boot outsole.'+side,(x,-.143,.095),(.59,.86,.18),.085,solemat,'foot.'+side)
        box('Boot upper.'+side,(x,-.15,.25),(.565,.815,.30),.13,rubber,'foot.'+side)
        loft('Boot ankle shaft.'+side,[(.24,x,.025,.25,.27),(.38,x,.025,.26,.267),(.58,x,.035,.235,.235),(.64,x,.035,.225,.226)],rubber,'foot.'+side,n=32,sub=1)
        ell('Boot toe cap.'+side,(x,-.37,.274),(.273,.315,.155),bootpanel,'foot.'+side)
        curve('Boot toe stitch.'+side,[(x-.22,-.30,.34),(x,-.382,.418),(x+.22,-.30,.34)],.011,solemat,'foot.'+side)
        box('Boot pull tab.'+side,(x,.238,.585),(.12,.09,.16),.025,bootpanel,'foot.'+side)
        for z in (.43,.50): curve('Boot instep seam.'+side+str(z),[(x-.12,-.222,z),(x,-.246,z+.012),(x+.12,-.222,z)],.010,bootpanel,'foot.'+side)
    # Shoulder radio with screen, speaker slats, side orange PTT and aerial.
    curve('Radio shoulder clip',[(-.51,-.26,3.35),(-.51,-.42,3.19),(-.52,-.45,3.04)],.052,strap)
    box('Radio casing',(-.50,-.445,3.18),(.233,.145,.375),.043,radio_edge,rot=(0,-.10,-.12))
    box('Radio front',(-.50,-.531,3.18),(.193,.038,.325),.028,radio_mat,rot=(0,-.10,-.12))
    box('Radio LCD',(-.493,-.559,3.263),(.12,.012,.069),.008,screen,outlined=False)
    for z in (3.11,3.155,3.20): box('Radio grille '+str(z),(-.50,-.559,z),(.126,.015,.016),.006,radio_edge,outlined=False)
    box('Radio PTT',(-.626,-.461,3.20),(.023,.06,.09),.009,orange)
    curve('Radio aerial',[(-.57,-.43,3.355),(-.615,-.424,3.66)],.022,radio_edge,outlined=True)
    if not A:
        curve('Radio coiled lead',[(-.57,-.46,2.37),(-.79,-.39,2.65),(-.78,-.42,2.92),(-.64,-.48,3.12)],.024,strap)
    # Belt flashlight, lens faces front-down for an immediately readable yellow disc.
    tube('Flashlight barrel',[(-.49,-.51,1.98),(-.46,-.56,2.30)],[.10,.11],radio_mat,'pelvis',n=32,sub=0)
    disk('Flashlight yellow rim',(-.46,-.599,2.23),.14,.105,gold)
    disk('Flashlight front bezel',(-.46,-.665,2.23),.119,.042,radio_edge)
    disk('Flashlight lens',(-.46,-.690,2.23),.093,.022,cream)
    ell('Flashlight lens glint',(-.485,-.710,2.26),(.047,.012,.025),white,'pelvis',outlined=False,seg=20,rings=12)
    box('Flashlight belt loop',(-.46,-.398,2.31),(.18,.10,.22),.03,strap,'pelvis')
    # Hanging gloves, including individual fingers and cuffs, on the opposite hip.
    for off in (0,.11):
        gx=.67+off
        box('Glove cuff '+str(off),(gx,-.08,2.28),(.19,.16,.17),.035,gloveline,'pelvis',rot=(0,-.1,0))
        ell('Glove palm '+str(off),(gx+.025,-.11,2.09),(.12,.09,.19),glovemat,'pelvis',rot=(0,-.1,0))
        for i in range(3): ell('Glove finger '+str(off)+' '+str(i),(gx-.05+i*.07,-.13,1.95+abs(i-1)*.025),(.046,.075,.13),glovemat,'pelvis',outlined=True,seg=20,rings=12)
        ell('Glove thumb '+str(off),(gx+.14,-.12,2.08),(.07,.075,.125),glovemat,'pelvis',rot=(0,-.4,0),seg=20,rings=12)
    if not A:
        box('Tool pouch body',(.64,-.292,2.075),(.32,.25,.41),.065,navy,'pelvis',rot=(0,-.04,-.2))
        box('Tool pouch folded flap',(.64,-.438,2.22),(.34,.075,.20),.05,yoke,'pelvis',rot=(0,-.04,-.2))
        box('Tool pouch brass latch',(.64,-.488,2.16),(.12,.034,.07),.012,gold,'pelvis')
    else:
        # Clear, correct Korean back label is actual editable geometry, not a texture.
        font=bpy.data.fonts.load(r'C:\Windows\Fonts\malgunbd.ttf')
        cu=bpy.data.curves.new('구조 back label','FONT'); cu.body='구조'; cu.font=font; cu.align_x='CENTER'; cu.size=.315; cu.extrude=.002
        ob=bpy.data.objects.new('구조',cu); CURRENT.objects.link(ob); ob.location=(0,.413,3.105); ob.rotation_euler=(pi/2,0,pi)
        finish(ob,'Back label • 구조',cream,'chest',False)
    return {'id':which,'collection':CURRENT,'parts':PARTS.copy(),'head':head}

def bake_and_rig(c):
    parts=c['parts']; which=c['id']
    bpy.ops.object.select_all(action='DESELECT')
    for ob in parts:
        bpy.context.view_layer.objects.active=ob; ob.select_set(True)
        if ob.type in {'CURVE','FONT'}: bpy.ops.object.convert(target='MESH')
        for mod in list(ob.modifiers):
            if mod.type in {'BEVEL','WEIGHTED_NORMAL','SOLIDIFY'}: bpy.ops.object.modifier_apply(modifier=mod.name)
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        for v in ob.data.vertices: v.co*=S
        ob.select_set(False)
    arm=bpy.data.armatures.new(which+' · Rescue FK')
    rig=bpy.data.objects.new(which+' | RIG • Pose Mode',arm); rigs.objects.link(rig)
    bpy.context.view_layer.objects.active=rig; rig.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
    def bn(name,h,t,parent=None):
        b=arm.edit_bones.new(name); b.head=Vector(h)*S; b.tail=Vector(t)*S
        if parent: b.parent=arm.edit_bones[parent]
    bn('root',(0,0,0),(0,0,.35))
    bn('pelvis',(0,0,1.91),(0,0,2.35),'root')
    bn('spine',(0,0,2.35),(0,0,2.78),'pelvis')
    bn('chest',(0,0,2.78),(0,0,3.32),'spine')
    bn('neck',(0,0,3.32),(0,0,3.55),'chest')
    bn('head',(0,0,3.55),(0,0,4.75),'neck')
    for s,side in [(-1,'R'),(1,'L')]:
        bn('upper_arm.'+side,(s*.65,0,3.21),(s*1.035,-.03,2.71),'chest')
        bn('forearm.'+side,(s*1.035,-.03,2.71),(s*1.19,-.074,2.24),'upper_arm.'+side)
        bn('hand.'+side,(s*1.19,-.074,2.24),(s*1.21,-.08,1.84),'forearm.'+side)
        bn('thigh.'+side,(s*.344,0,1.98),(s*.344,0,1.14),'pelvis')
        bn('shin.'+side,(s*.344,0,1.14),(s*.344,.025,.48),'thigh.'+side)
        bn('foot.'+side,(s*.344,.025,.48),(s*.344,-.48,.16),'shin.'+side)
    bpy.ops.object.mode_set(mode='OBJECT')
    rig.show_in_front=True; arm.display_type='STICK'
    rig['Purpose']='Basic FK posing only; modular mesh. No IK, finger rig, facial rig or animation clips.'
    for ob in parts:
        bind=ob['BindBone']
        if bind.startswith('sleeve.') or bind.startswith('leg.'):
            side=bind.split('.')[-1]; sleeve=bind.startswith('sleeve.')
            names=('upper_arm.'+side,'forearm.'+side) if sleeve else ('thigh.'+side,'shin.'+side)
            groups=[ob.vertex_groups.new(name=n) for n in names]
            center,width=(2.71,.32) if sleeve else (1.14,.30)
            for v in ob.data.vertices:
                w=max(0,min(1,(v.co.z/S-center)/width+.5))
                if w: groups[0].add([v.index],w,'REPLACE')
                if w<1: groups[1].add([v.index],1-w,'REPLACE')
        else:
            vg=ob.vertex_groups.new(name=bind); vg.add(list(range(len(ob.data.vertices))),1,'REPLACE')
        mod=ob.modifiers.new('FK skin • simple weights','ARMATURE'); mod.object=rig
        while list(ob.modifiers).index(mod)>0:
            bpy.context.view_layer.objects.active=ob; bpy.ops.object.modifier_move_up(modifier=mod.name)
        ob.parent=rig
    rig.select_set(False)
    c['rig']=rig
    return c

CA=bake_and_rig(make_character('A'))
CB=bake_and_rig(make_character('B'))
CA['rig'].location.x=-.59
CB['rig'].location.x=.59

# Freestyle renders warm brown silhouettes, independent of exportable model meshes.
freestyle=bpy.context.view_layer.freestyle_settings
ls=freestyle.linesets[0] if freestyle.linesets else freestyle.linesets.new('Warm brown silhouettes')
if ls.linestyle is None: ls.linestyle=bpy.data.linestyles.new('Warm brown ink')
ls.linestyle.color=rgba('#493327')[:3]; ls.linestyle.thickness=2.15
ls.select_silhouette=True; ls.select_border=True; ls.select_contour=True; ls.select_crease=False
ls.select_by_collection=True; ls.collection=outline; ls.collection_negation='INCLUSIVE'

world=bpy.data.worlds.new('Paper studio'); world.use_nodes=True
world.node_tree.nodes['Background'].inputs['Color'].default_value=rgba('#FFF6DB')
world.node_tree.nodes['Background'].inputs['Strength'].default_value=1
scene.world=world
def studio_ell(name,loc,scale,h):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=64,ring_count=12,location=loc)
    ob=bpy.context.object; ob.name=name; ob.scale=scale; link(ob,studio); ob.data.materials.append(toon(name,h,True))
    return ob
shadows=[]
for c in (CA,CB):
    shadows.append(studio_ell('Studio contact shadow '+c['id'],(c['rig'].location.x,0,-.001),(.36,.25,.001),'#E7D7BC'))
def area(name,loc,power,size):
    ld=bpy.data.lights.new(name,'AREA'); ld.energy=power; ld.shape='DISK'; ld.size=size
    ob=bpy.data.objects.new(name,ld); studio.objects.link(ob); ob.location=loc
    ob.rotation_euler=(Vector((0,0,.85))-ob.location).to_track_quat('-Z','Y').to_euler()
area('Key • warm daylight',(-3,-4,6),450,4)
area('Fill',(4,-1,3),100,4)
def camera(name,loc,target,scale):
    cd=bpy.data.cameras.new(name); cd.type='ORTHO'; cd.ortho_scale=scale; cd.clip_start=.01
    ob=bpy.data.objects.new(name,cd); studio.objects.link(ob); ob.location=loc
    ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
    return ob
cams={
 'Pair':camera('CAM_00 • 두 구조요원',(2.6,-10,3.0),(0,0,.86),2.80),
 'Front':camera('CAM_01 • 정면',(0,-10,.88),(0,0,.88),1.96),
 'Side':camera('CAM_02 • 측면',(10,0,.88),(0,0,.88),1.96),
 'Back':camera('CAM_03 • 후면',(0,10,.88),(0,0,.88),1.96),
 'Hero':camera('CAM_04 • 앞 사선',(4.6,-10,3.1),(0,0,.88),2.00),
 'BackHero':camera('CAM_05 • 뒤 사선',(4.6,10,3.0),(0,0,.88),2.00),
}
for which,refpath in [('A',r'E:\ProjectWait\ConceptArt\Wait! 구조요원 A안-1.png'),('B',r'E:\ProjectWait\ConceptArt\Wait! 구조요원 B안-2.png')]:
    if Path(refpath).exists():
        img=bpy.data.images.load(refpath); img.pack()
        ob=bpy.data.objects.new(which+' | 원본 레퍼런스',None); refs.objects.link(ob)
        ob.empty_display_type='IMAGE'; ob.data=img; ob.empty_display_size=2
        ob.location=(3 if which=='A' else 5,1,1); ob.rotation_euler=(pi/2,0,0)

README='''Wait! 구조요원 A · B — Blender 5.1.1

A: 소방 구조대원 — 주황 방화복, 소방 헬멧과 날개 방패, 구조 등판,
반사띠, 어깨 무전기, 벨트 손전등과 장갑, 카고 바지, 작업화.
B: 승강기 전문 구조요원 — 청록 작업복, 노란 안전모와 승강기 화살표,
묶은 머리, 무전기, 손전등, 장갑, 공구 파우치, 카고 바지와 작업화.

Wait_Rescue_AB.blend: 두 캐릭터 비교용 파일.
Wait_Rescuer_A_Firefighter.blend / Wait_Rescuer_B_LiftSpecialist.blend:
각 캐릭터를 원점에 배치한 개별 편집 파일.

01_CHARACTERS: 이름이 붙은 부위별 메시. 02_RIGS: 기본 FK 뼈대.
03_STUDIO: 정면/측면/후면/앞뒤 사선 카메라, 조명, 접지 그림자.
04_REFERENCES: 사용자가 제공한 컨셉 원본 이미지를 파일 내부에 포함(숨김).
00_INK: 선을 그릴 형태만 참조하는 추가 컬렉션. 메시가 복제된 것이 아닙니다.
Blender Z 위, -Y 정면, 미터 단위, 발바닥 z=0, 높이 약 1.72m.
F12로 셀 명암과 갈색 외곽선을 렌더할 수 있습니다.

프로젝트 기본색: 주황 #FF8C2D, 소매 #E87927, 피부 #FFC08A,
바지 #3B3E48, 외곽선 #493327. 무광 색면과 3단계 셀 명암.
Blender 미리보기는 노멀 기반 셀 재질, 렌더 외곽선은 Freestyle입니다.
FBX는 변환용 기본색 PBR 재질과 기본 뼈대를 포함합니다.
FBX에서 Blender 전용 셀 노드/Freestyle은 재현되지 않으므로 Unity에서는
프로젝트의 URP/XR 셀 재질과 외곽선 셰이더를 할당해야 합니다.
Unity 씬 반영 및 플레이 검증은 이번 Blender 모델링 범위에 포함하지 않았습니다.

FK는 간단한 포즈 조정용입니다. 얼굴/손가락/IK 컨트롤, 애니메이션 클립,
전신 리토폴로지와 극단적인 관절 변형 검수는 포함하지 않았습니다.
모듈형 겹침 메시이며 3D 프린팅용 단일 수밀 메시가 아닙니다.
장식은 색상 재질과 실제 메시로 구성되어 별도 이미지 텍스처가 필요 없습니다.
원본 이미지는 참고용으로만 팩되어 있습니다.
'''
bpy.data.texts.new('읽어주세요 • README').write(README)
(OUT/'README_구조요원.txt').write_text(README,encoding='utf-8-sig')
scene['Art_direction']='Warm storybook 3D / 3-band cel / brown ink / project palette'
scene['Rig_scope']='Basic FK; no IK, face rig, finger rig or animation clips'
scene['Reference']='User-supplied firefighter A and lift specialist B turnaround images'
scene['Model_height_m']=round(5.36*S,3)
scene.camera=cams['Pair']; scene.render.filepath='//Preview_00_AB.png'
bpy.ops.object.select_all(action='DESELECT')
CA['head'].select_set(True); bpy.context.view_layer.objects.active=CA['head']
for sc in bpy.data.screens:
    for ar in sc.areas:
        if ar.type=='VIEW_3D':
            sp=ar.spaces.active; sp.region_3d.view_distance=3.5; sp.region_3d.view_location=(0,0,.85)
            sp.region_3d.view_rotation=cams['Pair'].rotation_euler.to_quaternion()
            sp.shading.type='MATERIAL'; sp.overlay.show_extras=False; sp.overlay.show_floor=False
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Wait_Rescue_AB.blend'))
summary={'characters':[],'references_packed':len([im for im in bpy.data.images if im.packed_file])}
for c in (CA,CB):
    summary['characters'].append({'id':c['id'],'meshes':len(c['parts']),'vertices':sum(len(o.data.vertices) for o in c['parts']),'polygons':sum(len(o.data.polygons) for o in c['parts']),'bones':len(c['rig'].data.bones)})
(WORK/'Build_Validation.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print('BUILD_COMPLETE '+json.dumps(summary),flush=True)
scene.render.filepath=str(WORK/'Draft_AB.png')
bpy.ops.render.render(write_still=True)
print('DRAFT_RENDER_COMPLETE',flush=True)
