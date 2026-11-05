import bpy  # Blender에서 실제 메시와 골격을 제작하고 FBX로 저장합니다.
import bmesh  # 직접 작성한 표면의 면 방향을 바깥쪽으로 정리합니다.
import math  # 둥근 단면과 곡선의 정점 위치를 계산합니다.
import uuid  # 새 Unity 에셋의 고유 식별자를 작성합니다.
import sys  # 얼굴과 헤어만 교체하는 제작 범위 인자를 읽습니다.
from pathlib import Path  # 제작 결과를 현재 프로젝트의 모델 폴더에 저장합니다.
from mathutils import Vector, Quaternion  # 곡선 방향과 골격의 위치 및 자세를 계산합니다.
from mathutils.bvhtree import BVHTree  # 완성된 피부 표면에 눈과 입의 실제 정점을 밀착시킵니다.

AREA = Path(__file__).resolve().parents[2]  # 모델 원본과 같은 프로젝트의 Assets를 제작 기준으로 사용합니다.
OUTPUT = AREA / 'Models' / 'SoftCrosswalk'  # 개별 FBX와 대응 재질을 함께 저장합니다.
SOURCE = Path(__file__).resolve().parent  # 편집 원본은 Unity 임포트 대상에서 제외되는 Source~에 저장합니다.
HEADER = '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'  # Unity 직렬화 파일의 표준 헤더입니다.
ASSETS, MATERIALS = [], {}  # 개별 모델과 공통 재질을 제작 순서대로 보관합니다.
ACTIVE_ROOT = None  # 현재 제작하는 메시가 속할 모델 루트입니다.
BODY_SCALE = Vector((1.03, .68, 1.10))  # 몸의 높이를 줄이고 폭과 두께를 확보하여 큰 머리를 받치는 어린이 체형으로 만듭니다.
HEAD_DROP = .16  # 짧아진 몸의 목에 맞춰 얼굴과 머리카락 및 표정 전체를 내리는 거리입니다.
ARM_LENGTH = .70  # 어깨 바깥의 팔과 손 및 손가락 뼈를 같은 비율로 짧게 만드는 길이입니다.
HAND_FINGERS = (('Index', .045, .047, .014), ('Middle', .015, .052, .015), ('Ring', -.016, .046, .014), ('Little', -.044, .035, .012))  # 넓은 손바닥에 붙는 짧고 도톰한 네 손가락의 위치와 길이 및 반경입니다.
THUMB_POINTS = ((.603, 1.048, .010), (.631, 1.021, .029), (.659, 1.010, .041), (.679, 1.021, .043))  # 손바닥 옆에서 부드럽게 구부러지는 엄지의 메시와 관절이 공유할 중심선입니다.
COLORS = {  # 캐릭터와 거리 전체에서 공유하는 밝은 단색 팔레트입니다.
    'Mint': (.23, .72, .60),  # 첫 번째 친구의 민트색입니다.
    'Apricot': (.98, .61, .30),  # 두 번째 친구의 살구색입니다.
    'Ink': (.065, .105, .15),  # 눈과 입 및 가느다란 팔다리의 부드러운 먹색입니다.
    'Cream': (.98, .94, .82),  # 창틀과 소품에 사용하는 크림색입니다.
    'Coral': (.94, .38, .39),  # 자동차의 산호색입니다.
    'Blush': (.98, .54, .50),  # 작은 볼 표정의 분홍색입니다.
    'Glass': (.18, .36, .44),  # 복잡한 반사 없이 창문을 읽게 하는 청회색입니다.
    'Road': (.27, .32, .39),  # 캐릭터와 노면 표시가 구분되는 차도색입니다.
    'Pavement': (.83, .80, .70),  # 보행 구역의 따뜻한 모래색입니다.
    'Grass': (.51, .73, .50),  # 학교 주변 지면의 초록색입니다.
    'Leaf': (.25, .56, .40),  # 나무 수관의 초록색입니다.
    'LeafLight': (.66, .79, .45),  # 관목과 원경 지형의 밝은 초록색입니다.
    'Roof': (.24, .51, .54),  # 학교와 건물 지붕의 청록색입니다.
    'White': (.98, .98, .93),  # 횡단보도와 구름의 따뜻한 흰색입니다.
    'Yellow': (.99, .77, .29),  # 차선과 충격 효과의 노란색입니다.
    'School': (.97, .84, .54),  # 학교 외벽의 부드러운 노란색입니다.
    'Blue': (.49, .71, .79),  # 주변 건물과 휴대전화 화면의 하늘색입니다.
    'Pink': (.88, .63, .67),  # 배경 건물의 분홍색입니다.
    'Bark': (.48, .32, .23),  # 나무 줄기의 갈색입니다.
    'GreenLight': (.19, .90, .45),  # 보행 신호 그림의 초록색입니다.
    'RedLight': (.99, .24, .28),  # 정지 신호 그림의 빨간색입니다.
    'Skin': (.98, .76, .53),  # 밝은 조명에서도 얼굴의 따뜻한 복숭아색이 남도록 피부 색상을 지정합니다.
    'Hair': (.025, .018, .014),  # 남자아이의 단정한 짧은 머리에 사용할 흑갈색입니다.
    'HairEdge': (.060, .042, .032),  # 짙은 머리에서 머릿결만 은은하게 구분하는 갈색입니다.
    'Iris': (.24, .105, .045),  # 흰자 안에서 시선이 읽히는 갈색 홍채입니다.
    'Mouth': (.30, .025, .045),  # 입 안쪽을 선명한 적갈색으로 구분하여 열린 미소가 읽히게 합니다.
    'Denim': (.085, .17, .28),  # 바지의 형태를 구분하는 남색입니다.
    'Shoe': (.91, .92, .90),  # 갑피와 밑창을 구분하는 운동화의 밝은 회색입니다.
    'FaceInk': (.015, .022, .032),  # 밝은 피부에서 세로 타원 눈이 또렷하게 보이는 짙은 먹색입니다.
    'CopperHair': (.70, .24, .075),  # 선형 색상으로 표시해도 주황빛이 흐려지지 않는 따뜻한 머리색입니다.
    'BlondHair': (.98, .76, .30),  # 참고 이미지의 노란 크림색에 가까운 밝은 금발입니다.
    'HairLine': (.29, .18, .16),  # 앞머리 조각의 경계를 얇게 구분하는 갈색 선입니다.
    'HairPaint': (.995, .91, .65),  # 머리카락 위에 그린 작은 밝은 색면입니다.
}  # 각 모델이 같은 그래픽 방향을 공유하게 합니다.


def write(path, content):  # 재질과 임포트 설정을 원본 형식으로 저장합니다.
    path.parent.mkdir(parents=True, exist_ok=True)  # 해당 에셋에 필요한 폴더만 만듭니다.
    path.write_text(content, encoding='utf-8')  # 한글과 Unity 직렬화 내용을 그대로 저장합니다.


def metadata(path, importer='DefaultImporter', main_id=None, folder=False):  # Unity가 새 에셋의 참조를 유지할 메타 파일을 만듭니다.
    target = Path(str(path) + '.meta')  # 원본 옆에 대응 메타 파일을 둡니다.
    previous = target.read_text(encoding='utf-8') if target.exists() else ''  # 재제작 시 기존 식별자를 유지합니다.
    guid = next((line[6:].strip() for line in previous.splitlines() if line.startswith('guid: ')), uuid.uuid4().hex)  # 처음 만드는 에셋에만 새 식별자를 부여합니다.
    extra = 'folderAsset: yes\n' if folder else ''  # 폴더 에셋에는 해당 표시를 추가합니다.
    main = f'  mainObjectFileID: {main_id}\n' if main_id is not None else ''  # 머티리얼의 기본 오브젝트 식별자입니다.
    write(target, f'fileFormatVersion: 2\nguid: {guid}\n{extra}{importer}:\n  externalObjects: {{}}\n{main}  userData: \n  assetBundleName: \n  assetBundleVariant: \n')  # Unity 저장 형식에 임의의 주석은 추가하지 않습니다.
    return guid  # FBX의 머티리얼 연결에도 같은 식별자를 사용합니다.


def bp(point):  # Unity의 Y축 높이와 Z축 정면을 Blender 좌표로 바꿉니다.
    return (point[0], -point[2], point[1])  # FBX를 미터 단위로 가져와도 같은 방향을 유지합니다.


def body_point(point):  # 몸의 정점과 바인드 관절에 같은 어린이 비율을 적용합니다.
    x, y, z = point  # 축소 전 신체의 가로와 높이 및 깊이입니다.
    if y > .95 and abs(x) > .17:  # 어깨 바깥의 팔과 손에만 추가 길이 압축을 적용합니다.
        x = math.copysign(.17 + (abs(x) - .17) * ARM_LENGTH, x)  # 몸통 폭을 유지하면서 위팔부터 손끝까지 연속해서 줄입니다.
    return Vector((x * BODY_SCALE.x, y * BODY_SCALE.y, z * BODY_SCALE.z))  # 피부와 뼈가 동일한 위치에 놓이는 최종 설계 좌표입니다.


def finger_point(sign, offset, length, t):  # 짧은 손가락의 표면과 세 마디 관절이 같은 곡선을 사용하게 합니다.
    return Vector((sign * (.647 + length * t), 1.085 + offset * (1 - .10 * t), .006 + .022 * t * t))  # 손끝을 손바닥 쪽으로 살짝 굽혀 곧고 날카롭게 뻗은 손 모양을 피합니다.


def asset(name, position=(0, 0, 0), turn=0):  # 개별 FBX로 저장할 하나의 모델을 시작합니다.
    global ACTIVE_ROOT  # 이어서 만드는 부위를 같은 모델 아래에 묶습니다.
    root = bpy.data.objects.new(name, None)  # 게임 코드가 없는 모델 전용 루트입니다.
    bpy.context.scene.collection.objects.link(root)  # 편집 원본 장면에 루트를 등록합니다.
    root['export_name'] = name  # FBX에도 같은 모델 이름을 사용합니다.
    ASSETS.append((root, position, turn))  # 원본 조립에 사용할 배치와 회전을 함께 보관합니다.
    ACTIVE_ROOT = root  # 이후 제작하는 메시가 현재 모델에 속하게 합니다.
    return root  # 골격과 부품을 연결할 모델 루트를 반환합니다.


def apply_modifier(obj, modifier):  # 모델링 연산을 저장할 실제 메시 정점에 반영합니다.
    bpy.ops.object.select_all(action='DESELECT')  # 다른 모델이 편집 연산에 섞이지 않게 합니다.
    obj.select_set(True)  # 현재 부위만 선택합니다.
    bpy.context.view_layer.objects.active = obj  # 모델링 명령의 대상을 지정합니다.
    bpy.ops.object.modifier_apply(modifier=modifier.name)  # 게임 실행 중 생성 코드 없이 사용할 수 있는 표면으로 만듭니다.


def mesh(name, vertices, faces, material, smooth=True, subdivision=0):  # 설계한 단면과 면 연결을 실제 편집 가능한 메시로 만듭니다.
    data = bpy.data.meshes.new(ACTIVE_ROOT.name + '_' + name)  # 부위마다 독립적인 메시 데이터를 만듭니다.
    data.from_pydata([bp(point) for point in vertices], [], faces)  # 설계 좌표의 정점을 Blender 표면으로 작성합니다.
    data.materials.append(MATERIALS[material])  # 부위의 단색 재질을 연결합니다.
    edit = bmesh.new()  # 연결된 면의 방향을 정리할 편집 버퍼입니다.
    edit.from_mesh(data)  # 직접 작성한 면을 편집 버퍼로 가져옵니다.
    bmesh.ops.recalc_face_normals(edit, faces=list(edit.faces))  # 닫힌 표면의 면 방향을 일치시킵니다.
    edit.to_mesh(data)  # 정리한 표면을 실제 메시로 저장합니다.
    edit.free()  # 부위 편집이 끝난 버퍼를 해제합니다.
    obj = bpy.data.objects.new(ACTIVE_ROOT.name + '_' + name, data)  # 다른 모델의 동일한 부위 이름과 구분합니다.
    bpy.context.scene.collection.objects.link(obj)  # 편집 원본에 부위를 등록합니다.
    obj.parent, obj['export_name'] = ACTIVE_ROOT, name  # 모델 전체가 하나의 계층으로 이동하게 합니다.
    for face in data.polygons:  # 큰 색면에서도 곡면이 부드럽게 읽히게 합니다.
        face.use_smooth = smooth  # 도로 표시처럼 평평한 부위는 요청한 면 표현을 유지합니다.
    if subdivision:  # 몸통과 유기적인 소품에만 표면 다듬기를 적용합니다.
        modifier = obj.modifiers.new('곡면 다듬기', 'SUBSURF')  # 실제로 저장할 매끄러운 표면을 구성합니다.
        modifier.levels = subdivision  # 필요한 만큼만 표면의 정점 수를 늘립니다.
        apply_modifier(obj, modifier)  # FBX에도 완성된 곡면을 저장합니다.
    return obj  # 리깅과 표정 데이터를 같은 표면에 연결합니다.


def rounded_box(name, center, size, material, radius=.08):  # 모서리 폭을 설계한 건물과 단단한 소품 부위를 만듭니다.
    cx, cy, cz = center  # 부위의 실제 중심입니다.
    sx, sy, sz = (value * .5 for value in size)  # 폭과 높이를 정점 반경으로 바꿉니다.
    vertices = [(cx + x * sx, cy + y * sy, cz + z * sz) for x, y, z in ((-1, -1, -1), (1, -1, -1), (1, 1, -1), (-1, 1, -1), (-1, -1, 1), (1, -1, 1), (1, 1, 1), (-1, 1, 1))]  # 실제 크기가 반영된 표면 정점입니다.
    faces = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (3, 7, 6, 2), (0, 4, 7, 3), (1, 2, 6, 5)]  # 면이 연결된 닫힌 입체를 구성합니다.
    obj = mesh(name, vertices, faces, material)  # Unity 기본 도형에 의존하지 않는 실제 메시입니다.
    modifier = obj.modifiers.new('둥근 가장자리', 'BEVEL')  # 모서리에 실제 곡면을 추가합니다.
    modifier.width, modifier.segments = min(radius, min(size) * .12, .035), 2  # 물체의 평면과 모서리를 남기는 작은 가공 폭만 적용합니다.
    apply_modifier(obj, modifier)  # 둥근 모서리를 실제 정점에 반영합니다.
    normal = obj.modifiers.new('평면과 곡면의 법선', 'WEIGHTED_NORMAL')  # 넓은 면은 평평하게, 가장자리는 부드럽게 표현합니다.
    normal.keep_sharp = True  # 건물의 큰 면 방향을 유지합니다.
    apply_modifier(obj, normal)  # FBX에서도 같은 표면 법선을 사용하게 합니다.
    return obj  # 개별 부위로 움직일 수 있는 모델을 반환합니다.


def loft(name, profile, material, axis='Y', segments=40, subdivision=1, power=1.0):  # 신체와 의상에 맞는 원형 또는 납작한 단면을 이어 만듭니다.
    vertices, faces = [], []  # 단면 정점과 표면 연결을 순서대로 모읍니다.
    for distance, width, depth, shift_a, shift_b in profile:  # 단면마다 실루엣을 달리 설계합니다.
        for index in range(segments):  # 각 단면에 같은 수의 정점을 배치합니다.
            angle = math.tau * index / segments  # 해당 정점의 둘레 방향입니다.
            cosine, sine = math.cos(angle), math.sin(angle)  # 단면의 두 방향 성분입니다.
            a = shift_a + max(width, .0001) * math.copysign(abs(cosine) ** power, cosine)  # 의상에는 넓은 앞면과 좁은 옆면이 있는 단면을 사용합니다.
            b = shift_b + max(depth, .0001) * math.copysign(abs(sine) ** power, sine)  # 같은 타원 표면이 모든 부위에 반복되지 않게 합니다.
            vertices.append((a, distance, b) if axis == 'Y' else ((distance, a, b) if axis == 'X' else (a, b, distance)))  # 몸통과 차량 및 구름을 각자의 긴 방향으로 연결합니다.
    for row in range(len(profile) - 1):  # 인접한 단면 사이에 연속 표면을 작성합니다.
        for index in range(segments):  # 둘레의 마지막 면도 첫 정점으로 연결합니다.
            following = (index + 1) % segments  # 단면의 이음새를 닫습니다.
            faces.append((row * segments + index, (row + 1) * segments + index, (row + 1) * segments + following, row * segments + following))  # 네 정점이 이어지는 곡면입니다.
    faces.extend([tuple(reversed(range(segments))), tuple((len(profile) - 1) * segments + index for index in range(segments))])  # 양끝을 막아 실제 부피를 가진 표면을 만듭니다.
    return mesh(name, vertices, faces, material, subdivision=subdivision)  # 표면을 다듬어 매끄러운 실루엣으로 저장합니다.


def oval(name, center, radii, material, segments=32, rings=14):  # 눈과 작은 유기적인 부위를 타원 표면으로 제작합니다.
    cx, cy, cz = center  # 타원의 중심 위치입니다.
    rx, ry, rz = radii  # 가로와 세로 및 앞뒤 반경입니다.
    profile = [(cy + ry * math.cos(math.pi * row / rings), rx * math.sin(math.pi * row / rings), rz * math.sin(math.pi * row / rings), cx, cz) for row in range(rings + 1)]  # 둘레를 따라 반경을 달리해 닫힌 표면을 구성합니다.
    return loft(name, profile, material, segments=segments, subdivision=0)  # 작은 부위는 추가 세분화 없이 충분한 곡면을 사용합니다.


def tube(name, points, radii, material, segments=12, subdivision=1):  # 팔다리와 휘어진 소품을 하나로 이어진 표면으로 제작합니다.
    vertices, faces = [], []  # 중심 곡선을 따라 작성할 정점과 면입니다.
    centers = [Vector(point) for point in points]  # 방향 계산에 사용할 중심점입니다.
    for row, center in enumerate(centers):  # 중심점마다 곡선에 수직인 단면을 만듭니다.
        direction = (centers[min(row + 1, len(centers) - 1)] - centers[max(row - 1, 0)]).normalized()  # 인접 구간을 함께 사용해 자연스러운 방향을 얻습니다.
        reference = Vector((0, 0, 1)) if abs(direction.z) < .9 else Vector((0, 1, 0))  # 중심선과 평행하지 않은 기준축을 선택합니다.
        normal = direction.cross(reference).normalized()  # 첫 번째 단면 방향입니다.
        tangent = direction.cross(normal).normalized()  # 단면의 수직 방향입니다.
        for index in range(segments):  # 중심선 둘레를 균일하게 나눕니다.
            angle = math.tau * index / segments  # 해당 둘레 정점의 각도입니다.
            vertices.append(tuple(center + radii[row] * (normal * math.cos(angle) + tangent * math.sin(angle))))  # 구간마다 굵기가 달라지는 곡면입니다.
    for row in range(len(centers) - 1):  # 앞뒤 단면을 연결해 표면을 만듭니다.
        for index in range(segments):  # 마지막 면도 닫힌 둘레로 이어집니다.
            following = (index + 1) % segments  # 단면의 다음 정점입니다.
            faces.append((row * segments + index, row * segments + following, (row + 1) * segments + following, (row + 1) * segments + index))  # 중심선 전체를 연속된 사각 면으로 연결합니다.
    faces.extend([tuple(reversed(range(segments))), tuple((len(centers) - 1) * segments + index for index in range(segments))])  # 부위의 양끝을 막습니다.
    return mesh(name, vertices, faces, material, subdivision=subdivision)  # 곡선의 실루엣을 실제 메시로 저장합니다.


def extrude_outline(name, outline, depth, material, bevel=.04):  # 지붕과 별처럼 고유한 외곽선을 가진 입체를 만듭니다.
    vertices = [(x, y, z) for z in (-depth * .5, depth * .5) for x, y in outline]  # 앞뒤에 동일한 외곽선을 작성합니다.
    count = len(outline)  # 외곽선을 구성하는 정점 수입니다.
    faces = [tuple(reversed(range(count))), tuple(index + count for index in range(count))]  # 앞뒤 면을 채웁니다.
    faces.extend((index, (index + 1) % count, (index + 1) % count + count, index + count) for index in range(count))  # 외곽선을 따라 옆면을 연결합니다.
    obj = mesh(name, vertices, faces, material)  # 독립적인 부피를 가진 메시입니다.
    modifier = obj.modifiers.new('외곽선 곡면', 'BEVEL')  # 가장자리가 다른 모델과 같은 둥근 스타일을 갖게 합니다.
    modifier.width, modifier.segments = bevel, 3  # 부위 크기에 맞는 모서리 폭을 사용합니다.
    apply_modifier(obj, modifier)  # 내보낼 정점에 완성 곡면을 저장합니다.
    return obj  # 건물이나 효과의 부품으로 배치할 수 있게 합니다.


def bone_data():  # 기존 관절 이름과 부모 관계를 유지하고 SD 몸에 맞는 길이로 제작합니다.
    bones = []  # 부모부터 자식 순서로 관절을 저장합니다.
    def add(name, parent, head, tail):  # 실제 뼈 구간을 계층 정보와 함께 기록합니다.
        bones.append((name, parent, Vector(head), Vector(tail)))  # 동일한 정보로 골격과 웨이트를 연결합니다.
    add('Hips', None, (0, .74, 0), (0, .84, 0))  # 기존 보행의 골반 높이입니다.
    add('Spine', 'Hips', (0, .84, 0), (0, .97, 0))  # 낮은 몸통의 굽힘 기준입니다.
    add('Chest', 'Spine', (0, .97, 0), (0, 1.065, 0))  # 몸통 중심 회전의 기준입니다.
    add('UpperChest', 'Chest', (0, 1.065, 0), (0, 1.135, 0))  # 휴대전화 자세와 인사가 사용하는 상체 관절입니다.
    add('Neck', 'UpperChest', (0, 1.135, 0), (0, 1.2, 0))  # 얼굴 쪽으로 이어지는 고개 전환 관절입니다.
    add('Head', 'Neck', (0, 1.2, 0), (0, 1.43, 0))  # 별도 두상과 이목구비 및 머리카락을 함께 움직입니다.
    add('Jaw', 'Head', (0, 1.24, .05), (0, 1.20, .09))  # 기존 골격의 턱 관절 이름을 유지합니다.
    for side, sign in (('Left', -1), ('Right', 1)):  # 좌우 뼈를 같은 비율로 구성합니다.
        add(side + 'Eye', 'Head', (sign * .047, 1.307, .09), (sign * .047, 1.307, .13))  # 기존 눈 반응이 참조하는 관절입니다.
        add(side + 'Shoulder', 'UpperChest', (sign * .09, 1.095, 0), (sign * .195, 1.085, 0))  # 몸통과 가느다란 팔을 연결합니다.
        add(side + 'UpperArm', side + 'Shoulder', (sign * .195, 1.085, 0), (sign * .41, 1.085, 0))  # 기존 팔 들기 자세의 위팔 기준입니다.
        add(side + 'LowerArm', side + 'UpperArm', (sign * .41, 1.085, 0), (sign * .586, 1.085, 0))  # 기존 팔꿈치와 손목 길이를 유지합니다.
        add(side + 'Hand', side + 'LowerArm', (sign * .586, 1.085, 0), (sign * .65, 1.085, 0))  # 휴대전화를 부착할 손목 관절입니다.
        for finger, dy, length, _ in HAND_FINGERS:  # 실제로 제작하는 짧은 손가락과 같은 위치와 길이의 관절을 사용합니다.
            parent = side + 'Hand'  # 각 손가락을 손목 아래에서 시작합니다.
            for index, section in enumerate(('Proximal', 'Intermediate', 'Distal')):  # 표준 세 마디를 구성합니다.
                name = side + finger + section  # 게임 스크립트와 같은 관절 이름입니다.
                add(name, parent, finger_point(sign, dy, length, index / 3), finger_point(sign, dy, length, (index + 1) / 3))  # 새 손가락의 굽은 중심선을 따라 기존 이름의 세 마디를 배치합니다.
                parent = name  # 다음 마디를 앞 마디에 연결합니다.
        thumb = [(sign * x, y, z) for x, y, z in THUMB_POINTS]  # 도톰한 엄지 표면 안에 실제 변형 관절이 놓이게 합니다.
        parent = side + 'Hand'  # 엄지는 손목 아래에서 시작합니다.
        for index, section in enumerate(('Proximal', 'Intermediate', 'Distal')):  # 기존 엄지 마디 이름을 유지합니다.
            name = side + 'Thumb' + section  # 터치 자세와 연결 가능한 관절입니다.
            add(name, parent, thumb[index], thumb[index + 1])  # 실제 엄지 방향과 마디 길이입니다.
            parent = name  # 엄지 마디를 순서대로 연결합니다.
        add(side + 'UpperLeg', 'Hips', (sign * .081, .72, 0), (sign * .081, .41, .014))  # 기존 걸음의 허벅지 기준입니다.
        add(side + 'LowerLeg', side + 'UpperLeg', (sign * .081, .41, .014), (sign * .081, .115, 0))  # 무릎부터 발목까지 이어집니다.
        add(side + 'Foot', side + 'LowerLeg', (sign * .081, .115, 0), (sign * .081, .045, .12))  # 기존 발 회전과 접지 높이를 유지합니다.
        add(side + 'Toes', side + 'Foot', (sign * .081, .045, .12), (sign * .081, .04, .17))  # 발끝에 대응하는 마지막 관절입니다.
    result = []  # 작은 몸과 큰 머리에 맞춘 실제 바인드 관절입니다.
    for name, parent, head, tail in bones:  # 관절 이름과 부모 관계를 유지하면서 길이만 몸에 맞춥니다.
        head = body_point(head)  # 실제 피부와 동일하게 어깨와 팔 및 손가락 관절의 위치를 줄입니다.
        tail = body_point(tail)  # 팔꿈치와 손목이 짧아진 메시의 관절 위치에 놓이게 합니다.
        if name == 'Head':  # 큰 SD 두상을 실제 목 위에 연결합니다.
            head, tail = Vector((0, 1.2 * BODY_SCALE.y, 0)), Vector((0, 1.53 - HEAD_DROP, 0))  # 짧아진 목과 낮아진 큰 두상을 같은 머리 관절로 연결합니다.
        elif name.endswith('Eye'):  # 납작한 그래픽 눈과 관절 중심을 같은 높이에 둡니다.
            sign = -1 if name.startswith('Left') else 1  # 현재 눈의 좌우 방향입니다.
            head, tail = Vector((sign * .118, 1.175 - HEAD_DROP, .20)), Vector((sign * .118, 1.175 - HEAD_DROP, .25))  # 커진 눈의 중심과 눈 관절을 같은 위치로 맞춥니다.
        result.append((name, parent, head, tail))  # 기존 이름과 계층을 가진 SD 골격을 저장합니다.
    return result  # 실제 작은 몸에 맞는 골격으로 두 친구를 제작합니다.


def blend_weights(value, keys):  # 이웃한 두 관절의 정점 영향을 부드럽게 잇습니다.
    if value <= keys[0][0]:  # 첫 기준 아래의 정점입니다.
        return {keys[0][1]: 1.0}  # 첫 관절의 영향을 받습니다.
    for (low, first), (high, second) in zip(keys, keys[1:]):  # 정점이 속한 관절 구간을 찾습니다.
        if value <= high:  # 해당 두 관절 사이의 표면입니다.
            weight = (value - low) / (high - low)  # 구간 안의 상대 위치입니다.
            return {first: 1.0 - weight, second: weight}  # 영향 합계를 1로 유지합니다.
    return {keys[-1][1]: 1.0}  # 마지막 관절 위의 정점도 골격을 따릅니다.


