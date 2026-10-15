using System;
using System.IO;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;

namespace Plumb.Viewer
{
    /// <summary>
    /// Loads a <c>.plumb</c> package and lets the user pick elements. Driven from <see cref="Update"/> as a
    /// state machine: glTFast's loading tasks are polled each frame rather than awaited. Screenshots and benchmarks
    /// are a <see cref="ViewerAutomation"/>, told how loading goes and stepped every frame.
    /// </summary>
    public sealed class ViewerController : MonoBehaviour, IViewerHost
    {
        private const string GeometryFile = "model.glb";
        private const string ElementIndexFile = "elements.json";
        private const double ColliderBudgetMilliseconds = 5;

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
        private string _packageName;
        private ColliderBuilder _colliders;
        private ElementInfo _selected;
        private ViewerAutomation _automation = new NoAutomation();

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
            _automation = ViewerAutomation.For(arguments);
            _automation.Begin(this, Now);
            StartLoading();
        }

        private static double Now => Time.realtimeSinceStartupAsDouble;

        private void StartLoading()
        {
            if (!_arguments.HasPackage)
            {
                Finish(ViewerState.NoPackage, "No package. In Plumb, open a model and choose Open in 3D.");
                return;
            }

            var package = _arguments.PackagePath;
            _packageName = Path.GetFileName(package.TrimEnd('/', '\\'));
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
            _message = $"Opening {_packageName}…";
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

            _automation.Update(this, Time.unscaledDeltaTime);
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

            _automation.ModelRead(Now);
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

            ModelColors.Apply(_model, _index);
            _automation.SceneShown(Now);
            _colliders = new ColliderBuilder(_model.GetComponentsInChildren<MeshFilter>());
            if (_colliders.Done)
            {
                _automation.Ready(this, Now);
            }
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
            if (_colliders.Done)
            {
                _automation.Ready(this, Now);
            }
        }

        (int Meshes, long Triangles) IViewerHost.CountGeometry()
        {
            var meshes = 0;
            var indices = 0L;
            foreach (var filter in _model.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                meshes++;
                for (var sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    indices += mesh.GetIndexCount(sub);
                }
            }

            return (meshes, indices / 3);
        }

        void IViewerHost.CaptureScreenshot(string path) => ScreenCapture.CaptureScreenshot(path);

        void IViewerHost.WriteReport(string path, string text)
        {
            try
            {
                File.WriteAllText(path, text + Environment.NewLine);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Debug.LogError($"Cannot write the report to {path}: {ex.Message}");
            }
        }

        void IViewerHost.TurnCamera(float degrees) => _orbit.Turn(degrees);

        void IViewerHost.UncapFrameRate()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
        }

        void IViewerHost.Quit() => Application.Quit();

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
            if (Physics.Raycast(ray, out var hit) && _index.TryFind(hit.transform, _model, out var node, out var element))
            {
                Select(node, element);
            }
            else
            {
                _highlight.Clear();
                _selected = null;
            }
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

            // A ready viewer reports Ready once every element is clickable; see BuildColliders.
            if (state != ViewerState.Ready)
            {
                _automation.Stopped(this, message);
            }
        }

        /// <param name="pointer">A position from <c>Input.mousePosition</c>, whose origin is bottom left.</param>
        private bool IsPointerOverUi(Vector2 pointer) =>
            pointer.y > Screen.height - ViewerChrome.TopBarHeight
            || pointer.y < ViewerChrome.StatusBarHeight
            || (_selected != null && ElementPanel.Contains(pointer, Screen.height));

        private static bool Succeeded(Task<bool> task) => task.Status == TaskStatus.RanToCompletion && task.Result;

        private void OnGUI()
        {
            ViewerChrome.DrawTopBar(_packageName);

            switch (_state)
            {
                case ViewerState.NoPackage:
                    ViewerChrome.DrawNotice("NO PACKAGE", _message);
                    break;
                case ViewerState.Failed:
                    ViewerChrome.DrawNotice("CANNOT OPEN", _message);
                    break;
                case ViewerState.LoadingModel:
                case ViewerState.BuildingScene:
                    ViewerChrome.DrawNotice("LOADING", _message);
                    break;
            }

            if (_selected != null)
            {
                ElementPanel.Draw(_selected);
            }

            ViewerChrome.DrawStatusBar(StatusText);
        }

        /// <summary>Notices carry the message in every other state, so the status bar does not repeat it.</summary>
        private string StatusText => _state == ViewerState.Ready ? _message ?? ViewerChrome.HelpText : string.Empty;

        private void OnDestroy()
        {
            _highlight.Clear();
            _import?.Dispose();
        }
    }
}
