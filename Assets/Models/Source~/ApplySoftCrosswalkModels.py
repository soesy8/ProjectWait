import bpy  # 현재 Blender 원본의 완성 메시와 기존 스킨 웨이트를 게임 에셋으로 저장합니다.
import copy  # 두 캐릭터의 프리팹 식별자는 유지하면서 서로 다른 외형을 작성합니다.
import math  # 기존 배치의 회전과 모델 크기를 연결합니다.
import re  # Unity 직렬화 문서에서 요청과 관계된 필드만 교체합니다.
import struct  # 프로젝트에서 이미 사용하는 네이티브 메시 정점 버퍼를 작성합니다.
import uuid  # 새 게임용 메시와 여자아이 프리팹에만 Unity 식별자를 만듭니다.
import zlib  # 원본 표정 이름을 Unity 메시의 블렌드셰이프 채널에 기록합니다.
from pathlib import Path  # 현재 프로젝트의 모델과 프리팹 및 게임 씬에만 저장합니다.
from mathutils import Matrix, Quaternion, Vector  # 원본 메시와 골격의 전체 바인드 행렬을 게임 좌표로 옮깁니다.

AREA = Path(__file__).resolve().parents[2]  # 모델 원본에서 두 단계 올라가 게임 에셋이 모인 Assets를 찾습니다.
MODELS = AREA / 'Models' / 'SoftCrosswalk'  # 완성 모델과 실제 게임용 메시가 공유할 폴더입니다.
OUTPUT = MODELS / 'GameMeshes'  # 기존 Unity 네이티브 메시 방식으로 저장할 실제 게임 에셋입니다.
PREFABS = AREA / 'Prefabs'  # 기존 게임 제어기와 충돌체가 연결된 프리팹입니다.
SCENE = AREA / 'Scenes' / 'CrosswalkScene.unity'  # 현재 상황 제어기와 친구 두 명이 배치된 게임 씬입니다.
HEADER = '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'  # Unity 직렬화 파일의 기존 헤더입니다.
AXIS = Matrix(((1, 0, 0, 0), (0, 0, 1, 0), (0, -1, 0, 0), (0, 0, 0, 1)))  # 형태와 길이를 바꾸지 않는 Blender Z-up에서 Unity Y-up으로의 축 변환입니다.


def write(path, text):  # 실제 게임 에셋을 원래 Unity 텍스트 형식으로 저장합니다.
    path.parent.mkdir(parents=True, exist_ok=True)  # 출력 에셋에 필요한 폴더만 만듭니다.
    path.write_text(text.rstrip() + '\n', encoding='utf-8')  # 직렬화 본문에는 소스 코드용 주석을 넣지 않습니다.


def metadata(path, importer='NativeFormatImporter', main_id=4300000, folder=False):  # 기존 에셋 참조를 유지하고 새 출력에만 식별자를 만듭니다.
    target = Path(str(path) + '.meta')  # 해당 게임 에셋의 Unity 메타 파일입니다.
    guid = re.search(r'^guid: (\w+)', target.read_text(encoding='utf-8'), re.M).group(1) if target.exists() else uuid.uuid4().hex  # 재적용 시에도 같은 GUID를 사용합니다.
    extra = 'folderAsset: yes\n' if folder else ''  # 메시 폴더와 일반 에셋을 구분합니다.
    main = f'  mainObjectFileID: {main_id}\n' if main_id else ''  # 네이티브 메시의 실제 메인 오브젝트를 지정합니다.
    write(target, f'fileFormatVersion: 2\nguid: {guid}\n{extra}{importer}:\n  externalObjects: {{}}\n{main}  userData: \n  assetBundleName: \n  assetBundleVariant: \n')  # 현재 프로젝트가 사용하는 메타 형식을 유지합니다.
    return guid  # 프리팹과 씬에서 연결할 식별자입니다.


def vector(values):  # Unity 벡터와 쿼터니언의 저장 형식입니다.
    return '{' + ', '.join(f'{axis}: {value:.9g}' for axis, value in zip('xyzw', values)) + '}'  # 현재 좌표의 정밀도를 유지합니다.


def unity(point):  # 기존 모델 제작 파일과 같은 Blender 좌표 변환을 사용합니다.
    return Vector((point.x, point.z, -point.y))  # 게임의 높이는 Y축이고 모델 정면은 +Z입니다.


def descendants(root):  # 선택한 완성 모델의 실제 부위만 가져옵니다.
    return [root] + [item for child in root.children for item in descendants(child)]  # 다른 캐릭터나 배경 원형은 포함하지 않습니다.


def read_documents(path):  # Unity 파일을 기존 문서 단위로 읽어 수정 범위를 제한합니다.
    text = path.read_text(encoding='utf-8')  # 현재 프리팹 또는 게임 씬의 실제 내용입니다.
    return [{'kind': int(match[1]), 'id': int(match[2]), 'suffix': match[3], 'body': match[4]} for match in re.finditer(r'^--- !u!(\d+) &(-?\d+)([^\n]*)\n(.*?)(?=^--- !u!|\Z)', text, re.M | re.S)]  # 알 수 없는 다른 컴포넌트의 본문은 그대로 보존합니다.


def save_documents(path, documents):  # 요청한 필드만 바뀐 Unity 파일을 저장합니다.
    write(path, HEADER + ''.join(f'--- !u!{doc["kind"]} &{doc["id"]}{doc["suffix"]}\n{doc["body"]}' for doc in documents))  # 원래 문서 식별자와 나머지 본문을 유지합니다.


def set_vector(body, field, values):  # 기존 Transform과 경계의 벡터 필드만 교체합니다.
    return re.sub(r'(?m)^(\s*' + re.escape(field) + r': )\{[^\n]+\}', lambda match: match[1] + vector(values), body)  # 같은 문서의 다른 필드에는 접근하지 않습니다.


def reference(body, field):  # 현재 문서의 실제 Unity 오브젝트 참조를 읽습니다.
    return int(re.search(re.escape(field) + r': \{fileID: (-?\d+)', body).group(1))  # 기존 계층과 컴포넌트 식별자를 재사용합니다.


def material_references(palette):  # 모델에 실제 사용한 재질을 게임 머티리얼에 연결합니다.
    return ''.join('  - {fileID: 2100000, guid: ' + re.search(r'^guid: (\w+)', (MODELS / 'Materials' / (name + '.mat.meta')).read_text(encoding='utf-8'), re.M).group(1) + ', type: 2}\n' for name in palette)  # Blender 모델과 같은 색상 및 카툰 셰이더를 사용합니다.


def replace_materials(body, palette):  # 렌더러의 재질 배열만 완성 모델의 서브메시 순서에 맞춥니다.
    return re.sub(r'(?ms)^  m_Materials:\n(?:  - [^\n]*\n)*', '  m_Materials:\n' + material_references(palette), body)  # 스크립트와 렌더러의 기존 식별자는 유지합니다.


