import math  # 보폭과 발의 들림·감속 곡선을 계산합니다.
import re  # 기존 프리팹의 실제 관절 이름과 부모 관계를 읽어 같은 경로에 클립을 연결합니다.
import uuid  # 새 애니메이션 에셋에 Unity 식별자를 부여합니다.
import zlib  # Unity Transform 애니메이션 바인딩에 쓰이는 경로 CRC를 작성합니다.
from pathlib import Path  # 현재 프로젝트의 Assets 안에 있는 제작 및 출력 경로를 구합니다.
from mathutils import Vector, Quaternion, Euler, Matrix  # 원본 바인드 좌표와 보행 계산 좌표 사이의 변환을 계산합니다.

AREA = Path(__file__).resolve().parents[2]  # Source~의 위치에서 씬과 스크립트가 모인 Assets를 찾습니다.
ANIMATIONS = AREA / 'Animations'  # Animator 컨트롤러와 편집 가능한 네이티브 클립의 저장 위치입니다.
PREFAB = AREA / 'Prefabs' / 'FriendCharacter.prefab'  # 모델과 스킨을 유지할 기존 프리팹입니다.
SCENE = AREA / 'Scenes' / 'CrosswalkScene.unity'  # 친구 경로와 인스턴스 참조만 연결할 씬입니다.
HEADER = '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'  # 주석을 넣지 않는 Unity 직렬화 헤더입니다.
HIP_PATH = 'Armature/Hips'  # Animator는 VisualRoot에 붙으므로 상대 경로를 여기서 시작합니다.


def write(path, content):  # 생성한 소스 및 직렬화 에셋을 저장합니다.
    path.parent.mkdir(parents=True, exist_ok=True)  # 필요한 애니메이션 폴더만 생성합니다.
    path.write_text(content.rstrip() + '\n', encoding='utf-8')  # Unity YAML에 인라인 주석을 삽입하지 않습니다.


def meta(path, importer, main_id=0, folder=False):  # 에셋 재생성 시에도 동일한 GUID를 유지합니다.
    target = Path(str(path) + '.meta')  # 원본 파일과 짝을 이루는 메타 파일입니다.
    guid = re.search(r'^guid: (\w+)', target.read_text(encoding='utf-8'), re.M).group(1) if target.exists() else uuid.uuid4().hex  # 기존 참조를 보존합니다.
    extra = 'folderAsset: yes\n' if folder else ''  # Unity 폴더 에셋의 표시입니다.
    extra_importer = f'  mainObjectFileID: {main_id}\n' if main_id else ''  # 네이티브 에셋의 루트 오브젝트를 지정합니다.
    if importer == 'MonoImporter':  # 런타임 이동 스크립트의 표준 메타데이터입니다.
        extra_importer = '  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n'  # 별도 실행 순서를 추가하지 않습니다.
    write(target, f'fileFormatVersion: 2\nguid: {guid}\n{extra}{importer}:\n  externalObjects: {{}}\n{extra_importer}  userData: \n  assetBundleName: \n  assetBundleVariant: \n')  # Inspector 참조에 사용할 메타 파일을 저장합니다.
    return guid  # 컨트롤러와 프리팹의 참조를 연결합니다.


def vector_text(values):  # Vector3와 Quaternion의 Unity YAML 표기를 만듭니다.
    return '{' + ', '.join(f'{axis}: {value:.9g}' for axis, value in zip('xyzw', values)) + '}'  # 단위와 소수 정밀도를 유지합니다.


def smooth(value):  # 발을 떼고 내딛는 구간을 연속적으로 보간합니다.
    value = max(0.0, min(1.0, value))  # 구간 바깥에서 곡선이 역전되지 않게 합니다.
    return value * value * (3.0 - 2.0 * value)  # 양끝 속도가 0인 보간값입니다.


def rotation(axis, degrees):  # 기존 골격의 공통 기준축에서 회전을 구성합니다.
    return Quaternion(axis, math.radians(degrees))  # Blender 객체 축 대신 Unity 프리팹의 로컬 축을 사용합니다.


prefab_text = PREFAB.read_text(encoding='utf-8')  # 모델을 다시 만들지 않고 현재 프리팹 구조를 읽습니다.
blocks = re.findall(r'^--- !u!(\d+) &(\d+)\n(.*?)(?=^--- !u!|\Z)', prefab_text, re.M | re.S)  # 기존 게임 오브젝트와 Transform 구간을 구분합니다.
names, transforms = {}, {}  # 실제 이름과 부모·위치 연결을 보관합니다.
for kind, identifier, body in blocks:  # 현재 프리팹의 관절에만 애니메이션 경로를 연결합니다.
    if kind == '1':  # 게임 오브젝트에서 관절 이름을 읽습니다.
        names[int(identifier)] = re.search(r'^  m_Name: (.+)$', body, re.M).group(1)  # 임의의 새 골격 이름을 만들지 않습니다.
    if kind == '4':  # Transform에서 부모와 기본 관절 위치를 읽습니다.
        game_id = int(re.search(r'm_GameObject: \{fileID: (\d+)\}', body).group(1))  # 관절 이름에 대응하는 오브젝트입니다.
        parent_id = int(re.search(r'm_Father: \{fileID: (\d+)\}', body).group(1))  # 기존 뼈대의 부모 Transform입니다.
        values = re.search(r'm_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}', body).groups()  # 모델의 실제 신체 비율입니다.
        rotation_values = tuple(float(value) for value in re.search(r'm_LocalRotation: \{x: ([^,]+), y: ([^,]+), z: ([^,]+), w: ([^}]+)\}', body).groups())  # 원본 관절마다 다른 로컬 바인드 회전입니다.
        bind_rotation = Quaternion((rotation_values[3], *rotation_values[:3]))  # Unity 저장 순서를 실제 쿼터니언 성분으로 옮깁니다.
        transforms[int(identifier)] = {'game': game_id, 'parent': parent_id, 'position': Vector(tuple(float(value) for value in values)), 'rotation': bind_rotation}  # 원본 위치와 회전을 함께 보관합니다.
