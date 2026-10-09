using System;
using UnityEngine;

namespace Plumb.Viewer
{
    /// <summary>
    /// Builds the viewer from code when the player starts, so the scene itself stays empty.
    /// </summary>
    public static class ViewerBootstrap
    {
        private static readonly Color Background = new Color(0.12f, 0.12f, 0.13f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Start()
        {
            var viewCamera = new GameObject("Camera").AddComponent<Camera>();
            viewCamera.tag = "MainCamera";
            viewCamera.clearFlags = CameraClearFlags.SolidColor;
            viewCamera.backgroundColor = Background;
            viewCamera.fieldOfView = 45f;

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.64f, 0.68f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.45f, 0.47f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.25f, 0.26f);

            var orbit = viewCamera.gameObject.AddComponent<OrbitCamera>();
            orbit.Attach(viewCamera);

            var controller = new GameObject("Viewer").AddComponent<ViewerController>();
            controller.Begin(ViewerArguments.Parse(Environment.GetCommandLineArgs()), viewCamera, orbit);
        }
    }
}
