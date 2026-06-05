using System.Globalization;

using TreadmillController.Models;

namespace TreadmillController.Helpers;

public static class TelemetryParser
{
    /// <summary>
    /// IMPORTANT:
    /// Treadmill telemetry is ALWAYS KM/H.
    /// </summary>
    public static void ProcessTelemetry(
        string message,
        TelemetryState telemetry)
    {
        //
        // Speed
        //
        if (message.Contains(
            "Changed KPH",
            StringComparison.OrdinalIgnoreCase))
        {
            int idx =
                message.IndexOf(
                    "to:",
                    StringComparison.OrdinalIgnoreCase);

            if (idx >= 0)
            {
                string value =
                    message[(idx + 3)..].Trim();

                if (double.TryParse(
                    value,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double kph))
                {
                    telemetry.SpeedKph = kph;
                }
            }
        }

        //
        // Incline
        //
        if (message.Contains(
            "Changed Grade",
            StringComparison.OrdinalIgnoreCase))
        {
            int idx =
                message.IndexOf(
                    "to:",
                    StringComparison.OrdinalIgnoreCase);

            if (idx >= 0)
            {
                string value =
                    message[(idx + 3)..].Trim();

                if (double.TryParse(
                    value,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double incline))
                {
                    telemetry.Incline = incline;
                }
            }
        }

        //
        // Distance
        //
        if (message.Contains(
            "Distance to",
            StringComparison.OrdinalIgnoreCase))
        {
            int idx =
                message.IndexOf(
                    ":",
                    StringComparison.OrdinalIgnoreCase);

            if (idx >= 0)
            {
                string value =
                    message[(idx + 1)..].Trim();

                if (double.TryParse(
                    value,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double meters))
                {
                    telemetry.DistanceMeters = meters;
                }
            }
        }
    }
}