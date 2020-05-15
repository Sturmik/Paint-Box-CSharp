using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.IO;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Formatters.Binary;
using System.Drawing.Printing;
using System.Diagnostics;

namespace PaintBox
{
    public partial class MainForm : Form
    {
        #region Variables

        // Varaibles for line drawing
        List<Point> lineForm;
        string style;
        Rectangle lineExit;
        Point oldLocation;
        float width;

        // Variables for entire programm
        bool Line_PolyDraw;
        Pen toDraw;
        Color recentColor;
        Color backColor;
        bool isMouseDown;
        List<PObject> pObjects = new List<PObject>();

        // Variables for figures and other instruments
        DrawType drawType;
        string figureType;
        object figureToStore;

        // Pepette or filling mode  0 - Normal | 1 - Pepette | 2 - Fill
        int none_Pipette_Fill;

        // Undo_Redo
        
        // Act flag is some kind of the pointer, which moves througth action history. 
        // It allows user to undo or redo his actions
        int actFlag;
        List<ActionHistory> actHis = new List<ActionHistory>();

        #endregion

        #region Preparations

        public MainForm()
        {
            InitializeComponent();

            backColor = Color.White;
            recentColor = Color.Black;

            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            UpdateStyles();

            Line_PolyDraw = true;

            KeyPreview = true;
            lineForm = new List<Point>();
            actFlag = -1;
            actHis = new List<ActionHistory>();

            style = "Normal";

            comboBoxPenStyle.Text = style;
            comboBoxFigureStyle.Text = "Fill";

            tabControl.SelectedTab = tabPageMain;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            pObjects.Clear();
            pictureBoxPaint.Image = new Bitmap(pictureBoxPaint.Width, pictureBoxPaint.Height);
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            if (panelColor.Width * 2 < Width)
            {
                panelColor.Visible = true;
                panelShortColor.Visible = false;
            }
            else
            {
                panelColor.Visible = false;
                panelShortColor.Visible = true;
            }    
        }

        private void ClearData()
        {
            actHis.Clear();
            actFlag = -1;

            if (pObjects.Count > 0)
                pObjects[0].SetGlobalId(0);
            pObjects.Clear();

            listViewHierarchy.Items.Clear();
        }

        #endregion

        #region UpperTaskBarButtons

        // Create
        private void toolStripButtonCreate_Click(object sender, EventArgs e)
        {
            CreateProject cProj = new CreateProject();
            if (cProj.ShowDialog(this) == DialogResult.Yes)
            {
                toolStripLabelFileName.Text = "Unnamed";
                saveFileDialogTo.FileName = toolStripLabelFileName.Text;
                pictureBoxPaint.BackColor = cProj.Color;
                pictureBoxPaint.Size = new Size(cProj.ProjectWidth, cProj.ProjectHeight);
                pictureBoxPaint.Image = new Bitmap(cProj.ProjectWidth, cProj.ProjectHeight);
                backColor = pictureBoxPaint.BackColor;
            }
            ClearData();
        }                                 

        // Open
        private void toolStripButtonOpen_Click(object sender, EventArgs e)
        {
            if (openFileDialogFrom.ShowDialog() == DialogResult.OK)
            {
                ClearData();
                pObjects.Add(new PObject(PaintObjectType.Image, new MyImage(Image.FromFile(openFileDialogFrom.FileName),new Point(0,0))));
                string[] fileName = openFileDialogFrom.FileName.Split('\\');
                toolStripLabelFileName.Text = fileName[fileName.Length - 1];
                saveFileDialogTo.FileName = toolStripLabelFileName.Text;
                listViewHierarchy.Items.Add(pObjects[0].type.ToString());
                listViewHierarchy.Items[0].SubItems.Add(pObjects[0].GetID().ToString());
                actFlag++;
                actHis.Add(new ActionHistory(ActionType.Create, PaintObjectType.Image, pObjects[0]));
                pictureBoxPaint.Size = Image.FromFile(openFileDialogFrom.FileName).Size;
                pictureBoxPaint.Image = Image.FromFile(openFileDialogFrom.FileName);
                pictureBoxPaint_Paint(default, default);
            }
        }

        // Save
        private void toolStripButtonSave_Click(object sender, EventArgs e)
        {
            Graphics.FromImage(pictureBoxPaint.Image).Clear(backColor);

            for (int i = 0; i < pObjects.Count; i++)
                pObjects[i].isSelected = false;

            pictureBoxPaint_Paint(default, default);

            if (saveFileDialogTo.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    switch (saveFileDialogTo.FilterIndex)
                    {
                        case 1:
                            pictureBoxPaint.Image.Save(saveFileDialogTo.FileName, ImageFormat.Bmp);
                            break;
                        case 2:
                            pictureBoxPaint.Image.Save(saveFileDialogTo.FileName, ImageFormat.Png);
                            break;
                        case 3:
                            pictureBoxPaint.Image.Save(saveFileDialogTo.FileName, ImageFormat.Icon);
                            break;
                        case 4:
                            pictureBoxPaint.Image.Save(saveFileDialogTo.FileName, ImageFormat.Gif);
                            break;
                        case 5:
                            pictureBoxPaint.Image.Save(saveFileDialogTo.FileName, ImageFormat.Jpeg);
                            break;
                        case 6:
                            pictureBoxPaint.Image.Save(saveFileDialogTo.FileName, ImageFormat.Tiff);
                            break;
                    }
                } catch (Exception err) { MessageBox.Show(err.Message); }
            }
        }

