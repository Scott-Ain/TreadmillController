namespace TreadmillController;

public sealed partial class MainForm
{
    private void StartWorkout()
    {
        _elapsedSeconds = 0;
        _distanceKm = 0;

        _lblWorkoutTime.Text =
    "Workout Time: 00:00:00";

        _lblWorkoutDistance.Text =
            UsingMph
                ? "Distance: 0.00 miles"
                : "Distance: 0.00 km";

        UpdateWorkoutProgressLine(0);

        _lastWorkoutTick =
            DateTime.Now;

        _workoutRunning = true;

        _workoutTimer.Start();

        _btnWorkoutToggle.Text =
            "STOP WORKOUT";

        _btnWorkoutToggle.BackColor =
            Color.LightCoral;

        _lblStatus.Text =
            "Workout started";
    }

    private void StopWorkout()
    {
        _workoutRunning = false;

        _workoutTimer.Stop();

        _btnWorkoutToggle.Text =
            "START WORKOUT";

        _btnWorkoutToggle.BackColor =
            Color.LightGreen;

        _lblStatus.Text =
            "Workout stopped";

        UpdateWorkoutProgressLine(0);
    }

    private void BtnWorkoutToggle_Click(
    object? sender,
    EventArgs e)
    {
        if (_workoutRunning)
        {
            StopWorkout();
        }
        else
        {
            StartWorkout();
        }
    }

    private async void WorkoutTimer_Tick(
        object? sender,
        EventArgs e)
    {
        if (!_workoutRunning)
            return;

        DateTime now = DateTime.Now;

        double deltaSeconds =
            (now - _lastWorkoutTick)
            .TotalSeconds;

        _lastWorkoutTick = now;

        _elapsedSeconds +=
            deltaSeconds;

        _distanceKm +=
            (_telemetry.SpeedKph / 3600.0)
            * deltaSeconds;

        double currentX =
            _axisMode == Models.AxisMode.Time
                ? _elapsedSeconds / 60.0
                : _distanceKm;

        //
        // Update runtime labels
        //
        _lblWorkoutTime.Text =
            $"Workout Time: {TimeSpan.FromSeconds(_elapsedSeconds):hh\\:mm\\:ss}";

        double displayDistance =
            UsingMph
                ? _distanceKm * 0.621371
                : _distanceKm;

        string distanceUnit =
            UsingMph
                ? "miles"
                : "km";

        _lblWorkoutDistance.Text =
            $"Distance: {displayDistance:F2} {distanceUnit}";

        //
        // Move chart progress line
        //
        UpdateWorkoutProgressLine(currentX);

        if (currentX >= _xAxisMaximum)
        {
            StopWorkout();
            return;
        }

        double incline =
            _workoutController
                .GetInterpolatedIncline(currentX);

            try
            {
                await _ftms.SendCommandAsync(
                    $"-1;{incline:F1}",
                    _txtIp.Text);
            }
        catch (Exception ex)
        {
            _lblStatus.Text =
                ex.Message;
        }
    }
}