def skin_character(parts, root, rig=None):  # 새 모델에는 골격을 만들고 얼굴 교체에서는 기존 골격을 그대로 사용합니다.
    bones = bone_data()  # 현재 게임의 이름과 길이를 유지하는 관절 규격입니다.
    if rig is None:  # 전체 모델 제작에서만 새 골격을 만듭니다.
        visual = bpy.data.objects.new(root.name + '_VisualRoot', None)  # 기존 Animator가 기준으로 삼는 시각 계층입니다.
        bpy.context.scene.collection.objects.link(visual)  # 모델에 시각 루트를 등록합니다.
        visual.parent, visual['export_name'] = root, 'VisualRoot'  # 게임 이동 루트와 뼈대를 구분합니다.
        data = bpy.data.armatures.new(root.name + '_Skeleton')  # 각 친구의 독립적인 골격 데이터입니다.
        rig = bpy.data.objects.new(root.name + '_Armature', data)  # 원본에서 두 친구의 골격을 구분합니다.
        bpy.context.scene.collection.objects.link(rig)  # 골격을 원본 장면에 연결합니다.
        rig.parent, rig['export_name'] = visual, 'Armature'  # 기존 Armature/Hips 경로를 유지합니다.
        bpy.ops.object.select_all(action='DESELECT')  # 메시가 관절 편집에 섞이지 않게 합니다.
        rig.select_set(True)  # 현재 친구의 골격만 선택합니다.
        bpy.context.view_layer.objects.active = rig  # 뼈를 작성할 대상을 지정합니다.
        bpy.ops.object.mode_set(mode='EDIT')  # 실제 변형용 관절을 구성합니다.
        for name, parent, head, tail in bones:  # 부모부터 자식 순서로 뼈를 만듭니다.
            bone = data.edit_bones.new(name)  # 기존 동작 코드와 같은 이름입니다.
            bone.head, bone.tail = bp(head), bp(tail)  # 메시와 동일한 제작 좌표계입니다.
            if parent:  # 골반 이외의 관절은 부모를 따릅니다.
                bone.parent = data.edit_bones[parent]  # 기존 뼈대 계층을 유지합니다.
        bpy.ops.object.mode_set(mode='OBJECT')  # 완성한 골격에 스킨을 연결합니다.
    for obj, family in parts:  # 몸통과 팔다리 및 얼굴에 맞는 스킨 기준을 적용합니다.
        sleeve_group = obj.vertex_groups.get('상의_소매_연결')  # 몸판과 실제 소매 사이의 연결 구간에만 적용할 변형 비율을 가져옵니다.
        groups = {name: obj.vertex_groups.new(name=name) for name, _, _, _ in bones}  # 관절 이름별 웨이트 저장 공간입니다.
        for vertex in obj.data.vertices:  # 표면의 실제 위치로 관절 영향을 정합니다.
            x, y, z = vertex.co.x, vertex.co.z, -vertex.co.y  # 피부와 의상 정점의 실제 설계 좌표입니다.
            model_point = Vector((x, y, z))  # 실제 SD 몸 위의 정점 위치입니다.
            if family in ('Top', 'Pants', 'Shorts', 'Arm', 'Leg') or family.endswith('Palm'):  # 몸과 의상의 관절 구간은 축소 전 제작 좌표에서 판정합니다.
                x, y, z = x / BODY_SCALE.x, y / BODY_SCALE.y, z / BODY_SCALE.z  # 축소된 몸에서 웨이트 구간이 엉뚱한 부위로 이동하지 않게 합니다.
                if y > .95 and abs(x) > .17:  # 짧아진 팔의 정점을 기존 어깨와 팔꿈치 구간으로 되돌립니다.
                    x = math.copysign(.17 + (abs(x) - .17) / ARM_LENGTH, x)  # 길이 압축 때문에 팔 피부가 다른 관절을 따르지 않게 합니다.
            side = 'Left' if x < 0 else 'Right'  # 좌우 신체를 구분합니다.
            if family == 'Top':  # 연결된 상의와 소매는 척추 또는 팔을 따릅니다.
                values = blend_weights(y, ((.73, 'Hips'), (.84, 'Spine'), (.97, 'Chest'), (1.065, 'UpperChest')))  # 몸판은 가로 위치에 관계없이 몸통 뼈가 지지하여 위팔 회전에 끌려 올라가지 않게 합니다.
                sleeve_blend = next((entry.weight for entry in vertex.groups if sleeve_group is not None and entry.group == sleeve_group.index), 0.0)  # 공유 정점에서 소매 쪽으로 이어지는 실제 연결 구간의 비율입니다.
                if sleeve_blend > .00001:  # 소매와 어깨 연결부에 속하는 정점에만 팔 변형을 섞습니다.
                    arm_values = blend_weights(abs(x), ((.195, side + 'UpperArm'), (.41, side + 'LowerArm'), (.586, side + 'Hand')))  # 위팔의 회전 중심부터 팔꿈치와 손목까지 기존 관절을 따릅니다.
                    values = {name: values.get(name, 0.0) * (1 - sleeve_blend) + arm_values.get(name, 0.0) * sleeve_blend for name in values.keys() | arm_values.keys()}  # 몸판 경계의 영향은 유지하면서 연결부를 지나 소매에만 팔의 영향을 이어 줍니다.
            elif family == 'Pants':  # 바지의 골반과 두 다리통을 구분합니다.
                values = {'Hips': 1.0} if y > .67 else blend_weights(y, ((.115, side + 'Foot'), (.41, side + 'LowerLeg'), (.64, side + 'UpperLeg')))  # 바지통이 무릎과 발목을 따라 움직입니다.
            elif family == 'Shorts':  # 무릎 위에서 끝나는 반바지는 허벅지와 골반을 따라 움직입니다.
                values = blend_weights(y, ((.62, side + 'UpperLeg'), (.74, 'Hips')))  # 반바지 밑단이 종아리 뼈에 끌려가지 않도록 허벅지에 연결합니다.
            elif family.endswith('Palm'):  # 손바닥과 다섯 손가락의 연결된 피부입니다.
                hand_side = family[:-4]  # 현재 손의 좌우 관절 이름입니다.
                if abs(x) < .635 or (abs(x) < .659 and y > 1.042):  # 넓은 손바닥 중심은 손목이 지지하고 바깥 엄지와 짧은 손끝만 각 손가락을 따르게 합니다.
                    values = {hand_side + 'Hand': 1.0}  # 손목 회전에서 손바닥의 형태를 유지합니다.
                else:  # 손가락 표면은 가장 가까운 마디를 따릅니다.
                    distances = []  # 표면과 실제 손가락 뼈 구간의 거리를 보관합니다.
                    point = model_point  # 손가락 뼈와 같은 실제 SD 좌표에서 거리를 구합니다.
                    for bone_name, _, head, tail in bones:  # 같은 손의 손가락만 후보로 사용합니다.
                        if bone_name.startswith(hand_side) and any(finger in bone_name for finger in ('Thumb', 'Index', 'Middle', 'Ring', 'Little')):  # 다른 신체 부위는 제외합니다.
                            direction = tail - head  # 실제 마디 구간입니다.
                            t = max(0, min(1, (point - head).dot(direction) / direction.length_squared))  # 마디에서 가장 가까운 위치입니다.
                            distances.append(((point - head - direction * t).length, bone_name))  # 정점과 마디의 대응 거리를 저장합니다.
                    nearest = [(1 / max(.002, distance) ** 3, bone_name) for distance, bone_name in sorted(distances)[:2]]  # 인접한 두 마디에 영향을 나눕니다.
                    total = sum(weight for weight, _ in nearest)  # 웨이트 정규화 기준입니다.
                    values = {bone_name: weight / total for weight, bone_name in nearest}  # 손가락을 접어도 피부 연결을 유지합니다.
            elif family == 'Arm':  # 팔은 어깨부터 손목까지 영향을 이어 줍니다.
                values = blend_weights(abs(x), ((.19, side + 'Shoulder'), (.25, side + 'UpperArm'), (.41, side + 'LowerArm'), (.586, side + 'Hand')))  # 기존 팔꿈치 위치에 맞게 변형합니다.
            elif family == 'Leg':  # 얇은 다리도 기존 보행 관절을 따릅니다.
                values = blend_weights(y, ((.115, side + 'Foot'), (.41, side + 'LowerLeg'), (.66, side + 'UpperLeg'), (.72, 'Hips')))  # 무릎과 발목의 영향을 부드럽게 잇습니다.
            else:  # 얼굴과 손발처럼 특정 관절에 붙는 부위입니다.
                values = {family: 1.0}  # 고개와 손목을 돌려도 부위가 떨어지지 않게 합니다.
            for name, weight in values.items():  # 해당 정점의 실제 골격 영향을 기록합니다.
                if weight > .00001:  # 값이 없는 스킨 항목은 만들지 않습니다.
                    groups[name].add([vertex.index], weight, 'REPLACE')  # 정점을 실제 변형용 뼈에 연결합니다.
        if sleeve_group is not None:  # 소매 연결 정보가 실제 뼈 웨이트로 반영된 이후입니다.
            obj.vertex_groups.remove(sleeve_group)  # 제작용 연결 그룹을 제거하여 저장 모델에는 실제 관절 웨이트만 남깁니다.
        modifier = obj.modifiers.new('캐릭터 골격 변형', 'ARMATURE')  # 관절에 따라 휘어지는 스킨 메시로 구성합니다.
        modifier.object, obj.parent = rig, rig  # 표면과 골격이 같은 모델을 따릅니다.
    rig.show_in_front = False  # 얼굴과 몸을 보는 동안 내부 뼈가 피부 위로 겹쳐 표시되지 않게 합니다.
    return rig  # Blender 원본의 대기 자세에도 같은 골격을 사용합니다.


def merge_surface(name, objects, material, voxel=.003):  # 피부와 의상 연결부를 실제로 이어진 하나의 표면으로 만듭니다.
    bpy.ops.object.select_all(action='DESELECT')  # 현재 연결할 부위만 모델링 대상으로 지정합니다.
    for obj in objects:  # 머리와 귀 또는 몸판과 소매를 함께 선택합니다.
        obj.select_set(True)  # 겹친 표면을 하나의 메시로 정리합니다.
    bpy.context.view_layer.objects.active = objects[0]  # 첫 부위의 모델 루트 연결을 유지합니다.
    bpy.ops.object.join()  # 정점을 한 메시 안에 모읍니다.
    obj = bpy.context.object  # 연결된 부위를 편집할 대상입니다.
    obj.name, obj['export_name'] = ACTIVE_ROOT.name + '_' + name, name  # 완성 부위의 의미가 드러나는 이름입니다.
    remesh = obj.modifiers.new('피부와 의상 연결', 'REMESH')  # 겹친 연결부를 실제로 이어진 피부나 옷으로 만듭니다.
    remesh.mode, remesh.voxel_size, remesh.use_smooth_shade = 'VOXEL', voxel, True  # 작은 연결부를 유지할 표면 간격입니다.
    apply_modifier(obj, remesh)  # 연결된 표면을 실제 메시로 저장합니다.
    smooth = obj.modifiers.new('연결부 정리', 'SMOOTH')  # 접합부의 작은 격자 흔적만 정리합니다.
    smooth.factor, smooth.iterations = .22, 2  # 의상과 얼굴의 설계 윤곽을 유지하는 범위입니다.
    apply_modifier(obj, smooth)  # 정리된 연결부를 FBX에도 포함합니다.
    obj.data.materials.clear()  # 연결 전 생긴 중복 재질 슬롯을 제거합니다.
    obj.data.materials.append(MATERIALS[material])  # 완성 부위가 하나의 피부 또는 의상 재질을 사용합니다.
    for face in obj.data.polygons:  # 실제 표면에 동일한 재질을 지정합니다.
        face.material_index, face.use_smooth = 0, True  # 매끈한 피부와 의상을 같은 슬롯에 연결합니다.
    return obj  # 이후 골격 웨이트를 완성 표면에 부여합니다.


HEAD_PROFILE = [(.93, .060, .080, .070), (.955, .135, .135, .110), (.995, .216, .185, .170), (1.055, .262, .215, .220), (1.13, .280, .230, .250), (1.22, .279, .225, .260), (1.32, .267, .210, .255), (1.42, .227, .180, .225), (1.51, .125, .110, .130), (1.55, .025, .025, .030)]  # 참고 이미지처럼 몸보다 크고 아래 얼굴이 짧은 SD 두상입니다.


def head_section(y):  # 얼굴 높이의 실제 두상 폭과 앞뒤 깊이를 구합니다.
    y = max(HEAD_PROFILE[0][0], min(HEAD_PROFILE[-1][0], y))  # 정수리와 턱의 끝 구간을 유지합니다.
    for first, second in zip(HEAD_PROFILE, HEAD_PROFILE[1:]):  # 해당 높이가 속한 단면 구간입니다.
        if first[0] <= y <= second[0]:  # 얼굴 표면의 이웃한 두 단면입니다.
            t = (y - first[0]) / (second[0] - first[0])  # 두 단면의 보간 비율입니다.
            return tuple(a + (b - a) * t for a, b in zip(first[1:], second[1:]))  # 폭과 앞면 및 뒤통수 깊이입니다.
    return HEAD_PROFILE[-1][1:]  # 정수리 끝도 닫힌 표면으로 유지합니다.


def face_depth(x, y):  # 얼굴 그림을 피부 표면에 밀착시킬 위치를 계산합니다.
    width, front, _ = head_section(y)  # 해당 높이의 얼굴 단면입니다.
    depth = front * max(0, 1 - (x / width) ** 2) ** .29  # 얼굴 앞면은 넓고 납작하게 설계합니다.
    depth += .002 * math.exp(-((abs(x) - .16) / .07) ** 2 - ((y - 1.08) / .065) ** 2)  # 돌출된 코 없이 단순한 볼 곡률만 얼굴에 남깁니다.
    return depth  # 눈과 입을 별도 입체 덩어리로 띄우지 않습니다.


def face_patch(name, outline, material, offset=.003, projector=None):  # 얼굴과 앞머리의 얇은 색면을 각자의 실제 표면에 밀착시킵니다.
    if sum(a[0] * b[1] - b[0] * a[1] for a, b in zip(outline, outline[1:] + outline[:1])) < 0:  # 정면을 향하는 면 방향입니다.
        outline = list(reversed(outline))  # 얼굴 그림이 뒤집혀 보이지 않게 합니다.
    center = (sum(x for x, _ in outline) / len(outline), sum(y for _, y in outline) / len(outline))  # 피부 곡률을 따라갈 가운데 정점입니다.
    points = [center] + outline  # 넓은 색면도 피부 안쪽으로 파묻히지 않게 나눕니다.
    depth_at = projector or face_depth  # 얼굴과 머리카락의 서로 다른 표면 깊이를 사용합니다.
    vertices = [(x, y, depth_at(x, y) + offset) for x, y in points]  # 눈과 앞머리가 해당 표면 안으로 파묻히지 않게 합니다.
    faces = [(0, index + 1, (index + 1) % len(outline) + 1) for index in range(len(outline))]  # 중심에서 외곽까지 피부에 붙는 삼각형 색면입니다.
    return mesh(name, vertices, faces, material, smooth=False)  # 눈과 입에는 구체나 튜브 두께를 추가하지 않습니다.


def face_ribbon(name, points, width, material, offset=.003, projector=None):  # 눈썹과 미소를 완성된 피부에 밀착하는 얇은 선으로 만듭니다.
    depth_at = projector or face_depth  # 실제 피부 표면이 준비되면 해당 표면을 기준으로 사용합니다.
    vertices = [(x, y + sign * width * .5, depth_at(x, y + sign * width * .5) + offset) for x, y in points for sign in (-1, 1)]  # 선의 위아래 가장자리가 피부에서 떠 있지 않게 합니다.
    faces = [(index * 2, index * 2 + 2, index * 2 + 3, index * 2 + 1) for index in range(len(points) - 1)]  # 튜브 대신 납작한 띠로 선을 표현합니다.
    return mesh(name, vertices, faces, material, smooth=False)  # 표정의 선이 얼굴에서 돌출되지 않게 합니다.


def add_eye_expressions(eye, sign, skin_depth, head_drop=0.):  # 열린 눈과 별도로 놀람과 눈웃음의 실제 윤곽을 저장합니다.
    cx, cy = sign * .118, 1.175  # 기본 눈과 같은 중심에서 두 표정이 바뀌도록 맞춥니다.
    basis = eye.data.shape_keys.reference_key.data  # 접히거나 웃는 다른 키가 섞이지 않은 원본 정점을 읽습니다.
    for name in ('SurpriseEyes', 'SmileEyes'):  # 놀랄 때와 웃을 때 서로 다른 선을 사용합니다.
        key = eye.data.shape_keys.key_blocks.get(name) or eye.shape_key_add(name=name, from_mix=False)  # 같은 모델을 다시 적용해도 표정 키를 중복 생성하지 않습니다.
        key.value = 0.  # 평소 얼굴에서는 원래 열린 눈을 유지합니다.
        for vertex, original in zip(key.data, basis):  # 기존 눈의 삼각형 연결을 보존하며 각 정점을 새 윤곽으로 옮깁니다.
            u = (original.co.x - cx) / .037  # 원래 눈의 가로 위치를 중심 기준으로 정규화합니다.
            v = (original.co.z + head_drop - cy) / .064  # 저장된 모델에서도 같은 기준으로 세로 위치를 읽습니다.
            if name == 'SurpriseEyes':  # 양쪽 끝이 안쪽으로 접힌 눈을 만들어 놀람을 표현합니다.
                x, y = cx - sign * (.021 - .050 * abs(v)) + .010 * u, cy + .033 * v  # 두 눈의 꺾인 꼭짓점이 코 쪽을 향하게 합니다.
            else:  # 성공 표정의 눈은 양끝보다 가운데가 올라가게 합니다.
                x, y = cx + .046 * u, cy + .025 * (1. - abs(u)) + .008 * v  # 얇은 위쪽 눈웃음이 얼굴 폭을 따라 보이게 합니다.
            vertex.co = bp((x, y - head_drop, skin_depth(x, y) + .003))  # 얼굴 앞면을 따라 새 눈 모양을 밀착시킵니다.


def add_smiling_mouth(mouth, name, skin_depth, head_drop=0.):  # 감사할 때 열린 미소와 다문 미소를 모두 사용합니다.
    tongue = name == 'SmileTongue'  # 입 안쪽 색면은 입의 실루엣 안에서만 움직입니다.
    radius_x, radius_y, base_y = (.033, .010, 1.030) if tongue else (.055, .029, 1.045)  # 기존 기본 입의 정점 위치를 같은 기준으로 읽습니다.
    basis = mouth.data.shape_keys.reference_key.data  # 원래 미소에서 각각 독립적인 두 표정을 만듭니다.
    for expression in ('Happy', 'SmileClosed'):  # 벌어진 웃음과 말 사이에 입을 닫은 웃음을 준비합니다.
        key = mouth.data.shape_keys.key_blocks.get(expression) or mouth.shape_key_add(name=expression, from_mix=False)  # 기존 웃음 키는 새 입 모양으로 갱신합니다.
        key.value = 0.  # 기본 표정에는 감사 표정이 섞이지 않게 합니다.
        closed = expression == 'SmileClosed'  # 입을 다문 순간에는 안쪽 분홍 면을 접습니다.
        width, height = ((0., 0.) if closed else (.034, .008)) if tongue else (.066, .004 if closed else .026)  # 웃는 입꼬리의 가로 폭은 열고 닫을 때 유지합니다.
        center_y = 1.045 if closed or not tongue else 1.032  # 열린 입의 안쪽만 아래에 배치합니다.
        for vertex, original in zip(key.data, basis):  # 기존 입과 같은 정점 순서에서 연속적으로 움직이도록 저장합니다.
            u = original.co.x / radius_x  # 입 가운데와 양쪽 끝을 같은 비율로 구분합니다.
            v = (original.co.z + head_drop - base_y) / radius_y  # 기본 입의 높이를 새 웃음 높이로 옮깁니다.
            x = width * u  # 말하는 동안에도 넓게 웃는 입의 가로 폭을 유지합니다.
            curve = .011 * (x / .066) ** 2 if tongue else .028 * u ** 2  # 입술 양끝은 크게 올리고 안쪽 색면에는 작은 곡률만 줍니다.
            y = center_y + curve + height * v  # 위로 올라간 입꼬리에 현재 입의 세로 벌어짐을 더합니다.
            vertex.co = bp((x, y - head_drop, skin_depth(x, y) + (.005 if tongue else .0035)))  # 웃는 입과 안쪽 색면을 실제 피부에 맞춥니다.


def make_head(parts, girl):  # 참고 이미지의 큰 SD 두상과 납작한 검은 눈을 제작합니다.
    head_start = len(parts)  # 얼굴과 헤어에 해당하는 부위만 별도로 표시할 시작 위치입니다.
    vertices, faces = [], []  # 큰 볼과 작은 턱이 이어지는 피부 표면입니다.
    rows, segments = 49, 64  # 표정 투영과 같은 윤곽을 사용하는 실제 피부 단면입니다.
    for row in range(rows):  # 턱에서 정수리까지 넓은 얼굴 면을 이어 만듭니다.
        y = HEAD_PROFILE[0][0] + (HEAD_PROFILE[-1][0] - HEAD_PROFILE[0][0]) * row / (rows - 1)  # 현재 단면의 실제 높이입니다.
        width, _, back = head_section(y)  # 해당 높이의 큰 SD 두상 폭입니다.
        for index in range(segments):  # 두상 둘레를 같은 수의 정점으로 구성합니다.
            angle = math.tau * index / segments  # 정점의 둘레 방향입니다.
            x, cosine = width * math.sin(angle), math.cos(angle)  # 앞면과 뒤통수를 구분합니다.
            vertices.append((x, y, face_depth(x, y) if cosine >= 0 else back * cosine))  # 돌출된 코 없이 넓은 얼굴 면을 만듭니다.
    for row in range(rows - 1):  # 이웃 단면을 실제 피부 면으로 연결합니다.
        for index in range(segments):  # 두상에 끊어진 경계가 없게 합니다.
            faces.append((row * segments + index, row * segments + (index + 1) % segments, (row + 1) * segments + (index + 1) % segments, (row + 1) * segments + index))  # 연속된 피부 표면입니다.
    faces.extend([tuple(reversed(range(segments))), tuple((rows - 1) * segments + index for index in range(segments))])  # 턱과 정수리를 닫습니다.
    ears = [mesh('ChildHead', vertices, faces, 'Skin')]  # 실제 큰 얼굴 윤곽입니다.
    for side, sign in (('Left', -1), ('Right', 1)):  # 눈보다 조금 낮은 위치의 작은 귀입니다.
        profile = [(y, rx, rz, sign * (head_section(y)[0] + .001), 0) for y, rx, rz in ((1.075, .008, .010), (1.09, .022, .022), (1.125, .028, .026), (1.16, .020, .019), (1.175, .005, .007))]  # 넓은 볼과 실제로 연결되는 귀 단면입니다.
        ears.append(loft(side + 'Ear', profile, 'Skin', segments=24))  # 귀와 얼굴이 겹치는 피부입니다.
    skin = merge_surface('HeadSkin', ears, 'Skin', .0025)  # 귀와 볼을 하나로 이어진 실제 피부로 만듭니다.
    parts.append((skin, 'Head'))  # 큰 두상을 새 SD 머리 관절에 연결합니다.
    surface = BVHTree.FromPolygons([tuple(vertex.co) for vertex in skin.data.vertices], [tuple(face.vertices) for face in skin.data.polygons])  # 표정을 실제 피부에 밀착시킬 제작 표면입니다.
    def skin_depth(x, y):  # 피부의 최종 앞면 위치를 사용합니다.
        hit, _, _, _ = surface.ray_cast(Vector((x, -.7, y)), Vector((0, 1, 0)), 1.4)  # 정점을 얼굴 앞면으로 투영합니다.
        return -hit.y if hit is not None else face_depth(x, y)  # 눈과 입이 구체처럼 떠 있지 않게 합니다.
    parts.append((loft('NeckSkin', [(.88, .035, .033, 0, 0), (.94, .039, .036, 0, 0), (1.01, .043, .038, 0, .004)], 'Skin', segments=32), 'Neck'))  # 작은 몸과 큰 턱 사이에 짧은 목을 연결합니다.
    for side, sign in (('Left', -1), ('Right', 1)):  # 각 눈은 한 개의 검은 색면으로만 제작합니다.
        cx, cy = sign * .118, 1.175  # 두 눈을 볼 안쪽에 안정적으로 배치하여 가운데로 몰린 표정을 피합니다.
        outline = [(cx + .037 * math.cos(math.tau * index / 48), cy + .064 * math.sin(math.tau * index / 48)) for index in range(48)]  # 얼굴 폭에 맞는 크고 또렷한 세로 타원 눈을 만듭니다.
        eye = face_patch(side + 'EyeGraphic', outline, 'FaceInk', .0025, skin_depth)  # 흰자와 홍채 및 반짝임을 넣지 않는 그래픽 눈입니다.
        eye.shape_key_add(name='Basis', from_mix=False)  # 다른 표정이 섞이지 않은 열린 눈을 기본 형태로 저장합니다.
        blink = eye.shape_key_add(name='Blink', from_mix=False)  # 기본 열린 눈에서 독립적인 눈 감기 형태를 만듭니다.
        blink.value = 0.0  # 기본 얼굴에서는 눈을 뜨고 기존 눈 감기 동작이 요청될 때만 눈을 접습니다.
        for vertex in blink.data:  # 눈이 접혀도 실제 피부를 따르게 합니다.
            x, y = vertex.co.x, cy + (vertex.co.z - cy) * .07  # 눈의 중심을 유지하며 얇은 선으로 접습니다.
            vertex.co = bp((x, y, skin_depth(x, y) + .0025))  # 피부에서 돌출되지 않는 눈 감기입니다.
        add_eye_expressions(eye, sign, skin_depth)  # 깜빡임과 별개로 꺾인 놀람 눈과 눈웃음을 저장합니다.
        parts.append((eye, side + 'Eye'))  # 실제 눈 위치와 같은 높이의 관절에 연결합니다.
        cheek = [(sign * .181 + .029 * math.cos(math.tau * index / 40), 1.085 + .010 * math.sin(math.tau * index / 40)) for index in range(40)]  # 큰 눈 아래 바깥쪽에 작은 분홍 볼을 배치합니다.
        parts.append((face_patch(side + 'Cheek', cheek, 'Blush', .002, skin_depth), 'Head'))  # 얼굴에 그린 듯한 색면으로 저장합니다.
    for name, material, rx, ry, cy, offset in (('Smile', 'Mouth', .055, .029, 1.045, .0035), ('SmileTongue', 'Blush', .033, .010, 1.030, .005)):  # 점처럼 보이는 입 대신 안쪽 분홍색이 보이는 열린 미소를 만듭니다.
        outline = [(rx * math.copysign(abs(math.cos(math.tau * index / 48)) ** .7, math.cos(math.tau * index / 48)), cy + ry * math.copysign(abs(math.sin(math.tau * index / 48)) ** .7, math.sin(math.tau * index / 48))) for index in range(48)]  # 참고 이미지처럼 짧고 단순한 입의 외곽선입니다.
        mouth = face_patch(name, outline, material, offset, skin_depth)  # 실제 입술이나 치아를 입체로 만들지 않습니다.
        mouth.shape_key_add(name='Basis', from_mix=False)  # 표정이 겹치지 않은 기본 미소를 저장합니다.
        add_smiling_mouth(mouth, name, skin_depth)  # 웃을 때 입꼬리를 올리고 말 사이에도 같은 미소를 유지합니다.
        for expression, width, height in (('Surprise', .035, .043), ('Worried', .043, .004)):  # 둥근 놀람과 짧게 닫힌 입을 다른 표정으로 사용합니다.
            key = mouth.shape_key_add(name=expression, from_mix=False)  # 다른 입 표정의 변형이 누적되지 않은 독립적인 표정을 만듭니다.
            key.value = 0.0  # 웃음과 놀람 및 걱정이 동시에 더해져 얼굴이 뒤틀리지 않게 기본값을 비웁니다.
            if name == 'SmileTongue':  # 입 안쪽 분홍색 면도 표정의 크기를 따릅니다.
                width, height = (width * .60, height * .25) if expression != 'Worried' else (0, 0)  # 입이 닫힐 때 안쪽 색면도 함께 접힙니다.
            center_y = 1.045 if name == 'Smile' else 1.045 - height * 1.5  # 입 안쪽에 머무는 색면의 중심입니다.
            for index, vertex in enumerate(key.data):  # 표정의 중심과 외곽선을 함께 옮깁니다.
                angle = math.tau * (index - 1) / 48  # 같은 정점 순서를 유지합니다.
                x, y = (0, center_y) if index == 0 else (width * math.cos(angle), center_y + height * math.sin(angle))  # 단순한 그래픽 입 모양입니다.
                vertex.co = bp((x, y, skin_depth(x, y) + offset))  # 표정이 바뀌어도 피부에 밀착합니다.
        parts.append((mouth, 'Head'))  # 입을 머리 관절에 연결합니다.
    make_hair(parts, girl)  # 넓은 면으로 나뉜 앞머리와 밝은 헤어 색상을 제작합니다.
    for obj, _ in parts[head_start:]:  # 다음 얼굴 수정에서도 교체 범위를 정확히 유지합니다.
        obj['head_part'] = True  # 몸과 옷을 제외한 실제 얼굴 및 헤어 부위입니다.
        surfaces = [key.data for key in obj.data.shape_keys.key_blocks] if obj.data.shape_keys else [obj.data.vertices]  # 기본 얼굴과 모든 표정의 정점을 같은 거리로 옮깁니다.
        for surface in surfaces:  # 눈 감기와 입 표정도 낮아진 머리 위치를 공유합니다.
            for vertex in surface:  # 귀와 목 및 머리카락까지 하나의 두상 배치를 유지합니다.
                vertex.co.z -= HEAD_DROP  # 짧아진 몸 위로 큰 머리를 내려 목이 길게 드러나지 않게 합니다.
        obj.data.update()  # 실제 메시와 표정에 변경한 머리 위치를 반영합니다.


