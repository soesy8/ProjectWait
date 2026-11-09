using System.Linq;
using ProjectWait.M.Elevator;
using UnityEngine;

namespace ProjectWait.TeamShare
{
    /// <summary>Minimal highlight driver without lesson, input or XR dependencies.</summary>
    [RequireComponent(typeof(ElevatorOutlineTarget))]
    public sealed class SharedHighlightExample : MonoBehaviour
    {
        public bool visible = true;
        public bool hovered;
        public bool inRange;
        ElevatorOutlineTarget target;

        void Awake()
        {
            target = GetComponent<ElevatorOutlineTarget>();
            if (target.geometry == null || target.geometry.Length == 0)
                target.geometry = GetComponentsInChildren<Renderer>(true)
                    .Where(r => r is MeshRenderer || r is SkinnedMeshRenderer)
                    .Select(r => new ElevatorOutlineGeometry { sourceRenderer = r }).ToArray();
        }

        void Update()
        {
            target.SetPresentation(visible, hovered, inRange);
        }

        void OnDisable()
        {
            if (target) target.SetPresentation(false, false, false);
        }

        public void Flash()
        {
            if (target) target.Flash();
        }
    }
}
