using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace mapping.app
{
    sealed class StartCommand : ICommand
    {
        private readonly Runner _runner;

        public StartCommand(Runner runner)
        {
            _runner = runner;
            _runner.StateChanged += (_, e) => CanExecuteChanged?.Invoke(this, e);
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => _runner.IsOpened && !_runner.IsRunning;

        public void Execute(object parameter)
        {
            _runner.Start();
        }
    }

    sealed class StopCommand : ICommand
    {
        private readonly Runner _state;

        public StopCommand(Runner state)
        {
            _state = state;
            _state.StateChanged += (_, e) => CanExecuteChanged?.Invoke(this, e);
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => _state.IsRunning && !_state.IsStopping;

        public void Execute(object parameter)
        {
            _state.Stop();
        }
    }

    sealed class OpenCommand : ICommand
    {
        private readonly Runner _state;

        public event EventHandler CanExecuteChanged;

        public OpenCommand(Runner state)
        {
            _state = state;
            _state.StateChanged += (_, e) => CanExecuteChanged?.Invoke(this, e);
        }

        public bool CanExecute(object parameter)
        {
            return !_state.IsRunning;
        }

        public void Execute(object parameter)
        {
            _state.Open();
        }
    }

    sealed class SaveCommand : ICommand
    {
        private readonly Runner _state;

        public event EventHandler CanExecuteChanged;

        public SaveCommand(Runner state)
        {
            _state = state;
            _state.StateChanged += (_, e) => CanExecuteChanged?.Invoke(this, e);
        }

        public bool CanExecute(object parameter)
        {
            return !_state.IsRunning && _state.IsNonEmpty;
        }

        public void Execute(object parameter)
        {
            _state.Save();
        }
    }

    sealed class StitchingMV : IStitchingMV, INotifyPropertyChanged
    {
        private readonly Runner _state;

        public StitchingMV()
        {
            _state = new Runner(this);

            Start = new StartCommand(_state);
            Stop = new StopCommand(_state);
            Open = new OpenCommand(_state);
            Save = new SaveCommand(_state);
        }

        public StartCommand Start { get; }
        public StopCommand Stop { get; }
        public OpenCommand Open { get; }
        public SaveCommand Save { get; }

        private int _totalFrames = 0;
        public int TotalFrames
        {
            get => _totalFrames;
            set
            {
                if (_totalFrames != value)
                {
                    _totalFrames = value;
                    OnPropertyChanged(nameof(TotalFrames));
                }
            }
        }

        private string _phase = string.Empty;
        public string Phase
        {
            get => _phase;
            set
            {
                if (_phase != value)
                {
                    WindowVisibility = value == "Window"
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                    _phase = value;
                    OnPropertyChanged(nameof(Phase));
                }
            }
        }

        private int _frameNo = 0;
        public int FrameNo
        {
            get => _frameNo;
            set
            {
                if (_frameNo != value)
                {
                    _frameNo = value;
                    OnPropertyChanged(nameof(FrameNo));
                }
            }
        }

        private BitmapSource _frameImage;

        public BitmapSource FrameImage
        {
            get => _frameImage;
            set
            {
                _frameImage = value;
                OnPropertyChanged(nameof(FrameImage));
            }
        }

        private volatile int _skipFrames = 100;
        public int SkipFrames
        {
            get => _skipFrames;
            set
            {
                if (_skipFrames != value)
                {
                    _skipFrames = value;
                    OnPropertyChanged(nameof(SkipFrames));
                }
            }
        }

        private int _lastWorldUpdate = 0;
        public int LastWorldUpdate
        {
            get => _lastWorldUpdate;
            set
            {
                if (_lastWorldUpdate != value)
                {
                    _lastWorldUpdate = value;
                    OnPropertyChanged(nameof(LastWorldUpdate));
                }
            }
        }

        private BitmapSource _worldImage;
        public BitmapSource WorldImage
        {
            get => _worldImage;
            set
            {
                _worldImage = value;
                OnPropertyChanged(nameof(WorldImage));
            }
        }

        private int _windowTop = 0;
        public int WindowTop
        {
            get => _windowTop;
            set
            {
                if (_windowTop != value)
                {
                    _windowTop = value;
                    OnPropertyChanged(nameof(WindowTop));
                }
            }
        }

        private int _windowLeft = 0;
        public int WindowLeft
        {
            get => _windowLeft;
            set
            {
                if (_windowLeft != value)
                {
                    _windowLeft = value;
                    OnPropertyChanged(nameof(WindowLeft));
                }
            }
        }

        private int _windowHeight = 0;
        public int WindowHeight
        {
            get => _windowHeight;
            set
            {
                if (_windowHeight != value)
                {
                    _windowHeight = value;
                    OnPropertyChanged(nameof(WindowHeight));
                }
            }
        }

        private int _windowWidth = 0;
        public int WindowWidth
        {
            get => _windowWidth;
            set
            {
                if (_windowWidth != value)
                {
                    _windowWidth = value;
                    OnPropertyChanged(nameof(WindowWidth));
                }
            }
        }

        private Visibility _windowVisibility;
        public Visibility WindowVisibility
        {
            get => _windowVisibility;
            private set
            {
                if (_windowVisibility != value)
                {
                    _windowVisibility = value;
                    OnPropertyChanged(nameof(WindowVisibility));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
