using LiveChartsCore.Defaults;

namespace TreadmillController;

public sealed partial class MainForm
{
    /// <summary>
    /// Very simple drag editor.
    ///
    /// User drags nearest point vertically
    /// to set incline.
    /// </summary>
    private void Chart_MouseDown(
        object? sender,
        MouseEventArgs e)
    {
        //
        // Actual chart drawing area
        //
        var drawMargin =
            _chart.CoreChart.DrawMarginLocation;

        var drawSize =
            _chart.CoreChart.DrawMarginSize;

        double chartLeft = drawMargin.X;
        double chartTop = drawMargin.Y;

        double chartWidth = drawSize.Width;
        double chartHeight = drawSize.Height;

        //
        // Ignore clicks outside plot area
        //
        if (e.X < chartLeft ||
            e.X > chartLeft + chartWidth ||
            e.Y < chartTop ||
            e.Y > chartTop + chartHeight)
        {
            return;
        }

        //
        // RIGHT CLICK = delete point
        //
        if (e.Button == MouseButtons.Right)
        {
            ObservablePoint? deletePoint = null;

            foreach (ObservablePoint p in _workoutPoints)
            {
                double px =
                    chartLeft +
                    (((p.X ?? 0) / _xAxisMaximum)
                    * chartWidth);

                double py =
                    chartTop +
                    (
                        (1.0 -
                        (((p.Y ?? 0) - _minIncline)
                        / (_maxIncline - _minIncline)))
                        * chartHeight
                    );

                double dx = e.X - px;
                double dy = e.Y - py;

                double dist =
                    Math.Sqrt(dx * dx + dy * dy);

                if (dist < 14)
                {
                    deletePoint = p;
                    break;
                }
            }

            //
            // Don't delete endpoints
            //
            if (deletePoint != null)
            {
                int idx =
                    _workoutPoints.IndexOf(deletePoint);

                if (idx != 0 &&
                    idx != _workoutPoints.Count - 1)
                {
                    _workoutPoints.Remove(deletePoint);

                    _chart.Update();
                }
            }

            return;
        }

        //
        // Convert mouse -> chart coordinates
        //
        double x =
            ((e.X - chartLeft) / chartWidth)
            * _xAxisMaximum;

        //
        // Y axis inverted
        //
        double normalizedY =
            1.0 -
            ((e.Y - chartTop) / chartHeight);

        double incline =
            _minIncline +
            normalizedY *
            (_maxIncline - _minIncline);

        incline = Math.Clamp(
            incline,
            _minIncline,
            _maxIncline);

        incline = Math.Round(incline);

        //
        // First try selecting existing point
        //
        ObservablePoint? existing = null;

        foreach (ObservablePoint p in _workoutPoints)
        {
            double px =
                chartLeft +
                (((p.X ?? 0) / _xAxisMaximum)
                * chartWidth);

            double py =
                chartTop +
                (
                    (1.0 -
                    (((p.Y ?? 0) - _minIncline)
                    / (_maxIncline - _minIncline)))
                    * chartHeight
                );

            double dx = e.X - px;
            double dy = e.Y - py;

            double dist =
                Math.Sqrt(dx * dx + dy * dy);

            //
            // Hit radius
            //
            if (dist < 14)
            {
                existing = p;
                break;
            }
        }

        //
        // Existing point found:
        // start dragging
        //
        if (existing != null)
        {
            _dragging = true;
            _dragPoint = existing;
            return;
        }

        //
        // Otherwise create a new point
        //
        ObservablePoint newPoint =
            new(x, incline);

        _workoutPoints.Add(newPoint);

        //
        // Keep points sorted
        //
        List<ObservablePoint> sorted =
            _workoutPoints
                .OrderBy(p => p.X ?? 0)
                .ToList();

        _workoutPoints.Clear();

        foreach (ObservablePoint p in sorted)
        {
            _workoutPoints.Add(p);
        }

        _chart.Update();
    }

    private void Chart_MouseMove(
        object? sender,
        MouseEventArgs e)
    {
        if (!_dragging ||
            _dragPoint == null)
            return;

        var drawMargin =
            _chart.CoreChart.DrawMarginLocation;

        var drawSize =
            _chart.CoreChart.DrawMarginSize;

        double chartLeft = drawMargin.X;
        double chartTop = drawMargin.Y;

        double chartWidth = drawSize.Width;
        double chartHeight = drawSize.Height;

        //
        // Convert mouse -> chart X
        //
        double newX =
            ((e.X - chartLeft) / chartWidth)
            * _xAxisMaximum;

        //
        // Convert mouse -> incline
        //
        double normalizedY =
            1.0 -
            ((e.Y - chartTop) / chartHeight);

        double incline =
            _minIncline +
            normalizedY *
            (_maxIncline - _minIncline);

        incline = Math.Clamp(
            incline,
            _minIncline,
            _maxIncline);

        //
        // Integer incline only
        //
        incline = Math.Round(incline);

        //
        // Find current point index
        //
        int index =
            _workoutPoints.IndexOf(_dragPoint);

        //
        // FIRST point:
        // lock X to 0
        //
        if (index == 0)
        {
            newX = 0;
        }
        //
        // LAST point:
        // lock X to workout end
        //
        else if (index ==
                 _workoutPoints.Count - 1)
        {
            newX = _xAxisMaximum;
        }
        else
        {
            //
            // Constrain between neighbors
            //
            ObservablePoint left =
                _workoutPoints[index - 1];

            ObservablePoint right =
                _workoutPoints[index + 1];

            double minX =
                (left.X ?? 0) + 0.1;

            double maxX =
                (right.X ?? 0) - 0.1;

            newX = Math.Clamp(
                newX,
                minX,
                maxX);
        }

        //
        // Apply changes
        //
        _dragPoint.X = newX;
        _dragPoint.Y = incline;

        //
        // Keep points sorted
        //
        List<ObservablePoint> sorted =
            _workoutPoints
                .OrderBy(p => p.X ?? 0)
                .ToList();

        _workoutPoints.Clear();

        foreach (ObservablePoint p in sorted)
        {
            _workoutPoints.Add(p);
        }

        _chart.Update();
    }

    private void Chart_MouseUp(
        object? sender,
        MouseEventArgs e)
    {
        _dragging = false;
        _dragPoint = null;
    }
}