using System;
using System.Collections.Generic;
using ProjectWait.M.Elevator;
using UnityEngine;

namespace ProjectWait.Shared
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Wait/Shared Object Highlight")]
    public sealed class SharedObjectHighlight : MonoBehaviour
    {
        [Header("강조할 오브젝트 — Hierarchy에서 끌어 넣으세요")]
        [Tooltip("각 오브젝트의 MeshRenderer와 SkinnedMeshRenderer를 자동으로 연결합니다. 콜라이더는 필요하지 않습니다.")]
        public GameObject[] objects = Array.Empty<GameObject>();
        public bool includeChildren = true;
        public bool highlightOnStart = true;

        [Header("강조 모양")]
        public Color idleColor = new Color(1, .70f, .28f, 1);
        public Color hoverColor = new Color(.32f, .84f, 1, 1);
        [Min(1)] public float idleWidth = 6.5f;
        [Min(1)] public float hoverWidth = 10.4f;
        [Min(1)] public float idleHalo = 20;
        [Min(1)] public float hoverHalo = 32;
        [Min(0)] public float colorTransitionDuration = .25f;

        readonly List<ElevatorOutlineTarget> generated = new List<ElevatorOutlineTarget>();
        bool highlighted, hovered, rebuildRequested;
        public int ActiveTargetCount => generated.Count;
        public int RendererCount { get; private set; }

        void OnEnable()
        {
            if (!Application.isPlaying) return;
            highlighted = highlightOnStart;
            Rebuild();
        }

        void OnDisable() { Release(); }
        void OnValidate() { rebuildRequested = true; }

        void LateUpdate()
        {
            if (rebuildRequested) Rebuild();
            ApplyPresentation();
        }

        // Runtime-only helpers leave the connected objects and their existing components intact.
        [ContextMenu("연결한 오브젝트 다시 수집 (Play Mode)")]
        public void Rebuild()
        {
            rebuildRequested = false;
            Release();
            if (!Application.isPlaying || !isActiveAndEnabled) return;
            var seen = new HashSet<Renderer>();
            foreach (var root in objects ?? Array.Empty<GameObject>())
            {
                if (!root) continue;
                var geometry = new List<ElevatorOutlineGeometry>();
                var renderers = includeChildren ? root.GetComponentsInChildren<Renderer>(true) : root.GetComponents<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (!renderer || seen.Contains(renderer)) continue;
                    var meshRenderer = renderer as MeshRenderer;
                    var skin = renderer as SkinnedMeshRenderer;
                    var filter = meshRenderer ? meshRenderer.GetComponent<MeshFilter>() : null;
                    if (!(skin && skin.sharedMesh) && !(filter && filter.sharedMesh)) continue;
                    seen.Add(renderer);
                    geometry.Add(new ElevatorOutlineGeometry { sourceRenderer = renderer });
                }
                if (geometry.Count == 0) continue;
                var helper = new GameObject("Shared outline / " + root.name);
                helper.transform.SetParent(transform, false);
                helper.hideFlags = HideFlags.HideAndDontSave;
                var target = helper.AddComponent<ElevatorOutlineTarget>();
                target.geometry = geometry.ToArray();
                generated.Add(target);
                RendererCount += geometry.Count;
            }
            ApplyPresentation();
        }

        public void SetHighlighted(bool value) { highlighted = value; ApplyPresentation(); }
        public void Show() { SetHighlighted(true); }
        public void Hide() { SetHighlighted(false); }
        public void SetHovered(bool value) { hovered = value; ApplyPresentation(); }
        public void Flash() { foreach (var target in generated) if (target) target.Flash(); }

        void ApplyPresentation()
        {
            foreach (var target in generated)
            {
                if (!target) continue;
                target.idleColor = idleColor;
                target.hoverColor = hoverColor;
                target.idleWidth = Mathf.Max(1, idleWidth);
                target.hoverWidth = Mathf.Max(1, hoverWidth);
                target.idleHalo = Mathf.Max(1, idleHalo);
                target.hoverHalo = Mathf.Max(1, hoverHalo);
                target.colorTransitionDuration = Mathf.Max(0, colorTransitionDuration);
                target.SetPresentation(highlighted, hovered, hovered);
            }
        }

        void Release()
        {
            foreach (var target in generated)
            {
                if (!target) continue;
                target.SetPresentation(false, false);
                target.enabled = false;
                if (Application.isPlaying) Destroy(target.gameObject);
                else DestroyImmediate(target.gameObject);
            }
            generated.Clear();
            RendererCount = 0;
        }
    }
}
