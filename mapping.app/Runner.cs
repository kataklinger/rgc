using Microsoft.FSharp.Core;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace mapping.app
{
    sealed class Runner
    {
        private readonly IStitchingMV _modeView;
        private readonly Stitcher.IStitchingObserver _observer;

        private string _filePath;
        private Task _task;

        private Matrix.Matrix<byte> _plot;

        public Runner(IStitchingMV modeView)
        {
            _modeView = modeView;
            _observer = new StitchingObserver(modeView);
        }

        public event EventHandler StateChanged;

        public bool _isOpened;
        public bool IsOpened { get => _isOpened; }

        private volatile bool _isRunning;
        public bool IsRunning { get => _isRunning; }

        private volatile bool _isStopping;
        public bool IsStopping { get => _isStopping; }

        public bool IsNonEmpty { get => _plot != null; }

        public void Open()
        {
            if (!_isRunning)
            {
                OpenFileDialog fileDialog = new OpenFileDialog
                {
                    Filter = "Gameplay files (*.gpf)|*.gpf|All files (*.*)|*.*"
                };

                if (fileDialog.ShowDialog() == true)
                {
                    using (var archive = ZipFile.OpenRead(fileDialog.FileName))
                    {
                        _modeView.TotalFrames = archive.Entries.Count;
                    }

                    _filePath = fileDialog.FileName;
                    _isOpened = true;
                    RaiseStateChanged();
                }
            }
        }

        public void Save()
        {
            if (!_isRunning && _plot != null)
            {
                SaveFileDialog fileDialog = new SaveFileDialog
                {
                    Filter = "Image files (*.png)|*.png|All files (*.*)|*.*"
                };

                if (fileDialog.ShowDialog() == true)
                {
                    _plot
                        .ToBitmap()
                        .Save(fileDialog.FileName);
                }
            }
        }

        public void Start()
        {
            if (_isOpened && !_isRunning)
            {
                var dispatcher = Dispatcher.CurrentDispatcher;

                _task = Task.Run(() =>
                {
                    var result = Stitcher.loop(_observer, GetFrames());
                    _plot = FSharpOption<Matrix.Matrix<byte>>.get_IsSome(result)
                        ? result.Value
                        : null;

                    dispatcher.Invoke(() =>
                    {
                        _isRunning = false;
                        _isStopping = false;
                        RaiseStateChanged();
                    });
                });

                _isRunning = true;
                RaiseStateChanged();
            }
        }

        public async void Stop()
        {
            if (_isRunning && !_isStopping)
            {
                _isRunning = false;
                _isStopping = true;
                RaiseStateChanged();

                await _task;
                _task = null;
            }
        }

        private IEnumerable<Matrix.Matrix<byte>> GetFrames()
        {
            using var archive = ZipFile.OpenRead(_filePath);
            foreach (var file in archive.Entries.OrderBy(e => e.Name))
            {
                if (!_isRunning)
                {
                    break;
                }

                yield return ReadFrame(file);
            }
        }

        private Matrix.Matrix<byte> ReadFrame(ZipArchiveEntry file)
        {
            using var inStream = file.Open();
            using var outStream = new MemoryStream((int)file.Length);
            inStream.CopyTo(outStream);
            return Image.rawToC64(outStream.ToArray());
        }

        private void RaiseStateChanged() => StateChanged?.Invoke(this, new EventArgs { });
    }
}
