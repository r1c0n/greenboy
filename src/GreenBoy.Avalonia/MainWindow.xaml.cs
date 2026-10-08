using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GreenBoy.controller;
using GreenBoy.gui;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Button = GreenBoy.controller.Button;

namespace GreenBoy.Avalonia
{
    public class MainWindow : Window, IController, IDisposable
    {
        #region Private fields

        private bool isDisposed;
        private readonly object _updateLock = new object();

        private IButtonListener _listener;
        private Dictionary<Key, Button> _controls;
        private byte[] _lastFrame;
        private readonly Emulator _emulator;
        private readonly GameboyOptions _gameboyOptions;

        #endregion

        #region Constructor

        public MainWindow()
        {
            Opened += OnWindowOpenedBindWindowEvents;
            InitializeComponent();
            BuildMenuViewModel();
            BindKeysToButtons();
            AdjustEmulatorScreenSize();

            _gameboyOptions = new GameboyOptions();
            _emulator = new Emulator(_gameboyOptions) { Display = new BitmapDisplay() };

            ConnectEmulatorToUI();
        }

        private void OnWindowOpenedBindWindowEvents(object sender, EventArgs e)
        {
            PropertyChanged += OnWindowSizeChanged;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void BuildMenuViewModel()
        {
            var vm = new MainWindowViewModel();

            vm.MenuItems = new[]
                {
                new MenuItemViewModel()
                {
                    Header = "_Emulator",
                    Items = new[]
                    {
                        new MenuItemViewModel()
                        {
                            Header = "_Load ROM",
                            Command = ReactiveCommand.CreateFromTask(LoadROM)
                        },
                        new MenuItemViewModel()
                        {
                            Header = "_Pause",
                            Command = ReactiveCommand.Create(Pause)
                        },
                        new MenuItemViewModel()
                        {
                            Header = "_Quit",
                            Command = ReactiveCommand.Create(Quit)
                        }
                    }
                },
                new MenuItemViewModel()
                {
                    Header = "Graphics",
                    Items = new[]
                    {
                        new MenuItemViewModel()
                        {
                            Header = "Screenshot",
                            Command = ReactiveCommand.CreateFromTask(Screenshot)
                        }
                    }
                }
            };

            DataContext = vm;
        }

        private void BindKeysToButtons()
        {
            _controls = new Dictionary<Key, Button>
            {
                {Key.Left, Button.Left},
                {Key.Right, Button.Right},
                {Key.Up, Button.Up},
                {Key.Down, Button.Down},
                {Key.Z, Button.A},
                {Key.X, Button.B},
                {Key.Enter, Button.Start},
                {Key.Back, Button.Select}
            };
        }

        private void AdjustEmulatorScreenSize()
        {
            var imageBox = this.FindControl<Image>("ImageBox");
            if (imageBox != null)
            {
                imageBox.Width = BitmapDisplay.DisplayWidth * 5;
                imageBox.Height = BitmapDisplay.DisplayHeight * 5;

                MinHeight = imageBox.Height + 25;
                MinWidth = imageBox.Width;

                Height = imageBox.Height + 25;
                Width = imageBox.Width;
            }
        }

        private void ConnectEmulatorToUI()
        {
            _emulator.Controller = this;
            _emulator.Display.OnFrameProduced += UpdateDisplay;

            KeyDown += EmulatorSurface_KeyDown;
            KeyUp += EmulatorSurface_KeyUp;
            Closed += (_, e) => Dispose();
        }

        #endregion

        #region Menu Items command methods

        private async Task LoadROM()
        {
            var results = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Load Game Boy ROM",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Game Boy ROMs") { Patterns = new[] { "*.gb", "*.gbc" } },
                    FilePickerFileTypes.All
                }
            });

            using var romFile = results.FirstOrDefault();
            var romPath = romFile?.TryGetLocalPath();
            if (isDisposed || string.IsNullOrWhiteSpace(romPath)) return;

            var previousRom = _gameboyOptions.Rom;
            var previousDisplay = _emulator.Display;
            var display = new BitmapDisplay();
            display.OnFrameProduced += UpdateDisplay;
            try
            {
                _gameboyOptions.Rom = romPath;
                _emulator.Display = display;
                _emulator.Run(CancellationToken.None);
                previousDisplay.OnFrameProduced -= UpdateDisplay;
            }
            catch (Exception exception) when (exception is IOException || exception is InvalidDataException || exception is UnauthorizedAccessException ||
                exception is ArgumentException || exception is InvalidOperationException || exception is TimeoutException)
            {
                _gameboyOptions.Rom = previousRom;
                _emulator.Display = previousDisplay;
                display.OnFrameProduced -= UpdateDisplay;
                await ShowLoadError(exception.Message);
            }
        }

        private async Task ShowLoadError(string message)
        {
            var closeButton = new global::Avalonia.Controls.Button { Content = "OK" };
            var dialog = new Window
            {
                Title = "Unable to load ROM",
                SizeToContent = SizeToContent.WidthAndHeight,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Thickness(16),
                    Spacing = 12,
                    Width = 400,
                    Children =
                    {
                        new TextBlock { Text = message, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap },
                        closeButton
                    }
                }
            };
            closeButton.Click += (_, __) => dialog.Close();
            await dialog.ShowDialog(this);
        }

        private void Pause()
        {
            _emulator.TogglePause();
        }

        private void Quit()
        {
            Close();
        }

        private async Task Screenshot()
        {
            byte[] frame;
            lock (_updateLock)
            {
                frame = _lastFrame;
            }
            if (frame == null) return;

            using var screenshotFile = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save screenshot",
                DefaultExtension = "bmp",
                SuggestedFileName = "GreenBoy.bmp",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Bitmap") { Patterns = new[] { "*.bmp" } }
                }
            });
            if (isDisposed || screenshotFile == null) return;

            await using var stream = await screenshotFile.OpenWriteAsync();
            stream.SetLength(0);
            await stream.WriteAsync(frame);
        }

        #endregion

        #region Emulator events

        private void OnWindowSizeChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name.Equals("ClientSize", StringComparison.CurrentCulture))
            {
                var imageBox = this.FindControl<Image>("ImageBox");
                if (imageBox != null)
                {
                    imageBox.Width = Width;
                    imageBox.Height = Height - 25;
                }
            }
        }

        public void UpdateDisplay(object sender, byte[] frame)
        {
            if (isDisposed) return;
            Dispatcher.UIThread.Post(() =>
            {
                if (isDisposed || (sender != null && !ReferenceEquals(sender, _emulator.Display))) return;
                lock (_updateLock) _lastFrame = frame;
                using var memoryStream = new MemoryStream(frame);

                var imageBox = this.FindControl<Image>("ImageBox");
                if (imageBox != null)
                {
                    var previousFrame = imageBox.Source as IDisposable;
                    imageBox.Source = new Bitmap(memoryStream);
                    previousFrame?.Dispose();
                }
            });
        }

        private void EmulatorSurface_KeyDown(object sender, KeyEventArgs e)
        {
            var button = _controls.ContainsKey(e.Key) ? _controls[e.Key] : null;
            if (button != null)
            {
                _listener?.OnButtonPress(button);
            }
        }

        private void EmulatorSurface_KeyUp(object sender, KeyEventArgs e)
        {
            var button = _controls.ContainsKey(e.Key) ? _controls[e.Key] : null;
            if (button != null)
            {
                _listener?.OnButtonRelease(button);
            }
        }

        #endregion

        #region IController methods

        public void SetButtonListener(IButtonListener listener) => _listener = listener;

        #endregion

        #region IDisposable pattern

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;
            isDisposed = true;

            if (disposing)
            {
                _emulator.Dispose();
                _emulator.Display.OnFrameProduced -= UpdateDisplay;
                var imageBox = this.FindControl<Image>("ImageBox");
                if (imageBox != null)
                {
                    var frame = imageBox.Source as IDisposable;
                    imageBox.Source = null;
                    frame?.Dispose();
                }
                lock (_updateLock) _lastFrame = null;
            }
        }

        #endregion
    }
}
