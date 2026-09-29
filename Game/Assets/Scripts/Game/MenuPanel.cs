using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VrAction.Core.Model;

namespace VrAction.Game.Game
{
    /// <summary>
    /// Minimal in-world text menu (works in VR): stick left/right (or arrow keys) moves, primary button (or Enter) confirms.
    /// </summary>
    public sealed class MenuPanel : MonoBehaviour
    {
        TextMesh _text;
        readonly List<string> _labels = new List<string>();
        System.Action<int> _onChoose;
        int _index;
        InputAction _nav, _confirm;
        float _navCooldown;

        public bool Visible => gameObject.activeSelf;
        public IReadOnlyList<string> Labels => _labels;
        public string Title => _title;

        /// <summary>Confirms option i (used by input and tests).</summary>
        public void Select(int i) { _onChoose?.Invoke(i); }

        public static MenuPanel Create(Transform head)
        {
            var go = new GameObject("MenuPanel");
            go.transform.SetParent(head, false);
            go.transform.localPosition = new Vector3(0f, -0.05f, 1.0f);
            var p = go.AddComponent<MenuPanel>();
            var t = new GameObject("Text");
            t.transform.SetParent(go.transform, false);
            p._text = t.AddComponent<TextMesh>();
            p._text.anchor = TextAnchor.MiddleCenter;
            p._text.characterSize = 0.02f;
            p._text.fontSize = 48;
            p._text.color = Color.white;
            go.SetActive(false);
            return p;
        }

        void OnEnable()
        {
            _nav = new InputAction("nav", InputActionType.Value);
            _nav.AddBinding("<XRController>{LeftHand}/thumbstick");
            _confirm = new InputAction("confirm", InputActionType.Button);
            _confirm.AddBinding("<XRController>{RightHand}/primaryButton");
            _confirm.AddBinding("<Keyboard>/enter");
            _nav.Enable(); _confirm.Enable();
        }

        void OnDisable() { _nav?.Disable(); _confirm?.Disable(); }

        public void Bind(GameBootstrap b) { }

        public void ShowDifficulty(DifficultySelect select, System.Action then)
        {
            Show("Difficulty", new[] { "Checkpoint respawn", "No death" }, i =>
            {
                select.Choose(select.Options[i]);
                Hide();
                then?.Invoke();
            });
        }

        public void ShowNextStage(NextStageMenu menu)
        {
            var labels = new List<string>();
            foreach (var o in menu.Options) labels.Add(o.ToString());
            Show("Stage cleared! Next:", labels, i => { if (menu.Choose(menu.Options[i])) Hide(); });
        }

        /// <summary>Sharing exports the shape of the player's room, so it needs a clear yes (FR-025).</summary>
        public void ShowShareConsent(System.Action<bool> result)
        {
            Show("Sharing includes the shape of your room. Share?", new[] { "Yes, share", "No" }, i => { Hide(); result(i == 0); });
        }

        void Show(string title, IEnumerable<string> options, System.Action<int> onChoose)
        {
            _labels.Clear(); _labels.AddRange(options);
            _onChoose = onChoose; _index = 0;
            gameObject.SetActive(true);
            Redraw(title);
            _title = title;
        }

        string _title = "";

        void Hide() { gameObject.SetActive(false); }

        void Redraw(string title)
        {
            var sb = new System.Text.StringBuilder(title).AppendLine();
            for (int i = 0; i < _labels.Count; i++) sb.Append(i == _index ? "> " : "  ").AppendLine(_labels[i]);
            _text.text = sb.ToString();
        }

        void Update()
        {
            _navCooldown -= Time.unscaledDeltaTime;
            var v = _nav.ReadValue<Vector2>();
            var kb = Keyboard.current;
            float dir = Mathf.Abs(v.x) > 0.6f ? Mathf.Sign(v.x) : 0f;
            if (kb != null) { if (kb.rightArrowKey.isPressed) dir = 1; else if (kb.leftArrowKey.isPressed) dir = -1; }
            if (dir != 0f && _navCooldown <= 0f)
            {
                _index = (_index + (int)dir + _labels.Count) % _labels.Count;
                _navCooldown = 0.25f;
                Redraw(_title);
            }
            if (_confirm.WasPressedThisFrame()) Select(_index);
        }
    }
}