def make_boy_hair(parts):  # 둥근 두상에 맞는 짧은 옆머리와 자연스럽게 내려오는 앞머리를 제작합니다.
    profile = [(1.075, .220, .160, .200), (1.16, .291, .242, .278), (1.27, .301, .258, .288), (1.38, .287, .243, .272), (1.49, .235, .199, .224), (1.57, .139, .119, .136), (1.61, .002, .002, .002)]  # 정수리를 과하게 높이지 않고 귀 위와 뒷목으로 짧게 내려오는 머리 윤곽입니다.
    def section(y):  # 머리 높이에 따른 실제 가로 폭과 앞뒤 깊이를 이어 줍니다.
        for first, second in zip(profile, profile[1:]):  # 현재 높이가 속한 두 머리 단면입니다.
            if first[0] <= y <= second[0]:  # 옆머리와 정수리 사이의 해당 구간입니다.
                t = (y - first[0]) / (second[0] - first[0])  # 단면 사이의 상대 높이입니다.
                return tuple(a + (b - a) * t for a, b in zip(first[1:], second[1:]))  # 끊김 없이 이어지는 머리 폭과 깊이입니다.
        return profile[0][1:] if y < profile[0][0] else profile[-1][1:]  # 머리 위아래 끝에서도 닫힌 윤곽을 유지합니다.
    rows, segments = 33, 128  # 앞머리 끝과 옆머리 곡선을 함께 유지할 표면 해상도입니다.
    outer, inner = [], []  # 바깥 머리와 끝에서 보이는 실제 두께를 함께 만듭니다.
    for row in range(rows):  # 정수리에서 짧은 머리의 아래 경계까지 이어 갑니다.
        t = .001 + .999 * row / (rows - 1)  # 정수리 정점이 한 위치에 겹치지 않는 길이 비율입니다.
        for column in range(segments):  # 두상 둘레를 같은 순서로 구성합니다.
            angle = math.tau * column / segments  # 앞머리와 옆머리 및 뒷머리를 구분할 방향입니다.
            cosine, sine = math.cos(angle), math.sin(angle)  # 앞뒤 방향과 좌우 위치 성분입니다.
            front = max(0.0, min(1.0, (cosine - .10) / .80))  # 정면의 앞머리와 귀 위의 짧은 옆머리를 구분합니다.
            front = front * front * (3 - 2 * front)  # 앞머리 끝이 관자놀이에서 갑자기 꺾이지 않게 합니다.
            fringe = 1.285 + .022 * sine + .012 * math.cos(7 * angle + .4) + .006 * math.cos(13 * angle)  # 이마를 덮는 앞머리에 작고 부드러운 길이 차이를 줍니다.
            nape = 1.10 + .065 * sine * sine  # 귀 위는 정리하고 뒷목은 짧게 감싸는 아래 경계입니다.
            boundary = nape + (fringe - nape) * front  # 앞과 옆 및 뒤의 머리 길이를 하나의 연속 경계로 잇습니다.
            y = 1.61 + (boundary - 1.61) * t  # 솟은 장식 없이 둥근 정수리에서 머리 끝으로 내려옵니다.
            width, front_depth, back_depth = section(y)  # 현재 머리 높이의 실제 두상 윤곽입니다.
            x = width * sine + .012 * (1 - t)  # 정수리 쪽에만 작은 빗질 방향을 주고 머리 끝은 두상을 따르게 합니다.
            z = (front_depth if cosine >= 0 else back_depth) * cosine  # 얼굴 앞과 뒤통수의 서로 다른 깊이입니다.
            radial = max(.0001, math.hypot(x, z))  # 안쪽 표면의 두께를 계산할 머리 중심 거리입니다.
            groove = .0015 * math.sin(math.pi * t) ** 2 * (.5 + .5 * math.cos(6 * angle + 1.3 * t))  # 큰 판 경계 대신 표면 안의 얕은 머릿결을 만듭니다.
            x, z = x * (1 - groove / radial), z * (1 - groove / radial)  # 머리 전체의 둥근 볼륨은 유지하면서 머릿결만 반영합니다.
            outer.append((x, y, z))  # 정수리와 앞머리 및 짧은 옆머리가 이어진 바깥 표면입니다.
            thickness = min(.008, radial * .30)  # 머리 끝에도 실제 두께가 있고 정수리에서는 면이 뒤집히지 않게 합니다.
            inner.append((x * (1 - thickness / radial), y - .002 * (1 - t), z * (1 - thickness / radial)))  # 바깥 머리 아래에 놓이는 안쪽 표면입니다.
    count, faces = len(outer), []  # 바깥과 안쪽 표면을 구분할 정점 수입니다.
    for row in range(rows - 1):  # 같은 방향의 머리 단면들을 연결합니다.
        for column in range(segments):  # 정수리부터 머리 끝까지 연속된 사각 면을 만듭니다.
            following = (column + 1) % segments  # 두상 둘레의 이음새를 닫습니다.
            face = (row * segments + column, (row + 1) * segments + column, (row + 1) * segments + following, row * segments + following)  # 바깥 머리의 곡면 한 구간입니다.
            faces.extend((face, tuple(index + count for index in reversed(face))))  # 안쪽 머리 면은 반대 방향으로 연결합니다.
    for column in range(segments):  # 앞머리와 옆머리 및 뒷목의 아래 경계를 실제 두께로 닫습니다.
        following = (column + 1) % segments  # 같은 아래 경계의 다음 정점입니다.
        a, b = (rows - 1) * segments + column, (rows - 1) * segments + following  # 머리 끝의 바깥쪽 이웃 정점입니다.
        faces.append((a, b, b + count, a + count))  # 머리카락 끝에서 열린 면이 보이지 않게 합니다.
    faces.extend((tuple(reversed(range(segments))), tuple(index + count for index in range(segments))))  # 바깥과 안쪽 정수리도 각각 닫습니다.
    hair = mesh('ShortNaturalHair', outer + inner, faces, 'Hair')  # 별도 솟은 가닥이나 큰 앞머리 판 없이 이어진 짧은 머리입니다.
    parts.append((hair, 'Head'))  # 기존 머리뼈에 새 머리카락 전체를 연결합니다.
    surface = BVHTree.FromPolygons([tuple(vertex.co) for vertex in hair.data.vertices], [tuple(face.vertices) for face in hair.data.polygons])  # 얕은 머릿결 색면을 실제 머리 표면에 밀착시킬 위치입니다.
    def hair_depth(x, y):  # 머리카락 정면의 실제 표면 깊이를 구합니다.
        hit, _, _, _ = surface.ray_cast(Vector((x, -.7, y)), Vector((0, 1, 0)), 1.4)  # 머릿결 색면을 머리의 가장 앞쪽 표면에 붙입니다.
        return -hit.y if hit is not None else .20  # 머리 표면에서 밝은 선이 떠 있지 않게 합니다.
    paths = (((-.16, 1.38), (-.135, 1.43), (-.09, 1.49), (-.03, 1.53)), ((-.07, 1.355), (-.035, 1.41), (.01, 1.465), (.075, 1.49)))  # 앞머리와 정수리의 빗질 흐름을 나타내는 두 개의 짧은 곡선입니다.
    for index, points in enumerate(paths):  # 짙은 머리에 작은 머릿결만 추가합니다.
        parts.append((face_ribbon('ShortHairSheen_' + str(index), points, .004, 'HairEdge', .001, hair_depth), 'Head'))  # 금색 반짝임 대신 은은한 갈색 머릿결을 실제 표면에 붙입니다.
    for obj, _ in parts:  # 머리카락 전용 교체 경로에서 사용할 부위 표식입니다.
        if obj.get('export_name', '').startswith(('ShortNaturalHair', 'ShortHairSheen')):  # 이 함수가 만든 머리 부위만 표시합니다.
            obj['hair_part'] = True  # 얼굴과 귀 및 표정 메시를 구분하여 보존합니다.


def make_hair(parts, girl):  # 두 캐릭터의 현재 머리 스타일을 각각 제작합니다.
    if not girl:  # 남자아이에게는 짧은 흑갈색 머리를 사용합니다.
        make_boy_hair(parts)  # 전체 모델 재생성에서도 새 머리 모양을 유지합니다.
        return  # 이전 금발과 위로 솟은 장식은 만들지 않습니다.
    hair_color = 'CopperHair' if girl else 'BlondHair'  # 참고 이미지의 구리색과 옅은 금발로 두 친구를 구분합니다.
    lower = [(.98, .246, .175, .245), (1.06, .284, .225, .276)] if girl else [(1.09, .281, .225, .270)]  # 큰 볼 옆을 감싸는 뒤쪽 헤어 윤곽입니다.
    profile = lower + [(1.16, .303, .255, .291), (1.26, .303, .255, .291), (1.38, .280, .234, .272), (1.49, .218, .185, .220), (1.58, .120, .105, .120), (1.625, .001, .001, .001)]  # 실제 큰 두상 위에 연결되는 헤어 바탕입니다.
    skin = next((obj for obj, _ in parts if obj.get('export_name') == 'HeadSkin'), None)  # 전체 얼굴 제작에서는 방금 만든 귀와 두피의 실제 표면을 사용합니다.
    skin_shift = 0.0  # 새 얼굴의 피부는 아직 머리 높이 이동이 적용되지 않은 제작 좌표입니다.
    if skin is None:  # 머리카락만 교체할 때는 현재 얼굴과 귀의 메시를 그대로 읽습니다.
        skin = bpy.data.objects[ACTIVE_ROOT.name + '_HeadSkin']  # 이미 저장된 얼굴을 변경하지 않고 옆머리 안쪽 표면으로 사용합니다.
        skin_shift = HEAD_DROP  # 저장된 피부 좌표를 머리카락 제작과 같은 높이 기준으로 읽습니다.
    skin_surface = BVHTree.FromPolygons([(vertex.co.x, vertex.co.y, vertex.co.z + skin_shift) for vertex in skin.data.vertices], [tuple(face.vertices) for face in skin.data.polygons])  # 귀와 두피가 실제로 돌출된 모양을 옆머리 제작에 반영합니다.
    def hair_section(y):  # 헤어 높이별 폭과 앞뒤 깊이를 연결합니다.
        for first, second in zip(profile, profile[1:]):  # 해당 높이의 이웃한 단면입니다.
            if first[0] <= y <= second[0]:  # 현재 헤어 표면의 높이 구간입니다.
                t = (y - first[0]) / (second[0] - first[0])  # 두 단면의 보간 비율입니다.
                return tuple(a + (b - a) * t for a, b in zip(first[1:], second[1:]))  # 끊어진 타원 캡을 사용하지 않는 연속 단면입니다.
        return profile[0][1:] if y < profile[0][0] else profile[-1][1:]  # 양끝의 헤어 윤곽을 유지합니다.
    def hair_point(angle, t):  # 정수리에서 각 앞머리 끝까지 이어지는 실제 정점입니다.
        signed = math.atan2(math.sin(angle), math.cos(angle))  # 앞머리 양쪽을 같은 연속 각도로 계산합니다.
        blend = max(0, min(1, (math.cos(angle) + .10) / .82))  # 앞과 옆의 경계를 넓은 구간에서 연결합니다.
        blend = blend * blend * (3 - 2 * blend)  # 앞머리 높이가 임계각에서 갑자기 뛰지 않게 합니다.
        base = .99 if girl else 1.10  # 뒤쪽 헤어가 작은 몸의 목을 과하게 덮지 않는 높이입니다.
        tip = .010 * math.exp(-((signed + .32) / .22) ** 2)  # 바탕 헤어의 경계를 부드럽게 이어 줍니다.
        boundary = base + (1.285 - base) * blend - tip * blend  # 바탕 머리를 앞머리 아래까지 이어 조각 사이로 두피가 넓게 드러나지 않게 합니다.
        y = 1.625 + (boundary - 1.625) * t  # 넓어진 두상의 정수리와 헤어라인을 연결합니다.
        width, front, back = hair_section(y)  # 머리 높이와 맞는 실제 헤어 폭입니다.
        sweep = (.035 if girl else .12) * (1 - t) ** 1.5  # 앞머리 끝을 비틀지 않고 정수리 쪽에서 가르마 흐름을 만듭니다.
        theta = angle + sweep  # 두상 위를 따라 흐르는 머리카락 방향입니다.
        x = width * math.sin(theta)  # 실제 헤어의 좌우 위치입니다.
        z = (front if math.cos(theta) >= 0 else back) * math.cos(theta)  # 앞머리와 뒤통수의 서로 다른 깊이입니다.
        groove = .0025 * math.exp(-((signed - .28) / .055) ** 2) * math.sin(math.pi * t) ** 2  # 가르마는 별도 갈색 판 대신 표면의 얕은 흐름으로 표현합니다.
        radial = max(.0001, math.hypot(x, z))  # 헤어 단면의 중심으로부터 거리입니다.
        point = Vector((x * (1 - groove / radial), y, z * (1 - groove / radial)))  # 기존 머리 모양과 머릿결을 유지한 실제 헤어 정점입니다.
        if 1.055 <= y <= 1.19:  # 귀가 들어 있는 높이의 옆머리만 실제 피부 바깥으로 맞춥니다.
            direction = Vector((point.x, -point.z, 0)).normalized()  # 같은 높이에서 머리 중심을 향해 피부를 읽을 가로 방향입니다.
            origin = Vector((direction.x * .7, direction.y * .7, y))  # 귀와 머리카락보다 바깥쪽에서 피부 표면을 읽습니다.
            hit, _, _, _ = skin_surface.ray_cast(origin, -direction, 1.4)  # 해당 옆머리 방향으로 실제 귀와 두피의 가장 바깥 표면을 구합니다.
            if hit is not None:  # 실제 피부가 존재하는 방향에만 머리카락과의 간격을 적용합니다.
                hair_radius = max(.0001, math.hypot(point.x, point.z))  # 현재 옆머리의 중심으로부터 거리를 구합니다.
                scale = max(1.0, (math.hypot(hit.x, hit.y) + .009) / hair_radius)  # 머리카락 두께를 포함해 귀 피부보다 바깥에 놓이는 최소 간격을 확보합니다.
                point.x, point.z = point.x * scale, point.z * scale  # 높이와 앞머리 모양은 유지하고 귀가 뚫고 나온 옆머리만 바깥으로 맞춥니다.
        return point  # 실제 귀와 겹치지 않도록 제작한 옆머리 표면을 사용합니다.
    rows, segments = 29, 96  # 앞머리 곡선과 옆머리 경계를 함께 유지할 표면 해상도입니다.
    outer, inner = [], []  # 겉면과 안쪽 면을 함께 만들어 열린 종이 조각을 남기지 않습니다.
    for row in range(rows):  # 정수리부터 헤어 끝까지 연속된 격자로 만듭니다.
        t = .001 + .999 * row / (rows - 1)  # 정수리 정점의 겹침을 피합니다.
        for index in range(segments):  # 같은 각도의 헤어 경계를 위아래로 연결합니다.
            point = hair_point(math.tau * index / segments, t)  # 바깥쪽 헤어의 실제 위치입니다.
            radial = max(.0001, math.hypot(point.x, point.z))  # 안쪽 두께를 계산할 중심 거리입니다.
            thickness = min(.006, radial * .35)  # 정수리에서도 안쪽 면이 반대편으로 뒤집히지 않는 두께입니다.
            outer.append(tuple(point))  # 표면의 넓은 머리카락 흐름입니다.
            inner.append((point.x * (1 - thickness / radial), point.y - .002 * (1 - t), point.z * (1 - thickness / radial)))  # 옆과 아래에서도 실제 두께가 보이는 안쪽 면입니다.
    vertices, faces = outer + inner, []  # 하나의 닫힌 헤어 모델을 구성합니다.
    offset = len(outer)  # 안쪽 표면의 시작 정점입니다.
    for row in range(rows - 1):  # 앞머리와 옆머리를 같은 사각 면 흐름으로 잇습니다.
        for index in range(segments):  # 경계가 튀지 않는 연속된 둘레입니다.
            following = (index + 1) % segments  # 헤어 둘레의 이음새입니다.
            face = (row * segments + index, (row + 1) * segments + index, (row + 1) * segments + following, row * segments + following)  # 바깥쪽 헤어 면입니다.
            faces.append(face)  # 정수리와 앞머리 끝을 연결합니다.
            faces.append(tuple(vertex + offset for vertex in reversed(face)))  # 안쪽 면은 반대 방향으로 연결합니다.
    for index in range(segments):  # 헤어 끝의 바깥과 안쪽을 실제로 잇습니다.
        following = (index + 1) % segments  # 같은 경계의 이웃 정점입니다.
        a, b = (rows - 1) * segments + index, (rows - 1) * segments + following  # 앞머리 또는 단발 끝의 두 정점입니다.
        faces.append((a, b, b + offset, a + offset))  # 얇고 닫힌 끝면을 만듭니다.
    faces.extend([tuple(reversed(range(segments))), tuple(index + offset for index in range(segments))])  # 겉면과 안쪽 정수리를 각각 닫습니다.
    parts.append((mesh('BobHair' if girl else 'SidePartHair', vertices, faces, hair_color), 'Head'))  # 앞머리 면을 받치는 실제 두께의 헤어 바탕입니다.
    make_reference_fringe(parts, girl, profile)  # 참고 이미지처럼 큰 곡면 조각과 얇은 선으로 앞머리를 나눕니다.
    if girl:  # 기존 작은 머리핀도 새 헤어 표면에 직접 맞춥니다.
        clip_vertices = []  # 머리핀의 얇은 표면 정점입니다.
        for row in range(2):  # 머리핀의 위아래 가장자리입니다.
            for column in range(9):  # 헤어 곡률을 따라 길이를 나눕니다.
                point = hair_point(.78 + .26 * column / 8, .62 + .025 * row)  # 실제 옆머리 위의 부착 위치입니다.
                normal = Vector((point.x, 0, point.z)).normalized()  # 헤어 바깥쪽으로 향하는 방향입니다.
                clip_vertices.append(tuple(point + normal * .002))  # 떠 있는 상자 대신 헤어를 따르는 얇은 핀입니다.
        clip_faces = [(index, index + 9, index + 10, index + 1) for index in range(8)]  # 머리핀의 바깥 면이 헤어 표면의 바깥쪽을 향하게 합니다.
        parts.append((mesh('HairClip', clip_vertices, clip_faces, 'Yellow'), 'Head'))  # 기존 머리핀 역할을 새 헤어에 맞춰 유지합니다.


def make_reference_fringe(parts, girl, profile):  # 참고 이미지의 넓은 앞머리 조각과 그린 듯한 경계선을 제작합니다.
    hair_color = 'CopperHair' if girl else 'BlondHair'  # 구리색과 옅은 금발의 명확한 큰 색면입니다.
    def hair_depth(x, y):  # 앞머리 조각이 두상 위에서 흐르는 깊이입니다.
        for first, second in zip(profile, profile[1:]):  # 해당 높이의 헤어 바탕 단면입니다.
            if first[0] <= y <= second[0]:  # 바탕 표면의 실제 범위를 찾습니다.
                t = (y - first[0]) / (second[0] - first[0])  # 두 단면 사이의 위치입니다.
                width, front, _ = [a + (b - a) * t for a, b in zip(first[1:], second[1:])]  # 헤어 바탕의 폭과 깊이입니다.
                cap = front * math.sqrt(max(0, 1 - (x / width) ** 2))  # 바탕 헤어의 실제 앞면 깊이입니다.
                extension = .14 - .08 * max(0, (y - 1.4) / .22) - .04 * (abs(x) / .30) ** 2  # 바탕 밖으로 뻗는 앞머리 끝도 넓은 곡면을 유지합니다.
                return max(cap, face_depth(x, y) + .008, extension)  # 얼굴에 파묻히거나 바깥 가장자리가 갑자기 접히지 않게 합니다.
        return .025  # 정수리 끝에서도 얇은 앞머리 두께를 유지합니다.
    paths = [((-.075, 1.56), (-.20, 1.54), (-.19, 1.31), (-.115, 1.255), .090), ((-.035, 1.58), (.07, 1.57), (.12, 1.35), (.035, 1.265), .105), ((.075, 1.55), (.20, 1.48), (.23, 1.34), (.19, 1.22), .060)] if girl else [((-.06, 1.58), (-.16, 1.58), (-.19, 1.42), (-.175, 1.285), .075), ((-.035, 1.59), (.08, 1.60), (.24, 1.45), (.19, 1.31), .115), ((.09, 1.55), (.19, 1.50), (.245, 1.39), (.225, 1.29), .055)]  # 가닥 원통이 아닌 세 개의 넓고 다른 앞머리 흐름입니다.
    rows, columns = 25, 9  # 넓은 면의 길이와 가로 방향을 함께 나누는 격자입니다.
    for lock_index, (p0, p1, p2, p3, maximum_width) in enumerate(paths):  # 앞머리 조각마다 고유한 뿌리와 끝을 사용합니다.
        p0, p1, p2, p3 = (Vector(point) for point in (p0, p1, p2, p3))  # 큰 앞머리의 베지어 곡선 제어점입니다.
        front_vertices, back_vertices, normals = [], [], []  # 겉면과 안쪽 면 및 얇은 경계선 방향입니다.
        for row in range(rows):  # 뿌리에서 끝으로 이어지는 넓은 헤어 면입니다.
            t = row / (rows - 1)  # 앞머리 길이 방향의 위치입니다.
            center = (1 - t) ** 3 * p0 + 3 * (1 - t) ** 2 * t * p1 + 3 * (1 - t) * t ** 2 * p2 + t ** 3 * p3  # 앞머리의 실제 중심 곡선입니다.
            tangent = 3 * (1 - t) ** 2 * (p1 - p0) + 6 * (1 - t) * t * (p2 - p1) + 3 * t ** 2 * (p3 - p2)  # 해당 위치의 흐름 방향입니다.
            normal = Vector((-tangent.y, tangent.x)).normalized()  # 앞머리 폭을 펼칠 평면 방향입니다.
            normals.append(normal)  # 같은 폭 방향을 경계선에도 사용합니다.
            width = .012 * (1 - t) + maximum_width * math.sin(math.pi * t) ** 1.1 * (1 - t) ** .25 + .001  # 좁은 뿌리와 넓은 중간 및 가는 끝입니다.
            for column in range(columns):  # 원형 튜브 대신 넓은 판형 곡면으로 만듭니다.
                u = -1 + 2 * column / (columns - 1)  # 앞머리 폭 안의 위치입니다.
                point = center + normal * width * u  # 그림처럼 디자인한 앞머리의 실제 외곽입니다.
                z = hair_depth(point.x, point.y) + .008 + .035 * (1 - u * u) * math.sin(math.pi * t)  # 넓은 앞머리 중앙에 둥근 부피를 주어 얇은 판처럼 보이지 않게 합니다.
                front_vertices.append((point.x, point.y, z))  # 실제 바깥 헤어 표면입니다.
                back_vertices.append((point.x, point.y, z - .020))  # 옆에서도 앞머리에 실제 두께가 보이도록 안쪽 면을 둡니다.
        count, faces = len(front_vertices), []  # 앞머리 조각의 겉면과 안쪽 면을 연결할 기준입니다.
        for row in range(rows - 1):  # 같은 흐름의 앞뒤 격자를 연결합니다.
            for column in range(columns - 1):  # 넓은 면의 이웃 정점을 연결합니다.
                face = (row * columns + column, (row + 1) * columns + column, (row + 1) * columns + column + 1, row * columns + column + 1)  # 앞머리 한 칸의 실제 곡면입니다.
                faces.extend((face, tuple(index + count for index in reversed(face))))  # 겉면과 안쪽을 반대 방향으로 만듭니다.
        boundary = list(range(columns)) + [row * columns + columns - 1 for row in range(1, rows)] + [(rows - 1) * columns + column for column in range(columns - 2, -1, -1)] + [row * columns for row in range(rows - 2, 0, -1)]  # 격자의 실제 바깥 가장자리입니다.
        for a, b in zip(boundary, boundary[1:] + boundary[:1]):  # 바깥과 안쪽 가장자리를 실제로 닫습니다.
            faces.append((a, b, b + count, a + count))  # 닫힌 얇은 헤어 조각입니다.
        parts.append((mesh('HairLock_' + str(lock_index), front_vertices + back_vertices, faces, hair_color), 'Head'))  # 넓은 앞머리 면을 실제 입체 메시로 저장합니다.
        for edge_index, column in enumerate((0, columns - 1)):  # 앞머리 조각의 양쪽에 얇은 그림 선을 넣습니다.
            stroke = []  # 경계선의 실제 표면 정점입니다.
            for row in range(rows):  # 큰 앞머리 흐름과 같은 위치를 따릅니다.
                point = Vector(front_vertices[row * columns + column])  # 실제 앞머리 가장자리입니다.
                for sign in (-1, 1):  # 얇은 선의 양쪽 가장자리입니다.
                    stroke.append((point.x + normals[row].x * sign * .0012, point.y + normals[row].y * sign * .0012, point.z + .0008))  # 머리카락에 그려진 듯한 경계선입니다.
            stroke_faces = []  # 정면을 향하는 선의 면 연결입니다.
            for row in range(rows - 1):  # 곡선을 따라 얇은 띠를 연결합니다.
                face = (row * 2, row * 2 + 2, row * 2 + 3, row * 2 + 1)  # 경계선 한 구간의 면입니다.
                area = sum(stroke[a][0] * stroke[b][1] - stroke[b][0] * stroke[a][1] for a, b in zip(face, face[1:] + face[:1]))  # 선이 바깥 정면을 향하도록 면 방향을 정합니다.
                stroke_faces.append(face if area >= 0 else tuple(reversed(face)))  # 좌우 경계선이 뒤집혀 사라지지 않게 합니다.
            parts.append((mesh(f'HairLockLine_{lock_index}_{edge_index}', stroke, stroke_faces, 'HairLine', smooth=False), 'Head'))  # 검은 머리 덩어리 대신 분명한 조각 경계를 만듭니다.
    if girl:  # 참고 이미지의 가벼운 묶음머리 실루엣을 반영합니다.
        for side, sign in (('Left', -1), ('Right', 1)):  # 두 귀 뒤에 작은 묶음머리를 둡니다.
            points = [(sign * x, y, z) for x, y, z in ((.265, 1.17, -.045), (.335, 1.155, -.060), (.385, 1.075, -.045), (.375, 1.005, -.015), (.325, 1.015, .005))]  # 귀 뒤에서 아래로 휘어지는 짧은 묶음머리 중심선입니다.
            tuft = tube(side + 'HairTuft', points, [.030, .064, .068, .043, .004], hair_color, segments=20)  # 각진 납작한 조각을 둥글고 끝이 가늘어지는 입체 묶음머리로 만듭니다.
            parts.append((tuft, 'Head'))  # 묶음머리도 같은 머리 관절에 연결합니다.
    else:  # 금발 캐릭터에는 참고 이미지의 작은 위쪽 헤어 실루엣을 반영합니다.
        tuft = extrude_outline('TopHairTuft', [(.015, 1.60), (.10, 1.71), (.19, 1.73), (.14, 1.65), (.18, 1.61), (.09, 1.59)], .055, hair_color, .005)  # 원통 컬 대신 한 장의 넓은 헤어 면입니다.
        parts.append((tuft, 'Head'))  # 위쪽 헤어도 머리와 함께 움직입니다.

