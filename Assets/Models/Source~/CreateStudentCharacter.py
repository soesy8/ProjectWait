import bpy  # 설치된 Blender의 메시 편집과 FBX 내보내기를 사용합니다.
import bmesh  # 직접 만든 표면의 법선을 정리합니다.
import math  # 단면과 얼굴 곡선의 위치를 계산합니다.
import struct  # Unity의 정점과 스킨 버퍼를 작성합니다.
import uuid  # 새 에셋의 식별자를 만듭니다.
from pathlib import Path  # 출력은 개인 작업 영역 안으로 제한합니다.
from mathutils import Vector, Matrix  # 관절의 좌표 변환과 대기 자세를 구성합니다.

AREA = Path(__file__).resolve().parents[2]  # 제작 코드에서 Assets를 찾아 학생 모델과 재질을 이동한 경로에 저장합니다.
MODELS = AREA / 'Models' / 'StudentCharacter'  # 학생 모델의 Unity 메시와 FBX를 같은 출력 폴더에 저장합니다.
SOURCE = Path(__file__).resolve().parent  # 편집 가능한 Blender 원본은 제작 코드와 같은 Source~에 저장합니다.
HEADER = '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'  # Unity 에셋의 표준 헤더입니다.
C = Matrix(((1, 0, 0, 0), (0, 0, -1, 0), (0, 1, 0, 0), (0, 0, 0, 1)))  # Y축 높이 설계를 Blender Z축 높이로 바꿉니다.
PARTS = []  # 완성된 부위만 내보내기 목록에 보관합니다.
COLORS = {  # 피부와 머리카락, 의상과 운동화를 구분합니다.
    'StudentSkin': (.82, .59, .43),  # 따뜻한 피부색입니다.
    'StudentHair': (.11, .055, .028),  # 짙은 갈색 머리입니다.
    'StudentHairHighlight': (.19, .095, .05),  # 큰 머리카락 결에만 사용할 밝은 갈색입니다.
    'StudentShirt': (.18, .55, .72),  # 캐주얼 상의의 청록색입니다.
    'StudentPants': (.075, .12, .22),  # 바지의 남색입니다.
    'StudentStitch': (.19, .28, .39),  # 주머니와 바지 단의 약한 대비입니다.
    'StudentShoeAccent': (.94, .43, .26),  # 운동화의 주황색 포인트입니다.
    'StudentSole': (.9, .91, .88),  # 밝은 고무 밑창입니다.
    'StudentIris': (.28, .12, .05),  # 갈색 홍채입니다.
    'StudentLip': (.60, .25, .22),  # 자연스러운 입술색입니다.
    'FriendEyeWhite': (.99, .98, .93),  # 기존 흰자 재질을 재사용합니다.
    'FriendFaceDark': (.055, .08, .13),  # 기존 동공 재질을 재사용합니다.
}  # 외부 텍스처와 발광은 사용하지 않습니다.
MATERIALS = {}  # 원본과 Unity에서 같은 재질 이름을 사용합니다.


def write(path, content):  # 생성한 텍스트 에셋을 저장합니다.
    path.parent.mkdir(parents=True, exist_ok=True)  # 해당 에셋에 필요한 폴더만 만듭니다.
    path.write_text(content.rstrip() + '\n', encoding='utf-8')  # Unity YAML 본문에는 주석을 넣지 않습니다.


def meta(path, importer, main_id=0, folder=False):  # 새 에셋의 메타데이터를 만듭니다.
    meta_path = Path(str(path) + '.meta')  # 원본 재생성 때도 같은 에셋 참조를 유지합니다.
    result = next((line[6:].strip() for line in meta_path.read_text(encoding='utf-8').splitlines() if line.startswith('guid: ')), uuid.uuid4().hex) if meta_path.exists() else uuid.uuid4().hex  # 기존 GUID가 있으면 보존하고 새 에셋에만 식별자를 만듭니다.
    extra = 'folderAsset: yes\n' if folder else ''  # 폴더와 일반 파일을 구분합니다.
    main = f'  mainObjectFileID: {main_id}\n' if main_id else ''  # 네이티브 에셋의 기본 오브젝트입니다.
    write(Path(str(path) + '.meta'), f'fileFormatVersion: 2\nguid: {result}\n{extra}{importer}:\n  externalObjects: {{}}\n{main}  userData: \n  assetBundleName: \n  assetBundleVariant: \n')  # 대응하는 Unity 메타 파일을 저장합니다.
    return result  # 프리팹에서 사용할 참조를 돌려줍니다.


def bp(point):  # 설계 좌표를 Blender 좌표로 바꿉니다.
    return (point[0], -point[2], point[1])  # 정면과 높이를 일관되게 유지합니다.


def up(point):  # Blender 정점을 Unity 좌표로 되돌립니다.
    return (point[0], point[2], -point[1])  # 두 환경 모두 미터 단위를 사용합니다.


def mesh(name, vertices, faces, material, family, subdiv=0):  # 설계한 단면과 면으로 실제 폴리곤 메시를 만듭니다.
    data = bpy.data.meshes.new(name + '_Mesh')  # 편집 가능한 메시 데이터입니다.
    data.from_pydata([bp(v) for v in vertices], [], faces)  # 프리미티브 대신 직접 만든 표면을 사용합니다.
    data.update()  # 면 구성을 반영합니다.
    edit = bmesh.new()  # 법선을 정리할 편집 메시입니다.
    edit.from_mesh(data)  # 표면을 편집 메시로 옮깁니다.
    bmesh.ops.recalc_face_normals(edit, faces=list(edit.faces))  # 표면 법선을 바깥쪽으로 맞춥니다.
    edit.to_mesh(data)  # 정리한 표면을 돌려놓습니다.
    edit.free()  # 편집 메모리를 해제합니다.
    obj = bpy.data.objects.new(name, data)  # 원본에서 선택 가능한 부위로 만듭니다.
    bpy.context.collection.objects.link(obj)  # 제작 장면에 부위를 연결합니다.
    data.materials.append(MATERIALS[material])  # 실제 머티리얼을 부위에 연결합니다.
    obj['material_name'], obj['weight_family'] = material, family  # Unity 출력용 재질과 해부학적 영역입니다.
    for polygon in data.polygons:  # 단면 사이의 각진 음영을 없앱니다.
        polygon.use_smooth = True  # 깔끔한 카툰 곡면으로 표시합니다.
    if subdiv:  # 얼굴과 큰 의상 표면에 곡률을 추가합니다.
        bpy.context.view_layer.objects.active = obj  # 현재 부위를 수정 대상으로 지정합니다.
        modifier = obj.modifiers.new('곡면 세분화', 'SUBSURF')  # 사각 단면 사이를 부드럽게 만듭니다.
        modifier.levels = subdiv  # 필요한 곡면 단계만 적용합니다.
        bpy.ops.object.modifier_apply(modifier=modifier.name)  # 실제 메시로 확정합니다.
    PARTS.append(obj)  # 완성 부위 목록에 등록합니다.
    return obj  # 연결 표면 제작에 사용할 부위를 돌려줍니다.


def loft(name, rings, material, family, segments=24, axis='Y', centre=0, power=1.0, subdiv=1):  # 신체 길이를 따라 서로 다른 단면을 연결합니다.
    vertices, faces = [], []  # 유기적인 윤곽을 구성합니다.
    for along, radius_x, radius_z, shift in rings:  # 높이별 폭과 두께를 사용합니다.
        for index in range(segments):  # 둥근 사각 또는 타원 단면을 샘플링합니다.
            angle = 2 * math.pi * index / segments  # 둘레 위치를 계산합니다.
            sx = math.copysign(abs(math.sin(angle)) ** power, math.sin(angle))  # 의상의 둥근 모서리를 조절합니다.
            cz = math.copysign(abs(math.cos(angle)) ** power, math.cos(angle))  # 정면과 옆면의 볼륨을 조절합니다.
            vertices.append((centre + radius_x * sx, along, shift + radius_z * cz) if axis == 'Y' else (along, centre + radius_x * sx, shift + radius_z * cz))  # 몸통과 팔의 축을 구분합니다.
    for row in range(len(rings) - 1):  # 인접한 단면을 연결합니다.
        for index in range(segments):  # 관절을 따라 이어지는 면 흐름을 만듭니다.
            a, b = row * segments + index, row * segments + (index + 1) % segments  # 현재 단면의 이웃 정점입니다.
            faces.append((a, b, b + segments, a + segments))  # 다음 단면과 사각 면으로 연결합니다.
    faces.extend((tuple(reversed(range(segments))), tuple((len(rings) - 1) * segments + i for i in range(segments))))  # 보이지 않는 끝면을 닫습니다.
    return mesh(name, vertices, faces, material, family, subdiv)  # 실제 부위 표면을 만듭니다.


