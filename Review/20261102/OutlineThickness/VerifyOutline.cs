using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using ProjectWait.M.Elevator;
using Object = UnityEngine.Object;

public static class VerifyOutline
{
    public static object Main(string label = "Before")
    {
        const string root = "Review/20261102/OutlineThickness/";
        Directory.CreateDirectory(root);
        var previousTarget = ElevatorSilhouetteOutlineFeature.DebugTarget;
        var previousMask = ElevatorSilhouetteOutlineFeature.DebugMask;
        var previousRT = RenderTexture.active;
        var go = new GameObject("Outline verification camera");
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        var rt = new RenderTexture(1024, 1024, 24);
        var pixels = new Texture2D(1024, 1024, TextureFormat.RGB24, false);
        var results = new List<object>();
        try
        {
            var camera = go.AddComponent<Camera>();
            camera.enabled = false;
            camera.transform.position = new Vector3(1000, 1000, 1000);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.fieldOfView = 68;
            camera.nearClipPlane = .01f;
            camera.farClipPlane = 20;
            camera.targetTexture = rt;
            var cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = false;
            cameraData.requiresDepthTexture = true;
            material.SetColor("_BaseColor", Color.white);
            quad.GetComponent<Renderer>().sharedMaterial = material;
            quad.transform.localScale = new Vector3(.25f, .25f, 1);
            var target = quad.AddComponent<ElevatorOutlineTarget>();
            target.geometry = new[] { new ElevatorOutlineGeometry { sourceRenderer = quad.GetComponent<Renderer>() } };
            target.idleColor = new Color(1, 0, 0, 1);
            ElevatorSilhouetteOutlineFeature.DebugTarget = target;
            ElevatorSilhouetteOutlineFeature.DebugMask = false;
            var cases = new[] {
                new { ortho=false, distance=1f, size=1024, fov=68f, hovered=false },
                new { ortho=false, distance=2f, size=1024, fov=68f, hovered=false },
                new { ortho=false, distance=4f, size=1024, fov=68f, hovered=false },
                new { ortho=true, distance=1f, size=1024, fov=68f, hovered=false },
                new { ortho=true, distance=2f, size=1024, fov=68f, hovered=false },
                new { ortho=true, distance=4f, size=1024, fov=68f, hovered=false },
                new { ortho=false, distance=2f, size=512, fov=68f, hovered=false },
                new { ortho=false, distance=2f, size=1024, fov=45f, hovered=false },
                new { ortho=false, distance=2f, size=1024, fov=90f, hovered=false },
                new { ortho=false, distance=1f, size=1024, fov=68f, hovered=true },
                new { ortho=false, distance=4f, size=1024, fov=68f, hovered=true }
            };
            foreach (var test in cases)
            {
                bool ortho = test.ortho;
                float distance = test.distance;
                camera.orthographic = ortho;
                camera.orthographicSize = .7f;
                camera.fieldOfView = test.fov;
                if (rt.width != test.size)
                {
                    camera.targetTexture = null;
                    RenderTexture.active = null;
                    Object.DestroyImmediate(rt);
                    Object.DestroyImmediate(pixels);
                    rt = new RenderTexture(test.size, test.size, 24);
                    pixels = new Texture2D(test.size, test.size, TextureFormat.RGB24, false);
                    camera.targetTexture = rt;
                }
                quad.transform.position = camera.transform.position + Vector3.forward * distance;
                target.hoverColor = target.idleColor;
                target.SetPresentation(true, test.hovered, test.hovered);
                camera.Render();
                RenderTexture.active = rt;
                pixels.ReadPixels(new Rect(0, 0, test.size, test.size), 0, 0);
                pixels.Apply();
                string path = root + label + (ortho ? "_Ortho_" : "_Perspective_") + distance + "m_" + test.size + "px_" + test.fov + "fov_" + (test.hovered ? "Hover" : "Idle") + ".png";
                File.WriteAllBytes(path, pixels.EncodeToPNG());
                int edge = -1, outline = -1, leftEdge = test.size, leftOutline = test.size;
                for (int x = test.size / 2; x < test.size; x++)
                {
                    var colour = pixels.GetPixel(x, test.size / 2);
                    if (colour.g > .9f) edge = x;
                    if (colour.r > .8f && colour.g < .1f) outline = x;
                }
                for (int x = test.size / 2 - 1; x >= 0; x--)
                {
                    var colour = pixels.GetPixel(x, test.size / 2);
                    if (colour.g > .9f) leftEdge = x;
                    if (colour.r > .8f && colour.g < .1f) leftOutline = x;
                }
                float coreWidth = .5f * (outline - edge + leftEdge - leftOutline);
                float expected = target.Width * (4f * Mathf.Tan(34f * Mathf.Deg2Rad) / 1080f) * test.size * .5f * Mathf.Abs(camera.projectionMatrix.m11) / (ortho ? 1 : distance);
                bool passed = edge >= test.size / 2 && outline >= edge && Mathf.Abs(coreWidth - expected) <= 1.25f && ElevatorSilhouetteOutlineFeature.LastTargetCount == 1;
                results.Add(new { ortho, distance, test.size, test.fov, test.hovered, objectHalfWidth = .5f * (edge - leftEdge + 1), coreWidth, expected, passed, path });
            }
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(results, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(root + label + ".json", json);
            return results;
        }
        finally
        {
            ElevatorSilhouetteOutlineFeature.DebugTarget = previousTarget;
            ElevatorSilhouetteOutlineFeature.DebugMask = previousMask;
            RenderTexture.active = previousRT;
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(quad);
            Object.DestroyImmediate(material);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(pixels);
        }
    }
}