def make_hand(parts, side, sign):  # 넓은 손바닥과 짧게 말린 손가락을 연결하여 둥근 캐릭터 손을 만듭니다.
    palm = loft(side + 'PalmSkin', [(sign * .574, .035, .030, 1.085, 0), (sign * .592, .044, .033, 1.085, .001), (sign * .617, .059, .036, 1.085, .003), (sign * .642, .063, .036, 1.085, .004), (sign * .661, .056, .031, 1.085, .005), (sign * .671, .045, .025, 1.085, .006)], 'Skin', axis='X', segments=32)  # 손목에서 부드럽게 넓어지고 손가락 뿌리를 받치는 도톰한 손바닥입니다.
    pieces = [palm]  # 피부를 실제로 연결할 부위입니다.
    for finger, dy, length, radius in HAND_FINGERS:  # 메시와 골격이 공유하는 네 손가락 규격을 사용합니다.
        points = [tuple(finger_point(sign, dy, length, t)) for t in (0, .20, .40, .60, .80, .94, 1)]  # 손바닥에 붙은 뿌리부터 살짝 굽은 손끝까지 이어지는 중심선입니다.
        radii = [radius * factor for factor in (.82, 1.0, 1.03, .98, .78, .40, .035)]  # 중간은 도톰하게 유지하고 끝에서만 둥근 반구로 닫히도록 반경을 줄입니다.
        pieces.append(tube(side + finger + 'Skin', points, radii, 'Skin', segments=16))  # 손바닥 밖으로 짧은 둥근 손끝이 보이는 캐릭터 손가락입니다.
    pieces.append(tube(side + 'ThumbSkin', [(sign * x, y, z) for x, y, z in THUMB_POINTS], [.021, .021, .017, .002], 'Skin', segments=20))  # 넓은 손바닥의 옆면에 붙는 짧고 도톰한 엄지를 만듭니다.
    parts.append((merge_surface(side + 'HandSkin', pieces, 'Skin', .0018), side + 'Palm'))  # 손바닥과 손가락을 연결한 실제 피부에 웨이트를 부여합니다.


