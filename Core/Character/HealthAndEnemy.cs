using System;
using VrAction.Core.Model;

namespace VrAction.Core.Character
{
    /// <summary>HP and the two difficulty modes (FR-016).</summary>
    public sealed class HealthSystem
    {
        readonly DifficultyMode _mode;
        readonly Cell _start;
        Cell _checkpoint;
        bool _hasCheckpoint;
        Cell _lastSafe;

        public int Hp { get; private set; }
        public int MaxHp { get; }
        public bool IsDown { get; private set; }

        public HealthSystem(DifficultyMode mode, int maxHp, Cell start)
        {
            _mode = mode; MaxHp = maxHp; Hp = maxHp; _start = start; _lastSafe = start;
        }

        /// <summary>NoDeath mode: enemy attacks never reduce HP. Down characters take no further damage.</summary>
        public void ApplyDamage(int amount)
        {
            if (_mode == DifficultyMode.NoDeath || IsDown || amount <= 0) return;
            Hp = Math.Max(0, Hp - amount);
            if (Hp == 0) IsDown = true;
        }

        public void ReachCheckpoint(Cell c) { _checkpoint = c; _hasCheckpoint = true; }

        public void NotifySafeCell(Cell c) { _lastSafe = c; }

        /// <summary>Falling out of the stage: return to the last safe cell (costs 1 HP in checkpoint mode).</summary>
        public Cell OnFall()
        {
            ApplyDamage(1);
            return _lastSafe;
        }

        public Cell Respawn()
        {
            Hp = MaxHp; IsDown = false;
            return _hasCheckpoint ? _checkpoint : _start;
        }
    }

    public enum EnemyState { Idle, Chase, Attack }

    /// <summary>Plain rule-based state machine (no machine learning).</summary>
    public sealed class EnemyBrain
    {
        readonly float _sight, _attackRange, _cooldown;
        float _timer;
        bool _ready;

        public EnemyState State { get; private set; } = EnemyState.Idle;

        public EnemyBrain(float sightRange, float attackRange, float attackCooldown)
        {
            _sight = sightRange; _attackRange = attackRange; _cooldown = attackCooldown;
        }

        public EnemyState Update(float dt, float distanceToPlayer)
        {
            var next = distanceToPlayer <= _attackRange ? EnemyState.Attack
                     : distanceToPlayer <= _sight ? EnemyState.Chase
                     : EnemyState.Idle;
            if (next == EnemyState.Attack)
            {
                if (State != EnemyState.Attack) _timer = 0f;
                else
                {
                    _timer += dt;
                    if (_timer + 1e-4f >= _cooldown) { _ready = true; _timer = 0f; }
                }
            }
            else _timer = 0f;
            State = next;
            return State;
        }

        public bool ConsumeAttack()
        {
            bool r = _ready; _ready = false; return r;
        }
    }
}
