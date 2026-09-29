using UnityEngine;
using UnityEngine.InputSystem.XR;
using VrAction.Core.Character;

namespace VrAction.Game.Presentation
{
    /// <summary>
    /// Head-tracked rig. The player's head and body stay at real scale (scale 1); only the stage is small,
    /// so peeking in, looking from the side or under the table is just moving your real head.
    /// </summary>
    public sealed class PeekRig : MonoBehaviour
    {
        public Camera HeadCamera { get; private set; }

        public static PeekRig Create()
        {
            var root = new GameObject("XR Origin");
            var rig = root.AddComponent<PeekRig>();
            var camGo = new GameObject("Head Camera");
            camGo.transform.SetParent(root.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.4f, -0.5f);
            camGo.tag = "MainCamera";
            rig.HeadCamera = camGo.AddComponent<Camera>();
            rig.HeadCamera.nearClipPlane = 0.02f; // allow leaning in very close to the small world
            camGo.AddComponent<AudioListener>();
            // On a headset the pose comes from the HMD; without one the camera stays where it is.
            var driver = camGo.AddComponent<TrackedPoseDriver>();
            driver.positionInput = new UnityEngine.InputSystem.InputActionProperty(
                new UnityEngine.InputSystem.InputAction("pos", binding: "<XRHMD>/centerEyePosition"));
            driver.rotationInput = new UnityEngine.InputSystem.InputActionProperty(
                new UnityEngine.InputSystem.InputAction("rot", binding: "<XRHMD>/centerEyeRotation"));
            driver.positionAction.Enable();
            driver.rotationAction.Enable();
            return rig;
        }
    }

    /// <summary>Applies the seated/standing view offset to the rig (FR-021).</summary>
    public sealed class PostureSetup : MonoBehaviour
    {
        PostureState _state = new PostureState(PostureMode.Standing, 1600, 1150);
        float _baseY;
        bool _init;

        public PostureMode Mode => _state.Mode;

        void Init()
        {
            if (_init) return;
            _baseY = transform.position.y; _init = true;
        }

        public void Apply(PostureMode mode)
        {
            Init();
            _state.SetMode(mode);
            var p = transform.position;
            transform.position = new Vector3(p.x, _baseY + _state.ViewOffsetMm / 1000f, p.z);
        }

        /// <summary>Recentering keeps the chosen posture and its offset.</summary>
        public void Recenter()
        {
            Init();
            _state.Recenter();
            Apply(_state.Mode);
        }
    }
}