def make_shirt(profile, color, girl, loose=False):  # 몸판과 소매의 공유 연결을 유지하며 반소매와 넉넉한 후드티를 제작합니다.
    segments, lower_row, upper_row, half_span = 48, 5, 8, 4  # 높이 .99부터 1.135까지의 몸판 양옆을 넓은 어깨 연결부로 사용합니다.
    shoulder_radius = .071 if loose else .054  # 후드티는 위팔과 옷 사이에 여유가 있는 소매 단면을 사용합니다.
    vertices, faces, sleeve_weights = [], [], []  # 몸판과 소매의 표면 및 연결 구간별 팔 영향을 함께 기록합니다.
    for y, width, depth, shift_x, shift_z in profile:  # 기존 몸통 크기와 윤곽은 그대로 사용합니다.
        for column in range(segments):  # 몸판 둘레를 같은 수의 정점으로 구성합니다.
            angle = math.tau * column / segments  # 몸판의 좌우 어깨와 앞뒤 표면을 구분할 둘레 위치입니다.
            vertices.append((shift_x + width * math.cos(angle), y, shift_z + depth * math.sin(angle)))  # 어깨와 이어질 타원형 몸판의 실제 정점입니다.
            sleeve_weights.append(0.0)  # 몸판 정점은 위팔 회전의 영향을 받지 않게 합니다.
    for row in range(len(profile) - 1):  # 허리부터 목까지 이어지는 몸판 면을 만듭니다.
        for column in range(segments):  # 양옆 어깨를 제외한 몸판 면을 연결합니다.
            in_shoulder = any((column - center + half_span) % segments < half_span * 2 for center in (0, segments // 2))  # 소매를 직접 이어 붙일 양쪽 어깨 구간입니다.
            if lower_row <= row < upper_row and in_shoulder:  # 몸판 안쪽에 막힌 뚜껑을 남기지 않고 소매 연결 구멍을 비웁니다.
                continue  # 이 경계의 정점은 이후 소매 면과 공유합니다.
            following = (column + 1) % segments  # 몸판 둘레의 다음 정점입니다.
            faces.append((row * segments + column, row * segments + following, (row + 1) * segments + following, (row + 1) * segments + column))  # 기존 몸판의 사각 면을 유지합니다.
    faces.extend((tuple(reversed(range(segments))), tuple((len(profile) - 1) * segments + column for column in range(segments))))  # 허리와 목의 끝을 닫습니다.
    for sign, center in ((1, 0), (-1, segments // 2)):  # 양쪽 소매를 각자의 몸판 경계에서 직접 이어 만듭니다.
        columns = [(center + offset) % segments for offset in range(-half_span, half_span + 1)]  # 어깨 구멍의 앞뒤 폭을 구성하는 몸판 정점입니다.
        boundary = [lower_row * segments + column for column in columns]  # 겨드랑이 아래쪽 경계의 공유 정점입니다.
        boundary.extend(row * segments + columns[-1] for row in range(lower_row + 1, upper_row + 1))  # 어깨 구멍 한쪽의 세로 경계를 잇습니다.
        boundary.extend(upper_row * segments + column for column in reversed(columns[:-1]))  # 어깨 위쪽 경계를 반대 방향으로 잇습니다.
        boundary.extend(row * segments + columns[0] for row in range(upper_row - 1, lower_row, -1))  # 나머지 세로 경계까지 이어 닫힌 연결 고리를 만듭니다.
        angles = [math.atan2((vertices[index][1] - 1.085) / shoulder_radius, vertices[index][2] / (shoulder_radius * .94)) for index in boundary]  # 몸판 경계 순서를 해당 의상의 소매 굵기에 맞춰 대응시킵니다.
        previous_ring = boundary  # 첫 연결 면이 몸판의 기존 정점을 그대로 사용하게 합니다.
        for fraction in (.25, .50, .75, 1.0):  # 넓은 어깨 구멍에서 위팔 소매까지 여러 연결 단면을 만듭니다.
            ring = []  # 현재 어깨 연결 단면의 실제 정점 목록입니다.
            for source_index, angle in zip(boundary, angles):  # 같은 경계 순서를 유지하여 면이 비틀리거나 분리되지 않게 합니다.
                source = Vector(vertices[source_index])  # 몸판과 공유하는 실제 어깨 경계 위치입니다.
                target = Vector((sign * .24, 1.085 + shoulder_radius * math.sin(angle), shoulder_radius * .94 * math.cos(angle)))  # 기존 어깨 연결을 유지하면서 후드티의 여유 있는 소매를 만듭니다.
                ring.append(len(vertices))  # 새 정점을 연결 단면에 등록합니다.
                vertices.append(tuple(source.lerp(target, fraction)))  # 몸판과 소매 사이를 넓은 연속 표면으로 연결합니다.
                sleeve_weights.append(fraction * fraction * (3 - 2 * fraction))  # 몸판의 고정 영향에서 위팔 영향으로 부드럽게 전환합니다.
            for index in range(len(ring)):  # 두 단면 사이를 끊기지 않는 사각 면으로 잇습니다.
                following = (index + 1) % len(ring)  # 연결 단면 둘레의 다음 정점입니다.
                faces.append((previous_ring[index], previous_ring[following], ring[following], ring[index]))  # 몸판 경계와 어깨 및 위팔이 실제 모서리를 공유하게 합니다.
            previous_ring = ring  # 다음 연결 단면이 현재 단면을 이어받게 합니다.
        sleeve_profile = ((.28, .055), (.31, .051), (.335, .047)) if girl else ((.28, .055), (.34, .052), (.41, .047), (.50, .040), (.565, .035))  # 기존 반소매와 긴 소매의 길이 및 끝 굵기를 유지합니다.
        if loose:  # 넉넉한 후드티는 팔꿈치와 손목까지 옷의 여유를 유지합니다.
            sleeve_profile = ((.28, .074), (.34, .072), (.41, .067), (.50, .059), (.565, .048))  # 몸에 밀착된 긴팔 대신 손목으로 완만하게 모이는 후드 소매입니다.
        for distance, radius in sleeve_profile:  # 어깨 바깥에서 기존 소매 끝까지 같은 표면을 이어 갑니다.
            ring = []  # 현재 소매 단면의 정점 목록입니다.
            for angle in angles:  # 어깨 연결부와 같은 둘레 순서를 사용합니다.
                ring.append(len(vertices))  # 다음 사각 면에 사용할 소매 정점입니다.
                vertices.append((sign * distance, 1.085 + radius * math.sin(angle), radius * .94 * math.cos(angle)))  # 기존 팔뼈 축을 따라 소매 표면을 배치합니다.
                sleeve_weights.append(1.0)  # 연결부 바깥의 소매는 기존 팔 관절을 온전히 따르게 합니다.
            for index in range(len(ring)):  # 인접한 소매 단면을 실제 면으로 연결합니다.
                following = (index + 1) % len(ring)  # 소매 둘레의 이음새를 닫습니다.
                faces.append((previous_ring[index], previous_ring[following], ring[following], ring[index]))  # 어깨부터 소매 끝까지 표면의 연결을 유지합니다.
            previous_ring = ring  # 다음 단면의 연결 기준입니다.
        faces.append(tuple(reversed(previous_ring)))  # 시보리 안쪽의 소매 끝을 닫습니다.
    used = sorted({index for face in faces for index in face})  # 어깨 구멍 안쪽에서 면에 사용하지 않는 정점을 제작 데이터에서 제외합니다.
    remap = {previous: current for current, previous in enumerate(used)}  # 연속 표면에 실제 사용하는 정점의 새 인덱스입니다.
    obj = mesh('ConnectedShirt', [vertices[index] for index in used], [tuple(remap[index] for index in face) for face in faces], color)  # 작은 접점으로 합친 소매 대신 공유 경계를 가진 하나의 상의를 만듭니다.
    group = obj.vertex_groups.new(name='상의_소매_연결')  # 어깨 연결 단면의 실제 위치를 스킨 연결에 전달합니다.
    for index, source_index in enumerate(used):  # 사용한 정점과 팔 영향 비율을 같은 순서로 연결합니다.
        if sleeve_weights[source_index] > 0:  # 몸판에는 팔 영향 항목을 만들지 않습니다.
            group.add([index], sleeve_weights[source_index], 'REPLACE')  # 소매와 어깨 연결부에만 해당 변형 비율을 기록합니다.
    smooth = obj.modifiers.new('어깨와 소매 연속 곡면', 'SUBSURF')  # 공유 모서리에서 몸판과 소매의 곡률을 함께 다듬습니다.
    smooth.levels = 2  # 어깨 연결부가 각지지 않는 실제 표면 밀도입니다.
    apply_modifier(obj, smooth)  # 연결된 표면과 보간된 소매 영향 비율을 실제 정점에 반영합니다.
    return obj  # 기존 몸 비율과 팔뼈에 연결할 상의 표면입니다.


def make_boy_outfit(shoulders_only=False):  # 남자아이에게 산호색 후드티와 짙은 반바지로 구성된 새 의상을 제작합니다.
    profile = [(.66, .150, .104, 0, 0), (.70, .165, .116, 0, .003), (.76, .174, .125, 0, .005), (.83, .176, .128, 0, .006), (.91, .176, .128, 0, .006), (.99, .170, .121, 0, .003), (1.045, .158, .108, 0, .001), (1.09, .130, .086, 0, 0), (1.135, .084, .057, 0, 0), (1.17, .058, .045, 0, 0), (1.19, .055, .043, 0, 0)]  # 허리 아래로 내려오는 넓은 몸판과 여유 있는 가슴을 가진 후드티 패턴입니다.
    shirt = make_shirt(profile, 'Coral', False, loose=True)  # 기존 어깨의 공유 정점 연결을 유지한 산호색 후드티 본체입니다.
    parts = [(shirt, 'Top')]  # 새 옷의 표면만 기존 골격에 연결할 목록입니다.
    if shoulders_only:  # 어깨 수정에서도 남자아이의 새 후드티 디자인을 유지합니다.
        return parts  # 후드와 반바지를 중복 생성하지 않고 상의 본체만 돌려줍니다.
    surface = BVHTree.FromPolygons([tuple(vertex.co) for vertex in shirt.data.vertices], [tuple(face.vertices) for face in shirt.data.polygons])  # 주머니와 끈을 후드티의 실제 앞면에 부착할 표면입니다.
    def cloth_depth(x, y):  # 후드티 앞면에서 소품이 붙을 실제 깊이를 구합니다.
        hit, _, _, _ = surface.ray_cast(Vector((x, -.7, y)), Vector((0, 1, 0)), 1.4)  # 옷 앞면으로 향하는 위치를 읽습니다.
        return -hit.y if hit is not None else .10  # 옷의 곡면에 맞춰 주머니와 끈을 놓습니다.
    hem = [(.651, .143, .099, 0, 0), (.660, .153, .106, 0, 0), (.680, .158, .110, 0, .001), (.693, .157, .110, 0, .002)]  # 넓은 후드티 아래를 가볍게 모아 주는 둥근 밑단입니다.
    parts.append((loft('HoodieHem', hem, 'Cream', segments=48, power=1.0), 'Top'))  # 반바지 위에 겹쳐지는 후드티의 실제 밑단 두께입니다.
    for side, sign in (('Left', -1), ('Right', 1)):  # 양쪽 후드 소매를 같은 의상으로 마감합니다.
        cuff = [(sign * (.565 + shift), .048 * factor, .046 * factor, 1.085, 0) for shift, factor in ((-.018, .90), (-.008, 1.03), (.012, 1.03), (.022, .86))]  # 넓은 소매가 손목에서 모이는 후드티 시보리입니다.
        parts.append((loft(side + 'HoodieCuff', cuff, 'Cream', axis='X', segments=40, power=1.0), 'Arm'))  # 손목에서 옷과 손의 경계가 구분되는 크림색 마감입니다.
    hood = [(1.015, .040, .020, 0, -.10), (1.045, .093, .047, 0, -.10), (1.095, .124, .063, 0, -.075), (1.15, .118, .060, 0, -.045), (1.19, .080, .035, 0, -.020)]  # 목 뒤에 내려앉는 접힌 후드의 입체 윤곽입니다.
    parts.append((loft('HoodieHood', hood, 'Coral', segments=48), 'UpperChest'))  # 별도의 후드가 있는 옷으로 읽히도록 목 뒤에 천의 부피를 만듭니다.
    fold = [(-.075, 1.175, .018), (-.110, 1.15, -.050), (-.085, 1.125, -.125), (0, 1.115, -.150), (.085, 1.125, -.125), (.110, 1.15, -.050), (.075, 1.175, .018)]  # 접힌 후드의 양쪽 앞끝과 뒤쪽 가장자리를 잇는 곡선입니다.
    parts.append((tube('HoodieHoodRim', fold, [.017, .022, .024, .024, .024, .022, .017], 'Coral', segments=20), 'UpperChest'))  # 후드 가장자리가 얇은 판이 되지 않도록 둥근 접힘을 만듭니다.
    for side, sign in (('Left', -1), ('Right', 1)):  # 후드티 앞쪽의 두 끈을 제작합니다.
        points = [(sign * x, y, cloth_depth(sign * x, y) + .007) for x, y in ((.041, 1.145), (.043, 1.095), (.046, 1.035), (.052, .985))]  # 끈이 옷 앞면을 따라 짧게 내려오게 합니다.
        parts.append((tube(side + 'HoodieString', points, [.0045] * 4, 'Cream', segments=12), 'Top'))  # 후드티임을 구분할 수 있는 크림색 조임끈입니다.
    rows, columns = 7, 13  # 앞주머니의 부드러운 천 곡면을 구성할 격자입니다.
    pocket_front, pocket_faces = [], []  # 몸판과 별도로 살짝 부푼 주머니 표면입니다.
    for row in range(rows):  # 주머니의 아래쪽에서 위쪽 입구로 이어 갑니다.
        t = row / (rows - 1)  # 주머니 높이 안의 상대 위치입니다.
        y, half_width = .735 + .13 * t, .120 - .045 * t * t - .006 * (1 - t) ** 3  # 아래가 넓고 위쪽 양옆이 기울어진 앞주머니 외곽입니다.
        for column in range(columns):  # 후드티 곡면을 따라 주머니의 가로 방향을 나눕니다.
            u = -1 + 2 * column / (columns - 1)  # 주머니 좌우 안의 위치입니다.
            x = half_width * u  # 해당 높이에서의 실제 가로 위치입니다.
            pocket_front.append((x, y, cloth_depth(x, y) + .007 + .013 * math.sin(math.pi * t) * (1 - u * u)))  # 가운데가 조금 부푸는 실제 천 주머니 표면입니다.
    count = len(pocket_front)  # 주머니 앞면과 안쪽 면을 구분할 정점 수입니다.
    pocket_vertices = pocket_front + [(x, y, z - .006) for x, y, z in pocket_front]  # 가장자리에서 실제 두께가 있는 주머니를 만듭니다.
    for row in range(rows - 1):  # 주머니의 앞면과 안쪽 면을 함께 연결합니다.
        for column in range(columns - 1):  # 격자의 이웃 정점 네 개를 연결합니다.
            a = row * columns + column  # 현재 격자 칸의 시작 정점입니다.
            face = (a, a + 1, a + columns + 1, a + columns)  # 주머니의 부드러운 바깥 곡면입니다.
            pocket_faces.extend((face, tuple(index + count for index in reversed(face))))  # 안쪽 면도 반대 방향으로 닫습니다.
    boundary = list(range(columns)) + [row * columns + columns - 1 for row in range(1, rows)] + [(rows - 1) * columns + column for column in range(columns - 2, -1, -1)] + [row * columns for row in range(rows - 2, 0, -1)]  # 주머니 격자의 실제 바깥 경계입니다.
    pocket_faces.extend((a, b, b + count, a + count) for a, b in zip(boundary, boundary[1:] + boundary[:1]))  # 옆에서 보아도 주머니가 열린 종이 조각처럼 보이지 않게 합니다.
    parts.append((mesh('HoodiePouch', pocket_vertices, pocket_faces, 'Coral'), 'Top'))  # 몸판 앞에 실제로 부착된 후드티의 캥거루 주머니입니다.
    for side, sign in (('Left', -1), ('Right', 1)):  # 양옆 주머니 입구에 짧은 마감선을 넣습니다.
        points = [(sign * x, y, cloth_depth(sign * x, y) + .012) for x, y in ((.077, .852), (.094, .828), (.110, .798))]  # 주머니의 기울어진 양옆 입구를 따라가는 선입니다.
        parts.append((tube(side + 'PouchOpening', points, [.0025] * 3, 'Cream', segments=10), 'Top'))  # 앞주머니의 입구가 몸판에서 구분되도록 얇은 마감을 만듭니다.
    shorts = [loft('ShortsPelvis', [(.59, .104, .081, 0, 0), (.65, .137, .100, 0, 0), (.72, .142, .104, 0, 0), (.77, .131, .094, 0, 0)], 'Ink', segments=40)]  # 후드티 아래에 따로 입은 짙은 남색 반바지의 골반입니다.
    for side, sign in (('Left', -1), ('Right', 1)):  # 무릎 위에서 끝나는 넉넉한 두 바지통을 제작합니다.
        profile = [(.46, .067, .076, sign * .081, 0), (.48, .075, .083, sign * .081, 0), (.55, .079, .086, sign * .081, .003), (.64, .077, .085, sign * .081, 0), (.72, .071, .080, sign * .081, 0)]  # 허벅지에 달라붙지 않고 밑단에 여유가 남는 반바지 패턴입니다.
        shorts.append(loft(side + 'ShortsLeg', profile, 'Ink', segments=40))  # 골반과 이어질 별도의 반바지 통입니다.
        cuff = [(y, radius, radius * 1.10, sign * .081, 0) for y, radius in ((.452, .066), (.461, .075), (.483, .077), (.495, .073))]  # 밑단을 한 번 접은 둥근 반바지 마감입니다.
        parts.append((loft(side + 'ShortsCuff', cuff, 'Denim', segments=40), side + 'UpperLeg'))  # 상의와 다른 옷임을 구분하는 접힌 바지 밑단입니다.
        leg = [(.13, .043, .046, sign * .081, 0), (.28, .048, .052, sign * .081, .006), (.41, .054, .058, sign * .081, .014), (.58, .063, .066, sign * .081, 0), (.67, .066, .067, sign * .081, 0)]  # 긴 바지를 벗은 뒤 반바지 아래로 보이는 기존 체형의 다리 표면입니다.
        parts.append((loft(side + 'LegSkin', leg, 'Skin', segments=32), 'Leg'))  # 반바지 아래의 다리가 비어 보이지 않도록 기존 다리뼈에 연결합니다.
    parts.append((merge_surface('BermudaShorts', shorts, 'Ink', .003), 'Shorts'))  # 골반과 바지통이 연결된 실제 반바지 메시입니다.
    for obj, _ in parts:  # 현재 남자아이 의상에 속한 부위만 표시합니다.
        obj['boy_outfit'] = True  # 이후 의상 교체에서도 얼굴과 손 및 신발을 제외한 같은 범위를 찾게 합니다.
    return parts  # 기존 얼굴과 손 및 신발에 새 의상과 드러나는 다리만 연결합니다.


def make_body(color, girl=False, shoulders_only=False):  # 몸 전체 또는 어깨와 소매가 연결된 상의만 필요한 범위로 제작합니다.
    parts = [] if girl else make_boy_outfit(shoulders_only=shoulders_only)  # 남자아이는 새 후드티와 반바지를 기본 의상으로 사용합니다.
    if girl:  # 여자아이의 기존 의상 형태와 색상은 유지합니다.
        profile = [(.70, .126, .085, 0, 0), (.72, .137, .094, 0, .001), (.76, .143, .100, 0, .002), (.83, .146, .102, 0, .003), (.91, .149, .103, 0, .003), (.99, .149, .100, 0, .002), (1.045, .142, .091, 0, .001), (1.09, .115, .073, 0, 0), (1.135, .073, .049, 0, 0), (1.17, .048, .040, 0, 0), (1.19, .046, .039, 0, 0)]  # 여자아이에게 이미 연결된 기존 상의 윤곽입니다.
        parts.append((make_shirt(profile, color, girl), 'Top'))  # 여자아이의 기존 반소매와 몸판 연결을 유지합니다.
    if shoulders_only:  # 어깨 수정에서는 기존 손과 팔다리 및 하의 표면을 다시 만들지 않습니다.
        shirt = parts[0][0]  # 방금 만든 연속된 상의만 기존 캐릭터 비율에 맞춥니다.
        for vertex in shirt.data.vertices:  # 기존 팔뼈와 같은 몸 비율과 팔 길이를 적용합니다.
            vertex.co = bp(body_point((vertex.co.x, vertex.co.z, -vertex.co.y)))  # 원래 상의와 동일한 바인드 좌표계로 변환합니다.
        shirt.data.update()  # 변경한 실제 상의 표면을 스킨 연결에 반영합니다.
        return parts  # 현재 요청의 어깨와 소매 연결부만 기존 골격에 교체합니다.
    for side, sign in (('Left', -1), ('Right', 1)):  # 어깨와 팔꿈치를 따라 양쪽 소매를 만듭니다.
        if girl:  # 반소매 아래에는 피부로 된 아래팔을 드러냅니다.
            parts.append((loft(side + 'ArmSkin', [(sign * x, radius, radius * .94, 1.085, 0) for x, radius in ((.30, .045), (.36, .047), (.41, .046), (.50, .042), (.586, .037))], 'Skin', axis='X', segments=40), 'Arm'))  # 소매 아래에서 손바닥까지 손목만 급격히 가늘어지지 않도록 둥글게 연결합니다.
            cuff = [(sign * (.335 + shift), .048 * factor, .048 * .94 * factor, 1.085, 0) for shift, factor in ((-.016, .90), (-.008, 1.02), (.006, 1.02), (.014, .90))]  # 여자아이의 기존 반소매 마감 위치와 두께를 유지합니다.
            parts.append((loft(side + 'SleeveRib', cuff, 'Cream', axis='X', segments=40, subdivision=1, power=1.0), 'Arm'))  # 기존 반소매의 크림색 시보리입니다.
        make_hand(parts, side, sign)  # 구분되는 손가락을 가진 실제 손입니다.
    if girl:  # 두 번째 친구는 반소매와 단정한 치마로 실루엣을 구분합니다.
        parts.append((loft('ShirtHem', [(.698, .122, .082, 0, 0), (.708, .131, .089, 0, .001), (.720, .139, .096, 0, .001), (.734, .142, .099, 0, .002)], 'Cream', segments=48, subdivision=1, power=1.0), 'Top'))  # 여자아이의 기존 상의 밑단을 유지합니다.
        parts.append((loft('CrewCollar', [(1.161, .047, .040, 0, 0), (1.169, .051, .044, 0, 0), (1.183, .051, .044, 0, 0), (1.192, .045, .038, 0, 0)], 'Cream', segments=40, subdivision=1, power=1.0), 'UpperChest'))  # 여자아이의 기존 옷깃을 유지합니다.
        parts.append((loft('Skirt', [(.535, .160, .103, 0, 0), (.545, .177, .114, 0, 0), (.575, .176, .113, 0, 0), (.63, .161, .104, 0, 0), (.69, .139, .092, 0, 0), (.735, .126, .084, 0, 0), (.76, .123, .082, 0, 0)], 'Denim', segments=48, subdivision=1, power=1.0), 'Hips'))  # 사각 사다리꼴 블록 대신 허리에서 둥글게 퍼지고 끝이 부드럽게 말린 치마를 만듭니다.
    for side, sign in (('Left', -1), ('Right', 1)):  # 다리와 운동화의 연결을 만듭니다.
        if girl:  # 치마 아래에서는 무릎과 종아리를 피부로 표현합니다.
            parts.append((loft(side + 'LegSkin', [(.13, .043, .046, sign * .081, 0), (.28, .048, .052, sign * .081, .006), (.41, .054, .058, sign * .081, .014), (.58, .063, .066, sign * .081, 0), (.67, .066, .067, sign * .081, 0)], 'Skin', segments=32), 'Leg'))  # 치마 아래의 다리를 짧고 통통하게 만들고 무릎의 급격한 굴곡을 줄입니다.
        parts.append((loft(side + 'Socks', [(.125, .046, .050, sign * .081, 0), (.137, .050, .054, sign * .081, 0), (.189, .052, .056, sign * .081, .002), (.205, .048, .052, sign * .081, .002)], 'Cream', segments=40, subdivision=1, power=1.0), side + 'LowerLeg'))  # 발목을 감싸는 양말의 위아래 모서리를 둥글게 연결합니다.
        parts.append((loft(side + 'Outsole', [(.002, .061, .120, sign * .081, .042), (.008, .067, .130, sign * .081, .042), (.025, .067, .130, sign * .081, .042), (.034, .061, .122, sign * .081, .042)], 'Cream', segments=48, subdivision=0, power=1.0), side + 'Foot'))  # 바닥 접지는 유지하면서 사각 모서리가 없는 타원형 밑창을 만듭니다.
        parts.append((loft(side + 'SneakerUpper', [(.028, .059, .120, sign * .081, .042), (.049, .065, .117, sign * .081, .038), (.083, .061, .098, sign * .081, .025), (.115, .051, .068, sign * .081, .005), (.143, .041, .046, sign * .081, -.006), (.160, .036, .038, sign * .081, -.009)], 'Shoe', segments=48, power=1.0), side + 'Foot'))  # 발끝에서 발등과 뒤축까지 둥글게 이어지는 운동화 표면을 만듭니다.
        for lace in range(3):  # 적은 수의 끈으로 발등 방향을 표현합니다.
            y, z = .127 - lace * .011, .025 + lace * .025  # 실제 갑피의 발등 경사를 따릅니다.
            parts.append((tube(side + 'Lace_' + str(lace), [(sign * .081 - .028, y, z), (sign * .081, y + .003, z + .002), (sign * .081 + .028, y, z)], [.0018] * 3, 'White', segments=8, subdivision=0), side + 'Foot'))  # 작은 운동화 끈입니다.
    for obj, _ in parts:  # 몸과 옷 및 손발을 같은 SD 비율로 실제 변형합니다.
        for vertex in obj.data.vertices:  # 모델 정점과 바인드 골격이 같은 크기를 사용하게 합니다.
            vertex.co = bp(body_point((vertex.co.x, vertex.co.z, -vertex.co.y)))  # 골격과 같은 변환으로 몸의 높이와 팔 길이를 줄이고 폭과 두께를 확보합니다.
        obj.data.update()  # 변경한 실제 표면을 다음 골격 연결에 반영합니다.
    return parts  # 얼굴과 골격을 교체하지 않고도 새 몸 표면만 기존 캐릭터에 연결할 수 있게 합니다.


def make_character(name, color, position, girl=False):  # 전체 모델 제작에서는 기존 이름과 계층에 몸과 얼굴을 함께 연결합니다.
    root = asset(name, position)  # 기존 모델 이름과 원점 기준을 유지합니다.
    parts = make_body(color, girl)  # 몸만 수정할 때와 동일한 둥근 체형을 사용합니다.
    make_head(parts, girl)  # 전체 모델 제작에서만 얼굴과 머리카락을 새로 만듭니다.
    return root, skin_character(parts, root)  # 기존 이름과 부모 관계를 가진 골격에 완성 모델을 연결합니다.


def make_phone():  # 첫 상황에서 손에 들고 조사할 휴대전화를 제작합니다.
    asset('PocketPhone', (-1.9, .18, 5.0), -15)  # 손에 연결하기 쉬운 독립 소품입니다.
    rounded_box('Case', (0, .075, 0), (.082, .15, .016), 'Coral', .012)  # 얇은 케이스를 둥근 가장자리로 마감합니다.
    rounded_box('ScreenGlass', (0, .078, .009), (.069, .125, .004), 'Ink', .010)  # 화면과 케이스 사이에 일정한 테두리를 둡니다.
    rounded_box('Screen', (0, .080, .012), (.061, .108, .002), 'Blue', .007)  # 조사할 화면의 앞면을 구분합니다.
    rounded_box('MessageOne', (-.005, .101, .014), (.038, .012, .001), 'Cream', .004)  # 간단한 메시지 모양으로 사용 중인 화면을 표현합니다.
    rounded_box('MessageTwo', (.006, .082, .014), (.035, .012, .001), 'Mint', .004)  # 작은 색면으로 화면 내용을 정리합니다.
    oval('CameraDot', (0, .14, .012), (.003, .003, .001), 'Ink', segments=16, rings=8)  # 위아래를 구분하는 작은 전면 카메라입니다.


def rigid_edges(obj, width=.02):  # 기계 부품의 큰 평면과 작은 가공 모서리를 구분합니다.
    bevel = obj.modifiers.new('작은 가공 모서리', 'BEVEL')  # 차체와 휠의 실루엣을 유지하는 작은 모서리입니다.
    bevel.width, bevel.segments = width, 2  # 면 전체가 부풀어 보이지 않는 폭입니다.
    apply_modifier(obj, bevel)  # 실제 내보낼 정점에 가공 모서리를 반영합니다.
    normal = obj.modifiers.new('부품 평면 법선', 'WEIGHTED_NORMAL')  # 넓은 판재가 둥근 덩어리로 보이지 않게 합니다.
    normal.keep_sharp = True  # 차체와 유리의 면 방향을 유지합니다.
    apply_modifier(obj, normal)  # FBX에도 정리된 법선을 포함합니다.
    return obj  # 실제 제작 부품으로 사용합니다.


def axle_disc(name, radius, depth, material):  # 바퀴축을 중심으로 평평한 휠 부품을 만듭니다.
    vertices = [(x, radius * math.cos(math.tau * index / 40), radius * math.sin(math.tau * index / 40)) for x in (-depth * .5, depth * .5) for index in range(40)]  # 앞뒤 판이 평평한 원형 부품입니다.
    faces = [(index, (index + 1) % 40, (index + 1) % 40 + 40, index + 40) for index in range(40)]  # 원형 부품의 실제 옆면입니다.
    faces.extend([tuple(reversed(range(40))), tuple(index + 40 for index in range(40))])  # 앞뒤를 구체처럼 부풀리지 않고 판으로 닫습니다.
    return rigid_edges(mesh(name, vertices, faces, material), .003)  # 가장자리에만 작은 가공 폭을 줍니다.


def wheel(name, center):  # 타이어와 평평한 금속 휠을 회전 가능한 부품으로 제작합니다.
    vertices, faces = [], []  # 타이어 단면과 둘레의 면입니다.
    profile = [(-.13, .245), (-.125, .315), (-.085, .34), (.085, .34), (.125, .315), (.13, .245)]  # 타이어의 접지면만 둥글고 측면은 납작한 단면입니다.
    for offset, radius in profile:  # 실제 폭을 가진 타이어를 작성합니다.
        for index in range(40):  # 바퀴 둘레를 균일하게 나눕니다.
            angle = math.tau * index / 40  # 차축을 중심으로 한 둘레 방향입니다.
            vertices.append((offset, radius * math.cos(angle), radius * math.sin(angle)))  # 회전 원점이 차축에 있는 표면입니다.
    for row in range(len(profile) - 1):  # 폭 방향 단면을 연결합니다.
        for index in range(40):  # 타이어의 이음새도 닫습니다.
            faces.append((row * 40 + index, row * 40 + (index + 1) % 40, (row + 1) * 40 + (index + 1) % 40, (row + 1) * 40 + index))  # 연속된 고무 표면입니다.
    faces.extend([tuple(reversed(range(40))), tuple((len(profile) - 1) * 40 + index for index in range(40))])  # 타이어의 측면을 닫습니다.
    tire = mesh(name, vertices, faces, 'Ink')  # 바퀴 전체의 회전 루트입니다.
    tire.location = bp(center)  # 차체의 실제 차축 위치로 이동합니다.
    rim = axle_disc(name + '_FlatRim', .218, .286, 'Shoe')  # 구슬 모양 대신 평평한 휠을 사용합니다.
    rim.parent, rim.location = tire, (0, 0, 0)  # 휠이 타이어와 같은 축에서 회전합니다.
    cap = axle_disc(name + '_AxleCap', .055, .296, 'Glass')  # 작은 축 덮개도 평평한 기계 부품입니다.
    cap.parent, cap.location = tire, (0, 0, 0)  # 중심 덮개를 바퀴에 연결합니다.
    return tire  # 기존 차량 회전 제어에서 사용할 수 있는 부품입니다.


def make_car():  # 판재와 창문 및 휠 아치가 읽히는 작은 카툰 해치백입니다.
    asset('SoftCar', (-5.2, 0, -1.8), 90)  # 기존 모델 이름과 정면 방향을 유지합니다.
    sections = [(-1.95, .61, .32, .72), (-1.76, .86, .25, .86), (-1.15, .91, .23, .88), (.55, .90, .23, .86), (1.64, .82, .27, .77), (1.94, .59, .32, .68)]  # 범퍼와 펜더 및 보닛의 길이별 단면입니다.
    vertices, faces = [], []  # 차체의 직선과 절곡 면을 직접 작성합니다.
    for z, width, bottom, top in sections:  # 타원 대신 큰 면과 모서리가 있는 차체 단면입니다.
        outline = [(-width * .86, bottom), (-width, bottom + .09), (-width, top - .07), (-width * .90, top), (width * .90, top), (width, top - .07), (width, bottom + .09), (width * .86, bottom)]  # 판재의 바닥과 옆면 및 어깨선을 구분합니다.
        vertices.extend((x, y, z) for x, y in outline)  # 길이에 따라 폭과 높이가 달라지는 실제 차체입니다.
    for row in range(len(sections) - 1):  # 단면을 차체 길이 방향으로 연결합니다.
        for index in range(8):  # 보닛과 옆면 및 하부를 구분하는 면입니다.
            faces.append((row * 8 + index, row * 8 + (index + 1) % 8, (row + 1) * 8 + (index + 1) % 8, (row + 1) * 8 + index))  # 차체가 하나로 이어지게 합니다.
    faces.extend([tuple(reversed(range(8))), tuple((len(sections) - 1) * 8 + index for index in range(8))])  # 앞뒤 판재로 차체를 닫습니다.
    body = rigid_edges(mesh('HatchbackBody', vertices, faces, 'Coral'), .026)  # 큰 면을 유지하는 작은 곡면 모서리입니다.
    for sign in (-1, 1):  # 앞뒤 네 바퀴의 실제 공간을 만듭니다.
        for z in (-1.18, 1.13):  # 기존 회전 부품의 차축 위치입니다.
            bpy.ops.mesh.primitive_cylinder_add(vertices=40, radius=.375, depth=.55, location=bp((sign * .86, .35, z)), rotation=(0, math.pi * .5, 0))  # 휠 아치를 만드는 제작용 절삭 표면입니다.
            cutter = bpy.context.object  # 절삭 도구는 결과 모델에 포함하지 않습니다.
            modifier = body.modifiers.new('휠 아치', 'BOOLEAN')  # 차체에 실제 바퀴 공간을 만듭니다.
            modifier.operation, modifier.solver, modifier.object = 'DIFFERENCE', 'EXACT', cutter  # 해당 부분만 차체에서 제거합니다.
            apply_modifier(body, modifier)  # 실제 차체 정점에 휠 아치를 반영합니다.
            bpy.data.objects.remove(cutter, do_unlink=True)  # 제작 도구를 제거합니다.
            wheel(('Left' if sign < 0 else 'Right') + ('RearWheel' if z < 0 else 'FrontWheel'), (sign * .86, .35, z))  # 독립적인 회전 부품을 연결합니다.
    profile = [(-1.0, .84, .70), (-.67, 1.46, .60), (.34, 1.46, .60), (.92, .83, .70)]  # 해치백의 뒤 유리와 지붕 및 앞 유리의 경사입니다.
    vertices = [(sign * width, y, z) for sign in (-1, 1) for z, y, width in profile]  # 좌우 차체 폭이 지붕에서 줄어드는 실제 캐빈입니다.
    faces = [(0, 1, 2, 3), (7, 6, 5, 4), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)]  # 지붕과 앞뒤 경사 및 측면을 연결합니다.
    rigid_edges(mesh('CabinPanels', vertices, faces, 'Cream'), .018)  # 둥근 덩어리 대신 경사가 분명한 캐빈입니다.
    for sign in (-1, 1):  # 실제 캐빈 판재에 맞는 좌우 창문입니다.
        side = 'Left' if sign < 0 else 'Right'  # 부위의 좌우 이름입니다.
        panes = [('RearWindow', [(sign * .680, 1.00, -.83), (sign * .623, 1.36, -.60), (sign * .623, 1.36, -.06), (sign * .680, 1.00, -.06)]), ('FrontWindow', [(sign * .680, 1.00, .04), (sign * .623, 1.36, .04), (sign * .623, 1.36, .29), (sign * .680, 1.00, .66)])]  # 가운데 기둥을 남긴 두 장의 유리입니다.
        for name, points in panes:  # 유리는 차체 면에 밀착한 색면입니다.
            mesh(side + name, points, [tuple(range(4)) if sign > 0 else (3, 2, 1, 0)], 'Glass', smooth=False)  # 구형 유리 부품을 덧붙이지 않습니다.
        rounded_box(side + 'DoorHandle', (sign * .906, .73, -.16), (.018, .038, .16), 'Cream', .004)  # 납작한 문 손잡이입니다.
        rounded_box(side + 'Mirror', (sign * .80, .99, .64), (.15, .11, .17), 'Coral', .018)  # 작은 판형 거울입니다.
        rounded_box(side + 'Headlight', (sign * .40, .565, 1.949), (.26, .13, .018), 'White', .006)  # 제조된 램프처럼 읽히는 납작한 전조등입니다.
        rounded_box(side + 'TailLight', (sign * .42, .60, -1.956), (.20, .13, .018), 'RedLight', .006)  # 후면의 작은 사각 램프입니다.
    mesh('Windshield', [(-.58, .944, .824), (.58, .944, .824), (.50, 1.344, .454), (-.50, 1.344, .454)], [(0, 1, 2, 3)], 'Glass', smooth=False)  # 앞 유리 경사에 밀착한 실제 창문입니다.
    mesh('RearWindshield', [(-.62, 1.00, -.919), (-.55, 1.34, -.740), (.55, 1.34, -.740), (.62, 1.00, -.919)], [(0, 1, 2, 3)], 'Glass', smooth=False)  # 후면 경사에 맞는 뒤 유리입니다.
    rounded_box('FrontBumper', (0, .34, 1.96), (1.20, .11, .06), 'Cream', .012)  # 두께가 작고 단단한 범퍼입니다.
    rounded_box('RearBumper', (0, .34, -1.98), (1.24, .11, .06), 'Cream', .012)  # 후면에도 같은 제조물의 형태를 사용합니다.
    rounded_box('FrontGrille', (0, .46, 1.957), (.46, .08, .016), 'Ink', .002)  # 얼굴 모양 장식 대신 간결한 차량 그릴입니다.


def roof(name, width, base, peak, depth, material):  # 두꺼운 처마와 네 개의 경사면을 가진 닫힌 입체 지붕을 만듭니다.
    half_x, half_z = width * .52, depth * .5  # 벽 바깥으로 조금 돌출되는 처마의 실제 반폭입니다.
    long_half, short_half = max(half_x, half_z), min(half_x, half_z)  # 건물의 긴 방향과 지붕 경사를 결정하는 짧은 방향을 구분합니다.
    eave = base + .22  # 벽의 윗부분을 덮는 처마에 눈에 보이는 두께를 줍니다.
    ridge_height = max(peak, eave + short_half * .62)  # 넓은 학교 건물에서도 경사가 납작해지지 않도록 짧은 폭을 기준으로 높이를 정합니다.
    ridge_half = max(.16, long_half - short_half * .80)  # 용마루를 건물의 긴 방향으로 놓고 양끝에 경사면을 남깁니다.
    corners = [(-half_x, eave, -half_z), (half_x, eave, -half_z), (half_x, eave, half_z), (-half_x, eave, half_z)]  # 네 처마 모서리를 같은 높이에 맞춥니다.
    ridge = [(-ridge_half, ridge_height, 0), (ridge_half, ridge_height, 0)] if half_x >= half_z else [(0, ridge_height, -ridge_half), (0, ridge_height, ridge_half)]  # 가로와 세로 중 긴 축을 따라 용마루를 배치합니다.
    vertices = corners + ridge + [(x, base, z) for x, _, z in corners]  # 지붕 위와 처마 아래를 함께 구성하여 열린 틈을 없앱니다.
    faces = [(0, 1, 5, 4), (1, 2, 5), (2, 3, 4, 5), (3, 0, 4)] if half_x >= half_z else [(0, 1, 4), (1, 2, 5, 4), (2, 3, 5), (3, 0, 4, 5)]  # 삼각 경사면과 긴 경사면이 용마루에서 정확히 만나게 합니다.
    faces.extend((index, index + 6, (index + 1) % 4 + 6, (index + 1) % 4) for index in range(4))  # 처마의 네 옆면을 두께가 있는 띠로 닫습니다.
    faces.append((6, 9, 8, 7))  # 지붕 아래도 막아 비스듬한 시점에서 속이 비어 보이지 않게 합니다.
    obj = mesh(name, vertices, faces, material, smooth=False)  # 큰 경사면의 방향이 분명한 하나의 닫힌 지붕 메시입니다.
    obj.data.materials.append(MATERIALS['Cream'])  # 처마 옆면에 밝은 마감색을 연결합니다.
    for face in list(obj.data.polygons)[4:]:  # 경사면과 두꺼운 처마의 경계를 색상으로 구분합니다.
        face.material_index = 1  # 처마 옆면과 아랫면에 크림색 마감을 적용합니다.
    bevel = obj.modifiers.new('지붕과 처마 모서리', 'BEVEL')  # 판자처럼 날카로운 모서리를 작게 둥글립니다.
    bevel.width, bevel.segments = .035, 3  # 지붕 경사를 유지하는 범위에서 모서리에 실제 두께감을 줍니다.
    apply_modifier(obj, bevel)  # 내보내는 FBX에도 완성된 처마와 경사면을 저장합니다.
    return obj  # 학교와 주변 건물에서 같은 입체 지붕 구조를 사용합니다.


def window_front(name, x, y, front, width=.85, height=1.05):  # 학교와 거리 건물이 공유하는 둥근 창문 디자인입니다.
    rounded_box(name + '_Frame', (x, y, front), (width + .17, height + .17, .16), 'Cream', .095)  # 밝은 창틀로 큰 벽면의 리듬을 만듭니다.
    rounded_box(name + '_Glass', (x, y, front + .092), (width, height, .045), 'Glass', .09)  # 반사 대신 청회색 색면으로 유리를 표현합니다.
    rounded_box(name + '_Sill', (x, y - height * .5 - .06, front + .10), (width + .27, .10, .24), 'Cream', .035)  # 창 아래쪽에 작은 두께를 줍니다.


def label(name, text, center, size, material):  # 학교 표지를 게임용 입체 글자로 만듭니다.
    data = bpy.data.curves.new(name, 'FONT')  # 편집 가능한 글자 윤곽입니다.
    data.body, data.align_x, data.size = text, 'CENTER', size  # 표지의 중심과 크기를 맞춥니다.
    data.font = bpy.data.fonts.load(str(AREA / 'Fonts' / 'Jua-Regular.ttf'))  # 프로젝트의 둥근 한글 서체를 사용합니다.
    data.extrude, data.bevel_depth = .008, .002  # 글자에도 작은 입체 두께를 줍니다.
    obj = bpy.data.objects.new(ACTIVE_ROOT.name + '_' + name, data)  # 글자를 해당 건물의 부위로 만듭니다.
    bpy.context.scene.collection.objects.link(obj)  # 원본에 글자를 등록합니다.
    obj.location, obj.rotation_euler = bp(center), (math.pi * .5, 0, 0)  # Unity의 정면을 바라보도록 글자를 세웁니다.
    obj.parent, obj['export_name'] = ACTIVE_ROOT, name  # 건물과 간판이 함께 움직이게 합니다.
    data.materials.append(MATERIALS[material])  # 공통 팔레트의 색을 적용합니다.
    bpy.ops.object.select_all(action='DESELECT')  # 글자 이외의 부위는 변환하지 않습니다.
    obj.select_set(True)  # 해당 글자를 실제 메시로 바꿉니다.
    bpy.context.view_layer.objects.active = obj  # 변환 명령의 대상을 지정합니다.
    bpy.ops.object.convert(target='MESH')  # Unity에서 서체 설치 없이 입체 간판을 사용할 수 있게 합니다.


def make_school():  # 게임 배경의 중심이 되는 학교 외관입니다.
    asset('SoftSchool', (1.0, 0, 15), 180)  # 출입구가 도로를 향하는 원본 배치입니다.
    rounded_box('SchoolWalls', (0, 2.7, 0), (15.2, 5.4, 5.4), 'School', .30)  # 따뜻한 색상의 둥근 외벽입니다.
    rounded_box('Foundation', (0, .15, 0), (15.5, .30, 5.65), 'Cream', .10)  # 건물과 바닥의 접점을 정리합니다.
    roof('SchoolRoof', 15.8, 5.25, 6.7, 5.95, 'Roof')  # 멀리서도 학교가 구분되는 큰 곡선 지붕입니다.
    rounded_box('EntryFrame', (0, 1.24, 2.74), (2.28, 2.48, .29), 'Cream', .23)  # 중앙 출입구를 두꺼운 둥근 테두리로 강조합니다.
    rounded_box('EntryDoor', (0, 1.18, 2.91), (1.90, 2.22, .10), 'Glass', .20)  # 단순한 색면으로 표현한 학교 출입문입니다.
    rounded_box('DoorDivider', (0, 1.18, 2.98), (.07, 2.05, .045), 'Cream', .025)  # 양문형 입구를 가는 선으로 나눕니다.
    rounded_box('EntranceCanopy', (0, 2.68, 3.12), (3.00, .20, 1.15), 'Roof', .09)  # 입구 위를 짧은 차양으로 마감합니다.
    rounded_box('SchoolSign', (0, 3.30, 2.80), (2.40, .63, .17), 'Cream', .13)  # 큰 벽면에서 학교 표지를 구분합니다.
    label('SchoolName', '학교', (0, 3.09, 2.91), .40, 'Roof')  # 둥근 서체의 입체 학교 표지입니다.
    for floor, height in enumerate((1.50, 4.15)):  # 두 층으로 읽히는 창문 구성을 만듭니다.
        for column, x in enumerate((-6.0, -4.0, -2.0, 2.0, 4.0, 6.0)):  # 중앙 입구와 시계 부분을 비워 둡니다.
            window_front(f'Window_{floor}_{column}', x, height, 2.73, 1.0, 1.18)  # 같은 디자인의 창문을 일정한 간격으로 배치합니다.
    oval('ClockRim', (0, 4.54, 2.83), (.46, .46, .075), 'Roof')  # 학교 입면의 중심에 둥근 시계를 둡니다.
    oval('ClockFace', (0, 4.54, 2.91), (.37, .37, .025), 'Cream')  # 큰 숫자 없이도 시계 형태가 읽히게 합니다.
    tube('ClockHands', [(0, 4.81, 2.95), (0, 4.54, 2.95), (.19, 4.54, 2.95)], [.023, .023, .023], 'Ink', subdivision=0)  # 간결한 두 바늘로 시계를 완성합니다.


def make_building(name, color, width, height, position, shop=False):  # 형태와 높이가 다른 거리 배경 건물을 제작합니다.
    asset(name, position, 180)  # 출입구가 도로를 향하게 배치합니다.
    rounded_box('Walls', (0, height * .5, 0), (width, height, 3.7), color, .20)  # 전체 외벽에 같은 둥근 모서리 폭을 적용합니다.
    roof('Roof', width + .30, height - .08, height + .92, 4.02, 'Roof' if color != 'Blue' else 'Coral')  # 지붕 높이와 색상으로 실루엣을 구분합니다.
    rounded_box('BaseTrim', (0, .13, 0), (width + .13, .26, 3.82), 'Cream', .07)  # 주변 건물의 바닥 마감을 통일합니다.
    rounded_box('DoorFrame', (-width * .24, 1.0, 1.89), (1.0, 2.0, .16), 'Cream', .13)  # 둥근 입구 테두리입니다.
    rounded_box('Door', (-width * .24, .97, 2.0), (.78, 1.77, .05), 'Glass', .09)  # 문의 안쪽은 같은 창문색을 사용합니다.
    if shop:  # 상가는 넓은 창과 차양으로 구분합니다.
        window_front('ShopWindow', width * .20, 1.28, 1.90, width * .38, 1.20)  # 1층의 큰 진열창입니다.
        rounded_box('ShopCanopy', (0, 2.44, 2.14), (width + .18, .26, .85), 'Cream', .11)  # 간결하고 큰 형태의 차양입니다.
        for index in range(5):  # 작은 무늬 대신 굵은 색띠를 사용합니다.
            rounded_box(f'CanopyStripe_{index}', ((index - 2) * width / 5, 2.445, 2.15), (width / 10, .27, .87), 'Coral', .07)  # 다른 소품과 같은 산호색으로 연결합니다.
    for row in range(max(1, int((height - 2.1) / 1.7))):  # 건물 높이에 맞는 상층 창문 수입니다.
        for column, x in enumerate((-width * .25, width * .25)):  # 배경 실루엣이 복잡해지지 않는 창문 배치입니다.
            window_front(f'UpperWindow_{row}_{column}', x, 3.2 + row * 1.65, 1.89, .73, .88)  # 학교와 같은 창문 형태를 공유합니다.


def make_street():  # 도로와 보도 및 지형을 반복 배치 가능한 독립 모듈로 제작합니다.
    asset('RoadStraight')  # 길이 10m와 차도 폭 7m의 직선 도로입니다.
    rounded_box('RoadSurface', (0, -.08, 0), (10, .16, 7), 'Road', .025)  # 지면 높이를 원점에 맞춘 실제 도로 표면입니다.
    asset('SidewalkStraight', (0, 0, 5.0))  # 도로 양쪽에 반복 배치할 수 있는 인도입니다.
    rounded_box('SidewalkSurface', (0, .075, 0), (10, .15, 3.0), 'Pavement', .025)  # 경계석과 연결되는 보도 높이입니다.
    for index in range(10):  # 큰 간격의 줄눈으로 보행 구역을 표현합니다.
        rounded_box(f'PavingJoint_{index}', (index - 4.5, .151, 0), (.012, .001, 2.91), 'Cream', .0003)  # 작은 반복 무늬 대신 간결한 분할선을 사용합니다.
    asset('CurbSegment', (0, 0, 3.5))  # 도로와 인도를 구분하는 길이 2m의 경계석입니다.
    rounded_box('Curb', (0, .075, 0), (2.0, .15, .22), 'Cream', .025)  # 윗면과 끝을 둥글게 마감합니다.
    asset('CrosswalkStripes', (0, .003, 0))  # 차도를 건너는 흰 줄을 하나의 모듈로 만듭니다.
    for index in range(8):  # 기존 차도 폭에 맞춘 횡단보도 패턴입니다.
        rounded_box(f'Stripe_{index}', (0, 0, (index - 3.5) * .84), (3.8, .006, .43), 'White', .002)  # 얇고 읽기 쉬운 노면 표면입니다.
    asset('RoadCenterLines', (-10, .004, 0))  # 직선 도로에 사용할 이중 중앙선입니다.
    for sign in (-1, 1):  # 두 개의 노란 선을 같은 간격으로 배치합니다.
        rounded_box('CenterLine_' + str(sign), (0, 0, sign * .12), (10, .005, .08), 'Yellow', .002)  # 노면에 밀착하는 얇은 색면입니다.
    asset('RoadStopLine', (-3.0, .004, -1.8))  # 횡단보도 앞 차량 정지선을 별도로 배치합니다.
    rounded_box('StopLine', (0, 0, 0), (.32, .005, 3.15), 'White', .002)  # 한 차로를 가로지르는 정지선입니다.
    asset('GrassGround', (0, 0, 11.0))  # 학교와 주변 건물 아래에 놓을 지면입니다.
    rounded_box('GrassTile', (0, -.08, 0), (20, .16, 14), 'Grass', .07)  # 거리 바깥의 초록 지면도 개별 메시로 제공합니다.
    asset('SchoolForecourt', (1.0, 0, 9.5))  # 인도와 학교 출입구를 연결할 앞마당입니다.
    rounded_box('Forecourt', (0, .03, 0), (10, .06, 5.0), 'Pavement', .025)  # 출입구 앞의 넓은 보행 구역을 실제 표면으로 만듭니다.
    asset('SoftHill', (-19, 0, 25))  # 원경을 자연스럽게 마감할 둥근 지형입니다.
    make_hill_mesh()  # 지면과 능선이 이어지는 실제 카툰 지형으로 제작합니다.


def make_pole():  # 첫 상황에서 부딪히거나 피하게 되는 전봇대입니다.
    asset('SoftUtilityPole', (-3.4, .15, 4.0))  # 기둥의 바닥 중심을 모델 배치 기준으로 사용합니다.
    loft('Pole', [(0, .18, .18, 0, 0), (.16, .18, .18, 0, 0), (3.7, .12, .12, 0, 0), (4.15, .115, .115, 0, 0), (4.20, .045, .045, 0, 0)], 'Pavement', subdivision=1)  # 위로 갈수록 좁아지는 실제 기둥 곡면입니다.
    rounded_box('Base', (0, .12, 0), (.48, .24, .48), 'Cream', .08)  # 다른 거리 소품과 같은 둥근 받침입니다.
    for index in range(5):  # 위험 대상을 멀리서도 구분할 수 있게 합니다.
        loft(f'SafetyBand_{index}', [(.38 + index * .12, .183, .183, 0, 0), (.48 + index * .12, .182, .182, 0, 0)], 'Yellow' if index % 2 == 0 else 'Ink', segments=32, subdivision=0)  # 기둥 표면을 따라 이어지는 안전띠입니다.
    rounded_box('CrossArm', (0, 3.72, 0), (1.12, .14, .19), 'Pavement', .06)  # 전봇대의 상단 실루엣을 만드는 가로대입니다.
    for sign in (-1, 1):  # 전선 연결부를 간결하게 표현합니다.
        oval('Insulator_' + str(sign), (sign * .39, 3.91, 0), (.10, .17, .10), 'Cream')  # 날카롭지 않은 둥근 연결 부품입니다.
    rounded_box('Transformer', (.25, 3.18, -.03), (.42, .58, .39), 'Glass', .13)  # 측면 설비도 같은 둥근 조형으로 표현합니다.


def signal_person(name, y, walking, material):  # 정지와 보행 신호의 그림을 켜고 끌 수 있는 부품으로 만듭니다.
    group = bpy.data.objects.new(ACTIVE_ROOT.name + '_' + name, None)  # 신호 상태를 한 번에 전환할 그룹입니다.
    bpy.context.scene.collection.objects.link(group)  # 그룹을 신호등 모델에 등록합니다.
    group.parent, group['export_name'] = ACTIVE_ROOT, name  # 신호등 안에서 빨간 표시와 초록 표시를 구분합니다.
    previous = set(ACTIVE_ROOT.children)  # 현재 그림 이전의 부품은 그룹 변경에서 제외합니다.
    oval(name + '_Head', (0, y + .115, .182), (.037, .037, .009), material, segments=20, rings=10)  # 작은 머리 그림입니다.
    tube(name + '_Body', [(0, y + .062, .182), (0, y - .04, .182)], [.025, .023], material, subdivision=0)  # 먼 거리에서도 읽히는 굵기의 몸통입니다.
    for sign in (-1, 1):  # 두 팔과 두 다리로 신호 그림을 만듭니다.
        hand = (sign * .085, y + (.025 if walking and sign > 0 else -.065), .182)  # 보행 자세는 팔 방향을 다르게 합니다.
        tube(name + '_Arm_' + str(sign), [(0, y + .04, .182), hand], [.015, .015], material, subdivision=0)  # 두꺼운 선으로 팔을 표현합니다.
        foot = (sign * (.075 if walking else .038), y - .15, .182)  # 정지와 보행의 다리 벌어짐을 구분합니다.
        tube(name + '_Leg_' + str(sign), [(0, y - .03, .182), foot], [.017, .017], material, subdivision=0)  # 다리도 같은 그림 두께를 유지합니다.
    for child in set(ACTIVE_ROOT.children) - previous:  # 방금 만든 그림 부품만 신호 그룹으로 옮깁니다.
        child.parent = group  # 기존 신호 제어기에 그룹을 연결할 수 있는 구조입니다.


def make_signal():  # 두 번째와 세 번째 상황이 공유할 보행자 신호등입니다.
    asset('SoftPedestrianSignal', (2.4, .15, -4.5))  # 바닥의 기둥 중심을 배치 기준으로 둡니다.
    loft('Post', [(0, .09, .09, 0, 0), (.10, .09, .09, 0, 0), (2.65, .07, .07, 0, 0)], 'Glass', segments=24, subdivision=0)  # 단순하고 가느다란 신호등 기둥입니다.
    rounded_box('Base', (0, .08, 0), (.30, .16, .30), 'Cream', .06)  # 지면에 닿는 둥근 받침입니다.
    rounded_box('Housing', (0, 2.36, 0), (.54, 1.07, .30), 'Ink', .15)  # 두 신호를 감싸는 부드러운 외함입니다.
    rounded_box('Hood', (0, 2.91, .08), (.64, .13, .45), 'Glass', .06)  # 위쪽의 작은 차양입니다.
    for name, y, color, walking in (('RedSignal', 2.59, 'RedLight', False), ('GreenSignal', 2.13, 'GreenLight', True)):  # 켜고 끌 그림을 별도 계층에 제작합니다.
        rounded_box(name + '_Panel', (0, y, .162), (.39, .40, .025), 'Glass', .09)  # 그림 뒤의 어두운 표시판입니다.
        signal_person(name, y, walking, color)  # 상태별로 구분 가능한 신호 그림입니다.


def make_hill_mesh():  # 지면에서 이어지는 능선과 삼각 면을 가진 카툰 지형입니다.
    vertices, faces = [], []  # 실제 지면 격자와 면을 보관합니다.
    for row in range(13):  # 언덕 앞뒤의 높이를 나누어 설계합니다.
        z = -3.2 + 6.4 * row / 12  # 지형의 깊이 방향 좌표입니다.
        for column in range(17):  # 언덕 폭 방향의 격자입니다.
            x = -4.5 + 9.0 * column / 16  # 지형의 가로 좌표입니다.
            edge = max(0, 1 - (x / 4.5) ** 2) * max(0, 1 - (z / 3.2) ** 2)  # 바깥 가장자리는 지면 높이로 이어집니다.
            y = edge * (1.65 + .95 * math.exp(-((x - .85) / 1.8) ** 2 - ((z - .35) / 2.0) ** 2))  # 서로 다른 두 능선이 이어지는 지형입니다.
            vertices.append((x, y, z))  # 부푼 캡슐이 아닌 지면 위의 높이 표면입니다.
    for row in range(12):  # 지면 격자의 이웃한 칸을 연결합니다.
        for column in range(16):  # 큰 색면이 읽히는 삼각형 지형입니다.
            a = row * 17 + column  # 현재 격자의 시작 정점입니다.
            faces.extend(((a, a + 17, a + 18), (a, a + 18, a + 1)))  # 지형의 경사 방향이 면으로 읽히게 합니다.
    obj = mesh('TerrainRidge', vertices, faces, 'Grass', smooth=False)  # 능선의 평면을 유지하는 카툰 지형입니다.
    obj.data.materials.append(MATERIALS['LeafLight'])  # 일부 경사를 밝은 색면으로 구분합니다.
    for face in obj.data.polygons:  # 자연스러운 높이 구간을 큰 색면으로 나눕니다.
        face.material_index = 1 if face.center.x > .5 and face.center.z > .6 else 0  # 능선의 양쪽을 같은 색 덩어리로 합치지 않습니다.


def make_greenery():  # 식물과 구름은 명확한 외곽선과 넓은 색면으로 제작합니다.
    asset('RoundTree', (-10.0, 0, 9.0))  # 기존 나무 모델 참조를 유지합니다.
    trunk = [(-.16, 0), (-.13, 1.56), (-.40, 1.91), (-.33, 2.05), (-.08, 1.80), (.02, 2.39), (.15, 2.39), (.16, 1.77), (.43, 2.05), (.51, 1.92), (.16, 1.46), (.17, 0)]  # 줄기와 두 갈래 가지의 실루엣입니다.
    extrude_outline('TreeTrunkAndBranches', trunk, .24, 'Bark', .012)  # 굵기가 다른 판형 줄기로 나무 구조를 표현합니다.
    crown = [(-.85, 1.58), (-1.10, 1.94), (-.96, 2.25), (-.63, 2.42), (-.66, 2.73), (-.34, 3.02), (-.12, 3.35), (.22, 3.43), (.55, 3.17), (.81, 2.97), (.77, 2.59), (1.08, 2.34), (1.02, 1.92), (.71, 1.65), (.26, 1.54)]  # 잎 덩어리 대신 큰 잎 윤곽이 읽히는 그래픽 실루엣입니다.
    obj = extrude_outline('GraphicTreeCrown', crown, .88, 'Leaf', .018)  # 앞면과 옆면이 구분되는 입체 수관입니다.
    for face in obj.data.polygons:  # 잎의 넓은 면이 비누처럼 둥글게 보이지 않게 합니다.
        face.use_smooth = False  # 형태의 평면과 작은 모서리를 유지합니다.
    asset('SoftHedge', (9.3, 0, 8.0))  # 기존 관목 모델 참조를 유지합니다.
    rounded_box('TrimmedHedge', (0, .40, 0), (2.10, .80, .90), 'LeafLight', .025)  # 학교 앞의 정돈된 생울타리 형태입니다.
    asset('SchoolFence', (-7.0, 0, 7.0))  # 반복 배치하는 울타리입니다.
    for index in range(5):  # 개별 판재의 윤곽을 구분합니다.
        x = (index - 2) * .40  # 울타리 판재의 일정한 간격입니다.
        outline = [(x - .105, 0), (x - .105, 1.0), (x, 1.13), (x + .105, 1.0), (x + .105, 0)]  # 상단이 작게 뾰족한 판재의 외곽입니다.
        extrude_outline(f'FenceBoard_{index}', outline, .12, 'Cream', .006)  # 끝을 전부 둥근 캡슐로 만들지 않습니다.
    for y in (.28, .77):  # 두 가로대로 판재를 잇습니다.
        rounded_box('Rail_' + str(y), (0, y, -.07), (2.04, .13, .12), 'Cream', .006)  # 목재의 평면이 남는 얇은 가공 모서리입니다.
    asset('SoftCloud', (-10.0, 8.5, 22.0))  # 기존 구름 모델 참조를 유지합니다.
    outline = [(-1.50, 0), (-1.45, .25), (-1.25, .40), (-.96, .38), (-.83, .63), (-.50, .74), (-.18, .66), (.02, .82), (.36, .84), (.65, .62), (1.00, .56), (1.30, .40), (1.50, .12), (1.40, -.10), (1.10, -.21), (-1.12, -.21)]  # 여러 곡선이 이어지는 가로형 구름 외곽입니다.
    obj = extrude_outline('GraphicCloud', outline, .10, 'White', .014)  # 납작한 그림 같은 구름을 실제 입체 배경으로 만듭니다.
    for face in obj.data.polygons:  # 큰 앞면의 색상을 평면적으로 유지합니다.
        face.use_smooth = False  # 타원 덩어리의 입체 명암을 반복하지 않습니다.


def make_effect_shapes():  # 이펙트는 얇은 그래픽 메시로 제작합니다.
    asset('ImpactStar', (-2.3, .20, 5.0))  # 전봇대 충격용 별입니다.
    outline = [(math.sin(index * math.pi / 5) * (.19 if index % 2 == 0 else .085), .20 + math.cos(index * math.pi / 5) * (.19 if index % 2 == 0 else .085)) for index in range(10)]  # 뚜렷한 별 모양 외곽선입니다.
    extrude_outline('GraphicStar', outline, .012, 'Yellow', .002)  # 작은 두께를 가진 평면 그래픽입니다.
    asset('ImpactRing', (-2.8, .20, 5.0))  # 충격 중심에서 퍼지는 얇은 고리입니다.
    vertices = [(radius * math.cos(math.tau * index / 48), .20 + radius * math.sin(math.tau * index / 48), 0) for radius in (.17, .20) for index in range(48)]  # 굵은 토러스 대신 같은 평면의 안팎 윤곽을 만듭니다.
    faces = [(index, index + 48, (index + 1) % 48 + 48, (index + 1) % 48) for index in range(48)]  # 납작한 원형 띠로 연결합니다.
    mesh('GraphicRing', vertices, faces, 'Cream', smooth=False)  # 부피가 큰 점토 고리처럼 보이지 않는 충격 표현입니다.
    asset('SurpriseMarker', (-3.3, .20, 5.0))  # 놀람 반응의 느낌표입니다.
    extrude_outline('ExclamationStroke', [(-.038, .14), (-.047, .43), (.047, .43), (.038, .14)], .012, 'Yellow', .002)  # 가독성 있는 얇은 세로 획입니다.
    outline = [(.045 * math.cos(math.tau * index / 32), .052 + .045 * math.sin(math.tau * index / 32)) for index in range(32)]  # 느낌표의 작은 점입니다.
    extrude_outline('ExclamationDot', outline, .012, 'Yellow', .002)  # 점도 구슬 대신 납작한 그래픽으로 맞춥니다.
    asset('SweatDrop', (-3.8, .20, 5.0))  # 당황 반응의 물방울입니다.
    outline = [(.034, .28), (-.012, .20), (-.064, .12), (-.071, .065), (-.047, .018), (0, 0), (.052, .022), (.073, .071), (.059, .14)]  # 끝이 한쪽으로 기울어진 물방울의 외곽선입니다.
    extrude_outline('GraphicSweatDrop', outline, .012, 'Blue', .002)  # 얼굴 위의 감정 기호도 같은 평면 그래픽으로 통일합니다.


def save_materials(names=None):  # 전체 제작 또는 현재 수정한 캐릭터에 필요한 재질만 저장합니다.
    target = OUTPUT / 'Materials'  # 새 모델 전용 재질 폴더입니다.
    target.mkdir(parents=True, exist_ok=True)  # FBX 임포트 전에 머티리얼 위치를 만듭니다.
    metadata(OUTPUT, folder=True)  # 모델 묶음 폴더의 Unity 참조입니다.
    metadata(target, folder=True)  # 머티리얼 폴더의 Unity 참조입니다.
    toon_shader_guid = metadata(target / 'CleanCartoon.shader', 'ShaderImporter')  # 같은 모델의 명암을 제어할 실제 URP 셰이더 참조입니다.
    guids = {}  # FBX와 연결할 실제 머티리얼의 식별자입니다.
    selected = COLORS if names is None else {name: COLORS[name] for name in names}  # 얼굴과 헤어 수정에서 배경 재질을 다시 저장하지 않습니다.
    for name, rgb in selected.items():  # 현재 제작 범위에 필요한 실제 재질입니다.
        path = target / ('Soft_' + name + '.mat')  # 기존 게임 재질을 덮어쓰지 않는 에셋 이름입니다.
        guids[name] = metadata(path, 'NativeFormatImporter', 2100000)  # Unity 머티리얼의 고유 참조입니다.
        color = f'{{r: {rgb[0]}, g: {rgb[1]}, b: {rgb[2]}, a: 1}}'  # 실제 표면 색상값입니다.
        text = HEADER + '--- !u!21 &2100000\nMaterial:\n  serializedVersion: 8\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n'  # Unity 머티리얼의 기본 직렬화 필드입니다.
        text += f'  m_Name: Soft_{name}\n  m_Shader: {{fileID: 4800000, guid: {toon_shader_guid}, type: 3}}\n  m_Parent: {{fileID: 0}}\n  m_ModifiedSerializedProperties: 0\n  m_ValidKeywords: []\n  m_InvalidKeywords: []\n  m_LightmapFlags: 4\n  m_EnableInstancingVariants: 1\n  m_DoubleSidedGI: 0\n  m_CustomRenderQueue: -1\n  stringTagMap:\n    RenderType: Opaque\n  disabledShaderPasses: []\n  m_LockedProperties: \n  m_SavedProperties:\n    serializedVersion: 3\n    m_TexEnvs: []\n    m_Ints: []\n    m_Floats:\n'  # 큰 색면과 두 단계 명암을 표현하는 URP 카툰 셰이더에 연결합니다.
        for key, value in {'_Surface': 0, '_Blend': 0, '_SrcBlend': 1, '_DstBlend': 0, '_SrcBlendAlpha': 1, '_DstBlendAlpha': 0, '_ZWrite': 1, '_Cull': 2, '_AlphaClip': 0, '_Cutoff': .5, '_WorkflowMode': 1, '_Metallic': 0, '_ShadeStrength': .90 if name in ('Skin', 'White', 'Shoe') else .80, '_Smoothness': .16, '_SpecularHighlights': 0, '_EnvironmentReflections': 0, '_ReceiveShadows': 1}.items():  # 강한 반사 없이 부드러운 무광 표면을 사용합니다.
            text += f'    - {key}: {value}\n'  # 표면 설정을 기존 Unity 저장 형식으로 기록합니다.
        text += f'    m_Colors:\n    - _BaseColor: {color}\n    - _Color: {color}\n    - _EmissionColor: {{r: 0, g: 0, b: 0, a: 1}}\n  m_BuildTextureStacks: []\n  m_AllowLocking: 1\n'  # 외부 텍스처 없이 실제 모델 색상을 저장합니다.
        write(path, text)  # FBX와 함께 사용할 실제 URP 재질입니다.
    return guids  # 모델의 이름별 재질 매핑에 사용합니다.


def descendants(root):  # 하나의 모델에 속한 전체 계층을 가져옵니다.
    result = [root]  # 내보낼 모델 루트입니다.
    for child in root.children:  # 골격과 바퀴의 하위 부품까지 따라갑니다.
        result.extend(descendants(child))  # 다른 모델은 포함하지 않습니다.
    return result  # 모델 전체의 내보내기 목록입니다.


def export_asset(root, guids):  # 실제 FBX와 Unity의 재질 연결 설정을 저장합니다.
    objects = descendants(root)  # 현재 모델에 속한 실제 부품입니다.
    old_names = [(obj, obj.name) for obj in objects]  # 내보내기 뒤 원본의 구분 이름을 복원합니다.
    for obj in objects:  # 내보내는 모델에서는 간결한 부위 이름을 사용합니다.
        obj.name = obj.get('export_name', obj.name)  # 캐릭터 골격은 기존 상대 경로 이름을 유지합니다.
    bpy.ops.object.select_all(action='DESELECT')  # 다른 모델을 FBX에서 제외합니다.
    for obj in objects:  # 표정 메시와 골격을 포함한 전체 부위를 선택합니다.
        obj.select_set(True)  # 부위가 누락되지 않게 합니다.
    bpy.context.view_layer.objects.active = root  # 모델 루트를 내보내기의 기준으로 삼습니다.
    path = OUTPUT / (root['export_name'] + '.fbx')  # 개별 모델의 실제 FBX 파일입니다.
    bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True, object_types={'MESH', 'ARMATURE', 'EMPTY'}, apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS', use_mesh_modifiers=False, add_leaf_bones=False, use_armature_deform_only=True, bake_anim=False, axis_forward='-Z', axis_up='Y', path_mode='AUTO')  # 메시와 스킨 및 표정을 미터 단위 모델로 저장합니다.
    used = sorted({slot.material.name.removeprefix('Soft_') for obj in objects if obj.type == 'MESH' for slot in obj.material_slots if slot.material})  # 해당 모델의 실제 사용 재질만 연결합니다.
    guid = metadata(path)  # FBX를 다시 제작해도 Unity 식별자를 유지합니다.
    text = f'fileFormatVersion: 2\nguid: {guid}\nModelImporter:\n  serializedVersion: 22200\n  internalIDToNameTable: []\n  externalObjects:\n'  # 프로젝트 버전과 같은 모델 임포터 형식입니다.
    for material in used:  # FBX 내부 재질을 실제 URP 머티리얼로 매핑합니다.
        text += f'  - first:\n      type: UnityEngine:Material\n      assembly: UnityEngine.CoreModule\n      name: Soft_{material}\n    second: {{fileID: 2100000, guid: {guids[material]}, type: 2}}\n'  # 자동 생성 재질 대신 제작한 카툰 머티리얼을 연결합니다.
    rigged = any(obj.type == 'ARMATURE' for obj in objects)  # 캐릭터와 정적 소품의 임포트 방식을 구분합니다.
    text += '  materials:\n    materialImportMode: 2\n    materialName: 0\n    materialSearch: 1\n    materialLocation: 1\n  animations:\n    optimizeGameObjects: 0\n    clipAnimations: []\n    isReadable: 0\n  meshes:\n    globalScale: 1\n    meshCompression: 0\n    addColliders: 0\n    importBlendShapes: 1\n    importCameras: 0\n    importLights: 0\n    fileIdsGeneration: 2\n    useFileUnits: 1\n    useFileScale: 1\n    preserveHierarchy: 1\n    skinWeightsMode: 0\n    maxBonesPerVertex: 4\n    minBoneWeight: 0.001\n    optimizeBones: 0\n    indexFormat: 0\n  tangentSpace:\n    normalImportMode: 0\n    tangentImportMode: 3\n  importAnimation: 0\n'  # 관절과 표정은 유지하고 게임 코드 및 충돌체는 추가하지 않습니다.
    text += f'  animationType: {2 if rigged else 0}\n  avatarSetup: {1 if rigged else 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'  # 캐릭터는 이름 기반 관절 동작용 Generic 골격으로 저장합니다.
    write(Path(str(path) + '.meta'), text)  # 다음 Unity 임포트에 필요한 설정을 함께 저장합니다.
    for obj, name in old_names:  # 편집 원본에서는 모델별 부위 이름을 구분합니다.
        obj.name = name  # 내보내기용 이름이 다른 모델 제작에 영향을 주지 않게 합니다.
    print('모델 저장: ' + path.name, flush=True)  # 제작 파일의 저장 완료만 알립니다.


def copy_model(root, name, position, parent, turn=0):  # 같은 모델을 원본 거리에 반복 배치하고 FBX의 재질 연결을 보존합니다.
    mapping = {}  # 기존 부모와 새 부모 사이의 대응 관계입니다.
    for obj in descendants(root):  # 바퀴 같은 하위 부위도 동일한 계층으로 복제합니다.
        duplicate = obj.copy()  # 기존 부위의 위치와 계층 및 머티리얼 구성을 복제합니다.
        if obj.type == 'MESH':  # 반복 부품의 재질 인덱스가 FBX 안에서 서로 충돌하지 않게 합니다.
            duplicate.data = obj.data.copy()  # 내보낼 메시마다 독립적인 재질 슬롯 연결을 유지합니다.
        bpy.context.scene.collection.objects.link(duplicate)  # 새 배치를 원본 장면에 등록합니다.
        duplicate.parent = mapping.get(obj.parent, parent)  # 복제한 부모가 있으면 그 아래에 연결합니다.
        mapping[obj] = duplicate  # 다음 자식 부위가 새 부모를 찾게 합니다.
    duplicate = mapping[root]  # 배치 전체의 루트입니다.
    duplicate.name, duplicate['export_name'] = name, name  # 개별 배치를 구분할 이름입니다.
    duplicate.location, duplicate.rotation_euler.z = bp(position), math.radians(turn)  # 모델의 실제 크기를 유지하며 위치와 방향만 지정합니다.
    return duplicate  # 조립한 거리 세트의 한 배치입니다.


def assemble_street(guids):  # 별도 모델을 재사용해 한 번에 배치할 수 있는 거리 세트도 저장합니다.
    models = {root['export_name']: root for root, _, _ in ASSETS}  # 방금 제작한 독립 모델의 대응 관계입니다.
    town = asset('SoftStreetSet')  # 게임 로직이 없는 순수 배경 조립 모델입니다.
    excluded = {'MintFriend', 'ApricotFriend', 'PocketPhone', 'ImpactStar', 'ImpactRing', 'SurpriseMarker', 'SweatDrop'}  # 캐릭터와 효과 원형은 독립적으로 사용합니다.
    for name, root in models.items():  # 거리 배경에 해당하는 원본 배치를 묶습니다.
        if name not in excluded:  # 실제 배경과 차량 소품만 조립 세트에 포함합니다.
            root.parent = town  # 원점인 세트 루트 아래로 기존 위치를 유지합니다.
    for x in (-20, -10, 10, 20):  # 직선 도로를 양옆으로 연결합니다.
        copy_model(models['RoadStraight'], f'Road_{x}', (x, 0, 0), town)  # 개별 도로의 끝이 서로 맞닿는 배치입니다.
        copy_model(models['SidewalkStraight'], f'NearSidewalk_{x}', (x, 0, 5), town)  # 학교 쪽 보도도 같은 길이로 연결합니다.
        copy_model(models['RoadCenterLines'], f'CenterLines_{x}', (x, .004, 0), town) if x != -10 else None  # 횡단보도 주변을 비운 중앙선 배치입니다.
    for x in (-20, -10, 0, 10, 20):  # 건너편에도 같은 폭의 보행 구역을 만듭니다.
        copy_model(models['SidewalkStraight'], f'FarSidewalk_{x}', (x, 0, -5), town)  # 두 보도가 횡단보도로 연결되게 합니다.
    for x in range(-24, 25, 2):  # 두 도로 가장자리에 경계석을 이어 배치합니다.
        copy_model(models['CurbSegment'], f'FarCurb_{x}', (x, 0, -3.5), town)  # 건너편 보도의 경계입니다.
        if x != 0:  # 중앙의 기존 경계석은 중복하지 않습니다.
            copy_model(models['CurbSegment'], f'NearCurb_{x}', (x, 0, 3.5), town)  # 학교 쪽 보도의 경계입니다.
    for x, z in ((-20, 11), (20, 11), (-20, -11), (0, -11), (20, -11), (-20, 25), (0, 25), (20, 25)):  # 건물과 원경 아래의 지면을 연결합니다.
        copy_model(models['GrassGround'], f'Ground_{x}_{z}', (x, 0, z), town)  # 건물과 나무의 밑면을 같은 지면 높이에 맞춥니다.
    copy_model(models['RoadStopLine'], 'OppositeStopLine', (3, .004, 1.8), town)  # 반대 차로의 정지선입니다.
    for index, x in enumerate((-21, -7, 8, 20)):  # 거리 양옆에 같은 나무를 적은 수로 배치합니다.
        copy_model(models['RoundTree'], f'StreetTree_{index}', (x, 0, -8 if index % 2 else 8.5), town)  # 횡단보도 중앙의 관찰 시야를 비워 둡니다.
    copy_model(models['SoftPedestrianSignal'], 'OppositeSignal', (-2.4, .15, 4.5), town, 180)  # 양쪽 인도에서 읽을 수 있는 신호등 배치입니다.
    copy_model(models['SoftCloud'], 'SecondCloud', (12, 9.5, 24), town)  # 원경 양쪽에 구름을 나누어 둡니다.
    export_asset(town, guids)  # 전체 거리도 한 번에 가져올 수 있는 FBX로 저장합니다.


def set_blender_cartoon_materials(names=None):  # 현재 제작 범위의 실제 카툰 재질을 원본에도 연결합니다.
    for name, material in MATERIALS.items():  # 각 모델에 연결한 실제 재질을 수정합니다.
        if names is not None and name not in names:  # 배경 등 이번에 요청하지 않은 재질은 유지합니다.
            continue  # 지정한 캐릭터 재질만 연결합니다.
        nodes, links = material.node_tree.nodes, material.node_tree.links  # 해당 재질의 표면 구성입니다.
        nodes.clear()  # 부드러운 PBR 명암 대신 카툰 표면을 구성합니다.
        diffuse = nodes.new('ShaderNodeBsdfDiffuse')  # 스튜디오 조명의 밝고 어두운 방향만 읽습니다.
        diffuse.inputs['Color'].default_value = (1, 1, 1, 1)  # 조명 계산에서 원래 색상이 중복 곱해지지 않게 합니다.
        rgb = nodes.new('ShaderNodeShaderToRGB')  # 조명 밝기를 단계별 색상으로 바꿉니다.
        ramp = nodes.new('ShaderNodeValToRGB')  # 밝은 면과 그늘 면을 분명하게 나눕니다.
        ramp.color_ramp.interpolation = 'CONSTANT'  # 구간 사이의 점토형 그라데이션을 사용하지 않습니다.
        shade = .90 if name in ('Skin', 'White', 'Shoe') else .80  # 얼굴과 흰색은 밝게 유지하고 의상과 배경의 면을 구분합니다.
        ramp.color_ramp.elements[0].position, ramp.color_ramp.elements[0].color = .18, (shade, shade, shade, 1)  # 그늘의 일정한 밝기입니다.
        ramp.color_ramp.elements[1].position, ramp.color_ramp.elements[1].color = .22, (1, 1, 1, 1)  # 밝은 면은 원래 팔레트 색상을 사용합니다.
        multiply = nodes.new('ShaderNodeMixRGB')  # 모델의 실제 색상에 명암 단계를 곱합니다.
        multiply.blend_type = 'MULTIPLY'  # 밝기만 바꾸고 색상 디자인을 유지합니다.
        multiply.inputs[0].default_value, multiply.inputs[1].default_value = 1, (*COLORS[name], 1)  # 해당 피부와 의상 및 소품의 실제 색상입니다.
        emission = nodes.new('ShaderNodeEmission')  # 계산한 카툰 색상을 추가적인 PBR 반사 없이 출력합니다.
        emission.inputs['Strength'].default_value = 1  # 색상이 과하게 발광하지 않는 기본 출력 강도입니다.
        output = nodes.new('ShaderNodeOutputMaterial')  # 실제 모델에 적용할 재질 출력입니다.
        links.new(diffuse.outputs[0], rgb.inputs[0])  # 조명 방향을 색상 밝기로 전달합니다.
        links.new(rgb.outputs[0], ramp.inputs[0])  # 밝기를 두 단계로 나눕니다.
        links.new(ramp.outputs[0], multiply.inputs[2])  # 모델 색상에 명암을 반영합니다.
        links.new(multiply.outputs[0], emission.inputs[0])  # 실제 표시할 카툰 색상입니다.
        links.new(emission.outputs[0], output.inputs['Surface'])  # 원본의 모델 표면에 연결합니다.
    for screen in bpy.data.screens:  # 원본 파일에 저장되는 편집 화면의 표시 방식을 설정합니다.
        for area in screen.areas:  # 모델 편집 영역만 대상으로 합니다.
            if area.type == 'VIEW_3D':  # 사용자 컴퓨터의 창을 열거나 조작하지 않고 원본 설정만 저장합니다.
                space = area.spaces.active  # 해당 원본의 3D 편집 표시 설정입니다.
                space.shading.type = 'MATERIAL'  # 원본을 열 때 제작한 카툰 재질을 표시합니다.
                space.region_3d.view_location = Vector((0, -5, 1.0))  # 사람형 두 친구를 중심으로 원본의 시작 구도를 둡니다.
                space.region_3d.view_distance = 5.3  # 두 캐릭터의 전신이 들어오는 편집 거리입니다.
                space.region_3d.view_rotation = Vector((-1.8, 4.5, -1.2)).to_track_quat('-Z', 'Y')  # 얼굴과 의상이 함께 읽히는 앞쪽 사선 구도입니다.


def main():  # 수정한 실제 모델 제작과 에셋 저장만 수행합니다.
    OUTPUT.mkdir(parents=True, exist_ok=True)  # 개별 FBX를 저장할 실제 폴더입니다.
    for obj in list(bpy.data.objects):  # 별도의 제작 프로세스 안에 있는 기본 장면을 비웁니다.
        bpy.data.objects.remove(obj, do_unlink=True)  # 사용자 게임 씬이나 기존 모델 파일은 변경하지 않습니다.
    bpy.context.scene.unit_settings.system = 'METRIC'  # 캐릭터와 거리 소품이 미터 단위를 공유하게 합니다.
    bpy.context.scene.unit_settings.scale_length = 1.0  # Unity에서 크기가 달라지지 않는 기본 단위입니다.
    for name, rgb in COLORS.items():  # 원본과 Unity가 같은 단색 팔레트를 사용합니다.
        material = bpy.data.materials.new('Soft_' + name)  # FBX의 재질 매핑과 같은 이름입니다.
        material.diffuse_color, material.use_nodes = (*rgb, 1), True  # 원본의 색상 표시와 표면 재질을 함께 준비합니다.
        surface = material.node_tree.nodes.get('Principled BSDF')  # FBX가 읽을 수 있는 기본 표면 재질입니다.
        surface.inputs['Base Color'].default_value = (*rgb, 1)  # 큰 색면으로 캐릭터와 소품을 구분합니다.
        surface.inputs['Roughness'].default_value = .78  # 강한 반사 없이 부드러운 무광 표면입니다.
        MATERIALS[name] = material  # 모든 부위가 같은 재질을 재사용합니다.
    characters = [make_character('MintFriend', 'Denim', (-1.2, .15, 5.0)), make_character('ApricotFriend', 'Mint', (1.2, .15, 5.0), girl=True)]  # 참고 이미지의 남색 상의와 민트색 상의로 구분되는 작은 SD 어린이입니다.
    make_phone()  # 손에 부착 가능한 별도 휴대전화입니다.
    make_car()  # 바퀴의 회전 중심이 분리된 실제 차량 모델입니다.
    make_school()  # 학교 배경의 중심 외관입니다.
    make_building('PeachTownhouse', 'Pink', 4.3, 5.3, (-12.0, 0, 14.0))  # 낮은 분홍색 배경 건물입니다.
    make_building('BlueTownhouse', 'Blue', 4.0, 6.6, (12.5, 0, 14.0))  # 높이와 지붕색이 다른 배경 건물입니다.
    make_building('CornerShop', 'Mint', 4.7, 4.6, (-17.5, 0, 10.0), shop=True)  # 넓은 차양으로 구분되는 작은 상가입니다.
    make_street()  # 도로와 보도 및 노면 표시를 독립 모듈로 만듭니다.
    make_pole()  # 충돌 상황의 전봇대 모델입니다.
    make_signal()  # 신호 그림을 독립적으로 제어할 수 있는 신호등입니다.
    make_greenery()  # 나무와 관목 및 울타리와 구름입니다.
    make_effect_shapes()  # 충격과 놀람 연출에 사용할 입체 원형입니다.
    guids = save_materials()  # Unity에서 사용할 실제 재질을 먼저 저장합니다.
    for root, _, _ in ASSETS:  # 모든 개별 모델은 원점 기준으로 저장합니다.
        export_asset(root, guids)  # 게임 실행 없이 완성된 메시와 골격만 내보냅니다.
    for root, position, turn in ASSETS:  # 편집 원본에서는 모델을 학교 앞 거리로 배치합니다.
        root.location, root.rotation_euler.z = bp(position), math.radians(turn)  # 개별 FBX의 원점과 원본의 조립 위치를 구분합니다.
    assemble_street(guids)  # 반복 모듈을 사용한 배경 조립본도 함께 저장합니다.
    for _, rig in characters:  # 편집 원본의 두 친구는 편한 대기 자세로 세워 둡니다.
        for side, angle in (('Left', 75), ('Right', -75)):  # 내보낸 바인드 자세와 별개로 원본의 팔을 내립니다.
            pose = rig.pose.bones[side + 'UpperArm']  # 실제 스킨이 연결된 위팔입니다.
            pose.rotation_mode = 'QUATERNION'  # 관절의 로컬 회전으로 자세를 저장합니다.
            local_axis = rig.data.bones[side + 'UpperArm'].matrix_local.to_quaternion().inverted() @ Vector((0, -1, 0))  # 설계 정면 축을 해당 뼈의 로컬 축으로 옮깁니다.
            pose.rotation_quaternion = Quaternion(local_axis, math.radians(angle))  # 골격 이름과 길이를 유지하며 원본 자세만 설정합니다.
    set_blender_cartoon_materials()  # 원본 파일에도 실제 카툰 표시용 재질을 연결합니다.
    bpy.ops.object.select_all(action='DESELECT')  # 원본 저장 시 특정 부위가 선택 상태로 남지 않게 합니다.
    bpy.context.scene.world.color = (.55, .69, .76)  # 원본 배경을 팔레트와 어울리는 하늘색으로 설정합니다.
    bpy.context.preferences.filepaths.save_version = 0  # 제작 원본의 불필요한 백업 사본 생성을 막습니다.
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / 'SoftCrosswalk.blend'))  # 메시와 골격 및 표정을 편집 가능한 원본으로 저장합니다.
    print(f'제작 완료: FBX {len(ASSETS)}개와 URP 재질 {len(COLORS)}개 및 Blender 원본을 저장했습니다.', flush=True)  # 파일 저장 완료만 보고하고 게임 실행 결과는 주장하지 않습니다.


def update_character_hair(girl=False):  # 선택한 캐릭터의 얼굴과 표정 및 옷을 유지하고 머리카락만 교체합니다.
    global ACTIVE_ROOT  # 새 머리 부위가 선택한 캐릭터의 기존 로컬 좌표를 사용하게 합니다.
    for name in COLORS:  # 현재 장면의 실제 재질을 제작 목록에 연결합니다.
        material = bpy.data.materials.get('Soft_' + name)  # 기존 모델이 사용하는 재질입니다.
        if material is not None:  # 다른 캐릭터나 배경의 재질을 새로 만들지 않습니다.
            MATERIALS[name] = material  # 이번 머리 제작에서도 기존 재질 연결을 재사용합니다.
    for name in (() if girl else ('Hair', 'HairEdge')):  # 여자아이의 머리 수정에서는 기존 머리 재질을 그대로 사용합니다.
        material = bpy.data.materials.get('Soft_' + name)  # 같은 머리 재질이 있으면 기존 이름과 데이터를 사용합니다.
        if material is None:  # 현재 장면에서 사용하지 않던 머리 재질만 추가합니다.
            material = bpy.data.materials['Soft_BlondHair'].copy()  # 현재 캐릭터의 기존 카툰 표면 구조를 그대로 복제합니다.
            material.name = 'Soft_' + name  # 기존 Unity 머티리얼과 같은 이름을 사용합니다.
        material.diffuse_color = (*COLORS[name], 1)  # 흑갈색 머리와 낮은 대비의 갈색 머릿결을 지정합니다.
        for node in material.node_tree.nodes:  # 재질 이름에 의존하지 않고 실제 표면 노드의 색상을 바꿉니다.
            if node.type == 'MIX_RGB':  # 기존 카툰 명암에 곱하는 실제 머리색입니다.
                node.inputs[1].default_value = (*COLORS[name], 1)  # 금발 팔레트를 새 머리의 짙은 색상으로 교체합니다.
            elif node.type == 'BSDF_PRINCIPLED':  # 기본 표면 노드가 있는 재질에서도 같은 색상을 사용합니다.
                node.inputs['Base Color'].default_value = (*COLORS[name], 1)  # FBX와 원본의 표면색 기준을 맞춥니다.
        MATERIALS[name] = material  # 새 머리 메시가 사용할 실제 재질입니다.
    if not girl:  # 남자아이의 새 머리색을 제작하는 경우에만 머리 재질을 저장합니다.
        save_materials({'Hair', 'HairEdge'})  # 여자아이의 틈 수정에서는 얼굴과 머리 및 옷의 재질을 다시 쓰지 않습니다.
    guids = {name: next(line[6:].strip() for line in (OUTPUT / 'Materials' / ('Soft_' + name + '.mat.meta')).read_text(encoding='utf-8').splitlines() if line.startswith('guid: ')) for name in COLORS}  # 기존 및 새 머리 재질의 Unity 참조를 읽습니다.
    root = bpy.data.objects['ApricotFriend' if girl else 'MintFriend']  # 이번 요청에서 머리를 수정할 기존 캐릭터만 선택합니다.
    rig = next(obj for obj in descendants(root) if obj.type == 'ARMATURE')  # 얼굴과 몸의 동작이 연결된 기존 골격입니다.
    for obj in descendants(root):  # 선택한 캐릭터의 이전 머리카락 부위만 찾습니다.
        part_name = obj.get('export_name', '')  # 기존 모델에 기록된 실제 부위 이름입니다.
        if obj.type == 'MESH' and (obj.get('hair_part', False) or part_name in ('BobHair', 'SidePartHair', 'TopHairTuft', 'HairClip', 'LeftHairTuft', 'RightHairTuft') or part_name.startswith(('HairLock', 'HairPaint'))):  # 얼굴과 귀 및 표정은 보존하고 머리카락과 머리에 붙은 장식만 교체합니다.
            data = obj.data  # 교체하는 이전 머리의 메시 데이터입니다.
            bpy.data.objects.remove(obj, do_unlink=True)  # 현재 표정과 몸의 모델은 그대로 남깁니다.
            if data.users == 0:  # 다른 모델이 사용하지 않는 이전 머리 데이터만 정리합니다.
                bpy.data.meshes.remove(data)  # 새 머리가 불필요한 이전 메시를 이어받지 않게 합니다.
    ACTIVE_ROOT, parts = root, []  # 새 머리만 선택한 캐릭터의 기존 계층 아래에서 제작합니다.
    if girl:  # 여자아이의 기존 스타일에서 앞머리 사이의 빈 부분을 수정합니다.
        make_hair(parts, True)  # 바탕 머리가 앞머리 아래까지 이어진 기존 묶음머리를 제작합니다.
    else:  # 남자아이 머리 교체의 기존 범위입니다.
        make_boy_hair(parts)  # 짧은 흑갈색 머리를 현재 스타일로 유지합니다.
    for obj, _ in parts:  # 새 머리 표면만 현재 얼굴의 높이에 맞춥니다.
        obj['head_part'], obj['hair_part'] = True, True  # 이후 얼굴 수정과 머리 전용 수정의 범위를 구분합니다.
        for vertex in obj.data.vertices:  # 기존 두상이 사용하는 높이 이동을 새 머리에도 한 번 적용합니다.
            vertex.co.z -= HEAD_DROP  # 이미 저장된 얼굴과 귀 및 목의 위치에 머리카락을 맞춥니다.
        obj.data.update()  # 실제 머리 표면 위치를 기존 골격 연결에 반영합니다.
    skin_character(parts, root, rig)  # 새 머리만 기존 머리뼈에 연결하고 얼굴의 웨이트와 표정은 유지합니다.
    position, rotation, pose_position = root.location.copy(), root.rotation_euler.copy(), rig.data.pose_position  # 선택한 캐릭터의 기존 배치와 포즈 표시 방식을 보관합니다.
    material_states = []  # FBX 저장 뒤 기존 카툰 표면으로 복원할 연결 정보입니다.
    try:  # 머리 변경을 선택한 캐릭터의 같은 원점과 골격 기준으로 저장합니다.
        root.location, root.rotation_euler = (0, 0, 0), (0, 0, 0)  # 기존 FBX의 개별 모델 원점을 유지합니다.
        rig.data.pose_position = 'REST'  # 기존 바인드 골격에 새 머리와 보존한 몸을 함께 저장합니다.
        used = {slot.material for obj in descendants(root) if obj.type == 'MESH' for slot in obj.material_slots if slot.material}  # 선택한 캐릭터에 실제 연결된 재질만 전달합니다.
        for material in used:  # 원본의 표면 노드는 보존하면서 저장 중에만 표준 FBX 표면을 연결합니다.
            tree = material.node_tree  # 해당 부위가 사용 중인 실제 재질 구성입니다.
            output = next(node for node in tree.nodes if node.type == 'OUTPUT_MATERIAL' and node.is_active_output)  # 현재 활성 표면 출력입니다.
            connections = [(link.from_socket, link.to_socket) for link in output.inputs['Surface'].links]  # 머리와 얼굴 및 의상의 기존 표면 연결을 보관합니다.
            for link in list(output.inputs['Surface'].links):  # FBX용 표준 표면을 연결할 자리를 잠시 마련합니다.
                tree.links.remove(link)  # 기존 카툰 노드 자체는 삭제하지 않습니다.
            standard = tree.nodes.new('ShaderNodeBsdfPrincipled')  # FBX가 읽을 수 있는 기본 표면입니다.
            standard.inputs['Base Color'].default_value = material.diffuse_color  # 현재 머리와 얼굴 및 의상의 실제 색상입니다.
            standard.inputs['Roughness'].default_value = .78  # 기존 모델의 무광 표면 설정을 유지합니다.
            tree.links.new(standard.outputs[0], output.inputs['Surface'])  # 저장하는 동안만 표준 표면을 사용합니다.
            material_states.append((material, standard, connections))  # 저장 이후 원래 표시로 복원할 대상을 기록합니다.
        bpy.context.view_layer.update()  # 새 머리와 기존 골격의 연결을 출력 데이터에 반영합니다.
        export_asset(root, guids)  # 선택한 캐릭터 FBX와 기존 재질 참조에 머리 변경만 저장합니다.
    finally:  # 저장 뒤 원본의 기존 재질과 캐릭터 배치 및 포즈를 복원합니다.
        for material, standard, connections in material_states:  # 저장 중에만 사용한 표준 노드를 제거합니다.
            material.node_tree.nodes.remove(standard)  # 실제 원본의 카툰 표면 구조를 유지합니다.
            for source_socket, target_socket in connections:  # 기존 머리와 얼굴 및 의상 출력을 복원합니다.
                material.node_tree.links.new(source_socket, target_socket)  # 다른 부위에 임시 저장용 재질이 남지 않게 합니다.
        root.location, root.rotation_euler = position, rotation  # 원본 거리 안의 기존 캐릭터 위치와 방향입니다.
        rig.data.pose_position = pose_position  # 표정과 관절 회전을 변경하지 않고 기존 포즈 표시로 돌아갑니다.
    bpy.ops.object.select_all(action='DESELECT')  # 저장 시 새 머리만 선택된 상태로 남지 않게 합니다.
    bpy.context.preferences.filepaths.save_version = 0  # 같은 원본의 불필요한 별도 백업 파일을 만들지 않습니다.
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / 'SoftCrosswalk.blend'))  # 선택한 캐릭터의 머리 수정만 현재 Blender 원본에 저장합니다.
    print(f'머리 수정 저장: {root.name}.fbx, SoftCrosswalk.blend', flush=True)  # 이번 요청으로 실제 저장한 모델만 보고합니다.


