using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer
{
    internal class ImageHandler
    {
        public bool TryCropImage(Bitmap source, out Bitmap result, int x, int y, int height, int width)
        {
            result = null;
            //normalize X and Y, so width and height are positive
            if (width < 0)
            {
                x += width;
                width = -width;
            }

            if (height < 0)
            {
                y += height;
                height = -height;
            }

            //no intersections with source image 
            if (x > source.Width || y > source.Height || (x + width) < 0 || (y + height) < 0)
                return false;

            //coords of result image
            int trueX, trueY, trueWidth, trueHeigth;
            if (x < 0)
            {
                trueX = 0;
                trueWidth = width + x < source.Width ? width + x : source.Width;
            }
            else
            {
                trueX = x;
                trueWidth = x + width > source.Width - x ? source.Width - x : width;
            }

            if (y < 0)
            {
                trueY = 0;
                trueHeigth = height + y < source.Height ? height + y : source.Height;
            }
            else
            {
                trueY = y;
                trueHeigth = y + height > source.Height - y ? source.Height - y : height;
            }

            // An empty bitmap which will hold the cropped image
            result = new Bitmap(trueWidth, trueHeigth);

            Graphics g = Graphics.FromImage(result);

            // Draw the given area (section) of the source image
            // at location 0,0 on the empty bitmap (bmp)
            g.DrawImage(source, 0, 0, new Rectangle(trueX, trueY, trueWidth, trueHeigth), GraphicsUnit.Pixel);

            return true;
        }

        public void ApplySepia(Bitmap processedBitmap)
        {
            ProcessPixelsByAction(processedBitmap, new SepiaPixelAction());
        }
        public void ApplyGrayscale(Bitmap processedBitmap)
        {
            ProcessPixelsByAction(processedBitmap, new GrayscalePixelAction());
        }
        public void ApplyThreshold(Bitmap processedBitmap, int level)
        {
            if (level < 0 || level > 100)
                throw new ArgumentOutOfRangeException($"Level should be between 0 and 100");
            ProcessPixelsByAction(processedBitmap, new ThresholdPixelAction(level));
        }



        private unsafe void ProcessPixelsByAction(Bitmap processedBitmap,
            PixelAction actionForRgbPixels)
        {
            BitmapData bitmapData = processedBitmap.LockBits(
                new Rectangle(0, 0, processedBitmap.Width, processedBitmap.Height), ImageLockMode.ReadWrite,
                processedBitmap.PixelFormat);

            int bytesPerPixel = Bitmap.GetPixelFormatSize(processedBitmap.PixelFormat) / 8;
            int heightInPixels = bitmapData.Height;
            int widthInBytes = bitmapData.Width * bytesPerPixel;
            byte* ptrFirstPixel = (byte*) bitmapData.Scan0;

            Parallel.For(0, heightInPixels, y =>
            {
                byte* currentLine = ptrFirstPixel + (y * bitmapData.Stride);
                for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
                {
                    int blue = currentLine[x];
                    int green = currentLine[x + 1];
                    int red = currentLine[x + 2];
                    actionForRgbPixels.Invoke(ref red, ref green, ref blue);
                    currentLine[x] = (byte) blue;
                    currentLine[x + 1] = (byte) green;
                    currentLine[x + 2] = (byte) red;
                }
            });
            processedBitmap.UnlockBits(bitmapData);
        }

        

        #region PixelActions

        private abstract class PixelAction
        {
            public abstract void Invoke(ref int r, ref int g, ref int b);
        }

        private class GrayscalePixelAction : PixelAction
        {
            public override void Invoke(ref int r, ref int g, ref int b)
            {
                int intensity = (r + g + b) / 3;
                r = intensity;
                g = intensity;
                b = intensity;
            }
        }
        private class SepiaPixelAction : PixelAction
        {
            public override void Invoke(ref int r, ref int g, ref int b)
            {
                int newR = (int)(r * 0.393f + g * 0.769f + b * 0.189f);
                int newG = (int)(r * 0.349f + g * 0.686f + b * 0.168f);
                int newB = (int)(r * 0.272f + g * 0.543f + b * 0.131f);
                r = newR > 255 ? 255 : newR;
                g = newG > 255 ? 255 : newG;
                b = newB > 255 ? 255 : newB;
            }
        }
        private class ThresholdPixelAction : PixelAction
        {
            private readonly int level;

            public ThresholdPixelAction(int level)
            {
                this.level = level;
            }
            public override void Invoke(ref int r, ref int g, ref int b)
            {
                int intensity = (r + g + b) / 3;
                if (intensity >= 255 * level / 100)
                {
                    r = 255;
                    g = 255;
                    b = 255;
                }
                else
                {
                    r = 0;
                    g = 0;
                    b = 0;
                }

            }
        }

        #endregion

    }
}