def export_mesh(root, name, bones=(), objects=None, placement=None):  # 원본 정점과 재질 및 스킨을 크기 보정 없이 Unity 메시로 옮깁니다.
    objects = objects if objects is not None else [obj for obj in descendants(root) if obj.type == 'MESH']  # 현재 출력에 속한 실제 메시 부위입니다.
    bone_index = {bone: index for index, bone in enumerate(bones)}  # 기존 게임 프리팹의 관절 배열 순서를 유지합니다.
    inverse = root.matrix_world.inverted()  # 거리 원본의 배치를 제외한 개별 모델 좌표입니다.
    placement = placement if placement is not None else Matrix.Identity(4)  # 기존 소품의 Renderer와 Transform을 유지할 때 필요한 좌표 변환입니다.
    placed_normal = placement.to_3x3().inverted().transposed()  # 기존 비균일 크기의 소품에서도 표면 법선을 맞춥니다.
    vertices, normals, uvs, weights, triangles = [], [], [], [], {}  # 원본 정점과 모서리 법선 및 UV와 실제 스킨을 기록합니다.
    blend_shapes = {}  # 원본 눈과 입의 표정 변위를 통합 메시 정점 번호에 연결합니다.
    for obj in sorted(objects, key=lambda item: item.name):  # 같은 모델을 다시 적용해도 서브메시 순서가 유지되게 합니다.
        data = obj.data  # 원본의 편집 가능한 실제 표면이며 포즈 변형은 중복 적용하지 않습니다.
        reference_key = data.shape_keys.reference_key if data.shape_keys else None  # 표정이 있는 부위는 Blender가 기본 얼굴로 표시하는 기준 키를 읽습니다.
        base_vertices = reference_key.data if reference_key else data.vertices  # 기준 키와 다를 수 있는 원시 정점 대신 실제 기본 얼굴의 위치를 사용합니다.
        data.calc_loop_triangles()  # 완성된 면을 Unity 메시의 삼각형으로 저장합니다.
        base_normals = reference_key.normals_split_get() if reference_key else None  # 기본 얼굴의 위치에 대응하는 모서리 법선을 함께 가져옵니다.
        transform = inverse @ obj.matrix_world  # 부위의 원래 로컬 배치와 회전을 모델 안에 반영합니다.
        if obj.get('export_name') in ('SchoolName', 'Detail_ShopLettering'):  # Unity에서 건물 정면을 볼 때 좌우가 뒤집히는 간판 글자만 선택합니다.
            transform = transform @ Matrix.Diagonal((-1, 1, 1, 1))  # 글자의 원점을 기준으로 가로축을 반전하고 아래의 기존 삼각형 순서 보정으로 앞면을 유지합니다.
        normal_transform = transform.to_3x3().inverted().transposed()  # 크기가 다른 부위에서도 올바른 표면 법선을 사용합니다.
        mapped = {}  # 같은 위치라도 날카로운 모서리의 법선은 분리하여 보존합니다.
        for triangle in data.loop_triangles:  # 현재 부위의 실제 삼각형을 재질별로 기록합니다.
            material = obj.material_slots[triangle.material_index].material.name  # 해당 면에 실제 연결된 모델 재질입니다.
            target = triangles.setdefault(material, [])  # 같은 재질의 면을 하나의 서브메시로 모읍니다.
            loops = list(triangle.loops)  # 부드러운 면과 평평한 면의 실제 모서리 법선을 읽습니다.
            if transform.to_3x3().determinant() * placement.to_3x3().determinant() < 0:  # 반사 배치가 있는 부위에서는 바깥 면 방향을 보존합니다.
                loops.reverse()  # 좌우 반전으로 면이 뒤집혀 보이지 않게 합니다.
            for loop_index in loops:  # 삼각형을 구성하는 각 실제 모서리 정점입니다.
                source_index = data.loops[loop_index].vertex_index  # 해당 모서리가 참조하는 피부 또는 의상 정점입니다.
                source_normal = Vector(base_normals[loop_index * 3:loop_index * 3 + 3]) if reference_key else data.corner_normals[loop_index].vector  # 표정 부위의 기본 표면과 일반 메시에서 각각 올바른 법선을 읽습니다.
                normal = (placed_normal @ unity(normal_transform @ source_normal)).normalized()  # 기존 소품의 좌표 변환까지 반영한 완성 표면 법선입니다.
                uv = tuple(data.uv_layers.active.data[loop_index].uv) if data.uv_layers.active else (0., 0.)  # 원본에 UV가 있으면 실제 모서리 좌표를 그대로 전달합니다.
                key = (source_index, tuple(normal), uv)  # 원본의 법선과 UV 경계가 다른 정점은 합치지 않습니다.
                if key not in mapped:  # 아직 출력하지 않은 실제 표면 정점입니다.
                    mapped[key] = len(vertices)  # 통합 게임 메시에서의 정점 인덱스입니다.
                    vertices.append(placement @ unity(transform @ base_vertices[source_index].co))  # Blender에서 보이는 기본 얼굴과 완성 실루엣을 기존 게임 좌표로 옮깁니다.
                    if bones and data.shape_keys:  # 원본 캐릭터가 가진 실제 표정만 보존합니다.
                        for key_block in list(data.shape_keys.key_blocks)[1:]:  # 기본 얼굴을 제외한 눈 감기와 입 표정입니다.
                            delta = placement.to_3x3() @ unity(transform.to_3x3() @ (key_block.data[source_index].co - key_block.relative_key.data[source_index].co))  # 표정 키의 실제 기준에서 변위를 구해 눈과 입에 잘못된 높이 이동이 섞이지 않게 합니다.
                            if delta.length_squared > 1e-18:  # 움직이지 않는 정점을 표정 버퍼에 중복 저장하지 않습니다.
                                blend_shapes.setdefault(key_block.name, []).append((mapped[key], delta))  # 같은 이름의 양쪽 눈과 입 부위를 하나의 표정 채널로 묶습니다.
                    normals.append(normal)  # 머리와 몸의 곡면 및 지붕의 평평한 면을 유지합니다.
                    uvs.append(uv)  # 임의로 생성한 투영 좌표 대신 원본 UV를 보존합니다.
                    values = [(bone_index[obj.vertex_groups[group.group].name], group.weight) for group in data.vertices[source_index].groups if obj.vertex_groups[group.group].name in bone_index and group.weight > .00001]  # Blender에서 확정한 어깨와 손 및 얼굴의 실제 스킨을 사용합니다.
                    weights.append(sorted(values, key=lambda item: item[1], reverse=True)[:4])  # 기존 게임 렌더러와 같은 최대 네 관절 웨이트입니다.
                target.append(mapped[key])  # 해당 재질의 실제 삼각형 인덱스입니다.
    minimum = Vector(tuple(min(point[axis] for point in vertices) for axis in range(3)))  # 발부터 머리와 옷까지 포함하는 실제 메시 최소 경계입니다.
    maximum = Vector(tuple(max(point[axis] for point in vertices) for axis in range(3)))  # 손끝과 지붕 및 소품까지 포함하는 실제 최대 경계입니다.
    centre, extent = (minimum + maximum) * .5, (maximum - minimum) * .5  # Unity가 사용할 메시 경계입니다.
    buffer = bytearray()  # 프로젝트의 기존 메시와 같은 정점 스트림입니다.
    for point, normal, uv, values in zip(vertices, normals, uvs, weights):  # 원본의 위치와 법선 및 UV와 스킨을 같은 정점에 기록합니다.
        if bones:  # 기존 캐릭터 골격에 연결할 스킨 메시입니다.
            total = sum(value for _, value in values)  # 원본 웨이트를 네 관절 범위에서 정규화합니다.
            normalized = [value / total for _, value in values] + [0.] * (4 - len(values))  # 실제 영향의 합을 1로 유지합니다.
            indices = [index for index, _ in values] + [0] * (4 - len(values))  # 프리팹 관절 배열과 같은 뼈 인덱스입니다.
            buffer.extend(struct.pack('<3f3f2f4f4I', *point, *normal, *uv, *normalized, *indices))  # 원본 UV와 실제 스킨을 64바이트 정점 형식에 기록합니다.
        else:  # 건물과 소품 등 고정된 표면에는 관절 데이터를 추가하지 않습니다.
            buffer.extend(struct.pack('<3f3f2f', *point, *normal, *uv))  # 원본의 표면과 UV만 저장하며 기존 도형 크기에 맞추지 않습니다.
    text = HEADER + f'--- !u!43 &4300000\nMesh:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_Name: {name}\n  serializedVersion: 11\n  m_SubMeshes:\n'  # 현재 Unity 6.3 프로젝트의 기존 네이티브 메시 형식입니다.
    indices = []  # 서브메시별로 이어 저장할 32비트 삼각형 인덱스입니다.
    for material, values in triangles.items():  # 실제 사용한 재질 순서로 서브메시를 연결합니다.
        start = len(indices)  # 인덱스 버퍼 안의 현재 서브메시 시작입니다.
        indices.extend(values)  # 해당 재질의 모든 삼각형을 이어 기록합니다.
        text += f'  - serializedVersion: 2\n    firstByte: {start * 4}\n    indexCount: {len(values)}\n    topology: 0\n    baseVertex: 0\n    firstVertex: 0\n    vertexCount: {len(vertices)}\n    localAABB:\n      m_Center: {vector(centre)}\n      m_Extent: {vector(extent)}\n'  # 기존 메시의 서브메시 저장 구조를 유지합니다.
    text += '  m_Shapes:\n    vertices:' + ('\n' if blend_shapes else ' []\n')  # Blender 원본에 실제로 있는 표정 정점만 직렬화합니다.
    for changes in blend_shapes.values():  # 원본 표정의 상대 정점 이동을 저장합니다.
        for index, delta in changes:  # 분리 법선으로 나뉜 출력 정점에도 같은 표정 이동을 전달합니다.
            text += f'    - vertex: {vector(delta)}\n      normal: {{x: 0, y: 0, z: 0}}\n      tangent: {{x: 0, y: 0, z: 0}}\n      index: {index}\n'  # 표정은 원본 위치 변화만 적용하고 기본 표면 법선을 유지합니다.
    text += '    shapes:' + ('\n' if blend_shapes else ' []\n')  # 각 표정의 정점 버퍼 범위를 지정합니다.
    first_vertex = 0  # 첫 번째 표정이 시작하는 실제 정점 위치입니다.
    for changes in blend_shapes.values():  # 원본과 동일하게 표정마다 하나의 완전한 프레임을 저장합니다.
        text += f'    - firstVertex: {first_vertex}\n      vertexCount: {len(changes)}\n      hasNormals: 0\n      hasTangents: 0\n'  # 저장하지 않은 법선과 탄젠트 변위는 사용하지 않습니다.
        first_vertex += len(changes)  # 다음 표정의 시작을 현재 변위 뒤로 옮깁니다.
    text += '    channels:' + ('\n' if blend_shapes else ' []\n')  # 게임에서 원본 표정 이름으로 선택할 채널입니다.
    for index, shape_name in enumerate(blend_shapes):  # 원본 이름과 표정 프레임을 연결합니다.
        text += f'    - name: {shape_name}\n      nameHash: {zlib.crc32(shape_name.encode("utf-8")) & 0xffffffff}\n      frameIndex: {index}\n      frameCount: 1\n'  # 원본 표정의 이름과 순서를 유지합니다.
    text += '    fullWeights:' + ('\n' + '    - 100\n' * len(blend_shapes) if blend_shapes else ' []\n')  # Blender 값 1을 Unity의 표정 가중치 100에 대응시킵니다.
    text += '  m_BindPose:' + ('\n' if bones else ' []\n')  # 원본 골격의 전체 바인드 행렬을 이어 저장합니다.
    if bones:  # 현재 게임이 사용하는 공통 기준축의 바인드 행렬입니다.
        rig = next(obj for obj in descendants(root) if obj.type == 'ARMATURE')  # 원본의 실제 변형 골격입니다.
        for name_in_rig in bones:  # 기존 프리팹의 같은 관절 순서로 작성합니다.
            matrix = (AXIS @ inverse @ rig.matrix_world @ rig.data.bones[name_in_rig].matrix_local).inverted()  # 원본 관절의 위치와 회전 및 크기를 모두 포함한 실제 바인드 역행렬입니다.
            for row in range(4):  # 기존 메시와 같은 4×4 바인드 행렬 구조입니다.
                for column in range(4):  # 모든 회전 및 이동 성분을 저장합니다.
                    text += ('  - ' if row == 0 and column == 0 else '    ') + f'e{row}{column}: {matrix[row][column]:.9g}\n'  # 바인드 배열을 렌더러의 기존 뼈 배열에 맞춥니다.
    text += '  m_BoneNameHashes: \n  m_RootBoneNameHash: 0\n  m_BonesAABB: []\n  m_VariableBoneCountWeights:\n    m_Data: \n  m_MeshCompression: 0\n  m_IsReadable: 0\n  m_KeepVertices: 0\n  m_KeepIndices: 0\n  m_IndexFormat: 1\n'  # 스킨은 기존 프로젝트처럼 정점 스트림에 저장합니다.
    text += '  m_IndexBuffer: ' + b''.join(struct.pack('<I', index) for index in indices).hex() + '\n'  # 실제 삼각형을 리틀 엔디언 32비트 인덱스로 저장합니다.
    text += f'  m_VertexData:\n    serializedVersion: 3\n    m_VertexCount: {len(vertices)}\n    m_Channels:\n'  # 새 모델의 실제 정점 수입니다.
    channels = {0: (0, 0, 3), 1: (12, 0, 3), 4: (24, 0, 2)}  # 위치와 법선 및 UV의 기존 표준 채널입니다.
    if bones:  # 캐릭터에만 스킨 웨이트와 관절 인덱스 채널을 추가합니다.
        channels.update({12: (32, 0, 4), 13: (48, 10, 4)})  # 기존 네이티브 메시와 같은 가중치 및 UInt32 뼈 인덱스입니다.
    for index in range(14):  # Unity의 표준 정점 속성 순서로 채널을 작성합니다.
        offset, fmt, dimension = channels.get(index, (0, 0, 0))  # 사용하지 않는 채널의 차원은 0입니다.
        text += f'    - stream: 0\n      offset: {offset}\n      format: {fmt}\n      dimension: {dimension}\n'  # 각 정점 속성의 실제 버퍼 위치입니다.
    text += f'    m_DataSize: {len(buffer)}\n    _typelessdata: {buffer.hex()}\n  m_CompressedMesh:\n'  # 외부 변환이나 실행 코드 없이 실제 메시 버퍼를 저장합니다.
    for field in ('Vertices', 'UV', 'Normals', 'Tangents', 'Weights', 'NormalSigns', 'TangentSigns', 'FloatColors', 'BoneIndices', 'Triangles'):  # 기존 메시와 같은 빈 압축 채널입니다.
        text += f'    m_{field}:\n      m_NumItems: 0\n'  # 압축하지 않은 원본 정점과 인덱스를 사용합니다.
        if field in ('Vertices', 'UV', 'Normals', 'Tangents', 'FloatColors'):  # 실수 채널의 기본 범위 필드입니다.
            text += '      m_Range: 0\n      m_Start: 0\n'  # 별도 압축 범위는 사용하지 않습니다.
        text += '      m_Data: \n      m_BitSize: 0\n'  # 별도 압축 데이터는 없습니다.
    text += f'    m_UVInfo: 0\n  m_LocalAABB:\n    m_Center: {vector(centre)}\n    m_Extent: {vector(extent)}\n  m_MeshUsageFlags: 0\n  m_CookingOptions: 30\n  m_BakedConvexCollisionMesh: \n  m_BakedTriangleCollisionMesh: \n  m_MeshOptimizationFlags: 1\n  m_StreamData:\n    serializedVersion: 2\n    offset: 0\n    size: 0\n    path: \n'  # 기존 충돌체와 외부 스트림은 변경하지 않습니다.
    path = OUTPUT / (name + '.asset')  # 프리팹에서 직접 참조할 실제 게임 메시입니다.
    guid = metadata(path)  # 기존 파일이 있으면 같은 식별자를 이어 사용합니다.
    write(path, text)  # 완성 모델의 표면과 스킨을 게임 에셋으로 저장합니다.
    return {'guid': guid, 'palette': list(triangles), 'centre': centre, 'extent': extent, 'shapes': list(blend_shapes)}  # 원본 외형과 표정에 필요한 실제 연결 정보를 반환합니다.


