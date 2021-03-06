﻿namespace Mappy.Util.ImageSampling
{
    using System.Drawing;

    public class NearestNeighbourWrapper : IPixelImage
    {
        private readonly IPixelImage source;

        public NearestNeighbourWrapper(IPixelImage source, int width, int height)
        {
            this.source = source;
            this.Width = width;
            this.Height = height;
        }

        public int Width { get; }

        public int Height { get; }

        public Color GetPixel(int x, int y)
        {
            // sample at the centre of each pixel
            var ax = x + 0.5f;
            var ay = y + 0.5f;

            var imageX = (int)((ax / this.Width) * this.source.Width);
            var imageY = (int)((ay / this.Height) * this.source.Height);
            return this.source.GetPixel(imageX, imageY);
        }
        }
}
