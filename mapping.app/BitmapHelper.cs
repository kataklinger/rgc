using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace mapping.app
{
    public static class BitmapHelper
    {
        public static BitmapSource ToBitmap(this Matrix.Matrix<byte> raw)
        {
            var img = Image.c64ToRgb(raw);
            unsafe
            {
                fixed (int* buffer = img.Rep)
                {
                    var bitmap = BitmapSource.Create(img.Width, img.Height, 96, 96, PixelFormats.Bgr32, null, (IntPtr)buffer, 4 * img.Width * img.Height, 4 * img.Width);
                    bitmap.Freeze();

                    return bitmap;
                }
            }
        }

        public static void Save(this BitmapSource image, string filePath)
        {
            using var fileStream = new FileStream(filePath, FileMode.Create);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            encoder.Save(fileStream);
        }
    }
}
