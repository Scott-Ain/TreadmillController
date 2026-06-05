using LiveChartsCore.Defaults;

using TreadmillController.Models;

namespace TreadmillController.Controllers;

public sealed class WorkoutController
{
    private readonly IList<ObservablePoint> _workoutPoints;

    public WorkoutController(
        IList<ObservablePoint> workoutPoints)
    {
        _workoutPoints = workoutPoints;
    }

    /// <summary>
    /// Linear interpolation between
    /// workout profile points.
    /// </summary>
    public double GetInterpolatedIncline(
        double x)
    {
        if (_workoutPoints.Count == 0)
            return 0;

        double currentIncline =
            _workoutPoints[0].Y ?? 0;

        foreach (ObservablePoint p in _workoutPoints)
        {
            if ((p.X ?? 0) <= x)
            {
                currentIncline =
                    p.Y ?? 0;
            }
            else
            {
                break;
            }
        }

        return currentIncline;
    }

    public void BuildDefaultWorkout(
        double xAxisMaximum)
    {
        _workoutPoints.Clear();

        _workoutPoints.Add(
            new ObservablePoint(0, 0));

        _workoutPoints.Add(
            new ObservablePoint(
                xAxisMaximum,
                0));
    }
}