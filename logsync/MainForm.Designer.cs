namespace LogSync
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.openLogDialog = new System.Windows.Forms.OpenFileDialog();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.lvFights = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader6 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader4 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader5 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader7 = new System.Windows.Forms.ColumnHeader();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnOpen = new System.Windows.Forms.Button();
            this.textLogPath = new System.Windows.Forms.TextBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.textSearch = new System.Windows.Forms.TextBox();
            this.lnkSelectZone = new System.Windows.Forms.LinkLabel();
            this.lnkSelectDate = new System.Windows.Forms.LinkLabel();
            this.label2 = new System.Windows.Forms.Label();
            this.btnCombine = new System.Windows.Forms.Button();
            this.btnUpload = new System.Windows.Forms.Button();
            this.chkAutoDiscord = new System.Windows.Forms.CheckBox();
            this.chkAutoUpload = new System.Windows.Forms.CheckBox();
            this.btnChannel = new System.Windows.Forms.Button();
            this.textLog = new System.Windows.Forms.TextBox();
            this.statusStrip1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // openLogDialog
            // 
            this.openLogDialog.DefaultExt = "txt";
            this.openLogDialog.Filter = "EQ Log File|eqlog*.txt;eqlog*.txt.gz";
            this.openLogDialog.Title = "Select a log file";
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripStatusLabel2});
            this.statusStrip1.Location = new System.Drawing.Point(0, 539);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.statusStrip1.Size = new System.Drawing.Size(834, 22);
            this.statusStrip1.TabIndex = 8;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.AutoSize = false;
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(80, 17);
            this.toolStripStatusLabel1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // toolStripStatusLabel2
            // 
            this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            this.toolStripStatusLabel2.Size = new System.Drawing.Size(0, 17);
            // 
            // lvFights
            // 
            this.lvFights.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lvFights.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader6,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader7});
            this.lvFights.FullRowSelect = true;
            this.lvFights.GridLines = true;
            this.lvFights.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lvFights.HideSelection = false;
            this.lvFights.LabelEdit = true;
            this.lvFights.Location = new System.Drawing.Point(11, 55);
            this.lvFights.Margin = new System.Windows.Forms.Padding(3, 240, 3, 3);
            this.lvFights.Name = "lvFights";
            this.lvFights.Size = new System.Drawing.Size(811, 291);
            this.lvFights.TabIndex = 0;
            this.lvFights.UseCompatibleStateImageBehavior = false;
            this.lvFights.View = System.Windows.Forms.View.Details;
            this.lvFights.VirtualMode = true;
            this.lvFights.AfterLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.lvFights_AfterLabelEdit);
            this.lvFights.BeforeLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.lvFights_BeforeLabelEdit);
            this.lvFights.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.lvFights_ItemSelectionChanged);
            this.lvFights.RetrieveVirtualItem += new System.Windows.Forms.RetrieveVirtualItemEventHandler(this.lvFights_RetrieveVirtualItem);
            this.lvFights.VirtualItemsSelectionRangeChanged += new System.Windows.Forms.ListViewVirtualItemsSelectionRangeChangedEventHandler(this.lvFights_VirtualItemsSelectionRangeChanged);
            this.lvFights.DoubleClick += new System.EventHandler(this.btnUpload_Click);
            this.lvFights.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.lvFights_PreviewKeyDown);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Mob";
            this.columnHeader1.Width = 150;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Zone";
            this.columnHeader2.Width = 150;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Date/Time";
            this.columnHeader3.Width = 150;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "HP";
            this.columnHeader6.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Duration";
            this.columnHeader4.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "Party";
            this.columnHeader5.Width = 80;
            // 
            // columnHeader7
            // 
            this.columnHeader7.Text = "Status";
            this.columnHeader7.Width = 80;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnOpen);
            this.panel1.Controls.Add(this.textLogPath);
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(834, 52);
            this.panel1.TabIndex = 0;
            // 
            // btnOpen
            // 
            this.btnOpen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOpen.Location = new System.Drawing.Point(703, 9);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(114, 33);
            this.btnOpen.TabIndex = 1;
            this.btnOpen.Text = "Open";
            this.btnOpen.UseVisualStyleBackColor = true;
            this.btnOpen.Click += new System.EventHandler(this.btnOpen_Click);
            // 
            // textLogPath
            // 
            this.textLogPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textLogPath.Location = new System.Drawing.Point(11, 15);
            this.textLogPath.Name = "textLogPath";
            this.textLogPath.PlaceholderText = "Log File";
            this.textLogPath.ReadOnly = true;
            this.textLogPath.Size = new System.Drawing.Size(686, 23);
            this.textLogPath.TabIndex = 0;
            this.textLogPath.TabStop = false;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.textSearch);
            this.panel2.Controls.Add(this.lnkSelectZone);
            this.panel2.Controls.Add(this.lnkSelectDate);
            this.panel2.Controls.Add(this.label2);
            this.panel2.Controls.Add(this.btnCombine);
            this.panel2.Controls.Add(this.btnUpload);
            this.panel2.Controls.Add(this.chkAutoDiscord);
            this.panel2.Controls.Add(this.chkAutoUpload);
            this.panel2.Controls.Add(this.btnChannel);
            this.panel2.Controls.Add(this.textLog);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel2.Location = new System.Drawing.Point(0, 349);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(834, 190);
            this.panel2.TabIndex = 2;
            // 
            // textSearch
            // 
            this.textSearch.Location = new System.Drawing.Point(12, 5);
            this.textSearch.Name = "textSearch";
            this.textSearch.PlaceholderText = "Search...";
            this.textSearch.Size = new System.Drawing.Size(169, 23);
            this.textSearch.TabIndex = 0;
            this.textSearch.TextChanged += new System.EventHandler(this.textSearch_TextChanged);
            this.textSearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textSearch_KeyDown);
            // 
            // lnkSelectZone
            // 
            this.lnkSelectZone.AutoSize = true;
            this.lnkSelectZone.Location = new System.Drawing.Point(334, 8);
            this.lnkSelectZone.Name = "lnkSelectZone";
            this.lnkSelectZone.Size = new System.Drawing.Size(34, 15);
            this.lnkSelectZone.TabIndex = 1;
            this.lnkSelectZone.TabStop = true;
            this.lnkSelectZone.Text = "Zone";
            this.lnkSelectZone.Visible = false;
            this.lnkSelectZone.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkSelectZone_LinkClicked);
            // 
            // lnkSelectDate
            // 
            this.lnkSelectDate.AutoSize = true;
            this.lnkSelectDate.Location = new System.Drawing.Point(277, 8);
            this.lnkSelectDate.Name = "lnkSelectDate";
            this.lnkSelectDate.Size = new System.Drawing.Size(31, 15);
            this.lnkSelectDate.TabIndex = 0;
            this.lnkSelectDate.TabStop = true;
            this.lnkSelectDate.Text = "Date";
            this.lnkSelectDate.Visible = false;
            this.lnkSelectDate.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkSelectDate_LinkClicked);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(213, 8);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(58, 15);
            this.label2.TabIndex = 6;
            this.label2.Text = "Select All:";
            this.label2.Visible = false;
            // 
            // btnCombine
            // 
            this.btnCombine.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCombine.Enabled = false;
            this.btnCombine.Location = new System.Drawing.Point(521, 16);
            this.btnCombine.Name = "btnCombine";
            this.btnCombine.Size = new System.Drawing.Size(56, 33);
            this.btnCombine.TabIndex = 4;
            this.btnCombine.Text = "...";
            this.btnCombine.UseVisualStyleBackColor = true;
            this.btnCombine.Click += new System.EventHandler(this.btnCombine_Click);
            // 
            // btnUpload
            // 
            this.btnUpload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnUpload.Location = new System.Drawing.Point(583, 16);
            this.btnUpload.Name = "btnUpload";
            this.btnUpload.Size = new System.Drawing.Size(114, 33);
            this.btnUpload.TabIndex = 5;
            this.btnUpload.Text = "Upload";
            this.btnUpload.UseVisualStyleBackColor = true;
            this.btnUpload.Click += new System.EventHandler(this.btnUpload_Click);
            // 
            // chkAutoDiscord
            // 
            this.chkAutoDiscord.AutoSize = true;
            this.chkAutoDiscord.Location = new System.Drawing.Point(12, 61);
            this.chkAutoDiscord.Name = "chkAutoDiscord";
            this.chkAutoDiscord.Size = new System.Drawing.Size(380, 19);
            this.chkAutoDiscord.TabIndex = 3;
            this.chkAutoDiscord.TabStop = false;
            this.chkAutoDiscord.Text = "Auto upload fights to discord.com (that occur after this is checked)";
            this.chkAutoDiscord.UseVisualStyleBackColor = true;
            this.chkAutoDiscord.Visible = false;
            // 
            // chkAutoUpload
            // 
            this.chkAutoUpload.AutoSize = true;
            this.chkAutoUpload.Location = new System.Drawing.Point(12, 36);
            this.chkAutoUpload.Name = "chkAutoUpload";
            this.chkAutoUpload.Size = new System.Drawing.Size(382, 19);
            this.chkAutoUpload.TabIndex = 2;
            this.chkAutoUpload.TabStop = false;
            this.chkAutoUpload.Text = "Auto upload fights to raidloot.com (that occur after this is checked)";
            this.chkAutoUpload.UseVisualStyleBackColor = true;
            // 
            // btnChannel
            // 
            this.btnChannel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnChannel.Location = new System.Drawing.Point(703, 16);
            this.btnChannel.Name = "btnChannel";
            this.btnChannel.Size = new System.Drawing.Size(114, 33);
            this.btnChannel.TabIndex = 6;
            this.btnChannel.Text = "View Channel";
            this.btnChannel.UseVisualStyleBackColor = true;
            this.btnChannel.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // textLog
            // 
            this.textLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textLog.Location = new System.Drawing.Point(11, 88);
            this.textLog.Multiline = true;
            this.textLog.Name = "textLog";
            this.textLog.ReadOnly = true;
            this.textLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textLog.Size = new System.Drawing.Size(811, 91);
            this.textLog.TabIndex = 7;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(834, 561);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.lvFights);
            this.Controls.Add(this.statusStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MinimumSize = new System.Drawing.Size(800, 500);
            this.Name = "MainForm";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.OpenFileDialog openLogDialog;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
        private System.Windows.Forms.ListView lvFights;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnOpen;
        private System.Windows.Forms.TextBox textLogPath;
        private System.Windows.Forms.ColumnHeader columnHeader7;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button btnChannel;
        private System.Windows.Forms.TextBox textLog;
        private System.Windows.Forms.CheckBox chkAutoDiscord;
        private System.Windows.Forms.CheckBox chkAutoUpload;
        private System.Windows.Forms.Button btnUpload;
        private System.Windows.Forms.Button btnCombine;
        private System.Windows.Forms.LinkLabel lnkSelectZone;
        private System.Windows.Forms.LinkLabel lnkSelectDate;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textSearch;
    }
}

