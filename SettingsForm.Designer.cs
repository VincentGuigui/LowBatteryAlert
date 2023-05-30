namespace LowBatteryAlert
{
    partial class SettingsForm
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            numAlertLevel = new NumericUpDown();
            label1 = new Label();
            btnCancel = new Button();
            btnOK = new Button();
            timer = new System.Windows.Forms.Timer(components);
            notifyIcon = new NotifyIcon(components);
            contextMenuStrip = new ContextMenuStrip(components);
            toolStripMenuItemSettings = new ToolStripMenuItem();
            toolStripMenuItemClose = new ToolStripMenuItem();
            lstBatteries = new ComboBox();
            label2 = new Label();
            label3 = new Label();
            lblCurrentLevel = new Label();
            chAutoLaunch = new CheckBox();
            ((System.ComponentModel.ISupportInitialize)numAlertLevel).BeginInit();
            contextMenuStrip.SuspendLayout();
            SuspendLayout();
            // 
            // numAlertLevel
            // 
            numAlertLevel.Location = new Point(216, 106);
            numAlertLevel.Name = "numAlertLevel";
            numAlertLevel.Size = new Size(546, 39);
            numAlertLevel.TabIndex = 2;
            numAlertLevel.TextAlign = HorizontalAlignment.Center;
            numAlertLevel.Value = new decimal(new int[] { 10, 0, 0, 0 });
            numAlertLevel.ValueChanged += numAlertLevel_ValueChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 15);
            label1.Name = "label1";
            label1.Size = new Size(179, 32);
            label1.TabIndex = 7;
            label1.Text = "Select a battery";
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(612, 171);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(150, 46);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // btnOK
            // 
            btnOK.Anchor = AnchorStyles.Bottom;
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Location = new Point(456, 171);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(150, 46);
            btnOK.TabIndex = 3;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // timer
            // 
            timer.Enabled = true;
            timer.Interval = 60000;
            timer.Tick += timer_Tick;
            // 
            // notifyIcon
            // 
            notifyIcon.BalloonTipIcon = ToolTipIcon.Warning;
            notifyIcon.ContextMenuStrip = contextMenuStrip;
            notifyIcon.Icon = (Icon)resources.GetObject("notifyIcon.Icon");
            notifyIcon.Text = "LowBatteryAlert";
            notifyIcon.Visible = true;
            notifyIcon.DoubleClick += notifyIcon_DoubleClick;
            // 
            // contextMenuStrip
            // 
            contextMenuStrip.ImageScalingSize = new Size(32, 32);
            contextMenuStrip.Items.AddRange(new ToolStripItem[] { toolStripMenuItemSettings, toolStripMenuItemClose });
            contextMenuStrip.Name = "contextMenuStrip";
            contextMenuStrip.Size = new Size(190, 80);
            contextMenuStrip.Text = "Low Battery Alert";
            contextMenuStrip.ItemClicked += contextMenuStrip_ItemClicked;
            // 
            // toolStripMenuItemSettings
            // 
            toolStripMenuItemSettings.Name = "toolStripMenuItemSettings";
            toolStripMenuItemSettings.Size = new Size(189, 38);
            toolStripMenuItemSettings.Text = "&Settings...";
            // 
            // toolStripMenuItemClose
            // 
            toolStripMenuItemClose.Name = "toolStripMenuItemClose";
            toolStripMenuItemClose.Size = new Size(189, 38);
            toolStripMenuItemClose.Text = "&Close";
            // 
            // lstBatteries
            // 
            lstBatteries.FormattingEnabled = true;
            lstBatteries.Location = new Point(216, 12);
            lstBatteries.Name = "lstBatteries";
            lstBatteries.Size = new Size(548, 40);
            lstBatteries.TabIndex = 1;
            lstBatteries.SelectedIndexChanged += lstBatteries_SelectedIndexChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 108);
            label2.Name = "label2";
            label2.Size = new Size(159, 32);
            label2.TabIndex = 8;
            label2.Text = "Set alert level";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 61);
            label3.Name = "label3";
            label3.Size = new Size(151, 32);
            label3.TabIndex = 8;
            label3.Text = "Current level";
            // 
            // lblCurrentLevel
            // 
            lblCurrentLevel.AutoSize = true;
            lblCurrentLevel.Location = new Point(216, 61);
            lblCurrentLevel.Name = "lblCurrentLevel";
            lblCurrentLevel.Size = new Size(34, 32);
            lblCurrentLevel.TabIndex = 8;
            lblCurrentLevel.Text = "%";
            // 
            // chAutoLaunch
            // 
            chAutoLaunch.AutoSize = true;
            chAutoLaunch.Location = new Point(17, 177);
            chAutoLaunch.Name = "chAutoLaunch";
            chAutoLaunch.Size = new Size(334, 36);
            chAutoLaunch.TabIndex = 9;
            chAutoLaunch.Text = "Launch at Windows startup";
            chAutoLaunch.UseVisualStyleBackColor = true;
            // 
            // SettingsForm
            // 
            AcceptButton = btnOK;
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(774, 229);
            Controls.Add(chAutoLaunch);
            Controls.Add(numAlertLevel);
            Controls.Add(label1);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            Controls.Add(lstBatteries);
            Controls.Add(lblCurrentLevel);
            Controls.Add(label3);
            Controls.Add(label2);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MaximumSize = new Size(800, 300);
            MinimumSize = new Size(800, 300);
            Name = "SettingsForm";
            Text = "Low Battery Alert - Settings";
            FormClosing += SettingsForm_FormClosing;
            Load += SettingsForm_Load;
            ((System.ComponentModel.ISupportInitialize)numAlertLevel).EndInit();
            contextMenuStrip.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private NumericUpDown numAlertLevel;
        private Label label1;
        private Button btnCancel;
        private Button btnOK;
        private System.Windows.Forms.Timer timer;
        private NotifyIcon notifyIcon;
        private ComboBox lstBatteries;
        private Label label2;
        private Label label3;
        private Label lblCurrentLevel;
        private ContextMenuStrip contextMenuStrip;
        private ToolStripMenuItem toolStripMenuItemSettings;
        private ToolStripMenuItem toolStripMenuItemClose;
        private CheckBox chAutoLaunch;
    }
}