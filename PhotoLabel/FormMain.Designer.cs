namespace PhotoLabel
{
    partial class FormMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.toolStripMenuItemFile = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenuItemExit = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fontToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.colourToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.rotateLeftToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.folderBrowserDialogImages = new System.Windows.Forms.FolderBrowserDialog();
            this.toolStripToolbar = new System.Windows.Forms.ToolStrip();
            this.toolStripButtonOpen = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripComboBoxZoom = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonFont = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonColour = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonRotateLeft = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonRotateRight = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonSave = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonSaveAs = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonDontSave = new System.Windows.Forms.ToolStripButton();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabelStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabelOutputDirectory = new System.Windows.Forms.ToolStripStatusLabel();
            this.colorDialog = new System.Windows.Forms.ColorDialog();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.textBoxCaption = new System.Windows.Forms.TextBox();
            this.bindingSourceImages = new System.Windows.Forms.BindingSource(this.components);
            this.panelSize = new System.Windows.Forms.Panel();
            this.panelCanvas = new System.Windows.Forms.Panel();
            this.pictureBoxImage = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.checkBoxBottomRight = new System.Windows.Forms.CheckBox();
            this.checkBoxBottomCentre = new System.Windows.Forms.CheckBox();
            this.checkBoxBottomLeft = new System.Windows.Forms.CheckBox();
            this.checkBoxRight = new System.Windows.Forms.CheckBox();
            this.checkBoxCentre = new System.Windows.Forms.CheckBox();
            this.checkBoxLeft = new System.Windows.Forms.CheckBox();
            this.checkBoxTopRight = new System.Windows.Forms.CheckBox();
            this.checkBoxTopCentre = new System.Windows.Forms.CheckBox();
            this.checkBoxTopLeft = new System.Windows.Forms.CheckBox();
            this.listViewPreview = new System.Windows.Forms.ListView();
            this.imageListLarge = new System.Windows.Forms.ImageList(this.components);
            this.fontDialog = new System.Windows.Forms.FontDialog();
            this.folderBrowserDialogSave = new System.Windows.Forms.FolderBrowserDialog();
            this.bindingSourceMain = new System.Windows.Forms.BindingSource(this.components);
            this.rotateRightToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.toolStripToolbar.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSourceImages)).BeginInit();
            this.panelSize.SuspendLayout();
            this.panelCanvas.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxImage)).BeginInit();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSourceMain)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItemFile,
            this.editToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(800, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // toolStripMenuItemFile
            // 
            this.toolStripMenuItemFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.toolStripMenuItemSeparator,
            this.toolStripMenuItem1,
            this.toolStripMenuItemExit});
            this.toolStripMenuItemFile.Name = "toolStripMenuItemFile";
            this.toolStripMenuItemFile.Size = new System.Drawing.Size(37, 20);
            this.toolStripMenuItemFile.Text = "&File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Image = global::PhotoLabel.Properties.Resources.open;
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.openToolStripMenuItem.Text = "&Open...";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.OpenToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Enabled = false;
            this.saveToolStripMenuItem.Image = global::PhotoLabel.Properties.Resources.save;
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.saveToolStripMenuItem.Text = "&Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.SaveToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Enabled = false;
            this.saveAsToolStripMenuItem.Image = global::PhotoLabel.Properties.Resources.saveas;
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.saveAsToolStripMenuItem.Text = "Save &As...";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.SaveAsToolStripMenuItemSaveAs_Click);
            // 
            // toolStripMenuItemSeparator
            // 
            this.toolStripMenuItemSeparator.Name = "toolStripMenuItemSeparator";
            this.toolStripMenuItemSeparator.Size = new System.Drawing.Size(152, 6);
            this.toolStripMenuItemSeparator.Visible = false;
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(152, 6);
            // 
            // toolStripMenuItemExit
            // 
            this.toolStripMenuItemExit.Name = "toolStripMenuItemExit";
            this.toolStripMenuItemExit.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X)));
            this.toolStripMenuItemExit.Size = new System.Drawing.Size(155, 22);
            this.toolStripMenuItemExit.Text = "E&xit";
            this.toolStripMenuItemExit.Click += new System.EventHandler(this.ExitToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fontToolStripMenuItem,
            this.colourToolStripMenuItem,
            this.toolStripMenuItem2,
            this.rotateLeftToolStripMenuItem,
            this.rotateRightToolStripMenuItem});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.editToolStripMenuItem.Text = "&Edit";
            // 
            // fontToolStripMenuItem
            // 
            this.fontToolStripMenuItem.Enabled = false;
            this.fontToolStripMenuItem.Image = global::PhotoLabel.Properties.Resources.font;
            this.fontToolStripMenuItem.Name = "fontToolStripMenuItem";
            this.fontToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.fontToolStripMenuItem.Text = "&Font...";
            this.fontToolStripMenuItem.Click += new System.EventHandler(this.FontToolStripMenuItem_Click);
            // 
            // colourToolStripMenuItem
            // 
            this.colourToolStripMenuItem.Enabled = false;
            this.colourToolStripMenuItem.Image = global::PhotoLabel.Properties.Resources.colour;
            this.colourToolStripMenuItem.Name = "colourToolStripMenuItem";
            this.colourToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.colourToolStripMenuItem.Text = "&Colour...";
            this.colourToolStripMenuItem.Click += new System.EventHandler(this.ColourToolStripMenuItem_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(177, 6);
            // 
            // rotateLeftToolStripMenuItem
            // 
            this.rotateLeftToolStripMenuItem.Enabled = false;
            this.rotateLeftToolStripMenuItem.Image = global::PhotoLabel.Properties.Resources.rotate_left;
            this.rotateLeftToolStripMenuItem.Name = "rotateLeftToolStripMenuItem";
            this.rotateLeftToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.rotateLeftToolStripMenuItem.Text = "Rotate &Left";
            this.rotateLeftToolStripMenuItem.Click += new System.EventHandler(this.RotateLeftToolStripMenuItem_Click);
            // 
            // folderBrowserDialogImages
            // 
            this.folderBrowserDialogImages.Description = "Select the folder containing the images to label.";
            this.folderBrowserDialogImages.ShowNewFolderButton = false;
            // 
            // toolStripToolbar
            // 
            this.toolStripToolbar.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStripToolbar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonOpen,
            this.toolStripSeparator1,
            this.toolStripComboBoxZoom,
            this.toolStripSeparator2,
            this.toolStripButtonFont,
            this.toolStripButtonColour,
            this.toolStripSeparator4,
            this.toolStripButtonRotateLeft,
            this.toolStripButtonRotateRight,
            this.toolStripSeparator3,
            this.toolStripButtonSave,
            this.toolStripButtonSaveAs,
            this.toolStripButtonDontSave});
            this.toolStripToolbar.Location = new System.Drawing.Point(0, 24);
            this.toolStripToolbar.Name = "toolStripToolbar";
            this.toolStripToolbar.Size = new System.Drawing.Size(800, 25);
            this.toolStripToolbar.TabIndex = 1;
            this.toolStripToolbar.Text = "toolStrip1";
            // 
            // toolStripButtonOpen
            // 
            this.toolStripButtonOpen.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonOpen.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonOpen.Image")));
            this.toolStripButtonOpen.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonOpen.Name = "toolStripButtonOpen";
            this.toolStripButtonOpen.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonOpen.Text = "Open...";
            this.toolStripButtonOpen.Click += new System.EventHandler(this.ToolStripButtonOpen_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripComboBoxZoom
            // 
            this.toolStripComboBoxZoom.Items.AddRange(new object[] {
            "10%",
            "25%",
            "50%",
            "75%",
            "100%",
            "150%",
            "200%"});
            this.toolStripComboBoxZoom.Name = "toolStripComboBoxZoom";
            this.toolStripComboBoxZoom.Size = new System.Drawing.Size(80, 25);
            this.toolStripComboBoxZoom.SelectedIndexChanged += new System.EventHandler(this.ToolStripComboBoxZoom_SelectedIndexChanged);
            this.toolStripComboBoxZoom.Leave += new System.EventHandler(this.ToolStripComboBoxZoom_Leave);
            this.toolStripComboBoxZoom.Validated += new System.EventHandler(this.ToolStripComboBoxZoom_Validated);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonFont
            // 
            this.toolStripButtonFont.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonFont.Enabled = false;
            this.toolStripButtonFont.Image = global::PhotoLabel.Properties.Resources.font;
            this.toolStripButtonFont.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonFont.Name = "toolStripButtonFont";
            this.toolStripButtonFont.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonFont.Text = "Font";
            this.toolStripButtonFont.Click += new System.EventHandler(this.ToolStripButtonFont_Click);
            // 
            // toolStripButtonColour
            // 
            this.toolStripButtonColour.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonColour.Enabled = false;
            this.toolStripButtonColour.Image = global::PhotoLabel.Properties.Resources.colour;
            this.toolStripButtonColour.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonColour.Name = "toolStripButtonColour";
            this.toolStripButtonColour.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonColour.Text = "Colour";
            this.toolStripButtonColour.Click += new System.EventHandler(this.ToolStripButtonColour_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonRotateLeft
            // 
            this.toolStripButtonRotateLeft.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonRotateLeft.Enabled = false;
            this.toolStripButtonRotateLeft.Image = global::PhotoLabel.Properties.Resources.rotate_left;
            this.toolStripButtonRotateLeft.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonRotateLeft.Name = "toolStripButtonRotateLeft";
            this.toolStripButtonRotateLeft.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonRotateLeft.Text = "Rotate Left";
            this.toolStripButtonRotateLeft.Click += new System.EventHandler(this.ToolStripButtonRotateLeft_Click);
            // 
            // toolStripButtonRotateRight
            // 
            this.toolStripButtonRotateRight.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonRotateRight.Enabled = false;
            this.toolStripButtonRotateRight.Image = global::PhotoLabel.Properties.Resources.rotate_right;
            this.toolStripButtonRotateRight.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonRotateRight.Name = "toolStripButtonRotateRight";
            this.toolStripButtonRotateRight.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonRotateRight.Text = "Rotate Right";
            this.toolStripButtonRotateRight.Click += new System.EventHandler(this.ToolStripButtonRotateRight_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonSave
            // 
            this.toolStripButtonSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonSave.Enabled = false;
            this.toolStripButtonSave.Image = global::PhotoLabel.Properties.Resources.save;
            this.toolStripButtonSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSave.Name = "toolStripButtonSave";
            this.toolStripButtonSave.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonSave.Text = "Save";
            this.toolStripButtonSave.Click += new System.EventHandler(this.ToolStripButtonSave_Click);
            // 
            // toolStripButtonSaveAs
            // 
            this.toolStripButtonSaveAs.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonSaveAs.Enabled = false;
            this.toolStripButtonSaveAs.Image = global::PhotoLabel.Properties.Resources.saveas;
            this.toolStripButtonSaveAs.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSaveAs.Name = "toolStripButtonSaveAs";
            this.toolStripButtonSaveAs.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonSaveAs.Text = "Save As...";
            this.toolStripButtonSaveAs.Click += new System.EventHandler(this.ToolStripButtonSaveAs_Click);
            // 
            // toolStripButtonDontSave
            // 
            this.toolStripButtonDontSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonDontSave.Enabled = false;
            this.toolStripButtonDontSave.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonDontSave.Image")));
            this.toolStripButtonDontSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonDontSave.Name = "toolStripButtonDontSave";
            this.toolStripButtonDontSave.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonDontSave.Text = "Do not save";
            this.toolStripButtonDontSave.Click += new System.EventHandler(this.ToolStripButtonDontSave_Click);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabelStatus,
            this.toolStripStatusLabelOutputDirectory});
            this.statusStrip.Location = new System.Drawing.Point(0, 428);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(800, 22);
            this.statusStrip.TabIndex = 3;
            this.statusStrip.Text = "statusStrip1";
            // 
            // toolStripStatusLabelStatus
            // 
            this.toolStripStatusLabelStatus.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.toolStripStatusLabelStatus.Name = "toolStripStatusLabelStatus";
            this.toolStripStatusLabelStatus.Size = new System.Drawing.Size(4, 17);
            // 
            // toolStripStatusLabelOutputDirectory
            // 
            this.toolStripStatusLabelOutputDirectory.Name = "toolStripStatusLabelOutputDirectory";
            this.toolStripStatusLabelOutputDirectory.Size = new System.Drawing.Size(0, 17);
            // 
            // colorDialog
            // 
            this.colorDialog.AnyColor = true;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 75F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.Controls.Add(this.textBoxCaption, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.panelSize, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.listViewPreview, 2, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 49);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 128F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(800, 379);
            this.tableLayoutPanel1.TabIndex = 4;
            // 
            // textBoxCaption
            // 
            this.textBoxCaption.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.bindingSourceImages, "Caption", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.textBoxCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxCaption.Location = new System.Drawing.Point(10, 261);
            this.textBoxCaption.Margin = new System.Windows.Forms.Padding(10);
            this.textBoxCaption.Multiline = true;
            this.textBoxCaption.Name = "textBoxCaption";
            this.textBoxCaption.Size = new System.Drawing.Size(505, 108);
            this.textBoxCaption.TabIndex = 3;
            // 
            // bindingSourceImages
            // 
            this.bindingSourceImages.DataMember = "Images";
            this.bindingSourceImages.DataSource = this.bindingSourceMain;
            this.bindingSourceImages.CurrentChanged += new System.EventHandler(this.BindingSourceImages_CurrentChanged);
            this.bindingSourceImages.CurrentItemChanged += new System.EventHandler(this.BindingSourceImages_CurrentItemChanged);
            // 
            // panelSize
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.panelSize, 2);
            this.panelSize.Controls.Add(this.panelCanvas);
            this.panelSize.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelSize.Location = new System.Drawing.Point(10, 10);
            this.panelSize.Margin = new System.Windows.Forms.Padding(10, 10, 10, 0);
            this.panelSize.Name = "panelSize";
            this.panelSize.Size = new System.Drawing.Size(605, 209);
            this.panelSize.TabIndex = 4;
            // 
            // panelCanvas
            // 
            this.panelCanvas.AutoScroll = true;
            this.panelCanvas.BackColor = System.Drawing.Color.Black;
            this.panelCanvas.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panelCanvas.Controls.Add(this.pictureBoxImage);
            this.panelCanvas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelCanvas.Location = new System.Drawing.Point(0, 0);
            this.panelCanvas.Margin = new System.Windows.Forms.Padding(0);
            this.panelCanvas.Name = "panelCanvas";
            this.panelCanvas.Size = new System.Drawing.Size(605, 209);
            this.panelCanvas.TabIndex = 3;
            // 
            // pictureBoxImage
            // 
            this.pictureBoxImage.BackColor = System.Drawing.Color.Black;
            this.pictureBoxImage.Location = new System.Drawing.Point(0, 0);
            this.pictureBoxImage.Name = "pictureBoxImage";
            this.pictureBoxImage.Size = new System.Drawing.Size(100, 50);
            this.pictureBoxImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxImage.TabIndex = 0;
            this.pictureBoxImage.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.label1, 2);
            this.label1.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.bindingSourceImages, "Filename", true));
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(10, 219);
            this.label1.Margin = new System.Windows.Forms.Padding(10, 0, 10, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(605, 22);
            this.label1.TabIndex = 5;
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.checkBoxBottomRight);
            this.panel1.Controls.Add(this.checkBoxBottomCentre);
            this.panel1.Controls.Add(this.checkBoxBottomLeft);
            this.panel1.Controls.Add(this.checkBoxRight);
            this.panel1.Controls.Add(this.checkBoxCentre);
            this.panel1.Controls.Add(this.checkBoxLeft);
            this.panel1.Controls.Add(this.checkBoxTopRight);
            this.panel1.Controls.Add(this.checkBoxTopCentre);
            this.panel1.Controls.Add(this.checkBoxTopLeft);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(525, 261);
            this.panel1.Margin = new System.Windows.Forms.Padding(0, 10, 10, 10);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(90, 108);
            this.panel1.TabIndex = 7;
            // 
            // checkBoxBottomRight
            // 
            this.checkBoxBottomRight.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxBottomRight.BackgroundImage = global::PhotoLabel.Properties.Resources.bottom_right;
            this.checkBoxBottomRight.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.checkBoxBottomRight.Location = new System.Drawing.Point(60, 60);
            this.checkBoxBottomRight.Margin = new System.Windows.Forms.Padding(0);
            this.checkBoxBottomRight.Name = "checkBoxBottomRight";
            this.checkBoxBottomRight.Size = new System.Drawing.Size(30, 30);
            this.checkBoxBottomRight.TabIndex = 8;
            this.checkBoxBottomRight.UseVisualStyleBackColor = true;
            this.checkBoxBottomRight.Click += new System.EventHandler(this.CheckBoxBottomRight_Click);
            // 
            // checkBoxBottomCentre
            // 
            this.checkBoxBottomCentre.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxBottomCentre.BackgroundImage = global::PhotoLabel.Properties.Resources.bottom_centre;
            this.checkBoxBottomCentre.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.checkBoxBottomCentre.Location = new System.Drawing.Point(30, 60);
            this.checkBoxBottomCentre.Margin = new System.Windows.Forms.Padding(0);
            this.checkBoxBottomCentre.Name = "checkBoxBottomCentre";
            this.checkBoxBottomCentre.Size = new System.Drawing.Size(30, 30);
            this.checkBoxBottomCentre.TabIndex = 7;
            this.checkBoxBottomCentre.UseVisualStyleBackColor = true;
            this.checkBoxBottomCentre.Click += new System.EventHandler(this.CheckBoxBottomCentre_Click);
            // 
            // checkBoxBottomLeft
            // 
            this.checkBoxBottomLeft.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxBottomLeft.BackgroundImage = global::PhotoLabel.Properties.Resources.bottom_left;
            this.checkBoxBottomLeft.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.checkBoxBottomLeft.Location = new System.Drawing.Point(0, 60);
            this.checkBoxBottomLeft.Margin = new System.Windows.Forms.Padding(0);
            this.checkBoxBottomLeft.Name = "checkBoxBottomLeft";
            this.checkBoxBottomLeft.Size = new System.Drawing.Size(30, 30);
            this.checkBoxBottomLeft.TabIndex = 6;
            this.checkBoxBottomLeft.UseVisualStyleBackColor = true;
            this.checkBoxBottomLeft.Click += new System.EventHandler(this.CheckBoxBottomLeft_Click);
            // 
            // checkBoxRight
            // 
            this.checkBoxRight.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxRight.BackgroundImage = global::PhotoLabel.Properties.Resources.right;
            this.checkBoxRight.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.checkBoxRight.Location = new System.Drawing.Point(60, 30);
            this.checkBoxRight.Margin = new System.Windows.Forms.Padding(0);
            this.checkBoxRight.Name = "checkBoxRight";
            this.checkBoxRight.Size = new System.Drawing.Size(30, 30);
            this.checkBoxRight.TabIndex = 5;
            this.checkBoxRight.UseVisualStyleBackColor = true;
            this.checkBoxRight.Click += new System.EventHandler(this.CheckBoxRight_Click);
            // 
            // checkBoxCentre
            // 
            this.checkBoxCentre.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxCentre.Location = new System.Drawing.Point(30, 30);
            this.checkBoxCentre.Margin = new System.Windows.Forms.Padding(0);
            this.checkBoxCentre.Name = "checkBoxCentre";
            this.checkBoxCentre.Size = new System.Drawing.Size(30, 30);
            this.checkBoxCentre.TabIndex = 4;
            this.checkBoxCentre.UseVisualStyleBackColor = true;
            this.checkBoxCentre.Click += new System.EventHandler(this.CheckBoxCentre_Click);
            // 
            // checkBoxLeft
            // 
            this.checkBoxLeft.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxLeft.Image = global::PhotoLabel.Properties.Resources.left;
            this.checkBoxLeft.Location = new System.Drawing.Point(0, 30);
            this.checkBoxLeft.Margin = new System.Windows.Forms.Padding(0);
            this.checkBoxLeft.Name = "checkBoxLeft";
            this.checkBoxLeft.Size = new System.Drawing.Size(30, 30);
            this.checkBoxLeft.TabIndex = 3;
            this.checkBoxLeft.UseVisualStyleBackColor = true;
            this.checkBoxLeft.Click += new System.EventHandler(this.CheckBoxLeft_Click);
            // 
            // checkBoxTopRight
            // 
            this.checkBoxTopRight.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxTopRight.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("checkBoxTopRight.BackgroundImage")));
            this.checkBoxTopRight.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.checkBoxTopRight.Location = new System.Drawing.Point(60, 0);
            this.checkBoxTopRight.Margin = new System.Windows.Forms.Padding(0);
            this.checkBoxTopRight.Name = "checkBoxTopRight";
            this.checkBoxTopRight.Size = new System.Drawing.Size(30, 30);
            this.checkBoxTopRight.TabIndex = 2;
            this.checkBoxTopRight.UseVisualStyleBackColor = true;
            this.checkBoxTopRight.Click += new System.EventHandler(this.CheckBoxTopRight_Click);
            // 
            // checkBoxTopCentre
            // 
            this.checkBoxTopCentre.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxTopCentre.AutoSize = true;
            this.checkBoxTopCentre.Image = ((System.Drawing.Image)(resources.GetObject("checkBoxTopCentre.Image")));
            this.checkBoxTopCentre.Location = new System.Drawing.Point(30, 0);
            this.checkBoxTopCentre.Margin = new System.Windows.Forms.Padding(0);
            this.checkBoxTopCentre.Name = "checkBoxTopCentre";
            this.checkBoxTopCentre.Size = new System.Drawing.Size(30, 30);
            this.checkBoxTopCentre.TabIndex = 1;
            this.checkBoxTopCentre.UseVisualStyleBackColor = true;
            this.checkBoxTopCentre.Click += new System.EventHandler(this.CheckBoxTopCentre_Click);
            // 
            // checkBoxTopLeft
            // 
            this.checkBoxTopLeft.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxTopLeft.BackgroundImage = global::PhotoLabel.Properties.Resources.top_left;
            this.checkBoxTopLeft.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.checkBoxTopLeft.Location = new System.Drawing.Point(0, 0);
            this.checkBoxTopLeft.Margin = new System.Windows.Forms.Padding(0);
            this.checkBoxTopLeft.Name = "checkBoxTopLeft";
            this.checkBoxTopLeft.Size = new System.Drawing.Size(30, 30);
            this.checkBoxTopLeft.TabIndex = 0;
            this.checkBoxTopLeft.UseVisualStyleBackColor = true;
            this.checkBoxTopLeft.Click += new System.EventHandler(this.CheckBoxTopLeft_Click);
            // 
            // listViewPreview
            // 
            this.listViewPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewPreview.HideSelection = false;
            this.listViewPreview.LargeImageList = this.imageListLarge;
            this.listViewPreview.Location = new System.Drawing.Point(625, 10);
            this.listViewPreview.Margin = new System.Windows.Forms.Padding(0, 10, 10, 10);
            this.listViewPreview.MultiSelect = false;
            this.listViewPreview.Name = "listViewPreview";
            this.tableLayoutPanel1.SetRowSpan(this.listViewPreview, 3);
            this.listViewPreview.ShowGroups = false;
            this.listViewPreview.ShowItemToolTips = true;
            this.listViewPreview.Size = new System.Drawing.Size(165, 359);
            this.listViewPreview.TabIndex = 8;
            this.listViewPreview.UseCompatibleStateImageBehavior = false;
            this.listViewPreview.SelectedIndexChanged += new System.EventHandler(this.ListViewPreview_SelectedIndexChanged);
            // 
            // imageListLarge
            // 
            this.imageListLarge.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.imageListLarge.ImageSize = new System.Drawing.Size(128, 128);
            this.imageListLarge.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // folderBrowserDialogSave
            // 
            this.folderBrowserDialogSave.Description = "Where should the file be saved to?";
            // 
            // bindingSourceMain
            // 
            this.bindingSourceMain.AllowNew = false;
            this.bindingSourceMain.DataSource = typeof(PhotoLabel.ViewModels.MainFormViewModel);
            this.bindingSourceMain.CurrentItemChanged += new System.EventHandler(this.BindingSourceMain_CurrentItemChanged);
            // 
            // rotateRightToolStripMenuItem
            // 
            this.rotateRightToolStripMenuItem.Enabled = false;
            this.rotateRightToolStripMenuItem.Image = global::PhotoLabel.Properties.Resources.rotate_right;
            this.rotateRightToolStripMenuItem.Name = "rotateRightToolStripMenuItem";
            this.rotateRightToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.rotateRightToolStripMenuItem.Text = "Rotate &Right";
            this.rotateRightToolStripMenuItem.Click += new System.EventHandler(this.RotateRightToolStripMenuItem_Click);
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.toolStripToolbar);
            this.Controls.Add(this.menuStrip1);
            this.DataBindings.Add(new System.Windows.Forms.Binding("WindowState", this.bindingSourceMain, "WindowState", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "FormMain";
            this.Text = "Photo Label";
            this.SizeChanged += new System.EventHandler(this.FormMain_SizeChanged);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.toolStripToolbar.ResumeLayout(false);
            this.toolStripToolbar.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSourceImages)).EndInit();
            this.panelSize.ResumeLayout(false);
            this.panelCanvas.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxImage)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSourceMain)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemFile;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemExit;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialogImages;
        private System.Windows.Forms.ToolStrip toolStripToolbar;
        private System.Windows.Forms.ToolStripButton toolStripButtonOpen;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelStatus;
        private System.Windows.Forms.ColorDialog colorDialog;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.ToolStripComboBox toolStripComboBoxZoom;
        private System.Windows.Forms.FontDialog fontDialog;
        private System.Windows.Forms.ToolStripButton toolStripButtonFont;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton toolStripButtonColour;
        private System.Windows.Forms.TextBox textBoxCaption;
        private System.Windows.Forms.BindingSource bindingSourceMain;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItemSeparator;
        private System.Windows.Forms.Panel panelSize;
        private System.Windows.Forms.Panel panelCanvas;
        private System.Windows.Forms.PictureBox pictureBoxImage;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripButton toolStripButtonSave;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialogSave;
        private System.Windows.Forms.ToolStripButton toolStripButtonDontSave;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelOutputDirectory;
        private System.Windows.Forms.ToolStripButton toolStripButtonSaveAs;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ImageList imageListLarge;
        private System.Windows.Forms.BindingSource bindingSourceImages;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.CheckBox checkBoxTopLeft;
        private System.Windows.Forms.CheckBox checkBoxTopCentre;
        private System.Windows.Forms.CheckBox checkBoxTopRight;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripButton toolStripButtonRotateLeft;
        private System.Windows.Forms.CheckBox checkBoxLeft;
        private System.Windows.Forms.CheckBox checkBoxCentre;
        private System.Windows.Forms.ToolStripButton toolStripButtonRotateRight;
        private System.Windows.Forms.ListView listViewPreview;
        private System.Windows.Forms.CheckBox checkBoxRight;
        private System.Windows.Forms.CheckBox checkBoxBottomLeft;
        private System.Windows.Forms.CheckBox checkBoxBottomCentre;
        private System.Windows.Forms.CheckBox checkBoxBottomRight;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fontToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem colourToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem rotateLeftToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem rotateRightToolStripMenuItem;
    }
}