bone_transforms = {names[value['game']]: key for key, value in transforms.items() if 601000 <= value['game'] < 602000}  # 기존 스킨의 55개 관절을 재사용합니다.
bind_matrices = {}  # 원본 관절축을 유지하면서 보행 계산에 사용할 모델 기준 행렬입니다.
def model_bind(identifier):  # 관절의 실제 부모 회전을 포함하여 원본 바인드 행렬을 읽습니다.
    if identifier == 600011:  # Animator의 시각 루트는 모델 좌표의 원점입니다.
        return Matrix.Identity(4)  # 캐릭터의 게임 배치를 보행 계산에 포함하지 않습니다.
    if identifier not in bind_matrices:  # 같은 관절의 원본 행렬을 한 번만 구성합니다.
        item = transforms[identifier]  # 원본 관절의 부모와 로컬 위치 및 회전입니다.
        bind_matrices[identifier] = model_bind(item['parent']) @ Matrix.LocRotScale(item['position'], item['rotation'], Vector((1, 1, 1)))  # 원본 계층의 전체 회전을 누적합니다.
    return bind_matrices[identifier]  # 원본 관절의 실제 모델 좌표를 반환합니다.
for identifier in bone_transforms.values():  # 위치를 계산용 좌표로 바꾸기 전에 모든 원본 행렬을 확보합니다.
    model_bind(identifier)  # 원본 골격 데이터는 그대로 두고 계산용 행렬만 보관합니다.
for identifier in bone_transforms.values():  # 기존 역기구학 함수에는 모델 축 기준의 관절 차이만 전달합니다.
    item = transforms[identifier]  # 현재 원본 관절의 실제 부모입니다.
    item['position'] = model_bind(identifier).translation - model_bind(item['parent']).translation  # 원본 길이와 위치를 유지한 계산용 오프셋입니다.
BODY_RATIO = transforms[bone_transforms['Hips']]['position'].y / .74  # 현재 게임 프리팹의 골반 높이로 실제 신체 축소 비율을 읽습니다.
STRIDE = .64 * BODY_RATIO  # 새 다리 길이에 맞는 보폭으로 기존 거리 기반 걸음 진행도를 연결합니다.
STOP_DISTANCE = .192 * BODY_RATIO  # 같은 비율의 마지막 발 디딤 거리로 정지 클립과 루트 이동을 맞춥니다.


def bone_path(name):  # Animator에서 해당 뼈대까지의 실제 상대 경로를 계산합니다.
    current, chain = bone_transforms[name], []  # 대상 관절에서 부모를 따라갑니다.
    while current != 600011:  # VisualRoot 자신은 애니메이션에 포함하지 않습니다.
        chain.append(names[transforms[current]['game']])  # 부모를 포함한 이름을 순서대로 기록합니다.
        current = transforms[current]['parent']  # 한 단계 위 관절로 이동합니다.
    return '/'.join(reversed(chain))  # Unity Transform 곡선이 사용하는 경로입니다.


def rest_position(name):  # 현재 프리팹에서 관절의 바인드 위치를 공통 모델 좌표로 읽습니다.
    current, position = bone_transforms[name], Vector((0, 0, 0))  # 기존 관절 식별자와 위치 누적값입니다.
    while current != 600011:  # 시각 루트 아래의 실제 관절 위치만 더합니다.
        position += transforms[current]['position']  # 새 신체 비율에 맞게 저장된 부모 기준 위치입니다.
        current = transforms[current]['parent']  # 같은 골격의 부모 관절로 이동합니다.
    return position  # 다리와 발의 실제 기준 위치를 사용합니다.


def foot_height(pitch, lift=0.0):  # 운동화 밑창이 바닥을 관통하지 않도록 발목 높이를 계산합니다.
    angle = math.radians(pitch)  # 발목 회전의 라디안 값입니다.
    return rest_position('LeftFoot').y * math.cos(angle) + max(.1892 * math.sin(angle), -.0968 * math.sin(angle)) + .001 * BODY_RATIO + lift  # 새 발목 높이와 모델 운동화의 실제 앞뒤 밑창 길이로 접지 위치를 맞춥니다.


