using System;
using System.IO;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;

namespace Plumb.Viewer
{
    /// <summary>
    /// Loads a <c>.plumb</c> package and lets the user pick elements. Driven from <see cref="Update"/> as a
    /// state machine: glTFast's loading tasks are polled each frame rather than awaited.
    /// </summary>
    public sealed class ViewerController : MonoBehaviour
    {
        private const string GeometryFile = "model.glb";
        private const string ElementIndexFile = "elements.json";
        private const int FramesBeforeScreenshot = 5;
        private const int FramesBeforeQuit = 5;
        private const double ColliderBudgetMilliseconds = 5;
        private const string Help = "Left drag: rotate    Shift or middle drag: pan    Wheel: zoom    Click: select    F: frame";

        private readonly ClickGesture _click = new ClickGesture();
        private readonly SelectionHighlight _highlight = new SelectionHighlight();

        private ViewerArguments _arguments;
        private Camera _camera;
        private OrbitCamera _orbit;
        private ViewerState _state;
        private string _message;
        private ElementIndex _index;
        private GltfImport _import;
        private Task<bool> _pending;
        private Transform _model;
        private ColliderBuilder _colliders;
        private ElementInfo _selected;
        private int _screenshotCountdown = -1;
        private int _quitCountdown = -1;

        private enum ViewerState
        {
            NoPackage,
            LoadingModel,
            BuildingScene,
            Ready,
            Failed,
        }

        public void Begin(ViewerArguments arguments, Camera viewCamera, OrbitCamera orbit)
        {
            _arguments = arguments;
            _camera = viewCamera;
            _orbit = orbit;
            _orbit.IsOverUi = IsPointerOverUi;
            StartLoading();
        }

