namespace ADM
{
    partial class MainForm
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.speedTestButton = new System.Windows.Forms.Button();
            this.mediaPlayButton = new System.Windows.Forms.Button();
            this.transmitTextBox = new System.Windows.Forms.TextBox();
            this.transmitButton = new System.Windows.Forms.Button();
            this.transmitClearButton = new System.Windows.Forms.Button();
            this.topMostCheckBox = new System.Windows.Forms.CheckBox();
            this.mediaFetchButton = new System.Windows.Forms.Button();
            this.queryDiskSpaceButton = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.mediaTabPage = new System.Windows.Forms.TabPage();
            this.viewIDButton = new System.Windows.Forms.Button();
            this.mediaInfoButton = new System.Windows.Forms.Button();
            this.mediaSkipAheadButton = new System.Windows.Forms.Button();
            this.mediaSkipBackButton = new System.Windows.Forms.Button();
            this.mediaStopButton = new System.Windows.Forms.Button();
            this.mediaPauseButton = new System.Windows.Forms.Button();
            this.mediaView = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.browserTabPage = new System.Windows.Forms.TabPage();
            this.treeInfoTextBox = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.treeItemsListBox = new System.Windows.Forms.ListBox();
            this.treeStackListBox = new System.Windows.Forms.ListBox();
            this.treePlayButton = new System.Windows.Forms.Button();
            this.treeResetButton = new System.Windows.Forms.Button();
            this.treeUpButton = new System.Windows.Forms.Button();
            this.extrasTabPage = new System.Windows.Forms.TabPage();
            this.transmitTryParseButton = new System.Windows.Forms.Button();
            this.extraSplitContainer = new System.Windows.Forms.SplitContainer();
            this.receiveTextBox = new System.Windows.Forms.TextBox();
            this.trackInfoTabPage = new System.Windows.Forms.TabPage();
            this.getUrlButton = new System.Windows.Forms.Button();
            this.fetchButton = new System.Windows.Forms.Button();
            this.metaLayoutTable = new System.Windows.Forms.TableLayoutPanel();
            this.idLabel = new System.Windows.Forms.Label();
            this.genreLabel = new System.Windows.Forms.Label();
            this.artistLabel = new System.Windows.Forms.Label();
            this.titleLabel = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.albumLabel = new System.Windows.Forms.Label();
            this.coverPictureBox = new System.Windows.Forms.PictureBox();
            this.playUriTextBox = new System.Windows.Forms.TextBox();
            this.playUriButton = new System.Windows.Forms.Button();
            this.treeGoButton = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.mediaTabPage.SuspendLayout();
            this.browserTabPage.SuspendLayout();
            this.panel1.SuspendLayout();
            this.extrasTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.extraSplitContainer)).BeginInit();
            this.extraSplitContainer.Panel1.SuspendLayout();
            this.extraSplitContainer.Panel2.SuspendLayout();
            this.extraSplitContainer.SuspendLayout();
            this.trackInfoTabPage.SuspendLayout();
            this.metaLayoutTable.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.coverPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // speedTestButton
            // 
            this.speedTestButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.speedTestButton.Location = new System.Drawing.Point(338, 293);
            this.speedTestButton.Name = "speedTestButton";
            this.speedTestButton.Size = new System.Drawing.Size(75, 23);
            this.speedTestButton.TabIndex = 0;
            this.speedTestButton.Text = "Speed test";
            this.speedTestButton.UseVisualStyleBackColor = true;
            this.speedTestButton.Click += new System.EventHandler(this.speedTestButton_Click);
            // 
            // mediaPlayButton
            // 
            this.mediaPlayButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.mediaPlayButton.Location = new System.Drawing.Point(89, 293);
            this.mediaPlayButton.Name = "mediaPlayButton";
            this.mediaPlayButton.Size = new System.Drawing.Size(75, 23);
            this.mediaPlayButton.TabIndex = 1;
            this.mediaPlayButton.Text = "Play";
            this.mediaPlayButton.UseVisualStyleBackColor = true;
            this.mediaPlayButton.Click += new System.EventHandler(this.mediaPlayButton_Click);
            // 
            // transmitTextBox
            // 
            this.transmitTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.transmitTextBox.Location = new System.Drawing.Point(0, 0);
            this.transmitTextBox.Multiline = true;
            this.transmitTextBox.Name = "transmitTextBox";
            this.transmitTextBox.Size = new System.Drawing.Size(277, 284);
            this.transmitTextBox.TabIndex = 3;
            this.transmitTextBox.Text = "<requestplayabledata><nodeid>0</nodeid><numelem>-1</numelem></requestplayabledata" +
                ">";
            // 
            // transmitButton
            // 
            this.transmitButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.transmitButton.Location = new System.Drawing.Point(3, 293);
            this.transmitButton.Name = "transmitButton";
            this.transmitButton.Size = new System.Drawing.Size(75, 23);
            this.transmitButton.TabIndex = 4;
            this.transmitButton.Text = "Send";
            this.transmitButton.UseVisualStyleBackColor = true;
            this.transmitButton.Click += new System.EventHandler(this.transmitButton_Click);
            // 
            // transmitClearButton
            // 
            this.transmitClearButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.transmitClearButton.Location = new System.Drawing.Point(167, 293);
            this.transmitClearButton.Name = "transmitClearButton";
            this.transmitClearButton.Size = new System.Drawing.Size(75, 23);
            this.transmitClearButton.TabIndex = 5;
            this.transmitClearButton.Text = "Clear";
            this.transmitClearButton.UseVisualStyleBackColor = true;
            this.transmitClearButton.Click += new System.EventHandler(this.transmitClearButton_Click);
            // 
            // topMostCheckBox
            // 
            this.topMostCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.topMostCheckBox.AutoSize = true;
            this.topMostCheckBox.Location = new System.Drawing.Point(268, 297);
            this.topMostCheckBox.Name = "topMostCheckBox";
            this.topMostCheckBox.Size = new System.Drawing.Size(68, 17);
            this.topMostCheckBox.TabIndex = 6;
            this.topMostCheckBox.Text = "TopMost";
            this.topMostCheckBox.UseVisualStyleBackColor = true;
            this.topMostCheckBox.CheckedChanged += new System.EventHandler(this.topMostCheckBox_CheckedChanged);
            // 
            // mediaFetchButton
            // 
            this.mediaFetchButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.mediaFetchButton.Location = new System.Drawing.Point(8, 293);
            this.mediaFetchButton.Name = "mediaFetchButton";
            this.mediaFetchButton.Size = new System.Drawing.Size(75, 23);
            this.mediaFetchButton.TabIndex = 7;
            this.mediaFetchButton.Text = "Fetch";
            this.mediaFetchButton.UseVisualStyleBackColor = true;
            this.mediaFetchButton.Click += new System.EventHandler(this.mediaFetchButton_Click);
            // 
            // queryDiskSpaceButton
            // 
            this.queryDiskSpaceButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.queryDiskSpaceButton.Location = new System.Drawing.Point(419, 293);
            this.queryDiskSpaceButton.Name = "queryDiskSpaceButton";
            this.queryDiskSpaceButton.Size = new System.Drawing.Size(101, 23);
            this.queryDiskSpaceButton.TabIndex = 8;
            this.queryDiskSpaceButton.Text = "QueryDiskSpace";
            this.queryDiskSpaceButton.UseVisualStyleBackColor = true;
            this.queryDiskSpaceButton.Click += new System.EventHandler(this.queryDiskSpaceButton_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.mediaTabPage);
            this.tabControl1.Controls.Add(this.browserTabPage);
            this.tabControl1.Controls.Add(this.extrasTabPage);
            this.tabControl1.Controls.Add(this.trackInfoTabPage);
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(534, 348);
            this.tabControl1.TabIndex = 9;
            // 
            // mediaTabPage
            // 
            this.mediaTabPage.Controls.Add(this.viewIDButton);
            this.mediaTabPage.Controls.Add(this.mediaInfoButton);
            this.mediaTabPage.Controls.Add(this.mediaSkipAheadButton);
            this.mediaTabPage.Controls.Add(this.mediaSkipBackButton);
            this.mediaTabPage.Controls.Add(this.mediaStopButton);
            this.mediaTabPage.Controls.Add(this.mediaPauseButton);
            this.mediaTabPage.Controls.Add(this.mediaView);
            this.mediaTabPage.Controls.Add(this.mediaPlayButton);
            this.mediaTabPage.Controls.Add(this.mediaFetchButton);
            this.mediaTabPage.Location = new System.Drawing.Point(4, 22);
            this.mediaTabPage.Name = "mediaTabPage";
            this.mediaTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.mediaTabPage.Size = new System.Drawing.Size(526, 322);
            this.mediaTabPage.TabIndex = 1;
            this.mediaTabPage.Text = "Media";
            this.mediaTabPage.UseVisualStyleBackColor = true;
            // 
            // viewIDButton
            // 
            this.viewIDButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.viewIDButton.Location = new System.Drawing.Point(407, 293);
            this.viewIDButton.Name = "viewIDButton";
            this.viewIDButton.Size = new System.Drawing.Size(34, 23);
            this.viewIDButton.TabIndex = 13;
            this.viewIDButton.Text = "ID?";
            this.viewIDButton.UseVisualStyleBackColor = true;
            this.viewIDButton.Click += new System.EventHandler(this.viewIDButton_Click);
            // 
            // mediaInfoButton
            // 
            this.mediaInfoButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.mediaInfoButton.Location = new System.Drawing.Point(382, 293);
            this.mediaInfoButton.Name = "mediaInfoButton";
            this.mediaInfoButton.Size = new System.Drawing.Size(19, 23);
            this.mediaInfoButton.TabIndex = 12;
            this.mediaInfoButton.Text = "i";
            this.mediaInfoButton.UseVisualStyleBackColor = true;
            // 
            // mediaSkipAheadButton
            // 
            this.mediaSkipAheadButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.mediaSkipAheadButton.Location = new System.Drawing.Point(357, 293);
            this.mediaSkipAheadButton.Name = "mediaSkipAheadButton";
            this.mediaSkipAheadButton.Size = new System.Drawing.Size(19, 23);
            this.mediaSkipAheadButton.TabIndex = 11;
            this.mediaSkipAheadButton.Text = ">";
            this.mediaSkipAheadButton.UseVisualStyleBackColor = true;
            this.mediaSkipAheadButton.Click += new System.EventHandler(this.mediaSkipAheadButton_Click);
            // 
            // mediaSkipBackButton
            // 
            this.mediaSkipBackButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.mediaSkipBackButton.Location = new System.Drawing.Point(332, 293);
            this.mediaSkipBackButton.Name = "mediaSkipBackButton";
            this.mediaSkipBackButton.Size = new System.Drawing.Size(19, 23);
            this.mediaSkipBackButton.TabIndex = 10;
            this.mediaSkipBackButton.Text = "<";
            this.mediaSkipBackButton.UseVisualStyleBackColor = true;
            this.mediaSkipBackButton.Click += new System.EventHandler(this.mediaSkipBackButton_Click);
            // 
            // mediaStopButton
            // 
            this.mediaStopButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.mediaStopButton.Location = new System.Drawing.Point(251, 293);
            this.mediaStopButton.Name = "mediaStopButton";
            this.mediaStopButton.Size = new System.Drawing.Size(75, 23);
            this.mediaStopButton.TabIndex = 9;
            this.mediaStopButton.Text = "Stop";
            this.mediaStopButton.UseVisualStyleBackColor = true;
            this.mediaStopButton.Click += new System.EventHandler(this.mediaStopButton_Click);
            // 
            // mediaPauseButton
            // 
            this.mediaPauseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.mediaPauseButton.Location = new System.Drawing.Point(170, 293);
            this.mediaPauseButton.Name = "mediaPauseButton";
            this.mediaPauseButton.Size = new System.Drawing.Size(75, 23);
            this.mediaPauseButton.TabIndex = 8;
            this.mediaPauseButton.Text = "Pause";
            this.mediaPauseButton.UseVisualStyleBackColor = true;
            this.mediaPauseButton.Click += new System.EventHandler(this.mediaPauseButton_Click);
            // 
            // mediaView
            // 
            this.mediaView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.mediaView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3});
            this.mediaView.Location = new System.Drawing.Point(3, 3);
            this.mediaView.Name = "mediaView";
            this.mediaView.Size = new System.Drawing.Size(515, 284);
            this.mediaView.TabIndex = 0;
            this.mediaView.UseCompatibleStateImageBehavior = false;
            this.mediaView.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Title";
            this.columnHeader1.Width = 188;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Artist";
            this.columnHeader2.Width = 107;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Album";
            this.columnHeader3.Width = 115;
            // 
            // browserTabPage
            // 
            this.browserTabPage.Controls.Add(this.treeInfoTextBox);
            this.browserTabPage.Controls.Add(this.panel1);
            this.browserTabPage.Location = new System.Drawing.Point(4, 22);
            this.browserTabPage.Name = "browserTabPage";
            this.browserTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.browserTabPage.Size = new System.Drawing.Size(526, 322);
            this.browserTabPage.TabIndex = 3;
            this.browserTabPage.Text = "Browser";
            this.browserTabPage.UseVisualStyleBackColor = true;
            // 
            // treeInfoTextBox
            // 
            this.treeInfoTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeInfoTextBox.Location = new System.Drawing.Point(296, 3);
            this.treeInfoTextBox.Multiline = true;
            this.treeInfoTextBox.Name = "treeInfoTextBox";
            this.treeInfoTextBox.ReadOnly = true;
            this.treeInfoTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.treeInfoTextBox.Size = new System.Drawing.Size(227, 316);
            this.treeInfoTextBox.TabIndex = 5;
            this.treeInfoTextBox.Text = "Click \'Reset\' followed by an item to begin!";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.treeGoButton);
            this.panel1.Controls.Add(this.treeItemsListBox);
            this.panel1.Controls.Add(this.treeStackListBox);
            this.panel1.Controls.Add(this.treePlayButton);
            this.panel1.Controls.Add(this.treeResetButton);
            this.panel1.Controls.Add(this.treeUpButton);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(293, 316);
            this.panel1.TabIndex = 9;
            // 
            // treeItemsListBox
            // 
            this.treeItemsListBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.treeItemsListBox.FormattingEnabled = true;
            this.treeItemsListBox.Location = new System.Drawing.Point(0, 0);
            this.treeItemsListBox.Name = "treeItemsListBox";
            this.treeItemsListBox.ScrollAlwaysVisible = true;
            this.treeItemsListBox.Size = new System.Drawing.Size(287, 212);
            this.treeItemsListBox.TabIndex = 3;
            // 
            // treeStackListBox
            // 
            this.treeStackListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.treeStackListBox.FormattingEnabled = true;
            this.treeStackListBox.Location = new System.Drawing.Point(0, 218);
            this.treeStackListBox.Name = "treeStackListBox";
            this.treeStackListBox.ScrollAlwaysVisible = true;
            this.treeStackListBox.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.treeStackListBox.Size = new System.Drawing.Size(287, 69);
            this.treeStackListBox.TabIndex = 0;
            // 
            // treePlayButton
            // 
            this.treePlayButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.treePlayButton.Location = new System.Drawing.Point(68, 293);
            this.treePlayButton.Name = "treePlayButton";
            this.treePlayButton.Size = new System.Drawing.Size(53, 23);
            this.treePlayButton.TabIndex = 7;
            this.treePlayButton.Text = "Play";
            this.treePlayButton.UseVisualStyleBackColor = true;
            this.treePlayButton.Click += new System.EventHandler(this.treePlayButton_Click);
            // 
            // treeResetButton
            // 
            this.treeResetButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.treeResetButton.Location = new System.Drawing.Point(6, 293);
            this.treeResetButton.Name = "treeResetButton";
            this.treeResetButton.Size = new System.Drawing.Size(56, 23);
            this.treeResetButton.TabIndex = 6;
            this.treeResetButton.Text = "Reset";
            this.treeResetButton.UseVisualStyleBackColor = true;
            this.treeResetButton.Click += new System.EventHandler(this.treeResetButton_Click);
            // 
            // treeUpButton
            // 
            this.treeUpButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.treeUpButton.Location = new System.Drawing.Point(143, 293);
            this.treeUpButton.Name = "treeUpButton";
            this.treeUpButton.Size = new System.Drawing.Size(69, 23);
            this.treeUpButton.TabIndex = 2;
            this.treeUpButton.Text = "Go Up";
            this.treeUpButton.UseVisualStyleBackColor = true;
            this.treeUpButton.Click += new System.EventHandler(this.treeUpButton_Click);
            // 
            // extrasTabPage
            // 
            this.extrasTabPage.Controls.Add(this.transmitTryParseButton);
            this.extrasTabPage.Controls.Add(this.transmitButton);
            this.extrasTabPage.Controls.Add(this.transmitClearButton);
            this.extrasTabPage.Controls.Add(this.topMostCheckBox);
            this.extrasTabPage.Controls.Add(this.queryDiskSpaceButton);
            this.extrasTabPage.Controls.Add(this.speedTestButton);
            this.extrasTabPage.Controls.Add(this.extraSplitContainer);
            this.extrasTabPage.Location = new System.Drawing.Point(4, 22);
            this.extrasTabPage.Name = "extrasTabPage";
            this.extrasTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.extrasTabPage.Size = new System.Drawing.Size(526, 322);
            this.extrasTabPage.TabIndex = 0;
            this.extrasTabPage.Text = "Extras";
            this.extrasTabPage.UseVisualStyleBackColor = true;
            // 
            // transmitTryParseButton
            // 
            this.transmitTryParseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.transmitTryParseButton.Location = new System.Drawing.Point(84, 293);
            this.transmitTryParseButton.Name = "transmitTryParseButton";
            this.transmitTryParseButton.Size = new System.Drawing.Size(75, 23);
            this.transmitTryParseButton.TabIndex = 9;
            this.transmitTryParseButton.Text = "Try Parse";
            this.transmitTryParseButton.UseVisualStyleBackColor = true;
            this.transmitTryParseButton.Click += new System.EventHandler(this.transmitTryParseButton_Click);
            // 
            // extraSplitContainer
            // 
            this.extraSplitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.extraSplitContainer.Location = new System.Drawing.Point(3, 3);
            this.extraSplitContainer.Name = "extraSplitContainer";
            // 
            // extraSplitContainer.Panel1
            // 
            this.extraSplitContainer.Panel1.Controls.Add(this.transmitTextBox);
            // 
            // extraSplitContainer.Panel2
            // 
            this.extraSplitContainer.Panel2.Controls.Add(this.receiveTextBox);
            this.extraSplitContainer.Size = new System.Drawing.Size(517, 284);
            this.extraSplitContainer.SplitterDistance = 277;
            this.extraSplitContainer.TabIndex = 10;
            // 
            // receiveTextBox
            // 
            this.receiveTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.receiveTextBox.Location = new System.Drawing.Point(0, 0);
            this.receiveTextBox.Multiline = true;
            this.receiveTextBox.Name = "receiveTextBox";
            this.receiveTextBox.ReadOnly = true;
            this.receiveTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.receiveTextBox.Size = new System.Drawing.Size(236, 284);
            this.receiveTextBox.TabIndex = 4;
            this.receiveTextBox.Text = "Click send to get a reply!";
            // 
            // trackInfoTabPage
            // 
            this.trackInfoTabPage.Controls.Add(this.getUrlButton);
            this.trackInfoTabPage.Controls.Add(this.fetchButton);
            this.trackInfoTabPage.Controls.Add(this.metaLayoutTable);
            this.trackInfoTabPage.Controls.Add(this.coverPictureBox);
            this.trackInfoTabPage.Location = new System.Drawing.Point(4, 22);
            this.trackInfoTabPage.Name = "trackInfoTabPage";
            this.trackInfoTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.trackInfoTabPage.Size = new System.Drawing.Size(526, 322);
            this.trackInfoTabPage.TabIndex = 2;
            this.trackInfoTabPage.Text = "Track info";
            this.trackInfoTabPage.UseVisualStyleBackColor = true;
            // 
            // getUrlButton
            // 
            this.getUrlButton.Location = new System.Drawing.Point(67, 132);
            this.getUrlButton.Name = "getUrlButton";
            this.getUrlButton.Size = new System.Drawing.Size(63, 23);
            this.getUrlButton.TabIndex = 6;
            this.getUrlButton.Text = "Get URL";
            this.getUrlButton.UseVisualStyleBackColor = true;
            // 
            // fetchButton
            // 
            this.fetchButton.Location = new System.Drawing.Point(8, 132);
            this.fetchButton.Name = "fetchButton";
            this.fetchButton.Size = new System.Drawing.Size(53, 23);
            this.fetchButton.TabIndex = 5;
            this.fetchButton.Text = "Fetch";
            this.fetchButton.UseVisualStyleBackColor = true;
            // 
            // metaLayoutTable
            // 
            this.metaLayoutTable.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.metaLayoutTable.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            this.metaLayoutTable.ColumnCount = 2;
            this.metaLayoutTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.metaLayoutTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 70F));
            this.metaLayoutTable.Controls.Add(this.idLabel, 1, 4);
            this.metaLayoutTable.Controls.Add(this.genreLabel, 1, 3);
            this.metaLayoutTable.Controls.Add(this.artistLabel, 1, 2);
            this.metaLayoutTable.Controls.Add(this.titleLabel, 0, 0);
            this.metaLayoutTable.Controls.Add(this.label3, 0, 2);
            this.metaLayoutTable.Controls.Add(this.label2, 0, 1);
            this.metaLayoutTable.Controls.Add(this.label4, 0, 3);
            this.metaLayoutTable.Controls.Add(this.label5, 0, 4);
            this.metaLayoutTable.Controls.Add(this.albumLabel, 1, 1);
            this.metaLayoutTable.Location = new System.Drawing.Point(174, 6);
            this.metaLayoutTable.Name = "metaLayoutTable";
            this.metaLayoutTable.RowCount = 6;
            this.metaLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.metaLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.metaLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.metaLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.metaLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.metaLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.metaLayoutTable.Size = new System.Drawing.Size(344, 120);
            this.metaLayoutTable.TabIndex = 4;
            // 
            // idLabel
            // 
            this.idLabel.AutoEllipsis = true;
            this.idLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.idLabel.Location = new System.Drawing.Point(107, 95);
            this.idLabel.Name = "idLabel";
            this.idLabel.Size = new System.Drawing.Size(233, 20);
            this.idLabel.TabIndex = 9;
            this.idLabel.Text = "label8";
            this.idLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // genreLabel
            // 
            this.genreLabel.AutoEllipsis = true;
            this.genreLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.genreLabel.Location = new System.Drawing.Point(107, 74);
            this.genreLabel.Name = "genreLabel";
            this.genreLabel.Size = new System.Drawing.Size(233, 20);
            this.genreLabel.TabIndex = 8;
            this.genreLabel.Text = "label7";
            this.genreLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // artistLabel
            // 
            this.artistLabel.AutoEllipsis = true;
            this.artistLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.artistLabel.Location = new System.Drawing.Point(107, 53);
            this.artistLabel.Name = "artistLabel";
            this.artistLabel.Size = new System.Drawing.Size(233, 20);
            this.artistLabel.TabIndex = 7;
            this.artistLabel.Text = "label6";
            this.artistLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // titleLabel
            // 
            this.titleLabel.AutoEllipsis = true;
            this.titleLabel.BackColor = System.Drawing.Color.White;
            this.metaLayoutTable.SetColumnSpan(this.titleLabel, 2);
            this.titleLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.titleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.Location = new System.Drawing.Point(4, 1);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(336, 30);
            this.titleLabel.TabIndex = 1;
            this.titleLabel.Text = "Title";
            this.titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            this.label3.AutoEllipsis = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Location = new System.Drawing.Point(4, 53);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(96, 20);
            this.label3.TabIndex = 3;
            this.label3.Text = "Artist";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label2
            // 
            this.label2.AutoEllipsis = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(4, 32);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(96, 20);
            this.label2.TabIndex = 2;
            this.label2.Text = "Album";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label4
            // 
            this.label4.AutoEllipsis = true;
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Location = new System.Drawing.Point(4, 74);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(96, 20);
            this.label4.TabIndex = 4;
            this.label4.Text = "Genre";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label5
            // 
            this.label5.AutoEllipsis = true;
            this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label5.Location = new System.Drawing.Point(4, 95);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(96, 20);
            this.label5.TabIndex = 5;
            this.label5.Text = "ID";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // albumLabel
            // 
            this.albumLabel.AutoEllipsis = true;
            this.albumLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.albumLabel.Location = new System.Drawing.Point(107, 32);
            this.albumLabel.Name = "albumLabel";
            this.albumLabel.Size = new System.Drawing.Size(233, 20);
            this.albumLabel.TabIndex = 6;
            this.albumLabel.Text = "label1";
            this.albumLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // coverPictureBox
            // 
            this.coverPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.coverPictureBox.Location = new System.Drawing.Point(8, 6);
            this.coverPictureBox.Name = "coverPictureBox";
            this.coverPictureBox.Size = new System.Drawing.Size(160, 120);
            this.coverPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.coverPictureBox.TabIndex = 0;
            this.coverPictureBox.TabStop = false;
            // 
            // playUriTextBox
            // 
            this.playUriTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.playUriTextBox.Location = new System.Drawing.Point(4, 354);
            this.playUriTextBox.Name = "playUriTextBox";
            this.playUriTextBox.Size = new System.Drawing.Size(428, 20);
            this.playUriTextBox.TabIndex = 10;
            this.playUriTextBox.Text = "http://mp3-live.swr3.de/swr3raka03_m.m3u";
            // 
            // playUriButton
            // 
            this.playUriButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.playUriButton.Location = new System.Drawing.Point(438, 352);
            this.playUriButton.Name = "playUriButton";
            this.playUriButton.Size = new System.Drawing.Size(92, 23);
            this.playUriButton.TabIndex = 11;
            this.playUriButton.Text = "Play URI";
            this.playUriButton.UseVisualStyleBackColor = true;
            this.playUriButton.Click += new System.EventHandler(this.playUriButton_Click);
            // 
            // treeGoButton
            // 
            this.treeGoButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.treeGoButton.Location = new System.Drawing.Point(218, 293);
            this.treeGoButton.Name = "treeGoButton";
            this.treeGoButton.Size = new System.Drawing.Size(69, 23);
            this.treeGoButton.TabIndex = 8;
            this.treeGoButton.Text = "Go";
            this.treeGoButton.UseVisualStyleBackColor = true;
            this.treeGoButton.Click += new System.EventHandler(this.treeGoButton_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(534, 381);
            this.Controls.Add(this.playUriButton);
            this.Controls.Add(this.playUriTextBox);
            this.Controls.Add(this.tabControl1);
            this.Name = "MainForm";
            this.Text = "MCI500H - nxgmci - ADM Test";
            this.tabControl1.ResumeLayout(false);
            this.mediaTabPage.ResumeLayout(false);
            this.browserTabPage.ResumeLayout(false);
            this.browserTabPage.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.extrasTabPage.ResumeLayout(false);
            this.extrasTabPage.PerformLayout();
            this.extraSplitContainer.Panel1.ResumeLayout(false);
            this.extraSplitContainer.Panel1.PerformLayout();
            this.extraSplitContainer.Panel2.ResumeLayout(false);
            this.extraSplitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.extraSplitContainer)).EndInit();
            this.extraSplitContainer.ResumeLayout(false);
            this.trackInfoTabPage.ResumeLayout(false);
            this.metaLayoutTable.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.coverPictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button speedTestButton;
        private System.Windows.Forms.Button mediaPlayButton;
        private System.Windows.Forms.TextBox transmitTextBox;
        private System.Windows.Forms.Button transmitButton;
        private System.Windows.Forms.Button transmitClearButton;
        private System.Windows.Forms.CheckBox topMostCheckBox;
        private System.Windows.Forms.Button mediaFetchButton;
        private System.Windows.Forms.Button queryDiskSpaceButton;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage mediaTabPage;
        private System.Windows.Forms.TabPage extrasTabPage;
        private System.Windows.Forms.ListView mediaView;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.Button mediaStopButton;
        private System.Windows.Forms.Button mediaPauseButton;
        private System.Windows.Forms.Button mediaSkipAheadButton;
        private System.Windows.Forms.Button mediaSkipBackButton;
        private System.Windows.Forms.TextBox playUriTextBox;
        private System.Windows.Forms.Button playUriButton;
        private System.Windows.Forms.Button mediaInfoButton;
        private System.Windows.Forms.TabPage trackInfoTabPage;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.PictureBox coverPictureBox;
        private System.Windows.Forms.TableLayoutPanel metaLayoutTable;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label idLabel;
        private System.Windows.Forms.Label genreLabel;
        private System.Windows.Forms.Label artistLabel;
        private System.Windows.Forms.Label albumLabel;
        private System.Windows.Forms.Button getUrlButton;
        private System.Windows.Forms.Button fetchButton;
        private System.Windows.Forms.Button transmitTryParseButton;
        private System.Windows.Forms.SplitContainer extraSplitContainer;
        private System.Windows.Forms.TextBox receiveTextBox;
        private System.Windows.Forms.TabPage browserTabPage;
        private System.Windows.Forms.Button treeUpButton;
        private System.Windows.Forms.ListBox treeStackListBox;
        private System.Windows.Forms.Button treeResetButton;
        private System.Windows.Forms.TextBox treeInfoTextBox;
        private System.Windows.Forms.ListBox treeItemsListBox;
        private System.Windows.Forms.Button treePlayButton;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button viewIDButton;
        private System.Windows.Forms.Button treeGoButton;
    }
}

