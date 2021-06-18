using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using ATL;


namespace Cajonic.Services
{
    public static class BitmapHelper
    {
        public static BitmapImage LoadImage(IList<PictureInfo> embeddedPictures)
        {
            if (embeddedPictures.Count == 0)
            {
                return null;
            }

            byte[] imageData = embeddedPictures[0].PictureData;
            if (imageData == null || imageData.Length == 0)
            {
                return null;
            }

            BitmapImage image = new();
            using (MemoryStream mem = new(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }

        public static Bitmap LoadPicture(IList<PictureInfo> embeddedPictures)
        {
            if (embeddedPictures.Count == 0)
            {
                return null;
            }

            byte[] imageData = embeddedPictures[0].PictureData;
            if (imageData == null || imageData.Length == 0)
            {
                return null;
            }

            using MemoryStream memoryStream = new(imageData);
            Bitmap image = new(memoryStream);
            return image;
        }

        public static BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using MemoryStream memory = new();
            bitmap.Save(memory, ImageFormat.Bmp);
            memory.Position = 0;
            BitmapImage bitmapImage = new();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();

            return bitmapImage;
        }

        public static byte[] ConvertToBytes(BitmapImage bitmapImage)
        {
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
            using MemoryStream ms = new();
            encoder.Save(ms);

            return ms.ToArray();
        }

        public static BitmapImage ResizeImage(Image image, int width, int height)
        {
            Rectangle destRect = new(0, 0, width, height);
            Bitmap destImage = new(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using Graphics graphics = Graphics.FromImage(destImage);
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using ImageAttributes wrapMode = new();
            wrapMode.SetWrapMode(WrapMode.TileFlipXY);
            graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);

            return BitmapToImageSource(destImage);
        }
    }
}