        private void StartLoading()
        {
            if (!_arguments.HasPackage)
            {
                Finish(ViewerState.NoPackage, "No package. In Plumb, open a model and choose Open in 3D.");
                return;
            }

            var package = _arguments.PackagePath;
            var geometry = Path.Combine(package, GeometryFile);
            if (!Directory.Exists(package))
            {
                Finish(ViewerState.Failed, $"Package not found: {package}");
                return;
            }

            if (!File.Exists(geometry))
            {
                Finish(ViewerState.Failed, "This package has no 3D geometry (model.glb).");
                return;
            }

            try
            {
                _index = ElementIndex.Parse(File.ReadAllText(Path.Combine(package, ElementIndexFile)));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
            {
                Finish(ViewerState.Failed, $"Cannot read {ElementIndexFile}: {ex.Message}");
                return;
            }

            _import = new GltfImport();
            var settings = new ImportSettings { NodeNameMethod = NameImportMethod.Original };
            _pending = _import.LoadFile(geometry, null, settings);
            _state = ViewerState.LoadingModel;
            _message = $"Loading {Path.GetFileName(package.TrimEnd('/', '\\'))}…";
        }

        private void Update()
        {
            switch (_state)
            {
                case ViewerState.LoadingModel:
                    PollLoading();
                    break;
                case ViewerState.BuildingScene:
                    PollBuilding();
                    break;
                case ViewerState.Ready:
                    BuildColliders();
                    HandleInput();
                    break;
            }

            AdvanceScreenshot();
        }

        private void PollLoading()
        {
            if (!_pending.IsCompleted)
            {
                return;
            }

            if (!Succeeded(_pending))
            {
                Finish(ViewerState.Failed, $"Cannot read {GeometryFile}. {_pending.Exception?.GetBaseException().Message}".Trim());
                return;
            }

            _model = new GameObject("Model").transform;
            _pending = _import.InstantiateMainSceneAsync(_model);
            _state = ViewerState.BuildingScene;
        }

        private void PollBuilding()
        {
            if (!_pending.IsCompleted)
            {
                return;
            }

            if (!Succeeded(_pending))
            {
                Finish(ViewerState.Failed, $"Cannot show {GeometryFile}. {_pending.Exception?.GetBaseException().Message}".Trim());
                return;
            }

            _colliders = new ColliderBuilder(_model.GetComponentsInChildren<MeshFilter>());
            if (RendererBounds.TryEncapsulate(_model.GetComponentsInChildren<Renderer>(), out var scene))
            {
                _orbit.SetScene(scene);
            }

            FrameModel();
            SelectRequestedElement();
            Finish(ViewerState.Ready, null);
        }

        private void BuildColliders()
        {
            if (_colliders == null || _colliders.Done)
            {
                return;
            }

            _colliders.Advance(ColliderBudgetMilliseconds);
            _message = _colliders.Done ? null : $"Preparing selection… {_colliders.Progress:P0}";
            if (_colliders.Done && _arguments.TakesScreenshot)
            {
                _screenshotCountdown = FramesBeforeScreenshot;
            }
        }

        private void HandleInput()
        {
            var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (Input.GetMouseButtonDown(0) && !shift && !IsPointerOverUi(Input.mousePosition))
            {
                _click.Press(Input.mousePosition);
            }

            if (Input.GetMouseButtonUp(0) && _click.ReleaseIsClick(Input.mousePosition))
            {
                Pick(Input.mousePosition);
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                FrameSelectionOrModel();
            }
        }

        private void Pick(Vector2 screenPoint)
        {
            var ray = _camera.ScreenPointToRay(screenPoint);
            if (Physics.Raycast(ray, out var hit) && TryFindElement(hit.transform, out var node, out var element))
            {
                Select(node, element);
            }
            else
            {
                _highlight.Clear();
                _selected = null;
            }
        }

        private bool TryFindElement(Transform hit, out Transform node, out ElementInfo element)
        {
            for (var current = hit; current != null && current != _model; current = current.parent)
            {
                if (_index.TryGet(current.name, out element))
                {
                    node = current;
                    return true;
                }
            }

            node = null;
            element = null;
            return false;
        }

        private void Select(Transform node, ElementInfo element)
        {
            _highlight.Show(node);
            _selected = element;
        }

        private void SelectRequestedElement()
        {
            if (string.IsNullOrEmpty(_arguments.SelectId))
            {
                return;
            }

            foreach (var node in _model.GetComponentsInChildren<Transform>())
            {
                if (node.name == _arguments.SelectId && _index.TryGet(node.name, out var element))
                {
                    Select(node, element);
                    return;
                }
            }
        }

        private void FrameModel()
        {
            if (RendererBounds.TryEncapsulate(_model.GetComponentsInChildren<Renderer>(), out var bounds))
            {
                _orbit.Frame(bounds);
            }
        }

        private void FrameSelectionOrModel()
        {
            if (_highlight.TryGetBounds(out var bounds))
            {
                _orbit.Frame(bounds);
            }
            else
            {
                FrameModel();
            }
        }

        private void Finish(ViewerState state, string message)
        {
            _state = state;
            _message = message;
            _orbit.Enabled = state == ViewerState.Ready;

            // A ready viewer is captured once every element is clickable; see BuildColliders.
            if (_arguments.TakesScreenshot && state != ViewerState.Ready)
            {
                _screenshotCountdown = FramesBeforeScreenshot;
            }
        }

        private bool IsPointerOverUi(Vector2 pointer) => _selected != null && ElementPanel.Contains(pointer, Screen.height);

        private void AdvanceScreenshot()
        {
            if (_screenshotCountdown > 0 && --_screenshotCountdown == 0)
            {
                ScreenCapture.CaptureScreenshot(_arguments.ScreenshotPath);
                _quitCountdown = FramesBeforeQuit;
            }
            else if (_quitCountdown > 0 && --_quitCountdown == 0)
            {
                Application.Quit();
            }
        }

        private static bool Succeeded(Task<bool> task) => task.Status == TaskStatus.RanToCompletion && task.Result;

        private void OnGUI()
        {
            if (!string.IsNullOrEmpty(_message))
            {
                GUI.Box(new Rect(16, 16, Mathf.Min(640, Screen.width - 32), 32), _message, ViewerStyles.Message);
            }

            if (_state == ViewerState.Ready)
            {
                GUI.Label(new Rect(16, Screen.height - 30, Screen.width - 32, 24), Help, ViewerStyles.Hint);
            }

            if (_selected != null)
            {
                ElementPanel.Draw(_selected);
            }
        }

        private void OnDestroy()
        {
            _highlight.Clear();
            _import?.Dispose();
        }
    }
}
