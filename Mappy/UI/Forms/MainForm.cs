﻿namespace Mappy.UI.Forms
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Globalization;
    using System.Windows.Forms;

    using Mappy.Controllers;
    using Mappy.Data;
    using Mappy.Models;
    using Mappy.Views;

    public partial class MainForm : Form, IMainView
    {
        private IList<Section> sections;
        private IList<Feature> features;

        public MainForm()
        {
            this.InitializeComponent();
        }

        public MainPresenter Presenter { get; set; }

        public string TitleText
        {
            get { return this.Text; }
            set { this.Text = value; }
        }

        public bool UndoEnabled
        {
            get { return this.undoToolStripMenuItem.Enabled; }
            set { this.undoToolStripMenuItem.Enabled = value; }
        }

        public bool RedoEnabled
        {
            get { return this.redoToolStripMenuItem.Enabled; }
            set { this.redoToolStripMenuItem.Enabled = value; }
        }

        public bool CutEnabled
        {
            get
            {
                return this.toolStripMenuItem16.Enabled;
            }

            set
            {
                this.toolStripMenuItem16.Enabled = value;
            }
        }

        public bool CopyEnabled
        {
            get
            {
                return this.toolStripMenuItem14.Enabled;
            }

            set
            {
                this.toolStripMenuItem14.Enabled = value;
            }
        }

        public bool PasteEnabled
        {
            get
            {
                return this.toolStripMenuItem15.Enabled;
            }

            set
            {
                this.toolStripMenuItem15.Enabled = value;
            }
        }

        public bool SaveEnabled
        {
            get
            {
                return this.toolStripMenuItem5.Enabled;
            }

            set
            {
                this.toolStripMenuItem5.Enabled = value;
            }
        }

        public bool SaveAsEnabled
        {
            get
            {
                return this.toolStripMenuItem4.Enabled;
            }

            set
            {
                this.toolStripMenuItem4.Enabled = value;
            }
        }

        public bool CloseEnabled
        {
            get
            {
                return this.toolStripMenuItem12.Enabled;
            }

            set
            {
                this.toolStripMenuItem12.Enabled = value;
            }
        }

        public Rectangle ViewportRect
        {
            get
            {
                Point loc = this.imageLayerView1.AutoScrollPosition;
                loc.X *= -1;
                loc.Y *= -1;
                return new Rectangle(loc, this.imageLayerView1.ClientSize);
            }
        }

        public IList<Section> Sections
        {
            get
            {
                return this.sections;
            }

            set
            {
                this.sections = value;
                this.sectionView1.Sections = value;
            }
        }

        public IList<Feature> Features
        {
            get
            {
                return this.features;
            }

            set
            {
                this.features = value;
                this.featureview1.Features = value;
            }
        }

        public bool OpenAttributesEnabled
        {
            get { return this.toolStripMenuItem11.Enabled; }
            set { this.toolStripMenuItem11.Enabled = value; }
        }

        public bool MinimapVisibleChecked
        {
            get
            {
                return this.minimapToolStripMenuItem1.Checked;
            }

            set
            {
                this.minimapToolStripMenuItem1.Checked = value;
            }
        }

        public bool RefreshMinimapEnabled
        {
            get
            {
                return this.toolStripMenuItem6.Enabled;
            }

            set
            {
                this.toolStripMenuItem6.Enabled = value;
            }
        }

        public bool RefreshMinimapHighQualityEnabled
        {
            get
            {
                return this.toolStripMenuItem13.Enabled;
            }

            set
            {
                this.toolStripMenuItem13.Enabled = value;
            }
        }

        public int SeaLevel
        {
            get
            {
                return this.trackBar1.Value;
            }

            set
            {
                this.trackBar1.Value = value;
                this.label3.Text = value.ToString(CultureInfo.CurrentCulture);
            }
        }

        public bool SeaLevelEditEnabled
        {
            get
            {
                return this.trackBar1.Enabled;
            }

            set
            {
                this.label2.Enabled = value;
                this.label3.Enabled = value;
                this.trackBar1.Enabled = value;
            }
        }

        public bool ImportMinimapEnabled
        {
            get
            {
                return this.toolStripMenuItem19.Enabled;
            }

            set
            {
                this.toolStripMenuItem19.Enabled = value;
            }
        }

        public bool ExportMinimapEnabled
        {
            get
            {
                return this.toolStripMenuItem17.Enabled;
            }

            set
            {
                this.toolStripMenuItem17.Enabled = value;
            }
        }

        public bool ExportHeightmapEnabled
        {
            get
            {
                return this.toolStripMenuItem18.Enabled;
            }

            set
            {
                this.toolStripMenuItem18.Enabled = value;
            }
        }

        public bool ExportMapImageEnabled
        {
            get
            {
                return this.toolStripMenuItem21.Enabled;
            }

            set
            {
                this.toolStripMenuItem21.Enabled = value;
            }
        }

        public bool ImportHeightmapEnabled
        {
            get
            {
                return this.toolStripMenuItem20.Enabled;
            }

            set
            {
                this.toolStripMenuItem20.Enabled = value;
            }
        }

        public bool ImportCustomSectionEnabled
        {
            get
            {
                return this.toolStripMenuItem22.Enabled;
            }

            set
            {
                this.toolStripMenuItem22.Enabled = value;
            }
        }

        public SectionImportPaths AskUserToChooseSectionImportPaths()
        {
            var dlg = new ImportCustomSectionForm();
            var result = dlg.ShowDialog(this);
            if (result != DialogResult.OK)
            {
                return null;
            }

            return new SectionImportPaths
                {
                    GraphicPath = dlg.GraphicPath,
                    HeightmapPath = dlg.HeightmapPath
                };
        }

        public string AskUserToChooseMap(IList<string> maps)
        {
            MapSelectionForm f = new MapSelectionForm();
            foreach (string n in maps)
            {
                f.Items.Add(n);
            }

            DialogResult r = f.ShowDialog(this);
            if (r == DialogResult.OK)
            {
                return (string)f.SelectedItem;
            }

            return null;
        }

        public string AskUserToOpenFile()
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "TA Map Files|*.hpi;*.ufo;*.ccx;*.gpf;*.gp3;*.tnt|All files|*.*";
            if (d.ShowDialog(this) == DialogResult.OK)
            {
                return d.FileName;
            }

            return null;
        }

        public string AskUserToChooseMinimap()
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.gif;*.bmp|All files|*.*";
            if (d.ShowDialog(this) == DialogResult.OK)
            {
                return d.FileName;
            }

            return null;
        }

        public void SetViewportPosition(int x, int y)
        {
            this.imageLayerView1.AutoScrollPosition = new Point(x, y);
        }

        public void CapturePreferences()
        {
            PreferencesForm f = new PreferencesForm();
            f.ShowDialog();
        }

        public string AskUserToSaveFile()
        {
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = "HPI files|*.hpi;*.ufo;*.ccx;*.gpf;*.gp3|TNT files|*.tnt|All files|*.*";
            d.AddExtension = true;
            DialogResult result = d.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                return d.FileName;
            }

            return null;
        }

        public string AskUserToSaveMinimap()
        {
            SaveFileDialog d = new SaveFileDialog();
            d.Title = "Export Minimap";
            d.Filter = "PNG files|*.png|All files|*.*";
            d.AddExtension = true;
            DialogResult result = d.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                return d.FileName;
            }

            return null;
        }

        public string AskUserToSaveHeightmap()
        {
            SaveFileDialog d = new SaveFileDialog();
            d.Title = "Export Heightmap";
            d.Filter = "PNG files|*.png|All files|*.*";
            d.AddExtension = true;
            DialogResult result = d.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                return d.FileName;
            }

            return null;
        }

        public string AskUserToSaveMapImage()
        {
            var d = new SaveFileDialog();
            d.Title = "Export Map Image";
            d.Filter = "PNG files|*.png|All files|*.*";
            d.AddExtension = true;
            var result = d.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                return d.FileName;
            }

            return null;
        }

        public string AskUserToChooseHeightmap(int width, int height)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Title = string.Format("Import Heightmap ({0}x{1} image)", width, height);
            d.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.gif;*.bmp|All files|*.*";
            if (d.ShowDialog(this) == DialogResult.OK)
            {
                return d.FileName;
            }

            return null;
        }

        void IMainView.Close()
        {
            Application.Exit();
        }

        public DialogResult AskUserToDiscardChanges()
        {
            return MessageBox.Show("There are unsaved changes. Save before closing?", "Save", MessageBoxButtons.YesNoCancel);
        }

        public Size AskUserNewMapSize()
        {
            NewMapForm dialog = new NewMapForm();
            DialogResult result = dialog.ShowDialog(this);

            switch (result)
            {
                case DialogResult.OK:
                    return new Size(dialog.MapWidth, dialog.MapHeight);
                case DialogResult.Cancel:
                    return new Size();
                default:
                    throw new ArgumentException("bad dialogresult");
            }
        }

        public Color? AskUserGridColor(Color previousColor)
        {
            ColorDialog colorDialog = new ColorDialog();
            colorDialog.Color = previousColor;
            DialogResult result = colorDialog.ShowDialog(this);

            if (result == DialogResult.OK)
            {
                return colorDialog.Color;
            }

            return null;
        }

        public MapAttributesResult AskUserForMapAttributes(MapAttributesResult r)
        {
            MapAttributesForm f = new MapAttributesForm();

            f.mapAttributesResultBindingSource.Add(r);

            DialogResult result = f.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                return r;
            }

            return null;
        }

        public void ShowError(string message)
        {
            MessageBox.Show(this, message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public IProgressView CreateProgressView()
        {
            var dlg = new ProgressForm();
            dlg.Owner = this;
            return dlg;
        }

        private void OpenToolStripMenuItemClick(object sender, EventArgs e)
        {
            this.Presenter.Open();
        }

        private void HeightmapToolStripMenuItemCheckedChanged(object sender, EventArgs e)
        {
            this.Presenter.ToggleHeightmap();
        }

        private void MapPanel1SizeChanged(object sender, EventArgs e)
        {
            if (this.Presenter != null)
            {
                this.Presenter.UpdateMinimapViewport();
            }
        }

        private void PreferencesToolStripMenuItemClick(object sender, EventArgs e)
        {
            this.Presenter.PreferencesPressed(this, e);
        }

        private void ToolStripMenuItem4Click(object sender, EventArgs e)
        {
            this.Presenter.SaveAs();
        }

        private void ToolStripMenuItem5Click(object sender, EventArgs e)
        {
            this.Presenter.Save();
        }

        private void MinimapToolStripMenuItem1Click(object sender, EventArgs e)
        {
            this.Presenter.ToggleMinimap();
        }

        private void UndoToolStripMenuItemClick(object sender, EventArgs e)
        {
            this.Presenter.UndoPressed(this, e);
        }

        private void RedoToolStripMenuItemClick(object sender, EventArgs e)
        {
            this.Presenter.RedoPressed(this, e);
        }

        private void Form1FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                this.Presenter.ClosePressed(this, EventArgs.Empty); 
                e.Cancel = true;
            }
        }

        private void ExitToolStripMenuItemClick(object sender, EventArgs e)
        {
            this.Presenter.ClosePressed(this, e);
        }

        private void ToolStripMenuItem2Click(object sender, EventArgs e)
        {
            this.Presenter.New();
        }

        private void AboutToolStripMenuItemClick(object sender, EventArgs e)
        {
            AboutForm f = new AboutForm();
            f.ShowDialog(this);
        }

        private void ToolStripMenuItem6Click(object sender, EventArgs e)
        {
            this.Presenter.GenerateMinimapPressed(this, e);
        }

        private void ClearGridCheckboxes()
        {
            offToolStripMenuItem.Checked = false;
            toolStripMenuItem10.Checked = false;
            toolStripMenuItem9.Checked = false;
            toolStripMenuItem8.Checked = false;
            toolStripMenuItem7.Checked = false;
            x256ToolStripMenuItem.Checked = false;
            x512ToolStripMenuItem.Checked = false;
            x1024ToolStripMenuItem.Checked = false;
        }

        private void OffToolStripMenuItemClick(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            this.ClearGridCheckboxes();
            item.Checked = true;
            int size = Convert.ToInt32(item.Tag);

            this.Presenter.SetGridSize(size);
        }

        private void ChooseColorToolStripMenuItemClick(object sender, EventArgs e)
        {
            this.Presenter.ChooseColor();
        }

        private void FeaturesToolStripMenuItemClick(object sender, EventArgs e)
        {
            this.Presenter.ToggleFeatures();
        }

        private void ToolStripMenuItem11Click(object sender, EventArgs e)
        {
            this.Presenter.OpenMapAttributes();
        }

        private void TrackBar1ValueChanged(object sender, EventArgs e)
        {
            this.Presenter.SetSeaLevel(this.trackBar1.Value);
        }

        private void toolStripMenuItem12_Click(object sender, EventArgs e)
        {
            this.Presenter.CloseMap();
        }

        private void toolStripMenuItem13_Click(object sender, EventArgs e)
        {
            this.Presenter.GenerateMinimapHiqhQualityPressed(sender, e);
        }

        private void trackBar1_MouseUp(object sender, MouseEventArgs e)
        {
            this.Presenter.FlushSeaLevel();
        }

        private void toolStripMenuItem14_Click(object sender, EventArgs e)
        {
            this.Presenter.CopyToClipboard();
        }

        private void toolStripMenuItem15_Click(object sender, EventArgs e)
        {
            this.Presenter.PasteFromClipboard();
        }

        private void toolStripMenuItem16_Click(object sender, EventArgs e)
        {
            this.Presenter.CutToClipboard();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Presenter.Initialize();
        }

        private void toolStripMenuItem17_Click(object sender, EventArgs e)
        {
            this.Presenter.ExportMinimap();
        }

        private void toolStripMenuItem18_Click(object sender, EventArgs e)
        {
            this.Presenter.ExportHeightmap();
        }

        private void imageLayerView1_Scroll(object sender, ScrollEventArgs e)
        {
            this.Presenter.UpdateMinimapViewport();
        }

        private void toolStripMenuItem19_Click(object sender, EventArgs e)
        {
            this.Presenter.ImportMinimap();
        }

        private void toolStripMenuItem20_Click(object sender, EventArgs e)
        {
            this.Presenter.ImportHeightmap();
        }

        private void toolStripMenuItem21_Click(object sender, EventArgs e)
        {
            this.Presenter.ExportMapImage();
        }

        private void toolStripMenuItem22_Click(object sender, EventArgs e)
        {
            this.Presenter.ImportCustomSection();
        }
    }
}