def update_character_heads():  # 저장된 원본의 얼굴과 헤어만 기존 골격에 교체합니다.
    global ACTIVE_ROOT  # 새 얼굴 부품을 현재 친구 모델에 연결합니다.
    for name in COLORS:  # 기존 모델과 동일한 머티리얼을 재사용합니다.
        material = bpy.data.materials.get('Soft_' + name)  # 원본에는 실제 사용한 색상 재질만 저장되어 있습니다.
        if material is not None:  # 사용하지 않아 저장 대상에서 빠진 팔레트 재질은 불러오지 않습니다.
            MATERIALS[name] = material  # 몸과 배경에 실제 연결된 기존 재질을 그대로 사용합니다.
    guids = {name: next(line[6:].strip() for line in (OUTPUT / 'Materials' / ('Soft_' + name + '.mat.meta')).read_text(encoding='utf-8').splitlines() if line.startswith('guid: ')) for name in COLORS}  # 기존 Unity 머티리얼 참조를 읽어 그대로 연결합니다.
    head_names = {'HeadSkin', 'NeckSkin', 'Smile', 'BobHair', 'SidePartHair', 'DesignedFringe', 'HairClip'}  # 이전 얼굴과 머리카락에 속한 부품만 교체합니다.
    head_prefixes = ('LeftEye', 'RightEye', 'LeftCheek', 'RightCheek')  # 양쪽 눈과 눈썹 및 볼의 기존 부위 이름입니다.
    for name, girl in (('MintFriend', False), ('ApricotFriend', True)):  # 현재 두 친구의 얼굴과 헤어를 같은 기준으로 수정합니다.
        root = bpy.data.objects[name]  # 기존 위치와 몸 및 의상을 가진 모델 루트입니다.
        rig = next(obj for obj in descendants(root) if obj.type == 'ARMATURE')  # 기존 골격과 저장된 대기 자세를 재사용합니다.
        for obj in descendants(root):  # 교체할 얼굴과 헤어의 실제 부위만 찾습니다.
            part_name = obj.get('export_name', '')  # 내보내기에서 사용한 의미별 부위 이름입니다.
            if obj.type == 'MESH' and (obj.get('head_part', False) or part_name in head_names or part_name.startswith(head_prefixes)):  # 새 헤어 조각과 입 안쪽 색면까지 얼굴 범위에 함께 포함합니다.
                previous_mesh = obj.data  # 교체할 부위의 기존 메시 데이터입니다.
                bpy.data.objects.remove(obj, do_unlink=True)  # 이전 얼굴과 분리된 앞머리 판을 모델에서 제거합니다.
                if previous_mesh.users == 0:  # 교체한 얼굴만 사용하던 메시의 이름을 새 부위가 이어받게 합니다.
                    bpy.data.meshes.remove(previous_mesh)  # 다른 모델이 사용하는 데이터에는 영향을 주지 않습니다.
        ACTIVE_ROOT = root  # 새 머리 부위를 기존 모델의 로컬 기준으로 만듭니다.
        parts = []  # 교체할 피부와 얼굴 표정 및 헤어입니다.
        make_head(parts, girl)  # 둥근 얼굴과 연속된 새 헤어를 실제 메시로 제작합니다.
        skin_character(parts, root, rig)  # 새 부위만 기존 머리와 눈 관절에 연결합니다.
        original_location, original_rotation = root.location.copy(), root.rotation_euler.copy()  # 원본의 두 친구 배치를 보관합니다.
        original_pose = rig.data.pose_position  # 원본의 대기 자세를 보관합니다.
        material_states = []  # FBX의 표준 재질 전달 뒤 원본 카툰 노드를 복원할 정보입니다.
        try:  # 내보내기 과정에서도 원본의 배치와 재질을 복원합니다.
            root.location, root.rotation_euler = (0, 0, 0), (0, 0, 0)  # 개별 FBX는 이전과 같은 모델 원점으로 저장합니다.
            rig.data.pose_position = 'REST'  # 기존 FBX와 동일한 바인드 자세로 저장합니다.
            used = {slot.material for obj in descendants(root) if obj.type == 'MESH' for slot in obj.material_slots if slot.material}  # 현재 캐릭터의 실제 사용 재질입니다.
            for material in used:  # FBX에는 표준 표면색을 전달하고 원본 카툰 재질은 유지합니다.
                tree = material.node_tree  # 원본 카툰 재질의 실제 노드 연결입니다.
                output = next(node for node in tree.nodes if node.type == 'OUTPUT_MATERIAL' and node.is_active_output)  # 현재 재질 출력입니다.
                connections = [(link.from_socket, link.to_socket) for link in output.inputs['Surface'].links]  # 기존 카툰 출력 연결을 보관합니다.
                for link in list(output.inputs['Surface'].links):  # 표준 FBX 표면을 잠시 연결할 자리를 만듭니다.
                    tree.links.remove(link)  # 기존 노드 자체는 삭제하지 않습니다.
                standard = tree.nodes.new('ShaderNodeBsdfPrincipled')  # FBX에서 읽을 수 있는 색상 전달용 표면입니다.
                standard.inputs['Base Color'].default_value = material.diffuse_color  # 기존 피부와 의상 색상을 그대로 사용합니다.
                standard.inputs['Roughness'].default_value = .78  # 이전 FBX의 기본 표면 설정을 유지합니다.
                tree.links.new(standard.outputs[0], output.inputs['Surface'])  # FBX 저장 동안만 표준 색상 출력을 사용합니다.
                material_states.append((material, standard, connections))  # 원본 카툰 연결을 복원할 대상을 기록합니다.
            bpy.context.view_layer.update()  # 변경한 부모 연결과 바인드 자세를 내보내기 데이터에 반영합니다.
            export_asset(root, guids)  # 수정한 캐릭터 FBX와 기존 재질 참조만 저장합니다.
        finally:  # 내보내기 종료 시 원본의 자세와 카툰 재질을 돌려놓습니다.
            for material, standard, connections in material_states:  # 잠시 사용한 표준 표면만 제거합니다.
                material.node_tree.nodes.remove(standard)  # 원본에는 제작한 카툰 노드만 남깁니다.
                for source_socket, target_socket in connections:  # 기존 표면 연결을 그대로 복원합니다.
                    material.node_tree.links.new(source_socket, target_socket)  # 몸과 배경을 포함한 재질 표시를 유지합니다.
            root.location, root.rotation_euler = original_location, original_rotation  # 기존 원본의 캐릭터 배치입니다.
            rig.data.pose_position = original_pose  # 기존 대기 자세로 되돌립니다.
    bpy.ops.object.select_all(action='DESELECT')  # 원본에서 특정 얼굴 부위가 선택된 상태로 남지 않게 합니다.
    bpy.context.preferences.filepaths.save_version = 0  # 같은 제작 원본의 불필요한 백업 파일을 만들지 않습니다.
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / 'SoftCrosswalk.blend'))  # 기존 몸과 배경을 유지한 채 수정된 얼굴과 헤어를 저장합니다.
    print('얼굴과 헤어 교체 저장: MintFriend.fbx, ApricotFriend.fbx, SoftCrosswalk.blend', flush=True)  # 실제 저장한 대상만 보고합니다.


