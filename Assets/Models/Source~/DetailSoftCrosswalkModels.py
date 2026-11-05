import bpy  # 현재 Blender 원본에 실제 환경과 모델 디테일을 제작합니다.
import importlib.util  # 기존 제작기와 게임 에셋 연결 함수를 재사용합니다.
import math  # 연결 지형과 곡선 및 식재의 형태를 계산합니다.
import random  # 일정한 배치를 유지하면서 자연스러운 형태 차이를 만듭니다.
import re  # 기존 씬의 시각 필드만 변경합니다.
import sys  # 배치만 수정하는 제작 범위를 구분합니다.
from pathlib import Path  # 결과를 현재 프로젝트의 원본과 실제 에셋에 저장합니다.
from mathutils import Matrix, Vector  # 원본의 부위 좌표와 스킨 연결을 계산합니다.
from mathutils.bvhtree import BVHTree  # 지붕과 옷의 실제 표면에 세부 형상을 밀착합니다.
from mathutils.kdtree import KDTree  # 새 봉제선에 기존 의상의 실제 스킨 웨이트를 전달합니다.

SOURCE = Path(__file__).resolve().parent  # 기존 Blender 제작 소스가 있는 폴더입니다.
AREA = SOURCE.parents[1]  # 현재 CrosswalkScene의 에셋 영역입니다.
RNG = random.Random(7319)  # 다시 저장해도 동일한 식재와 디테일 배치를 유지합니다.
PLACEMENT_GROUND = None  # 실제 게임의 지면 표면에서 소품의 배치 높이를 구합니다.
BUILDING_FOOTPRINTS = []  # 원본 건물이 차지하는 실제 영역을 식재 배치에서 제외합니다.


def module(name, filename):  # 기존 제작 함수를 실행 진입부와 분리하여 가져옵니다.
    spec = importlib.util.spec_from_file_location(name, SOURCE / filename)  # 현재 프로젝트의 실제 제작 소스입니다.
    result = importlib.util.module_from_spec(spec)  # 원본 제작기의 상태를 유지할 모듈입니다.
    spec.loader.exec_module(result)  # 모델 전체를 다시 만드는 진입부는 실행하지 않습니다.
    return result  # 기존 메시와 재질 및 직렬화 함수를 재사용합니다.


C = module('crosswalk_detail_library', 'CreateSoftCrosswalkModels.py')  # 기존 카툰 모델 제작 도구입니다.
A = module('crosswalk_asset_library', 'ApplySoftCrosswalkModels.py')  # 기존 게임 위치와 동작 연결을 보존하는 적용 도구입니다.


def root(name):  # 세부 제작 대상을 지정하며 기존 모델의 배치는 유지합니다.
    target = bpy.data.objects.get(name)  # 원본의 기존 모델을 우선 사용합니다.
    if target is None:  # 새로운 배경 에셋에 필요한 루트만 만듭니다.
        target = C.asset(name)  # 기존 제작기의 같은 모델 계층을 사용합니다.
        target.parent = bpy.data.objects['SoftStreetSet']  # 원본 거리와 함께 편집할 수 있게 합니다.
    C.ACTIVE_ROOT = target  # 다음 실제 메시를 같은 모델에 연결합니다.
    return target  # 기존 모델이나 새 배경의 실제 루트입니다.


def line(name, points, width, material):  # 봉제선과 금속 이음새 및 가지를 실제 연속 표면으로 제작합니다.
    return C.tube('Detail_' + name, points, [width] * len(points), material, segments=8, subdivision=0)  # 정점으로 이어진 선형 부품을 사용합니다.


def block(name, centre, size, material, radius=.018):  # 구조에 필요한 판재와 마감재를 제작합니다.
    return C.rounded_box('Detail_' + name, centre, size, material, radius)  # 주 형상에 맞는 모서리와 실제 두께를 남깁니다.


def bounds(target, objects):  # 실제 원본 형상에서 세부 배치에 필요한 경계를 읽습니다.
    inverse = target.matrix_world.inverted()  # 원본의 장면 배치를 제외합니다.
    points = [A.unity(inverse @ obj.matrix_world @ vertex.co) for obj in objects for vertex in obj.data.vertices]  # 모든 부위의 실제 모델 좌표입니다.
    return Vector(tuple(min(point[a] for point in points) for a in range(3))), Vector(tuple(max(point[a] for point in points) for a in range(3)))  # 임시 도형 크기를 사용하지 않습니다.


def surface(target, objects):  # 여러 부품으로 된 실제 지붕의 투영 표면을 구성합니다.
    inverse = target.matrix_world.inverted()  # 해당 원본 모델의 좌표를 사용합니다.
    vertices, faces = [], []  # 원본 표면의 실제 정점과 면입니다.
    for obj in objects:  # 지붕이나 의상에 속한 부위만 읽습니다.
        offset = len(vertices)  # 부위별 정점 번호를 이어 줍니다.
        vertices.extend(A.unity(inverse @ obj.matrix_world @ vertex.co) for vertex in obj.data.vertices)  # 원본의 실제 표면 위치입니다.
        faces.extend(tuple(offset + index for index in face.vertices) for face in obj.data.polygons)  # 표면의 연결 관계도 유지합니다.
    return BVHTree.FromPolygons(vertices, faces)  # 새 형상이 원본 위로 뜨거나 파묻히지 않는 배치 기준입니다.


def ease(value):  # 지형 구간 사이의 기울기를 연속적으로 연결합니다.
    value = max(0., min(1., value))  # 경계 바깥에서도 높이가 역전되지 않게 합니다.
    return value * value * (3. - 2. * value)  # 양끝의 기울기가 이어지는 보간입니다.


def terrain_height(x, z):  # 기존 보행 구간을 평탄하게 유지하며 바깥 지형을 연결합니다.
    distance = max(abs(z) - 17., abs(x) - 46., 0.)  # 기존 도로와 보도 및 도입 구간 밖의 거리입니다.
    rise = ease(distance / 18.)  # 가까운 바닥에서 원경 언덕으로 완만하게 이어 줍니다.
    rolling = 2.1 + 1.15 * math.sin(x * .071 + z * .026) + .8 * math.cos(z * .087 - x * .022)  # 작은 도형 대신 넓은 능선이 이어지는 높이입니다.
    hills = 5.5 * math.exp(-((x + 40.) / 22.) ** 2 - ((z - 48.) / 20.) ** 2) + 4.2 * math.exp(-((x - 38.) / 27.) ** 2 - ((z - 58.) / 18.) ** 2)  # 서로 다른 두 원경 능선입니다.
    height = -.1 + rise * (rolling + hills)  # 도로 밑의 기존 지면 높이에서 연속적으로 올라갑니다.
    introduction = ease((-z - 6.7) / .8) * (1. - ease((-z - 15.7) / 2.5)) * ease((x + 52.) / 2.) * (1. - ease((x - 30.) / 3.))  # 기존 첫 상황 녹지의 바닥 영역입니다.
    return height * (1. - introduction) + .149 * introduction  # 기존 녹지 높이를 유지하며 큰 지형에 이어 줍니다.


def prepare_placement_surface():  # 실제 적용되는 지형과 포장면을 소품의 배치 기준으로 읽습니다.
    global PLACEMENT_GROUND, BUILDING_FOOTPRINTS  # 이번 원본에 해당하는 실제 표면과 건물 영역입니다.
    vertices, faces = [], []  # 배치 계산에 사용할 실제 지면 정점과 면입니다.
    BUILDING_FOOTPRINTS = []  # 이전 제작의 건물 범위를 누적하지 않습니다.
    def add_surface(source, matrix):  # 원본 표면을 실제 게임에서 적용하는 위치로 옮겨 읽습니다.
        inverse = source.matrix_world.inverted()  # 원본 편집 장면의 배치는 제외합니다.
        for obj in C.descendants(source):  # 지면에 속한 실제 부품만 가져옵니다.
            if obj.type != 'MESH':  # 루트와 보조 오브젝트는 지면이 아닙니다.
                continue  # 실제 표면만 배치 높이에 반영합니다.
            offset = len(vertices)  # 현재 부품의 정점 시작입니다.
            vertices.extend(matrix @ A.unity(inverse @ obj.matrix_world @ vertex.co) for vertex in obj.data.vertices)  # 최종 노면과 동일한 좌표입니다.
            faces.extend(tuple(offset + index for index in face.vertices) for face in obj.data.polygons)  # 원본 지형의 실제 표면 연결입니다.
    for name in ('SoftTerrain', 'SoftRoadSurfaces'):  # 게임에 직접 연결하는 연속 지형과 노면입니다.
        add_surface(bpy.data.objects[name], Matrix.Identity(4))  # 높이 함수의 추정치 대신 현재 메시 표면을 사용합니다.
    documents = A.read_documents(A.PREFABS / 'CrosswalkEnvironment.prefab')  # 현재 게임의 실제 앞마당과 건물 배치입니다.
    _, transforms, _ = A.layout(documents)  # 기존 제어와 충돌체가 사용하는 위치입니다.
    source = bpy.data.objects['SchoolForecourt']  # 실제 게임에서 반복 배치하는 원본 앞마당입니다.
    low, high = bounds(source, [obj for obj in C.descendants(source) if obj.type == 'MESH'])  # 원본 모듈의 실제 폭과 길이입니다.
    position, _, scale = A.world_matrix(transforms, 100251).decompose()  # 기존 앞마당의 실제 중심과 범위입니다.
    position.y += abs(scale.y) * .5 - .06  # 게임 적용 코드와 동일한 앞마당 표면 높이입니다.
    count_x, count_z = math.ceil(abs(scale.x) / (high.x - low.x) - .00001), math.ceil(abs(scale.z) / (high.z - low.z) - .00001)  # 실제로 적용되는 원본 모듈의 수입니다.
    for x in range(count_x):  # 게임에 놓이는 같은 앞마당 구간입니다.
        for z in range(count_z):  # 포장된 앞마당 전체를 배치 높이에 반영합니다.
            offset = Vector(((x - (count_x - 1) * .5) * (high.x - low.x), 0, (z - (count_z - 1) * .5) * (high.z - low.z)))  # 적용 코드와 같은 타일 중심입니다.
            add_surface(source, Matrix.Translation(position + offset))  # 나무와 소품이 앞마당 아래로 묻히지 않게 합니다.
    PLACEMENT_GROUND = BVHTree.FromPolygons(vertices, faces)  # 실제 표면에서 소품의 접지 위치를 계산합니다.
    def footprint(source, matrix):  # 건물의 마감까지 포함한 실제 배치 영역을 읽습니다.
        low, high = bounds(source, [obj for obj in C.descendants(source) if obj.type == 'MESH'])  # 원본 외벽과 창턱 및 지붕의 실제 경계입니다.
        points = [matrix @ Vector((x, 0, z)) for x in (low.x, high.x) for z in (low.z, high.z)]  # 실제 건물 방향까지 반영한 네 모서리입니다.
        BUILDING_FOOTPRINTS.append((min(p.x for p in points), max(p.x for p in points), min(p.z for p in points), max(p.z for p in points)))  # 수관이 외벽에 겹치지 않을 배치 영역입니다.
    for name, identifier, turn in (('SoftSchool', 100261, math.pi), ('PeachTownhouse', 100671, math.pi), ('BlueTownhouse', 100761, math.pi), ('CornerShop', 100851, 0)):  # 실제 환경에 적용하는 네 건물입니다.
        position, _, scale = A.world_matrix(transforms, identifier).decompose()  # 기존 외벽의 실제 바닥 위치입니다.
        position.y -= abs(scale.y) * .5  # 기존 건물의 기준 높이는 유지합니다.
        footprint(bpy.data.objects[name], A.rigid_placement(transforms, identifier, position, turn))  # 실제 적용 방향과 동일하게 배치 영역을 구합니다.
    names, transforms, by_game = A.layout(A.read_documents(A.SCENE))  # 도입 구간에 직접 놓인 기존 건물들입니다.
    buildings = iter(('PeachTownhouse', 'CornerShop', 'BlueTownhouse'))  # 실제 적용 코드와 같은 건물 대응입니다.
    for game, name in names.items():  # 기존 도입 배경의 실제 외벽만 선택합니다.
        if name.startswith('인도 뒤편_카툰 건물 외벽_'):  # 기존 건물 배치는 수정하지 않습니다.
            footprint(bpy.data.objects[next(buildings)], A.rigid_placement(transforms, by_game[game]))  # 나무와 잎이 도입 구간의 건물에 파고들지 않게 합니다.
    neighborhood = bpy.data.objects.get('SoftNeighborhood')  # 추가한 건물도 이후의 식재 배치에서 실제 건물로 취급합니다.
    if neighborhood is not None:  # 건물이 추가된 원본에서만 같은 영역을 연결합니다.
        for building in neighborhood.children:  # 개별 추가 건물의 실제 루트입니다.
            if building.get('neighborhood_building'):  # 기초 표면과 구분하여 건물 자체의 영역을 읽습니다.
                footprint(building, A.AXIS @ building.matrix_world @ A.AXIS.inverted())  # 건물의 실제 위치와 방향을 식재 배치에 전달합니다.


