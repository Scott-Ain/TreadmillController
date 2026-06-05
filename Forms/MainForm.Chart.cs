using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

using SkiaSharp;

namespace TreadmillController;

public sealed partial class MainForm
{
    private void ConfigureChart()
    {
        StepLineSeries<ObservablePoint> workoutSeries =
            new()
            {
                Name = "Workout Incline",
                Values = _workoutPoints,

                GeometrySize = 12,

                Stroke =
                    new SolidColorPaint(
                        SKColors.OrangeRed,
                        4),

                Fill = null,

                GeometryStroke =
                    new SolidColorPaint(
                        SKColors.Black,
                        2),

                GeometryFill =
                    new SolidColorPaint(
                        SKColors.Gold)
            };

        StepLineSeries<ObservablePoint> progressSeries =
     new()
     {
         Name = "Progress",

         Values = _progressLine,

         GeometrySize = 0,

         Stroke =
             new SolidColorPaint(
                 SKColors.DeepSkyBlue,
                 3),

         Fill = null
     };

        _chart.Series =
        [
            workoutSeries,
    progressSeries
        ];

        _chart.XAxes =
        [
            new Axis
            {
                Name = "Time (minutes)",
                MinLimit = 0,
                MaxLimit = _xAxisMaximum,
                LabelsRotation = 0
            }
        ];

        _chart.YAxes =
        [
            new Axis
            {
                Name = "Incline %",
                MinLimit = _minIncline,
                MaxLimit = _maxIncline
            }
        ];
    }

    private void ApplyChartSettings()
    {
        _axisMode =
            _cmbAxisMode.SelectedIndex == 0
                ? Models.AxisMode.Time
                : Models.AxisMode.Distance;

        _xAxisMaximum =
            (double)_numXAxis.Value;

        if (_workoutPoints.Count > 0)
        {
            ObservablePoint last =
                _workoutPoints.Last();

            last.X = _xAxisMaximum;
        }

        _minIncline =
            (double)_numMinIncline.Value;

        _maxIncline =
            (double)_numMaxIncline.Value;

        _chart.XAxes = new[]
        {
            new Axis
            {
                Name =
                    _axisMode == Models.AxisMode.Time
                        ? "Time (minutes)"
                        : "Distance (km)",

                MinLimit = 0,
                MaxLimit = _xAxisMaximum
            }
        };

        _chart.YAxes = new[]
        {
            new Axis
            {
                Name = "Incline %",
                MinLimit = _minIncline,
                MaxLimit = _maxIncline
            }
        };

        _chart.Update();
    }

    private void UpdateWorkoutProgressLine(
    double currentX)
    {
        _progressLine.Clear();

        _progressLine.Add(
            new ObservablePoint(
                currentX,
                _minIncline));

        _progressLine.Add(
            new ObservablePoint(
                currentX,
                _maxIncline));

        _chart.Update();
    }
}