def update_reference_characters():  # 참고 이미지에 맞춰 두 캐릭터의 얼굴과 몸 및 헤어만 다시 제작합니다.
    focused = {'Skin', 'Mouth', 'FaceInk', 'CopperHair', 'BlondHair', 'HairLine', 'HairPaint'}  # 얼굴과 헤어에 필요한 재질만 갱신하고 배경의 색상은 유지합니다.
    for name in COLORS:  # 기존 모델의 실제 사용 재질을 재사용합니다.
        material = bpy.data.materials.get('Soft_' + name)  # 원본에 저장된 재질입니다.
        if material is not None:  # 사용하지 않은 팔레트 색상은 새로 만들지 않습니다.
            MATERIALS[name] = material  # 몸과 원본 배경의 기존 재질을 유지합니다.
    for name in focused:  # 피부와 그래픽 눈 및 밝은 헤어 재질을 준비합니다.
        if name not in MATERIALS:  # 새로 필요한 헤어 색상과 눈 색상만 추가합니다.
            material = bpy.data.materials.new('Soft_' + name)  # FBX와 Unity에서 공유할 실제 재질 이름입니다.
            material.use_nodes = True  # 원본에도 실제 표면 재질을 연결합니다.
            MATERIALS[name] = material  # 새 앞머리와 눈 모델이 사용할 재질입니다.
        MATERIALS[name].diffuse_color = (*COLORS[name], 1)  # 얼굴과 손발 및 헤어가 참고 이미지의 색상을 공유합니다.
    save_materials(focused)  # 배경 재질을 다시 저장하지 않고 이번 색상만 Unity에 반영합니다.
    guids = {name: next(line[6:].strip() for line in (OUTPUT / 'Materials' / ('Soft_' + name + '.mat.meta')).read_text(encoding='utf-8').splitlines() if line.startswith('guid: ')) for name in COLORS}  # 기존 및 새 머티리얼의 실제 참조입니다.
    set_blender_cartoon_materials(focused)  # 새 재질도 원본에서 같은 평면적인 카툰 명암을 사용합니다.
    for name, color, girl in (('MintFriend', 'Denim', False), ('ApricotFriend', 'Mint', True)):  # 남색과 민트색 옷을 가진 두 SD 어린이를 제작합니다.
        previous_root = bpy.data.objects[name]  # 교체할 기존 모델의 루트입니다.
        previous_rig = next(obj for obj in descendants(previous_root) if obj.type == 'ARMATURE')  # 기존 편집 원본의 자세를 읽습니다.
        position, rotation = previous_root.location.copy(), previous_root.rotation_euler.copy()  # 원본의 캐릭터 배치를 유지합니다.
        poses = {bone.name: bone.matrix_basis.copy() for bone in previous_rig.pose.bones}  # 팔을 내린 기존 대기 자세입니다.
        for obj in reversed(descendants(previous_root)):  # 이번에 교체하는 캐릭터의 계층만 제거합니다.
            data, kind = obj.data, obj.type  # 기존 모델 부위의 실제 데이터입니다.
            bpy.data.objects.remove(obj, do_unlink=True)  # 배경과 다른 모델에는 접근하지 않습니다.
            if data is not None and data.users == 0:  # 교체한 캐릭터만 사용하던 이름을 새 모델이 이어받게 합니다.
                if kind == 'MESH':  # 새 메시의 이름에 불필요한 번호가 붙지 않게 합니다.
                    bpy.data.meshes.remove(data)  # 실제 교체한 몸과 얼굴의 이전 데이터입니다.
                elif kind == 'ARMATURE':  # 새 비율의 골격도 같은 이름을 사용합니다.
                    bpy.data.armatures.remove(data)  # 다른 모델의 골격은 유지합니다.
        root, rig = make_character(name, color, (0, 0, 0), girl=girl)  # 작은 몸과 큰 머리 및 단순한 표정을 실제 새 메시로 제작합니다.
        material_states = []  # FBX 저장 뒤 원본 카툰 재질을 복원할 정보입니다.
        try:  # 표준 FBX 재질 전달과 원본 재질 복원을 함께 처리합니다.
            used = {slot.material for obj in descendants(root) if obj.type == 'MESH' for slot in obj.material_slots if slot.material}  # 새 캐릭터가 사용하는 실제 재질입니다.
            for material in used:  # FBX에 정확한 표면 색상과 재질 이름을 전달합니다.
                tree = material.node_tree  # 원본의 실제 재질 노드입니다.
                output = next(node for node in tree.nodes if node.type == 'OUTPUT_MATERIAL' and node.is_active_output)  # 실제 표면 출력입니다.
                connections = [(link.from_socket, link.to_socket) for link in output.inputs['Surface'].links]  # 기존 카툰 출력 연결입니다.
                for link in list(output.inputs['Surface'].links):  # 표준 FBX 표면을 잠시 연결할 자리를 만듭니다.
                    tree.links.remove(link)  # 원본 노드 자체는 삭제하지 않습니다.
                standard = tree.nodes.new('ShaderNodeBsdfPrincipled')  # FBX가 읽을 수 있는 표면색 전달용 노드입니다.
                standard.inputs['Base Color'].default_value = material.diffuse_color  # 피부와 의상 및 헤어의 실제 색상입니다.
                standard.inputs['Roughness'].default_value = .78  # 기존 FBX 기본 표면 설정입니다.
                tree.links.new(standard.outputs[0], output.inputs['Surface'])  # 내보내는 동안만 표준 표면 출력을 사용합니다.
                material_states.append((material, standard, connections))  # 원본 표시를 복원할 대상입니다.
            bpy.context.view_layer.update()  # 새 골격과 메시 연결을 내보내기 데이터에 반영합니다.
            export_asset(root, guids)  # 두 캐릭터의 모델 파일과 대응 재질 참조만 저장합니다.
        finally:  # 원본에는 제작한 카툰 재질을 남깁니다.
            for material, standard, connections in material_states:  # 잠시 사용한 표준 재질 노드를 제거합니다.
                material.node_tree.nodes.remove(standard)  # 원본 재질의 기존 구성은 유지합니다.
                for source_socket, target_socket in connections:  # 원래의 카툰 표면을 복원합니다.
                    material.node_tree.links.new(source_socket, target_socket)  # 배경과 공유한 기존 재질에도 변화를 남기지 않습니다.
        root.location, root.rotation_euler = position, rotation  # 기존 거리 원본의 배치로 돌려놓습니다.
        for bone in rig.pose.bones:  # 작은 골격에서도 원본의 편한 대기 자세를 유지합니다.
            if bone.name in poses:  # 같은 관절 이름에만 기존 로컬 자세를 적용합니다.
                bone.matrix_basis = poses[bone.name]  # 새 바인드 위치를 덮어쓰지 않고 포즈만 이어받습니다.
    bpy.ops.object.select_all(action='DESELECT')  # 원본에서 특정 부위가 선택 상태로 남지 않게 합니다.
    bpy.context.preferences.filepaths.save_version = 0  # 별도의 불필요한 원본 백업 사본을 만들지 않습니다.
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / 'SoftCrosswalk.blend'))  # 배경을 유지한 채 두 캐릭터의 새 SD 모델을 저장합니다.
    print('참고 이미지형 SD 캐릭터 저장: MintFriend.fbx, ApricotFriend.fbx, SoftCrosswalk.blend', flush=True)  # 실제 제작한 파일 저장만 보고합니다.