def apply_character(root_name, path, template):  # 기존 게임 컴포넌트와 관절 식별자에 완성 캐릭터를 연결합니다.
    docs = copy.deepcopy(template)  # 두 캐릭터가 서로의 외형과 재질을 덮어쓰지 않게 합니다.
    by_id = {doc['id']: doc for doc in docs}  # 기존 프리팹의 실제 문서 식별자입니다.
    names = {doc['id']: re.search(r'^  m_Name: (.+)', doc['body'], re.M).group(1) for doc in docs if doc['kind'] == 1}  # 기존 관절 이름과 게임 오브젝트의 연결입니다.
    renderer = by_id[13700000]  # 게임과 관찰 테두리가 이미 참조하는 기존 스킨 렌더러입니다.
    bone_section = re.search(r'(?ms)^  m_Bones:\n(.*?)(?=^  m_BlendShapeWeights:)', renderer['body']).group(1)  # 기존 렌더러의 실제 55개 관절 순서입니다.
    bone_ids = [int(value) for value in re.findall(r'fileID: (\d+)', bone_section)]  # 기존 프리팹 관절의 Transform 식별자를 유지합니다.
    bones = [names[reference(by_id[identifier]['body'], 'm_GameObject')] for identifier in bone_ids]  # 기존 배열 순서에 대응하는 실제 뼈 이름입니다.
    root = bpy.data.objects[root_name]  # Blender에서 수정한 완성 캐릭터입니다.
    rig = next(obj for obj in descendants(root) if obj.type == 'ARMATURE')  # 현재 얼굴과 몸 및 옷에 사용한 실제 골격입니다.
    mesh = export_mesh(root, root_name + 'Skin', bones)  # 현재 프로젝트의 스킨 메시 방식으로 같은 외형을 저장합니다.
    bind_matrices = {bone.name: AXIS @ root.matrix_world.inverted() @ rig.matrix_world @ bone.matrix_local for bone in rig.data.bones}  # 원본 관절의 위치와 회전 및 길이를 포함하는 모델 기준 행렬입니다.
    spaces = []  # 기존 연출을 원본 관절축으로 전달할 바인드 정보입니다.
    for name, identifier in zip(bones, bone_ids):  # 기존 식별자는 참조 연결에만 사용하고 관절 데이터는 원본에서 가져옵니다.
        bone = rig.data.bones[name]  # Blender 원본의 실제 관절입니다.
        parent_matrix = bind_matrices[bone.parent.name] if bone.parent else Matrix.Identity(4)  # 원본 부모 관절의 모델 기준 좌표입니다.
        local = parent_matrix.inverted() @ bind_matrices[name]  # 원본 관절을 실제 부모의 로컬 좌표로 변환합니다.
        position, rotation, scale = local.decompose()  # 바인드 위치와 회전 및 크기를 모두 보존합니다.
        body = set_vector(by_id[identifier]['body'], 'm_LocalPosition', position)  # 임시 골격의 공통 축 오프셋을 원본 위치로 교체합니다.
        body = set_vector(body, 'm_LocalRotation', (rotation.x, rotation.y, rotation.z, rotation.w))  # 원본 팔과 손가락 및 발의 관절 방향을 보존합니다.
        by_id[identifier]['body'] = set_vector(body, 'm_LocalScale', scale)  # 관절 길이를 임시 모델에 맞춰 늘이거나 줄이지 않습니다.
        bind, parent_bind = bind_matrices[name].to_quaternion(), parent_matrix.to_quaternion()  # 기존 동작의 회전 좌표만 바꿀 기준입니다.
        spaces.append(f'  - Bone: {{fileID: {identifier}}}\n    Bind: {vector((bind.x, bind.y, bind.z, bind.w))}\n    ParentBind: {vector((parent_bind.x, parent_bind.y, parent_bind.z, parent_bind.w))}\n')  # 프리팹에 원본 관절 기준을 명시적으로 저장합니다.
    rig_guid = re.search(r'^guid: (\w+)', (AREA / 'Scripts' / 'Characters' / 'BlenderRigPose.cs.meta').read_text(encoding='utf-8'), re.M).group(1)  # 캐릭터 스크립트 폴더에서 기존 골격 연결 컴포넌트의 GUID를 읽습니다.
    component_id = 114600005  # 이 프리팹에서 비어 있는 원본 골격 정보 컴포넌트 식별자입니다.
    component_body = f'MonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: 600000}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {{fileID: 11500000, guid: {rig_guid}, type: 3}}\n  m_Name: \n  m_EditorClassIdentifier: Assembly-CSharp::Y.Crosswalk.BlenderRigPose\n  m_Renderer: {{fileID: 13700000}}\n  m_BoneSpaces:\n' + ''.join(spaces)  # 원본 렌더러와 실제 관절만 연결합니다.
    if component_id in by_id:  # 재적용 시 같은 컴포넌트의 원본 데이터만 갱신합니다.
        by_id[component_id]['body'] = component_body  # 원본 기준이 중복 등록되지 않게 합니다.
    else:  # 최초 적용 시 기존 캐릭터 루트에 원본 기준을 연결합니다.
        by_id[600000]['body'] = by_id[600000]['body'].replace('  m_Layer:', f'  - component: {{fileID: {component_id}}}\n  m_Layer:', 1)  # 기존 이동 및 입력 컴포넌트의 연결은 보존합니다.
        docs.append({'kind': 114, 'id': component_id, 'suffix': '', 'body': component_body})  # 원본 골격 좌표에 필요한 정보만 추가합니다.
    renderer['body'] = re.sub(r'(?m)^  m_Mesh: .*', '  m_Mesh: {fileID: 4300000, guid: ' + mesh['guid'] + ', type: 2}', renderer['body'])  # 이전 학생 메시를 현재 완성 모델로 교체합니다.
    renderer['body'] = replace_materials(renderer['body'], mesh['palette'])  # 후드티와 반바지 및 두 캐릭터의 머리색을 서브메시 순서에 맞춥니다.
    renderer['body'] = set_vector(renderer['body'], 'm_Center', mesh['centre'])  # 새 머리와 신발이 포함되는 스킨 경계 중심입니다.
    renderer['body'] = set_vector(renderer['body'], 'm_Extent', mesh['extent'] + Vector((.10, .10, .10)))  # 기존 관절 연출 중 손과 머리가 경계 밖으로 잘리지 않게 합니다.
    renderer['body'] = re.sub(r'(?ms)^  m_BlendShapeWeights:.*?(?=^  m_RootBone:)', '  m_BlendShapeWeights:\n' + '  - 0\n' * len(mesh['shapes']), renderer['body'])  # 원본의 열린 눈과 기본 미소를 초기 표정으로 사용합니다.
    collider = by_id[136600000]  # 기존 상호작용과 차량 판정이 참조하는 캐릭터 충돌체입니다.
    collider['body'] = set_vector(collider['body'], 'm_Center', (0, mesh['centre'].y, 0))  # 실제 머리와 신발 사이의 높이를 충돌체 중심에 반영합니다.
    collider['body'] = re.sub(r'(?m)^  m_Height: .*', f'  m_Height: {mesh["extent"].y * 2:.9g}', collider['body'])  # 임시 캐릭터의 높이가 새 모델 위로 남지 않게 합니다.
    save_documents(path, docs)  # 기존 애니메이터와 이동 및 상호작용 컴포넌트를 유지한 게임 프리팹입니다.
    return mesh  # 게임 씬과 관찰 테두리에 같은 외형을 연결합니다.


