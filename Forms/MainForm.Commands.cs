using System.Globalization;

using TreadmillController.Helpers;

namespace TreadmillController;

public sealed partial class MainForm
{
    /// <summary>
    /// Manual update button.
    ///
    /// IMPORTANT:
    /// Convert UI speed to KPH
    /// BEFORE sending to treadmill.
    /// </summary>
    private async void BtnUpdate_Click(
        object? sender,
        EventArgs e)
    {
        _btnUpdate.Enabled = false;

        try
        {
            double displaySpeed =
                double.Parse(
                    _txtSpeed.Text,
                    CultureInfo.InvariantCulture);

            //
            // ALWAYS convert to KPH
            //
            double speedKph =
                UnitConverter.ToKph(
                    displaySpeed,
                    UsingMph);

            double incline =
                double.Parse(
                    _txtIncline.Text,
                    CultureInfo.InvariantCulture);

            // Broadcast no longer used for FTMS/TCP; Ignore

            //
            // Send speed
            //
            await _ftms.SendCommandAsync(
                $"{speedKph:F1};-1",
                _txtIp.Text);

            //
            // Required treadmill delay
            //
            await Task.Delay(1200);

            //
            // Send incline
            //
            await _ftms.SendCommandAsync(
                $"-1;{incline:F1}",
                _txtIp.Text);

            _lblStatus.ForeColor =
                Color.DarkGreen;

            _lblStatus.Text =
                $"Updated treadmill";
        }
        catch (Exception ex)
        {
            _lblStatus.ForeColor =
                Color.DarkRed;

            _lblStatus.Text =
                $"Error: {ex.Message}";
        }
        finally
        {
            _btnUpdate.Enabled = true;
        }
    }

    private void UpdateConnectionStatus()
    {
        TimeSpan delta =
            DateTime.Now -
            _telemetry.LastUpdate;

        bool connected =
            delta <= _timeout;

        _lblConnection.Text =
            connected
                ? $"Connection OK ({delta.TotalSeconds:F1}s ago)"
                : $"Connection LOST ({delta.TotalSeconds:F1}s ago)";

        _lblConnection.ForeColor =
            connected
                ? Color.DarkGreen
                : Color.DarkRed;
    }
}