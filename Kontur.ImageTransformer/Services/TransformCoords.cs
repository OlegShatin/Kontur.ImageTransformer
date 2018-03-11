
namespace Kontur.ImageTransformer.Services
{
    //made static because transforms are very simple
    /// <summary>
    /// static class to map coords between transforms
    /// </summary>
    public static class TransformCoords
    {
        #region Transformers

        public static class ToCw
        {
            public static int GetX(int y, int picWidth)
            {
                return picWidth - 1 - y;
            }

            public static int GetY(int x)
            {
                return x;
            }

            public static int GetWidth(int height)
            {
                return -height;
            }

            public static int GetHeight(int width)
            {
                return width;
            }
        }
        public static class ToCww
        {
            public static int GetX(int y)
            {
                return y;
            }
            
            public static int GetY(int x, int picHeigth)
            {
                return picHeigth - 1 - x;
            }

            public static int GetWidth(int height)
            {
                return height;
            }

            public static int GetHeight(int width)
            {
                return -width;
            }
        }
        public static class ToFlipV
        {
            public static int GetX(int x)
            {
                return x;
            }

            public static int GetY(int y, int picHeight)
            {
                return picHeight - 1 - y;
            }

            public static int GetWidth(int width)
            {
                return width;
            }

            public static int GetHeight(int height)
            {
                return -height;
            }
        }
        public static class ToFlipH
        {
            public static int GetX(int x, int picWidth)
            {
                return picWidth - 1 - x;
            }

            public static int GetY(int y)
            {
                return y;
            }

            public static int GetWidth(int width)
            {
                return -width;
            }

            public static int GetHeight(int height)
            {
                return height;
            }
        }

        #endregion
    }


}
