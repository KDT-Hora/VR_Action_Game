using System;
using System.Collections.Generic;
using System.Text;

namespace VrAction.Core.Model
{
    public enum StageType { Exploration, Treasure, Combat, Defense, Boss }
    public enum DifficultyMode { CheckpointRespawn, NoDeath }
    public enum ElementKind { Enemy, Item, Gimmick, Chest, Boss, Obstacle, Checkpoint }
    public enum ObjectiveKind { ReachGoal, OpenChest, DefeatAll, DefendPoint, DefeatBoss }
    public enum GenerationFailure { None, TooSmall, NoValidLayout, Invalid }

    public readonly struct Cell : IEquatable<Cell>
    {
        public int X { get; }
        public int Z { get; }
        public Cell(int x, int z) { X = x; Z = z; }
        public bool Equals(Cell o) => X == o.X && Z == o.Z;
        public override bool Equals(object obj) => obj is Cell c && Equals(c);
        public override int GetHashCode() => unchecked(X * 73856093 ^ Z * 19349663);
        public override string ToString() => X + "," + Z;
    }

    public sealed class GenerationRequest
    {
        public ulong Seed { get; }
        public StageType StageType { get; }
        public DifficultyMode Difficulty { get; }
        public int CharacterHeightCm { get; }

        public GenerationRequest(ulong seed, StageType stageType, DifficultyMode difficulty, int characterHeightCm = 10)
        {
            Seed = seed; StageType = stageType; Difficulty = difficulty; CharacterHeightCm = characterHeightCm;
        }
    }

    public sealed class StageElement
    {
        public ElementKind Kind { get; }
        public Cell Cell { get; }
        public int Param { get; }
        public StageElement(ElementKind kind, Cell cell, int param = 0) { Kind = kind; Cell = cell; Param = param; }
    }

    public sealed class Stage
    {
        public GenerationRequest Request { get; }
        public Cell Start { get; }
        public Cell Goal { get; }
        public IReadOnlyList<Cell> Path { get; }
        public IReadOnlyList<StageElement> Elements { get; }
        public ObjectiveKind Objective { get; }

        public Stage(GenerationRequest request, Cell start, Cell goal, IEnumerable<Cell> path,
                     IEnumerable<StageElement> elements, ObjectiveKind objective)
        {
            Request = request; Start = start; Goal = goal; Objective = objective;
            Path = new List<Cell>(path).AsReadOnly();
            Elements = new List<StageElement>(elements).AsReadOnly();
        }

        public Stage WithElements(IEnumerable<StageElement> elements, ObjectiveKind objective)
            => new Stage(Request, Start, Goal, Path, elements, objective);

        /// <summary>Canonical string; two stages are identical iff fingerprints are equal.</summary>
        public string Fingerprint()
        {
            var sb = new StringBuilder();
            sb.Append(Request.Seed).Append('|').Append(Request.StageType).Append('|').Append(Request.Difficulty)
              .Append('|').Append(Objective).Append('|').Append(Start).Append('|').Append(Goal).Append('|');
            foreach (var c in Path) sb.Append(c).Append(';');
            sb.Append('|');
            foreach (var e in Elements) sb.Append(e.Kind).Append('@').Append(e.Cell).Append('#').Append(e.Param).Append(';');
            return sb.ToString();
        }
    }

    public sealed class GenerationOutcome
    {
        public Stage Stage { get; }
        public GenerationFailure Failure { get; }
        public bool IsSuccess => Stage != null;
        GenerationOutcome(Stage s, GenerationFailure f) { Stage = s; Failure = f; }
        public static GenerationOutcome Success(Stage s) => new GenerationOutcome(s, GenerationFailure.None);
        public static GenerationOutcome Fail(GenerationFailure f) => new GenerationOutcome(null, f);
    }
}