def walk_foot(phase):  # 지지 구간과 유각 구간을 구분한 한쪽 발의 궤적입니다.
    phase %= 1.0  # 양발의 위상 차이를 같은 주기로 정리합니다.
    if phase <= 0.6:  # 주기의 60퍼센트 동안 지지발을 월드 공간에 유지합니다.
        z = STOP_DISTANCE - STRIDE * phase  # 루트 전진과 같은 거리를 뒤로 보내 발바닥 미끄러짐을 줄입니다.
        pitch = -8.0 * (1.0 - smooth(phase / 0.1)) if phase < 0.1 else 10.0 * smooth((phase - 0.5) / 0.1)  # 뒤꿈치 접지에서 발끝 밀기까지 작은 각도로 굴립니다.
        lift = 0.0  # 지지발을 공중으로 띄우지 않습니다.
    else:  # 남은 구간에서 다음 발 디딤 위치로 다리를 옮깁니다.
        u = (phase - 0.6) / 0.4  # 발을 든 구간의 정규화 시간입니다.
        z = (2*u**3 - 3*u*u + 1) * -STOP_DISTANCE + (-2*u**3 + 3*u*u) * STOP_DISTANCE + (2*u**3 - 3*u*u + u) * -STRIDE * 0.4  # 지지 구간의 발 속도를 양끝에서 이어 주는 Hermite 궤적입니다.
        lift = .055 * BODY_RATIO * math.sin(math.pi * u) ** 2  # 짧아진 다리 길이에 맞춰 발을 드는 높이를 조절합니다.
        pitch = 10.0 * (1.0 - smooth(u / 0.5)) - 8.0 * smooth((u - 0.5) / 0.5)  # 발끝을 떼고 다음 뒤꿈치 접지를 준비합니다.
    return z, foot_height(pitch, lift), pitch  # 발목 목표 위치와 신발 방향을 반환합니다.


def solve_leg(side, hip_position, hip_rotation, target, foot_pitch):  # 고관절·무릎·발목의 실제 길이를 보존한 두 관절 역기구학입니다.
    upper = transforms[bone_transforms[side + 'UpperLeg']]['position']  # 기존 골반에서 고관절까지의 오프셋입니다.
    thigh = transforms[bone_transforms[side + 'LowerLeg']]['position']  # 기존 허벅지의 길이와 기본 무릎 방향입니다.
    shin = transforms[bone_transforms[side + 'Foot']]['position']  # 기존 종아리의 길이와 기본 발목 방향입니다.
    origin = hip_position + hip_rotation @ upper  # 흔들리는 골반 아래의 실제 고관절 위치입니다.
    delta = target - origin  # 발목으로 향하는 벡터입니다.
    length = min(delta.length, thigh.length + shin.length - 0.00001)  # 완전히 펴진 다리의 수치적 특이점을 피합니다.
    direction = delta.normalized()  # 고관절에서 발목으로 향하는 단위 벡터입니다.
    pole = Vector((0, 0, 1)) - direction * direction.z  # 무릎이 신체 앞쪽으로 구부러지도록 풀이 평면을 정합니다.
    pole.normalize()  # 무릎 굽힘 거리에 쓰일 단위 벡터입니다.
    along = (thigh.length_squared - shin.length_squared + length * length) / (2.0 * length)  # 삼각형에서 허벅지가 차지하는 전방 투영 거리입니다.
    bend = math.sqrt(max(0.0, thigh.length_squared - along * along))  # 뼈 길이를 바꾸지 않고 필요한 무릎 굽힘을 계산합니다.
    knee = origin + direction * along + pole * bend  # 실제 무릎의 목표 위치입니다.
    upper_world = thigh.rotation_difference(knee - origin)  # 허벅지의 기준 벡터를 목표 무릎 방향으로 회전합니다.
    lower_world = shin.rotation_difference(target - knee)  # 종아리의 기준 벡터를 목표 발목 방향으로 회전합니다.
    return {side + 'UpperLeg': hip_rotation.inverted() @ upper_world, side + 'LowerLeg': upper_world.inverted() @ lower_world, side + 'Foot': lower_world.inverted() @ rotation((1, 0, 0), foot_pitch)}  # 부모 관절 기준의 로컬 회전으로 변환합니다.


