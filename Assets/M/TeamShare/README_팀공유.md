# Wait 팀 공유 패키지

## 가져오기

- 제작 기준: **Unity 6000.3.18f1 / Universal RP 17.3.0**.
- 팀 프로젝트를 백업하거나 버전 관리에 저장한 뒤 `Assets > Import Package > Custom Package...`에서 가져옵니다.
- 전체가 필요하면 `Wait_TeamShare_All.unitypackage` **하나만** 가져옵니다.
- 개별 사용: `Wait_Highlight`, `Wait_Friend`, `Wait_Rescuers`. 전체판과 개별판은 같은 자산/GUID를 공유하므로 중복 가져오기가 필요하지 않습니다.
- Package Manager에서 Universal RP 17.3.0을 먼저 설치하세요. Built-in/HDRP 및 구버전 Unity 호환을 보증하지 않습니다.
- 프로젝트 설정, 전체 체험 씬, XR 리그, 입력 설정은 포함하지 않으며 자동으로 덮어쓰지 않습니다. 모델과 강조 효과 자체에는 XR/Input System 패키지가 필요하지 않습니다.
- 기존 프로젝트와 같은 GUID/경로의 자산이 있으면 Import가 해당 자산을 업데이트합니다. 특히 `Assets/Shared Assets/Highlight/`와 `Assets/M/Scripts/Elevator/ElevatorOptimizedOutlineFeature.cs`의 기존 팀 수정 사항을 먼저 비교하세요.

## 포함 내용

| 패키지 | 내용 |
|---|---|
| `Wait_Highlight.unitypackage` | 실루엣 외곽선/후광 렌더러, 두 셰이더, 대상 컴포넌트, 씬 전용 파이프라인 전환 컴포넌트, 독립 예제 드라이버, 예제 Renderer/Pipeline 자산 |
| `Wait_Friend.unitypackage` | 독립형 친구 프리팹, FBX, 팔레트 텍스처/머티리얼, 카툰 셰이더, Idle 애니메이션/Controller, 독립형 표정 컴포넌트 |
| `Wait_Rescuers.unitypackage` | 소방 구조요원 A / 승강기 전문요원 B 프리팹, 최적화 스킨 메시, 공용 팔레트 머티리얼/셰이더 |
| `Wait_TeamShare_All.unitypackage` | 위 세 패키지의 중복 없는 합집합 |

## 친구 캐릭터

- 시작 프리팹: `Assets/M/TeamShare/Prefabs/Wait_Friend_Shared.prefab`.
- 원본 모델: `Assets/M/Arts/Models/FriendImproved/Wait_Friend_Improved.fbx`.
- 스킨 본 22개, 표정 BlendShape: `Blink`, `Smile`, `Surprised`, `Worried`, `Sad`.
- `SharedFriendExpressions` Inspector의 Expression/Strength/Automatic Blink로 조절합니다. 외부 코드에서는 `SetExpression(...)`을 호출할 수 있습니다.
- 원래 친구 프리팹의 표정 스크립트는 `ElevatorLesson`에 직접 의존하므로, **공유용 프리팹에만 독립 표정 컴포넌트로 교체**했습니다. 원래 씬/프리팹/스크립트는 수정하지 않습니다.
- 포함된 Animator는 Generic 계열 원본 리그의 Idle용입니다. Humanoid 리타게팅, 걷기·구조 연출·점프 시나리오 전체는 포함하지 않습니다.

## 구조요원 캐릭터

- `Assets/M/Arts/Models/Rescuers/Rescuer_A_Firefighter.prefab`
- `Assets/M/Arts/Models/Rescuers/Rescuer_B_LiftSpecialist.prefab`
- A는 스킨 본 24개, B는 26개입니다. 프리팹의 본 계층과 최적화 `RuntimeSkin.asset`을 함께 사용합니다.
- 두 캐릭터는 현재 프리팹 기준 Animator/표정 BlendShape/독립 애니메이션 Clip이 없습니다. 리그 포즈·이동 연출은 가져온 프로젝트에서 연결해야 합니다.
- 정점 색과 추가 UV 채널에 팔레트/셀 명암/외곽선 정보가 들어 있습니다. 메시를 재가공할 때 이 채널들을 제거하지 마세요. 별도 텍스처 누락이 아닙니다.
- 현재 작업 폴더에서 이미 제거된 구조요원 FBX·Blender 원본은 복구하거나 패키지에 넣지 않았습니다. 구조요원은 Unity 런타임 프리팹/메시 형식으로 제공합니다.