def tube(name, points, radii, material, family, segments=8):  # 머리카락 결과 얼굴·의상 디테일을 곡선 표면으로 만듭니다.
    vertices, faces = [], []  # 곡선의 단면과 면을 기록합니다.
    for index, point in enumerate(points):  # 곡선이 이어지는 방향을 구합니다.
        p = Vector(point)  # 현재 곡선 위치입니다.
        direction = (Vector(points[min(index + 1, len(points) - 1)]) - Vector(points[max(0, index - 1)])).normalized()  # 곡선의 접선을 구합니다.
        side = direction.cross(Vector((0, 0, 1)))  # 접선에 수직인 축입니다.
        if side.length < .01:  # 정면 축과 나란한 곡선도 표면을 유지합니다.
            side = direction.cross(Vector((0, 1, 0)))  # 다른 기준축으로 단면을 구성합니다.
        side.normalize()  # 폭을 실제 크기로 유지합니다.
        normal = direction.cross(side).normalized()  # 두 번째 단면 축입니다.
        width, depth = radii[index] if isinstance(radii[index], tuple) else (radii[index], radii[index])  # 납작한 결과 둥근 끈을 구분합니다.
        for segment in range(segments):  # 접선에 수직인 타원 단면을 만듭니다.
            angle = 2 * math.pi * segment / segments  # 단면의 정점 각도입니다.
            vertices.append(tuple(p + side * math.cos(angle) * width + normal * math.sin(angle) * depth))  # 실제 표면 정점을 기록합니다.
    for row in range(len(points) - 1):  # 곡선 방향으로 단면을 연결합니다.
        for segment in range(segments):  # 세부도 실제 폴리곤 표면으로 저장합니다.
            a, b = row * segments + segment, row * segments + (segment + 1) % segments  # 이웃 정점입니다.
            faces.append((a, b, b + segments, a + segments))  # 곡선 표면을 이어 줍니다.
    faces.extend((tuple(reversed(range(segments))), tuple((len(points) - 1) * segments + i for i in range(segments))))  # 곡선 양끝을 닫습니다.
    return mesh(name, vertices, faces, material, family)  # 디테일 부위를 등록합니다.


def union(name, objects, material, family, voxel):  # 피부와 의상 연결부를 하나의 실제 표면으로 바꿉니다.
    vertices, faces = [], []  # 겹친 단면을 모읍니다.
    for obj in objects:  # 같은 재질에 속하는 부분만 연결합니다.
        offset = len(vertices)  # 면 인덱스의 새 기준입니다.
        vertices.extend(up(v.co) for v in obj.data.vertices)  # 설계 좌표로 부위를 합칩니다.
        faces.extend(tuple(v + offset for v in p.vertices) for p in obj.data.polygons)  # 원래 면 연결을 유지합니다.
        PARTS.remove(obj)  # 연결 전 부위는 출력에서 제외합니다.
        bpy.data.objects.remove(obj, do_unlink=True)  # 원본에도 중복 부위를 남기지 않습니다.
    obj = mesh(name, vertices, faces, material, family)  # 통합 표면을 만듭니다.
    bpy.context.view_layer.objects.active = obj  # 연결 표면을 수정 대상으로 지정합니다.
    obj.data.remesh_voxel_size = voxel  # 손가락과 옷 크기에 맞는 표면 해상도입니다.
    bpy.ops.object.voxel_remesh()  # 겹친 부분을 연결된 피부나 의상으로 만듭니다.
    obj.data.materials.clear()  # 재구성 이후에도 하나의 표면 재질을 유지합니다.
    obj.data.materials.append(MATERIALS[material])  # 해당 피부나 의상 재질을 복원합니다.
    smooth = obj.modifiers.new('연결부 곡률', 'SMOOTH')  # 복셀 연결부의 각을 완화합니다.
    smooth.factor, smooth.iterations = .6, 4  # 실루엣을 유지하는 정도로 정리합니다.
    bpy.ops.object.modifier_apply(modifier=smooth.name)  # 연결부를 실제 메시로 확정합니다.
    reduce = obj.modifiers.new('게임용 표면 밀도', 'DECIMATE')  # 지나치게 작은 복셀 면을 정리합니다.
    reduce.ratio = .48  # 얼굴이나 손의 전체 형태는 유지합니다.
    bpy.ops.object.modifier_apply(modifier=reduce.name)  # 생성용 수정자를 남기지 않습니다.
    for polygon in obj.data.polygons:  # 연결 표면에도 부드러운 음영을 적용합니다.
        polygon.use_smooth = True  # 분리된 도형의 경계를 없앱니다.
    return obj  # 스키닝할 연결 표면입니다.


def disk(name, centre, width, height, depth, material, family):  # 구체 대신 아몬드형 눈의 앞면을 제작합니다.
    vertices, faces = [tuple(Vector(centre) + Vector((0, 0, depth)))], []  # 곡률 중심을 정합니다.
    for ring in range(1, 5):  # 눈 표면을 네 곡률 구간으로 나눕니다.
        t = ring / 4  # 중심에서 둘레까지의 비율입니다.
        for index in range(32):  # 눈 둘레를 부드럽게 구성합니다.
            angle = 2 * math.pi * index / 32  # 둘레의 각도입니다.
            vertices.append((centre[0] + math.cos(angle) * width * t, centre[1] + math.sin(angle) * height * t * (.86 + .14 * abs(math.sin(angle))), centre[2] + depth * (1 - t * t)))  # 가장자리는 얼굴 쪽으로 이어집니다.
    for index in range(32):  # 눈 중앙을 막힌 표면으로 만듭니다.
        faces.append((0, 1 + index, 1 + (index + 1) % 32))  # 첫 고리를 중심에 연결합니다.
    for ring in range(3):  # 나머지 고리를 연결합니다.
        for index in range(32):  # 흰자와 홍채의 면을 구성합니다.
            a, b = 1 + ring * 32 + index, 1 + ring * 32 + (index + 1) % 32  # 이웃 정점입니다.
            faces.append((a, b, b + 32, a + 32))  # 렌즈 곡면을 이어 줍니다.
    return mesh(name, vertices, faces, material, family)  # 얼굴의 실제 세부 메시입니다.


bpy.ops.object.select_all(action='SELECT')  # 새 생성 장면의 기본 오브젝트만 선택합니다.
bpy.ops.object.delete(use_global=False)  # 사용자 Blender 원본을 열지 않고 빈 장면에서 시작합니다.
bpy.context.scene.unit_settings.system = 'METRIC'  # 실제 학생 크기를 미터로 제작합니다.
bpy.context.scene.unit_settings.scale_length = 1  # Unity와 같은 크기 기준입니다.
for name, rgb in COLORS.items():  # 원본의 재질을 구성합니다.
    material = bpy.data.materials.new(name)  # 부위별 표면 색상입니다.
    material.diffuse_color = (*rgb, 1)  # 원본 뷰포트 색상을 지정합니다.
    shader = material.node_tree.nodes.get('Principled BSDF')  # 외부 텍스처 없이 색을 지정합니다.
    shader.inputs['Base Color'].default_value = (*rgb, 1)  # Unity 출력과 같은 색상입니다.
    shader.inputs['Roughness'].default_value = .72  # 과도한 피부 반사를 피합니다.
    MATERIALS[name] = material  # 부위에서 재사용합니다.