        // Print

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
        private void toolStripButtonPrint_Click(object sender, EventArgs e)
        {
            //Show print dialog
            PrintDialog pd = new PrintDialog();
            PrintDocument doc = new PrintDocument();
            doc.DefaultPageSettings.Landscape = true;
            doc.PrintPage += Doc_PrintPage;
            pd.Document = doc;
            if (pd.ShowDialog() == DialogResult.OK)
                doc.Print();
        }

        private void Doc_PrintPage(object sender, PrintPageEventArgs e)
        {
            //Print image
            PrintDocument doc = new PrintDocument();
            doc.DefaultPageSettings.Landscape = true;

            Bitmap bm;

            // Formatting the image
            if (pictureBoxPaint.Image.Width > doc.DefaultPageSettings.PaperSize.Height
             & pictureBoxPaint.Image.Height > doc.DefaultPageSettings.PaperSize.Width)
                bm = ResizeImage((Bitmap)pictureBoxPaint.Image,
                   doc.DefaultPageSettings.PaperSize.Height,
                   doc.DefaultPageSettings.PaperSize.Width);
            else
            if (pictureBoxPaint.Image.Width > doc.DefaultPageSettings.PaperSize.Height)
                bm = ResizeImage((Bitmap)pictureBoxPaint.Image,
                    doc.DefaultPageSettings.PaperSize.Height, 
                    doc.DefaultPageSettings.PaperSize.Width - (pictureBoxPaint.Image.Width - doc.DefaultPageSettings.PaperSize.Height));
            else
            if (pictureBoxPaint.Image.Height > doc.DefaultPageSettings.PaperSize.Width)
                bm = ResizeImage((Bitmap)pictureBoxPaint.Image, 
                    doc.DefaultPageSettings.PaperSize.Height - (pictureBoxPaint.Image.Height - doc.DefaultPageSettings.PaperSize.Width), 
                    doc.DefaultPageSettings.PaperSize.Width);
            else
                bm = new Bitmap(pictureBoxPaint.Image);


           e.Graphics.DrawImage(bm, 0, 0);
            bm.Dispose();
        }

        // Undo button
        private void toolStripButtonUndo_Click(object sender, EventArgs e)
        {
            try
            {
                actHis[actFlag].Undo(pObjects, listViewHierarchy);
                actFlag--;
                UpdateNumbers();
                pictureBoxPaint_Paint(default, default);
            }
            catch (Exception) { }
        }

        // Redo button
        private void toolStripButtonRedo_Click(object sender, EventArgs e)
        {
            try
            {
                actHis[actFlag + 1].Redo(pObjects, listViewHierarchy);
                actFlag++;
                pictureBoxPaint_Paint(default, default);
                UpdateNumbers();
            }
            catch (Exception) { }
        }

        #endregion

        #region MainPageMethods

        #region Color

        // Pippete 
        private void PipetteColor(int X, int Y)
        {
            using (var bmp = new Bitmap(pictureBoxPaint.Width, pictureBoxPaint.Height))
            {
                pictureBoxPaint.DrawToBitmap(bmp, pictureBoxPaint.ClientRectangle);
                Color color = bmp.GetPixel(X, Y);

                recentColor = color;
                labelColor.ForeColor = color;
                labelShortColor.ForeColor = color;

                NewColorChecker();
                SelectActiveColor();

                none_Pipette_Fill = 0;
            }
        }
        private void buttonPipette_Click(object sender, EventArgs e) { none_Pipette_Fill = 1; }

