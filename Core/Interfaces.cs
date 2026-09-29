using VrAction.Core.Model;

namespace VrAction.Core
{
    /// <summary>Deterministic: same input gives same output.</summary>
    public interface ISpatialAbstractor
    {
        AbstractionResult Abstract(ScanResult scan);
    }

    public interface IStageGenerator
    {
        GenerationOutcome Generate(SpatialData space, GenerationRequest request);
    }

    public interface IStageValidator
    {
        ValidationReport Validate(Stage stage, SpatialData space);
    }

    public enum ScanError { None, NoSurfaces, NoTable, NoFloor, TooSmall }

    public sealed class AbstractionResult
    {
        public SpatialData Data { get; }
        public ScanError Error { get; }
        public bool IsSuccess => Data != null;
        AbstractionResult(SpatialData d, ScanError e) { Data = d; Error = e; }
        public static AbstractionResult Ok(SpatialData d) => new AbstractionResult(d, ScanError.None);
        public static AbstractionResult Fail(ScanError e) => new AbstractionResult(null, e);
    }

    public sealed class ValidationReport
    {
        public bool IsValid { get; }
        public string Reason { get; }
        public ValidationReport(bool valid, string reason = "") { IsValid = valid; Reason = reason; }
    }
}
