using System.Collections.Generic;
using UnityEngine;
using VrAction.Core.Character;
using VrAction.Core.Model;

namespace VrAction.Game.Character
{
    /// <summary>Sword attack and dodge (FR-002, FR-022).</summary>
    public sealed class CombatController
    {
        const float Cooldown = 0.4f;
        const float DodgeTime = 0.3f;

        readonly SmallHero _hero;
        readonly System.Func<IReadOnlyList<EnemyController>> _enemies;
        float _cooldown, _invulnerable;

        public CombatController(SmallHero hero, System.Func<IReadOnlyList<EnemyController>> enemies)
        {
            _hero = hero; _enemies = enemies;
        }

        public bool IsInvulnerable => _invulnerable > 0f;

        /// <returns>Number of enemies hit by this swing.</returns>
        public int Attack()
        {
            if (_cooldown > 0f) return 0;
            _cooldown = Cooldown;
            float reach = _hero.Scale.CharacterHeightMm / 1000f * 1.2f;
            int hits = 0;
            foreach (var e in _enemies())
            {
                if (e == null || e.Hp <= 0) continue;
                var delta = e.transform.localPosition - _hero.transform.localPosition;
                delta.y = 0f;
                if (delta.magnitude <= reach && Vector3.Dot(delta.normalized, _hero.Forward) > 0f)
                {
                    e.Hit(1);
                    hits++;
                }
            }
            return hits;
        }

        public void Dodge() { _invulnerable = DodgeTime; }

        public void Tick(float dt)
        {
            _cooldown = Mathf.Max(0f, _cooldown - dt);
            _invulnerable = Mathf.Max(0f, _invulnerable - dt);
        }
    }

    /// <summary>Enemy body. Behaviour is the rule-based EnemyBrain from Core (no machine learning).</summary>
    public sealed class EnemyController : MonoBehaviour
    {
        const float CellMetres = 0.05f;

        SmallHero _hero;
        EnemyBrain _brain;
        float _speed;
        System.Func<bool> _heroInvulnerable = () => false;

        public int Hp { get; private set; } = 2;
        public bool IsBoss { get; private set; }
        public int Wave { get; private set; }
        public event System.Action<EnemyController> Died;

        public static EnemyController Spawn(Transform parent, Vector3 localPos, float heightMetres, bool boss, int wave)
        {
            var go = GameObject.CreatePrimitive(boss ? PrimitiveType.Cube : PrimitiveType.Sphere);
            go.name = boss ? "Boss" : "Enemy";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            float s = heightMetres * (boss ? 1.6f : 0.7f);
            go.transform.localScale = Vector3.one * s;
            go.transform.localPosition = localPos + Vector3.up * (s * 0.5f);
            go.GetComponent<Renderer>().material = new Material(Shader.Find("Sprites/Default"))
            { color = boss ? new Color(0.6f, 0.2f, 0.8f) : new Color(0.9f, 0.25f, 0.25f) };
            var e = go.AddComponent<EnemyController>();
            e.IsBoss = boss; e.Wave = wave; e.Hp = boss ? 8 : 2;
            e._speed = heightMetres * 1.2f;
            e._brain = new EnemyBrain(sightRange: 6f, attackRange: 1.5f, attackCooldown: 1f);
            return e;
        }

        public void Bind(SmallHero hero, System.Func<bool> heroInvulnerable)
        {
            _hero = hero; _heroInvulnerable = heroInvulnerable;
        }

        public void PlaceInFrontOf(SmallHero hero, float metres)
        {
            var p = hero.transform.localPosition + hero.Forward * metres;
            transform.localPosition = new Vector3(p.x, transform.localPosition.y, p.z);
        }

        public void Hit(int damage)
        {
            if (Hp <= 0) return;
            Hp -= damage;
            if (Hp <= 0)
            {
                Died?.Invoke(this);
                gameObject.SetActive(false);
            }
        }

        public void Tick(float dt)
        {
            if (Hp <= 0 || _hero == null) return;
            var delta = _hero.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            var state = _brain.Update(dt, delta.magnitude / CellMetres);
            if (state == EnemyState.Chase)
                transform.localPosition += delta.normalized * _speed * dt;
            if (_brain.ConsumeAttack() && !_heroInvulnerable())
                _hero.Health.ApplyDamage(1);
        }
    }
}