def body_pose(phase, mode, progress=0.0):  # 대기·걷기·마지막 한 걸음의 연속적인 전신 자세를 구성합니다.
    angle = 2.0 * math.pi * phase  # 반대쪽 팔과 다리를 반 주기 차이로 움직일 기준입니다.
    idle_height = (.738 + .0015 * math.sin(angle)) * BODY_RATIO  # 현재 골반 높이에서 기존의 작은 대기 호흡을 유지합니다.
    walk_height = (.702 - .006 * math.cos(2.0 * angle)) * BODY_RATIO  # 새 다리가 지지할 수 있는 높이로 걷기 중 골반 곡선을 맞춥니다.
    blend = smooth(progress) if mode == 'stop' else (1.0 if mode == 'idle' else 0.0)  # 마지막 한 걸음이 대기 자세로 이어지는 비율입니다.
    sway = .003 * BODY_RATIO * math.sin(angle) * (1.0 - blend)  # 새 신체 크기에서 기존 좌우 체중 이동의 비율을 유지합니다.
    height = walk_height * (1.0 - blend) + idle_height * blend  # 정지 시 다리를 모으며 자연스럽게 일어섭니다.
    hip_position = Vector((sway, height, 0))  # 캐릭터 루트는 그대로 두고 골반 관절만 미세하게 움직입니다.
    hip_rotation = Euler((0, math.radians(1.6 * math.sin(angle) * (1.0-blend)), math.radians(1.2 * math.sin(angle) * (1.0-blend))), 'XYZ').to_quaternion()  # 골반 회전을 작은 범위로 제한합니다.
    poses = {'Hips': hip_rotation, 'Spine': rotation((1, 0, 0), -1.8 * (1.0-blend) + 0.2 * math.sin(angle) * blend), 'Chest': rotation((0, 1, 0), -1.0 * math.sin(angle) * (1.0-blend)), 'UpperChest': rotation((1, 0, 0), 0.3 * math.sin(angle) * blend), 'Neck': rotation((1, 0, 0), 0.8 * (1.0-blend)), 'Head': rotation((0, 1, 0), 0.5 * math.sin(angle) * (1.0-blend))}  # 상체와 머리가 발걸음에 지나치게 흔들리지 않게 합니다.
    for side, sign, offset in [('Left', -1, 0.0), ('Right', 1, 0.5)]:  # 양팔과 양다리를 반 주기 차이로 움직입니다.
        cosine = math.cos(angle + offset * 2.0 * math.pi)  # 같은 쪽 다리가 앞으로 나갈 때 팔은 뒤로 갑니다.
        swing = 11.0 * cosine * (1.0 - blend)  # 팔 흔들림을 학생의 편안한 걷기 범위로 제한합니다.
        poses[side + 'Shoulder'] = Quaternion()  # 기존 어깨 너비와 관절 위치를 유지합니다.
        poses[side + 'UpperArm'] = rotation((1, 0, 0), swing) @ rotation((0, 0, 1), -sign * 78.0)  # 수평 T포즈의 팔을 내린 뒤 앞뒤로 교차하여 흔듭니다.
        poses[side + 'LowerArm'] = rotation((0, 1, 0), -sign * (7.0 + 6.0 * max(0.0, -cosine) * (1.0-blend)))  # 앞으로 나오는 팔꿈치를 조금 더 굽힙니다.
        poses[side + 'Hand'] = rotation((0, 1, 0), sign * 2.0)  # 손이 바지 안으로 접히지 않는 편안한 방향을 유지합니다.
        if mode == 'idle':  # 대기 중에는 양발의 위치를 바꾸지 않습니다.
            z, y, pitch = 0.0, foot_height(0), 0.0  # 밑창이 같은 바닥 높이에 닿습니다.
        elif mode == 'walk':  # 루프 걷기의 발 궤적을 가져옵니다.
            z, y, pitch = walk_foot(phase + offset)  # 루트 이동과 맞는 지지·유각 구간을 적용합니다.
        else:  # 왼발을 지지하고 오른발을 모으며 정확히 정지합니다.
            root_distance = STOP_DISTANCE * (2.0 * progress - progress * progress)  # C#의 감속 이동과 같은 거리 함수입니다.
            pitch = -8.0 * (1.0 - smooth(progress / 0.3)) if side == 'Left' else 5.0 * math.sin(2.0 * math.pi * progress) * (1.0-progress)  # 마지막 지지발의 뒤꿈치를 내리고 반대 발을 옮깁니다.
            z = STOP_DISTANCE - root_distance if side == 'Left' else (-.128 + .32 * smooth(progress)) * BODY_RATIO - root_distance  # 새 보폭에서 왼발은 고정하고 오른발을 같은 착지선으로 모읍니다.
            y = foot_height(pitch, .05 * BODY_RATIO * math.sin(math.pi * progress) ** 2 if side == 'Right' else 0.0)  # 마지막 발 들기도 현재 다리 길이와 밑창에 맞춥니다.
        poses.update(solve_leg(side, hip_position, hip_rotation, Vector((rest_position(side + 'UpperLeg').x, y, z)), pitch))  # 프리팹의 실제 고관절 너비와 다리 길이를 사용해 기존 관절 곡선을 만듭니다.
    return hip_position, poses  # 루트 전진 없이 스킨에 적용할 관절 자세만 반환합니다.


def curve_text(times, values, duration, loop, scalar=False):  # Unity의 네이티브 키프레임 곡선을 작성합니다.
    width = 1 if scalar else len(values[0])  # 스칼라 편집 곡선과 벡터·쿼터니언 런타임 곡선을 구분합니다.
    data = [[value] if scalar else list(value) for value in values]  # 같은 미분 계산으로 처리할 성분 배열입니다.
    lines = ['serializedVersion: 2', 'm_Curve:']  # 표준 AnimationCurve의 시작입니다.
    for index, (time, value) in enumerate(zip(times, data)):  # 모든 프레임의 위치·회전과 접선을 기록합니다.
        before, after = max(0, index-1), min(len(times)-1, index+1)  # 가운데 차분 또는 끝점 차분의 이웃입니다.
        if loop and index in (0, len(times)-1):  # 루프 경계에서도 속도가 끊기지 않게 합니다.
            slope = [(data[1][axis] - data[-2][axis]) / (times[1] + duration - times[-2]) for axis in range(width)]  # 마지막 구간과 첫 구간의 미분을 연결합니다.
        else:  # 루프 내부 또는 한 번 재생하는 정지 클립의 접선입니다.
            slope = [(data[after][axis] - data[before][axis]) / (times[after]-times[before]) for axis in range(width)]  # 이웃 자세 사이의 부드러운 보간 속도입니다.
        format_value = (lambda item: f'{item[0]:.9g}') if scalar else vector_text  # 각 곡선 형식의 값 표기입니다.
        lines.extend(['- serializedVersion: 3', f'  time: {time:.9g}', f'  value: {format_value(value)}', f'  inSlope: {format_value(slope)}', f'  outSlope: {format_value(slope)}', '  tangentMode: 0', '  weightedMode: 0', f'  inWeight: {format_value([1/3]*width)}', f'  outWeight: {format_value([1/3]*width)}'])  # Animator가 평가할 실제 키프레임입니다.
    lines.extend(['m_PreInfinity: 2', 'm_PostInfinity: 2', 'm_RotationOrder: 4'])  # 클립 루프 설정과 독립적인 곡선 외삽 설정입니다.
    return '\n'.join(lines)  # 들여쓰기는 곡선이 들어갈 위치에서 적용합니다.


