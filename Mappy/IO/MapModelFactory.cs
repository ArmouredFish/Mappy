﻿namespace Mappy.IO
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    using Mappy.Data;
    using Mappy.Models;

    using TAUtil;
    using TAUtil.Gdi.Bitmap;
    using TAUtil.Gdi.Palette;
    using TAUtil.Tdf;
    using TAUtil.Tnt;

    /// <summary>
    /// Provides methods for creating a MapModel instance
    /// from TNT and OTA files.
    /// </summary>
    public class MapModelFactory
    {
        private readonly BitmapDeserializer bitmapDeserializer;

        public MapModelFactory(IPalette palette)
        {
            this.bitmapDeserializer = new BitmapDeserializer(palette);
        }

        public MapModel FromTntAndOta(ITntSource tnt, TdfNode ota)
        {
            var attrs = MapAttributes.Load(ota);
            var model = new MapModel(tnt.DataWidth, tnt.DataHeight, attrs);

            var schemaData = ota.Keys["GlobalHeader"].Keys["Schema 0"];
            if (schemaData.Keys.ContainsKey("features"))
            {
                var featureData = schemaData.Keys["features"];
                foreach (var data in featureData.Keys)
                {
                    TdfNode node = data.Value;
                    var x = TdfConvert.ToInt32(node.Entries["XPos"]);
                    var y = TdfConvert.ToInt32(node.Entries["ZPos"]);
                    var name = node.Entries["Featurename"];

                    var inst = new FeatureInstance(Guid.NewGuid(), name, x, y);
                    model.AddFeatureInstance(inst);
                }
            }

            return this.ReadTnt(tnt, model);
        }

        public MapModel FromTnt(ITntSource tnt)
        {
            MapModel m = new MapModel(tnt.DataWidth, tnt.DataHeight);

            return this.ReadTnt(tnt, m);
        }

        private static void ReadFeatures(ITntSource tnt, MapModel model, List<string> features)
        {
            var enumer = tnt.EnumerateAttrs().GetEnumerator();
            for (int y = 0; y < tnt.DataHeight * 2; y++)
            {
                for (int x = 0; x < tnt.DataWidth * 2; x++)
                {
                    enumer.MoveNext();
                    model.Tile.HeightGrid[x, y] = enumer.Current.Height;

                    switch (enumer.Current.Feature)
                    {
                        case TileAttr.FeatureNone:
                        case TileAttr.FeatureUnknown:
                            break;
                        case TileAttr.FeatureVoid:
                            model.Voids[x, y] = true;
                            break;
                        default:
                            var inst = new FeatureInstance(Guid.NewGuid(), features[enumer.Current.Feature], x, y);
                            model.AddFeatureInstance(inst);
                            break;
                    }
                }
            }
        }

        private static void ReadData(ITntSource tnt, MapModel model, List<Bitmap> tiles)
        {
            var enumer = tnt.EnumerateData().GetEnumerator();
            for (int y = 0; y < tnt.DataHeight; y++)
            {
                for (int x = 0; x < tnt.DataWidth; x++)
                {
                    enumer.MoveNext();
                    model.Tile.TileGrid[x, y] = tiles[enumer.Current];
                }
            }
        }

        private MapModel ReadTnt(ITntSource tnt, MapModel model)
        {
            List<Bitmap> tiles = new List<Bitmap>(tnt.TileCount);
            tiles.AddRange(tnt.EnumerateTiles().Select(this.ToBitmap));

            ReadData(tnt, model, tiles);

            List<string> features = new List<string>(tnt.AnimCount);
            features.AddRange(tnt.EnumerateAnims());

            ReadFeatures(tnt, model, features);

            model.SeaLevel = tnt.SeaLevel;

            model.Minimap = this.ToBitmap(tnt.GetMinimap());

            return model;
        }

        private Bitmap ToBitmap(byte[] tile)
        {
            Bitmap bmp = this.bitmapDeserializer.Deserialize(tile, MapConstants.TileWidth, MapConstants.TileHeight);
            return Globals.TileCache.GetOrAddBitmap(bmp);
        }

        private Bitmap ToBitmap(MinimapInfo minimap)
        {
            return this.bitmapDeserializer.Deserialize(minimap.Data, minimap.Width, minimap.Height);
        }
    }
}
