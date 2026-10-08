using GreenBoy.controller;
using GreenBoy.gui;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Button = GreenBoy.controller.Button;

namespace GreenBoy.Windows
{
    public partial class WinFormsEmulatorSurface : Form, IController
    {
        private IButtonListener _listener;

        private readonly MenuStrip _menu;
        private readonly BitmapDisplayControl _display;
        private readonly Dictionary<Keys, Button> _controls;

        private readonly Emulator _emulator;
        private readonly GameboyOptions _gameboyOptions;

        public WinFormsEmulatorSurface()
        {
            InitializeComponent();

            Controls.Add(_display = new BitmapDisplayControl
            {
                BackColor = Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(230)))), ((int)(((byte)(248)))), ((int)(((byte)(218))))),
                DisplayEnabled = false,
                Dock = DockStyle.Fill,
                Location = new Point(0, 44),
                Size = new Size(800, 720)
            });

            Controls.Add(_menu = new MenuStrip
            {
                Items =
                {
                    new ToolStripMenuItem("Emulator")
                    {
                        DropDownItems =
                        {
                            new ToolStripMenuItem("Load ROM", null, (sender, args) => { StartEmulation(); }),
                            new ToolStripMenuItem("Pause", null, (sender, args) => { _emulator.TogglePause(); }),
                            new ToolStripMenuItem("Quit", null, (sender, args) => { Close(); })
                        }
                    },
                    new ToolStripMenuItem("Graphics")
                    {
                        DropDownItems =
                        {
                            new ToolStripMenuItem("Screenshot", null, (sender, args) => { Screenshot(); })
                        }
                    }
                }
            });

            _controls = new Dictionary<Keys, Button>
            {
                {Keys.Left, Button.Left},
                {Keys.Right, Button.Right},
                {Keys.Up, Button.Up},
                {Keys.Down, Button.Down},
                {Keys.Z, Button.A},
                {Keys.X, Button.B},
                {Keys.Enter, Button.Start},
                {Keys.Back, Button.Select}
            };

            AutoScaleDimensions = new SizeF(192F, 192F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(_display.Width, _display.Height + _menu.Height);

            _gameboyOptions = new GameboyOptions();
            _emulator = new Emulator(_gameboyOptions)
            {
                Display = _display
            };

            ConnectEmulatorToPanel();
        }

        private void ConnectEmulatorToPanel()
        {
            _emulator.Controller = this;

            KeyDown += WinFormsEmulatorSurface_KeyDown;
            KeyUp += WinFormsEmulatorSurface_KeyUp;
        }

        private void StartEmulation()
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = "Gameboy ROMs (*.gb;*.gbc)|*.gb;*.gbc|Gameboy ROM (*.gb)|*.gb|Gameboy Color ROM (*.gbc)|*.gbc|All files (*.*)|*.*",
                FilterIndex = 0,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog(this) != DialogResult.OK || IsDisposed) return;
            var previousRom = _gameboyOptions.Rom;
            try
            {
                _gameboyOptions.Rom = openFileDialog.FileName;
                _emulator.Run(CancellationToken.None);
            }
            catch (Exception exception) when (exception is IOException || exception is InvalidDataException || exception is UnauthorizedAccessException ||
                exception is ArgumentException || exception is InvalidOperationException || exception is TimeoutException)
            {
                _gameboyOptions.Rom = previousRom;
                MessageBox.Show(this, exception.Message, "Unable to load ROM", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Screenshot()
        {
            _emulator.TogglePause();

            using var sfd = new SaveFileDialog
            {
                Filter = "Bitmap (*.bmp)|*.bmp",
                FilterIndex = 0,
                RestoreDirectory = true
            };

            var (success, romPath) = sfd.ShowDialog() == DialogResult.OK
                ? (true, sfd.FileName)
                : (false, null);

            if (success)
            {
                _display.SaveLastFrame(sfd.FileName);
            }

            _emulator.TogglePause();
        }

        private void WinFormsEmulatorSurface_KeyDown(object sender, KeyEventArgs e)
        {
            var button = _controls.ContainsKey(e.KeyCode) ? _controls[e.KeyCode] : null;
            if (button != null)
            {
                _listener?.OnButtonPress(button);
            }
        }

        private void WinFormsEmulatorSurface_KeyUp(object sender, KeyEventArgs e)
        {
            var button = _controls.ContainsKey(e.KeyCode) ? _controls[e.KeyCode] : null;
            if (button != null)
            {
                _listener?.OnButtonRelease(button);
            }
        }

        public void SetButtonListener(IButtonListener listener) => _listener = listener;

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _emulator.Dispose();
            base.OnFormClosed(e);
        }
    }
}