shirt = loft('ShirtBody', [(.715,.13,.078,0),(.74,.139,.084,0),(.82,.143,.086,0),(.94,.158,.09,.002),(1.045,.185,.088,0),(1.085,.194,.08,0),(1.115,.14,.071,0),(1.145,.052,.048,.003)], 'StudentShirt', 'torso', power=.82)  # 허리·가슴·어깨·목선이 다른 몸판을 만듭니다.
sleeves = [shirt]  # 소매를 몸판에 실제로 연결합니다.
for side, sign in [('Left',-1),('Right',1)]:  # 좌우 팔을 같은 비율로 제작합니다.
    rings = [(sign*x,ry,rz,0) for x,ry,rz in [(.15,.066,.068),(.21,.069,.065),(.28,.058,.057),(.39,.05,.049),(.43,.048,.048),(.51,.043,.043),(.571,.034,.037)]]  # 팔꿈치와 손목까지 가늘어지는 단면입니다.
    sleeves.append(loft(side+'Sleeve',rings,'StudentShirt','arms',axis='X',centre=1.085))  # 사람의 어깨와 팔 실루엣을 구성합니다.
    loft(side+'Cuff',[(sign*.557,.035,.038,0),(sign*.564,.036,.039,0),(sign*.582,.033,.035,0)],'StudentPants','arms',axis='X',centre=1.085,segments=20)  # 손목의 좁은 시보리입니다.
union('Sweatshirt',sleeves,'StudentShirt','shirt',.0045)  # 어깨에 구체를 끼운 모양이 아닌 연결된 의상을 만듭니다.
loft('ShirtHem',[(.718,.132,.08,0),(.725,.14,.085,0),(.743,.14,.085,0)],'StudentShirt','torso',power=.82)  # 상의 밑단의 두께입니다.
loft('CrewNeckRib',[(1.122,.057,.05,.003),(1.137,.059,.052,.003),(1.15,.05,.045,.003)],'StudentPants','torso')  # 목과 상의 경계를 표현합니다.
loft('NeckSkin',[(1.095,.038,.036,0),(1.145,.04,.037,0),(1.19,.046,.039,.003)],'StudentSkin','neck')  # 목 피부 메시 이름을 Neck 관절과 구분하여 Humanoid 매핑의 이름 충돌을 방지합니다.
head_profile=[(1.165,.026,.037,.026),(1.185,.065,.075,.061),(1.22,.104,.096,.085),(1.265,.121,.105,.102),(1.31,.124,.105,.11),(1.355,.117,.1,.109),(1.395,.094,.082,.09),(1.425,.053,.048,.052),(1.437,.007,.007,.008)]  # 턱·볼·이마·정수리의 다른 윤곽입니다.
vertices,faces=[],[]  # 얼굴을 하나의 연속적인 표면으로 구성합니다.
for y,width,front,back in head_profile:  # 얼굴의 높이별 단면입니다.
    for index in range(48):  # 이목구비 주변에 충분한 표면 정점을 둡니다.
        angle=2*math.pi*index/48  # 두상 둘레의 위치입니다.
        x,c=width*math.sin(angle),math.cos(angle)  # 얼굴의 좌우와 전후 방향입니다.
        z=front*c**.65 if c>=0 else back*c  # 앞면은 납작하고 뒤통수는 둥글게 만듭니다.
        if c>0:  # 안와와 볼은 얼굴 전면에만 적용합니다.
            z-=.006*math.exp(-((abs(x)-.049)/.027)**2-((y-1.307)/.026)**2)  # 눈 주위의 표면을 살짝 넣습니다.
            z+=.003*math.exp(-((abs(x)-.065)/.04)**2-((y-1.26)/.035)**2)  # 볼의 부드러운 볼륨입니다.
        vertices.append((x,y,z))  # 얼굴 단면 정점을 저장합니다.
for row in range(len(head_profile)-1):  # 턱부터 두상까지 연결합니다.
    for index in range(48):  # 얼굴 표면의 사각 면 흐름입니다.
        a,b=row*48+index,row*48+(index+1)%48  # 현재 둘레의 이웃 정점입니다.
        faces.append((a,b,b+48,a+48))  # 세로 방향으로 표면을 연결합니다.
faces.extend((tuple(reversed(range(48))),tuple((len(head_profile)-1)*48+i for i in range(48))))  # 턱과 정수리를 닫습니다.
mesh('FaceAndCranium',vertices,faces,'StudentSkin','Head',1)  # 구체가 아닌 얼굴 윤곽 메시를 제작합니다.
loft('Nose',[(1.267,.008,.006,.106),(1.278,.015,.016,.116),(1.289,.014,.018,.116),(1.31,.007,.012,.108),(1.325,.004,.004,.103)],'StudentSkin','Head',segments=20)  # 콧대와 코끝을 구분합니다.
for side,sign in [('Left',-1),('Right',1)]:  # 얼굴의 좌우 세부를 제작합니다.
    loft(side+'Ear',[(1.253,.009,.009,-.004),(1.264,.02,.014,-.003),(1.288,.022,.015,-.003),(1.309,.014,.012,-.005),(1.315,.005,.004,-.006)],'StudentSkin','Head',centre=sign*.122,segments=20)  # 위아래 윤곽이 다른 귀입니다.
    x=sign*.047  # 코 양쪽의 눈 위치입니다.
    disk(side+'EyeWhite',(x,1.307,.101),.026,.0185,.008,'FriendEyeWhite',side+'Eye')  # 아몬드형 흰자입니다.
    disk(side+'Iris',(x,1.307,.1105),.011,.013,.002,'StudentIris',side+'Eye')  # 흰자 안의 홍채입니다.
    disk(side+'Pupil',(x,1.307,.113),.0055,.008,.0015,'FriendFaceDark',side+'Eye')  # 홍채와 분리된 동공입니다.
    disk(side+'EyeCatchlight',(x-.003,1.312,.115),.0025,.003,.0008,'FriendEyeWhite',side+'Eye')  # 작은 눈 반사점입니다.
    tube(side+'UpperEyelid',[(x+.026*math.cos(math.pi*i/12),1.307+.019*math.sin(math.pi*i/12),.102+.002*math.sin(math.pi*i/12)) for i in range(13)],[.0015]*13,'StudentHair','Head')  # 눈 위쪽의 얇은 윤곽입니다.
    tube(side+'Eyebrow',[(x-.027+.054*i/10,1.343+.005*math.sin(math.pi*i/10),.104) for i in range(11)],[.0015+.002*math.sin(math.pi*i/10) for i in range(11)],'StudentHair','Head')  # 얼굴에 별도 눈썹을 표현합니다.
tube('Smile',[(-.03+.06*i/16,1.237-.006*math.sin(math.pi*i/16),.103) for i in range(17)],[.0013+.0007*math.sin(math.pi*i/16) for i in range(17)],'StudentLip','Head')  # 입술 곡선으로 작은 미소를 만듭니다.
vertices,faces=[],[]  # 두상에 맞는 머리카락 표면입니다.
for ring in range(13):  # 정수리부터 헤어라인까지 이어집니다.
    t=.008+.992*ring/12  # 정수리의 정점 겹침을 피합니다.
    for index in range(64):  # 앞머리와 뒷머리의 높이를 다르게 합니다.
        angle=2*math.pi*index/64  # 둘레 방향입니다.
        c=math.cos(angle)  # 얼굴 방향을 구분합니다.
        boundary=1.365+.008*math.sin(angle) if c>.25 else (1.246 if c>-.25 else 1.222)  # 앞 헤어라인을 눈썹보다 위에 두어 표정을 가리지 않습니다.
        y=1.455-(1.455-boundary)*t  # 머리카락의 높이입니다.
        q=math.sqrt(max(0,1-((y-1.317)/.143)**2))  # 두상의 곡률을 따라갑니다.
        vertices.append((.132*q*math.sin(angle)-.009*(1-t),y,.118*q*c-.003))  # 피부와 구분되는 머리카락 두께입니다.
for ring in range(12):  # 머리카락 고리를 연결합니다.
    for index in range(64):  # 짧은 머리 전체의 표면입니다.
        a,b=ring*64+index,ring*64+(index+1)%64  # 이웃 정점입니다.
        faces.append((a,b,b+64,a+64))  # 정수리와 헤어라인을 잇습니다.