def indent(text, count):  # YAML 중첩 위치에 맞춰 공백을 붙입니다.
    return '\n'.join(' ' * count + line for line in text.splitlines())  # 직렬화 본문에 설명 주석을 섞지 않습니다.


def make_clip(name, mode, duration, samples, loop):  # 기존 관절 경로에 직접 연결되는 Animator용 비레거시 클립을 제작합니다.
    times = [duration * i / samples for i in range(samples+1)]  # 마지막 루프 접점과 정지 끝점도 포함합니다.
    frames = [body_pose(0.0 if mode == 'stop' else i/samples, mode, i/samples) for i in range(samples+1)]  # 정지 클립은 걷기 위상 0에서 마지막 발을 모읍니다.
    position_values = [tuple(frame[0]) for frame in frames]  # 골반의 작은 높이·체중 이동 곡선입니다.
    rotations = {}  # 몸통과 양팔·양다리의 회전 곡선입니다.
    for bone in frames[0][1]:  # 실제로 움직이는 관절만 키프레임을 작성합니다.
        values = []  # 해당 관절의 쿼터니언 키프레임입니다.
        for frame in frames:  # 각 시점의 관절 자세를 변환합니다.
            identifier = bone_transforms[bone]  # 현재 동작이 연결될 원본 관절입니다.
            parent_bind = model_bind(transforms[identifier]['parent']).to_quaternion()  # 원본 부모 관절의 실제 모델 기준 회전입니다.
            bone_bind = model_bind(identifier).to_quaternion()  # 원본 관절의 실제 바인드 회전입니다.
            q = (parent_bind.inverted() @ frame[1][bone] @ bone_bind).normalized()  # 보행 회전만 원본 관절축으로 변환하여 골격과 스킨이 어긋나지 않게 합니다.
            values.append((q.x, q.y, q.z, q.w))  # Unity는 쿼터니언을 xyzw 순서로 저장합니다.
            if len(values) > 1 and sum(a*b for a, b in zip(values[-2], values[-1])) < 0:  # 같은 회전이 반대 부호로 표현되는 경계입니다.
                values[-1] = tuple(-value for value in values[-1])  # 긴 방향으로 보간하여 관절이 뒤집히는 것을 방지합니다.
        rotations[bone_path(bone)] = values  # 실제 프리팹의 부모 관계를 유지합니다.
    text = HEADER + f'--- !u!74 &7400000\nAnimationClip:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_Name: {name}\n  serializedVersion: 7\n  m_Legacy: 0\n  m_Compressed: 0\n  m_UseHighQualityCurve: 1\n  m_RotationCurves:\n'  # Animator에서 평가하는 네이티브 AnimationClip입니다.
    for path, values in rotations.items():  # 각 골격 회전을 쿼터니언 곡선으로 저장합니다.
        text += '  - curve:\n' + indent(curve_text(times, values, duration, loop), 6) + f'\n    path: {path}\n'  # Transform 전체 회전을 한 번에 바인딩합니다.
    text += '  m_CompressedRotationCurves: []\n  m_EulerCurves: []\n  m_PositionCurves:\n  - curve:\n' + indent(curve_text(times, position_values, duration, loop), 6) + f'\n    path: {HIP_PATH}\n  m_ScaleCurves: []\n  m_FloatCurves: []\n  m_PPtrCurves: []\n  m_SampleRate: 60\n  m_WrapMode: 0\n  m_Bounds:\n    m_Center: {{x: 0, y: 0.73, z: 0}}\n    m_Extent: {{x: 0.85, y: 0.8, z: 0.5}}\n  m_ClipBindingConstant:\n    genericBindings:\n'  # VisualRoot와 캐릭터 루트에는 이동 곡선을 넣지 않습니다.
    bindings = [(HIP_PATH, 1)] + [(path, 2) for path in rotations]  # 위치는 속성 1, 회전은 속성 2에 해당합니다.
    for path, attribute in bindings:  # 런타임에 사용할 Transform 바인딩을 작성합니다.
        text += f'    - serializedVersion: 2\n      path: {zlib.crc32(path.encode("utf-8")) & 0xffffffff}\n      attribute: {attribute}\n      script: {{fileID: 0}}\n      typeID: 4\n      customType: 0\n      isPPtrCurve: 0\n      isIntCurve: 0\n      isSerializeReferenceCurve: 0\n'  # 네이티브 클립의 실제 경로 해시를 연결합니다.
    text += f'    pptrCurveMapping: []\n  m_AnimationClipSettings:\n    serializedVersion: 2\n    m_AdditiveReferencePoseClip: {{fileID: 0}}\n    m_AdditiveReferencePoseTime: 0\n    m_StartTime: 0\n    m_StopTime: {duration:.9g}\n    m_OrientationOffsetY: 0\n    m_Level: 0\n    m_CycleOffset: 0\n    m_HasAdditiveReferencePose: 0\n    m_LoopTime: {int(loop)}\n    m_LoopBlend: 0\n    m_LoopBlendOrientation: 0\n    m_LoopBlendPositionY: 0\n    m_LoopBlendPositionXZ: 0\n    m_KeepOriginalOrientation: 1\n    m_KeepOriginalPositionY: 1\n    m_KeepOriginalPositionXZ: 1\n    m_HeightFromFeet: 0\n    m_Mirror: 0\n  m_EditorCurves:\n'  # 대기와 걷기는 반복하고 마지막 발 디딤은 한 번만 재생합니다.
    editor_curves = [(HIP_PATH, 'm_LocalPosition', position_values)] + [(path, 'm_LocalRotation', values) for path, values in rotations.items()]  # Unity의 Animation 창에서도 편집할 수 있는 성분 곡선입니다.
    for path, attribute, values in editor_curves:  # 편집용 곡선은 런타임 곡선과 동일한 자세를 기록합니다.
        for index, axis in enumerate('xyzw'[:len(values[0])]):  # 위치 3성분과 회전 4성분을 분리합니다.
            text += '  - serializedVersion: 2\n    curve:\n' + indent(curve_text(times, [value[index] for value in values], duration, loop, scalar=True), 6) + f'\n    attribute: {attribute}.{axis}\n    path: {path}\n    classID: 4\n    script: {{fileID: 0}}\n    flags: 16\n'  # 원본 클립을 Unity에서 수정할 수 있게 합니다.
    text += '  m_EulerEditorCurves: []\n  m_HasGenericRootTransform: 0\n  m_HasMotionFloatCurves: 0\n  m_Events: []\n'  # 애니메이션 이벤트나 추가 시스템을 연결하지 않습니다.
    path = ANIMATIONS / (name + '.anim')  # 각 상태의 재사용 가능한 클립 경로입니다.
    write(path, text)  # 기존 외형·스킨 메시를 변경하지 않고 클립만 생성합니다.
    return meta(path, 'NativeFormatImporter', 7400000)  # Animator 상태의 모션 참조에 사용합니다.