def ground_height(x, z):  # 최종 지형과 포장면의 실제 높이를 배치에 사용합니다.
    if PLACEMENT_GROUND is None:  # 지형 제작 이전의 원본 작업도 지원합니다.
        return terrain_height(x, z)  # 아직 메시가 없을 때만 제작용 높이를 사용합니다.
    hit, _, _, _ = PLACEMENT_GROUND.ray_cast(Vector((x, 80., z)), Vector((0, -1, 0)), 240.)  # 현재 게임 표면으로 바로 내려놓을 위치입니다.
    return hit.y if hit is not None else terrain_height(x, z)  # 실제 메시 표면이 있으면 그 높이를 우선합니다.


def building_occupied(x, z, radius):  # 실제 건물과 겹치는 식재 위치를 제외합니다.
    return any(left - radius < x < right + radius and near - radius < z < far + radius for left, right, near, far in BUILDING_FOOTPRINTS)  # 수관 크기까지 반영한 배치 범위입니다.


def place_grounded_parts(before, x, z, turn):  # 벤치와 가로등의 실제 밑면을 같은 지면에 맞춥니다.
    objects = [obj for obj in set(bpy.data.objects) - before if obj.type == 'MESH']  # 방금 제작한 소품 부품만 이동합니다.
    bottom = min(A.unity(obj.matrix_local @ vertex.co).y for obj in objects for vertex in obj.data.vertices)  # 임의의 기준 높이 대신 실제 밑면을 읽습니다.
    placement = Matrix.Translation(Vector((x, ground_height(x, z) - bottom, z))) @ Matrix.Rotation(turn, 4, 'Y')  # 실제 접지 높이와 도로를 향한 방향입니다.
    for obj in objects:  # 같은 소품의 모든 부품을 함께 놓습니다.
        obj.matrix_local = A.AXIS.inverted() @ placement @ A.AXIS @ obj.matrix_local  # 원본 형태를 변형하지 않고 강체 배치만 적용합니다.


def make_terrain():  # 타일이 아닌 하나의 연결된 거리 지형을 제작합니다.
    target = root('SoftTerrain')  # 실제 게임 좌표의 넓은 지형 루트입니다.
    xs = list(range(-224, -64, 8)) + list(range(-64, 66, 2)) + list(range(72, 225, 8))  # 가까운 지면은 촘촘하게 만들고 원경은 넓은 면으로 연결합니다.
    zs = list(range(-208, -32, 8)) + list(range(-32, 34, 2)) + list(range(40, 241, 8))  # 보행 공간 밖으로도 지면이 충분히 이어지게 합니다.
    width, depth = len(xs) - 1, len(zs) - 1  # 이웃한 실제 지형 좌표 사이의 격자 수입니다.
    vertices = [(x, terrain_height(x, z), z) for z in zs for x in xs]  # 모든 지면 구간이 같은 정점과 모서리를 공유합니다.
    faces = []  # 이웃한 격자를 빈틈없이 연결합니다.
    for z in range(depth):  # 지형의 앞뒤를 이어 줍니다.
        for x in range(width):  # 지형의 좌우를 이어 줍니다.
            a = z * (width + 1) + x  # 현재 격자의 시작 정점입니다.
            faces.extend(((a, a + width + 1, a + width + 2), (a, a + width + 2, a + 1)))  # 모든 지면 칸이 실제 모서리를 공유합니다.
    obj = C.mesh('Detail_ContinuousTerrain', vertices, faces, 'Grass', smooth=True)  # 모서리가 드러난 녹지 상자들을 대체할 실제 지형입니다.
    for name in ('Meadow', 'Sage', 'DistantGreen'):  # 능선과 지면을 같은 계열의 색면으로 나눕니다.
        obj.data.materials.append(C.MATERIALS[name])  # 현실적인 얼룩 대신 설계된 카툰 재질입니다.
    for face in obj.data.polygons:  # 넓고 자연스러운 색상 구간을 선택합니다.
        centre = sum((obj.data.vertices[index].co for index in face.vertices), Vector()) / len(face.vertices)  # 원본 지형 면의 실제 중심입니다.
        x, y, z = centre.x, centre.z, -centre.y  # 카툰 거리의 설계 좌표입니다.
        face.material_index = 3 if z > 43 or abs(x) > 65 else (1 if math.sin(x * .055 + z * .08) > .48 else (2 if y > 2.5 else 0))  # 끊어진 타일 경계 없이 넓은 색면으로 연결합니다.
    print('연속 지형 원본 제작 저장 준비', flush=True)  # 실제 모델링 진행만 알립니다.


def make_road_surfaces():  # 차도와 보도를 원래 게임 위치에서 끊기지 않는 표면으로 제작합니다.
    root('SoftRoadSurfaces')  # 기존 차로와 보행 높이에 맞춘 실제 원본 노면입니다.
    def ribbon(name, z, width, top, bottom, material):  # 가공된 가장자리와 연속된 긴 표면을 구성합니다.
        bevel = .012  # 보도 경계의 작은 가공 모서리입니다.
        section = [(-width * .5, bottom), (-width * .5, top - bevel), (-width * .5 + bevel, top), (width * .5 - bevel, top), (width * .5, top - bevel), (width * .5, bottom)]  # 같은 단면이 도로 끝까지 이어집니다.
        vertices = [(x, y, z + offset) for x in (-220., 220.) for offset, y in section]  # 원경 안개까지 이어지는 실제 노면 정점입니다.
        count = len(section)  # 앞뒤 단면의 실제 정점 수입니다.
        faces = [tuple(reversed(range(count))), tuple(count + i for i in range(count))]  # 노면의 끝도 닫힌 표면입니다.
        faces.extend((i, (i + 1) % count, (i + 1) % count + count, i + count) for i in range(count))  # 각 구간이 같은 연결 면을 공유합니다.
        return C.mesh('Detail_' + name, vertices, faces, material, smooth=False)  # 늘인 임시 도형 대신 설계된 노면 단면입니다.
    ribbon('ContinuousRoad', 0, 7., 0, -.18, 'Road')  # 기존 게임과 같은 차도 폭과 표면 높이입니다.
    for start, end in ((-218., -41.), (41., 218.)):  # 기존 횡단보도 주변 표시를 유지하며 바깥 차선을 이어 줍니다.
        for z in (-.12, .12):  # 기존 원본과 같은 이중 중앙선 간격입니다.
            C.mesh('Detail_DistantCentreLine_' + str(start) + str(z), [(start, .0075, z - .04), (end, .0075, z - .04), (end, .0075, z + .04), (start, .0075, z + .04)], [(0, 3, 2, 1)], 'Yellow', smooth=False)  # 원경 안개까지 실제 노면 표시가 이어집니다.
    for sign in (-1, 1):  # 기존 게임의 양쪽 보행 공간입니다.
        ribbon('ContinuousSidewalk_' + str(sign), sign * 5.2, 3., .15, -.10, 'Pavement')  # 보도도 길이 방향의 실제 이음 없이 이어집니다.
        ribbon('ContinuousCurb_' + str(sign), sign * 3.6, .20, .15, -.03, 'Cream')  # 보도와 차도를 잇는 같은 연속 연석입니다.
        for x in range(-70, 71):  # 눈앞의 보도에 실제 포장 줄눈을 넣습니다.
            z0, z1 = sign * 5.2 - 1.47, sign * 5.2 + 1.47  # 보도 안쪽의 실제 포장 영역입니다.
            C.mesh('Detail_PavingJoint_' + str(sign) + str(x), [(x - .009, .1505, z0), (x + .009, .1505, z0), (x + .009, .1505, z1), (x - .009, .1505, z1)], [(0, 3, 2, 1)], 'Sandstone', smooth=False)  # 지면 틈 대신 표면에 밀착한 얇은 줄눈입니다.
        for offset in (-.5, .5):  # 큰 보도 폭 안의 포장 규격입니다.
            z = sign * 5.2 + offset  # 기존 보도의 실제 높이와 위치입니다.
            C.mesh('Detail_LongPavingJoint_' + str(sign) + str(offset), [(-72, .1507, z - .006), (72, .1507, z - .006), (72, .1507, z + .006), (-72, .1507, z + .006)], [(0, 3, 2, 1)], 'Sandstone', smooth=False)  # 물리 보행 표면을 바꾸지 않는 포장 마감입니다.
        for x in (-32, -18, 16, 30):  # 횡단보도와 정지선에서 떨어진 배수 위치입니다.
            block('DrainFrame_' + str(sign) + str(x), (x, .004, sign * 3.29), (.58, .018, .30), 'RoadDetail', .006)  # 도로 가장자리와 이어지는 실제 배수 프레임입니다.
            for index in range(7):  # 배수구의 제작 구조를 보여 주는 슬롯입니다.
                block('DrainSlot_' + str(sign) + str(x) + str(index), (x + (index - 3) * .068, .014, sign * 3.29), (.027, .006, .21), 'Ink', .001)  # 바닥 위에서 과하게 돌출되지 않는 얇은 홈 표현입니다.
    print('연속 차도와 보도 및 포장 마감 원본 제작 저장 준비', flush=True)  # 게임의 길 위치는 변경하지 않습니다.