def apply_scene_characters(boy, girl, girl_guid):  # 게임 씬의 친구 두 명과 기존 관찰 및 표정 연결을 새 외형에 맞춥니다.
    docs = read_documents(SCENE)  # 기존 경로와 XR 및 상황 진행은 그대로 읽습니다.
    by_id = {doc['id']: doc for doc in docs}  # 씬에서 이미 사용 중인 컴포넌트 식별자입니다.
    old_guid = re.search(r'^guid: (\w+)', (PREFABS / 'FriendCharacter.prefab.meta').read_text(encoding='utf-8'), re.M).group(1)  # 친구 1이 계속 사용할 기존 프리팹 참조입니다.
    legacy_mesh = '869935fa36894a29a47c2aeecf7f3fad'  # 현재 게임 씬의 관찰 테두리가 참조하는 이전 학생 메시입니다.
    for doc in docs:  # 기존 씬 문서에서 모델에 직접 관계된 연결만 변경합니다.
        if doc['id'] == 60000000 or (re.search(r'^  m_PrefabInstance: \{fileID: 60000000\}', doc['body'], re.M) and 'm_CorrespondingSourceObject:' in doc['body']):  # 기존 친구 2 인스턴스와 그 실제 관절 참조입니다.
            doc['body'] = doc['body'].replace(old_guid, girl_guid)  # 같은 관절 식별자를 가진 여자아이 프리팹으로 연결합니다.
        if doc['kind'] == 1001 and doc['id'] in (60000000, 80000000):  # 두 친구의 기존 위치와 상황 설정은 유지합니다.
            doc['body'] = re.sub(r'(?m)^    - target: \{fileID: 13700000, guid: \w+, type: 3\}\n      propertyPath: m_Materials\.Array\.data\[3\]\n      value: [^\n]*\n      objectReference: [^\n]*\n', '', doc['body'])  # 이전 상의 재질 번호로 새 얼굴과 옷 색상을 덮어쓰지 않게 합니다.
        if doc['kind'] == 137 and legacy_mesh in doc['body']:  # 기존 관찰 테두리도 새 캐릭터와 같은 스킨을 사용합니다.
            root_id = reference(doc['body'], 'm_RootBone')  # 기존 테두리가 가리키는 실제 친구의 골격입니다.
            instance = reference(by_id[root_id]['body'], 'm_PrefabInstance')  # 친구 1과 친구 2를 기존 인스턴스로 구분합니다.
            mesh = girl if instance == 60000000 else boy  # 각각의 옷과 머리 및 체형에 같은 관찰 테두리를 적용합니다.
            doc['body'] = doc['body'].replace(legacy_mesh, mesh['guid'])  # 기존 테두리 동작은 유지하고 메시만 교체합니다.
    for identifier in (4000000022, 9200001158):  # 기존 충격 연출에서 켜는 친구 두 명의 놀란 입입니다.
        by_id[identifier]['body'] = set_vector(by_id[identifier]['body'], 'm_LocalPosition', (0, .069, .23))  # 커진 얼굴의 실제 입 높이와 앞면에 기존 표정 오브젝트를 맞춥니다.
        by_id[identifier]['body'] = set_vector(by_id[identifier]['body'], 'm_LocalScale', (1.75, 1.75, 1))  # 새 얼굴의 입 크기에 기존 놀란 입을 맞춥니다.
    by_id[4000000017]['body'] = set_vector(by_id[4000000017]['body'], 'm_LocalPosition', (0, .36, .225))  # 새 얼굴의 이마 위치에 기존 전봇대 접촉 범위를 연결합니다.
    by_id[4000000019]['body'] = set_vector(by_id[4000000019]['body'], 'm_Size', (.32, .15, .08))  # 실제 커진 머리의 앞쪽 접촉 영역에 기존 충돌체 크기를 맞춥니다.
    save_documents(SCENE, docs)  # 상황 진행과 관찰 및 전화와 인사 참조를 유지한 씬을 저장합니다.


def apply_characters():  # 기존 친구 프리팹을 유지하면서 남자아이와 여자아이를 실제 게임에 적용합니다.
    template = read_documents(PREFABS / 'FriendCharacter.prefab')  # 기존 게임 컴포넌트와 관절 ID가 연결된 원본 프리팹입니다.
    boy = apply_character('MintFriend', PREFABS / 'FriendCharacter.prefab', template)  # 친구 1에는 현재 후드티와 반바지 및 짧은 머리를 연결합니다.
    girl_path = PREFABS / 'ApricotFriendCharacter.prefab'  # 친구 2의 다른 외형에 필요한 실제 별도 프리팹입니다.
    girl_guid = metadata(girl_path, 'PrefabImporter', 0)  # 여자아이 프리팹의 기존 또는 새 참조입니다.
    girl = apply_character('ApricotFriend', girl_path, template)  # 같은 관절과 제어기를 가진 여자아이 외형을 저장합니다.
    apply_scene_characters(boy, girl, girl_guid)  # 기존 씬의 두 친구와 관찰 및 표정 오브젝트를 연결합니다.
    motion_path = Path(__file__).resolve().parent / 'CreateStudentLocomotion.py'  # 기존의 보행 클립 제작 코드입니다.
    source = motion_path.read_text(encoding='utf-8').split('\nANIMATIONS.mkdir(', 1)[0]  # 경로와 씬을 재설정하는 뒤쪽 진입부는 사용하지 않습니다.
    motion = {'__file__': str(motion_path), '__name__': 'soft_crosswalk_locomotion'}  # 기존 보행 함수가 현재 프리팹 위치를 읽을 작업 범위입니다.
    exec(source, motion)  # 기존 보행 함수와 현재 게임 골격만 불러오며 플레이나 검증은 실행하지 않습니다.
    motion['make_clip']('FriendIdle', 'idle', 2.4, 32, True)  # 새 골반 높이에서 제자리 호흡과 기존 발 접지를 유지합니다.
    motion['make_clip']('FriendWalk', 'walk', .8, 64, True)  # 새 다리 길이에 맞는 기존 거리 기반 걷기 클립입니다.
    motion['make_clip']('FriendStop', 'stop', .48, 40, False)  # 같은 비율의 마지막 발 디딤과 정지 클립을 저장합니다.
    print('게임 캐릭터 적용 저장: 친구 프리팹 2개, 스킨 메시 2개, 기존 보행 클립 3개, CrosswalkScene.unity', flush=True)  # 실제 적용해 저장한 에셋만 보고합니다.


def read_vector(body, field):  # 기존 배치에서 실제로 저장된 벡터 값을 읽습니다.
    values = re.search(re.escape(field) + r': \{([^}]+)\}', body).group(1)  # 해당 좌표 필드의 성분만 읽습니다.
    return tuple(float(value) for value in re.findall(r':\s*([-+\d.eE]+)', values))  # 기존 위치와 회전 및 크기를 유지할 수치입니다.


def layout(documents):  # 기존 게임 오브젝트의 실제 계층과 배치를 읽습니다.
    names = {doc['id']: re.search(r'^  m_Name: (.+)', doc['body'], re.M).group(1) for doc in documents if doc['kind'] == 1 and re.search(r'^  m_Name: (.+)', doc['body'], re.M)}  # 씬의 프리팹 참조 문서에는 이름을 새로 만들지 않습니다.
    transforms = {doc['id']: doc for doc in documents if doc['kind'] == 4 and 'm_LocalPosition:' in doc['body']}  # 실제로 저장된 로컬 배치만 읽습니다.
    by_game = {reference(doc['body'], 'm_GameObject'): identifier for identifier, doc in transforms.items()}  # 게임 오브젝트와 Transform의 기존 연결입니다.
    return names, transforms, by_game  # 배경과 소품의 원래 위치를 재사용합니다.


def world_matrix(transforms, identifier):  # 기존 계층의 배치를 같은 모델 좌표로 연결합니다.
    if identifier == 0 or identifier not in transforms:  # 프리팹 루트 밖은 모델의 원점입니다.
        return Matrix.Identity(4)  # 루트 위치를 임의로 추가하지 않습니다.
    body = transforms[identifier]['body']  # 현재 배경 부위의 실제 Transform입니다.
    x, y, z, w = read_vector(body, 'm_LocalRotation')  # Unity가 저장한 회전 순서입니다.
    local = Matrix.LocRotScale(Vector(read_vector(body, 'm_LocalPosition')), Quaternion((w, x, y, z)), Vector(read_vector(body, 'm_LocalScale')))  # 기존 위치와 방향 및 크기를 그대로 사용합니다.
    return world_matrix(transforms, reference(body, 'm_Father')) @ local  # 부모의 실제 배치까지 이어 계산합니다.


def clear_generated(documents):  # 같은 모델을 다시 적용할 때 이 제작 코드가 추가했던 외형만 교체합니다.
    owned = {doc['id'] for doc in documents if doc['kind'] == 1 and re.search(r'^  m_Name: SoftModel_', doc['body'], re.M)}  # 이 작업이 만든 외형 오브젝트만 찾습니다.
    identifiers = set(owned)  # 제거할 외형과 그 컴포넌트의 실제 문서 식별자입니다.
    for doc in documents:  # 기존 제어기와 충돌체 문서는 보존합니다.
        if doc['id'] in owned:  # 이 작업이 소유한 외형 오브젝트입니다.
            identifiers.update(int(value) for value in re.findall(r'component: \{fileID: (\d+)\}', doc['body']))  # 같은 외형의 Transform과 렌더링 컴포넌트만 포함합니다.
    result = [doc for doc in documents if doc['id'] not in identifiers]  # 이전에 추가한 외형의 중복만 제거합니다.
    for doc in result:  # 부모의 기존 자식 목록에서도 같은 외형 참조만 제외합니다.
        if doc['kind'] == 4:  # 다른 스크립트의 숫자 필드는 바꾸지 않습니다.
            doc['body'] = re.sub(r'(?m)^  - \{fileID: (\d+)\}\n', lambda match: '' if int(match[1]) in identifiers else match[0], doc['body'])  # 원래의 자식과 충돌체 연결은 그대로 남깁니다.
    return result  # 현재 모델을 한 번만 연결할 원본 문서입니다.


def hide_renderers(documents, transform_ids):  # 충돌체와 제어 계층은 유지하고 이전 외형만 숨깁니다.
    _, transforms, _ = layout(documents)  # 기존 배경의 실제 부모 관계입니다.
    selected = set(transform_ids)  # 교체할 외형 범위의 Transform입니다.
    changed = True  # 선택한 부위의 자식 외형도 같은 범위로 포함합니다.
    while changed:  # 창문과 지붕처럼 여러 단계 아래의 기존 외형까지 찾습니다.
        children = {identifier for identifier, doc in transforms.items() if reference(doc['body'], 'm_Father') in selected}  # 원래 계층에서 해당 부위의 실제 자식입니다.
        changed = not children.issubset(selected)  # 새로 포함할 자식이 있는 경우만 다음 단계로 이동합니다.
        selected.update(children)  # 해당 부위의 연결된 외형 범위를 유지합니다.
    game_ids = {reference(transforms[identifier]['body'], 'm_GameObject') for identifier in selected}  # 기존 렌더러가 속한 실제 게임 오브젝트입니다.
    for doc in documents:  # 모델 외형을 결정하는 렌더링 컴포넌트만 변경합니다.
        if doc['kind'] == 23 and reference(doc['body'], 'm_GameObject') in game_ids:  # 기존 충돌체와 활성 상태 및 게임 스크립트에는 접근하지 않습니다.
            doc['body'] = re.sub(r'(?m)^  m_Enabled: \d+', '  m_Enabled: 0', doc['body'])  # 같은 위치에 이전 도형이 겹쳐 보이지 않게 합니다.


