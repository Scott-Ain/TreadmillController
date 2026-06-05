namespace TreadmillController.Models;

/// <summary>
/// Current treadmill telemetry snapshot.
/// IMPORTANT:
/// All internal storage is ALWAYS in KM/H.
/// UI may display MPH.
/// </summary>
public sealed class TelemetryState
{
    public double SpeedKph { get; set; }

    public double Incline { get; set; }

    // Distance in meters
    public double DistanceMeters { get; set; }

    public DateTime LastUpdate { get; set; } =
        DateTime.Now;
}