def crown(name, centre, size, material, seed):  # 비대칭 잎 윤곽을 가진 하나의 입체 수관을 제작합니다.
    cx, cy, cz = centre  # 수관의 실제 중심입니다.
    rx, ry, rz = size  # 수관의 가로와 높이 및 깊이입니다.
    rings, sides = 13, 22  # 가지가 읽히는 형태를 유지할 실제 표면 해상도입니다.
    vertices, faces = [], []  # 한 표면으로 연결할 수관 정점과 면입니다.
    for row in range(rings + 1):  # 위아래 높이에 따라 실루엣이 달라집니다.
        phi = math.pi * row / rings  # 꼭대기와 밑면을 잇는 각도입니다.
        for index in range(sides):  # 서로 붙인 구체 대신 수관 전체 둘레를 설계합니다.
            angle = math.tau * index / sides  # 수관의 실제 둘레 방향입니다.
            lobes = 1. + .105 * math.sin(angle * 3 + seed) * math.sin(phi) + .055 * math.sin(angle * 5 - phi * 3 + seed)  # 잎 무리의 부드럽고 비대칭인 외곽입니다.
            radius = max(.0001, math.sin(phi)) * lobes  # 위아래가 하나의 닫힌 표면으로 모이게 합니다.
            vertices.append((cx + rx * radius * math.cos(angle) + .10 * rx * math.sin(phi * 2), cy + ry * math.cos(phi), cz + rz * radius * math.sin(angle)))  # 실제 나무의 흐르는 실루엣을 만듭니다.
    for row in range(rings):  # 인접한 수관 단면을 이어 줍니다.
        for index in range(sides):  # 둘레의 이음새도 같은 표면입니다.
            a, following = row * sides + index, row * sides + (index + 1) % sides  # 현재 단면의 이웃한 정점입니다.
            faces.append((a, a + sides, following + sides, following))  # 수관의 실제 연결 면입니다.
    obj = C.mesh('Detail_' + name, vertices, faces, material, smooth=True)  # 독립된 구 덩어리의 겹침을 남기지 않습니다.
    obj.data.materials.append(C.MATERIALS['LeafLight'])  # 햇빛 쪽의 잎 무리를 같은 팔레트로 구분합니다.
    for face in obj.data.polygons:  # 잎 무리의 일부를 넓은 밝은 색면으로 만듭니다.
        face.material_index = 1 if face.normal.x > .42 and face.normal.z > .15 else 0  # 전체 수관을 같은 플라스틱 표면으로 보이지 않게 합니다.
    return obj  # 나무와 관목에 사용할 실제 수관입니다.


def tree(name, x, y, z, height=3.4):  # 뿌리와 굽은 줄기 및 수관을 갖춘 나무를 제작합니다.
    factor = height / 3.4  # 서로 다른 수종 크기에 같은 설계 비율을 사용합니다.
    points = [(x, y, z), (x + .04 * factor, y + .5 * factor, z), (x - .04 * factor, y + 1.3 * factor, z + .03), (x + .08 * factor, y + 2.25 * factor, z)]  # 곧은 원기둥이 아닌 줄기 중심선입니다.
    C.tube('Detail_' + name + 'Trunk', points, [r * factor for r in (.19, .145, .105, .055)], 'Bark', segments=12, subdivision=0)  # 뿌리에서 가지로 점점 가늘어지는 표면입니다.
    for sign in (-1, 1):  # 양쪽 가지의 시작 높이를 다르게 만듭니다.
        C.tube('Detail_' + name + 'Branch' + str(sign), [(x, y + 1.15 * factor, z), (x + sign * .35 * factor, y + 1.8 * factor, z + .03), (x + sign * .55 * factor, y + 2.1 * factor, z + .07)], [.10 * factor, .065 * factor, .018], 'Bark', segments=8, subdivision=0)  # 수관 안으로 자연스럽게 이어지는 가지입니다.
    crown(name + 'Canopy', (x, y + 2.4 * factor, z), (1.04 * factor, 1.15 * factor, .82 * factor), 'Leaf', x * .3 + z)  # 하나의 비대칭 표면으로 연결된 수관입니다.
    for index in range(3):  # 줄기 밑에 작은 뿌리 흐름을 만듭니다.
        angle = index * math.tau / 3  # 같은 방향에 뿌리가 겹치지 않게 합니다.
        C.tube('Detail_' + name + 'Root' + str(index), [(x, y + .25, z), (x + .22 * math.cos(angle), y + .04, z + .22 * math.sin(angle)), (x + .35 * math.cos(angle), y + .015, z + .35 * math.sin(angle))], [.095, .06, .012], 'Bark', segments=6, subdivision=0)  # 땅에 붙어 있는 작은 뿌리 형상입니다.


def grass(name, x, y, z, scale=1.):  # 가늘고 휘어진 잎 표면이 모인 풀을 제작합니다.
    vertices, faces = [], []  # 여러 잎을 하나의 메시로 저장합니다.
    for index in range(6):  # 각 풀잎의 기울기와 길이를 다르게 설계합니다.
        angle = index * 2.4 + x  # 같은 방향으로 겹치는 평면 잎을 피합니다.
        height = scale * (.14 + .07 * (index % 3))  # 짧고 긴 잎이 섞인 자연스러운 높이입니다.
        start = len(vertices)  # 현재 잎의 정점 시작입니다.
        side = Vector((math.cos(angle) * .027 * scale, 0, math.sin(angle) * .027 * scale))  # 잎의 실제 폭입니다.
        centre = Vector((x, y, z))  # 땅에 붙은 풀의 밑면입니다.
        bend = Vector((math.sin(angle) * height * .45, height, math.cos(angle) * height * .45))  # 끝이 자연스럽게 휘는 잎입니다.
        vertices.extend((tuple(centre - side), tuple(centre + side), tuple(centre + bend * .60 + side * .45), tuple(centre + bend), tuple(centre + bend * .60 - side * .45)))  # 하나의 잎 윤곽을 실제 표면으로 구성합니다.
        faces.extend(((start, start + 1, start + 2, start + 4), (start + 4, start + 2, start + 3), (start + 4, start + 2, start + 1, start)))  # 양쪽에서 보이는 잎의 연결 면입니다.
    return C.mesh('Detail_' + name, vertices, faces, 'Meadow', smooth=False)  # 바닥 위에 놓인 기본 도형을 사용하지 않습니다.


def flower(name, x, y, z, color, turn=0):  # 꽃의 중심과 바라보는 방향을 같은 식재 배치에 연결합니다.
    before = set(bpy.data.objects)  # 현재 꽃에 속한 실제 부품만 같은 방향으로 돌립니다.
    line(name + 'Stem', [(x, y, z), (x + .02, y + .18, z), (x, y + .30, z)], .008, 'Leaf')  # 땅에서 이어지는 가느다란 줄기입니다.
    outline = []  # 꽃잎이 하나로 이어진 실제 윤곽입니다.
    for index in range(40):  # 다섯 꽃잎의 곡선을 같은 외곽선으로 연결합니다.
        angle = math.tau * index / 40  # 꽃의 둘레 각도입니다.
        radius = .067 + .025 * math.cos(angle * 5)  # 원이나 구를 붙이지 않은 꽃잎 형태입니다.
        outline.append((x + radius * math.cos(angle), y + .30 + radius * math.sin(angle)))  # 꽃잎의 실제 가로와 높이입니다.
    petals = C.extrude_outline('Detail_' + name + 'Petals', outline, .018, color, .003)  # 깊이 0에서 만든 꽃잎을 식재 위치로 옮길 수 있게 보관합니다.
    C.oval('Detail_' + name + 'Heart', (x, y + .30, z + .014), (.023, .023, .012), 'Yellow', segments=12, rings=6)  # 꽃잎 가운데의 작은 꽃술입니다.
    rotation = Matrix.Translation(Vector((x, 0, z))) @ Matrix.Rotation(turn, 4, 'Y') @ Matrix.Translation(Vector((-x, 0, -z)))  # 꽃의 실제 중심에서 도로 쪽을 향하게 합니다.
    for obj in set(bpy.data.objects) - before:  # 꽃잎과 꽃술 및 줄기에 같은 방향을 적용합니다.
        depth = Matrix.Translation(Vector(C.bp((0, 0, z)))) if obj == petals else Matrix.Identity(4)  # 꽃잎만 깊이를 별도로 부여하고 줄기와 꽃술은 정점에 저장된 깊이를 사용합니다.
        obj.matrix_basis = A.AXIS.inverted() @ rotation @ A.AXIS @ depth  # 아직 갱신되지 않은 오브젝트 행렬에 의존하지 않고 식재 위치를 기준으로 회전합니다.


def bench(name, x, z, turn):  # 목재 좌판과 휘어진 등받침 및 프레임을 갖는 벤치입니다.
    before = set(bpy.data.objects)  # 벤치에 해당하는 실제 부품만 묶기 위한 기준입니다.
    for index in range(4):  # 좌판을 실제 목재 판재 간격으로 나눕니다.
        block(name + 'Seat' + str(index), (0, .47, (index - 1.5) * .115), (1.6, .065, .10), 'Wood', .012)  # 판재의 얇은 가공 모서리를 남깁니다.
    for height in (.76, .94):  # 두 장의 등받침으로 벤치의 구조를 보여 줍니다.
        block(name + 'Back' + str(height), (0, height, -.28 - (height - .76) * .18), (1.6, .13, .055), 'Wood', .012)  # 등받침은 뒤로 조금 기울어진 배치입니다.
    for sign in (-1, 1):  # 양쪽에서 좌판과 등받침을 연결하는 금속 프레임입니다.
        line(name + 'Frame' + str(sign), [(sign * .62, .16, .21), (sign * .62, .42, .15), (sign * .62, .50, -.22), (sign * .62, 1.02, -.34)], .035, 'TealMetal')  # 단순한 네 다리 대신 이어지는 제작 구조입니다.
        for offset in (-.15, .15):  # 바닥에 닿는 실제 지지 부분입니다.
            line(name + 'Leg' + str(sign) + str(offset), [(sign * .62, .45, offset), (sign * .70, .025, offset + .04)], .035, 'TealMetal')  # 벤치 무게를 지지하는 비스듬한 다리입니다.
    place_grounded_parts(before, x, z, turn)  # 실제 다리 밑면이 지형이나 앞마당에 닿도록 놓습니다.


def lamp(name, x, z, turn):  # 받침과 조명 암의 배치를 하나의 방향과 접지 기준으로 연결합니다.
    before = set(bpy.data.objects)  # 현재 가로등 부품을 원점에서 함께 제작합니다.
    profile = [(0, .17, .17, 0, 0), (.12, .18, .18, 0, 0), (.24, .105, .105, 0, 0), (2.55, .055, .055, 0, 0), (3.1, .045, .045, 0, 0)]  # 실제 밑면을 읽을 수 있는 가로등 로컬 단면입니다.
    C.loft('Detail_' + name + 'Post', profile, 'TealMetal', segments=16, subdivision=0)  # 일체형으로 이어지는 기둥 표면입니다.
    line(name + 'Arm', [(0, 2.92, 0), (0, 3.20, 0), (0, 3.32, .18), (0, 3.30, .48)], .045, 'TealMetal')  # 한 방향으로 설계한 암을 양쪽 도로 방향에 맞춰 회전합니다.
    shade = [(2.99, .25, .25, 0, .48), (3.08, .28, .28, 0, .48), (3.19, .10, .10, 0, .48), (3.22, .055, .055, 0, .48)]  # 암 끝과 같은 로컬 좌표의 조명 갓입니다.
    C.loft('Detail_' + name + 'Shade', shade, 'TealMetal', segments=20, subdivision=0)  # 구체를 붙이지 않은 조명 갓입니다.
    C.oval('Detail_' + name + 'Lens', (0, 3.0, .48), (.20, .022, .20), 'Cream', segments=20, rings=6)  # 조명 암과 함께 움직이는 렌즈입니다.
    place_grounded_parts(before, x, z, turn)  # 받침을 실제 지면에 놓고 조명 암을 도로 쪽으로 돌립니다.


def planter(name, x, z, radius=.84):  # 나무가 포장면을 뚫고 나오지 않도록 실제 식재 공간을 만듭니다.
    y = min(ground_height(x + math.cos(angle) * radius, z + math.sin(angle) * radius) for angle in (0, math.pi * .5, math.pi, math.pi * 1.5)) - .008  # 경사진 지면에서도 받침이 떠 보이지 않는 밑면입니다.
    profile = [(y, radius * .90, radius * .90, x, z), (y + .08, radius, radius, x, z), (y + .28, radius, radius, x, z), (y + .28, radius * .84, radius * .84, x, z), (y + .14, radius * .80, radius * .80, x, z)]  # 안쪽이 열린 연결된 화단 벽입니다.
    C.loft('Detail_' + name + 'Rim', profile, 'Sandstone', segments=24, subdivision=0)  # 석재 테두리와 받침이 한 표면으로 이어집니다.
    C.loft('Detail_' + name + 'Soil', [(y + .15, radius * .79, radius * .79, x, z), (y + .245, radius * .83, radius * .83, x, z)], 'Wood', segments=24, subdivision=0)  # 테두리 안에 실제 흙 표면을 만듭니다.
    return y + .24  # 나무와 식재가 같은 흙에 붙는 높이입니다.


