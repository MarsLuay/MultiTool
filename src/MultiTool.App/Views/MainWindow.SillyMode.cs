using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;
using MultiTool.App.Localization;
using MultiTool.App.Services;
using MultiTool.App.ViewModels;
using MultiTool.Core.Results;
using MultiTool.Core.Services;

namespace MultiTool.App.Views;

public partial class MainWindow : Window
{
    private void ApplySillyModeState()

    {

        if (!IsLoaded)

        {

            return;

        }



        if (viewModel.IsSillyModeEnabled)

        {

            StartConfetti();

            return;

        }



        StopSillyModeEffects();

    }



    private void StopSillyModeEffects()

    {

        if (confettiTimer is not null)

        {

            confettiTimer.Stop();

        }



        SillyConfettiCanvas.Children.Clear();

        SillyConfettiCanvas.Visibility = Visibility.Collapsed;

    }



    private void StartConfetti()

    {

        if (confettiTimer is not null)

        {

            confettiTimer.Stop();

        }



        SillyConfettiCanvas.Children.Clear();

        SillyConfettiCanvas.Visibility = Visibility.Visible;

        confettiStartedAtUtc = DateTime.UtcNow;

        SpawnConfettiBurst(12, ConfettiDuration);



        confettiTimer = new DispatcherTimer

        {

            Interval = TimeSpan.FromMilliseconds(120),

        };

        confettiTimer.Tick += ConfettiTimer_OnTick;

        confettiTimer.Start();

    }



    private void ConfettiTimer_OnTick(object? sender, EventArgs e)

    {

        if (!viewModel.IsSillyModeEnabled)

        {

            confettiTimer?.Stop();

            confettiTimer = null;

            return;

        }



        var elapsed = DateTime.UtcNow - confettiStartedAtUtc;

        if (elapsed >= ConfettiDuration)

        {

            confettiTimer?.Stop();

            confettiTimer = null;

            return;

        }



        // Spawn denser waves near the middle for a fuller cascade feel.

        var progress = elapsed.TotalMilliseconds / ConfettiDuration.TotalMilliseconds;

        var waveSize = progress is >= 0.25 and <= 0.75 ? 10 : 6;

        SpawnConfettiBurst(waveSize, ConfettiDuration - elapsed + TimeSpan.FromMilliseconds(300));

    }



    private void SpawnConfettiBurst(int pieceCount, TimeSpan duration)

    {

        var width = Math.Max(SillyConfettiCanvas.ActualWidth, MainRootGrid.ActualWidth);

        var height = Math.Max(SillyConfettiCanvas.ActualHeight, MainRootGrid.ActualHeight);

        if (width <= 0 || height <= 0)

        {

            return;

        }



        for (var index = 0; index < pieceCount; index++)

        {

            var piece = new System.Windows.Shapes.Rectangle

            {

                Width = random.Next(6, 14),

                Height = random.Next(6, 14),

                RadiusX = 1.5,

                RadiusY = 1.5,

                Fill = ConfettiBrushes[random.Next(ConfettiBrushes.Length)],

                Opacity = 0.95,

                IsHitTestVisible = false,

                RenderTransformOrigin = new System.Windows.Point(0.5, 0.5),

                RenderTransform = new RotateTransform(random.Next(0, 360)),

            };



            SillyConfettiCanvas.Children.Add(piece);



            var startX = random.NextDouble() * Math.Max(1, width - 20);

            var endX = Math.Clamp(startX + random.Next(-120, 121), 0, Math.Max(0, width - piece.Width));



            Canvas.SetLeft(piece, startX);

            Canvas.SetTop(piece, -20);



            var fallAnimation = new DoubleAnimation

            {

                From = -20,

                To = height + 30,

                Duration = duration,

            };



            var driftAnimation = new DoubleAnimation

            {

                From = startX,

                To = endX,

                Duration = duration,

            };



            fallAnimation.Completed += (_, _) => SillyConfettiCanvas.Children.Remove(piece);



            piece.BeginAnimation(Canvas.TopProperty, fallAnimation);

            piece.BeginAnimation(Canvas.LeftProperty, driftAnimation);

        }

    }





}