ANIMATIONS.mkdir(parents=True, exist_ok=True)  # 애니메이션을 개인 작업 폴더 안에 모읍니다.
meta(ANIMATIONS, 'DefaultImporter', folder=True)  # Unity 폴더 식별자를 만듭니다.
idle_guid = make_clip('FriendIdle', 'idle', 2.4, 32, True)  # 제자리 호흡과 고정된 발을 가진 대기 클립입니다.
walk_guid = make_clip('FriendWalk', 'walk', 0.8, 64, True)  # 기본 0.8m/s에 맞춘 0.64m 보폭의 전신 걷기입니다.
stop_guid = make_clip('FriendStop', 'stop', 0.48, 40, False)  # 마지막 19.2cm를 감속하며 발을 모으는 정지 클립입니다.
controller = ANIMATIONS / 'FriendLocomotion.controller'  # 친구 걷기 전용 컨트롤러입니다.
controller_guid = meta(controller, 'NativeFormatImporter', 9100000)  # Animator 참조를 먼저 확보합니다.
text = HEADER + '--- !u!91 &9100000\nAnimatorController:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_Name: FriendLocomotion\n  serializedVersion: 5\n  m_AnimatorParameters:\n'  # 기존 뼈대의 Transform 곡선을 Animator로 재생합니다.
for name in ('걸음 진행도', '정지 진행도'):  # 실제 거리와 연동하는 두 정규화 시간 파라미터입니다.
    text += f'  - m_Name: {name}\n    m_Type: 1\n    m_DefaultFloat: 0\n    m_DefaultInt: 0\n    m_DefaultBool: 0\n    m_Controller: {{fileID: 9100000}}\n'  # Float 파라미터로 저장합니다.
text += '  m_AnimatorLayers:\n  - serializedVersion: 5\n    m_Name: Base Layer\n    m_StateMachine: {fileID: 11070000}\n    m_Mask: {fileID: 0}\n    m_Motions: []\n    m_Behaviours: []\n    m_BlendingMode: 0\n    m_SyncedLayerIndex: -1\n    m_DefaultWeight: 1\n    m_IKPass: 0\n    m_SyncedLayerAffectsTiming: 0\n    m_Controller: {fileID: 9100000}\n--- !u!1107 &11070000\nAnimatorStateMachine:\n  serializedVersion: 6\n  m_ObjectHideFlags: 1\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_Name: Base Layer\n  m_ChildStates:\n'  # 단일 레이어에 대기·걷기·정지만 둡니다.
for index in range(3):  # 상태 그래프를 보기 쉬운 간격으로 배치합니다.
    text += f'  - serializedVersion: 1\n    m_State: {{fileID: {11020000+index}}}\n    m_Position: {{x: {240+index*240}, y: 100, z: 0}}\n'  # 상태 선택은 이동 스크립트에서 한 번씩 수행합니다.