def make_dressing():  # 기존 보행 동선을 비워 두고 주변의 생활 밀도를 높입니다.
    root('SoftStreetDressing')  # 새 소품은 기존 경로나 동작에 연결하지 않는 실제 배경입니다.
    bpy.context.view_layer.update()  # 원본 지면의 현재 행렬을 배치 계산에 반영합니다.
    prepare_placement_surface()  # 최종 적용되는 지형과 포장면 및 건물 위치를 읽습니다.
    RNG.seed(7319)  # 배치만 다시 작성해도 식재의 형태가 임의로 바뀌지 않게 합니다.
    for sign in (-1, 1):  # 도로 양쪽의 같은 보행 경계를 기준으로 배치합니다.
        turn = math.pi if sign > 0 else 0  # 양쪽 보도의 벤치 정면과 조명 암이 모두 도로를 향하는 회전입니다.
        for index, x in enumerate((-42., -34., -26., -18., -10., -2., 10., 18., 26., 34., 42.)):  # 기존 나무 사이와 이전의 넓은 빈 구간을 엇갈려 채웁니다.
            z = 8.6 if sign > 0 else -7.9  # 기존 가로수와 건물 외벽을 피한 식재 줄입니다.
            if (sign > 0 and abs(x) < 4) or building_occupied(x, z, 1.10):  # 학교 입구의 중앙 시야와 실제 건물 영역입니다.
                continue  # 큰 수관으로 입구나 외벽을 가리지 않습니다.
            y = planter(f'TreeBed_{index}_{sign}', x, z)  # 포장면과 흙의 연결이 보이는 실제 나무 식재 공간입니다.
            tree(f'StreetTree_{index}_{sign}', x, y, z, 3.15 + .18 * (index % 3))  # 기존 나무와 겹치지 않는 수관 높이입니다.
            for flower_index in range(3):  # 화단 안에만 작은 꽃을 모읍니다.
                fx, fz = x + (flower_index - 1) * .28, z - sign * .42  # 나무 앞의 같은 흙 표면입니다.
                flower(f'Flower_{index}_{sign}_{flower_index}', fx, y, fz, 'Pink' if index % 2 else 'Cream', turn)  # 꽃술도 도로를 향하도록 함께 놓습니다.
        for index, x in enumerate((-36., -20., -4., 12., 28., 40.)):  # 기존 줄기와 새 나무 사이의 독립된 벤치 자리입니다.
            z = sign * 8.2  # 지형의 높이가 안정된 보도 바깥 띠입니다.
            if not building_occupied(x, z, .90):  # 기존 건물과 겹치는 자리는 사용하지 않습니다.
                bench(f'Bench_{index}_{sign}', x, z, turn)  # 양쪽 모두 좌석 정면이 도로를 바라봅니다.
        for index, x in enumerate((-40., -24., -8., 16., 32., 44.)):  # 벤치 및 수관과 떨어진 별도의 조명 간격입니다.
            z = sign * 8.25  # 경계 경사면 대신 실제 지면에 놓는 가로등 받침입니다.
            if not building_occupied(x, z, .40):  # 벽에 파고드는 가로등 배치를 제외합니다.
                lamp(f'Lamp_{index}_{sign}', x, z, turn)  # 조명 갓이 양쪽 모두 도로 쪽으로 뻗습니다.
    for index, (x, z) in enumerate(((-6., -10.3), (3., -10.3), (14., -10.3), (30., -11.2), (-4., 10.5), (4., 10.5), (-12., 11.5), (14., 11.5), (32., 10.5))):  # 키 큰 수관 사이를 낮은 식재로 연결합니다.
        if building_occupied(x, z, .90):  # 기존 출입구와 실제 외벽 내부에는 식재하지 않습니다.
            continue  # 부피를 늘리기 위해 건물을 침범하지 않습니다.
        y = planter('ShrubBed_' + str(index), x, z, .72)  # 현재 지면과 맞닿는 작은 화단입니다.
        crown('LowShrub_' + str(index), (x, y + .23, z), (.61, .36, .52), 'Leaf', index * .9)  # 입구와 횡단 시야 아래의 낮은 식재입니다.
    for index in range(220):  # 나무 사이의 비어 있는 녹지에 작은 풀 군락을 연결합니다.
        x = RNG.uniform(-43., 43.)  # 기존 배경 구간 안의 위치입니다.
        z = RNG.choice((-1, 1)) * RNG.uniform(7.5, 15.0)  # 실제 보행 구간 바깥의 녹지입니다.
        if building_occupied(x, z, .22) or (z > 0 and abs(x) < 15.1 and z < 15.2):  # 건물과 앞마당의 포장 영역입니다.
            continue  # 잔디가 벽이나 포장면에서 자라지 않게 합니다.
        grass('VergeGrass_' + str(index), x, ground_height(x, z) + .004, z, RNG.uniform(.65, 1.3))  # 실제 지형의 높이에 맞춰 붙입니다.
    for sign in (-1, 1):  # 한쪽에만 남았던 넓은 원경 빈 공간을 함께 채웁니다.
        for index, x in enumerate((-52., -42., -32., -22., -12., -2., 10., 22., 34., 46., 56.)):  # 앞 가로수와 엇갈린 원경 간격입니다.
            z = sign * (29. + 4. * math.sin(index * 1.8))  # 한 줄의 벽처럼 보이지 않는 깊이 차이입니다.
            if not building_occupied(x, z, 1.7):  # 실제 건물 뒤의 여유 공간만 사용합니다.
                tree(f'BackgroundTree_{index}_{sign}', x, ground_height(x, z), z, 3.9 + .35 * (index % 3))  # 원경도 추정 높이 대신 실제 지형에 붙입니다.
    print('식재와 거리 소품 원본 제작 저장 준비', flush=True)  # 실제 제작 진행만 알립니다.


def make_building_details():  # 기존 건물의 실제 표면에 건축 마감과 생활 디테일을 제작합니다.
    specifications = (('SoftSchool', 15.2, 5.4, 2.7), ('PeachTownhouse', 4.3, 5.3, 1.85), ('BlueTownhouse', 4.0, 6.6, 1.85), ('CornerShop', 4.7, 4.6, 1.85))  # 원본 건물의 설계 치수입니다.
    for name, width, height, front in specifications:  # 기존 학교와 세 거리 건물을 각각 마감합니다.
        target = root(name)  # 원본 위치와 벽 및 출입구 형상을 유지합니다.
        original = [obj for obj in C.descendants(target) if obj.type == 'MESH' and not obj.get('visual_detail')]  # 현재 원본의 실제 건축 표면입니다.
        roofs = [obj for obj in original if 'roof' in obj.get('export_name', '').lower()]  # 바뀐 지붕의 실제 곡면을 찾습니다.
        if roofs:  # 원본 지붕이 있는 건물에만 마감을 연결합니다.
            roof_surface = surface(target, roofs)  # 장식이 뜨지 않도록 실제 지붕을 기준으로 삼습니다.
            low, high = bounds(target, roofs)  # 지붕의 실제 처마와 능선 경계입니다.
            count = max(4, round(width / .85))  # 지붕 크기에 맞는 단정한 판 이음새 수입니다.
            for index in range(count + 1):  # 지붕을 따라 같은 제작 간격의 이음새를 놓습니다.
                x = (index / count - .5) * width * .84  # 경사진 양 끝의 두꺼운 처마는 비워 둡니다.
                points = []  # 실제 지붕 곡면을 따를 선입니다.
                for step in range(17):  # 앞 처마부터 뒤 처마까지 표면을 읽습니다.
                    z = low.z * .94 + (high.z * .94 - low.z * .94) * step / 16  # 지붕의 실제 앞뒤 범위입니다.
                    hit, _, _, _ = roof_surface.ray_cast(Vector((x, high.y + 2., z)), Vector((0, -1, 0)), high.y + 5.)  # 원본 지붕의 높이를 직접 읽습니다.
                    if hit is not None:  # 실제 지붕 표면이 있는 구간만 제작합니다.
                        points.append((hit.x, hit.y + .012, hit.z))  # 표면에 밀착한 얇은 판 이음새입니다.
                if len(points) > 1:  # 원본 표면을 따라 이어지는 선만 저장합니다.
                    line('RoofSeam_' + str(index), points, .010, 'RoofTrim')  # 임의의 삼각 도형 대신 실제 지붕 구조를 강조합니다.
            for sign in (-1, 1):  # 양쪽 처마의 물받이와 배수관입니다.
                x = sign * (width * .5 - .1)  # 기존 외벽 옆의 실제 배관 위치입니다.
                line('Downpipe_' + str(sign), [(x, .18, front + .08), (x, height - .18, front + .08), (x + sign * .15, height + .01, front + .04)], .034, 'RoofTrim')  # 처마에서 바닥까지 이어지는 제조 구조입니다.
                for y in (.55, height * .55):  # 벽에 붙는 실제 고정 밴드입니다.
                    block('PipeClip_' + str(sign) + str(y), (x, y, front + .08), (.095, .055, .095), 'TealMetal', .006)  # 배관이 공중에 떠 보이지 않게 합니다.
        for y in (.20, .37):  # 기초와 벽이 맞닿는 두 겹의 석재 마감입니다.
            block('StonePlinth_' + str(y), (0, y, front + .025), (width - .12, .075, .07), 'Sandstone', .010)  # 벽과 지면의 경계를 건축 요소로 연결합니다.
        for sign in (-1, 1):  # 모서리에는 엇갈린 석재 마감을 놓습니다.
            for row in range(5):  # 같은 높이로 반복되는 작은 조적 단입니다.
                block('CornerStone_' + str(sign) + str(row), (sign * (width * .5 - .13), .56 + row * .28, front + .035), (.31 if row % 2 else .23, .19, .085), 'Sandstone', .010)  # 넓은 벽의 평면에 시공 구조를 더합니다.
        if name == 'SoftSchool':  # 학교에는 두 층의 창가와 출입구 마감을 제작합니다.
            for y in (1.50, 4.15):  # 원본 창문 높이를 유지합니다.
                for x in (-6., -4., -2., 2., 4., 6.):  # 원본 출입구와 시계 위치는 비워 둡니다.
                    block('WindowSill_' + str(x) + str(y), (x, y - .65, 2.91), (1.20, .10, .37), 'Cream', .018)  # 창틀과 외벽을 잇는 실제 창턱입니다.
                    line('WindowMullion_' + str(x) + str(y), [(x, y - .49, 2.94), (x, y + .49, 2.94)], .018, 'Cream')  # 창의 크기에 맞는 얇은 세로 분할입니다.
            for sign in (-1, 1):  # 입구 양옆에 벽등과 작은 식재를 둡니다.
                x = sign * 1.52  # 기존 출입구를 막지 않는 위치입니다.
                block('EntryLampBack_' + str(sign), (x, 1.95, 2.78), (.19, .34, .10), 'RoofTrim', .015)  # 벽에 붙은 실제 조명 받침입니다.
                block('EntryLampGlass_' + str(sign), (x, 1.97, 2.86), (.12, .23, .07), 'Cream', .015)  # 작은 밝은 면으로 입구를 정리합니다.
            for index in range(3):  # 넓은 출입구 앞의 얇은 문턱 마감입니다.
                block('EntryThreshold_' + str(index), (0, .04 + index * .035, 3.12 - index * .11), (2.55 - index * .12, .07, .35), 'Sandstone', .018)  # 마당에서 출입구로 이어지는 실제 두께입니다.
        else:  # 거리 건물에는 창가와 작은 표찰을 제작합니다.
            for sign in (-1, 1):  # 원본 상층 창문 위치를 사용합니다.
                x = sign * width * .25  # 기존 창문과 같은 가로 배치입니다.
                for row in range(max(1, int((height - 2.1) / 1.7))):  # 기존 층수에 맞는 창가입니다.
                    y = 3.2 + row * 1.65  # 원본 창문의 실제 높이입니다.
                    block('Sill_' + str(sign) + str(row), (x, y - .48, 2.04), (.93, .085, .28), 'Cream', .012)  # 창틀에서 이어지는 창턱입니다.
                    if row == 0:  # 건물마다 한 층에만 작은 꽃받침을 둡니다.
                        block('WindowBox_' + str(sign), (x, y - .62, 2.11), (.70, .23, .28), 'Terracotta', .022)  # 벽에 걸린 실제 화분 상자입니다.
                        for flower_index in range(3):  # 창가에 어울리는 작은 식재입니다.
                            flower('WindowFlower_' + str(sign) + str(flower_index), x + (flower_index - 1) * .19, y - .51, 2.13, 'Pink' if sign > 0 else 'Cream')  # 건물의 생활감을 더합니다.
        if name == 'CornerShop':  # 상점만의 간판과 어닝 끝단을 제작합니다.
            block('ShopPlaque', (0, 3.18, 1.93), (1.90, .48, .13), 'Cream', .028)  # 창과 차양 사이의 실제 간판입니다.
            C.label('Detail_ShopLettering', '동네 가게', (0, 3.05, 2.015), .23, 'Roof')  # 같은 둥근 한글 서체로 상점의 정체성을 만듭니다.
            for index in range(9):  # 천막의 끝단을 하나씩 이어 제작합니다.
                x = (index - 4) * .50  # 기존 어닝의 폭을 따라 나눈 배치입니다.
                outline = [(x - .245, 2.39), (x + .245, 2.39), (x + .245, 2.26), (x + .13, 2.18), (x, 2.16), (x - .13, 2.18), (x - .245, 2.26)]  # 천막 끝의 부드러운 가공 윤곽입니다.
                obj = C.extrude_outline('Detail_AwningHem_' + str(index), outline, .045, 'Coral' if index % 2 else 'Cream', .007)  # 단순한 상자 끝단을 천의 곡선으로 마감합니다.
                obj.location.y = -2.58  # 기존 차양의 앞면에 실제 끝단을 연결합니다.
    print('건물 외벽과 창가 및 지붕 마감 원본 제작 저장 준비', flush=True)  # 실제 제작 진행만 알립니다.


