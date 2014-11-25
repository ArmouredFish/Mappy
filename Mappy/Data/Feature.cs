namespace Mappy.Data
{
    using System;
    using System.Drawing;

    using Mappy.Collections;

    /// <summary>
    /// Represents the "blueprint" for a feature.
    /// Contains metadata about the feature.
    /// </summary>
    public class Feature
    {
        public Feature(string name, Bitmap image)
            : this(name, image, new Point(0, 0), new Size(1, 1))
        {
        }

        public Feature(string name, Bitmap image, Point offset, Size footprint)
        {
            this.Name = name;
            this.Image = image;
            this.Offset = offset;
            this.Footprint = footprint;
        }

        public string Name { get; set; }

        public string World { get; set; }

        public string Category { get; set; }

        public Size Footprint { get; set; }

        public Point Offset { get; set; }

        public Bitmap Image { get; set; }

        public Rectangle GetDrawBounds(IGrid<int> heightmap, int xPos, int yPos)
        {
            int accum = 0;
            for (int y = 0; y <= this.Footprint.Width; y++)
            {
                for (int x = 0; x <= this.Footprint.Height; x++)
                {
                    int accX = xPos + x;
                    int accY = yPos + y;

                    // avoid crashing if we try to draw a feature too close to the map edge
                    if (accX < 0 || accY < 0 || accX >= heightmap.Width || accY >= heightmap.Height)
                    {
                        continue;
                    }

                    accum += heightmap[xPos + x, yPos + y];
                }
            }

            int avg = accum / ((this.Footprint.Width + 1) * (this.Footprint.Height + 1));

            float posX = ((float)xPos + (this.Footprint.Width / 2.0f)) * 16;
            float posY = (((float)yPos + (this.Footprint.Height / 2.0f)) * 16) - (avg / 2);

            Point pos = new Point((int)Math.Round(posX) - this.Offset.X, (int)Math.Round(posY) - this.Offset.Y);
            return new Rectangle(pos, this.Image.Size);
        }
    }
}