def update_character_bodies(shoulders_only=False, boy_outfit=False):  # 얼굴을 보존하고 몸 전체와 어깨 또는 남자아이 의상 중 요청한 범위만 교체합니다.
    global ACTIVE_ROOT  # 새 몸 부위가 기존 캐릭터의 로컬 좌표를 사용하게 합니다.
    for name in COLORS:  # 이미 연결된 재질을 가져오고 색상이나 표면 노드는 다시 작성하지 않습니다.
        material = bpy.data.materials.get('Soft_' + name)  # 원본 캐릭터와 의상이 사용하는 실제 재질입니다.
        if material is not None:  # 존재하는 재질만 몸 제작에 재사용합니다.
            MATERIALS[name] = material  # 기존 얼굴과 배경의 재질 구성은 그대로 유지합니다.
    guids = {name: next(line[6:].strip() for line in (OUTPUT / 'Materials' / ('Soft_' + name + '.mat.meta')).read_text(encoding='utf-8').splitlines() if line.startswith('guid: ')) for name in COLORS}  # 내보낼 모델에 기존 Unity 재질 참조를 연결합니다.
    for name, color, girl in (('MintFriend', 'Denim', False), ('ApricotFriend', 'Mint', True)):  # 두 친구의 기존 의상 색상과 종류를 유지합니다.
        if boy_outfit and girl:  # 남자아이 옷 교체에서는 여자아이의 모델과 출력 파일을 변경하지 않습니다.
            continue  # 이번 의상 교체 대상인 남자아이만 처리합니다.
        root = bpy.data.objects[name]  # 현재 얼굴과 몸이 속한 모델 루트입니다.
        rig = next(obj for obj in descendants(root) if obj.type == 'ARMATURE')  # 현재 동작과 얼굴이 연결된 골격을 그대로 사용합니다.
        if not shoulders_only and not boy_outfit:  # 어깨와 의상 교체에서는 현재 손가락과 다른 뼈의 기준 위치를 변경하지 않습니다.
            poses = {bone.name: bone.matrix_basis.copy() for bone in rig.pose.bones}  # 손가락 기준 위치를 바꿀 때 기존 얼굴과 몸의 포즈를 보존합니다.
            bpy.ops.object.select_all(action='DESELECT')  # 손가락 뼈 편집에 다른 모델이 포함되지 않게 합니다.
            rig.select_set(True)  # 현재 캐릭터의 골격만 선택합니다.
            bpy.context.view_layer.objects.active = rig  # 손 모양과 맞출 기존 골격을 편집 대상으로 지정합니다.
            bpy.ops.object.mode_set(mode='EDIT')  # 실제 손가락의 바인드 위치를 수정합니다.
            for bone_name, _, head, tail in bone_data():  # 메시와 같은 곡선으로 계산한 손가락 관절 위치를 가져옵니다.
                if any(finger in bone_name for finger in ('Thumb', 'Index', 'Middle', 'Ring', 'Little')):  # 머리와 척추 및 팔과 다리의 기존 뼈는 변경하지 않습니다.
                    bone = rig.data.edit_bones[bone_name]  # 기존 동작이 참조하는 손가락 이름을 유지합니다.
                    bone.head, bone.tail = bp(head), bp(tail)  # 넓어진 손바닥과 짧아진 손가락의 표면 안으로 관절을 옮깁니다.
            bpy.ops.object.mode_set(mode='OBJECT')  # 수정한 손가락 골격에 새 손 표면을 연결할 상태로 돌아갑니다.
            for bone in rig.pose.bones:  # 기존 동작 포즈가 골격 편집으로 초기화되지 않게 합니다.
                bone.matrix_basis = poses[bone.name]  # 얼굴과 몸의 현재 회전 및 위치와 크기를 그대로 유지합니다.
        for obj in descendants(root):  # 현재 몸과 의상 및 손발만 교체 대상으로 찾습니다.
            clothing_names = {'ConnectedShirt', 'ShirtHem', 'CrewCollar', 'ConnectedTrousers', 'LeftSleeveRib', 'RightSleeveRib'}  # 새 의상으로 교체할 이전 남색 상의와 긴 바지의 실제 부위 이름입니다.
            replace = obj.get('export_name') in clothing_names or obj.get('boy_outfit', False) if boy_outfit else (obj.get('export_name') == 'ConnectedShirt' if shoulders_only else not obj.get('head_part', False))  # 옷 교체에서는 얼굴과 손 및 양말과 운동화를 제외한 의상 부위만 선택합니다.
            if obj.type == 'MESH' and replace:  # 현재 요청 범위에 포함되는 메시만 교체합니다.
                data = obj.data  # 교체하는 몸 부위의 이전 메시 데이터입니다.
                bpy.data.objects.remove(obj, do_unlink=True)  # 기존 골격과 얼굴을 남기고 몸 표면만 제거합니다.
                if data.users == 0:  # 다른 모델이 사용하지 않는 이전 몸 데이터만 정리합니다.
                    bpy.data.meshes.remove(data)  # 새 몸이 이전 부위 이름을 그대로 이어받게 합니다.
        ACTIVE_ROOT = root  # 둥근 몸통과 팔다리를 기존 루트의 좌표로 만듭니다.
        if boy_outfit:  # 기존 남색 옷을 새 후드티와 반바지로 교체하는 범위입니다.
            parts = make_boy_outfit()  # 기존 손과 신발을 다시 만들지 않고 새 옷과 드러나는 다리만 제작합니다.
            for obj, _ in parts:  # 새 의상 부위만 현재 몸과 골격의 비율에 맞춥니다.
                for vertex in obj.data.vertices:  # 후드와 주머니 및 반바지에 같은 제작 좌표 변환을 적용합니다.
                    vertex.co = bp(body_point((vertex.co.x, vertex.co.z, -vertex.co.y)))  # 기존 골격의 바인드 위치와 팔 길이에 맞는 실제 정점입니다.
                obj.data.update()  # 변환한 의상 표면을 기존 골격 연결에 반영합니다.
        else:  # 기존 몸 전체 또는 어깨 수정의 제작 범위입니다.
            parts = make_body(color, girl, shoulders_only=shoulders_only)  # 해당 범위에 필요한 상의나 몸 전체만 제작합니다.
        skin_character(parts, root, rig)  # 새 몸에만 기존 뼈 이름과 어깨의 연속적인 웨이트를 연결합니다.
        position, rotation, pose_position = root.location.copy(), root.rotation_euler.copy(), rig.data.pose_position  # 기존 배치와 현재 포즈 표시 방식을 보관합니다.
        material_states = []  # FBX 저장 후 기존 재질 출력으로 복원할 연결 정보입니다.
        try:  # 몸 교체를 기존 모델 파일과 같은 바인드 기준으로 저장합니다.
            root.location, root.rotation_euler = (0, 0, 0), (0, 0, 0)  # 개별 FBX의 기존 모델 원점을 유지합니다.
            rig.data.pose_position = 'REST'  # 기존 골격의 바인드 자세로 새 몸과 보존한 얼굴을 함께 저장합니다.
            used = {slot.material for obj in descendants(root) if obj.type == 'MESH' for slot in obj.material_slots if slot.material}  # 현재 캐릭터가 실제 사용하는 재질만 전달합니다.
            for material in used:  # 원본 노드를 남겨 두고 저장 중에만 FBX용 표면을 연결합니다.
                tree = material.node_tree  # 현재 사용 중인 실제 재질 구성입니다.
                output = next(node for node in tree.nodes if node.type == 'OUTPUT_MATERIAL' and node.is_active_output)  # 기존 활성 표면 출력입니다.
                connections = [(link.from_socket, link.to_socket) for link in output.inputs['Surface'].links]  # 보존할 얼굴과 의상 재질의 기존 연결입니다.
                for link in list(output.inputs['Surface'].links):  # FBX용 출력 연결을 잠시 마련합니다.
                    tree.links.remove(link)  # 기존 표면 노드 자체는 수정하지 않습니다.
                standard = tree.nodes.new('ShaderNodeBsdfPrincipled')  # FBX가 읽을 수 있는 색상 전달용 표면입니다.
                standard.inputs['Base Color'].default_value = material.diffuse_color  # 기존 피부와 얼굴 및 의상의 색상을 그대로 전달합니다.
                standard.inputs['Roughness'].default_value = .78  # 기존 모델과 같은 무광 표면 설정입니다.
                tree.links.new(standard.outputs[0], output.inputs['Surface'])  # 내보내는 동안만 표준 표면을 연결합니다.
                material_states.append((material, standard, connections))  # 저장 후 원래 재질로 돌려놓을 대상을 기록합니다.
            bpy.context.view_layer.update()  # 새 몸과 기존 골격의 부모 연결을 내보내기 데이터에 반영합니다.
            export_asset(root, guids)  # 기존 캐릭터 FBX와 재질 참조에 몸 수정만 반영합니다.
        finally:  # 저장 뒤 원본의 얼굴 표시와 캐릭터 배치 및 자세를 복원합니다.
            for material, standard, connections in material_states:  # FBX용으로 추가한 표면 노드만 제거합니다.
                material.node_tree.nodes.remove(standard)  # 원래 재질 구성은 유지합니다.
                for source_socket, target_socket in connections:  # 보관한 기존 표면 출력을 그대로 복원합니다.
                    material.node_tree.links.new(source_socket, target_socket)  # 얼굴과 머리카락의 기존 표시가 바뀌지 않게 합니다.
            root.location, root.rotation_euler = position, rotation  # 캐릭터의 기존 거리 배치를 복원합니다.
            rig.data.pose_position = pose_position  # 기존 뼈의 포즈와 표정 값을 건드리지 않고 표시 방식만 돌려놓습니다.
    bpy.ops.object.select_all(action='DESELECT')  # 원본에 특정 몸 부위가 선택 상태로 남지 않게 합니다.
    bpy.context.preferences.filepaths.save_version = 0  # 같은 원본의 불필요한 별도 백업 사본을 만들지 않습니다.
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / 'SoftCrosswalk.blend'))  # 얼굴과 배경을 유지한 몸 수정 결과를 기존 Blender 원본에 저장합니다.
    print('남자아이 의상 교체 저장: MintFriend.fbx, SoftCrosswalk.blend' if boy_outfit else ('어깨 연결 저장: ' if shoulders_only else '몸 표면 저장: ') + 'MintFriend.fbx, ApricotFriend.fbx, SoftCrosswalk.blend', flush=True)  # 이번 요청으로 실제 저장한 모델 파일만 보고합니다.


def update_building_roofs():  # 기존 건물의 벽과 창문 및 배치는 보존하고 지붕 메시만 교체합니다.
    global ACTIVE_ROOT  # 새 지붕이 해당 건물의 기존 부모 아래에 만들어지게 합니다.
    for name in COLORS:  # 지붕과 처마가 원본의 기존 재질을 재사용하게 합니다.
        material = bpy.data.materials.get('Soft_' + name)  # 장면에 저장된 실제 재질입니다.
        if material is not None:  # 존재하는 재질만 제작 목록에 연결합니다.
            MATERIALS[name] = material  # 건물의 색상과 다른 부위의 표면은 바꾸지 않습니다.
    specifications = [('SoftSchool', 'SchoolRoof', 15.8, 5.25, 6.7, 5.95, 'Roof'), ('PeachTownhouse', 'Roof', 4.6, 5.22, 6.22, 4.02, 'Roof'), ('BlueTownhouse', 'Roof', 4.3, 6.52, 7.52, 4.02, 'Coral'), ('CornerShop', 'Roof', 5.0, 4.52, 5.52, 4.02, 'Roof')]  # 현재 네 건물의 벽 크기와 위치에 맞는 지붕 규격입니다.
    roots = []  # 변경한 건물의 개별 FBX를 저장할 대상입니다.
    for root_name, part_name, width, base, peak, depth, material in specifications:  # 요청한 네 지붕만 원래 건물 안에서 교체합니다.
        root = bpy.data.objects[root_name]  # 벽과 문 및 창문을 가진 기존 건물 루트입니다.
        previous = next(obj for obj in root.children if obj.get('export_name') == part_name)  # 교체할 지붕의 실제 메시입니다.
        previous_name, transform, data = previous.name, previous.matrix_basis.copy(), previous.data  # 지붕의 부위 이름과 로컬 배치를 보존합니다.
        bpy.data.objects.remove(previous, do_unlink=True)  # 벽과 다른 건축 부품을 건드리지 않고 이전 지붕만 제거합니다.
        if data.users == 0:  # 다른 배치가 사용하지 않는 이전 지붕 데이터만 정리합니다.
            bpy.data.meshes.remove(data)  # 교체한 지붕의 불필요한 이전 메시를 남기지 않습니다.
        ACTIVE_ROOT = root  # 새 지붕을 기존 건물의 로컬 좌표로 제작합니다.
        replacement = roof(part_name, width, base, peak, depth, material)  # 두꺼운 처마와 용마루 및 네 경사면을 실제 메시로 만듭니다.
        replacement.name, replacement.matrix_basis = previous_name, transform  # 원본의 부위 이름과 건물 안의 배치를 이어받습니다.
        roots.append(root)  # 교체한 건물의 개별 출력 대상을 기록합니다.
    roots.append(bpy.data.objects['SoftStreetSet'])  # 같은 건물들이 포함된 거리 조립 FBX에도 새 지붕을 반영합니다.
    guids = {name: next(line[6:].strip() for line in (OUTPUT / 'Materials' / ('Soft_' + name + '.mat.meta')).read_text(encoding='utf-8').splitlines() if line.startswith('guid: ')) for name in COLORS}  # 기존 재질의 Unity 참조를 그대로 연결합니다.
    used = {slot.material for root in roots for obj in descendants(root) if obj.type == 'MESH' for slot in obj.material_slots if slot.material}  # 출력 대상에 이미 연결된 실제 재질만 모읍니다.
    material_states = []  # FBX용 표면을 연결한 뒤 원본의 기존 재질로 복원할 정보입니다.
    try:  # 개별 건물과 조립 거리의 표면색을 같은 방식으로 저장합니다.
        for material in used:  # 기존 카툰 노드를 보존하면서 FBX에 표준 표면색을 전달합니다.
            tree = material.node_tree  # 해당 부위가 사용 중인 실제 표면 구성입니다.
            output = next(node for node in tree.nodes if node.type == 'OUTPUT_MATERIAL' and node.is_active_output)  # 현재 활성 재질 출력입니다.
            connections = [(link.from_socket, link.to_socket) for link in output.inputs['Surface'].links]  # 원본 표면의 연결을 보관합니다.
            for link in list(output.inputs['Surface'].links):  # FBX 저장 동안 사용할 출력 자리를 마련합니다.
                tree.links.remove(link)  # 원본 재질의 노드 자체는 보존합니다.
            standard = tree.nodes.new('ShaderNodeBsdfPrincipled')  # FBX가 읽는 기본 표면 노드입니다.
            standard.inputs['Base Color'].default_value = material.diffuse_color  # 기존 건물과 지붕 및 소품의 실제 색상입니다.
            standard.inputs['Roughness'].default_value = .78  # 기존 모델과 같은 무광 표면으로 전달합니다.
            tree.links.new(standard.outputs[0], output.inputs['Surface'])  # 저장하는 동안만 표준 표면을 연결합니다.
            material_states.append((material, standard, connections))  # 저장 이후 원본 연결을 복원할 대상을 기록합니다.
        for root in roots:  # 네 개별 건물과 하나의 거리 조립본을 각각 저장합니다.
            position, rotation = root.location.copy(), root.rotation_euler.copy()  # 원본 장면에서의 기존 배치를 보관합니다.
            try:  # 개별 건물은 모델 원점을 기준으로 내보냅니다.
                root.location, root.rotation_euler = (0, 0, 0), (0, 0, 0)  # 기존 FBX와 동일한 배치 기준으로 저장합니다.
                bpy.context.view_layer.update()  # 새 지붕과 부모 좌표를 내보내기 데이터에 반영합니다.
                export_asset(root, guids)  # 기존 에셋 이름과 GUID를 유지하며 수정된 실제 모델을 저장합니다.
            finally:  # 개별 출력 뒤 편집 원본의 거리 배치를 복원합니다.
                root.location, root.rotation_euler = position, rotation  # 건물과 거리의 위치 및 방향을 그대로 돌려놓습니다.
    finally:  # 원본에는 기존 카툰 표면 연결만 남깁니다.
        for material, standard, connections in material_states:  # 내보내기용으로 추가한 표준 노드만 제거합니다.
            material.node_tree.nodes.remove(standard)  # 기존 건물과 소품의 재질 구성은 유지합니다.
            for source_socket, target_socket in connections:  # 저장 전 원본의 실제 출력 연결을 복원합니다.
                material.node_tree.links.new(source_socket, target_socket)  # Blender 표시가 FBX용 임시 표면으로 남지 않게 합니다.
    bpy.ops.object.select_all(action='DESELECT')  # 저장 시 지붕이나 거리 전체가 선택된 상태로 남지 않게 합니다.
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / 'SoftCrosswalk.blend'))  # 캐릭터 수정과 새 지붕이 함께 들어 있는 편집 원본을 저장합니다.
    print('지붕 저장: 네 건물 FBX, SoftStreetSet.fbx, SoftCrosswalk.blend', flush=True)  # 실제 저장한 모델만 보고합니다.


if __name__ == '__main__':  # 명시적으로 호출한 모델 제작 명령에서만 작업합니다.
    if '--girl-hair' in sys.argv:  # 여자아이의 앞머리 틈만 기존 스타일 안에서 수정하는 제작 범위입니다.
        update_character_hair(girl=True)  # 얼굴과 몸 및 남자아이를 유지하고 여자아이 머리만 저장합니다.
    elif '--boy-hair' in sys.argv:  # 남자아이 머리카락만 짧은 흑갈색 스타일로 교체하는 제작 범위입니다.
        update_character_hair()  # 얼굴과 옷 및 다른 캐릭터를 유지하고 남자아이 머리만 저장합니다.
    elif '--boy-outfit' in sys.argv:  # 남자아이의 이전 옷을 새 후드티와 반바지로 교체하는 제작 범위입니다.
        update_character_bodies(boy_outfit=True)  # 얼굴과 손 및 신발과 여자아이를 유지하고 남자아이 의상만 저장합니다.
    elif '--shoulders-only' in sys.argv:  # 현재 얼굴과 손 및 하의를 유지하면서 어깨 연결과 상의 웨이트만 수정합니다.
        update_character_bodies(shoulders_only=True)  # 몸판을 지지하는 웨이트와 공유 정점으로 이어진 소매를 저장합니다.
    elif '--bodies-only' in sys.argv:  # 얼굴을 보존하고 몸 표면과 의상 연결부만 수정하는 제작 범위입니다.
        update_character_bodies()  # 기존 골격과 얼굴 메시를 유지하면서 두 친구의 둥근 몸을 저장합니다.
    elif '--reference-models' in sys.argv:  # 이번 요청의 캐릭터와 건물 지붕을 함께 수정하는 제작 범위입니다.
        update_reference_characters()  # 두 친구의 얼굴과 몸 및 골격을 같은 비율로 제작합니다.
        update_building_roofs()  # 기존 배경에서 네 건물의 지붕만 새 입체 구조로 교체합니다.
    elif '--reference-characters' in sys.argv:  # 참고 이미지의 얼굴과 몸 비율을 함께 제작하는 범위입니다.
        update_reference_characters()  # 캐릭터 두 명과 필요한 재질만 새로 저장합니다.
    elif '--heads-only' in sys.argv:  # 얼굴과 헤어 수정에서는 저장된 원본과 기존 골격을 재사용합니다.
        update_character_heads()  # 캐릭터의 얼굴 및 헤어와 대응 원본만 저장합니다.
    else:  # 전체 세트를 명시적으로 제작할 때의 기존 진입점입니다.
        main()  # 컴파일이나 게임 플레이 및 테스트 없이 실제 모델 에셋을 저장합니다.