def make_neighborhood():  # 기존 건물과 동선을 보존하며 비어 있는 거리에 상세 건물을 추가합니다.
    target = root('SoftNeighborhood')  # 새 건물만 별도로 편집할 수 있는 원본 루트입니다.
    target['neighborhood_part'] = True  # 건물 배치만 다시 제작할 때의 소유 범위입니다.
    bpy.context.view_layer.update()  # 기존 소품의 접지와 회전이 실제 좌표에 반영되게 합니다.
    prepare_placement_surface()  # 기존 지형과 앞마당 및 건물 영역을 재사용합니다.
    obstacles = list(BUILDING_FOOTPRINTS)  # 기존 건물은 추가 건물로 대체하지 않습니다.
    dressing = bpy.data.objects['SoftStreetDressing']  # 이미 배치한 가로수와 벤치 및 조명입니다.
    for obj in C.descendants(dressing):  # 새 건물이 기존 소품을 덮지 않게 실제 영역을 읽습니다.
        if obj.type != 'MESH':  # 메시가 없는 루트는 배치 부피가 없습니다.
            continue  # 실제 시각 부품만 반영합니다.
        name = obj.get('export_name', '')  # 가로수와 생활 소품의 실제 부위 이름입니다.
        if not any(word in name for word in ('Canopy', 'Trunk', 'Branch', 'LowShrub', 'Seat', 'Back', 'Frame', 'Shade', 'Rim')):  # 건물과 충돌할 수 있는 주요 부피입니다.
            continue  # 바닥의 작은 풀잎 때문에 넓은 건물 자리를 비우지 않습니다.
        low, high = bounds(dressing, [obj])  # 소품의 실제 위치와 회전을 포함한 경계입니다.
        obstacles.append((low.x, high.x, low.z, high.z))  # 수관과 조명 갓까지 건물 배치에서 제외합니다.
    models = [bpy.data.objects[name] for name in ('PeachTownhouse', 'BlueTownhouse', 'CornerShop')]  # 창가와 지붕 마감이 완성된 기존 카툰 건물입니다.
    palettes = tuple(name for name in ('Pink', 'Blue', 'School', 'Mint', 'Cream', 'Apricot') if name in C.MATERIALS)  # 현재 원본에 실제로 남아 있는 기존 재질만 외벽에 사용합니다.
    roofs = ('Roof', 'Coral', 'TealMetal')  # 새 그래픽 계열을 만들지 않는 지붕 팔레트입니다.
    serial = 0  # 새 건물의 배치와 재질 변형을 일정한 순서로 정합니다.
    for row, distance, spacing, start, end in ((0, None, 7, -66, 67), (1, 38., 12, -64, 65)):  # 가까운 건물 줄과 뒤쪽 원경 건물 줄입니다.
        for sign in (-1, 1):  # 도로 양쪽을 함께 채웁니다.
            turn = math.pi if sign > 0 else 0  # 모든 출입구가 도로 쪽을 향하는 실제 회전입니다.
            z = sign * distance if distance is not None else (14.2 if sign > 0 else -16.5)  # 기존 식재 띠와 도입 건물 뒤의 여유 공간입니다.
            for index, x in enumerate(range(start, end, spacing)):  # 기존 동선의 위치는 그대로 두고 빈 건물 자리를 선택합니다.
                if row == 0 and sign > 0 and abs(x) < 13.:  # 기존 학교 앞마당과 입구가 보이는 구간입니다.
                    continue  # 학교 앞에 다른 건물을 세우지 않습니다.
                source = models[(index + row + (1 if sign > 0 else 0)) % len(models)]  # 서로 다른 폭과 층수 및 상점이 교대로 보이게 합니다.
                source_objects = [obj for obj in C.descendants(source) if obj.type == 'MESH']  # 간판과 창가 및 지붕까지 포함한 실제 원본입니다.
                low, high = bounds(source, source_objects)  # 임시 도형의 크기가 아닌 상세 모델의 경계입니다.
                flat = Matrix.Translation(Vector((x, 0, z))) @ Matrix.Rotation(turn, 4, 'Y')  # 크기 변형 없이 적용할 배치와 정면 방향입니다.
                corners = [flat @ Vector((a, 0, b)) for a in (low.x, high.x) for b in (low.z, high.z)]  # 지붕과 차양의 돌출까지 포함한 평면 범위입니다.
                rect = (min(p.x for p in corners), max(p.x for p in corners), min(p.z for p in corners), max(p.z for p in corners))  # 기존 건물과 소품 사이에 필요한 실제 영역입니다.
                clearance = .65 if row == 0 else 1.0  # 가까운 골목과 원경 사이의 간격입니다.
                if any(rect[0] < right + clearance and rect[1] > left - clearance and rect[2] < far + clearance and rect[3] > near - clearance for left, right, near, far in obstacles):  # 실제 기존 부피와 겹치는 자리입니다.
                    continue  # 건물 수를 늘리려고 기존 배치를 침범하지 않습니다.
                base_objects = [obj for obj in source_objects if obj.get('export_name') in ('Walls', 'BaseTrim')]  # 기초를 받칠 실제 외벽과 바닥 마감입니다.
                base_low, base_high = bounds(source, base_objects)  # 지붕 돌출 때문에 기초가 불필요하게 넓어지지 않게 합니다.
                footprint_points = [flat @ Vector((a, 0, b)) for a in (base_low.x - .08, (base_low.x + base_high.x) * .5, base_high.x + .08) for b in (base_low.z - .08, (base_low.z + base_high.z) * .5, base_high.z + .08)]  # 네 모서리와 가운데 지형 높이를 반영합니다.
                floor = max(ground_height(point.x, point.z) for point in footprint_points) + .08  # 기초 아래의 가장 높은 지형보다 건물 밑면을 조금 높게 둡니다.
                before = set(bpy.data.objects)  # 현재 추가 건물의 실제 소유 부위를 구분합니다.
                building = C.copy_model(source, f'Neighborhood_{row}_{sign}_{index}', (x, floor - base_low.y, z), target, math.degrees(turn))  # 기존 상세 모델을 동일한 형태와 비율로 복제합니다.
                building['neighborhood_building'] = True  # 이후의 식재 배치에도 건물 영역을 전달합니다.
                for obj in set(bpy.data.objects) - before:  # 새로 복제한 부품에만 색상 변형을 적용합니다.
                    obj['visual_detail'], obj['neighborhood_part'] = True, True  # 기존 원본과 새 건물을 명확하게 구분합니다.
                    if obj.type != 'MESH':  # 빈 루트에는 재질이 없습니다.
                        continue  # 실제 건물 표면만 조정합니다.
                    name = obj.get('export_name', '')  # 원본의 실제 건축 부위입니다.
                    if name == 'Walls':  # 외벽만 기존 팔레트 안에서 교대로 채색합니다.
                        for slot in range(len(obj.data.materials)):  # 복제본의 독립 메시 재질 슬롯입니다.
                            obj.data.materials[slot] = C.MATERIALS[palettes[serial % len(palettes)]]  # 원본 건물과 공유 재질 값은 바꾸지 않습니다.
                    elif 'roof' in name.lower():  # 지붕 마감선은 유지하고 주 표면 색만 변형합니다.
                        for slot, material in enumerate(obj.data.materials):  # 복제 지붕의 실제 재질입니다.
                            if material and material.name in ('Soft_Roof', 'Soft_Coral'):  # 주 지붕 색상만 선택합니다.
                                obj.data.materials[slot] = C.MATERIALS[roofs[serial % len(roofs)]]  # 창틀과 배수관의 색상은 유지합니다.
                perimeter = [flat @ Vector((a, 0, b)) for a, b in ((base_low.x - .08, base_low.z - .08), (base_high.x + .08, base_low.z - .08), (base_high.x + .08, base_high.z + .08), (base_low.x - .08, base_high.z + .08))]  # 실제 건물 방향에 맞춘 기초 외곽입니다.
                vertices = [(point.x, ground_height(point.x, point.z) - .04, point.z) for point in perimeter] + [(point.x, floor + .008, point.z) for point in perimeter]  # 아래는 지형에 묻히고 위는 수평인 실제 기초입니다.
                faces = [(0, 3, 2, 1), (4, 5, 6, 7)] + [(i, (i + 1) % 4, (i + 1) % 4 + 4, i + 4) for i in range(4)]  # 경사진 지면에서도 닫힌 기초 표면입니다.
                footing = C.mesh(f'Detail_HouseFooting_{row}_{sign}_{index}', vertices, faces, 'Sandstone', smooth=False)  # 건물이 떠 보이지 않도록 지형과 연결합니다.
                footing['visual_detail'], footing['neighborhood_part'] = True, True  # 같은 추가 건물 작업에서만 기초를 교체합니다.
                obstacles.append(rect)  # 다음 건물도 이번 건물과 겹치지 않게 합니다.
                serial += 1  # 실제 추가한 건물에만 다음 변형을 사용합니다.
    print('기존 건물과 동선을 유지한 거리·원경 건물 원본 제작 저장 준비', flush=True)  # 화면 확인이나 실행 결과는 주장하지 않습니다.


