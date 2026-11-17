using System.Collections.Generic; // 기존 카툰 모델의 윤곽선 패스 이름만 렌더링 목록에 지정합니다.
using UnityEngine; // 게임 카메라와 씬 카메라의 종류를 구분합니다.
using UnityEngine.Rendering; // 기존 카메라의 컬링 결과와 불투명 모델 목록을 사용합니다.
using UnityEngine.Rendering.RenderGraphModule; // Unity 6.3의 Render Graph에서 깊이 의존성을 명시합니다.
using UnityEngine.Rendering.Universal; // 표면 깊이가 준비된 뒤 모델의 외곽선만 추가로 그립니다.

namespace Y.Crosswalk // 기존 횡단보도 모델의 표면 표시와 외곽선을 연결합니다.
{ // 게임 오브젝트와 상황 진행 코드는 변경하지 않습니다.
    public sealed class CrosswalkModelOutlineFeature : ScriptableRendererFeature // 일반 표면과 구분된 시점에 카툰 모델의 외곽선을 그립니다.
    { // 원래 표면 깊이를 읽어 지붕 이음새와 창틀에 번지는 선을 제외합니다.
        private ModelOutlinePass m_Pass; // 이 렌더러에서 사용할 외곽선 패스 하나를 보관합니다.
        public override void Create() // 렌더러가 준비되거나 설정을 다시 읽을 때 패스를 구성합니다.
        { // 추가 재질과 화면 전체 색상 효과는 생성하지 않습니다.
            m_Pass = new ModelOutlinePass(); // 기존 모델 재질의 전용 외곽선 패스를 사용합니다.
        } // 실제 그리기는 카메라별 Render Graph가 담당합니다.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) // 기존 카메라 렌더링에 깊이를 읽는 외곽선 단계를 연결합니다.
        { // 미리보기와 반사 카메라에 불필요한 화면 효과를 추가하지 않습니다.
            CameraType type = renderingData.cameraData.cameraType; // 이번 렌더링의 실제 카메라 종류를 읽습니다.
            if ((type != CameraType.Game && type != CameraType.SceneView) || renderingData.cameraData.renderType != CameraRenderType.Base) return; // 게임과 씬의 기본 카메라에만 한 번 그립니다.
            renderer.EnqueuePass(m_Pass); // 표면 렌더링과 깊이 복사가 끝난 뒤 외곽선을 실행합니다.
        } // 원래 셰이더의 색상 패스는 기존 불투명 렌더링을 사용합니다.
        private sealed class ModelOutlinePass : ScriptableRenderPass // 불투명 모델의 최종 깊이에 맞춰 실루엣을 표시합니다.
        { // 별도의 조명이나 팔레트를 덮어쓰지 않습니다.
            private readonly List<ShaderTagId> m_Tags = new List<ShaderTagId> { new ShaderTagId("CrosswalkModelOutline") }; // 수정한 카툰 셰이더의 외곽선만 선택합니다.
            private sealed class PassData // Render Graph 실행에 필요한 그리기 목록입니다.
            { // 카메라별 컬링과 재질 선택 결과를 전달합니다.
                public RendererListHandle Renderers; // 기존 재질과 원래 메시를 그대로 사용하는 모델 목록입니다.
            } // 화면 전체를 덮는 합성 재질은 사용하지 않습니다.
            public ModelOutlinePass() // 외곽선의 실행 시점과 깊이 입력을 함께 지정합니다.
            { // 불투명 표면이 모두 기록된 뒤에만 내부 선을 판단합니다.
                renderPassEvent = RenderPassEvent.BeforeRenderingTransparents; // 표면과 그림자 렌더링 뒤이며 UI와 투명 오브젝트 앞에 그립니다.
                ConfigureInput(ScriptableRenderPassInput.Depth); // 외곽선 셰이더가 읽을 실제 장면 깊이를 URP에 요청합니다.
            } // 아직 준비되지 않은 깊이로 선을 출력하지 않습니다.
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) // 카메라의 실제 표면과 깊이 자원을 연결합니다.
            { // Unity 6.3의 렌더링 자원 수명과 읽기·쓰기 관계를 따릅니다.
                UniversalResourceData resources = frameData.Get<UniversalResourceData>(); // 현재 카메라의 색상과 깊이 텍스처를 읽습니다.
                if (!resources.cameraDepthTexture.IsValid()) return; // 표면 깊이가 없으면 내부 선을 잘못 칠하지 않도록 그리기를 생략합니다.
                UniversalCameraData camera = frameData.Get<UniversalCameraData>(); // 현재 눈과 카메라의 불투명 정렬 방식을 사용합니다.
                UniversalRenderingData rendering = frameData.Get<UniversalRenderingData>(); // 기존 씬의 실제 컬링 결과를 재사용합니다.
                UniversalLightData lights = frameData.Get<UniversalLightData>(); // URP의 기존 그리기 설정을 만드는 데 필요한 광원 데이터를 전달합니다.
                DrawingSettings drawing = RenderingUtils.CreateDrawingSettings(m_Tags, rendering, camera, lights, camera.defaultOpaqueSortFlags); // 원래 모델 재질의 전용 외곽선 패스만 선택합니다.
                FilteringSettings filtering = new FilteringSettings(RenderQueueRange.opaque, camera.camera.cullingMask); // 현재 카메라가 보는 불투명 모델만 포함합니다.
                RendererListParams parameters = new RendererListParams(rendering.cullResults, drawing, filtering); // 기존 배치와 스킨 변형을 그대로 그리는 목록을 구성합니다.
                using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>("모델 바깥 윤곽선", out PassData data)) // 원래 화면을 유지하면서 윤곽선을 추가합니다.
                { // 색상과 깊이를 지우거나 새 색으로 초기화하지 않습니다.
                    data.Renderers = renderGraph.CreateRendererList(parameters); // 카메라 컬링 결과를 실제 그리기 목록으로 연결합니다.
                    builder.UseRendererList(data.Renderers); // 이 패스가 사용할 모델과 재질 의존성을 등록합니다.
                    builder.UseTexture(resources.cameraDepthTexture, AccessFlags.Read); // 내부 선을 제외하는 데 필요한 표면 깊이의 읽기를 명시합니다.
                    builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite); // 원래 모델 색상이 남은 화면에 외곽선 픽셀만 더합니다.
                    builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.Read); // 가려진 선은 기존 깊이로 제외하고 원래 깊이는 변경하지 않습니다.
                    builder.SetRenderFunc(static (PassData pass, RasterGraphContext context) => context.cmd.DrawRendererList(pass.Renderers)); // 기존 메시의 전용 외곽선 패스만 실제로 그립니다.
                } // 모델 표면 가까이에 붙은 내부 선은 셰이더의 깊이 비교에서 제외합니다.
            } // 카메라의 색상 처리와 기존 관찰 강조 효과는 각각의 기존 패스를 유지합니다.
        } // 모델의 실루엣 전용 패스 정의를 마칩니다.
    } // 기존 카툰 모델의 외곽선 연결을 마칩니다.
} // 횡단보도 렌더링 범위를 마칩니다.
