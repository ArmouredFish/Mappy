﻿namespace Mappy.UI.Controls
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    using Mappy.Collections;
    using Mappy.Data;
    using Mappy.Database;
    using Mappy.Models;
    using Mappy.UI.Drawables;
    using Mappy.UI.Tags;

    public partial class MapViewPanel : UserControl
    {
        private const int BandboxDepth = 100000000;

        private static readonly Color BandboxFillColor = Color.FromArgb(127, Color.Blue);

        private static readonly Color BandboxBorderColor = Color.FromArgb(127, Color.Black);

        private static readonly IDrawable[] StartPositionImages = new IDrawable[10];

        private static readonly Feature DefaultFeatureRecord = new Feature
        {
            Name = "default",
            Offset = new Point(0, 0),
            Footprint = new Size(1, 1),
            Image = Mappy.Properties.Resources.nofeature
        };

        private readonly List<DrawableItemCollection.Item> tileMapping = new List<DrawableItemCollection.Item>();

        private readonly IDictionary<Guid, DrawableItemCollection.Item> featureMapping =
            new Dictionary<Guid, DrawableItemCollection.Item>();

        private readonly DrawableItemCollection.Item[] startPositionMapping = new DrawableItemCollection.Item[10];

        private readonly GridLayer grid = new GridLayer(16, Color.Black);

        private readonly GuideLayer guides = new GuideLayer();

        private SelectableItemsLayer itemsLayer = new SelectableItemsLayer(0, 0);

        private IMapViewSettingsModel settingsModel;

        private IMainModel mapModel;

        private bool mouseDown;

        private Point lastMousePos;

        private bool bandboxMode;

        private DrawableItemCollection.Item bandboxMapping;

        private DrawableTile baseTile;

        private DrawableItemCollection.Item baseItem;

        private Point oldAutoScrollPos;

        private IFeatureDatabase featureDatabase;

        private bool featuresVisible;

        private bool heightmapVisible;

        static MapViewPanel()
        {
            for (int i = 0; i < 10; i++)
            {
                var image = new DrawableBitmap(Mappy.Util.Util.GetStartImage(i + 1));
                StartPositionImages[i] = image;
            }
        }

        public MapViewPanel()
        {
            this.InitializeComponent();

            this.mapView.Layers.Add(this.itemsLayer);
            this.mapView.Layers.Add(this.grid);
            this.mapView.Layers.Add(this.guides);
        }

        public void SetSettingsModel(IMapViewSettingsModel model)
        {
            this.settingsModel = model;

            model.HeightmapVisible.Subscribe(x => this.heightmapVisible = x);
            model.Map.Subscribe(this.SetMapModel);
            model.GridVisible.Subscribe(x => this.grid.Enabled = x);
            model.GridColor.Subscribe(x => this.grid.Color = x);

            // FIXME: this should not ignore height
            model.GridSize.Subscribe(x => this.grid.CellSize = x.Width);
            model.HeightmapVisible.Subscribe(this.RefreshHeightmapVisibility);
            model.FeaturesVisible.Subscribe(x => this.featuresVisible = x);
            model.FeatureRecords.Subscribe(x => this.featureDatabase = x);
            model.CanvasSize.Subscribe(x => this.mapView.CanvasSize = x);

            model.CanvasSize.Subscribe(
                x =>
                    {
                        this.guides.ClearGuides();
                        this.guides.AddHorizontalGuide(x.Height - 128);
                        this.guides.AddVerticalGuide(x.Width - 32);
                    });
            model.ViewportLocation.Subscribe(x => this.mapView.AutoScrollPosition = x);
        }

        private void SetMapModel(IMainModel model)
        {
            this.mapModel = model;
            this.WireMapModel();
            this.ResetView();
        }

        private void ResetView()
        {
            this.UpdateItemsLayer();

            this.UpdateBaseTile();

            this.UpdateFloatingTiles();

            this.UpdateFeatures();

            this.UpdateStartPositions();
        }

        private void UpdateItemsLayer()
        {
            if (this.mapModel == null)
            {
                this.itemsLayer = new SelectableItemsLayer(0, 0);
            }
            else
            {
                this.itemsLayer = new SelectableItemsLayer(
                    this.mapModel.MapWidth * 32,
                    this.mapModel.MapHeight * 32);
            }

            this.mapView.Layers[0] = this.itemsLayer;
        }

        private void UpdateStartPositions()
        {
            for (int i = 0; i < 10; i++)
            {
                this.UpdateStartPosition(i);
            }
        }

        private void UpdateFeatures()
        {
            foreach (var f in this.featureMapping.Values)
            {
                this.itemsLayer.Items.Remove(f);
            }

            this.featureMapping.Clear();

            if (this.mapModel == null)
            {
                return;
            }

            foreach (var f in this.mapModel.EnumerateFeatureInstances())
            {
                this.InsertFeature(f.Id);
            }
        }

        private void UpdateBaseTile()
        {
            if (this.baseItem != null)
            {
                this.itemsLayer.Items.Remove(this.baseItem);
            }

            if (this.mapModel == null)
            {
                this.baseTile = null;
                this.baseItem = null;
                return;
            }

            this.baseTile = new DrawableTile(this.mapModel.BaseTile);
            this.baseTile.BackgroundColor = Color.CornflowerBlue;
            this.baseTile.DrawHeightMap = this.heightmapVisible;
            this.baseTile.SeaLevel = this.mapModel.SeaLevel;
            this.baseItem = new DrawableItemCollection.Item(
                0,
                0,
                -1,
                this.baseTile);

            this.baseItem.Locked = true;

            this.itemsLayer.Items.Add(this.baseItem);
        }

        private void RefreshHeightmapVisibility(bool visible)
        {
            if (this.baseTile == null)
            {
                return;
            }

            this.baseTile.DrawHeightMap = visible;
        }

        private void RefreshSeaLevel()
        {
            this.baseTile.SeaLevel = this.mapModel.SeaLevel;
        }

        private void WireMapModel()
        {
            if (this.mapModel == null)
            {
                return;
            }

            this.mapModel.TilesChanged += this.TilesChanged;
            this.mapModel.BaseTileGraphicsChanged += this.BaseTileChanged;
            this.mapModel.BaseTileHeightChanged += this.BaseTileChanged;

            foreach (var t in this.mapModel.FloatingTiles)
            {
                t.LocationChanged += this.TileLocationChanged;
            }

            this.mapModel.FeatureInstanceChanged += this.FeatureInstanceChanged;

            this.mapModel.StartPositionChanged += this.StartPositionChanged;

            this.mapModel.PropertyChanged += this.MapModelPropertyChanged;

            this.mapModel.SelectedFeatures.CollectionChanged += this.SelectedFeaturesCollectionChanged;
        }

        private void SelectedFeaturesCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.RefreshSelection();
        }

        private void MapModelPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            switch (propertyChangedEventArgs.PropertyName)
            {
                case "SeaLevel":
                    this.RefreshSeaLevel();
                    break;
                case "SelectedTile":
                case "SelectedFeatures":
                case "SelectedStartPosition":
                    this.RefreshSelection();
                    break;
                case "BandboxRectangle":
                    this.UpdateBandbox();
                    break;
            }
        }

        private void UpdateBandbox()
        {
            if (this.bandboxMapping != null)
            {
                this.itemsLayer.Items.Remove(this.bandboxMapping);
            }

            if (this.mapModel == null)
            {
                return;
            }

            if (this.mapModel.BandboxRectangle == Rectangle.Empty)
            {
                return;
            }

            var bandbox = DrawableBandbox.CreateSimple(
                this.mapModel.BandboxRectangle.Size,
                BandboxFillColor,
                BandboxBorderColor);

            this.bandboxMapping = new DrawableItemCollection.Item(
                this.mapModel.BandboxRectangle.X,
                this.mapModel.BandboxRectangle.Y,
                BandboxDepth,
                bandbox);

            this.bandboxMapping.Locked = true;

            this.itemsLayer.Items.Add(this.bandboxMapping);
        }

        private void RefreshSelection()
        {
            this.itemsLayer.ClearSelection();

            if (this.mapModel == null)
            {
                return;
            }

            if (this.mapModel.SelectedTile.HasValue)
            {
                if (this.tileMapping.Count > this.mapModel.SelectedTile)
                {
                    this.itemsLayer.AddToSelection(this.tileMapping[this.mapModel.SelectedTile.Value]);
                }
            }
            else if (this.mapModel.SelectedFeatures.Count > 0)
            {
                foreach (var item in this.mapModel.SelectedFeatures)
                {
                    if (this.featureMapping.ContainsKey(item))
                    {
                        this.itemsLayer.AddToSelection(this.featureMapping[item]);
                    }
                }
            }
            else if (this.mapModel.SelectedStartPosition.HasValue)
            {
                var mapping = this.startPositionMapping[this.mapModel.SelectedStartPosition.Value];
                if (mapping != null)
                {
                    this.itemsLayer.AddToSelection(mapping);
                }
            }
        }

        private void BaseTileChanged(object sender, EventArgs e)
        {
            this.mapView.Invalidate();
        }

        private void TileLocationChanged(object sender, EventArgs e)
        {
            Positioned<IMapTile> item = (Positioned<IMapTile>)sender;
            int index = this.mapModel.FloatingTiles.IndexOf(item);

            this.RemoveTile(index);
            this.InsertTile(item, index);
        }

        private void StartPositionChanged(object sender, StartPositionChangedEventArgs e)
        {
            this.UpdateStartPosition(e.Index);
        }

        private void UpdateStartPosition(int index)
        {
            if (this.startPositionMapping[index] != null)
            {
                var mapping = this.startPositionMapping[index];
                this.itemsLayer.Items.Remove(mapping);
                this.itemsLayer.RemoveFromSelection(mapping);
                this.startPositionMapping[index] = null;
            }

            if (this.mapModel == null)
            {
                return;
            }

            Point? p = this.mapModel.GetStartPosition(index);
            if (p.HasValue)
            {
                IDrawable img = StartPositionImages[index];
                var i = new DrawableItemCollection.Item(
                    p.Value.X - (img.Width / 2),
                    p.Value.Y - 58,
                    int.MaxValue,
                    img);
                i.Tag = new StartPositionTag(index);
                this.startPositionMapping[index] = i;
                this.itemsLayer.Items.Add(i);

                if (this.mapModel.SelectedStartPosition == index)
                {
                    this.itemsLayer.AddToSelection(i);
                }
            }
        }

        private void TilesChanged(object sender, ListChangedEventArgs e)
        {
            switch (e.ListChangedType)
            {
                case ListChangedType.ItemAdded:
                    this.InsertTile(this.mapModel.FloatingTiles[e.NewIndex], e.NewIndex);
                    this.mapModel.FloatingTiles[e.NewIndex].LocationChanged += this.TileLocationChanged;
                    break;
                case ListChangedType.ItemDeleted:
                    this.RemoveTile(e.NewIndex);
                    break;
                case ListChangedType.ItemMoved:
                    this.RemoveTile(e.OldIndex);
                    this.InsertTile(this.mapModel.FloatingTiles[e.NewIndex], e.NewIndex);
                    break;
                case ListChangedType.Reset:
                    this.UpdateFloatingTiles();
                    break;
                default:
                    throw new ArgumentException("unknown list changed type: " + e.ListChangedType);
            }
        }

        private void FeatureInstanceChanged(object sender, FeatureInstanceEventArgs e)
        {
            switch (e.Action)
            {
                case FeatureInstanceEventArgs.ActionType.Add:
                    this.InsertFeature(e.FeatureInstanceId);
                    break;
                case FeatureInstanceEventArgs.ActionType.Move:
                    this.UpdateFeature(e.FeatureInstanceId);
                    break;
                case FeatureInstanceEventArgs.ActionType.Remove:
                    this.RemoveFeature(e.FeatureInstanceId);
                    break;
            }
        }

        private void UpdateFloatingTiles()
        {
            foreach (var t in this.tileMapping)
            {
                this.itemsLayer.Items.Remove(t);
            }

            this.tileMapping.Clear();

            if (this.mapModel == null)
            {
                return;
            }

            int count = 0;
            foreach (var t in this.mapModel.FloatingTiles)
            {
                this.InsertTile(t, count++);
            }
        }

        private void InsertTile(Positioned<IMapTile> t, int index)
        {
            var drawable = new DrawableTile(t.Item);
            drawable.BackgroundColor = Color.CornflowerBlue;
            DrawableItemCollection.Item i = new DrawableItemCollection.Item(
                    t.Location.X * 32,
                    t.Location.Y * 32,
                    index,
                    drawable);
            i.Tag = new SectionTag(index);
            this.tileMapping.Insert(index, i);
            this.itemsLayer.Items.Add(i);

            if (this.mapModel.SelectedTile == index)
            {
                this.itemsLayer.AddToSelection(i);
            }
        }

        private void RemoveTile(int index)
        {
            DrawableItemCollection.Item item = this.tileMapping[index];
            this.itemsLayer.Items.Remove(item);
            this.itemsLayer.RemoveFromSelection(item);
            this.tileMapping.RemoveAt(index);
        }

        private int ToFeatureIndex(GridCoordinates p)
        {
            return this.ToFeatureIndex(p.X, p.Y);
        }

        private int ToFeatureIndex(int x, int y)
        {
            return (y * this.mapModel.FeatureGridWidth) + x;
        }

        private void InsertFeature(Guid id)
        {
            var f = this.mapModel.GetFeatureInstance(id);
            var coords = f.Location;
            int index = this.ToFeatureIndex(coords);
            Feature featureRecord;
            if (!this.featureDatabase.TryGetFeature(f.FeatureName, out featureRecord))
            {
                featureRecord = DefaultFeatureRecord;
            }

            Rectangle r = featureRecord.GetDrawBounds(this.mapModel.BaseTile.HeightGrid, coords.X, coords.Y);
            DrawableItemCollection.Item i = new DrawableItemCollection.Item(
                    r.X,
                    r.Y,
                    index + 1000, // magic number to separate from tiles
                    new DrawableBitmap(featureRecord.Image));
            i.Tag = new FeatureTag(f.Id);
            i.Visible = this.featuresVisible;
            this.featureMapping[f.Id] = i;
            this.itemsLayer.Items.Add(i);

            if (this.mapModel.SelectedFeatures.Contains(f.Id))
            {
                this.itemsLayer.AddToSelection(i);
            }
        }

        private void UpdateFeature(Guid id)
        {
            this.RemoveFeature(id);
            this.InsertFeature(id);
        }

        private bool RemoveFeature(Guid id)
        {
            if (this.featureMapping.ContainsKey(id))
            {
                DrawableItemCollection.Item item = this.featureMapping[id];
                this.itemsLayer.Items.Remove(item);
                this.itemsLayer.RemoveFromSelection(item);
                this.featureMapping.Remove(id);
                return true;
            }

            return false;
        }

        private void MapViewDragDrop(object sender, DragEventArgs e)
        {
            var loc = this.mapView.ToVirtualPoint(this.mapView.PointToClient(new Point(e.X, e.Y)));
            this.settingsModel.DragDropData(e.Data, loc);
        }

        private void MapViewMouseDown(object sender, MouseEventArgs e)
        {
            var loc = this.mapView.ToVirtualPoint(e.Location);
            var virtualX = loc.X;
            var virtualY = loc.Y;

            this.mouseDown = true;
            this.lastMousePos = new Point(virtualX, virtualY);

            if (this.settingsModel == null)
            {
                return;
            }

            if (!this.itemsLayer.IsInSelection(virtualX, virtualY))
            {
                var hit = this.itemsLayer.HitTest(virtualX, virtualY);
                if (hit != null)
                {
                    this.SelectFromTag(hit.Tag);
                }
                else
                {
                    this.settingsModel.ClearSelection();
                    this.settingsModel.StartBandbox(virtualX, virtualY);
                    this.bandboxMode = true;
                }
            }
        }

        private void MapViewMouseMove(object sender, MouseEventArgs e)
        {
            var loc = this.mapView.ToVirtualPoint(e.Location);
            var virtualX = loc.X;
            var virtualY = loc.Y;

            try
            {
                if (this.settingsModel == null)
                {
                    return;
                }

                if (!this.mouseDown)
                {
                    return;
                }

                if (this.bandboxMode)
                {
                    this.settingsModel.GrowBandbox(
                        virtualX - this.lastMousePos.X,
                        virtualY - this.lastMousePos.Y);
                }
                else
                {
                    this.settingsModel.TranslateSelection(
                        virtualX - this.lastMousePos.X,
                        virtualY - this.lastMousePos.Y);
                }
            }
            finally
            {
                this.lastMousePos = loc;
            }
        }

        private void MapViewMouseUp(object sender, MouseEventArgs e)
        {
            this.mouseDown = false;

            if (this.settingsModel == null)
            {
                return;
            }

            if (this.bandboxMode)
            {
                this.settingsModel.CommitBandbox();
                this.bandboxMode = false;
            }
            else
            {
                this.settingsModel.FlushTranslation();
            }
        }

        private void MapViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                this.settingsModel.DeleteSelection();
            }
        }

        private void MapViewLeave(object sender, EventArgs e)
        {
            this.settingsModel.ClearSelection();
        }

        private void SelectFromTag(object tag)
        {
            IMapItemTag t = (IMapItemTag)tag;
            t.SelectItem(this.settingsModel);
        }

        private Rectangle CalculateViewportRect()
        {
            Point loc = this.mapView.AutoScrollPosition;
            loc.X *= -1;
            loc.Y *= -1;
            return new Rectangle(loc, this.mapView.ClientSize);
        }

        private void UpdateMinimapViewport()
        {
            var rect = this.CalculateViewportRect();
            this.settingsModel?.SetViewportRectangle(rect);
        }

        private void MapViewSizeChanged(object sender, EventArgs e)
        {
            this.UpdateMinimapViewport();
        }

        private void MapViewDragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void MapViewPaint(object sender, PaintEventArgs e)
        {
            // We listen to paint to detect when scroll position has changed.
            // We could use the scroll event, but this only detects
            // scrollbar interaction, and won't catch other scrolling
            // such as mouse wheel scrolling.
            var pos = this.mapView.AutoScrollPosition;
            if (pos != this.oldAutoScrollPos)
            {
                this.UpdateMinimapViewport();
                this.oldAutoScrollPos = pos;
            }
        }
    }
}
