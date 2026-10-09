using UnityEngine;

namespace Plumb.Viewer
{
    /// <summary>
    /// Orbits around a pivot: left drag rotates, middle drag or Shift + left drag pans, the wheel zooms.
    /// </summary>
    public sealed class OrbitCamera : MonoBehaviour
    {
        private const float DegreesPerPixel = 0.3f;
        private const float ZoomPerWheelStep = 0.12f;
        private const float MinPitch = -89f;
        private const float MaxPitch = 89f;
        private const float FramingMargin = 1.15f;

        private Camera _camera;
        private Vector3 _pivot;
        private float _distance = 10f;
        private float _yaw = 45f;
        private float _pitch = 30f;
        private Vector2 _lastPointer;

        public bool Enabled { get; set; } = true;

        public void Attach(Camera viewCamera)
        {
            _camera = viewCamera;
            _lastPointer = Input.mousePosition;
        }

        public void Frame(Bounds bounds)
        {
            _pivot = bounds.center;
            var radius = Mathf.Max(bounds.extents.magnitude, 0.01f);
            var halfFov = _camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            _distance = radius / Mathf.Sin(halfFov) * FramingMargin;
            _camera.nearClipPlane = Mathf.Max(_distance / 1000f, 0.01f);
            _camera.farClipPlane = _distance * 20f;
            Apply();
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                return;
            }

            Vector2 pointer = Input.mousePosition;
            var delta = pointer - _lastPointer;
            _lastPointer = pointer;

            if (Enabled)
            {
                HandleInput(delta);
            }

            Apply();
        }

        private void HandleInput(Vector2 delta)
        {
            var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var panning = Input.GetMouseButton(2) || (Input.GetMouseButton(0) && shift);

            if (panning)
            {
                var metresPerPixel = _distance * 2f * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad) / Screen.height;
                _pivot -= (_camera.transform.right * delta.x + _camera.transform.up * delta.y) * metresPerPixel;
            }
            else if (Input.GetMouseButton(0))
            {
                _yaw += delta.x * DegreesPerPixel;
                _pitch = Mathf.Clamp(_pitch - delta.y * DegreesPerPixel, MinPitch, MaxPitch);
            }

            var wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > Mathf.Epsilon)
            {
                _distance *= Mathf.Pow(1f - ZoomPerWheelStep, wheel);
            }
        }

        private void Apply()
        {
            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            _camera.transform.SetPositionAndRotation(_pivot - rotation * Vector3.forward * _distance, rotation);
        }
    }
}
