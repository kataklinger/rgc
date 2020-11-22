using System;
using System.Windows.Threading;

namespace mapping.app
{
    sealed class StitchingObserver : Stitcher.IStitchingObserver
    {
        private readonly IStitchingMV _modelView;
        private int _frameNo = 0;

        public StitchingObserver(IStitchingMV modelView)
        {
            _modelView = modelView;
        }

        public void WindowUpdate(int frameNo, Matrix.Matrix<byte> frame, Tuple<Tuple<int, int>, Tuple<int, int>> region, bool final)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                _modelView.Phase = "Window";
                _modelView.FrameNo = frameNo;
                _modelView.FrameImage = frame.ToBitmap();

                var ((top, left), (bottom, right)) = region;
                _modelView.WindowTop = top;
                _modelView.WindowLeft = left;
                _modelView.WindowHeight = bottom - top;
                _modelView.WindowWidth = right - left;
            });
        }

        public void WorldFrame(int frameNo, Matrix.Matrix<byte> frame)
        {
            _frameNo = frameNo;

            var img = frame.ToBitmap();
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                _modelView.Phase = "World";
                _modelView.FrameNo = frameNo;
                _modelView.FrameImage = img;
            });
        }

        public void WorldUpdate(Func<Matrix.Matrix<byte>> getWorld)
        {
            if (_frameNo % _modelView.SkipFrames == 0)
            {
                var img = getWorld().ToBitmap();
                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    _modelView.LastWorldUpdate = _frameNo;
                    _modelView.WorldImage = img;
                });
            }
        }

        public void WorldComplete(Matrix.Matrix<byte> world)
        {
            var img = world.ToBitmap();
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                _modelView.Phase = "Complete";
                _modelView.LastWorldUpdate = _frameNo;
                _modelView.WorldImage = img;
            });
        }
    }
}