def add_visual(documents, parent, name, mesh, matrix):  # 기존 제어 계층 아래에 완성 외형만 연결합니다.
    by_id = {doc['id']: doc for doc in documents}  # 실제 부모 Transform을 유지합니다.
    number = sum(1 for doc in documents if doc['kind'] == 1 and '  m_Name: SoftModel_' in doc['body'])  # 같은 파일의 추가 외형을 구분합니다.
    identifier = 78000000 + number * 10  # 기존 게임의 컴포넌트 식별자와 분리한 외형 전용 범위입니다.
    while any(identifier + offset in by_id for offset in range(4)):  # 기존 씬이 같은 번호를 사용한 경우 그 문서를 보존합니다.
        identifier += 10  # 비어 있는 외형 식별자 묶음만 배정합니다.
    position, rotation, scale = matrix.decompose()  # 원래 게임 배치에 맞춘 새 모델의 로컬 위치와 크기입니다.
    game = f'GameObject:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  serializedVersion: 6\n  m_Component:\n  - component: {{fileID: {identifier + 1}}}\n  - component: {{fileID: {identifier + 2}}}\n  - component: {{fileID: {identifier + 3}}}\n  m_Layer: 0\n  m_Name: SoftModel_{name}\n  m_TagString: Untagged\n  m_Icon: {{fileID: 0}}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1\n'  # 이동이나 충돌 기능을 추가하지 않는 외형 오브젝트입니다.
    transform = f'Transform:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: {identifier}}}\n  serializedVersion: 2\n  m_LocalRotation: {vector((rotation.x, rotation.y, rotation.z, rotation.w))}\n  m_LocalPosition: {vector(position)}\n  m_LocalScale: {vector(scale)}\n  m_ConstrainProportionsScale: 0\n  m_Children: []\n  m_Father: {{fileID: {parent}}}\n  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}\n'  # 기존 부모의 배치를 그대로 따르는 모델 Transform입니다.
    mesh_filter = f'MeshFilter:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: {identifier}}}\n  m_Mesh: {{fileID: 4300000, guid: {mesh["guid"]}, type: 2}}\n'  # 실제 완성 모델의 네이티브 메시 참조입니다.
    renderer = next(doc['body'] for doc in documents if doc['kind'] == 23)  # 해당 프리팹에서 사용 중인 렌더러 설정을 재사용합니다.
    renderer = re.sub(r'(?m)^  m_GameObject: .*', f'  m_GameObject: {{fileID: {identifier}}}', renderer)  # 기존 렌더링 옵션을 새 외형 오브젝트에 연결합니다.
    renderer = re.sub(r'(?m)^  m_Enabled: \d+', '  m_Enabled: 1', renderer)  # 이전 외형을 숨긴 상태와 구분하여 완성 모델을 표시합니다.
    renderer = replace_materials(renderer, mesh['palette'])  # 완성 모델의 실제 재질 배열입니다.
    parent_body = by_id[parent]['body']  # 기존 스크립트와 충돌체가 참조하는 부모입니다.
    if '  m_Children: []' in parent_body:  # 기존에 자식 외형이 없던 부위입니다.
        parent_body = parent_body.replace('  m_Children: []', f'  m_Children:\n  - {{fileID: {identifier + 1}}}')  # 현재 외형만 자식으로 연결합니다.
    else:  # 기존 자식과 기능 오브젝트는 그대로 유지합니다.
        parent_body = parent_body.replace('  m_Father:', f'  - {{fileID: {identifier + 1}}}\n  m_Father:', 1)  # 기존 자식 목록 끝에 새 외형을 연결합니다.
    by_id[parent]['body'] = parent_body  # 원래 부모의 컴포넌트 식별자를 유지합니다.
    documents.extend({'kind': kind, 'id': identifier + offset, 'suffix': '', 'body': body} for offset, kind, body in ((0, 1, game), (1, 4, transform), (2, 33, mesh_filter), (3, 23, renderer)))  # 렌더링에 필요한 네 문서만 추가합니다.


def set_transform(documents, identifier, matrix):  # 원본 행렬을 지정된 게임 Transform에 직접 저장합니다.
    doc = next(item for item in documents if item['id'] == identifier)  # 기존 스크립트가 참조하는 Transform 문서를 유지합니다.
    position, rotation, scale = matrix.decompose()  # 원본의 이동과 회전 및 크기를 분리합니다.
    doc['body'] = set_vector(doc['body'], 'm_LocalPosition', position)  # 원본 좌표를 부모 기준으로 저장합니다.
    doc['body'] = set_vector(doc['body'], 'm_LocalRotation', (rotation.x, rotation.y, rotation.z, rotation.w))  # 원본 방향을 쿼터니언으로 보존합니다.
    doc['body'] = set_vector(doc['body'], 'm_LocalScale', scale)  # 임시 도형의 비균일 배율을 남기지 않습니다.


def apply_source_materials(names=None):  # 필요한 재질을 지정하거나 원본 전체의 실제 노드 값을 Unity 재질에 전달합니다.
    for material in bpy.data.materials:  # 제작 스크립트의 과거 팔레트 대신 현재 원본 재질을 읽습니다.
        if names is not None and material.name not in names:  # 일부 재질만 교체할 때 관계없는 원본 재질은 저장하지 않습니다.
            continue  # 요청한 얼굴 재질만 현재 Unity 에셋에 반영합니다.
        path = MODELS / 'Materials' / (material.name + '.mat')  # 기존 재질 에셋과 GUID를 재사용합니다.
        if not path.exists():  # 현재 게임 모델에서 사용하지 않는 원본 재질은 수정하지 않습니다.
            continue  # 요청 범위의 기존 카툰 재질만 연결합니다.
        mix = next((node for node in material.node_tree.nodes if node.type == 'MIX_RGB'), None)  # 원본 카툰 표면의 실제 색상 입력입니다.
        ramp = next((node for node in material.node_tree.nodes if node.type == 'VALTORGB'), None)  # 원본의 명암 단계와 경계값입니다.
        color = tuple(mix.inputs[1].default_value) if mix else tuple(material.diffuse_color)  # 저장된 선형 색상값을 변형 없이 사용합니다.
        text = path.read_text(encoding='utf-8')  # 기존 Unity 재질의 렌더링 연결은 유지합니다.
        if material.name in {'Soft_FaceInk', 'Soft_Mouth', 'Soft_Blush'}:  # 얇은 눈과 입 표면은 원본의 앞뒷면 표시 설정을 보존합니다.
            cull = 2 if material.use_backface_culling else 0  # Blender의 뒷면 숨김 설정을 Unity의 Back 또는 Off 값으로 옮깁니다.
            text = re.sub(r'(?m)^    - _Cull: .*', f'    - _Cull: {cull}', text)  # 원본에서 보이던 입의 뒷면이 Unity에서 사라지지 않게 합니다.
        rgba = '{' + ', '.join(f'{axis}: {value:.9g}' for axis, value in zip('rgba', color)) + '}'  # 원본 색상의 각 성분을 그대로 기록합니다.
        text = re.sub(r'(?m)^(    - (?:_BaseColor|_Color): ).*', lambda match: match[1] + rgba, text)  # 임시 재질의 색으로 원본 모델을 재도색하지 않습니다.
        if ramp:  # 원본이 사용하는 카툰 명암 값도 함께 전달합니다.
            text = re.sub(r'(?m)^    - _ShadeStrength: .*', f'    - _ShadeStrength: {ramp.color_ramp.elements[0].color[0]:.9g}', text)  # 원본 그늘의 밝기를 사용합니다.
            text = re.sub(r'(?m)^    - _ShadeThreshold: .*\n?', '', text)  # 재적용 시 경계값이 중복 저장되지 않게 합니다.
            text = text.replace('    m_Colors:', f'    - _ShadeThreshold: {ramp.color_ramp.elements[1].position:.9g}\n    m_Colors:', 1)  # 원본 색상 단계가 바뀌는 위치를 전달합니다.
        write(path, text)  # 원본 재질 정보만 기존 파일에 저장합니다.


def apply_phone():  # 원본 휴대전화의 형태와 크기를 보존한 채 원본 손 관절에 연결합니다.
    root = bpy.data.objects['PocketPhone']  # 화면과 본체를 포함하는 원본 소품입니다.
    case = next(obj for obj in root.children if obj.get('export_name') == 'Case')  # 손에 쥐는 기준이 되는 실제 본체입니다.
    centre, _ = source_bounds(root, case)  # 원본 본체의 중심만 잡고 크기는 변경하지 않습니다.
    mesh = export_mesh(root, 'PocketPhoneInHand', placement=Matrix.Translation(-centre))  # 손잡기 기준만 중앙으로 옮긴 동일한 원본 메시입니다.
    screen = next(obj for obj in root.children if obj.get('export_name') == 'Screen')  # 고개가 바라볼 실제 원본 화면입니다.
    screen_centre, _ = source_bounds(root, screen)  # 임시 화면 좌표 대신 실제 원본 화면의 중심입니다.
    for filename, character in (('FriendCharacter.prefab', 'MintFriend'), ('ApricotFriendCharacter.prefab', 'ApricotFriend')):  # 두 캐릭터의 각 원본 골격에 같은 소품을 연결합니다.
        path = PREFABS / filename  # 현재 게임에서 참조하는 실제 프리팹입니다.
        docs = read_documents(path)  # 캐릭터의 기존 이동과 상호작용 연결을 보존합니다.
        rig = next(obj for obj in descendants(bpy.data.objects[character]) if obj.type == 'ARMATURE')  # 해당 캐릭터의 원본 손 관절을 읽습니다.
        bind = (AXIS @ rig.data.bones['LeftHand'].matrix_local).to_quaternion().to_matrix().to_4x4()  # 원본 손의 실제 바인드 회전입니다.
        grip = Matrix.Translation(Vector((-.057, 0, .03))) @ Matrix.Rotation(math.pi * .5, 4, 'Z')  # 기존 휴대전화 사용 동작의 손바닥 기준 장착 위치입니다.
        set_transform(docs, 603001, bind.inverted() @ grip)  # 모델이나 손을 변형하지 않고 소품 장착 좌표만 원본 손축으로 옮깁니다.
        set_transform(docs, 603011, Matrix.Translation(screen_centre - centre))  # 시선 목표가 원본 화면 한가운데를 가리키게 합니다.
        for doc in docs:  # 게임이 이미 참조하는 전화 컴포넌트만 갱신합니다.
            if doc['kind'] == 33 and reference(doc['body'], 'm_GameObject') == 603000:  # 기존 전화의 메시 참조입니다.
                doc['body'] = re.sub(r'(?m)^  m_Mesh: .*', '  m_Mesh: {fileID: 4300000, guid: ' + mesh['guid'] + ', type: 2}', doc['body'])  # 원본 메시를 같은 관찰 대상으로 연결합니다.
            if doc['kind'] == 23 and reference(doc['body'], 'm_GameObject') == 603000:  # 실제 전화의 렌더러입니다.
                doc['body'] = replace_materials(doc['body'], mesh['palette'])  # 원본 화면과 본체 색상을 유지합니다.
            if doc['kind'] == 23 and reference(doc['body'], 'm_GameObject') == 603010:  # 시선 목표로만 남기는 임시 화면입니다.
                doc['body'] = re.sub(r'(?m)^  m_Enabled: \d+', '  m_Enabled: 0', doc['body'])  # 원본 화면 위에 임시 도형이 겹치지 않게 합니다.
            if doc['kind'] == 65 and reference(doc['body'], 'm_GameObject') == 603000:  # 기존 전화 선택과 접촉 충돌체입니다.
                doc['body'] = set_vector(doc['body'], 'm_Center', mesh['centre'])  # 원본 전화의 실제 범위를 사용합니다.
                doc['body'] = set_vector(doc['body'], 'm_Size', mesh['extent'] * 2)  # 소품 크기에 맞춰 선택 범위도 교체합니다.
        save_documents(path, docs)  # 전화 소품과 화면 시선 및 기존 조작 참조를 함께 저장합니다.
    return mesh  # 씬의 관찰 외곽선에도 동일한 원본 표면을 연결합니다.


