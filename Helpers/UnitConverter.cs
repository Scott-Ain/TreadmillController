using TreadmillController.Constants;

namespace TreadmillController.Helpers;

public static class UnitConverter
{
    public static double ToDisplaySpeed(
        double kph,
        bool usingMph)
    {
        return usingMph
            ? kph * UnitConstants.KphToMph
            : kph;
    }

    public static double ToKph(
        double display,
        bool usingMph)
    {
        return usingMph
            ? display / UnitConstants.KphToMph
            : display;
    }
}