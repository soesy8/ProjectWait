"""Rebuild the saved elevator scene as a self-contained, editable Blender file."""
import bpy
import json
import math
from pathlib import Path
from mathutils import Matrix, Quaternion, Vector

WORK = Path(__file__).resolve().parent
PROJECT = WORK.parents[3]
SOURCE = WORK / 'Elevator_Map_Source.json'
OUTPUT = WORK / '엘레베이터_맵.blend'
data = json.loads(SOURCE.read_text(encoding='utf-8'))
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.preferences.filepaths.save_version = 0
scene = bpy.context.scene
scene.name = '엘레베이터 맵'
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0
scene.render.engine = 'BLENDER_EEVEE'
scene.view_settings.view_transform = 'Standard'
scene.view_settings.look = 'None'
scene.render.resolution_x = 1280
scene.render.resolution_y = 800
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
scene.render.engine = 'BLENDER_EEVEE'
scene.render.use_file_extension = True
scene.render.image_settings.color_mode = 'RGBA'
scene['Unity_Source_Scene'] = data['sourceScene']
scene['Unity_Source_Version'] = data['unityVersion']
scene['Export_Note'] = 'Exact saved map geometry, transforms, UVs, normals, palette colors. Unity shaders approximated in Blender. Characters are static posed meshes.'

def collection(name):
    col = bpy.data.collections.new(name)
    scene.collection.children.link(col)
    return col

cols = {key: collection(name) for key, name in [
    ('cabin', '01_엘리베이터 내부'), ('lobby', '02_로비'),
    ('characters', '03_캐릭터와 소품'), ('hidden', '04_비활성 소품'),
    ('helpers', '05_원본 계층'), ('cameras', '06_카메라와 조명')]}
# Unity is left handed; exchanging Y/Z also corrects handedness for readable text.
# A pure rotation would mirror every sign when viewed through a Blender camera.
CONV = Matrix(((1,0,0,0),(0,0,1,0),(0,1,0,0),(0,0,0,1)))
INV = CONV.inverted()
D = CONV.to_3x3()
sun_direction = (D @ Vector(data['sun']['direction'])).normalized()

def srgb(v):
    return v / 12.92 if v <= 0.04045 else ((v + .055) / 1.055) ** 2.4

def lin(c):
    return tuple(srgb(float(v)) for v in c[:3]) + (float(c[3]) if len(c)>3 else 1.,)

material_cache = {}
def toon(name, color, shade=(.51,.57,.55), mid=.9, shadow=.02, light=.62, softness=.012, emission=0., ink=(.067,.033,.019,1.), width=0.):
    key = tuple(round(float(x),6) for x in (*color,*shade,mid,shadow,light,softness,emission,*ink,width))
    if key in material_cache:
        return material_cache[key]
    mat = bpy.data.materials.new(name)
    material_cache[key] = mat
    mat.diffuse_color = tuple(color[:3])+(1.,)
    mat.use_nodes = True
    mat['Unity_Outline_Width_Pixels'] = width
    mat['Unity_Outline_Color_Linear'] = list(ink)
    mat['Unity_Toon_Shadow_Threshold'] = shadow
    mat['Unity_Toon_Light_Threshold'] = light
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()
    out=nodes.new('ShaderNodeOutputMaterial');out.location=(600,0)
    emit=nodes.new('ShaderNodeEmission');emit.location=(390,0)
    links.new(emit.outputs[0],out.inputs['Surface'])
    geo=nodes.new('ShaderNodeNewGeometry');geo.location=(-650,0)
    dot=nodes.new('ShaderNodeVectorMath');dot.operation='DOT_PRODUCT';dot.location=(-450,0)
    dot.inputs[1].default_value=sun_direction
    links.new(geo.outputs['Normal'],dot.inputs[0])
    scale=nodes.new('ShaderNodeMath');scale.operation='MULTIPLY_ADD';scale.location=(-250,0)
    scale.inputs[1].default_value=.5;scale.inputs[2].default_value=.5
    links.new(dot.outputs['Value'],scale.inputs[0])
    ramp=nodes.new('ShaderNodeValToRGB');ramp.location=(-40,0);ramp.label='원본 3단계 셀 명암'
    links.new(scale.outputs[0],ramp.inputs[0]);links.new(ramp.outputs['Color'],emit.inputs['Color'])
    low=tuple(color[i]*(shade[i]*(1-emission)+emission) for i in range(3))+(1.,)
    middle=tuple(color[i]*(mid*(1-emission)+emission) for i in range(3))+(1.,)
    high=tuple(color[:3])+(1.,)
    a=(shadow+1)*.5;b=(light+1)*.5;e=max(.0001,softness*.5)
    stops=[(0.,low),(max(0,a-e),low),(min(1,a+e),middle),(max(0,b-e),middle),(min(1,b+e),high),(1.,high)]
    ramp.color_ramp.interpolation='LINEAR'
    ramp.color_ramp.elements.remove(ramp.color_ramp.elements[1])
    for i,(pos,col) in enumerate(stops):
        element=ramp.color_ramp.elements[0] if i==0 else ramp.color_ramp.elements.new(pos)
        element.position=pos;element.color=col
    return mat