def source_bounds(root, obj):  # 원본 부위의 실제 경계를 모델 좌표로 읽습니다.
    transform = root.matrix_world.inverted() @ obj.matrix_world  # 거리 배치만 제외한 원본 부위 행렬입니다.
    points = [unity(transform @ vertex.co) for vertex in obj.data.vertices]  # 원본 정점에 축 변환만 적용합니다.
    minimum = Vector(tuple(min(point[axis] for point in points) for axis in range(3)))  # 원본 부위의 최소 좌표입니다.
    maximum = Vector(tuple(max(point[axis] for point in points) for axis in range(3)))  # 원본 부위의 최대 좌표입니다.
    return (minimum + maximum) * .5, (maximum - minimum) * .5  # 크기 변경에 사용하지 않는 실제 원본 경계입니다.


def copy_model_geometry(assembly, source, matrix, objects=None):  # 원본 메시를 게임의 기존 배치에 복제하며 형태와 크기는 보존합니다.
    objects = objects if objects is not None else [obj for obj in descendants(source) if obj.type == 'MESH']  # 해당 원본의 실제 부품만 사용합니다.
    inverse = source.matrix_world.inverted()  # Blender 편집 장면의 위치와 방향만 제거합니다.
    for obj in objects:  # 원본 부품의 상대 배치는 그대로 보존합니다.
        duplicate = obj.copy()  # 원본 메시와 재질을 공유하는 실제 게임 배치용 인스턴스입니다.
        bpy.context.scene.collection.objects.link(duplicate)  # 내보낼 게임 배치에만 복제한 부품을 등록합니다.
        duplicate.parent = assembly  # 완성된 게임 배치를 하나의 메시로 내보낼 루트입니다.
        duplicate.matrix_world = AXIS.inverted() @ matrix @ AXIS @ inverse @ obj.matrix_world  # 게임의 기존 위치에 축 변환과 강체 이동만 적용합니다.


def assembly_root(name):  # 저장할 게임 배치의 원점 루트를 준비합니다.
    root = bpy.data.objects.new(name, None)  # 원본 파일의 모델과 분리된 내보내기용 루트입니다.
    bpy.context.scene.collection.objects.link(root)  # 현재 에셋 저장 작업에만 사용합니다.
    return root  # Blender 원본 파일에는 이 배치를 저장하지 않습니다.


def rigid_placement(transforms, identifier, position=None, turn=0):  # 기존 게임 위치와 방향을 읽되 임시 도형의 배율은 사용하지 않습니다.
    world = world_matrix(transforms, identifier)  # 기존 부모 계층까지 반영한 실제 게임 배치입니다.
    original, rotation, _ = world.decompose()  # 임시 도형의 크기 성분만 제외합니다.
    return Matrix.Translation(original if position is None else position) @ rotation.to_matrix().to_4x4() @ Matrix.Rotation(turn, 4, 'Y')  # 원본 모델에 기존 위치와 필요한 정면 방향만 연결합니다.


def add_surface_tiles(assembly, source, position, width, depth=None):  # 원본 도로와 보도 및 지면 모듈을 원래 게임 구간에 반복 배치합니다.
    source_objects = [obj for obj in descendants(source) if obj.type == 'MESH']  # 원본 모듈의 실제 표면과 줄눈입니다.
    bounds = [source_bounds(source, obj) for obj in source_objects]  # 원본 모듈의 실제 길이와 폭입니다.
    minimum = Vector(tuple(min(centre[axis] - extent[axis] for centre, extent in bounds) for axis in range(3)))  # 원본 표면의 최소 경계입니다.
    maximum = Vector(tuple(max(centre[axis] + extent[axis] for centre, extent in bounds) for axis in range(3)))  # 원본 표면의 최대 경계입니다.
    length_x, length_z = maximum.x - minimum.x, maximum.z - minimum.z  # 늘이거나 줄이지 않을 원본 모듈 크기입니다.
    count_x = max(1, math.ceil(width / length_x - .00001))  # 기존 구간을 덮는 데 필요한 원본 모듈 수입니다.
    count_z = max(1, math.ceil(depth / length_z - .00001)) if depth is not None else 1  # 넓은 지면에만 원본 폭 방향 반복을 적용합니다.
    for x in range(count_x):  # 원본 길이 간격으로 표면을 이어 배치합니다.
        for z in range(count_z):  # 기존 지면 구간에 필요한 폭만 반복합니다.
            offset = Vector(((x - (count_x - 1) * .5) * length_x, 0, (z - (count_z - 1) * .5) * length_z))  # 원래 게임 구간의 중심은 유지합니다.
            copy_model_geometry(assembly, source, Matrix.Translation(position + offset))  # 모델 정점이나 축별 비율을 바꾸지 않습니다.