def make_prop_details():  # 차량과 전화 및 교통 소품에 제작 구조가 읽히는 디테일을 추가합니다.
    root('SoftCar')  # 기존 차체와 바퀴 및 주행 방향을 유지합니다.
    for sign in (-1, 1):  # 좌우 차체에 같은 제조 구조를 적용합니다.
        x = sign * .913  # 실제 차체 옆면에 밀착한 좌표입니다.
        line('DoorSeam_' + str(sign), [(x, .33, -.52), (x, .74, -.52), (x * .985, .82, .42), (x, .33, .52)], .009, 'CoralShadow')  # 도어의 실제 절개와 이음새입니다.
        line('LowerTrim_' + str(sign), [(sign * .77, .30, -1.70), (x, .30, -.6), (x, .30, .8), (sign * .72, .34, 1.65)], .024, 'RoofTrim')  # 앞뒤 펜더와 이어지는 차체 하단 마감입니다.
        block('MirrorLens_' + str(sign), (sign * .80, .991, .550), (.11, .065, .012), 'GlassHighlight', .004)  # 거울 하우징 안의 실제 반사 면입니다.
        for z in (-1.18, 1.13):  # 원본 제작기에서 사용한 실제 앞뒤 차축 위치입니다.
            for index in range(5):  # 금속 휠의 고정 구조입니다.
                angle = math.tau * index / 5  # 휠 중심 둘레의 일정한 간격입니다.
                C.oval('Detail_WheelBolt_' + str(sign) + str(z) + str(index), (sign * 1.011, .35 + .075 * math.cos(angle), z + .075 * math.sin(angle)), (.010, .014, .014), 'Cream', segments=8, rings=4)  # 실제 휠 바깥 면에 붙은 작은 고정 부품입니다.
    for index in range(6):  # 기존 검은 그릴 안의 얇은 판입니다.
        block('GrilleFin_' + str(index), ((index - 2.5) * .065, .46, 1.970), (.018, .055, .012), 'RoofTrim', .002)  # 차체에 실제로 붙은 그릴 구조입니다.
    for sign in (-1, 1):  # 앞 유리의 경사와 맞춘 와이퍼입니다.
        line('Wiper_' + str(sign), [(sign * .32, .955, .820), (sign * .22, 1.06, .725), (sign * .09, 1.11, .680)], .011, 'Ink')  # 유리 표면을 따라 놓인 제조 부품입니다.
    block('NumberPlate', (0, .29, 2.0), (.40, .12, .017), 'Cream', .008)  # 범퍼에 밀착한 작은 번호판입니다.
    for index in range(5):  # 판 위에 낮은 대비의 인쇄 요소를 만듭니다.
        block('PlateMark_' + str(index), ((index - 2) * .055, .29, 2.012), (.027, .055, .006), 'RoofTrim', .001)  # 임의의 큰 장식 없이 차체 마감을 완성합니다.
    root('PocketPhone')  # 기존 손잡기 위치와 화면 중심을 유지합니다.
    for index in range(4):  # 화면 위의 실제 스피커 그릴입니다.
        block('Speaker_' + str(index), ((index - 1.5) * .0042, .137, .0105), (.0022, .0014, .002), 'Ink', .0003)  # 전화 크기에 맞는 작은 부품입니다.
    for sign in (-1, 1):  # 본체 양쪽의 실제 버튼입니다.
        block('PhoneKey_' + str(sign), (sign * .0413, .085, 0), (.0022, .020, .006), 'RoofTrim', .0006)  # 큰 상자 표면이 아닌 얇은 제조 디테일입니다.
    root('SoftUtilityPole')  # 기존 사고 대상 기둥의 위치와 접촉 표면을 유지합니다.
    for y in (2.95, 3.35):  # 원본 변압기와 기둥을 잇는 실제 고정 구조입니다.
        block('TransformerBracket_' + str(y), (.19, y, -.03), (.25, .055, .11), 'TealMetal', .008)  # 설비가 기둥 옆에서 떠 보이지 않게 합니다.
    line('TransformerCable', [(.30, 3.49, -.06), (.34, 3.70, -.08), (.09, 3.87, -.07), (-.37, 3.93, -.04)], .016, 'Ink')  # 연결부 사이를 실제 곡선으로 이어 줍니다.
    root('SoftPedestrianSignal')  # 기존 빨간불과 초록불 도형은 수정하지 않습니다.
    for sign in (-1, 1):  # 하우징의 실제 양쪽 고정 나사입니다.
        for y in (2.13, 2.95):  # 표시 도형을 가리지 않는 모서리입니다.
            C.oval('Detail_HousingScrew_' + str(sign) + str(y), (sign * .205, y, .145), (.018, .018, .012), 'RoofTrim', segments=10, rings=5)  # 과한 반사 없이 작은 금속 구조를 보여 줍니다.
    print('차량과 휴대전화 및 교통 소품 마감 원본 제작 저장 준비', flush=True)  # 실제 에셋 제작 진행입니다.


def attach_cloth_skin(obj, shirt, rig):  # 새 봉제선이 기존 의상과 같은 관절 영향을 받게 합니다.
    kd = KDTree(len(shirt.data.vertices))  # 기존 의상 정점을 찾을 실제 좌표 목록입니다.
    for vertex in shirt.data.vertices:  # 원본 옷의 변경되지 않은 표면입니다.
        kd.insert(vertex.co, vertex.index)  # 실제 스킨 정점 번호를 보관합니다.
    kd.balance()  # 새 디테일 정점에서 가까운 원본 옷 정점을 찾습니다.
    groups = {}  # 실제 사용한 관절에만 웨이트 그룹을 만듭니다.
    for vertex in obj.data.vertices:  # 봉제선과 패치의 각 실제 정점입니다.
        _, index, _ = kd.find(vertex.co)  # 같은 위치의 기존 옷 스킨을 읽습니다.
        for entry in shirt.data.vertices[index].groups:  # 몸과 어깨에 적용된 원본 웨이트입니다.
            name = shirt.vertex_groups[entry.group].name  # 원본의 실제 관절 이름입니다.
            if name not in rig.data.bones:  # 모델링 보조 그룹은 변형에 사용하지 않습니다.
                continue  # 실제 골격 영향만 전달합니다.
            if name not in groups:  # 필요한 관절 그룹을 한 번만 만듭니다.
                groups[name] = obj.vertex_groups.new(name=name)  # 새 디테일에 같은 관절 이름을 연결합니다.
            groups[name].add([vertex.index], entry.weight, 'REPLACE')  # 기존 옷과 동일한 변형 영향입니다.
    modifier = obj.modifiers.new('원본 의상 골격', 'ARMATURE')  # 새 디테일에도 같은 원본 관절 변형을 연결합니다.
    modifier.object, obj.parent = rig, rig  # 새 골격이나 별도의 애니메이션을 만들지 않습니다.


def make_cloth_details():  # 얼굴과 신체 및 골격을 유지하며 실제 옷 표면에 봉제 마감을 제작합니다.
    for name in ('MintFriend', 'ApricotFriend'):  # 현재 원본 두 캐릭터입니다.
        target = root(name)  # 현재 얼굴과 헤어 및 비율은 유지합니다.
        rig = next(obj for obj in C.descendants(target) if obj.type == 'ARMATURE')  # 원본의 기존 골격입니다.
        shirt = next(obj for obj in C.descendants(target) if obj.type == 'MESH' and obj.get('export_name') == 'ConnectedShirt')  # 몸판과 소매가 연결된 실제 의상입니다.
        tree = BVHTree.FromPolygons([tuple(vertex.co) for vertex in shirt.data.vertices], [tuple(face.vertices) for face in shirt.data.polygons])  # 이미 완성된 옷의 실제 앞면입니다.
        def point(x, y, offset=.002):  # 봉제 마감이 옷에서 뜨지 않도록 배치합니다.
            hit, _, _, _ = tree.ray_cast(Vector((x, -.6, y)), Vector((0, 1, 0)), 1.4)  # 기존 옷의 실제 표면을 읽습니다.
            return (hit.x, hit.z, -hit.y + offset) if hit is not None else None  # 원래 실루엣 위의 미세한 마감 위치입니다.
        objects = []  # 기존 옷 스킨을 전달할 새 부위입니다.
        for sign in (-1, 1):  # 양쪽 몸판의 실제 봉제 흐름입니다.
            points = [point(sign * (.115 + .005 * math.sin(i)), .49 + i * .028) for i in range(8)]  # 밑단에서 가슴으로 이어지는 얇은 봉제선입니다.
            points = [item for item in points if item is not None]  # 실제 옷 표면이 있는 구간만 사용합니다.
            if len(points) > 1:  # 연속된 실제 표면에서만 봉제 마감을 만듭니다.
                objects.append(line('BodyStitch_' + str(sign), points, .0016, 'Thread' if name == 'MintFriend' else 'Cream'))  # 원본 의상을 해치지 않는 미세한 두께입니다.
        patch = [point(.062 + .024 * math.cos(math.tau * i / 6), .67 + .025 * math.sin(math.tau * i / 6), .003) for i in range(6)]  # 가슴 위의 작은 천 라벨 윤곽입니다.
        if all(item is not None for item in patch):  # 실제 옷 위에서만 라벨을 제작합니다.
            objects.append(C.mesh('Detail_ClothBadge', patch, [tuple(range(6))], 'Cream', smooth=False))  # 입체 기본 도형 대신 밀착한 천 패치입니다.
            centre = point(.062, .67, .004)  # 천 라벨의 실제 중앙입니다.
            if centre is not None:  # 중심도 옷 위에 존재하는 경우입니다.
                x, y, z = centre  # 원본 가슴 표면의 실제 위치입니다.
                objects.append(line('BadgeLeaf', [(x - .011, y - .009, z), (x, y + .007, z), (x + .010, y + .012, z)], .0021, 'Roof'))  # 세계의 식물 모티브를 작은 봉제로 표현합니다.
        for obj in objects:  # 새 의상 디테일의 기존 변형 연결입니다.
            attach_cloth_skin(obj, shirt, rig)  # 기존 보행과 전화 동작에 같은 옷처럼 따라갑니다.
    print('원본 골격을 유지한 의상 봉제와 천 라벨 제작 저장 준비', flush=True)  # 기존 동작은 실행하지 않습니다.


def make_atmosphere():  # 카툰 구름과 원경의 큰 실루엣을 실제 메시로 제작합니다.
    root('SoftAtmosphere')  # 기존 게임 위치를 바꾸지 않는 원경 전용 배경입니다.
    for index, (x, y, z, width) in enumerate(((-50., 19., 52., 12.), (-17., 23., 69., 16.), (23., 18., 63., 13.), (59., 26., 85., 18.), (-66., 26., -61., 16.), (22., 22., -74., 18.))):  # 모든 시야 방향에 나누어 놓은 구름입니다.
        profile = []  # 여러 공을 붙이지 않은 구름의 연속 단면입니다.
        for step in range(17):  # 구름 전체를 하나의 가로형 표면으로 설계합니다.
            u = step / 16  # 왼쪽 끝에서 오른쪽 끝으로의 위치입니다.
            envelope = max(.001, math.sin(math.pi * u)) ** .65  # 양끝이 자연스럽게 모이는 실루엣입니다.
            upper = .75 + .35 * math.sin(u * math.pi * 5 + index) ** 2  # 이어진 구름 안에서 높이가 다른 봉우리입니다.
            profile.append((x + (u - .5) * width, envelope * width * .12 * upper, envelope * width * .055, y, z))  # 한 표면으로 이어진 구름 단면입니다.
        C.loft('Detail_CloudBank_' + str(index), profile, 'White', axis='X', segments=20, subdivision=0)  # 깊이가 얕고 실루엣이 분명한 그림책 구름입니다.


