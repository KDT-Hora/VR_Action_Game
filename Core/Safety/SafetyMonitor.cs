namespace VrAction.Core.Safety
{
    /// <summary>
    /// Decides when passthrough must be shown (FR-014, FR-027).
    /// Automatic: on within TriggerMm of a boundary/hazard, off again beyond ReleaseMm (hysteresis).
    /// Manual toggle forces it on regardless of distance; safety triggers still apply when manual is off.
    /// </summary>
    public sealed class SafetyMonitor
    {
        readonly int _trigger, _release;
        bool _auto;
        bool _manual;

        public SafetyMonitor(int triggerMm = 300, int releaseMm = 400)
        {
            _trigger = triggerMm; _release = releaseMm;
        }

        public bool PassthroughOn => _manual || _auto;

        public void ToggleManual() { _manual = !_manual; }

        /// <param name="distanceMm">Smallest distance from head/controllers to a boundary or hazard.</param>
        public bool Update(int distanceMm)
        {
            if (distanceMm <= _trigger) _auto = true;
            else if (distanceMm >= _release) _auto = false;
            return PassthroughOn;
        }
    }
}
