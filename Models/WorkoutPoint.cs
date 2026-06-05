namespace TreadmillController.Models;

/// <summary>
/// User editable workout point.
/// X = time or distance
/// Y = incline
/// </summary>
public sealed class WorkoutPoint
{
    public double X { get; set; }

    public double Incline { get; set; }
}