def prepare_materials():  # 기존 카툰 팔레트에 마감재의 재질 차이만 보탭니다.
    C.MATERIALS.update({material.name.removeprefix('Soft_'): material for material in bpy.data.materials if material.name.startswith('Soft_')})  # 현재 원본에 연결된 실제 재질을 재사용합니다.
    colors = {'Meadow': (.43, .65, .36), 'Sage': (.48, .67, .49), 'DistantGreen': (.48, .66, .62), 'Wood': (.56, .34, .20), 'TealMetal': (.15, .32, .33), 'RoofTrim': (.18, .37, .39), 'Sandstone': (.82, .75, .59), 'Terracotta': (.72, .36, .23), 'CoralShadow': (.64, .22, .23), 'GlassHighlight': (.48, .71, .74), 'Thread': (.99, .80, .63), 'RoadDetail': (.23, .28, .32)}  # 기존 색상 계열 안에서 서로 다른 마감재를 구분합니다.
    C.COLORS.update(colors)  # 기존 제작기의 같은 팔레트 저장 방식을 사용합니다.
    for name, color in colors.items():  # 새 마감재만 원본에 연결합니다.
        material = bpy.data.materials.get('Soft_' + name) or bpy.data.materials.new('Soft_' + name)  # 같은 원본을 다시 적용해도 재질을 중복 만들지 않습니다.
        material.diffuse_color, material.use_nodes = (*color, 1), True  # 원본의 실제 선형 팔레트입니다.
        C.MATERIALS[name] = material  # 기존 메시 제작 함수에 같은 재질을 전달합니다.
    C.set_blender_cartoon_materials(set(colors))  # 기존 카툰의 두 단계 명암 방식을 유지합니다.
    C.save_materials(set(colors))  # 새 마감재만 기존 Unity 머티리얼 형식으로 저장합니다.


def write_material(path, shader, floats=(), colors=()):  # 실제 Unity 표면 재질을 기존 에셋 형식으로 저장합니다.
    guid = A.metadata(path, 'NativeFormatImporter', 2100000)  # 재적용 시 같은 재질 GUID를 유지합니다.
    text = A.HEADER + f'--- !u!21 &2100000\nMaterial:\n  serializedVersion: 8\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_Name: {path.stem}\n  m_Shader: {{fileID: 4800000, guid: {shader}, type: 3}}\n  m_Parent: {{fileID: 0}}\n  m_ModifiedSerializedProperties: 0\n  m_ValidKeywords: []\n  m_InvalidKeywords: []\n  m_LightmapFlags: 4\n  m_EnableInstancingVariants: 1\n  m_DoubleSidedGI: 0\n  m_CustomRenderQueue: -1\n  stringTagMap: {{}}\n  disabledShaderPasses: []\n  m_LockedProperties: \n  m_SavedProperties:\n    serializedVersion: 3\n    m_TexEnvs: []\n    m_Ints: []\n    m_Floats:\n'  # Unity 직렬화 파일에는 소스 코드 주석을 넣지 않습니다.
    text = text if floats else text.removesuffix('    m_Floats:\n') + '    m_Floats: []\n'  # 실수 속성이 없는 하늘 재질도 빈 배열로 직렬화하여 비어 있는 매핑을 만들지 않습니다.
    text += ''.join(f'    - {key}: {value}\n' for key, value in floats)  # 실제 실수 속성만 원래 배열 형식으로 저장합니다.
    text += ('    m_Colors:\n' if colors else '    m_Colors: []\n') + ''.join('    - ' + key + ': {' + ', '.join(f'{axis}: {value:.9g}' for axis, value in zip('rgba', values)) + '}\n' for key, values in colors)  # 색상 속성도 비어 있을 때는 명시적인 빈 배열로 저장합니다.
    A.write(path, text + '  m_BuildTextureStacks: []\n  m_AllowLocking: 1\n')  # 실제 씬이 참조할 표면 에셋을 저장합니다.
    return guid  # 씬의 렌더링 연결에 사용합니다.


def apply_sky_and_ui():  # 기존 UI 동작과 게임 배치를 유지하며 시각 재질만 연결합니다.
    sky_shader = A.metadata(AREA / 'Shaders' / 'StorybookSky.shader', 'ShaderImporter', 0)  # 새 하늘의 실제 셰이더 GUID입니다.
    ui_shader = A.metadata(AREA / 'Shaders' / 'StorybookUI.shader', 'ShaderImporter', 0)  # 카드와 버튼의 실제 셰이더 GUID입니다.
    sky = write_material(AREA / 'Materials' / 'Environment' / 'StorybookSky.mat', sky_shader, colors=(('_Horizon', (.72, .86, .88, 1)), ('_Zenith', (.29, .57, .73, 1)), ('_SunDirection', (-.35, .55, -.76, 0))))  # 환경 재질 폴더에 있는 기존 하늘 에셋에 색을 저장합니다.
    roles = {}  # 원래 UI의 역할별로 공유할 표면 재질입니다.
    for style, name in enumerate(('StorybookCard', 'StorybookButton', 'StorybookBadge')):  # 같은 인쇄 규칙의 카드와 버튼 및 장식입니다.
        floats = (('_Style', style), ('_StencilComp', 8), ('_Stencil', 0), ('_StencilOp', 0), ('_StencilReadMask', 255), ('_StencilWriteMask', 255), ('_ColorMask', 15), ('_UseUIAlphaClip', 0))  # 기존 UI 마스크 설정을 유지합니다.
        roles[style] = write_material(AREA / 'Materials' / 'UI' / (name + '.mat'), ui_shader, floats, (('_Color', (1, 1, 1, 1)),))  # UI 재질 폴더에서 기존 카드와 버튼 및 장식의 GUID를 재사용합니다.
    docs = A.read_documents(A.SCENE)  # 모델 교체 이후의 실제 게임 씬입니다.
    names, _, _ = A.layout(docs)  # UI의 실제 소유 오브젝트 이름입니다.
    button_images = {A.reference(doc['body'], 'm_TargetGraphic') for doc in docs if doc['kind'] == 114 and '  m_TargetGraphic:' in doc['body'] and '  m_OnClick:' in doc['body']}  # 실제 클릭 버튼의 기존 배경 이미지만 선택합니다.
    sprites = {0: '87df43b483d753629b771495d4ed4226', 1: '3bed11e7501e5853b6569b4dc0d59dc3', 2: '46950fda73bf5f56a4defed0b123af40'}  # 기존 카드와 쿠션 버튼 및 구름 장식의 실제 GUID입니다.
    for doc in docs:  # 위치와 진행 코드 대신 실제 렌더링 필드만 수정합니다.
        body = doc['body']  # 기존 직렬화 본문입니다.
        if doc['kind'] == 104:  # 이 씬의 하늘과 원경 연결 설정입니다.
            body = re.sub(r'(?m)^  m_SkyboxMaterial: .*', f'  m_SkyboxMaterial: {{fileID: 2100000, guid: {sky}, type: 2}}', body)  # 기본 하늘 대신 카툰 전용 하늘을 연결합니다.
            for field, value in (('m_Fog', '1'), ('m_FogMode', '1'), ('m_LinearFogStart', '55'), ('m_LinearFogEnd', '165'), ('m_FogColor', '{r: 0.72, g: 0.86, b: 0.88, a: 1}')):  # 원경 지형이 하늘에 이어지는 같은 색상의 얕은 안개입니다.
                body = re.sub(r'(?m)^  ' + field + r': .*', f'  {field}: {value}', body)  # 카메라 위치나 시야각은 바꾸지 않습니다.
        if doc['kind'] == 114 and '  m_Sprite:' in body:  # 기존 UI Image 컴포넌트입니다.
            style = next((key for key, guid in sprites.items() if guid in body or roles[key] in body), None)  # 같은 역할의 기존 표면만 교체합니다.
            if style == 1 and doc['id'] not in button_images:  # 안내 그림 안의 작은 기호는 실제 버튼이 아닙니다.
                style = None  # 기존 인포그래픽의 선과 마우스 모양을 유지합니다.
            if style is not None:  # 실제 카드와 클릭 버튼 및 배경 장식입니다.
                body = re.sub(r'(?m)^  m_Material: .*', f'  m_Material: {{fileID: 2100000, guid: {roles[style]}, type: 2}}', body)  # 기존 입력과 표시 오브젝트에 새 표면만 연결합니다.
                body = re.sub(r'(?m)^  m_Sprite: .*', '  m_Sprite: {fileID: 0}', body)  # 기존 구름 이미지의 늘어난 외곽 대신 실제 카드 외곽을 그립니다.
                body = re.sub(r'(?m)^  m_Type: .*', '  m_Type: 0', body)  # 기존 RectTransform 전체를 같은 비율의 카드 좌표로 사용합니다.
                body = re.sub(r'(?m)^  m_PreserveAspect: .*', '  m_PreserveAspect: 0', body)  # 카드가 실제 기존 버튼 영역을 채우게 합니다.
            if '2ec74526914a5d8dbc82d9032e0dc1a0' in body:  # 기존 말풍선 꼬리의 연결 색상입니다.
                values = re.search(r'm_Color: \{r: ([^,]+), g: ([^,]+), b: ([^,]+), a: ([^}]+)\}', body)  # 현재 상황별 기존 파스텔입니다.
                if values:  # 실제 이미지 색상이 저장된 경우입니다.
                    rgba = [float(value) for value in values.groups()]  # 원래 알파를 유지할 실제 색상입니다.
                    color = tuple(rgba[index] * .36 + (1., .985, .94)[index] * .64 for index in range(3)) + (rgba[3],)  # 종이 카드 본체와 같은 크림색 비율입니다.
                    body = re.sub(r'(?m)^  m_Color: .*', '  m_Color: {' + ', '.join(f'{axis}: {value:.9g}' for axis, value in zip('rgba', color)) + '}', body)  # 기존 꼬리와 새 카드의 이음새를 연결합니다.
        if doc['kind'] == 114 and '  m_fontColor:' in body:  # 기존 둥근 한글 글꼴과 텍스트 영역입니다.
            game = A.reference(body, 'm_GameObject')  # 실제 UI 텍스트 소유 오브젝트입니다.
            name = names.get(game, '')  # 조준점과 별도 HUD 문구는 구분합니다.
            if name not in ('중앙 조준 표시', '관찰 조작 모드 안내'):  # 기존 관찰 조준 표시의 대비는 유지합니다.
                body = re.sub(r'(?m)^  m_fontColor: .*', '  m_fontColor: {r: 0.16, g: 0.27, b: 0.28, a: 1}', body)  # 밝은 종이 표면에서 읽기 좋은 통일된 먹색입니다.
                packed = sum(round(value * 255) << (8 * index) for index, value in enumerate((.16, .27, .28, 1.)))  # 같은 먹색의 RGBA 바이트 표현입니다.
                body = re.sub(r'(?m)^  m_fontColor32:.*\n    rgba: .*', f'  m_fontColor32:\n    rgba: {packed}', body)  # 두 텍스트 색상 필드가 같은 값을 사용합니다.
        doc['body'] = body  # 기존 이벤트와 카메라 및 진행 필드는 유지합니다.
    A.save_documents(A.SCENE, docs)  # 시각 재질만 변경한 실제 게임 씬을 저장합니다.
    print('하늘과 원경 및 기존 UI 표면 연결 저장', flush=True)  # 에셋 연결 저장만 알립니다.