faces.append(tuple(reversed(range(64))))  # 정수리를 닫습니다.
mesh('ShortHairCap',vertices,faces,'StudentHair','Head',1)  # 머리카락을 독립된 표면으로 저장합니다.
for index in range(7):  # 큰 앞머리 가닥만 만듭니다.
    end_x=-.095+.03*index  # 이마를 따라 가닥을 분산합니다.
    points=[]  # 옆 가르마에서 이마로 흐르는 곡선입니다.
    for step in range(13):  # 가닥의 곡률을 나눕니다.
        t=step/12  # 뿌리에서 끝까지의 비율입니다.
        points.append((-.043*(1-t)+end_x*t,1.438*(1-t)+(1.36+.009*math.sin(index))*t+.018*math.sin(math.pi*t),.035*(1-t)+.108*t+.018*math.sin(math.pi*t)))  # 앞머리 끝을 눈썹 위에 두고 두상을 따라 휘게 합니다.
    tube('FringeLock'+str(index+1),points,[(.001+.014*math.sin(math.pi*(.08+.9*step/12)),.004) for step in range(13)],'StudentHairHighlight' if index in (1,5) else 'StudentHair','Head')  # 끝이 가는 가닥으로 머리 결을 구분합니다.
pelvis=loft('PantsPelvis',[(.665,.105,.073,0),(.71,.137,.085,0),(.755,.136,.084,0),(.78,.125,.077,0)],'StudentPants','pants',power=.8)  # 허리와 골반이 구분되는 바지입니다.
pants_parts=[pelvis]  # 두 바지통을 실제로 연결합니다.
for side,sign in [('Left',-1),('Right',1)]:  # 허벅지부터 발목까지의 비율입니다.
    rings=[(.17,.043,.045,.002),(.20,.046,.047,.003),(.29,.049,.052,.005),(.40,.054,.055,.014),(.46,.058,.059,.01),(.57,.068,.066,.003),(.68,.074,.073,0),(.73,.076,.075,0)]  # 무릎과 밑단을 구분합니다.
    pants_parts.append(loft(side+'TrouserLeg',rings,'StudentPants','pants',centre=sign*.081,power=.86))  # 사람 다리의 단면을 구성합니다.
    loft(side+'TrouserHem',[(.168,.044,.046,.002),(.177,.047,.049,.002),(.19,.046,.048,.003)],'StudentStitch','pants',centre=sign*.081,segments=20,power=.86)  # 발목 위의 접힌 단입니다.
    tube(side+'PocketSeam',[(sign*(.055+.06*i/8),.752-.04*i/8,.085) for i in range(9)],[.0013]*9,'StudentStitch','Hips')  # 주머니의 입구를 작게 표현합니다.
union('Trousers',pants_parts,'StudentPants','pants',.004)  # 골반과 바지통이 끊기지 않는 표면입니다.
for side,sign in [('Left',-1),('Right',1)]:  # 손과 운동화를 제작합니다.
    palm=loft(side+'Palm',[(sign*.574,.023,.025,0),(sign*.605,.032,.024,.001),(sign*.64,.037,.023,.002),(sign*.66,.034,.02,.002)],'StudentSkin',side+'hand',axis='X',centre=1.085,segments=16)  # 넓고 얇은 손바닥입니다.
    pieces=[palm]  # 다섯 손가락을 연결할 준비입니다.
    for finger,dy,length in [('Index',.028,.055),('Middle',.009,.063),('Ring',-.01,.058),('Little',-.027,.047)]:  # 손가락 길이를 서로 다르게 합니다.
        radius=.0065 if finger!='Little' else .0055  # 어린이 손의 굵기입니다.
        points=[(sign*(.647+length*t),1.085+dy-.003*math.sin(math.pi*t),.003+.003*math.sin(math.pi*t)) for t in (0,.25,.5,.75,1)]  # 손가락에 작은 마디 곡률을 줍니다.
        pieces.append(tube(side+finger+'Surface',points,[radius,radius,radius*.94,radius*.8,.0025],'StudentSkin',side+'hand',10))  # 구분되는 네 손가락입니다.
    pieces.append(tube(side+'ThumbSurface',[(sign*.611,1.062,.012),(sign*.628,1.041,.026),(sign*.649,1.02,.03),(sign*.662,1.013,.028)],[.011,.009,.0075,.003],'StudentSkin',side+'hand',10))  # 엄지는 옆으로 비스듬히 뻗습니다.
    union(side+'HandSurface',pieces,'StudentSkin',side+'hand',.0018)  # 손바닥과 손가락의 피부를 연결합니다.
    cx=sign*.081  # 골반 아래에 발을 놓습니다.
    loft(side+'Sole',[(0,.055,.115,.045),(.006,.064,.127,.045),(.021,.065,.128,.045),(.029,.06,.123,.045)],'StudentSole',side+'Foot',centre=cx,power=.66,segments=32,subdiv=0)  # 밑창의 가장 낮은 면을 바닥 0에 맞춥니다.
    loft(side+'SneakerUpper',[(.025,.059,.121,.045),(.046,.064,.12,.043),(.075,.06,.101,.033),(.107,.051,.073,.006),(.14,.043,.048,-.005),(.153,.038,.04,-.008)],'FriendEyeWhite',side+'Foot',centre=cx,power=.77,segments=32)  # 발끝·발등·발목 입구가 다른 운동화입니다.
    for stripe in range(3):  # 발등 위의 신발끈입니다.
        z,y=.04+stripe*.018,.12-stripe*.009  # 발등 경사에 맞춘 위치입니다.
        tube(side+'Lace'+str(stripe),[(cx-.033,y-.002,z),(cx,y+.004,z+.004),(cx+.033,y-.002,z)],[.0022]*3,'StudentSole',side+'Foot')  # 짧은 세 줄의 실제 끈 메시입니다.
    tube(side+'ShoeSidePanel',[(cx+sign*.06,.048,.065),(cx+sign*.06,.068,.02),(cx+sign*.047,.092,-.03)],[(.008,.0025),(.01,.0025),(.006,.0025)],'StudentShoeAccent',side+'Foot')  # 옆면의 작은 색상 포인트입니다.

BONES=[]  # 표준 인간형 골격을 구성합니다.


def bone(name,parent,head,tail):  # 해부학적 위치와 부모 관계를 함께 보관합니다.
    BONES.append({'name':name,'parent':parent,'head':Vector(head),'tail':Vector(tail)})  # 실제 뼈 길이도 저장합니다.


