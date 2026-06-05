using System.Globalization;
using System.Net;

using TreadmillController.Helpers;

namespace TreadmillController;

public sealed partial class MainForm
{
    private bool UsingMph =>
        _rbMph.Checked;

    private void RefreshDisplayedUnits()
    {
        if (double.TryParse(
                _txtSpeed.Text,
                out _))
        {
        //    if (_telemetry.SpeedKph > 0)
        //    {
        //        _txtSpeed.Text =
        //            UnitConverter.ToDisplaySpeed(
        //                _telemetry.SpeedKph,
        //                UsingMph)
        //            .ToString("F1");
        //    }
        }

        string unit =
            UsingMph
                ? "mph"
                : "km/h";

        _lblSpeed.Text =
            $"Speed: {UnitConverter.ToDisplaySpeed(_telemetry.SpeedKph, UsingMph):F1} {unit}";

        _chart.XAxes.First().Name =
            _axisMode == Models.AxisMode.Time
                ? "Time (minutes)"
                : UsingMph
                    ? "Distance (miles)"
                    : "Distance (km)";

        _chart.Update();
    }

    private void Udp_ErrorOccurred(
        string error)
    {
        if (!IsHandleCreated)
            return;

        BeginInvoke(() =>
        {
            _lblStatus.ForeColor =
                Color.DarkRed;

            _lblStatus.Text =
                $"Error: {error}";
        });
    }
    private void Ftms_ErrorOccurred(string error)
    {
        if (!IsHandleCreated)
            return;

        BeginInvoke(() =>
        {
            _lblStatus.ForeColor = Color.DarkRed;
            _lblStatus.Text = $"Error: {error}";
        });
    }

    private void Ftms_RawTelemetryReceived(
        string message,
        IPEndPoint endpoint)
    {
        _telemetry.LastUpdate = DateTime.Now;

        if (!IsHandleCreated)
            return;

        BeginInvoke(() =>
        {
            if (_chkAutoDetectIp.Checked)
            {
                _txtIp.Text = endpoint.Address.ToString();
            }

            TelemetryParser.ProcessTelemetry(message, _telemetry);

            RefreshDisplayedUnits();

            _lblIncline.Text = $"Incline: {_telemetry.Incline:F1}%";

            _lblWorkoutDistance.Text = $"Distance: {_telemetry.DistanceMeters:F0} m";

            _lblStatus.ForeColor = Color.Black;
            _lblStatus.Text = $"Telemetry received: {message}";
        });
    }
}