records = {m['id']:m for m in data['materials']}
regular_materials = {}
images = {}
for rec in data['materials']:
    if 'TextMeshPro' in rec['shader']:
        mat=bpy.data.materials.new('안내판 글자_원본 아틀라스')
        mat.use_nodes=True;mat.diffuse_color=lin((.28,.19,.14,1))
        n,l=mat.node_tree.nodes,mat.node_tree.links;n.clear()
        output=n.new('ShaderNodeOutputMaterial');output.location=(620,0)
        mix=n.new('ShaderNodeMixShader');mix.location=(400,0)
        transparent=n.new('ShaderNodeBsdfTransparent');transparent.location=(180,-110)
        emit=n.new('ShaderNodeEmission');emit.location=(180,80)
        vertex=n.new('ShaderNodeVertexColor');vertex.layer_name='Unity_Color';vertex.location=(-60,200)
        l.new(vertex.outputs['Color'],emit.inputs['Color'])
        image=n.new('ShaderNodeTexImage');image.location=(-650,0)
        texpath=PROJECT / rec['texture']
        image.image=bpy.data.images.load(str(texpath),check_existing=True)
        image.image.colorspace_settings.name='Non-Color';image.image.pack()
        images[str(texpath)]=image.image
        ramp=n.new('ShaderNodeMapRange');ramp.location=(-280,0);ramp.clamp=True
        ramp.inputs['From Min'].default_value=.47;ramp.inputs['From Max'].default_value=.53
        l.new(image.outputs['Alpha'],ramp.inputs['Value'])
        l.new(ramp.outputs['Result'],mix.inputs[0]);l.new(transparent.outputs[0],mix.inputs[1]);l.new(emit.outputs[0],mix.inputs[2])
        l.new(mix.outputs[0],output.inputs['Surface'])
        mat.surface_render_method='DITHERED'
        mat.use_transparency_overlap=False
    elif 'Vertex Palette' in rec['shader']:
        continue
    else:
        c=rec['colors'];f=rec['floats'];base=lin(c.get('_BaseColor',c.get('_Color',[1,1,1,1])))
        shade=lin(c.get('_ShadeColor',[.741,.784,.765,1]))
        mat=toon(rec['name'],base,shade[:3],f.get('_MidTone',.9),f.get('_ShadowThreshold',.02),f.get('_LightThreshold',.62),f.get('_BandSoftness',.012),f.get('_EmissionStrength',0),lin(c.get('_OutlineColor',[.286,.2,.153,1])),f.get('_OutlineWidth',0))
    mat['Unity_Material_Path']=rec['path'];mat['Unity_Shader']=rec['shader']
    regular_materials[rec['id']]=mat