bone('Hips',None,(0,.74,0),(0,.84,0))  # 골반을 골격 기준으로 둡니다.
bone('Spine','Hips',(0,.84,0),(0,.97,0))  # 허리 관절입니다.
bone('Chest','Spine',(0,.97,0),(0,1.065,0))  # 가슴 관절입니다.
bone('UpperChest','Chest',(0,1.065,0),(0,1.135,0))  # 어깨와 목의 연결 기준입니다.
bone('Neck','UpperChest',(0,1.135,0),(0,1.2,0))  # 고개를 돌릴 목입니다.
bone('Head','Neck',(0,1.2,0),(0,1.43,0))  # 얼굴과 머리카락의 기준입니다.
bone('Jaw','Head',(0,1.24,.05),(0,1.20,.09))  # 표준 턱 관절입니다.
for side,sign in [('Left',-1),('Right',1)]:  # 좌우 관절을 대칭으로 구성합니다.
    bone(side+'Eye','Head',(sign*.047,1.307,.09),(sign*.047,1.307,.13))  # 눈의 기준 관절입니다.
    bone(side+'Shoulder','UpperChest',(sign*.09,1.095,0),(sign*.195,1.085,0))  # 쇄골과 어깨를 구분합니다.
    bone(side+'UpperArm',side+'Shoulder',(sign*.195,1.085,0),(sign*.41,1.085,0))  # 위팔 회전 기준은 어깨입니다.
    bone(side+'LowerArm',side+'UpperArm',(sign*.41,1.085,0),(sign*.586,1.085,0))  # 팔꿈치에서 손목으로 이어집니다.
    bone(side+'Hand',side+'LowerArm',(sign*.586,1.085,0),(sign*.65,1.085,0))  # 손목 관절입니다.
    for finger,dy,length in [('Index',.028,.055),('Middle',.009,.063),('Ring',-.01,.058),('Little',-.027,.047)]:  # 네 손가락을 구분합니다.
        parent=side+'Hand'  # 첫 마디는 손목 아래에 연결합니다.
        for index,label in enumerate(('Proximal','Intermediate','Distal')):  # 표준 세 마디 이름입니다.
            name=side+finger+label  # 좌우 손가락을 구분합니다.
            a,b=.647+length*index/3,.647+length*(index+1)/3  # 표면에 맞는 마디 위치입니다.
            bone(name,parent,(sign*a,1.085+dy,.003),(sign*b,1.085+dy,.003))  # 손가락 내부에 관절을 둡니다.
            parent=name  # 다음 마디가 이전 마디를 따르게 합니다.
    thumb=[(sign*.611,1.062,.012),(sign*.628,1.041,.026),(sign*.649,1.02,.03),(sign*.662,1.013,.028)]  # 엄지의 실제 방향입니다.
    parent=side+'Hand'  # 엄지도 손목 아래에서 시작합니다.
    for index,label in enumerate(('Proximal','Intermediate','Distal')):  # 엄지 마디를 구분합니다.
        name=side+'Thumb'+label  # 표준 인간형 이름입니다.
        bone(name,parent,thumb[index],thumb[index+1])  # 엄지 표면과 같은 방향입니다.
        parent=name  # 마디의 연결 관계입니다.
    bone(side+'UpperLeg','Hips',(sign*.081,.72,0),(sign*.081,.41,.014))  # 골반 아래의 허벅지입니다.
    bone(side+'LowerLeg',side+'UpperLeg',(sign*.081,.41,.014),(sign*.081,.115,0))  # 무릎과 종아리입니다.
    bone(side+'Foot',side+'LowerLeg',(sign*.081,.115,0),(sign*.081,.045,.12))  # 발목과 발등입니다.
    bone(side+'Toes',side+'Foot',(sign*.081,.045,.12),(sign*.081,.04,.17))  # 발끝 관절입니다.
BONE_INDEX={b['name']:i for i,b in enumerate(BONES)}  # 스킨 인덱스와 골격 순서를 일치시킵니다.
BONE_MAP={b['name']:b for b in BONES}  # 부모 관절의 위치를 가져옵니다.


def blend_at(value,keys):  # 두 관절 사이의 웨이트를 보간합니다.
    if value<=keys[0][0]:  # 첫 기준점 아래의 표면입니다.
        return {keys[0][1]:1.0}  # 첫 관절에 고정합니다.
    for (a,na),(b,nb) in zip(keys,keys[1:]):  # 해당 관절 구간을 찾습니다.
        if value<=b:  # 두 기준점 사이인 경우입니다.
            t=(value-a)/(b-a)  # 구간 내 비율입니다.
            return {na:1-t,nb:t}  # 합계 1의 스킨 웨이트입니다.
    return {keys[-1][1]:1.0}  # 마지막 관절 바깥은 끝 관절에 고정합니다.


def weights(point,family):  # 부위별로 실제 관절에 스킨을 연결합니다.
    x,y,z=point  # 설계 좌표의 신체 위치입니다.
    side='Left' if x<0 else 'Right'  # 좌우 팔과 다리를 구분합니다.
    if family in BONE_INDEX:  # 머리카락과 운동화 등 고정 부위입니다.
        return {family:1.0}  # 해당 관절을 따라갑니다.
    if family=='neck':  # 목에서 머리로 연결합니다.
        return blend_at(y,[(1.13,'Neck'),(1.19,'Head')])  # 고개 회전 때 목도 함께 변형됩니다.
    if family in ('torso','shirt','arms'):  # 몸판과 소매를 같은 스킨으로 처리합니다.
        if abs(x)>.155 and y>.98:  # 어깨 바깥의 소매입니다.
            return blend_at(abs(x),[(.155,'UpperChest'),(.235,side+'UpperArm'),(.41,side+'LowerArm'),(.586,side+'Hand')])  # 어깨·팔꿈치·손목을 잇습니다.
        return blend_at(y,[(.74,'Hips'),(.84,'Spine'),(.97,'Chest'),(1.065,'UpperChest'),(1.15,'Neck')])  # 상체는 척추를 따라 변형됩니다.
    if family=='pants':  # 바지의 골반과 다리 웨이트입니다.
        if y>=.68:  # 허리 영역입니다.
            return {'Hips':1.0}  # 허리는 골반에 고정합니다.
        return blend_at(y,[(.12,side+'Foot'),(.41,side+'LowerLeg'),(.63,side+'UpperLeg'),(.70,'Hips')])  # 무릎과 발목의 영향을 이어 줍니다.
    if family.endswith('hand'):  # 손바닥과 손가락입니다.
        side=family[:-4]  # 해당 손의 좌우 이름입니다.
        if abs(x)<.643 and y>1.055:  # 손바닥 중심입니다.
            return {side+'Hand':1.0}  # 손가락 회전에서 손바닥을 유지합니다.
        distances=[]  # 가까운 두 손가락 마디를 구합니다.
        p=Vector(point)  # 피부 정점의 위치입니다.
        for item in BONES:  # 해당 손의 손가락만 후보로 사용합니다.
            if not item['name'].startswith(side) or not any(f in item['name'] for f in ('Thumb','Index','Middle','Ring','Little')):  # 다른 신체 부위를 제외합니다.
                continue  # 손가락 관절에만 웨이트를 적용합니다.
            direction=item['tail']-item['head']  # 실제 뼈 구간입니다.
            t=max(0,min(1,(p-item['head']).dot(direction)/direction.length_squared))  # 가장 가까운 뼈 위치입니다.
            distances.append(((p-item['head']-direction*t).length,item['name']))  # 피부와 관절의 거리를 저장합니다.
        values=[(1/max(.002,d)**3,n) for d,n in sorted(distances)[:2]]  # 인접 마디 두 개에 영향을 나눕니다.
        total=sum(v for v,n in values)  # 가중치 정규화 기준입니다.
        return {n:v/total for v,n in values}  # 합계 1인 손가락 스킨입니다.
    return {'Hips':1.0}  # 고정 장식의 기본 기준입니다.


armature_data=bpy.data.armatures.new('StudentHumanoidSkeleton')  # 실제 인간형 골격 데이터입니다.
armature=bpy.data.objects.new('StudentArmature',armature_data)  # 모델과 분리된 골격입니다.
bpy.context.collection.objects.link(armature)  # 원본 장면에 골격을 연결합니다.
bpy.context.view_layer.objects.active=armature  # 관절 편집 대상을 지정합니다.
armature.select_set(True)  # 편집할 골격을 선택합니다.
bpy.ops.object.mode_set(mode='EDIT')  # 실제 뼈를 생성합니다.
for item in BONES:  # 표준 이름과 부모 관계를 순서대로 적용합니다.
    edit=armature_data.edit_bones.new(item['name'])  # 독립적인 실제 관절입니다.
    edit.head,edit.tail=bp(item['head']),bp(item['tail'])  # 신체 내부에 정확하게 배치합니다.
    if item['parent']:  # 골반 이외의 관절입니다.
        edit.parent=armature_data.edit_bones[item['parent']]  # 상위 관절을 따라가게 합니다.
