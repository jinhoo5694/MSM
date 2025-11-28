using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace MSM
{
    public partial class ScreensaverWindow : Window
    {
        private readonly List<FloatingPhoto> _photos = new();
        private readonly DispatcherTimer _animationTimer;
        private readonly Random _random = new();
        private readonly string[] _imagePaths;
        private bool _isClosing;

        private class FloatingPhoto
        {
            public Image ImageControl { get; set; } = null!;
            public double X { get; set; }
            public double Y { get; set; }
            public double VelocityX { get; set; }
            public double VelocityY { get; set; }
            public double Rotation { get; set; }
            public double RotationSpeed { get; set; }
            public double Scale { get; set; }
        }

        public ScreensaverWindow()
        {
            InitializeComponent();

            // Get screensaver images
            var screensaverDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "screensaver");
            if (Directory.Exists(screensaverDir))
            {
                _imagePaths = Directory.GetFiles(screensaverDir, "*.jpeg")
                    .Concat(Directory.GetFiles(screensaverDir, "*.jpg"))
                    .Concat(Directory.GetFiles(screensaverDir, "*.png"))
                    .ToArray();
            }
            else
            {
                _imagePaths = Array.Empty<string>();
            }

            // Setup animation timer
            _animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
            };
            _animationTimer.Tick += OnAnimationTick;

            // Close on any input
            PointerPressed += OnInteraction;
            PointerMoved += OnPointerMoved;
            KeyDown += OnInteraction;

            Opened += OnWindowOpened;
            Closed += OnWindowClosed;
        }

        private Point? _lastPointerPosition;

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            var currentPosition = e.GetPosition(this);

            if (_lastPointerPosition.HasValue)
            {
                var distance = Math.Sqrt(
                    Math.Pow(currentPosition.X - _lastPointerPosition.Value.X, 2) +
                    Math.Pow(currentPosition.Y - _lastPointerPosition.Value.Y, 2));

                // Only close if mouse moved significantly (more than 20 pixels)
                if (distance > 20)
                {
                    CloseScreensaver();
                }
            }

            _lastPointerPosition = currentPosition;
        }

        private void OnInteraction(object? sender, EventArgs e)
        {
            CloseScreensaver();
        }

        private void CloseScreensaver()
        {
            if (!_isClosing)
            {
                _isClosing = true;
                _animationTimer.Stop();
                Close();
            }
        }

        private void OnWindowOpened(object? sender, EventArgs e)
        {
            InitializePhotos();
            _animationTimer.Start();
        }

        private void OnWindowClosed(object? sender, EventArgs e)
        {
            _animationTimer.Stop();

            // Dispose bitmaps
            foreach (var photo in _photos)
            {
                if (photo.ImageControl.Source is Bitmap bitmap)
                {
                    bitmap.Dispose();
                }
            }
            _photos.Clear();
        }

        private void InitializePhotos()
        {
            if (_imagePaths.Length == 0) return;

            var canvas = this.FindControl<Canvas>("PhotoCanvas");
            if (canvas == null) return;

            // Create 8-12 floating photos
            var photoCount = Math.Min(_imagePaths.Length, _random.Next(8, 13));
            var selectedImages = _imagePaths.OrderBy(_ => _random.Next()).Take(photoCount).ToList();

            foreach (var imagePath in selectedImages)
            {
                try
                {
                    var bitmap = new Bitmap(imagePath);
                    var image = new Image
                    {
                        Source = bitmap,
                        Width = 200 + _random.Next(100), // Random size between 200-300
                        Stretch = Stretch.Uniform
                    };

                    // Add subtle border/shadow effect via a container
                    var border = new Border
                    {
                        Child = image,
                        BorderBrush = Brushes.White,
                        BorderThickness = new Thickness(4),
                        CornerRadius = new CornerRadius(8),
                        BoxShadow = new BoxShadows(new BoxShadow
                        {
                            Blur = 20,
                            Color = Color.FromArgb(100, 0, 0, 0),
                            OffsetX = 0,
                            OffsetY = 5
                        }),
                        ClipToBounds = true,
                        Background = Brushes.White
                    };

                    var photo = new FloatingPhoto
                    {
                        ImageControl = new Image { Width = 1, Height = 1 }, // Placeholder, we use border
                        X = _random.NextDouble() * (Bounds.Width > 0 ? Bounds.Width - 300 : 800),
                        Y = _random.NextDouble() * (Bounds.Height > 0 ? Bounds.Height - 300 : 600),
                        VelocityX = (_random.NextDouble() - 0.5) * 2, // -1 to 1
                        VelocityY = (_random.NextDouble() - 0.5) * 2,
                        Rotation = _random.NextDouble() * 30 - 15, // -15 to 15 degrees
                        RotationSpeed = (_random.NextDouble() - 0.5) * 0.5,
                        Scale = 0.8 + _random.NextDouble() * 0.4 // 0.8 to 1.2
                    };

                    // Store border reference in a custom way
                    border.Tag = photo;
                    photo.ImageControl = image;

                    Canvas.SetLeft(border, photo.X);
                    Canvas.SetTop(border, photo.Y);

                    var transformGroup = new TransformGroup();
                    transformGroup.Children.Add(new RotateTransform(photo.Rotation));
                    transformGroup.Children.Add(new ScaleTransform(photo.Scale, photo.Scale));
                    border.RenderTransform = transformGroup;
                    border.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

                    canvas.Children.Add(border);
                    _photos.Add(photo);
                }
                catch
                {
                    // Skip invalid images
                }
            }
        }

        private void OnAnimationTick(object? sender, EventArgs e)
        {
            var canvas = this.FindControl<Canvas>("PhotoCanvas");
            if (canvas == null) return;

            var width = Bounds.Width;
            var height = Bounds.Height;

            foreach (var child in canvas.Children)
            {
                if (child is Border border && border.Tag is FloatingPhoto photo)
                {
                    // Update position
                    photo.X += photo.VelocityX;
                    photo.Y += photo.VelocityY;

                    // Bounce off edges
                    var photoWidth = border.Bounds.Width * photo.Scale;
                    var photoHeight = border.Bounds.Height * photo.Scale;

                    if (photo.X <= 0 || photo.X + photoWidth >= width)
                    {
                        photo.VelocityX *= -1;
                        photo.X = Math.Max(0, Math.Min(photo.X, width - photoWidth));
                    }

                    if (photo.Y <= 0 || photo.Y + photoHeight >= height)
                    {
                        photo.VelocityY *= -1;
                        photo.Y = Math.Max(0, Math.Min(photo.Y, height - photoHeight));
                    }

                    // Update rotation
                    photo.Rotation += photo.RotationSpeed;

                    // Apply transforms
                    Canvas.SetLeft(border, photo.X);
                    Canvas.SetTop(border, photo.Y);

                    if (border.RenderTransform is TransformGroup tg && tg.Children.Count >= 1)
                    {
                        if (tg.Children[0] is RotateTransform rt)
                        {
                            rt.Angle = photo.Rotation;
                        }
                    }
                }
            }
        }
    }
}