objects={}
counts={'meshes':0,'vertices':0,'triangles':0,'text_meshes':0}
for part in data['parts']:
    g=part['geometry']
    if g:
        verts=[D@Vector(v) for v in g['vertices']]
        normals=[D@Vector(v) for v in g['normals']]
        faces=[];source_materials=[]
        for subindex,indices in enumerate(g['submeshes']):
            for i in range(0,len(indices),3):
                face=indices[i:i+3]
                # TMP allocates trailing empty glyph quads; preserve their source vertices.
                if normals:
                    a,b,c=(verts[j] for j in face)
                    normal=sum((normals[j] for j in face),Vector())
                    if (b-a).cross(c-a).dot(normal)<0: face=list(reversed(face))
                faces.append(face);source_materials.append(subindex)
        mesh=bpy.data.meshes.new(part['name']+'_Mesh')
        mesh.from_pydata(verts,[],faces);mesh.update()
        if len(normals)==len(verts):
            for poly in mesh.polygons:poly.use_smooth=True
            mesh.normals_split_custom_set_from_vertices(normals)
        uv=g['uv'].get('0')
        if uv:
            layer=mesh.uv_layers.new(name='Unity_UV0')
            layer.data.foreach_set('uv',[v for loop in mesh.loops for v in uv[loop.vertex_index][:2]])
        colors=g['colors']
        if len(colors)==len(verts):
            attr=mesh.color_attributes.new(name='Unity_Color',type='FLOAT_COLOR',domain='POINT')
            # Palette colors were explicitly stored in linear space; TMP Color32 is sRGB.
            palette=any('Vertex Palette' in records[m]['shader'] for m in part['materials'] if m in records)
            attr.data.foreach_set('color',[v for c in colors for v in (c if palette else lin(c))])
        else:palette=False
        for channel,name in [('1','Unity_Style'),('2','Unity_Shade'),('3','Unity_Ink'),('4','Unity_Bands')]:
            values=g['uv'].get(channel)
            if values:
                attr=mesh.attributes.new(name=name,type='FLOAT_COLOR',domain='POINT')
                attr.data.foreach_set('color',[v for value in values for v in value])
        slots={}
        for poly,source_slot in zip(mesh.polygons,source_materials):
            material_id=part['materials'][min(source_slot,len(part['materials'])-1)]
            rec=records[material_id]
            if 'Vertex Palette' in rec['shader']:
                v=poly.vertices[0];style=g['uv']['1'][v];shade=g['uv']['2'][v];ink=g['uv']['3'][v];bands=g['uv']['4'][v]
                mat=toon('팔레트_%02d'%len(material_cache),colors[v],shade[:3],shade[3],bands[0],bands[1],style[3],style[1],ink,style[0])
            else:mat=regular_materials[material_id]
            if mat.name not in slots:slots[mat.name]=len(mesh.materials);mesh.materials.append(mat)
            poly.material_index=slots[mat.name]
        obj=bpy.data.objects.new(part['name'],mesh)
        obj['Unity_Mesh_Path']=g['path'];obj['Unity_Mesh_Name']=g['name']
        counts['meshes']+=1;counts['vertices']+=len(verts);counts['triangles']+=len(faces)
        if part['text']:
            obj['안내판 원문']=part['text']['content'];counts['text_meshes']+=1
    else:
        obj=bpy.data.objects.new(part['name'],None);obj.empty_display_size=.045
    path=part['path']
    key='hidden' if not part['visible'] else 'cabin' if '/Cabin' in path else 'lobby' if '/BasketballLobby' in path else 'characters' if ('Friend' in path or 'Basketball' in path) else 'helpers'
    cols[key].objects.link(obj)
    obj['Unity_Path']=path;obj['Unity_Source_ID']=part['id'];obj['Unity_Visible']=part['visible']
    if part['note']:obj['Export_Note']=part['note']
    if part['parent']:obj.parent=objects[part['parent']]
    q=part['localRotation']
    obj.matrix_basis=CONV@Matrix.LocRotScale(Vector(part['localPosition']),Quaternion((q[3],q[0],q[1],q[2])),Vector(part['localScale']))@INV
    obj.hide_render=not part['visible'];obj.hide_set(not part['visible'])
    objects[part['id']]=obj

def camera(name,position,target=None,rotation=None,fov=68):
    c=bpy.data.cameras.new(name);obj=bpy.data.objects.new(name,c);cols['cameras'].objects.link(obj)
    obj.location=D@Vector(position)
    if target is not None:obj.rotation_euler=((D@Vector(target))-obj.location).to_track_quat('-Z','Y').to_euler()
    elif rotation is not None:
        q=Quaternion((rotation[3],*rotation[:3]));forward=D@(q@Vector((0,0,1)));up=D@(q@Vector((0,1,0)))
        right=forward.cross(up).normalized();correct_up=right.cross(forward).normalized()
        obj.rotation_euler=Matrix((right,correct_up,-forward)).transposed().to_euler()
    c.lens_unit='FOV';c.sensor_fit='VERTICAL';c.angle=math.radians(fov);c.clip_start=.025;c.clip_end=100
    return obj