bpy.ops.object.mode_set(mode='OBJECT')  # 스킨을 연결할 모드로 돌아옵니다.
armature.show_in_front=True  # 원본에서 관절을 쉽게 선택합니다.
for obj in PARTS:  # 모든 피부와 의상을 스키닝합니다.
    groups={b['name']:obj.vertex_groups.new(name=b['name']) for b in BONES}  # FBX에서도 표준 이름을 보존합니다.
    for vertex in obj.data.vertices:  # 실제 표면 위치에 맞는 웨이트입니다.
        for name,value in weights(up(vertex.co),obj['weight_family']).items():  # 관절 영향값을 가져옵니다.
            if value>.00001:  # 의미 있는 영향만 저장합니다.
                groups[name].add([vertex.index],value,'REPLACE')  # 피부를 실제 뼈에 연결합니다.
    modifier=obj.modifiers.new('학생 골격 변형','ARMATURE')  # 메시의 관절 변형을 준비합니다.
    modifier.object=armature  # 모든 부위를 같은 골격에 연결합니다.
    obj.parent=armature  # 원본 전체가 같은 루트를 따라갑니다.

MODELS.mkdir(parents=True,exist_ok=True)  # 실제 모델 출력 위치입니다.
SOURCE.mkdir(parents=True,exist_ok=True)  # 제작 원본 출력 위치입니다.
if not Path(str(MODELS)+'.meta').exists():  # 기존 폴더 식별자를 유지합니다.
    meta(MODELS,'DefaultImporter',folder=True)  # 새 폴더 메타데이터입니다.
material_guids={'FriendEyeWhite':'fcf480cc4469d512d3f4710bc2c01878','FriendFaceDark':'871866a959870c379d64b63cfd18afe6'}  # 기존 재질을 수정하지 않고 연결합니다.
for name,rgb in COLORS.items():  # 학생 전용 머티리얼을 저장합니다.
    if name in material_guids:  # 흰자와 동공 재질은 기존 파일입니다.
        continue  # 공통 리소스를 덮어쓰지 않습니다.
    path=AREA/'Materials'/'Characters'/(name+'.mat')  # 캐릭터 재질 폴더에서 학생 재질의 기존 파일과 GUID를 유지합니다.
    material_guids[name]=meta(path,'NativeFormatImporter',2100000)  # 새 재질 참조를 만듭니다.
    color=f'{{r: {rgb[0]}, g: {rgb[1]}, b: {rgb[2]}, a: 1}}'  # 불투명 표면색입니다.
    text=HEADER+'--- !u!21 &2100000\nMaterial:\n  serializedVersion: 8\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n'  # 기존 Unity 재질 형식입니다.
    text+=f'  m_Name: {name}\n  m_Shader: {{fileID: 4800000, guid: 933532a4fcc9baf4fa0491de14d08ed7, type: 3}}\n  m_Parent: {{fileID: 0}}\n  m_ModifiedSerializedProperties: 0\n  m_ValidKeywords:\n  - _ENVIRONMENTREFLECTIONS_OFF\n  - _SPECULARHIGHLIGHTS_OFF\n  m_InvalidKeywords: []\n  m_LightmapFlags: 4\n  m_EnableInstancingVariants: 0\n  m_DoubleSidedGI: 0\n  m_CustomRenderQueue: -1\n  stringTagMap:\n    RenderType: Opaque\n  disabledShaderPasses:\n  - MOTIONVECTORS\n  m_LockedProperties:\n  m_SavedProperties:\n    serializedVersion: 3\n    m_TexEnvs: []\n    m_Ints: []\n    m_Floats:\n'  # 기존 URP Lit 렌더링을 유지합니다.
    for key,value in {'_Surface':0,'_Blend':0,'_SrcBlend':1,'_DstBlend':0,'_SrcBlendAlpha':1,'_DstBlendAlpha':0,'_ZWrite':1,'_Cull':2,'_AlphaClip':0,'_Cutoff':.5,'_WorkflowMode':1,'_Metallic':0,'_Smoothness':.22,'_SpecularHighlights':0,'_EnvironmentReflections':0,'_ReceiveShadows':1}.items():  # 과도한 반사와 발광이 없는 표면입니다.
        text+=f'    - {key}: {value}\n'  # 표면 속성을 기록합니다.
    text+=f'    m_Colors:\n    - _BaseColor: {color}\n    - _Color: {color}\n    - _EmissionColor: {{r: 0, g: 0, b: 0, a: 1}}\n  m_BuildTextureStacks: []\n  m_AllowLocking: 1\n'  # 실제 색상만 사용합니다.
    write(path,text)  # 재질을 저장합니다.
armature.data.pose_position='REST'  # FBX는 인간형 기준 T자 자세로 저장합니다.
bpy.ops.object.select_all(action='DESELECT')  # 모델 이외의 오브젝트를 제외합니다.
armature.select_set(True)  # 표준 골격을 내보냅니다.
for obj in PARTS:  # 직접 만든 모든 부위를 내보냅니다.
    obj.select_set(True)  # 카메라와 조명은 포함하지 않습니다.
