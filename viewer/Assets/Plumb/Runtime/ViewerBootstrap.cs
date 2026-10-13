using System;
using UnityEngine;

namespace Plumb.Viewer
{
    /// <summary>
    /// Builds the viewer from code when the player starts, so the scene itself stays empty.
    /// </summary>
    public static class ViewerBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Start()
        {
            var viewCamera = new GameObject("Camera").AddComponent<Camera>();
            viewCamera.tag = "MainCamera";
            viewCamera.clearFlags = CameraClearFlags.SolidColor;
            viewCamera.backgroundColor = Graphite.Ink;
            viewCamera.fieldOfView = 45f;

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 0.9f;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.57f, 0.61f);
            RenderSettings.ambientEquatorColor = new Color(0.40f, 0.40f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.22f, 0.23f);

            var orbit = viewCamera.gameObject.AddComponent<OrbitCamera>();
            orbit.Attach(viewCamera);

            var controller = new GameObject("Viewer").AddComponent<ViewerController>();
            controller.Begin(ViewerArguments.Parse(Environment.GetCommandLineArgs()), viewCamera, orbit);
        }
    }
}
