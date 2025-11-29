using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using SkiaSharp;

namespace MSM
{
    public partial class ScreensaverWindow : Window
    {
        private readonly DispatcherTimer _slideshowTimer;
        private readonly string[] _imagePaths;
        private int _currentImageIndex;
        private Bitmap? _currentBitmap;
        private bool _isClosing;
        private Point? _lastPointerPosition;

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

            // Shuffle images
            var random = new Random();
            _imagePaths = _imagePaths.OrderBy(_ => random.Next()).ToArray();

            // Setup slideshow timer - 10 seconds per image
            _slideshowTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _slideshowTimer.Tick += OnSlideshowTick;

            // Close on any input
            PointerPressed += OnInteraction;
            PointerMoved += OnPointerMoved;
            KeyDown += OnInteraction;

            Opened += OnWindowOpened;
            Closed += OnWindowClosed;
        }

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
                _slideshowTimer.Stop();
                Close();
            }
        }

        private void OnWindowOpened(object? sender, EventArgs e)
        {
            ShowCurrentImage();
            _slideshowTimer.Start();
        }

        private void OnWindowClosed(object? sender, EventArgs e)
        {
            _slideshowTimer.Stop();
            _currentBitmap?.Dispose();
            _currentBitmap = null;
        }

        private void ShowCurrentImage()
        {
            if (_imagePaths.Length == 0) return;

            var imageControl = this.FindControl<Image>("ScreensaverImage");
            if (imageControl == null) return;

            try
            {
                // Dispose previous bitmap
                _currentBitmap?.Dispose();

                // Load image with EXIF orientation correction
                _currentBitmap = LoadImageWithExifOrientation(_imagePaths[_currentImageIndex]);
                imageControl.Source = _currentBitmap;
            }
            catch
            {
                // Skip invalid images, try next
                _currentImageIndex = (_currentImageIndex + 1) % _imagePaths.Length;
            }
        }

        private Bitmap? LoadImageWithExifOrientation(string path)
        {
            using var stream = File.OpenRead(path);
            using var codec = SKCodec.Create(stream);
            if (codec == null) return new Bitmap(path);

            var origin = codec.EncodedOrigin;
            using var original = SKBitmap.Decode(codec);
            if (original == null) return new Bitmap(path);

            SKBitmap rotated;
            switch (origin)
            {
                case SKEncodedOrigin.RightTop: // 90 degrees clockwise
                    rotated = new SKBitmap(original.Height, original.Width);
                    using (var canvas = new SKCanvas(rotated))
                    {
                        canvas.Translate(rotated.Width, 0);
                        canvas.RotateDegrees(90);
                        canvas.DrawBitmap(original, 0, 0);
                    }
                    break;

                case SKEncodedOrigin.BottomRight: // 180 degrees
                    rotated = new SKBitmap(original.Width, original.Height);
                    using (var canvas = new SKCanvas(rotated))
                    {
                        canvas.Translate(rotated.Width, rotated.Height);
                        canvas.RotateDegrees(180);
                        canvas.DrawBitmap(original, 0, 0);
                    }
                    break;

                case SKEncodedOrigin.LeftBottom: // 90 degrees counter-clockwise
                    rotated = new SKBitmap(original.Height, original.Width);
                    using (var canvas = new SKCanvas(rotated))
                    {
                        canvas.Translate(0, rotated.Height);
                        canvas.RotateDegrees(-90);
                        canvas.DrawBitmap(original, 0, 0);
                    }
                    break;

                default:
                    // No rotation needed
                    rotated = original.Copy();
                    break;
            }

            // Convert SKBitmap to Avalonia Bitmap
            using var image = SKImage.FromBitmap(rotated);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var memStream = new MemoryStream();
            data.SaveTo(memStream);
            memStream.Seek(0, SeekOrigin.Begin);

            rotated.Dispose();
            return new Bitmap(memStream);
        }

        private void OnSlideshowTick(object? sender, EventArgs e)
        {
            // Move to next image
            _currentImageIndex = (_currentImageIndex + 1) % _imagePaths.Length;
            ShowCurrentImage();
        }
    }
}
