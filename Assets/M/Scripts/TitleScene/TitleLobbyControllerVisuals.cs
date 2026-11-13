using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;

namespace ProjectWait.M.TitleScene
{
    // M-owned copies of the elevator's visual hierarchy, attached to its aim pose basis.
    public sealed class TitleLobbyControllerVisuals : IDisposable
    {
        readonly Transform root, left, right, pointer;
        readonly Material teal, orange, rayMaterial, previousRayMaterial;
        readonly InputAction leftPose,rightPose;
        readonly LineRenderer line;
        readonly Renderer pointerRenderer;
        bool visible;

        public TitleLobbyControllerVisuals(Transform parent,LineRenderer source,GameObject leftPrefab,GameObject rightPrefab,InputAction rightAim)
        {
            root = new GameObject("Lobby tracked controller visuals").transform;
            root.SetParent(parent, false);
            line = source;
            var shader = source && source.sharedMaterial ? source.sharedMaterial.shader : Shader.Find("Universal Render Pipeline/Unlit");
            if (!shader) { root.gameObject.SetActive(false); return; }
            teal = Material(shader, new Color(.16f, .48f, .44f));
            orange = Material(shader, new Color(1f, .55f, .18f));
            left = Controller("Left controller",leftPrefab);
            right = Controller("Right controller",rightPrefab);
            leftPose=new InputAction("LobbyLeftAim",InputActionType.Value,"<XRController>{LeftHand}/pointer",expectedControlType:"Pose");
            leftPose.Enable();rightPose=rightAim;
            pointer = Part("Aim hit point", root, PrimitiveType.Sphere, Vector3.zero, Vector3.one * .018f, teal);
            pointerRenderer = pointer.GetComponent<Renderer>();
            pointer.gameObject.SetActive(false);
            if (line)
            {
                previousRayMaterial = line.sharedMaterial;
                rayMaterial = Material(shader, new Color(.34f, .76f, .64f));
                line.sharedMaterial = rayMaterial;
                line.useWorldSpace = true; line.positionCount = 2;
                line.startWidth = .006f; line.endWidth = .003f; line.numCapVertices = 4;
                line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
            }
        }

        static Material Material(Shader shader, Color color)
        {
            var material = new Material(shader) { name = "Lobby controller (runtime)" };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            return material;
        }
        Transform Controller(string name,GameObject prefab)
        {
            var hand = new GameObject(name).transform; hand.SetParent(root, false);
            if(prefab)UnityEngine.Object.Instantiate(prefab,hand,false);
            else Debug.LogError("Lobby controller visual prefab is not assigned: "+name);
            return hand;
        }
        static Transform Part(string name, Transform parent, PrimitiveType shape, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(shape); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            var collider = go.GetComponent<Collider>(); if (collider) { collider.enabled = false; Remove(collider); }
            var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return go.transform;
        }
        public void Update(bool show)
        {
            visible = show;
            if (!root || !left || !right) return;
            root.gameObject.SetActive(show);
            if (!show) { pointer.gameObject.SetActive(false); return; }
            Track(left,leftPose);Track(right,rightPose);
        }
        static void Track(Transform model,InputAction action)
        {
            bool tracked=TitleControllerPose.TryRead(action,out var position,out var rotation);
            model.gameObject.SetActive(tracked);
            if(tracked)model.SetLocalPositionAndRotation(position,rotation);
        }
        public void ShowPointer(Vector3 end, bool show, bool hovered)
        {
            if (!pointer) return;
            pointer.gameObject.SetActive(visible && show);
            if (!visible || !show) return;
            pointer.position = end;
            pointerRenderer.sharedMaterial = hovered ? orange : teal;
        }
        public void Dispose()
        {
            if (line && line.sharedMaterial == rayMaterial) line.sharedMaterial = previousRayMaterial;
            if (root) Remove(root.gameObject);
            leftPose?.Dispose();Remove(teal);Remove(orange);Remove(rayMaterial);
        }
        static void Remove(UnityEngine.Object value)
        {
            if (!value) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
