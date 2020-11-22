using System.Windows.Media.Imaging;

namespace mapping.app
{
    interface IStitchingMV
    {
        int TotalFrames { get; set; }
        string Phase { get; set; }
        int FrameNo { get; set; }

        BitmapSource FrameImage { get; set; }

        int SkipFrames { get; set; }

        int LastWorldUpdate { get; set; }

        BitmapSource WorldImage { get; set; }

        int WindowTop { get; set; }
        int WindowLeft { get; set; }
        int WindowHeight { get; set; }
        int WindowWidth { get; set; }
    }
}