lobby_cam=camera('Camera_로비_원본',data['camera']['position'],rotation=data['camera']['rotation'])
interior_cam=camera('Camera_내부',(0,1.55,-1.95),(0,1.46,2.8),fov=72)
overview_cam=camera('Camera_전체',(8,7,12),(0,1.1,2.1),fov=58)
scene.camera=lobby_cam
world=bpy.data.worlds.new('따뜻한 크림 배경');world.use_nodes=True
world.node_tree.nodes['Background'].inputs['Color'].default_value=lin((1,.965,.86,1))
world.node_tree.nodes['Background'].inputs['Strength'].default_value=.7
scene.world=world
light=bpy.data.lights.new('Warm cabin light','SUN');light.energy=1.6
light_obj=bpy.data.objects.new('Warm cabin light',light);cols['cameras'].objects.link(light_obj)
light_obj.rotation_euler=(-sun_direction).to_track_quat('-Z','Y').to_euler()
scene.render.use_freestyle=True
fs=bpy.context.view_layer.freestyle_settings
line=fs.linesets[0] if fs.linesets else fs.linesets.new('Warm brown outlines')
if line.linestyle is None:line.linestyle=bpy.data.linestyles.new('Warm brown ink')
line.linestyle.color=lin((.2862745,.2,.152941,1))[:3]
line.linestyle.thickness=1.15
line.select_silhouette=True;line.select_border=True;line.select_contour=True;line.select_crease=False
line.select_by_collection=True
outline_collection=collection('07_외곽선 대상')
for obj in list(objects.values()):
    if obj.type=='MESH' and '안내판 원문' not in obj: outline_collection.objects.link(obj)
line.collection=outline_collection
line.collection_negation='INCLUSIVE'

readme='''엘리베이터 맵 — Unity 현재 저장 씬에서 추출

원본: Assets/M/Scene/Elevator_Safety_Prototype.unity
Blender 5.1.1 / 미터 단위 / Z축 위

01: 엘리베이터 내부, 문, 버튼, 한글 안내판.
02: 로비 구조물과 표지판.
03: 현재 배치된 친구와 농구공. 캐릭터는 현재 자세의 정적 메시.
04: 원본에서 비활성인 구조대원, 추가 소품. 기본 숨김.
06: 원본 로비, 내부, 전체 보기 카메라.

모든 메시, 부모 계층, 로컬 변환, UV0, 커스텀 노멀, 정점 색상과 셀 명암 데이터를 보존.
Unity에서 합쳐진 CabinArchitecture / LobbyArchitecture는 그대로 보존했습니다.
개별 구조물은 편집 모드에서 L(연결 선택), P > By Loose Parts로 분리 가능합니다.
문은 LeftDoor와 RightDoor 별도 메시로 유지했습니다.
팔레트는 편집하기 쉬운 Blender 재질로도 변환했고 원래 정점 데이터도 유지했습니다.
한글 안내판은 원본 글리프 메시와 내장 SDF 아틀라스로 보존했습니다.
문자 원문은 오브젝트의 '안내판 원문' 커스텀 속성에서 확인할 수 있습니다.

3단계 셀 명암과 갈색 Freestyle 외곽선은 Blender 미리보기용 변환입니다.
Unity의 URP 셰이더, 그림자, 런타임 UI, 게임 스크립트, 상호작용,
XR 설정, 캐릭터 리깅/애니메이션은 Blender 기능으로 변환하지 않았습니다.
모든 사용 텍스처를 blend 안에 포함해 외부 텍스처 파일이 필요 없습니다.
Unity 원본 씬과 원본 자산은 저장하거나 수정하지 않았습니다.
'''
bpy.data.texts.new('읽어주세요.txt').write(readme)
(WORK/'읽어주세요.txt').write_text(readme,encoding='utf-8')
bpy.context.view_layer.update()
for obj in bpy.context.selected_objects:obj.select_set(False)
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            space=area.spaces.active;space.clip_end=200
            space.shading.type='MATERIAL';space.shading.use_scene_world=True;space.shading.use_scene_lights=True
            space.overlay.show_extras=False
            space.region_3d.view_perspective='CAMERA'
bpy.ops.file.pack_all()
assert counts['meshes']==data['meshes'],counts
assert counts['vertices']==data['vertices'],counts
assert counts['triangles']==data['triangles'],counts
bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT),compress=True)
validation={'blend':str(OUTPUT),'blenderVersion':bpy.app.version_string,**counts,'objects':len(objects),'materials':len(bpy.data.materials),'packedImages':len(images),'warnings':data['warnings']}
(WORK/'Build_Validation.json').write_text(json.dumps(validation,ensure_ascii=False,indent=2),encoding='utf-8')
print('EXPORT_VALIDATION '+json.dumps(validation,ensure_ascii=False),flush=True)
for cam,filename in [(lobby_cam,'Preview_로비.png'),(interior_cam,'Preview_내부.png')]:
    scene.camera=cam;scene.render.filepath=str(WORK/filename)
    bpy.ops.render.render(write_still=True)