        // Deletes color
        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < flowLayoutPanelColorChoice.Controls.Count; i++)
                if (recentColor == ((Button)flowLayoutPanelColorChoice.Controls[i]).BackColor)
                {
                    flowLayoutPanelColorChoice.Controls.Remove(flowLayoutPanelColorChoice.Controls[i]);
                    removeToolStripMenuItem.Enabled = false;
                    return;
                }

        }

        // Color Change
        private void ColorChange(object sender, EventArgs e)
        {
            none_Pipette_Fill = 0;

            Button check = (Button)sender;
            labelColor.ForeColor = check.BackColor;
            labelShortColor.ForeColor = check.BackColor;
            recentColor = check.BackColor;

            removeToolStripMenuItem.Enabled = true;

            Line_PolyDraw = true;
        }

        private void SelectActiveColor()
        {
            for (int i = 0; i < flowLayoutPanelColorChoice.Controls.Count; i++)
                if (((Button)flowLayoutPanelColorChoice.Controls[i]).BackColor.R == recentColor.R
                    && ((Button)flowLayoutPanelColorChoice.Controls[i]).BackColor.G == recentColor.G
                    && ((Button)flowLayoutPanelColorChoice.Controls[i]).BackColor.B == recentColor.B)
                    ((Button)flowLayoutPanelColorChoice.Controls[i]).Select();
        }

        // Checks if there is new color added by the user
        private void NewColorChecker()
        { 
            for (int i = 0; i < flowLayoutPanelColorChoice.Controls.Count; i++)
                if (((Button)flowLayoutPanelColorChoice.Controls[i]).BackColor.R == recentColor.R
                    && ((Button)flowLayoutPanelColorChoice.Controls[i]).BackColor.G == recentColor.G
                    && ((Button)flowLayoutPanelColorChoice.Controls[i]).BackColor.B == recentColor.B)
                    return;

            // We add new user color to the color choicer
            Button newAdd = new Button();
            newAdd.Text = "";
            newAdd.BackColor = recentColor;
            newAdd.Size = new Size(20, 18);
            newAdd.Click += ColorChange;
            flowLayoutPanelColorChoice.Controls.Add(newAdd);

            Line_PolyDraw = true;
        }

        private void buttonColorChanger_Click(object sender, EventArgs e)
        {
            none_Pipette_Fill = 0;
            colorDialogChange.Color = recentColor;
            if (colorDialogChange.ShowDialog() == DialogResult.OK)
            {
                recentColor = colorDialogChange.Color;
                labelColor.ForeColor = recentColor;
                labelShortColor.ForeColor = recentColor;

                NewColorChecker();
                SelectActiveColor();

                Line_PolyDraw = true;
            }    
        }

        private void numericUpDownWidth_ValueChanged(object sender, EventArgs e)
        {
            none_Pipette_Fill = 0;
            width = (float)((NumericUpDown)sender).Value;
            numericUpDownWidth.Value = (decimal)width;
            numericUpDownShortWidth.Value = (decimal)width;
        }

        #endregion

        #region Style

        private void comboBoxPenStyle_SelectedIndexChanged(object sender, EventArgs e)
        {
            none_Pipette_Fill = 0;
            style = comboBoxPenStyle.Text;
        }

        #endregion

        #region Figures

        private void FigureChoice(object sender, EventArgs e)
        {
            removeToolStripMenuItem.Enabled = false;
            none_Pipette_Fill = 0;
            Line_PolyDraw = false;
            figureType = (string)(sender as Button).Tag;
        }

        #endregion

        #endregion

        #region HierarchyWindow

        // Updates changed hierarchy
        private void UpdateNumbers()
        {
            for (int i = 0; i < pObjects.Count; i++)
            {
                pObjects[i].SetID((uint)i + 1);
                listViewHierarchy.Items[i].SubItems[1].Text = (i + 1).ToString();
            }
        }

        private void listViewHierarchy_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewHierarchy.SelectedItems.Count > 0)
            {
                toolStripMenuItemRemove.Enabled = true;
                hierarchyUpToolStripMenuItem.Enabled = true;
                hierarchyDownToolStripMenuItem.Enabled = true;
                pObjects.Find(o => o.GetID() == uint.Parse(listViewHierarchy.SelectedItems[0].SubItems[1].Text)).isSelected = true;
                pictureBoxPaint_Paint(default, default);
            }
            else
            {
                PObject lookFor = pObjects.Find(o => o.isSelected == true);
                if (lookFor != default)
                    lookFor.isSelected = false;
                toolStripMenuItemRemove.Enabled = false;
                hierarchyUpToolStripMenuItem.Enabled = false;
                hierarchyDownToolStripMenuItem.Enabled = false;
                pictureBoxPaint_Paint(default, default);
            }
        }

        private void toolStripMenuItemRemove_Click(object sender, EventArgs e)
        {
            PObject toRemove = pObjects.Find(o => o.GetID().ToString() == listViewHierarchy.SelectedItems[0].SubItems[1].Text);

            toRemove.SetGlobalId(toRemove.GetGlobalId() - 1);
            actHis.Add(new ActionHistory(ActionType.Remove, toRemove.type, toRemove));
            actFlag++;
            pObjects.Remove(toRemove);
            listViewHierarchy.Items.Remove(listViewHierarchy.SelectedItems[0]);

            UpdateNumbers();

            pictureBoxPaint_Paint(default, default);
        }

        private void hierarchyUpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewHierarchy.SelectedItems[0].Index == 0) return;

            PObject toChangeLocation = pObjects.Find(o => o.GetID().ToString() == listViewHierarchy.SelectedItems[0].SubItems[1].Text);

            pObjects.Insert(listViewHierarchy.SelectedItems[0].Index - 1, toChangeLocation);
            listViewHierarchy.Items.Insert(listViewHierarchy.SelectedItems[0].Index - 1, toChangeLocation.type.ToString());
            listViewHierarchy.Items[listViewHierarchy.SelectedItems[0].Index - 2].SubItems.Add(toChangeLocation.GetID().ToString());

            actHis.Add(new ActionHistory(ActionType.HierarchyUp, toChangeLocation.type, toChangeLocation));
            actFlag++;
            pObjects.RemoveAt(listViewHierarchy.SelectedItems[0].Index);
            listViewHierarchy.Items.RemoveAt(listViewHierarchy.SelectedItems[0].Index);

            UpdateNumbers();
            pictureBoxPaint_Paint(default, default);
        }

        private void hierarchyDownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewHierarchy.SelectedItems[0].Index == listViewHierarchy.Items.Count - 1) return;

            PObject toChangeLocation = pObjects.Find(o => o.GetID().ToString() == listViewHierarchy.SelectedItems[0].SubItems[1].Text);

            pObjects.Insert(listViewHierarchy.SelectedItems[0].Index + 2, toChangeLocation);
            listViewHierarchy.Items.Insert(listViewHierarchy.SelectedItems[0].Index + 2, toChangeLocation.type.ToString());
            listViewHierarchy.Items[listViewHierarchy.SelectedItems[0].Index + 2].SubItems.Add(toChangeLocation.GetID().ToString());

            actHis.Add(new ActionHistory(ActionType.HierarchyDown, toChangeLocation.type, toChangeLocation));
            actFlag++;
            pObjects.RemoveAt(listViewHierarchy.SelectedItems[0].Index);
            listViewHierarchy.Items.RemoveAt(listViewHierarchy.SelectedItems[0].Index);

            UpdateNumbers();
            pictureBoxPaint_Paint(default, default);
        }

        #endregion

        #region Methods

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true) == true)
            {
                e.Effect = DragDropEffects.All;
            }
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            try { pictureBoxPaint.Image = Image.FromFile(files[0]); } catch (Exception) { return; }
            foreach (string file in files)
            {
                ClearData();
                pObjects.Add(new PObject(PaintObjectType.Image, new MyImage(Image.FromFile(file), new Point(0, 0))));
                string[] fileName = file.Split('\\');
                toolStripLabelFileName.Text = fileName[fileName.Length - 1];
                saveFileDialogTo.FileName = toolStripLabelFileName.Text;
                listViewHierarchy.Items.Add(pObjects[0].type.ToString());
                listViewHierarchy.Items[0].SubItems.Add(pObjects[0].GetID().ToString());
                actFlag++;
                actHis.Add(new ActionHistory(ActionType.Create, PaintObjectType.Image, pObjects[0]));
                pictureBoxPaint.Size = Image.FromFile(file).Size;
                pictureBoxPaint.Image = Image.FromFile(file);
                pictureBoxPaint_Paint(default, default);
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
                if (File.Exists("README.TXT"))
                    Process.Start("README.TXT");

            if (e.Control == true)
                if (e.Shift == true)
                    if (e.KeyCode == Keys.Z)
                    {
                        toolStripButtonRedo_Click(default, default);
                        return;
                    }

            if (e.Control == true)
                if (e.KeyCode == Keys.Z)
                {
                    toolStripButtonUndo_Click(default, default);
                    return;
                }

            if (e.Control == true)
                if (e.KeyCode == Keys.C)
                {
                    for (int i = 0; i < pObjects.Count; i++)
                        pObjects[i].isSelected = false;

                    pictureBoxPaint.Image = new Bitmap(pictureBoxPaint.Size.Width, pictureBoxPaint.Size.Height);
                    backgroundWorkerSceneUpdate_DoWork(default, default);

                    Clipboard.SetDataObject(pictureBoxPaint.Image);
                }
        }

        private void pictureBoxPaint_Paint(object sender, PaintEventArgs e)
        {
            try
            { 
                if (backgroundWorkerSceneUpdate.IsBusy == true)
                    backgroundWorkerSceneUpdate.CancelAsync();

                backgroundWorkerSceneUpdate.RunWorkerAsync();
            }
            catch (Exception) { }
        }

        private void pictureBoxPaint_MouseUp(object sender, MouseEventArgs e)
        {
            if (isMouseDown != true) return;
            if (Line_PolyDraw == true || Line_PolyDraw == false & figureType == "Line")
            {
                pObjects.Add(new PObject(PaintObjectType.Line, new MyLine(toDraw, lineForm.ToArray())));
                actFlag++;

                if (actFlag <= actHis.Count - 1)
                    actHis.RemoveRange(actFlag, actHis.Count - actFlag);

                actHis.Add(new ActionHistory(ActionType.Create, PaintObjectType.Line, pObjects[pObjects.Count - 1]));

                pictureBoxPaint_Paint(default, default);
            }
            else
            {
                if (comboBoxFigureStyle.Text == "Fill")
                    pObjects.Add(new PObject(PaintObjectType.Figure, new MyFigure((Rectangle)figureToStore, toDraw, true, drawType)));
                else
                    pObjects.Add(new PObject(PaintObjectType.Figure, new MyFigure((Rectangle)figureToStore, toDraw, false, drawType)));

                actFlag++;

                if (actFlag <= actHis.Count - 1)
                    actHis.RemoveRange(actFlag, actHis.Count - actFlag);

                actHis.Add(new ActionHistory(ActionType.Create, PaintObjectType.Figure, pObjects[pObjects.Count - 1]));

                pictureBoxPaint_Paint(default, default);
            }

            listViewHierarchy.Items.Add(pObjects[pObjects.Count - 1].type.ToString());
            listViewHierarchy.Items[listViewHierarchy.Items.Count - 1].SubItems.Add(pObjects[pObjects.Count - 1].GetID().ToString());

            lineForm.Clear();

            isMouseDown = false;
        }

        private void pictureBoxPaint_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                if (none_Pipette_Fill == 1)
                {
                    PipetteColor(e.X, e.Y);
                    return;
                }

                lineExit = new Rectangle(e.X - 1, e.Y - 1, 2, 2);
                oldLocation = e.Location;
                isMouseDown = true;

                if (figureType == "Text")
                {
                    TextLabelCreation txtCreate = new TextLabelCreation();
                    if (txtCreate.ShowDialog(this) != DialogResult.Yes) return;

                    pObjects.Add(new PObject(PaintObjectType.Text, new MyText(txtCreate.TextToWorkWith, txtCreate.BrushStyle, txtCreate.FontStyle, new PointF(e.Location.X, e.Location.Y))));

                    actFlag++;

                    if (actFlag <= actHis.Count - 1)
                        actHis.RemoveRange(actFlag, actHis.Count - actFlag);

                    actHis.Add(new ActionHistory(ActionType.Create, PaintObjectType.Text, pObjects[pObjects.Count - 1]));

                    pictureBoxPaint_Paint(default, default);

                    listViewHierarchy.Items.Add(pObjects[pObjects.Count - 1].type.ToString());
                    listViewHierarchy.Items[listViewHierarchy.Items.Count - 1].SubItems.Add(pObjects[pObjects.Count - 1].GetID().ToString());

                    lineForm.Clear();

                    isMouseDown = false;

                    figureType = "None";
                }
            }
        }

        private void pictureBoxPaint_MouseMove(object sender, MouseEventArgs e)
        {
            if (!lineExit.Contains(e.Location))
            {
                if (isMouseDown == true)
                {
                    Brush brush = new SolidBrush(recentColor);
                    toDraw = new Pen(brush, width);

                    // Styles
                    switch (style)
                    {
                        case "Normal":
                            toDraw.StartCap = LineCap.Round;
                            toDraw.EndCap = LineCap.Round;
                            break;
                        case "Dash":
                            toDraw.DashStyle = DashStyle.Dash;
                            break;
                        case "DashDot":
                            toDraw.DashStyle = DashStyle.DashDot;
                            break;
                        case "DashDotDot":
                            toDraw.DashStyle = DashStyle.DashDotDot;
                            break;
                        case "Dot":
                            toDraw.DashStyle = DashStyle.Dot;
                            break;
                        case "Arrow":
                            toDraw.StartCap = LineCap.Flat;
                            toDraw.EndCap = LineCap.ArrowAnchor;
                            break;
                        case "Round":
                            toDraw.StartCap = LineCap.Flat;
                            toDraw.EndCap = LineCap.RoundAnchor;
                            break;
                        case "Square":
                            toDraw.StartCap = LineCap.Flat;
                            toDraw.EndCap = LineCap.Square;
                            break;
                        case "Triangle":
                            toDraw.StartCap = LineCap.Flat;
                            toDraw.EndCap = LineCap.Triangle;
                            break;
                    }

                    Graphics g = pictureBoxPaint.CreateGraphics();

                    // Type drawing
                    switch (Line_PolyDraw)
                    {
                        case true:

                    lineExit = new Rectangle(e.X - 1, e.Y - 1, 2, 2);

                    if (e.Location.X >= pictureBoxPaint.Right && e.Location.Y >= pictureBoxPaint.Bottom)
                    {
                        isMouseDown = false;
                        pictureBoxPaint_MouseUp(default, default);
                    }

                    g.DrawLine(toDraw, oldLocation, new Point(e.X, e.Y));
                    lineForm.Add(oldLocation);
                    lineForm.Add(e.Location);
                    oldLocation = e.Location;
                            break;
                        case false:
                            switch(figureType)
                            {
                                case "Line":
                                    pictureBoxPaint_Paint(default, default);
                                    lineForm.Clear();
                                    lineForm.Add(oldLocation);
                                    lineForm.Add(e.Location);
                                    g.Clear(backColor);
                                    g.DrawLine(toDraw, oldLocation, new Point(e.X, e.Y));
                                    Graphics.FromImage(pictureBoxPaint.Image).DrawLine(toDraw, oldLocation, new Point(e.X, e.Y));
                                    break;
                                case "Rectangle":
                                case "Ellipse":
                                    pictureBoxPaint_Paint(default, default);
                                    lineForm.Clear();
                                    lineForm.Add(oldLocation);
                                    lineForm.Add(e.Location);

                                    Point startPos = new Point();
                                    int width = 0, height = 0;

                                    if (oldLocation.X < e.Location.X & oldLocation.Y < e.Location.Y)
                                    { startPos = new Point(oldLocation.X, oldLocation.Y); width = e.Location.X - oldLocation.X; height = e.Location.Y - oldLocation.Y; }
                                    else if (oldLocation.X > e.Location.X & oldLocation.Y < e.Location.Y)
                                    { startPos = new Point(e.Location.X, oldLocation.Y); width = oldLocation.X - e.Location.X; height = e.Location.Y - oldLocation.Y; }
                                    else if (oldLocation.X > e.Location.X & oldLocation.Y > e.Location.Y)
                                    { startPos = new Point(e.Location.X, e.Location.Y); width = oldLocation.X - e.Location.X; height = oldLocation.Y - e.Location.Y; }
                                    else if (oldLocation.X < e.Location.X & oldLocation.Y > e.Location.Y)
                                    { startPos = new Point(oldLocation.X, e.Location.Y); width = e.Location.X - oldLocation.X; height = oldLocation.Y - e.Location.Y; }

                                    g.Clear(backColor);

                                    Rectangle toCreate = new Rectangle(startPos, new Size(width, height));

                                    if (figureType == "Rectangle")
                                    {
                                        if (comboBoxFigureStyle.Text == "Fill")
                                            g.FillRectangle(brush, toCreate);
                                        else
                                            g.DrawRectangle(toDraw, toCreate);

                                        drawType = DrawType.Rectangle;
                                    }
                                    if (figureType == "Ellipse")
                                    {
                                        if (comboBoxFigureStyle.Text == "Fill")
                                            g.FillEllipse(brush, toCreate);
                                        else
                                            g.DrawEllipse(toDraw, toCreate);

                                        drawType = DrawType.Ellipse;
                                    }

                                    figureToStore = toCreate;

                                    break;
                            }
                            break;
                    }
                }
            }
        }

        #endregion

        #region BackgroundWorkers

        /// <summary>
        /// Background worker for scene update
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorkerSceneUpdate_DoWork(object sender, DoWorkEventArgs e)
        {
            Graphics g = pictureBoxPaint.CreateGraphics();
            g.Clear(pictureBoxPaint.BackColor);
            for (int i = 0; i < pObjects.Count; i++)
                pObjects[i].Draw(pictureBoxPaint, g);
        }

        #endregion
    }

    #region PaintClass

    /// <summary>
    /// Types of action, which can be applied to the objects
    /// </summary>
    public enum ActionType
    {
        Create, Remove, HierarchyUp, HierarchyDown
    }

    /// <summary>
    /// Paint object type. Helps to define, which object should be drawn
    /// </summary>
    public enum PaintObjectType
    {
        Line, Image, Figure, Text
    }

    /// <summary>
    /// Class for working with undo and redo functions
    /// </summary>
    public class ActionHistory
    {
        public ActionType toDo;
        public PaintObjectType workWith;
        public PObject paintObj;

        public ActionHistory(ActionType toDo, PaintObjectType workWith, PObject paintObj)
        {
            this.toDo = toDo;
            this.workWith = workWith;
            this.paintObj = paintObj;
        }

        public void Undo(List<PObject> listUpdate, ListView updateHier)
        {
            try
            {
                string id;
                switch (toDo)
                {
                    case ActionType.Create:
                        paintObj.isSelected = false;
                        // Dealing with global id
                        if (paintObj.GetID() != paintObj.GetGlobalId())
                        {
                            uint incr = (uint)paintObj.GetID() - 1;
                            for (int i = (int)paintObj.GetID(); i < listUpdate.Count; i++)
                                listUpdate[i].SetID(incr--);
                        }
                        paintObj.SetGlobalId(paintObj.GetGlobalId() - 1);

                        // Clearing object from hierarchy window
                        id = paintObj.GetID().ToString();
                        listUpdate.Remove(paintObj);
                        for (int i = 0; i < updateHier.Items.Count; i++)
                            if (updateHier.Items[i].SubItems[1].Text == id)
                            {
                                updateHier.Items.RemoveAt(i);
                                return;
                            }
                        break;
                    case ActionType.Remove:
                        paintObj.isSelected = false;
                        paintObj.SetGlobalId(paintObj.GetGlobalId() + 1);
                        listUpdate.Insert((int)paintObj.GetID() - 1, paintObj);
                        updateHier.Items.Insert((int)paintObj.GetID() - 1, workWith.ToString());
                        updateHier.Items[(int)paintObj.GetID() - 1].SubItems.Add((paintObj.GetID()).ToString());
                        for (int i = (int)paintObj.GetID(); i < listUpdate.Count; i++)
                        {
                            listUpdate[i].SetID(listUpdate[i].GetID() + 1);
                            updateHier.Items[i].SubItems[1].Text = (uint.Parse(updateHier.Items[i].SubItems[1].Text) + 1).ToString();
                        }
                        break;
                    case ActionType.HierarchyUp:
                        listUpdate.Insert((int)paintObj.GetID() + 1, paintObj);
                        updateHier.Items.Insert((int)paintObj.GetID() + 1, paintObj.type.ToString());
                        updateHier.Items[(int)paintObj.GetID() + 1].SubItems.Add(paintObj.GetID().ToString());

                        listUpdate.RemoveAt((int)paintObj.GetID() - 1);
                        updateHier.Items.RemoveAt((int)paintObj.GetID() - 1);
                        break;
                    case ActionType.HierarchyDown:
                        listUpdate.Insert((int)paintObj.GetID() - 2, paintObj);
                        updateHier.Items.Insert((int)paintObj.GetID() - 2, paintObj.type.ToString());
                        updateHier.Items[(int)paintObj.GetID() - 2].SubItems.Add(paintObj.GetID().ToString());

                        listUpdate.RemoveAt((int)paintObj.GetID());
                        updateHier.Items.RemoveAt((int)paintObj.GetID());
                        break;
                }
            }
            catch (Exception) { }
        }
        public void Redo(List<PObject> listUpdate, ListView updateHier)
        {
            try
            {
                switch (toDo)
                {
                    case ActionType.Create:
                        // Dealing with global id
                        paintObj.isSelected = false;
                        paintObj.SetGlobalId(paintObj.GetGlobalId() + 1);
                        listUpdate.Add(paintObj);
                        updateHier.Items.Add(workWith.ToString());
                        updateHier.Items[(int)paintObj.GetID() - 1].SubItems.Add(paintObj.GetID().ToString());
                        break;
                    case ActionType.Remove:
                        paintObj.isSelected = false;
                        paintObj.SetGlobalId(paintObj.GetGlobalId() - 1);
                        listUpdate.Remove(paintObj);
                        for (int i = 0; i < updateHier.Items.Count; i++)
                            if (updateHier.Items[i].SubItems[1].Text == paintObj.GetID().ToString())
                            {
                                updateHier.Items.Remove(updateHier.Items[i]);
                                return;
                            }
                        break;
                    case ActionType.HierarchyUp:
                        listUpdate.Insert((int)paintObj.GetID() - 2, paintObj);
                        updateHier.Items.Insert((int)paintObj.GetID() - 2, paintObj.type.ToString());
                        updateHier.Items[(int)paintObj.GetID() - 2].SubItems.Add(paintObj.GetID().ToString());

                        listUpdate.RemoveAt((int)paintObj.GetID());
                        updateHier.Items.RemoveAt((int)paintObj.GetID());
                        break;
                    case ActionType.HierarchyDown:
                        listUpdate.Insert((int)paintObj.GetID() + 1, paintObj);
                        updateHier.Items.Insert((int)paintObj.GetID() + 1, paintObj.type.ToString());
                        updateHier.Items[(int)paintObj.GetID() + 1].SubItems.Add(paintObj.GetID().ToString());

                        listUpdate.RemoveAt((int)paintObj.GetID() - 1);
                        updateHier.Items.RemoveAt((int)paintObj.GetID() - 1);
                        break;
                }
            } catch (Exception) { }
        }
    }

    /// <summary>
    /// Class, which represents object, which user has painted
    /// </summary>
    public class PObject
    {
        /// <summary>
        /// Id for every new added paint object
        /// </summary>
        private static uint id;
        public uint GetGlobalId() { return id; }
        public void SetGlobalId(uint value) { id = value; }

        private uint myId;
        public uint GetID() { return myId; }
        public void SetID(uint value) { myId = value; }
        /// <summary>
        /// Various type
        /// </summary>
        public PaintObjectType type;
        /// <summary>
        /// Object to draw
        /// </summary>
        public object toDraw;
        /// <summary>
        /// Is the object selected
        /// </summary>
        public bool isSelected;

        static PObject()
        {
            id = 0;
        }

        public PObject(PaintObjectType type, object toDraw)
        {
            isSelected = false;
            this.type = type;
            this.toDraw = toDraw;
            myId = ++id;
        }

        public void Draw(PictureBox update, Graphics g)
        {
            switch(type)
            {
                case PaintObjectType.Line:
                    if (isSelected == true)
                        ((MyLine)toDraw).Draw(update, g, true);
                    else
                        ((MyLine)toDraw).Draw(update, g);
                    break;
                case PaintObjectType.Image:
                    ((MyImage)toDraw).Draw(update, g);
                    break;
                case PaintObjectType.Figure:
                    if (toDraw is MyFigure)
                    {
                        if (isSelected == true)
                            (toDraw as MyFigure).Draw(update, g, true);
                        else
                            (toDraw as MyFigure).Draw(update, g);
                    }
                    break;
                case PaintObjectType.Text:
                    if (isSelected == true)
                        (toDraw as MyText).Draw(update, g, true);
                    else
                        (toDraw as MyText).Draw(update, g);
                    break;
            }
        }
    }

    #region Objects

    /// <summary>
    /// Line class
    /// </summary>
    public class MyLine
    {
        public Pen Style { get; set; }
        public Point[] Points { get; set; }

        public MyLine(Pen style, Point[] points)
        {
            this.Style = style;
            this.Points = points;
        }

        public void Draw(PictureBox update, Graphics g, bool select = false)
        {
            try
            {
                if (select == true)
                {
                    g.DrawLines(new Pen(new HatchBrush(HatchStyle.LargeCheckerBoard, Color.White), Style.Width), Points);
                }
                else
                {
                    Graphics.FromImage(update.Image).DrawLines(Style, Points);
                    g.DrawLines(Style, Points);
                }
            }
            catch(Exception) { }
        }
    }

    /// <summary>
    /// Image class
    /// </summary>
    public class MyImage
    {
        public Image Im { get; set; }
        public Point ImPlace { get; set; }

        public MyImage(Image im, Point imPlace)
        {
            this.Im = im;
            this.ImPlace = imPlace;
        }

        public void Draw(PictureBox update, Graphics g)
        {
            try
            {
                g.DrawImage(Im, ImPlace);
                Graphics.FromImage(update.Image).DrawImage(Im, ImPlace);
            }
            catch (Exception) { }
        }
    }


    public enum DrawType { Rectangle, Ellipse, Pepette}
    /// <summary>
    /// Rectangle class
    /// </summary>
    public class MyFigure
    {
        public DrawType FigureType { get; set; }
        public bool Fill_Draw { get; set; }
        public Rectangle Rect { get; set; }
        public Pen Style { get; set; }

        public MyFigure(Rectangle rect, Pen style, bool Fill_Draw, DrawType Type)
        {
            Rect = rect;
            Style = style;
            this.Fill_Draw = Fill_Draw;
            FigureType = Type;
        }

        public void Draw(PictureBox update, Graphics g, bool select = false)
        {
            try
            {
                if (select == true)
                {
                    switch (FigureType)
                    {
                        case DrawType.Rectangle:
                            if (Fill_Draw == true)
                                g.FillRectangle(new HatchBrush(HatchStyle.LargeCheckerBoard, Color.White), Rect);
                            else
                                g.DrawRectangle(new Pen(new HatchBrush(HatchStyle.LargeCheckerBoard, Color.White), Style.Width), Rect);
                            break;
                        case DrawType.Ellipse:
                            if (Fill_Draw == true)
                                g.FillEllipse(new HatchBrush(HatchStyle.LargeCheckerBoard, Color.White), Rect);
                            else
                                g.DrawEllipse(new Pen(new HatchBrush(HatchStyle.LargeCheckerBoard, Color.White), Style.Width), Rect);
                            break;
                    }
                }
                else
                {
                    switch (FigureType)
                    {
                        case DrawType.Rectangle:
                            if (Fill_Draw == true)
                            {
                                g.FillRectangle(new SolidBrush(Style.Color), Rect);
                                Graphics.FromImage(update.Image).FillRectangle(new SolidBrush(Style.Color), Rect);
                            }
                            else
                            {
                                g.DrawRectangle(Style, Rect);
                                Graphics.FromImage(update.Image).DrawRectangle(Style, Rect);
                            }
                            break;
                        case DrawType.Ellipse:
                            if (Fill_Draw == true)
                            {
                                g.FillEllipse(new SolidBrush(Style.Color), Rect);
                                Graphics.FromImage(update.Image).FillEllipse(new SolidBrush(Style.Color), Rect);
                            }
                            else
                            {
                                g.DrawEllipse(Style, Rect);
                                Graphics.FromImage(update.Image).DrawEllipse(Style, Rect);
                            }
                            break;
                    }
                }
            }
            catch (Exception) { }
        }
    }

    /// <summary>
    /// Text class
    /// </summary>
    public class MyText
    {
        public string TextIn { get; set; }
        public Font FontStyle { get; set; }
        public Brush BrushStyle { get; set; } 
        public PointF Position { get; set; }

        public MyText(string textIn, Brush brushStyle, Font fontStyle, PointF position)
        {
            TextIn = textIn;
            BrushStyle = brushStyle;
            FontStyle = fontStyle;
            Position = position;
        }

        public void Draw(PictureBox update, Graphics g, bool select = false)
        {
            try
            {
                if (select == true)
                {
                    g.DrawString(TextIn, FontStyle, new HatchBrush(HatchStyle.LargeCheckerBoard, Color.White), Position);
                }
                else
                {
                    Graphics.FromImage(update.Image).DrawString(TextIn, FontStyle, BrushStyle, Position);
                    g.DrawString(TextIn, FontStyle, BrushStyle, Position);
                }
            }
            catch (Exception) { }
        }
    }

    #endregion

    #endregion
}
