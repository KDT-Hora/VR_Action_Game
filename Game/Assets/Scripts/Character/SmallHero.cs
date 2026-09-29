using UnityEngine;
using VrAction.Core.Character;
using VrAction.Core.Model;

namespace VrAction.Game.Character
{
    /// <summary>
    /// The sword-carrying child (FR-022). All movement rules live in Core (CharacterMotor);
    /// this component maps them onto the generated grid. Positions are in stage-local metres.
    /// </summary>
    public sealed class SmallHero : MonoBehaviour
    {
        const float FallLimit = -0.5f;

        SpatialData _space;
        Cell _lastSafe;

        public CharacterMotor Motor { get; private set; }
        public HealthSystem Health { get; private set; }
        public ScaleSettings Scale { get; private set; }
        public Vector2 MoveInput;
        public bool JumpInput;
        public bool Paused;
        public Vector3 Forward { get; private set; } = Vector3.forward;

        public event System.Action<Cell> Fell;

        public static SmallHero Create(Transform stageRoot, SpatialData space, ScaleSettings scale, Cell start, DifficultyMode mode)
        {
            var go = new GameObject("Hero");
            go.transform.SetParent(stageRoot, false);
            var hero = go.AddComponent<SmallHero>();
            hero.Init(space, scale, start, mode);
            CharacterView.Build(go.transform, scale.CharacterHeightMm / 1000f);
            return hero;
        }

        void Init(SpatialData space, ScaleSettings scale, Cell start, DifficultyMode mode)
        {
            _space = space; Scale = scale; _lastSafe = start;
            Motor = CharacterMotor.ForHeight(scale.CharacterHeightMm / 1000f);
            Health = new HealthSystem(mode, 5, start);
            PlaceAtCell(start);
        }

        public void Rebind(SpatialData space, Cell start, DifficultyMode mode)
        {
            _space = space; _lastSafe = start;
            Health = new HealthSystem(mode, 5, start);
            PlaceAtCell(start);
        }

        public Cell CurrentCell
        {
            get
            {
                int x = Mathf.FloorToInt((Motor.X * 1000f - _space.OriginXMm) / _space.CellSizeMm);
                int z = Mathf.FloorToInt((Motor.Z * 1000f - _space.OriginZMm) / _space.CellSizeMm);
                return new Cell(x, z);
            }
        }

        public void PlaceAtCell(Cell c)
        {
            float x = (_space.OriginXMm + c.X * _space.CellSizeMm + _space.CellSizeMm / 2) / 1000f;
            float z = (_space.OriginZMm + c.Z * _space.CellSizeMm + _space.CellSizeMm / 2) / 1000f;
            Motor.Teleport(x, GroundAt(c) , z);
            Sync();
        }

        float GroundAt(Cell c) => _space.IsWalkable(c.X, c.Z) ? _space.HeightAt(c.X, c.Z) / 1000f : -100f;

        void Update() => Step(Time.deltaTime);

        public void Step(float dt)
        {
            if (Paused || Motor == null) return;

            var input = MoveInput.sqrMagnitude > 1f ? MoveInput.normalized : MoveInput;
            if (input.sqrMagnitude > 0.01f) Forward = new Vector3(input.x, 0f, input.y).normalized;

            float px = Motor.X, pz = Motor.Z;
            var here = CurrentCell;
            float ground = GroundAt(here);
            Motor.Step(dt, input.x, input.y, JumpInput, ground);

            // resolve the horizontal move against the grid
            var cell = CurrentCell;
            if (!cell.Equals(here))
            {
                bool blocked = false;
                if (_space.InBounds(cell.X, cell.Z))
                {
                    var kind = _space.KindAt(cell.X, cell.Z);
                    if (kind == CellKind.Wall || kind == CellKind.Obstacle) blocked = true;
                    else if (_space.IsWalkable(cell.X, cell.Z) &&
                             _space.HeightAt(cell.X, cell.Z) > Motor.Y * 1000f + SpatialRules.MaxStepMm) blocked = true;
                }
                if (blocked) Motor.SetHorizontal(px, pz);
            }

            if (Motor.Y < FallLimit)
            {
                var back = Health.OnFall();
                PlaceAtCell(back);
                Fell?.Invoke(back);
            }
            else if (Motor.IsGrounded)
            {
                var c = CurrentCell;
                if (_space.InBounds(c.X, c.Z) && _space.IsWalkable(c.X, c.Z) && !_space.IsHazard(c.X, c.Z))
                {
                    _lastSafe = c;
                    Health.NotifySafeCell(c);
                }
            }
            Sync();
        }

        void Sync()
        {
            transform.localPosition = new Vector3(Motor.X, Motor.Y, Motor.Z);
            transform.localRotation = Quaternion.LookRotation(Forward, Vector3.up);
        }
    }

    /// <summary>Placeholder model: a small child with a sword. Replace with art later.</summary>
    public static class CharacterView
    {
        public static void Build(Transform hero, float heightMetres)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            Object.Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(hero, false);
            body.transform.localScale = new Vector3(heightMetres * 0.45f, heightMetres * 0.5f, heightMetres * 0.45f);
            body.transform.localPosition = new Vector3(0, heightMetres * 0.5f, 0);
            Tint(body, new Color(0.95f, 0.75f, 0.55f));

            var sword = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sword.name = "Sword";
            Object.Destroy(sword.GetComponent<Collider>());
            sword.transform.SetParent(hero, false);
            sword.transform.localScale = new Vector3(heightMetres * 0.06f, heightMetres * 0.06f, heightMetres * 0.6f);
            sword.transform.localPosition = new Vector3(heightMetres * 0.3f, heightMetres * 0.5f, heightMetres * 0.4f);
            Tint(sword, new Color(0.8f, 0.85f, 0.95f));
        }

        static void Tint(GameObject go, Color c)
        {
            var r = go.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Sprites/Default")) { color = c };
        }
    }
}