def authoring_sky():  # Blender 원본에도 같은 하늘 팔레트와 카툰 조명을 저장합니다.
    world = bpy.context.scene.world or bpy.data.worlds.new('StorybookWorld')  # 원본의 실제 배경 재질입니다.
    bpy.context.scene.world, world.use_nodes = world, True  # 원본 장면에 같은 배경을 연결합니다.
    nodes, links = world.node_tree.nodes, world.node_tree.links  # 하늘의 실제 노드 구성입니다.
    nodes.clear()  # 원본 하늘 표면을 같은 색상 설계로 저장합니다.
    coordinate = nodes.new('ShaderNodeTexCoord')  # Blender 하늘의 방향 좌표입니다.
    split = nodes.new('ShaderNodeSeparateXYZ')  # Blender의 높이 축인 Z 방향을 읽습니다.
    ramp = nodes.new('ShaderNodeValToRGB')  # 수평선과 머리 위 하늘을 이어 주는 색상입니다.
    ramp.color_ramp.elements[0].position, ramp.color_ramp.elements[0].color = 0., (.72, .86, .88, 1)  # 지형과 이어지는 수평선 색상입니다.
    ramp.color_ramp.elements[1].position, ramp.color_ramp.elements[1].color = .82, (.29, .57, .73, 1)  # 원본과 게임에서 공유하는 위쪽 하늘색입니다.
    background = nodes.new('ShaderNodeBackground')  # 원본의 실제 배경 출력입니다.
    output = nodes.new('ShaderNodeOutputWorld')  # 편집 가능한 원본 하늘의 최종 연결입니다.
    links.new(coordinate.outputs['Normal'], split.inputs[0])  # 하늘 방향에서 높이를 읽습니다.
    links.new(split.outputs['Z'], ramp.inputs[0])  # 높이에 따라 파스텔 색상을 연결합니다.
    links.new(ramp.outputs['Color'], background.inputs['Color'])  # 설계한 카툰 하늘을 표시합니다.
    links.new(background.outputs[0], output.inputs['Surface'])  # 원본 장면의 배경에 연결합니다.
    if bpy.data.objects.get('StorybookSun') is None:  # 원본에 필요한 카툰 방향광만 한 번 만듭니다.
        light = bpy.data.lights.new('StorybookSun', 'SUN')  # 원본 ShaderToRGB의 일정한 조명입니다.
        light.energy, light.angle = 2., .12  # 큰 색면과 부드러운 그림자를 유지합니다.
        obj = bpy.data.objects.new('StorybookSun', light)  # 원본 편집용 실제 광원입니다.
        bpy.context.scene.collection.objects.link(obj)  # 게임 카메라나 기존 게임 광원은 건드리지 않습니다.
        obj.rotation_euler = Vector((.35, -.76, -.55)).to_track_quat('-Z', 'Y').to_euler()  # 게임의 태양 방향을 Blender의 높이 축으로 변환한 햇빛입니다.
    for screen in bpy.data.screens:  # 원본 파일 안에 저장되는 편집 표시 설정입니다.
        for area in screen.areas:  # 실제 모델 편집 영역만 설정합니다.
            if area.type == 'VIEW_3D':  # 사용자 컴퓨터의 창을 열거나 실행하지 않습니다.
                area.spaces.active.shading.use_scene_world = True  # 원본을 열 때 제작한 하늘을 사용합니다.
                area.spaces.active.shading.use_scene_lights = True  # 원본의 카툰 조명을 함께 사용합니다.
                area.spaces.active.clip_end = 1000  # 연결된 원경 지형이 편집 표시에서 잘리지 않게 합니다.


def update_dressing_layout():  # 가로수와 벤치 및 가로등의 배치만 원본과 게임 배경에 저장합니다.
    existing = bpy.data.objects['SoftStreetDressing']  # 이전에 제작한 거리 소품의 소유 루트입니다.
    for obj in reversed(C.descendants(existing)):  # 같은 소품 루트 아래의 기존 배치만 교체합니다.
        bpy.data.objects.remove(obj, do_unlink=True)  # 원래 가로수와 건물 및 게임 오브젝트는 삭제하지 않습니다.
    C.MATERIALS.update({material.name.removeprefix('Soft_'): material for material in bpy.data.materials if material.name.startswith('Soft_')})  # 기존 카툰 재질을 변경하지 않고 재사용합니다.
    before = set(bpy.data.objects)  # 이번 배치에서 새로 제작한 실제 부위를 구분합니다.
    make_dressing()  # 실제 지면 높이와 건물 영역을 반영한 소품 배치를 작성합니다.
    for obj in set(bpy.data.objects) - before:  # 이 배치 코드가 소유한 부위만 기록합니다.
        obj['visual_detail'] = True  # 다음 원본 수정에서도 기존 모델을 보존합니다.
    bpy.context.view_layer.update()  # 저장할 소품의 실제 행렬을 반영합니다.
    bpy.ops.object.select_all(action='DESELECT')  # 원본에 전체 선택 상태를 남기지 않습니다.
    bpy.context.preferences.filepaths.save_version = 0  # 배치 수정에 불필요한 이전 버전 복사본을 만들지 않습니다.
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / 'SoftCrosswalk.blend'))  # 수정한 실제 소품 배치가 들어 있는 Blender 원본입니다.
    print('가로수·벤치·가로등 배치 Blender 원본 저장', flush=True)  # 화면이나 게임 동작 확인을 주장하지 않습니다.
    A.apply_environment_models(background_only=True)  # 같은 배경 프리팹에 수정한 배치를 연결합니다.
    print('거리 소품 배경 연결 저장: 실제 접지·도로 방향·빈 구간 식재', flush=True)  # 캐릭터와 UI 및 진행 설정은 다시 작성하지 않습니다.


def update_neighborhood():  # 건물 보강만 원본과 기존 게임 배경에 저장합니다.
    existing = bpy.data.objects.get('SoftNeighborhood')  # 이 제작 코드가 추가한 건물만 다시 배치합니다.
    if existing is not None:  # 같은 건물 보강을 재적용하는 경우입니다.
        for obj in reversed(C.descendants(existing)):  # 추가 건물과 기초를 같은 소유 범위에서 교체합니다.
            bpy.data.objects.remove(obj, do_unlink=True)  # 기존 학교와 거리 건물 및 가로수는 보존합니다.
    C.MATERIALS.update({material.name.removeprefix('Soft_'): material for material in bpy.data.materials if material.name.startswith('Soft_')})  # 현재 카툰 재질을 변경하지 않고 재사용합니다.
    before = set(bpy.data.objects)  # 이번 요청으로 추가한 실제 원본 부위입니다.
    make_neighborhood()  # 지형과 기존 배치를 반영하여 가까운 거리와 원경을 채웁니다.
    for obj in set(bpy.data.objects) - before:  # 새 건물 루트와 실제 부품만 기록합니다.
        obj['visual_detail'], obj['neighborhood_part'] = True, True  # 이후에도 기존 모델과 구분합니다.
    bpy.context.view_layer.update()  # 저장할 새 건물의 실제 위치와 회전을 반영합니다.
    bpy.ops.object.select_all(action='DESELECT')  # 원본에 전체 선택 상태를 남기지 않습니다.
    bpy.context.preferences.filepaths.save_version = 0  # 실제 원본 저장에 불필요한 이전 버전 복사본을 만들지 않습니다.
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / 'SoftCrosswalk.blend'))  # 상세 건물이 보강된 실제 Blender 원본입니다.
    print('거리와 원경 건물 보강 Blender 원본 저장', flush=True)  # 에셋 저장 결과만 보고합니다.
    A.apply_environment_models(background_only=True)  # 기존 게임 배경 프리팹에 새 건물만 연결합니다.
    print('건물 보강 게임 배경 연결 저장', flush=True)  # 캐릭터와 UI 및 진행 설정은 다시 작성하지 않습니다.


def main():  # 기존 그래픽 컨셉과 게임 동작을 유지하며 요청한 시각 에셋을 제작합니다.
    for obj in list(bpy.data.objects):  # 같은 디테일 패스를 다시 적용할 때의 대상입니다.
        if obj.get('visual_detail'):  # 이 제작 코드가 만든 실제 시각 부위만 교체합니다.
            bpy.data.objects.remove(obj, do_unlink=True)  # 원래 모델과 골격 및 배치는 삭제하지 않습니다.
    before = set(bpy.data.objects)  # 이번 요청에서 제작한 실제 부위를 구분합니다.
    prepare_materials()  # 기존 카툰 재질 안에서 마감재 팔레트를 연결합니다.
    make_terrain()  # 하나로 이어진 지형을 제작합니다.
    make_road_surfaces()  # 같은 게임 위치에서 차도와 보도도 하나로 이어 줍니다.
    make_dressing()  # 보행 구간 바깥의 식재와 거리 소품을 제작합니다.
    make_building_details()  # 원본 건물의 표면에 건축 마감을 제작합니다.
    make_neighborhood()  # 기존 상세 건물로 빈 거리와 원경을 함께 채웁니다.
    make_prop_details()  # 차량과 교통 및 전화의 제조 디테일입니다.
    make_cloth_details()  # 기존 의상과 골격을 재사용하는 봉제 마감입니다.
    make_atmosphere()  # 같은 카툰 실루엣의 구름을 제작합니다.
    authoring_sky()  # 원본에도 같은 하늘 팔레트를 저장합니다.
    town = bpy.data.objects['SoftStreetSet']  # 원본 거리 안의 이전 배경 모듈입니다.
    hidden_prefixes = ('Ground_', 'Road_', 'FarCurb_', 'NearCurb_', 'NearSidewalk_', 'FarSidewalk_')  # 새 연속 표면으로 교체된 과거 반복 부품입니다.
    for original in list(town.children):  # 기존 모듈 데이터는 재사용을 위해 보존합니다.
        if original.name in ('GrassGround', 'SoftHill', 'RoadStraight', 'SidewalkStraight', 'CurbSegment') or original.name.startswith(hidden_prefixes):  # 새 지형과 같은 자리에 있는 이전 지면과 노면입니다.
            for obj in C.descendants(original):  # 원본 편집 표시에서도 타일 경계가 겹치지 않게 합니다.
                obj.hide_viewport, obj.hide_render = True, True  # 데이터 삭제 없이 새 연속 표면을 표시합니다.
    for obj in set(bpy.data.objects) - before:  # 이번 디테일의 실제 소유 범위만 기록합니다.
        obj['visual_detail'] = True  # 다음 제작에서 기존 골격과 원래 메시를 보존합니다.
    bpy.context.view_layer.update()  # 저장할 실제 모델의 계층과 행렬을 반영합니다.
    bpy.ops.object.select_all(action='DESELECT')  # 원본을 열 때 거리 전체가 선택된 상태를 남기지 않습니다.
    bpy.context.preferences.filepaths.save_version = 0  # 실제 원본 저장에 불필요한 이전 버전 복사본을 만들지 않습니다.
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / 'SoftCrosswalk.blend'))  # 실제 모델과 재질 및 연결 지형이 담긴 Blender 원본입니다.
    print('디테일 제작 Blender 원본 저장', flush=True)  # 렌더링이나 게임 동작 확인을 주장하지 않습니다.
    A.apply_models()  # 기존 게임 위치와 진행을 유지하며 새 원본 에셋을 연결합니다.
    apply_sky_and_ui()  # 하늘과 원경 및 기존 UI에 같은 시각 컨셉을 연결합니다.


if __name__ == '__main__':  # 실제 시각 에셋 제작 요청에서만 실행하는 진입점입니다.
    if '--buildings-only' in sys.argv:  # 건물 보강만 요청한 제작 범위입니다.
        update_neighborhood()  # 기존 가로수와 소품 및 게임 배치는 다시 제작하지 않습니다.
    elif '--layout-only' in sys.argv:  # 거리 소품의 위치와 방향을 수정하는 제작 범위입니다.
        update_dressing_layout()  # 관련 없는 건물과 캐릭터 및 UI는 다시 제작하지 않습니다.
    else:  # 전체 시각 에셋 제작을 명시한 경우의 기존 진입입니다.
        main()  # 컴파일과 게임 실행 및 검증 없이 원본과 실제 에셋만 작성합니다.