def apply_environment_models(background_only=False):  # 배경 배치만 수정할 때 캐릭터와 교통 소품 연결은 다시 작성하지 않습니다.
    path = PREFABS / 'CrosswalkEnvironment.prefab'  # 기존 씬이 참조하는 환경 프리팹입니다.
    docs = clear_generated(read_documents(path))  # 과거에 크기 보정을 적용했던 외형만 교체합니다.
    _, transforms, _ = layout(docs)  # 원래 게임의 위치와 지형 구간을 읽습니다.
    assembly = assembly_root('GameEnvironmentSource')  # 기존 게임 배치로 조립할 원본 표면 루트입니다.
    for name, identifier, turn in (('SoftSchool', 100261, math.pi), ('PeachTownhouse', 100671, math.pi), ('BlueTownhouse', 100761, math.pi), ('CornerShop', 100851, 0)):  # 기존 학교와 세 건물의 실제 배치입니다.
        world = world_matrix(transforms, identifier)  # 임시 외벽의 실제 중심과 지면을 읽습니다.
        position, _, scale = world.decompose()  # 기존 건물의 밑면 위치만 가져옵니다.
        position.y -= abs(scale.y) * .5  # 원본 건물도 기존 게임의 같은 바닥에 놓습니다.
        copy_model_geometry(assembly, bpy.data.objects[name], rigid_placement(transforms, identifier, position, turn))  # 원본 건물을 임시 외벽 크기에 맞춰 변형하지 않습니다.
    road_surface = bpy.data.objects.get('SoftRoadSurfaces')  # 기존 차도와 보도 위치에서 이어지도록 제작한 실제 연속 노면입니다.
    if road_surface is not None:  # 새로운 원본 노면이 있는 경우 모듈 경계가 남지 않게 합니다.
        copy_model_geometry(assembly, road_surface, Matrix.Identity(4))  # 기존 차로와 보도 높이를 유지한 원본 표면입니다.
    road_parts = () if road_surface is not None else ((100081, 'RoadStraight', 0), (100131, 'SidewalkStraight', .15), (100141, 'SidewalkStraight', .15), (100151, 'CurbSegment', .15), (100161, 'CurbSegment', .15))  # 기본 원본은 기존 모듈 연결을 유지합니다.
    for identifier, name, height in road_parts:  # 현재 원본의 실제 도로 표면을 연결합니다.
        position, _, scale = world_matrix(transforms, identifier).decompose()  # 원래 표면 중심과 구간 길이를 읽습니다.
        position.y += abs(scale.y) * .5 - height  # 기존 도로 높이와 보행 바닥 높이를 유지합니다.
        add_surface_tiles(assembly, bpy.data.objects[name], position, abs(scale.x))  # 원본 모듈을 반복하여 기존 80m 구간을 이어 줍니다.
    terrain = bpy.data.objects.get('SoftTerrain')  # 디테일 제작 원본에 저장된 하나로 이어진 지형입니다.
    if terrain is not None:  # 연속 지형이 있는 원본은 지면 타일을 겹쳐 배치하지 않습니다.
        copy_model_geometry(assembly, terrain, Matrix.Identity(4))  # 기존 게임 좌표에 맞춰 제작한 지형 전체를 연결합니다.
    ground_parts = ((100251, 'SchoolForecourt', .06),) if terrain is not None else ((100071, 'GrassGround', 0), (100251, 'SchoolForecourt', .06))  # 앞마당은 기존 보행 높이를 유지합니다.
    for identifier, name, height in ground_parts:  # 현재 원본에 필요한 기존 마당과 지면만 연결합니다.
        position, _, scale = world_matrix(transforms, identifier).decompose()  # 원래 지면의 중심과 범위입니다.
        position.y += abs(scale.y) * .5 - height  # 원래 게임의 표면 높이에 원본 지면을 놓습니다.
        add_surface_tiles(assembly, bpy.data.objects[name], position, abs(scale.x), abs(scale.z))  # 원본 지면의 형태를 유지하며 필요한 구간만 반복합니다.
    stripe_root = bpy.data.objects['CrosswalkStripes']  # 블렌더 원본의 횡단보도 모듈입니다.
    stripe = next(obj for obj in descendants(stripe_root) if obj.type == 'MESH')  # 원본 흰 줄 하나의 실제 메시입니다.
    stripe_centre, _ = source_bounds(stripe_root, stripe)  # 원본 흰 줄의 중심입니다.
    crosswalk = assembly_root('GameCrosswalkSource')  # 관찰에도 같은 실제 횡단보도 표면을 연결할 배치입니다.
    for identifier in range(100171, 100242, 10):  # 기존 게임에서 정한 여덟 흰 줄의 위치를 유지합니다.
        matrix = rigid_placement(transforms, identifier) @ Matrix.Translation(-stripe_centre)  # 원본 흰 줄의 중심만 기존 표시 위치에 놓습니다.
        copy_model_geometry(assembly, stripe_root, matrix, [stripe])  # 원본 흰 줄의 폭과 두께를 보존합니다.
        copy_model_geometry(crosswalk, stripe_root, matrix, [stripe])  # 관찰 강조도 실제 새 흰 줄과 같은 모양을 사용합니다.
    for identifier in (100111, 100121):  # 기존 양쪽 차로 정지선입니다.
        copy_model_geometry(assembly, bpy.data.objects['RoadStopLine'], rigid_placement(transforms, identifier))  # 원본 정지선의 비율을 유지하며 기존 위치에 놓습니다.
    for identifier in (100091, 100101):  # 횡단보도 양옆의 기존 중앙선 구간입니다.
        position, _, scale = world_matrix(transforms, identifier).decompose()  # 원래 중앙선의 중심과 길이를 읽습니다.
        add_surface_tiles(assembly, bpy.data.objects['RoadCenterLines'], position, abs(scale.x))  # 원본 이중 중앙선을 늘이지 않고 반복합니다.
    for name in ('SoftStreetDressing', 'SoftAtmosphere', 'SoftNeighborhood'):  # 원본의 식재와 소품 및 추가 거리 건물을 함께 연결합니다.
        source = bpy.data.objects.get(name)  # 현재 원본에 실제로 제작된 배경만 가져옵니다.
        if source is not None:  # 기본 원본을 적용할 때는 없는 배경을 새로 만들지 않습니다.
            copy_model_geometry(assembly, source, Matrix.Identity(4))  # 게임의 기존 동선 밖에 설계한 실제 원본 배경을 연결합니다.
    bpy.context.view_layer.update()  # 저장할 인스턴스의 행렬을 현재 메시 데이터에 반영합니다.
    mesh = export_mesh(assembly, 'SoftStreetSetStatic')  # 기존 게임 배치로 조립한 원본 배경의 실제 표면입니다.
    crosswalk_mesh = export_mesh(crosswalk, 'CrosswalkGameSurface')  # 기존 관찰 대상에 연결할 동일한 원본 횡단보도입니다.
    hide_renderers(docs, [100001])  # 임시 외벽과 도로 및 지면이 원본 외형과 겹치지 않게 합니다.
    add_visual(docs, 100001, 'BlenderEnvironment', mesh, Matrix.Identity(4))  # 기존 환경 루트와 모든 게임 위치를 유지합니다.
    save_documents(path, docs)  # 기존 지면 충돌체와 경로 연결은 그대로 저장합니다.
    if background_only:  # 가로수와 벤치 및 가로등 배치 교체에 필요한 저장이 끝난 경우입니다.
        return  # 관련 없는 캐릭터와 주행 소품 및 씬 설정은 수정하지 않습니다.
    assets = {'Crosswalk': crosswalk_mesh}  # 관찰과 게임 소품이 공유할 원본 메시입니다.
    for name, filename, parent in (('SoftCar', 'CartoonCar.prefab', 400001), ('SoftUtilityPole', 'CartoonUtilityPole.prefab', 800001)):  # 기존 주행과 사고 대상입니다.
        source = bpy.data.objects[name]  # 해당 블렌더 원본 소품입니다.
        visual = export_mesh(source, name)  # 원본 모양과 크기 및 재질을 그대로 저장합니다.
        target = PREFABS / filename  # 기존 제어기와 참조를 보존할 프리팹입니다.
        documents = clear_generated(read_documents(target))  # 과거의 비균일 크기 보정 외형을 제거합니다.
        hide_renderers(documents, [parent])  # 기존 임시 소품 외형만 표시하지 않습니다.
        add_visual(documents, parent, name, visual, Matrix.Identity(4))  # 기존 이동 루트에 원본 크기의 외형을 직접 연결합니다.
        if name == 'SoftUtilityPole':  # 원본 기둥의 접촉 범위만 실제 외형으로 교체합니다.
            stem = next(obj for obj in descendants(source) if obj.get('export_name') == 'Pole')  # 원본 콘크리트 기둥입니다.
            centre, extent = source_bounds(source, stem)  # 원본 기둥의 실제 경계입니다.
            collider = next(doc for doc in documents if doc['id'] == 136800000)  # 기존 사고 판정에 연결된 충돌체입니다.
            collider['body'] = set_vector(collider['body'], 'm_Center', centre)  # 새 기둥 외형의 중심을 사용합니다.
            collider['body'] = re.sub(r'(?m)^  m_Radius: .*', f'  m_Radius: {max(extent.x, extent.z):.9g}', collider['body'])  # 원본 두께만 접촉 판정에 반영합니다.
            collider['body'] = re.sub(r'(?m)^  m_Height: .*', f'  m_Height: {extent.y * 2:.9g}', collider['body'])  # 원본 높이만 접촉 판정에 반영합니다.
        save_documents(target, documents)  # 게임의 기존 위치와 이동 설정을 유지합니다.
        assets[name] = visual  # 관찰 외곽선에도 같은 원본 표면을 사용합니다.
    assets['Signal'] = apply_signal()  # 기존 신호등 위치와 상태 제어에 원본 외형을 연결합니다.
    apply_scene_environment()  # 기존 첫 상황의 건물과 가로수 위치에서도 외형만 교체합니다.
    phone = apply_phone()  # 기존 휴대전화 사용 동작을 원본 손축에 연결합니다.
    apply_scene_source(phone, assets)  # 원본 외형에 관찰과 접촉 참조만 연결합니다.

def apply_signal():  # 기존 신호등의 위치와 표시 전환을 유지하며 원본 메시로 교체합니다.
    root = bpy.data.objects['SoftPedestrianSignal']  # 원본 신호등 하나의 실제 외형입니다.
    groups = {name: next(obj for obj in root.children if obj.get('export_name') == name) for name in ('RedSignal', 'GreenSignal')}  # 기존 상태 전환에 연결할 원본 사람 도형입니다.
    symbols = {obj for group in groups.values() for obj in descendants(group)}  # 항상 표시하는 외형에서 신호 표시만 분리합니다.
    shell = [obj for obj in descendants(root) if obj.type == 'MESH' and obj not in symbols]  # 원본 기둥과 하우징 및 검은 표시판입니다.
    housing = export_mesh(root, 'SignalHousing', objects=shell)  # 임시 표시판의 크기에 맞추지 않은 원본 외형입니다.
    path = PREFABS / 'PedestrianTrafficLight.prefab'  # 기존 씬에서 사용하는 신호등 프리팹입니다.
    docs = clear_generated(read_documents(path))  # 과거 크기 보정으로 만든 외형만 교체합니다.
    hide_renderers(docs, [300001])  # 기존 임시 도형의 렌더러만 숨깁니다.
    turn = Matrix.Rotation(math.pi, 4, 'Y')  # 기존 게임에서 신호 표시가 향하던 정면 방향을 유지합니다.
    add_visual(docs, 300001, 'SignalHousing', housing, turn)  # 기존 루트 위치와 회전 및 신호 제어는 유지합니다.
    _, transforms, _ = layout(docs)  # 기존 표시 그룹의 실제 로컬 좌표입니다.
    for name, parent in (('RedSignal', 300091), ('GreenSignal', 300101)):  # 기존 제어기가 활성 상태를 바꾸는 같은 그룹입니다.
        mesh = export_mesh(root, name, objects=[obj for obj in descendants(groups[name]) if obj.type == 'MESH'])  # 원본 신호 표시의 하우징 기준 위치와 모양입니다.
        matrix = world_matrix(transforms, parent).inverted() @ turn  # 기존 그룹 좌표는 보존하고 외형의 연결 좌표만 변환합니다.
        add_visual(docs, parent, name, mesh, matrix)  # 기존 빨간불과 초록불 전환에서 원본 표시를 켜고 끕니다.
    save_documents(path, docs)  # 기존 시간 설정과 상황 정지 및 씬 배치를 그대로 저장합니다.
    return housing  # 관찰 강조에도 원본 기둥과 하우징 표면을 사용합니다.


def apply_scene_environment():  # 첫 상황의 기존 건물과 나무 위치에 원본 외형만 연결합니다.
    docs = clear_generated(read_documents(SCENE))  # 이전의 비균일 크기 보정 외형만 교체합니다.
    names, transforms, by_game = layout(docs)  # 기존 씬의 실제 배경 위치를 읽습니다.
    parent = by_game[next(identifier for identifier, name in names.items() if name == '첫 번째 상황_횡단보도까지 이어지는 인도 풍경')]  # 기존 건물과 가로수의 배경 루트입니다.
    hide_renderers(docs, [parent])  # 기존 임시 건물과 잎 도형만 숨깁니다.
    assembly = assembly_root('GameIntroductionSource')  # 기존 배경 위치로 조립할 원본 외형 루트입니다.
    buildings = iter(('PeachTownhouse', 'CornerShop', 'BlueTownhouse'))  # 기존 세 건물을 각각의 원본 모델로 교체합니다.
    for game_id, name in names.items():  # 기존 배경에서 실제 대응하는 대상만 선택합니다.
        if name.startswith('인도 뒤편_카툰 건물 외벽_'):  # 기존 도입 배경 건물의 외벽 위치입니다.
            identifier = by_game[game_id]  # 해당 건물의 기존 Transform입니다.
            position, _, scale = world_matrix(transforms, identifier).decompose()  # 기존 외벽이 놓인 바닥을 계산합니다.
            position.y -= abs(scale.y) * .5  # 같은 게임 바닥에 원본 건물을 놓습니다.
            copy_model_geometry(assembly, bpy.data.objects[next(buildings)], rigid_placement(transforms, identifier, position))  # 원본 건물의 폭과 높이를 변경하지 않습니다.
        elif name.startswith('가로수_줄기_'):  # 원래 게임의 가로수 위치입니다.
            identifier = by_game[game_id]  # 기존 줄기의 Transform을 배치 기준으로만 사용합니다.
            position, _, scale = world_matrix(transforms, identifier).decompose()  # 기존 원기둥의 실제 밑면을 읽습니다.
            position.y -= abs(scale.y)  # Unity 원기둥 기본 높이 2를 반영한 기존 지면입니다.
            copy_model_geometry(assembly, bpy.data.objects['RoundTree'], Matrix.Translation(position))  # 원본 나무 전체의 모양과 크기를 그대로 사용합니다.
    for game_id, name in names.items():  # 기존 도입 구간의 녹지 표면만 교체합니다.
        if name.startswith('인도 연결 녹지_') and bpy.data.objects.get('SoftTerrain') is None:  # 연속 지형이 없는 원본에서만 기존 녹지 타일을 사용합니다.
            identifier = by_game[game_id]  # 기존 지면 위치와 구간을 읽습니다.
            position, _, scale = world_matrix(transforms, identifier).decompose()  # 기존 지면의 중심과 크기입니다.
            position.y += abs(scale.y) * .5 - .001  # 원본 녹지 가장자리가 인도와 겹치는 곳에서 보도 표면과 깜박이지 않게 합니다.
            hide_renderers(docs, [identifier])  # 같은 위치의 임시 녹지 외형은 표시하지 않습니다.
            add_surface_tiles(assembly, bpy.data.objects['GrassGround'], position, abs(scale.x), abs(scale.z))  # 원본 지면을 늘이지 않고 기존 구간에 반복합니다.
    bpy.context.view_layer.update()  # 내보낼 배경 인스턴스의 실제 행렬을 반영합니다.
    mesh = export_mesh(assembly, 'IntroductionEnvironment')  # 기존 배치 위에 조립한 원본 건물과 가로수 및 녹지입니다.
    add_visual(docs, parent, 'BlenderIntroduction', mesh, world_matrix(transforms, parent).inverted())  # 기존 배경 루트와 상황 경로의 위치는 바꾸지 않습니다.
    save_documents(SCENE, docs)  # 원본 외형만 교체한 실제 게임 씬을 저장합니다.