## 강조 효과 연결

### 기존 URP Renderer에 추가 (팀 프로젝트 권장)

1. 카메라가 사용하는 Universal Renderer Data를 선택하고 `Add Renderer Feature`에서 `ElevatorOptimizedOutlineFeature`를 추가합니다.
2. Mask Shader에 `ProjectWait/M/Outline Coverage`, Distance Shader에 `ProjectWait/M/Outline Distance`를 지정합니다. 명시적 참조는 빌드 셰이더 스트리핑 방지에도 필요합니다.
3. URP Asset에서 **Depth Texture**를 켜세요. **Render Graph 활성**, Compatibility Mode 비활성 상태가 필요합니다.
4. 일반 Game 카메라의 **Base** 렌더러에서 사용합니다. Scene View/Overlay 카메라 전용 렌더링은 구현 대상이 아닙니다.
5. 강조할 루트에 `ElevatorOutlineTarget`과 `SharedHighlightExample`을 붙입니다. 예제는 자식 MeshRenderer/SkinnedMeshRenderer를 자동 수집하고 `visible / hovered / inRange` 상태를 전달합니다. Play Mode에서 Inspector 값으로 확인합니다.
6. 실제 상호작용에서는 예제 드라이버를 제거/비활성화하고 프로젝트 코드가 대상의 `SetPresentation(visible, hovered, inRange)`와 `Flash()`를 호출하도록 연결합니다. 두 드라이버가 동시에 상태를 덮어쓰지 않게 하세요.

### 독립 확인용 파이프라인

- `Assets/M/TeamShare/Rendering/WaitHighlightRenderer.asset`
- `Assets/M/TeamShare/Rendering/WaitHighlightPipeline.asset`
- 기존 프로젝트 전역 설정을 바꾸지 않고 시험하려면 빈 GameObject에 `ElevatorSceneRendering`을 붙이고 Scene Pipeline에 위 Pipeline을 지정합니다. Play 중 Quality 파이프라인을 임시 전환하고 비활성화 시 이전 값을 복원합니다. 이 컴포넌트는 런타임 전역 Quality 설정에 영향을 주므로 실제 씬에서는 팀의 파이프라인 정책을 우선하세요.
- 기본은 주황 강조, 범위 내 청록/하늘색 전환, hover 시 더 두꺼운 외곽선과 후광입니다. 거리/포인터 판정 자체는 포함하지 않으며 팀 상호작용 코드가 결정합니다.
- 투명 재질의 알파 형태를 자동 추적하지 않습니다. 일반 불투명 메시 또는 직접 지정한 `mesh + anchor` 마스크를 사용하세요.
- XR 스테레오 코드 경로는 보존되어 있으나 실제 HMD/모바일 기기 검증과 성능 측정은 별도입니다.

## 검증 자료

- `ExportManifest.json`: 실제 포함 경로/GUID/원본 파일 해시, 외부 UPM 의존성 목록.
- `PackageVerification.json`: 실제 unitypackage 압축 구조·파일 해시·GUID 및 참조 검사.
- `ImportAcceptance.json`: 별도 빈 URP 프로젝트에서의 컴파일·프리팹·표정·강조 상태 검사 결과.
- `SHA256SUMS.txt`: 전달 파일 무결성 확인용 SHA-256.

검증 범위와 미검증 사항은 실제 결과 JSON을 기준으로 합니다. 별도 프로젝트 검사 전에는 이 문서만으로 통과를 주장하지 않습니다.