text += '  m_ChildStateMachines: []\n  m_AnyStateTransitions: []\n  m_EntryTransitions: []\n  m_StateMachineTransitions: {}\n  m_StateMachineBehaviours: []\n  m_AnyStatePosition: {x: 40, y: 20, z: 0}\n  m_EntryPosition: {x: 40, y: 100, z: 0}\n  m_ExitPosition: {x: 980, y: 100, z: 0}\n  m_ParentStateMachinePosition: {x: 980, y: 20, z: 0}\n  m_DefaultState: {fileID: 11020000}\n'  # 씬 시작 시 기본 상태는 대기입니다.
for index, (name, guid, parameter) in enumerate([('Idle', idle_guid, ''), ('Walk', walk_guid, '걸음 진행도'), ('Stop', stop_guid, '정지 진행도')]):  # 이동 상태와 시각적 상태를 일대일로 연결합니다.
    text += f'--- !u!1102 &{11020000+index}\nAnimatorState:\n  serializedVersion: 6\n  m_ObjectHideFlags: 1\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_Name: {name}\n  m_Speed: 1\n  m_CycleOffset: 0\n  m_Transitions: []\n  m_StateMachineBehaviours: []\n  m_Position: {{x: 50, y: 50, z: 0}}\n  m_IKOnFeet: 0\n  m_WriteDefaultValues: 0\n  m_Mirror: 0\n  m_SpeedParameterActive: 0\n  m_MirrorParameterActive: 0\n  m_CycleOffsetParameterActive: 0\n  m_TimeParameterActive: {int(bool(parameter))}\n  m_Motion: {{fileID: 7400000, guid: {guid}, type: 2}}\n  m_Tag: \n  m_SpeedParameter: \n  m_MirrorParameter: \n  m_CycleOffsetParameter: \n  m_TimeParameter: {parameter}\n'  # 걷기와 정지의 재생 시간은 스크립트가 실제 이동과 맞춥니다.
write(controller, text)  # 비레거시 Animator 컨트롤러를 저장합니다.
movement_guid = meta(AREA / 'Scripts' / 'Characters' / 'FriendCharacterMovement.cs', 'MonoImporter')  # 캐릭터 스크립트 폴더에서 기존 이동 컴포넌트의 프리팹 참조를 이어받습니다.
prefab_text = re.sub(r'^--- !u!(?:95 &9560000|114 &114600000)\n.*?(?=^--- !u!|\Z)', '', prefab_text, flags=re.M | re.S)  # 다시 제작할 때 이 도구가 소유한 컴포넌트 본문만 교체합니다.
prefab_text = prefab_text.replace('  - component: {fileID: 114600000}\n', '').replace('  - component: {fileID: 9560000}\n', '')  # 재제작으로 컴포넌트 참조가 중복되지 않게 합니다.
prefab_text = prefab_text.replace('  - component: {fileID: 600001}\n', '  - component: {fileID: 600001}\n  - component: {fileID: 114600000}\n', 1)  # 이동 루트에 독립적인 스크립트만 추가합니다.
prefab_text = prefab_text.replace('  - component: {fileID: 600011}\n', '  - component: {fileID: 600011}\n  - component: {fileID: 9560000}\n', 1)  # 시각 모델 쪽에 Animator를 분리합니다.
prefab_text = prefab_text.replace('m_Extent: {x: 0.85, y: 0.78, z: 0.35}', 'm_Extent: {x: 0.85, y: 0.8, z: 0.5}')  # 앞으로 내딛는 운동화가 표시 경계를 벗어나지 않게 합니다.
prefab_text += f'--- !u!95 &9560000\nAnimator:\n  serializedVersion: 7\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: 600010}}\n  m_Enabled: 1\n  m_Avatar: {{fileID: 0}}\n  m_Controller: {{fileID: 9100000, guid: {controller_guid}, type: 2}}\n  m_CullingMode: 0\n  m_UpdateMode: 0\n  m_ApplyRootMotion: 0\n  m_LinearVelocityBlending: 0\n  m_StabilizeFeet: 0\n  m_AnimatePhysics: 0\n  m_WarningMessage: \n  m_HasTransformHierarchy: 1\n  m_AllowConstantClipSamplingOptimization: 1\n  m_KeepAnimatorStateOnDisable: 0\n  m_WriteDefaultValuesOnDisable: 0\n--- !u!114 &114600000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: 600000}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {{fileID: 11500000, guid: {movement_guid}, type: 3}}\n  m_Name: \n  m_EditorClassIdentifier: Assembly-CSharp::Y.Crosswalk.FriendCharacterMovement\n  m_StartPoint: {{fileID: 0}}\n  m_EndPoint: {{fileID: 0}}\n  m_Speed: 0.8\n  m_StartDelay: 2\n  m_Acceleration: 1.6\n  m_TurnSpeed: 90\n  m_Animator: {{fileID: 9560000}}\n  m_FadeTime: 0.22\n'  # 기존 모델의 관절을 그대로 Animator로 움직이며 Avatar를 다시 생성하지 않습니다.
write(PREFAB, prefab_text)  # 기존 루트 ID와 GUID 및 스킨 참조를 유지합니다.
scene_text = SCENE.read_text(encoding='utf-8')  # 친구 인스턴스 외의 기존 씬 데이터는 유지합니다.
scene_text = re.sub(r'^--- !u![14] &700000(?:00|01|10|11|20|21)\n.*?(?=^--- !u!|\Z)', '', scene_text, flags=re.M | re.S)  # 재제작 시 친구 경로 지점만 교체합니다.
scene_text = scene_text.replace('  - {fileID: 70000001}\n', '')  # 씬 루트 목록의 경로 참조가 중복되지 않게 합니다.
start, end = Vector((0.0, 0.15, 6.25)), Vector((0.0, 0.15, 4.25))  # 건너편 인도 안에서 플레이어 정면의 횡단보도 진입 직전까지 접근하는 경로입니다.
heading = math.atan2((end-start).x, (end-start).z)  # 모델 전면 +Z를 실제 진행 방향에 맞춥니다.
friend_start = scene_text.index('--- !u!1001 &60000000\n')  # 기존 친구 프리팹 인스턴스 구간입니다.
friend_end = scene_text.index('--- !u!1660057539', friend_start)  # 씬 루트 목록 직전까지가 기존 친구 설정입니다.
instance = scene_text[friend_start:friend_end]  # 기존 인스턴스와 프리팹 연결을 보존합니다.
instance = re.sub(r'    - target: \{fileID: 114600000, guid: 4750afde6b91062dd6d8d4ca25cba07d, type: 3\}\n      propertyPath: m_(?:Start|End)Point\n      value: [^\n]*\n      objectReference: \{fileID: \d+\}\n', '', instance)  # 이전 제작의 경로 참조만 현재 지점으로 교체합니다.
for property_name, value in [('m_LocalPosition.x', start.x), ('m_LocalPosition.y', start.y), ('m_LocalPosition.z', start.z), ('m_LocalRotation.y', math.sin(heading/2)), ('m_LocalRotation.w', math.cos(heading/2)), ('m_LocalEulerAnglesHint.y', math.degrees(heading))]:  # 친구 루트의 출발 배치만 변경합니다.
    instance = re.sub(r'(propertyPath: ' + re.escape(property_name) + r'\n      value: )[^\n]+', lambda match: match.group(1) + f'{value:.9g}', instance)  # XR·신호등·차량 인스턴스 구간은 접근하지 않습니다.
