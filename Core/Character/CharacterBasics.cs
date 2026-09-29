using System;

namespace VrAction.Core.Character
{
    /// <summary>Character size setting: 5-20cm, default 10cm (FR-001).</summary>
    public sealed class ScaleSettings
    {
        public const int MinCm = 5;
        public const int MaxCm = 20;
        public const int DefaultCm = 10;

        public int CharacterHeightCm { get; }
        public int CharacterHeightMm => CharacterHeightCm * 10;

        public ScaleSettings() : this(DefaultCm) { }
        public ScaleSettings(int cm) { CharacterHeightCm = Math.Max(MinCm, Math.Min(MaxCm, cm)); }
    }

    public enum PostureMode { Seated, Standing }

    /// <summary>Seated/standing view offset that survives recentering (FR-021).</summary>
    public sealed class PostureState
    {
        readonly int _standingEyeMm;
        readonly int _seatedEyeMm;
        public PostureMode Mode { get; private set; }

        public PostureState(PostureMode mode, int standingEyeMm, int seatedEyeMm)
        {
            Mode = mode; _standingEyeMm = standingEyeMm; _seatedEyeMm = seatedEyeMm;
        }

        public int ViewOffsetMm => Mode == PostureMode.Standing ? 0 : _seatedEyeMm - _standingEyeMm;

        public void SetMode(PostureMode mode) { Mode = mode; }

        /// <summary>Recentering never resets the posture or its offset.</summary>
        public void Recenter() { }
    }

    /// <summary>Movement/jump/gravity for the small character. Units: metres and seconds.</summary>
    public sealed class CharacterMotor
    {
        readonly float _speed;
        readonly float _jumpSpeed;
        readonly float _gravity;

        public float X { get; private set; }
        public float Y { get; private set; }
        public float Z { get; private set; }
        public float VelocityY { get; private set; }
        public bool IsGrounded { get; private set; } = true;

        public CharacterMotor(float speed, float jumpSpeed, float gravity)
        {
            _speed = speed; _jumpSpeed = jumpSpeed; _gravity = gravity;
        }

        /// <summary>Speed of 3 body-heights per second; jump apex about 1.2 body-heights.</summary>
        public static CharacterMotor ForHeight(float heightMetres)
        {
            const float g = 9.8f;
            return new CharacterMotor(heightMetres * 3f, (float)Math.Sqrt(2f * g * heightMetres * 1.2f), g);
        }

        public void SetHorizontal(float x, float z) { X = x; Z = z; }

        public void Teleport(float x, float y, float z)
        {
            X = x; Y = y; Z = z; VelocityY = 0f; IsGrounded = true;
        }

        public void Step(float dt, float moveX, float moveZ, bool jumpPressed, float groundY)
        {
            X += moveX * _speed * dt;
            Z += moveZ * _speed * dt;

            if (IsGrounded && jumpPressed)
            {
                VelocityY = _jumpSpeed;
                IsGrounded = false;
            }

            if (!IsGrounded)
            {
                VelocityY -= _gravity * dt;
                Y += VelocityY * dt;
                if (Y <= groundY && VelocityY <= 0f)
                {
                    Y = groundY; VelocityY = 0f; IsGrounded = true;
                }
            }
            else
            {
                // walking: follow ground height; drop off ledges
                if (Y > groundY + 1e-6f) IsGrounded = false;
                else Y = groundY;
            }
        }
    }
}
