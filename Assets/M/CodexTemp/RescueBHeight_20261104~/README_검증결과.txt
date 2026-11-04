구조요원 B 원본 축소 및 씬 재적용 / 2026-11-04

원본: E:\ProjectWait\Blender\구조요원\Wait_Rescuer_B_LiftSpecialist.blend
합본: E:\ProjectWait\Blender\구조요원\Wait_Rescue_AB.blend
해당 Blender 원본의 메시와 뼈대를 함께 수정한 뒤 FBX와 미리보기를 갱신했습니다.
Before/는 작업 직전 사본, Delivery/는 검증 후 원본 폴더에 반영한 납품본입니다.

전체 키: 1.6612087700m → 1.4784758681m (89%).
머리·이목구비·머리카락·안전모: 세 축 동일하게 96%.
몸의 좌우 폭과 앞뒤 두께는 유지했습니다. 머리의 형태를 유지하면서 전체 키 비율을
맞추기 위해 몸과 팔다리의 높이 비율은 0.8611077609로 조정했습니다.
단독·합본 원본을 다시 열어 치수, 18개 뼈대, 141개 메시, 스킨 가중치와 FBX 왕복을 확인했습니다.
검증: Delivery/BHeightHead_Validation.json, Delivery/Validation_구조요원.json.

Unity: Sources/Rescuer_B.fbx를 수정 원본에서 재생성했습니다. 매니페스트의
source_blender_sha256을 실제 E 드라이브 원본 해시와 대조했습니다.
메시·프리팹·씬의 기존 GUID와 참조를 유지했습니다. 모델 Transform 배율은 1입니다.
B 동작 목표와 선택 콜라이더는 새 뼈대와 키에 맞췄습니다.
재내보내기로 정점 수가 바뀔 때 발생하던 스킨 메시 GPU 버퍼 오류를 수정했습니다.
기존 메시 자산의 정점·속성·인덱스·가중치 버퍼를 다시 구성해 저장합니다.
A 원본 합본 내 기하와 Unity A FBX·통합 메시·프리팹이 작업 전과 동일함을 확인했습니다.

검증 결과:
- PrototypePlay.json: PASS, 32개, 새 렌더링 오류·런타임 예외 0개.
- InteractionPlay.json: PASS, 32개, 새 렌더링 오류·런타임 예외 0개.
- AppliedValidation.json: PASS, 14개 (원본 해시·전체 키·머리 크기·콜라이더·단위 배율).
- SavedResult.json: PASS, 42개 (저장된 두 씬의 참조·뼈대·머티리얼·누락 스크립트).
재생 검사는 두 요원의 표시·발 접지·선택 레이·기존 시야 유지·하차·동행·1층·완료·재시작을 포함합니다.
Scene_B_Updated.png: 실제 Prototype 재생 화면, 최종 비율과 한글 대화 확인.
Unity는 Prototype 씬의 편집 상태로 복원했습니다.
물리 HMD 착용과 기기 빌드는 검증하지 않았습니다.

런타임은 이 임시 폴더에 의존하지 않습니다.
기존 Assets/XR/Settings/OpenXRPackageSettings.asset 사용자 변경은 보존했습니다.