overrides = ''  # 씬의 고정 경로 지점을 프리팹 이동 컴포넌트에 연결합니다.
for field, identifier in [('m_StartPoint', 70000011), ('m_EndPoint', 70000021)]:  # 출발점과 도착점은 프리팹 밖에 둡니다.
    overrides += f'    - target: {{fileID: 114600000, guid: 4750afde6b91062dd6d8d4ca25cba07d, type: 3}}\n      propertyPath: {field}\n      value: \n      objectReference: {{fileID: {identifier}}}\n'  # 인스턴스 오버라이드로 씬의 경로를 지정합니다.
instance = instance.replace('    m_RemovedComponents:', overrides + '    m_RemovedComponents:', 1)  # 기존 프리팹 인스턴스에 필요한 참조만 추가합니다.


def marker(identifier, name, position, parent, children=(), yaw=0.0):  # Scene 뷰에서 이동 경로를 조정할 빈 오브젝트를 만듭니다.
    q = rotation((0, 1, 0), yaw)  # 도착 지점의 파란 축으로 최종 대기 방향을 표시합니다.
    text = f'--- !u!1 &{identifier}\nGameObject:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  serializedVersion: 6\n  m_Component:\n  - component: {{fileID: {identifier+1}}}\n  m_Layer: 0\n  m_Name: {name}\n  m_TagString: Untagged\n  m_Icon: {{fileID: 0}}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1\n--- !u!4 &{identifier+1}\nTransform:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: {identifier}}}\n  serializedVersion: 2\n  m_LocalRotation: {vector_text((q.x, q.y, q.z, q.w))}\n  m_LocalPosition: {vector_text(position)}\n  m_LocalScale: {{x: 1, y: 1, z: 1}}\n  m_ConstrainProportionsScale: 0\n'  # 모델이나 표시 UI는 추가하지 않습니다.
    text += '  m_Children:\n' + ''.join(f'  - {{fileID: {child}}}\n' for child in children) if children else '  m_Children: []\n'  # 경로 오브젝트를 한 부모 아래 정리합니다.
    return text + f'  m_Father: {{fileID: {parent}}}\n  m_LocalEulerAnglesHint: {{x: 0, y: {yaw:.9g}, z: 0}}\n'  # 사람이 직접 Scene 뷰에서 조절할 Transform입니다.


markers = marker(70000000, 'FriendWalkPath', (0, 0, 0), 0, (70000011, 70000021))  # 친구 전용 이동 경로를 기존 환경과 분리합니다.
markers += marker(70000010, 'FriendStartPoint', start, 70000001, yaw=math.degrees(heading))  # 건너편 인도 안쪽에서 플레이어 방향을 바라보는 출발 지점입니다.
markers += marker(70000020, 'FriendArrivalPoint', end, 70000001, yaw=180.0)  # 도로에 진입하지 않고 플레이어 쪽인 -Z 방향을 바라보며 멈추는 도착점입니다.
scene_text = scene_text[:friend_start] + instance + markers + scene_text[friend_end:]  # 친구 배치와 새 경로만 씬에 반영합니다.
scene_text = scene_text.rstrip() + '\n  - {fileID: 70000001}\n'  # 기존 SceneRoots 목록에 경로 부모 Transform을 추가합니다.
write(SCENE, scene_text)  # 신호등·자동차·환경·XR 설정을 변경하지 않고 씬을 저장합니다.
print('친구 걷기·대기·정지 클립, Animator, 프리팹 연결과 인도 위 경로를 저장했습니다.')  # 에셋 제작만 보고하고 실행 검증 결과로 표현하지 않습니다.