def replace_outline(documents, observation_id, mesh, matrix):  # 기존 관찰 입력과 위치를 유지하며 외곽선 표면만 원본으로 교체합니다.
    by_id = {doc['id']: doc for doc in documents}  # 기존 관찰 컴포넌트와 렌더러의 식별자입니다.
    observation = by_id[observation_id]  # 게임이 이미 켜고 끄는 관찰 대상입니다.
    section = re.search(r'(?ms)^  m_Outlines:\n(.*?)(?=^  m_Color:)', observation['body']).group(1)  # 기존 강조 렌더러 목록입니다.
    renderer_ids = [int(value) for value in re.findall(r'fileID: (\d+)', section)]  # 같은 관찰 대상의 실제 렌더러만 선택합니다.
    for identifier in renderer_ids:  # 임시 부위 외곽선이 새 모델 위에 함께 나타나지 않게 합니다.
        by_id[identifier]['body'] = re.sub(r'(?m)^  m_Enabled: \d+', '  m_Enabled: 0', by_id[identifier]['body'])  # 사용하지 않는 이전 강조 부위를 표시하지 않습니다.
    renderer = by_id[renderer_ids[0]]  # 기존 대표 렌더러를 그대로 재사용합니다.
    game_id = reference(renderer['body'], 'm_GameObject')  # 같은 대표 렌더러가 속한 오브젝트입니다.
    transform = next(doc for doc in documents if doc['kind'] == 4 and 'm_GameObject:' in doc['body'] and reference(doc['body'], 'm_GameObject') == game_id)  # 원본 메시를 연결할 시각 자식입니다.
    mesh_filter = next(doc for doc in documents if doc['kind'] == 33 and reference(doc['body'], 'm_GameObject') == game_id)  # 기존 대표 렌더러의 메시 필터입니다.
    set_transform(documents, transform['id'], matrix)  # 관찰 루트 위치는 유지하고 임시 부위의 크기 보정만 제거합니다.
    mesh_filter['body'] = re.sub(r'(?m)^  m_Mesh: .*', '  m_Mesh: {fileID: 4300000, guid: ' + mesh['guid'] + ', type: 2}', mesh_filter['body'])  # 실제 새 모델과 동일한 표면을 사용합니다.
    material = re.search(r'(?m)^  m_Materials:\n(  - [^\n]*\n)', renderer['body']).group(1)  # 기존 관찰 전용 강조 셰이더를 유지합니다.
    renderer['body'] = re.sub(r'(?ms)^  m_Materials:\n(?:  - [^\n]*\n)*', '  m_Materials:\n' + material * len(mesh['palette']), renderer['body'])  # 원본 모델의 모든 재질 부위를 강조합니다.
    observation['body'] = re.sub(r'(?ms)^  m_Outlines:\n.*?(?=^  m_Color:)', f'  m_Outlines:\n  - {{fileID: {renderer_ids[0]}}}\n', observation['body'])  # 같은 기존 입력이 교체된 대표 표면만 켜고 끄게 합니다.


def apply_scene_source(phone, assets):  # 게임 배치와 진행 값은 보존하고 원본 모델에 필요한 관찰 연결만 갱신합니다.
    docs = read_documents(SCENE)  # 이미 교체한 배경 외형과 기존 게임 위치를 그대로 읽습니다.
    by_id = {doc['id']: doc for doc in docs}  # 게임의 기존 참조 식별자입니다.
    names, _, _ = layout(docs)  # 전화 선택 범위와 강조 오브젝트를 실제 이름으로 구분합니다.
    counts = {}  # 원본 캐릭터별 실제 재질과 표정 수입니다.
    for filename in ('FriendCharacter.prefab', 'ApricotFriendCharacter.prefab'):  # 기존 두 캐릭터 프리팹을 읽습니다.
        renderer = next(doc for doc in read_documents(PREFABS / filename) if doc['id'] == 13700000)  # 실제 원본 스킨 렌더러입니다.
        guid = re.search(r'm_Mesh: \{fileID: 4300000, guid: (\w+)', renderer['body']).group(1)  # 원본 메시의 기존 GUID입니다.
        counts[guid] = len(re.findall(r'^  - \{fileID: 2100000', renderer['body'], re.M))  # 외곽선에도 같은 재질 슬롯 수를 전달합니다.
    for doc in docs:  # 모델의 실제 관찰 표면에 관련된 필드만 갱신합니다.
        if 'stripped' in doc['suffix']:  # 프리팹 참조 문서의 형식은 그대로 유지합니다.
            continue  # 씬에 실제로 저장된 컴포넌트만 수정합니다.
        count = 0  # 현재 강조 렌더러가 사용할 원본 서브메시 수입니다.
        game = reference(doc['body'], 'm_GameObject') if 'm_GameObject:' in doc['body'] else 0  # 현재 컴포넌트의 실제 소유 오브젝트입니다.
        name = names.get(game, '')  # 같은 캐릭터의 전화 관찰 대상을 구분합니다.
        if doc['kind'] == 137:  # 기존 캐릭터 스킨 외곽선입니다.
            count = next((value for guid, value in counts.items() if guid in doc['body']), 0)  # 해당 캐릭터의 실제 원본 재질 수입니다.
        is_phone = ('테두리' in name or '외곽선' in name) and ('전화' in name or '휴대폰' in name)  # 기존 전화 강조 오브젝트만 선택합니다.
        if doc['kind'] == 33 and is_phone:  # 기존 전화 외곽선의 메시 필터입니다.
            doc['body'] = re.sub(r'(?m)^  m_Mesh: .*', '  m_Mesh: {fileID: 4300000, guid: ' + phone['guid'] + ', type: 2}', doc['body'])  # 실제 원본 전화 표면을 사용합니다.
        if doc['kind'] == 23 and is_phone:  # 전화의 기존 강조 렌더러입니다.
            count = len(phone['palette'])  # 원본 본체와 화면 및 카메라를 함께 강조합니다.
        if doc['kind'] == 65 and name.startswith('관찰_휴대전화 전용 선택 범위'):  # 원본 소품으로 교체되는 기존 전화 선택 범위입니다.
            doc['body'] = set_vector(doc['body'], 'm_Center', phone['centre'])  # 실제 원본 전화의 중심입니다.
            doc['body'] = set_vector(doc['body'], 'm_Size', phone['extent'] * 2 + Vector((.01, .01, .01)))  # 원본 소품 둘레의 작은 선택 여유입니다.
        if count:  # 기존 관찰 활성화와 입력 규칙을 유지합니다.
            material = re.search(r'(?m)^  m_Materials:\n(  - [^\n]*\n)', doc['body']).group(1)  # 기존 강조 재질을 그대로 사용합니다.
            doc['body'] = re.sub(r'(?ms)^  m_Materials:\n(?:  - [^\n]*\n)*', '  m_Materials:\n' + material * count, doc['body'])  # 모든 원본 서브메시를 같은 강조에 연결합니다.
    for identifier, name, turn in ((4000000445, 'SoftUtilityPole', 0), (9000001136, 'Signal', math.pi), (9000001240, 'SoftCar', 0), (9100001021, 'Crosswalk', 0)):  # 기존 사고와 교통 관찰 대상의 대표 표면입니다.
        replace_outline(docs, identifier, assets[name], Matrix.Rotation(turn, 4, 'Y'))  # 관찰 루트와 클릭 연결은 유지하며 원본 표면만 교체합니다.
    car_body = by_id[9000001238]  # 기존 차량의 실제 접근과 급정거 판정에서 참조하는 충돌체입니다.
    car_body['body'] = set_vector(car_body['body'], 'm_Center', assets['SoftCar']['centre'])  # 원본 차체의 실제 중심을 사용합니다.
    car_body['body'] = set_vector(car_body['body'], 'm_Size', assets['SoftCar']['extent'] * 2)  # 원본 외형에 맞는 접촉 범위를 같은 판정에 연결합니다.
    save_documents(SCENE, docs)  # 기존 인스턴스 위치와 경로 및 카메라와 상황 진행 설정은 유지합니다.

def apply_models():  # 현재 게임의 위치와 진행을 유지하며 Blender 원본 외형을 실제 에셋으로 교체합니다.
    OUTPUT.mkdir(parents=True, exist_ok=True)  # 실제 게임 메시를 저장할 기존 폴더를 사용합니다.
    metadata(OUTPUT, 'DefaultImporter', 0, True)  # 기존 출력 폴더의 Unity GUID를 유지합니다.
    apply_source_materials()  # 현재 원본 노드의 실제 색상과 명암 값을 먼저 연결합니다.
    apply_characters()  # 원본 스킨과 관절축 및 표정으로 캐릭터 외형을 교체합니다.
    apply_environment_models()  # 기존 게임 배치에서 원본 배경과 소품 외형만 교체합니다.
    print('원본 모델 교체 저장: 기존 게임 위치·경로·진행 유지, 원본 형태·재질·골격·표정 연결', flush=True)  # 에셋 작성 결과만 알리며 실행 결과를 주장하지 않습니다.


if __name__ == '__main__':  # 원본 모델 적용을 요청한 작업에서만 에셋을 작성합니다.
    apply_models()  # 컴파일이나 게임 실행 및 테스트 없이 실제 모델 데이터를 저장합니다.
