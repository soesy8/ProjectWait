import bpy,json
from pathlib import Path
WORK=Path(__file__).resolve().parent
S=bpy.context.scene
bpy.context.preferences.filepaths.save_version=0
S['기본 카메라']='3F · 02 · 엘리베이터 내부 / 1프레임 문 닫힘 / 현재 3층'
S['오른쪽 보조 승강기 역할']='사용하지 않는 닫힌 2호기. 현재 5층에 대기하며 5층 실내는 모델링 범위 밖.'
S['계단 규격']='층당 22단 / 단높이 0.18 m / 디딤깊이 0.28 m / 폭 2.34 m / 층간 높이 3.96 m'
S['구성']='1층 기존 로비 / 2층 빈 구조 / 3층 승강기 홀·농구장·탈의 보관 복도 / 층간 계단'
S['방향']='Z축 위 / 승강기 홀 정면 +Y / +X 계단 / -X 통창 / -Y 뒤쪽 자동문'
S.frame_set(1);S.camera=bpy.data.objects['3F · 02 · 엘리베이터 내부']
S.render.filepath='//3층_02_내부.png'
bpy.ops.wm.save_as_mainfile(filepath=str(WORK/'엘베_맵_체육센터_3층.blend'),compress=True)
report=json.loads((WORK/'Build_Validation.json').read_text(encoding='utf-8'))
report.update({'objects':len(S.objects),'directoryHasCurrentLocationHighlight':False,'defaultCamera':S.camera.name,'reviewViewsFinalized':True})
report['facilities'][0][0]='6–12F'
(WORK/'Build_Validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('FINAL_METADATA_SAVED',flush=True)
