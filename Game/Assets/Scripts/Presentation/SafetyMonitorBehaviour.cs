using UnityEngine;
using VrAction.Core.Safety;

namespace VrAction.Game.Presentation
{
    /// <summary>
    /// Shows the real world (passthrough) when the head gets near the play boundary (FR-014, FR-027).
    /// The actual passthrough layer is switched by <see cref="PassthroughChanged"/> handlers on the headset build.
    /// </summary>
    public sealed class SafetyMonitorBehaviour : MonoBehaviour
    {
        readonly SafetyMonitor _monitor = new SafetyMonitor();
        int _minX = -2000, _minZ = -2000, _maxX = 2000, _maxZ = 2000;
        int _cullMask;

        public bool PassthroughOn { get; private set; }
        public event System.Action<bool> PassthroughChanged;

        public void SetBoundaryMm(int minX, int minZ, int maxX, int maxZ)
        {
            _minX = minX; _minZ = minZ; _maxX = maxX; _maxZ = maxZ;
        }

        public void ToggleManual() { _monitor.ToggleManual(); Evaluate(); }

        void Update() => Evaluate();

        /// <summary>Recomputes passthrough from the head position (world space, metres).</summary>
        public void Evaluate()
        {
            var rig = GetComponent<PeekRig>();
            var cam = rig != null ? rig.HeadCamera : Camera.main;
            if (cam == null) return;
            var p = cam.transform.position;
            int xmm = Mathf.RoundToInt(p.x * 1000f), zmm = Mathf.RoundToInt(p.z * 1000f);
            int dist = Mathf.Min(Mathf.Min(xmm - _minX, _maxX - xmm), Mathf.Min(zmm - _minZ, _maxZ - zmm));
            bool on = _monitor.Update(dist);
            if (on == PassthroughOn) return;
            PassthroughOn = on;
            if (cam != null)
            {
                if (on) { _cullMask = cam.cullingMask; cam.cullingMask = 0; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0, 0, 0, 0); }
                else { cam.cullingMask = _cullMask == 0 ? ~0 : _cullMask; cam.clearFlags = CameraClearFlags.Skybox; }
            }
            PassthroughChanged?.Invoke(on);
        }
    }
}