bpy.context.view_layer.objects.active=armature  # 골격을 내보내기의 기준으로 사용합니다.
bpy.ops.export_scene.fbx(filepath=str(MODELS/'StudentCharacter.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',use_mesh_modifiers=False,add_leaf_bones=False,use_armature_deform_only=True,bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='AUTO')  # 메시·스킨·표준 골격을 가진 FBX를 저장합니다.
fbx_meta_path=MODELS/'StudentCharacter.fbx.meta'  # 기존 FBX의 참조를 유지할 메타 파일입니다.
fbx_guid=next((line[6:].strip() for line in fbx_meta_path.read_text(encoding='utf-8').splitlines() if line.startswith('guid: ')),uuid.uuid4().hex) if fbx_meta_path.exists() else uuid.uuid4().hex  # 모델을 수정해 다시 저장해도 FBX 식별자를 유지합니다.
text=f'fileFormatVersion: 2\nguid: {fbx_guid}\nModelImporter:\n  serializedVersion: 22200\n  internalIDToNameTable: []\n  externalObjects:\n'  # 프로젝트의 모델 임포터 형식입니다.
for name,asset_guid in material_guids.items():  # FBX 재질도 URP 에셋으로 연결합니다.
    text+=f'  - first:\n      type: UnityEngine:Material\n      assembly: UnityEngine.CoreModule\n      name: {name}\n    second: {{fileID: 2100000, guid: {asset_guid}, type: 2}}\n'  # 기본 Standard 재질 생성을 피합니다.
text+='  materials:\n    materialImportMode: 2\n    materialName: 0\n    materialSearch: 1\n    materialLocation: 1\n  animations:\n    optimizeGameObjects: 0\n    clipAnimations: []\n    isReadable: 0\n  meshes:\n    globalScale: 1\n    meshCompression: 0\n    addColliders: 0\n    importCameras: 0\n    importLights: 0\n    fileIdsGeneration: 2\n    useFileUnits: 1\n    useFileScale: 1\n    preserveHierarchy: 1\n    skinWeightsMode: 0\n    maxBonesPerVertex: 4\n    minBoneWeight: 0.001\n    optimizeBones: 0\n    indexFormat: 0\n  tangentSpace:\n    normalImportMode: 0\n    tangentImportMode: 3\n  importAnimation: 0\n  humanDescription:\n    serializedVersion: 3\n    human:\n'  # 임포트는 Humanoid로 준비하고 애니메이션 클립은 만들지 않습니다.
for item in BONES:  # 몸통과 팔다리, 손가락을 표준 이름에 매핑합니다.
    name=item['name']  # 실제 뼈의 이름입니다.
    text+=f'    - boneName: {name}\n      humanName: {name}\n      limit:\n        min: {{x: 0, y: 0, z: 0}}\n        max: {{x: 0, y: 0, z: 0}}\n        value: {{x: 0, y: 0, z: 0}}\n        length: 0\n        modified: 0\n'  # 관절 제한은 Unity 기본값을 사용합니다.
text+='    skeleton: []\n    armTwist: 0.5\n    foreArmTwist: 0.5\n    upperLegTwist: 0.5\n    legTwist: 0.5\n    armStretch: 0.05\n    legStretch: 0.05\n    feetSpacing: 0\n    globalScale: 1\n    rootMotionBoneName: \n    hasTranslationDoF: 0\n    hasExtraRoot: 0\n    skeletonHasParents: 1\n  lastHumanDescriptionAvatarSource: {instanceID: 0}\n  autoGenerateAvatarMappingIfUnspecified: 1\n  animationType: 3\n  humanoidOversampling: 1\n  avatarSetup: 1\n  addHumanoidExtraRootOnlyWhenUsingAvatar: 1\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'  # 다음 Unity 임포트에서 Avatar를 생성하도록 준비합니다.
write(MODELS/'StudentCharacter.fbx.meta',text)  # FBX 임포트 설정을 저장합니다.

vertices,normals,skin_values=[],[],[]  # Unity가 직접 사용할 스킨 메시입니다.
triangles={name:[] for name in COLORS}  # 재질별 서브메시를 구성합니다.
for obj in PARTS:  # Blender의 완성 표면을 그대로 옮깁니다.
    data=obj.data  # T자 바인드 자세의 표면입니다.
    data.calc_loop_triangles()  # Unity용 삼각형 인덱스를 만듭니다.
    offset=len(vertices)  # 통합 메시에서의 시작 정점입니다.
    for vertex in data.vertices:  # 실제 피부와 의상 정점을 기록합니다.
        point=up(vertex.co)  # Unity 좌표입니다.
        vertices.append(point)  # 부위 실루엣을 유지합니다.
        normals.append(up(vertex.normal))  # 부드러운 법선을 유지합니다.
        skin_values.append(weights(point,obj['weight_family']))  # FBX와 동일한 관절 웨이트입니다.
    for triangle in data.loop_triangles:  # 같은 재질의 면을 모읍니다.
        triangles[obj['material_name']].extend(i+offset for i in triangle.vertices)  # 통합 인덱스로 바꿉니다.
palette=[name for name,values in triangles.items() if values]  # 실제 사용된 재질 순서입니다.
buffer,indices=bytearray(),[]  # 정점과 삼각형 버퍼입니다.
for point,normal,values in zip(vertices,normals,skin_values):  # 위치·법선·UV·스킨을 작성합니다.
    influences=sorted(values.items(),key=lambda item:item[1],reverse=True)[:4]  # 정점당 최대 네 뼈를 사용합니다.
    total=sum(value for name,value in influences)  # 가중치 정규화 기준입니다.
    ws=[value/total for name,value in influences]+[0.]*(4-len(influences))  # 가중치 합은 1입니다.
    ids=[BONE_INDEX[name] for name,value in influences]+[0]*(4-len(influences))  # 골격 배열의 인덱스입니다.
    buffer.extend(struct.pack('<3f3f2f4f4I',*point,*normal,point[0]+.5,point[1]/1.46,*ws,*ids))  # 표준 64바이트 정점 스트림입니다.
minimum=[min(v[a] for v in vertices) for a in range(3)]  # 밑창부터 머리카락까지의 경계입니다.
maximum=[max(v[a] for v in vertices) for a in range(3)]  # 손끝도 메시 범위에 포함합니다.
centre=[(a+b)/2 for a,b in zip(minimum,maximum)]  # 메시 경계의 중심입니다.
extent=[(b-a)/2 for a,b in zip(minimum,maximum)]  # 경계의 반크기입니다.


def vec(value):  # Unity 벡터 표기를 만듭니다.
    return '{x: %.8g, y: %.8g, z: %.8g}'%tuple(value)  # 미터 단위 정밀도를 유지합니다.


text=HEADER+'--- !u!43 &4300000\nMesh:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_Name: StudentCharacterSkin\n  serializedVersion: 11\n  m_SubMeshes:\n'  # 기존 프로젝트의 네이티브 메시 형식입니다.
for name in palette:  # 각 재질의 삼각형 범위입니다.
    start,count=len(indices),len(triangles[name])  # 버퍼 안의 시작과 길이입니다.
    indices.extend(triangles[name])  # 재질별로 연속 배치합니다.
    text+=f'  - serializedVersion: 2\n    firstByte: {start*4}\n    indexCount: {count}\n    topology: 0\n    baseVertex: 0\n    firstVertex: 0\n    vertexCount: {len(vertices)}\n    localAABB:\n      m_Center: {vec(centre)}\n      m_Extent: {vec(extent)}\n'  # 충분한 정점 수를 위해 32비트 인덱스를 사용합니다.
text+='  m_Shapes:\n    vertices: []\n    shapes: []\n    channels: []\n    fullWeights: []\n  m_BindPose:\n'  # 이번에는 표정 애니메이션을 만들지 않습니다.
for item in BONES:  # T자 기준 골격의 바인드 행렬입니다.
    matrix=Matrix.Translation(-item['head'])  # 기준 관절의 역변환입니다.
    for row in range(4):  # 4×4 행렬을 작성합니다.
        for column in range(4):  # 회전과 이동을 함께 기록합니다.
            text+=('  - ' if row==0 and column==0 else '    ')+f'e{row}{column}: {matrix[row][column]:.9g}\n'  # 골격 배열 순서를 보존합니다.
text+='  m_BoneNameHashes: \n  m_RootBoneNameHash: 0\n  m_BonesAABB: []\n  m_VariableBoneCountWeights:\n    m_Data: \n  m_MeshCompression: 0\n  m_IsReadable: 0\n  m_KeepVertices: 0\n  m_KeepIndices: 0\n  m_IndexFormat: 1\n'  # 스킨 웨이트는 정점 스트림에 저장합니다.
text+='  m_IndexBuffer: '+b''.join(struct.pack('<I',i) for i in indices).hex()+'\n'  # 리틀 엔디언 삼각형 버퍼입니다.
text+=f'  m_VertexData:\n    serializedVersion: 3\n    m_VertexCount: {len(vertices)}\n    m_Channels:\n'  # 실제 정점 수를 기록합니다.
for index in range(14):  # Unity의 표준 정점 속성 순서입니다.
    offset,fmt,dimension={0:(0,0,3),1:(12,0,3),4:(24,0,2),12:(32,0,4),13:(48,10,4)}.get(index,(0,0,0))  # 본 인덱스는 UInt32입니다.
    text+=f'    - stream: 0\n      offset: {offset}\n      format: {fmt}\n      dimension: {dimension}\n'  # 사용하지 않는 채널 차원은 0입니다.
text+=f'    m_DataSize: {len(buffer)}\n    _typelessdata: {buffer.hex()}\n  m_CompressedMesh:\n'  # 정점과 스킨을 실제 메시 데이터로 저장합니다.
for field in ('Vertices','UV','Normals','Tangents','Weights','NormalSigns','TangentSigns','FloatColors','BoneIndices','Triangles'):  # 압축하지 않는 채널의 기본값입니다.
    text+=f'    m_{field}:\n      m_NumItems: 0\n'  # 별도 압축 버퍼는 없습니다.
    if field in ('Vertices','UV','Normals','Tangents','FloatColors'):  # 실수 압축 채널의 기본 범위입니다.
        text+='      m_Range: 0\n      m_Start: 0\n'  # 빈 압축 메타데이터입니다.
    text+='      m_Data: \n      m_BitSize: 0\n'  # 압축 데이터는 기록하지 않습니다.
text+=f'    m_UVInfo: 0\n  m_LocalAABB:\n    m_Center: {vec(centre)}\n    m_Extent: {vec(extent)}\n  m_MeshUsageFlags: 0\n  m_CookingOptions: 30\n  m_BakedConvexCollisionMesh: \n  m_BakedTriangleCollisionMesh: \n  m_MeshOptimizationFlags: 1\n  m_StreamData:\n    serializedVersion: 2\n    offset: 0\n    size: 0\n    path: \n'  # 물리 메시나 외부 스트림은 추가하지 않습니다.
mesh_path=MODELS/'StudentCharacterSkin.asset'  # 프리팹에서 직접 사용할 스킨 메시입니다.
mesh_guid=meta(mesh_path,'NativeFormatImporter',4300000)  # 메시의 Unity 참조를 만듭니다.
write(mesh_path,text)  # 메시와 스킨 웨이트를 저장합니다.

nodes=[{'id':600000,'name':'FriendCharacter','parent':None,'pos':Vector((0,0,0)),'rot':(0,0,0,1)},{'id':600010,'name':'VisualRoot','parent':600000,'pos':Vector((0,0,0)),'rot':(0,0,0,1)},{'id':600020,'name':'Armature','parent':600010,'pos':Vector((0,0,0)),'rot':(0,0,0,1)}]  # 기존 프리팹 루트 식별자를 유지합니다.
bone_ids={item['name']:601000+i*10 for i,item in enumerate(BONES)}  # 각 관절의 프리팹 식별자입니다.
posed_world={}  # 팔을 내린 정적인 대기 자세입니다.
for item in BONES:  # 부모 관절에서 자식 순서로 구성합니다.
    parent_head=BONE_MAP[item['parent']]['head'] if item['parent'] else Vector((0,0,0))  # 부모의 기준 위치입니다.
    position=item['head']-parent_head  # 실제 관절의 로컬 위치입니다.
    rotation=Matrix.Identity(4)  # 기준 T자 자세의 회전입니다.
    if item['name'].endswith('UpperArm'):  # 씬에서는 양팔을 내립니다.
        rotation=Matrix.Rotation(math.radians(75 if item['name'].startswith('Left') else -75),4,'Z')  # 어깨에서 팔을 자연스럽게 내립니다.
    if item['name'].endswith('LowerArm'):  # 팔꿈치는 조금 앞으로 굽힙니다.
        rotation=Matrix.Rotation(math.radians(7 if item['name'].startswith('Left') else -7),4,'Y')  # 손이 몸통을 관통하지 않게 합니다.
    local=Matrix.Translation(position)@rotation  # 로컬 위치와 회전입니다.
    posed_world[item['name']]=posed_world[item['parent']]@local if item['parent'] else local  # 최종 대기 자세를 계산합니다.
    q=rotation.to_quaternion()  # Unity 회전 표기로 바꿉니다.
    nodes.append({'id':bone_ids[item['name']],'name':item['name'],'parent':bone_ids[item['parent']] if item['parent'] else 600020,'pos':position,'rot':(q.x,q.y,q.z,q.w)})  # 독립적으로 제어할 실제 관절입니다.
nodes.append({'id':602000,'name':'StudentSkinnedMesh','parent':600010,'pos':Vector((0,0,0)),'rot':(0,0,0,1)})  # 실제 스킨 메시를 골격과 분리합니다.
prefab=HEADER  # 기존 프리팹 파일의 본문을 교체합니다.
for node in nodes:  # 루트와 시각 모델, 전체 관절을 직렬화합니다.
    oid=node['id']  # 게임 오브젝트의 식별자입니다.
    prefab+=f'--- !u!1 &{oid}\nGameObject:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  serializedVersion: 6\n  m_Component:\n  - component: {{fileID: {oid+1}}}\n'  # 각 관절에는 하나의 Transform을 연결합니다.
    if oid==602000:  # 메시 오브젝트에만 스킨 렌더러를 추가합니다.
        prefab+='  - component: {fileID: 13700000}\n'  # 기본 도형 렌더러는 남기지 않습니다.
    prefab+=f'  m_Layer: 0\n  m_Name: {node["name"]}\n  m_TagString: Untagged\n  m_Icon: {{fileID: 0}}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1\n--- !u!4 &{oid+1}\nTransform:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: {oid}}}\n  serializedVersion: 2\n'  # 관절은 단위 크기의 Transform입니다.
    q=node['rot']  # 정적인 대기 자세의 회전입니다.
    prefab+=f'  m_LocalRotation: {{x: {q[0]:.9g}, y: {q[1]:.9g}, z: {q[2]:.9g}, w: {q[3]:.9g}}}\n  m_LocalPosition: {vec(node["pos"])}\n  m_LocalScale: {{x: 1, y: 1, z: 1}}\n  m_ConstrainProportionsScale: 0\n'  # 신체 스케일로 관절이 왜곡되지 않게 합니다.
    children=[child['id']+1 for child in nodes if child['parent']==oid]  # 실제 자식 관절 목록입니다.
    prefab+=('  m_Children:\n'+''.join(f'  - {{fileID: {child}}}\n' for child in children)) if children else '  m_Children: []\n'  # 전체가 같은 루트를 따라 움직입니다.
    prefab+=f'  m_Father: {{fileID: {node["parent"]+1 if node["parent"] is not None else 0}}}\n  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}\n'  # 다른 씬이나 도로 기능에는 의존하지 않습니다.
prefab+='--- !u!137 &13700000\nSkinnedMeshRenderer:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 602000}\n  m_Enabled: 1\n  m_CastShadows: 1\n  m_ReceiveShadows: 1\n  m_DynamicOccludee: 1\n  m_StaticShadowCaster: 0\n  m_MotionVectors: 0\n  m_LightProbeUsage: 1\n  m_ReflectionProbeUsage: 0\n  m_RayTracingMode: 0\n  m_RayTraceProcedural: 0\n  m_RenderingLayerMask: 1\n  m_RendererPriority: 0\n  m_Materials:\n'  # 뼈의 움직임으로 변형되는 실제 스킨 렌더러입니다.
prefab+=''.join(f'  - {{fileID: 2100000, guid: {material_guids[name]}, type: 2}}\n' for name in palette)  # 서브메시와 재질 순서를 맞춥니다.
prefab+=f'  m_StaticBatchInfo:\n    firstSubMesh: 0\n    subMeshCount: 0\n  m_StaticBatchRoot: {{fileID: 0}}\n  m_ProbeAnchor: {{fileID: {bone_ids["Hips"]+1}}}\n  m_LightProbeVolumeOverride: {{fileID: 0}}\n  m_ScaleInLightmap: 1\n  m_ReceiveGI: 1\n  m_PreserveUVs: 0\n  m_IgnoreNormalsForChartDetection: 0\n  m_ImportantGI: 0\n  m_StitchLightmapSeams: 1\n  m_SelectedEditorRenderState: 3\n  m_MinimumChartSize: 4\n  m_AutoUVMaxDistance: 0.5\n  m_AutoUVMaxAngle: 89\n  m_LightmapParameters: {{fileID: 0}}\n  m_SortingLayerID: 0\n  m_SortingLayer: 0\n  m_SortingOrder: 0\n  m_Quality: 4\n  m_UpdateWhenOffscreen: 0\n  m_SkinnedMotionVectors: 0\n  m_Mesh: {{fileID: 4300000, guid: {mesh_guid}, type: 2}}\n  m_Bones:\n'  # 애니메이션 클립과 교육 동작 코드는 추가하지 않습니다.
prefab+=''.join(f'  - {{fileID: {bone_ids[item["name"]]+1}}}\n' for item in BONES)  # 스킨 인덱스에 실제 관절을 연결합니다.
prefab+='  m_BlendShapeWeights: []\n  m_RootBone: {fileID: 600021}\n  m_AABB:\n    m_Center: {x: 0, y: 0.73, z: 0}\n    m_Extent: {x: 0.85, y: 0.78, z: 0.35}\n  m_DirtyAABB: 0\n'  # 표시 경계는 원점이 고정된 골격 루트를 기준으로 합니다.
write(AREA/'Prefabs'/'FriendCharacter.prefab',prefab)  # 프리팹 GUID와 기존 씬 인스턴스 연결을 유지합니다.
armature.data.pose_position='POSE'  # Blender 원본도 같은 정적 대기 자세로 저장합니다.
for item in BONES:  # Blender 기준축에 같은 관절 변형을 적용합니다.
    rest=Matrix.Translation(item['head'])  # Unity의 바인드 위치입니다.
    deformation=C@posed_world[item['name']]@rest.inverted()@C.inverted()  # 두 좌표계의 차이를 보정합니다.
    armature.pose.bones[item['name']].matrix=deformation@armature.data.bones[item['name']].matrix_local  # 키프레임 없이 대기 자세만 설정합니다.
    bpy.context.view_layer.update()  # 자식 관절이 변경된 부모 자세를 사용하게 합니다.
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'StudentCharacter.blend'))  # 메시·스킨·표준 골격을 편집 가능한 원본으로 저장합니다.
print('학생 모델 원본, FBX, Unity 스킨 메시와 FriendCharacter 프리팹을 저장했습니다.')  # 생성 완료만 보고하고 실행 검증 성공을 주장하지 않습니다.
