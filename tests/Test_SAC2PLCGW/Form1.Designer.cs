namespace Test_SAC2PLCGW
{
    partial class Form1
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.debugingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.networkToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuClossEngineCom = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.mnuCloseCCTVCom = new System.Windows.Forms.ToolStripMenuItem();
            this.testingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.testing1byValueAndByReferenceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuExternal = new System.Windows.Forms.ToolStripTextBox();
            this.mnuInternal = new System.Windows.Forms.ToolStripTextBox();
            this.txtData = new System.Windows.Forms.TextBox();
            this.btnToCCTV = new System.Windows.Forms.Button();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.btnToEnge = new System.Windows.Forms.Button();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.debugingToolStripMenuItem,
            this.networkToolStripMenuItem,
            this.testingToolStripMenuItem,
            this.exitToolStripMenuItem,
            this.mnuExternal,
            this.mnuInternal});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(472, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(52, 20);
            this.fileToolStripMenuItem.Text = "&About";
            // 
            // debugingToolStripMenuItem
            // 
            this.debugingToolStripMenuItem.Name = "debugingToolStripMenuItem";
            this.debugingToolStripMenuItem.Size = new System.Drawing.Size(103, 20);
            this.debugingToolStripMenuItem.Text = "Show &Debuging";
            // 
            // networkToolStripMenuItem
            // 
            this.networkToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuClossEngineCom,
            this.toolStripMenuItem1,
            this.mnuCloseCCTVCom});
            this.networkToolStripMenuItem.Name = "networkToolStripMenuItem";
            this.networkToolStripMenuItem.Size = new System.Drawing.Size(64, 20);
            this.networkToolStripMenuItem.Text = "Network";
            // 
            // mnuClossEngineCom
            // 
            this.mnuClossEngineCom.Name = "mnuClossEngineCom";
            this.mnuClossEngineCom.Size = new System.Drawing.Size(226, 22);
            this.mnuClossEngineCom.Text = "Close Connections to Engine";
            this.mnuClossEngineCom.Click += new System.EventHandler(this.mnuClossEngineCom_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(223, 6);
            // 
            // mnuCloseCCTVCom
            // 
            this.mnuCloseCCTVCom.Name = "mnuCloseCCTVCom";
            this.mnuCloseCCTVCom.Size = new System.Drawing.Size(226, 22);
            this.mnuCloseCCTVCom.Text = "Close Connections to CCTV";
            this.mnuCloseCCTVCom.Click += new System.EventHandler(this.mnuCloseCCTVCom_Click);
            // 
            // testingToolStripMenuItem
            // 
            this.testingToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.testing1byValueAndByReferenceToolStripMenuItem});
            this.testingToolStripMenuItem.Name = "testingToolStripMenuItem";
            this.testingToolStripMenuItem.Size = new System.Drawing.Size(58, 20);
            this.testingToolStripMenuItem.Text = "Testing";
            // 
            // testing1byValueAndByReferenceToolStripMenuItem
            // 
            this.testing1byValueAndByReferenceToolStripMenuItem.Name = "testing1byValueAndByReferenceToolStripMenuItem";
            this.testing1byValueAndByReferenceToolStripMenuItem.Size = new System.Drawing.Size(268, 22);
            this.testing1byValueAndByReferenceToolStripMenuItem.Text = "Testing 1 (by value and by reference)";
            this.testing1byValueAndByReferenceToolStripMenuItem.Click += new System.EventHandler(this.testing1byValueAndByReferenceToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // mnuExternal
            // 
            this.mnuExternal.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.mnuExternal.AutoSize = false;
            this.mnuExternal.BackColor = System.Drawing.Color.Red;
            this.mnuExternal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.mnuExternal.Font = new System.Drawing.Font("Tahoma", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.mnuExternal.Margin = new System.Windows.Forms.Padding(1);
            this.mnuExternal.Name = "mnuExternal";
            this.mnuExternal.ReadOnly = true;
            this.mnuExternal.Size = new System.Drawing.Size(65, 18);
            this.mnuExternal.Text = "External";
            this.mnuExternal.TextBoxTextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // mnuInternal
            // 
            this.mnuInternal.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.mnuInternal.AutoSize = false;
            this.mnuInternal.BackColor = System.Drawing.Color.Lime;
            this.mnuInternal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.mnuInternal.Font = new System.Drawing.Font("Tahoma", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.mnuInternal.Name = "mnuInternal";
            this.mnuInternal.ReadOnly = true;
            this.mnuInternal.Size = new System.Drawing.Size(65, 18);
            this.mnuInternal.Text = "Internal";
            this.mnuInternal.TextBoxTextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtData
            // 
            this.txtData.Location = new System.Drawing.Point(1, 32);
            this.txtData.Multiline = true;
            this.txtData.Name = "txtData";
            this.txtData.Size = new System.Drawing.Size(399, 50);
            this.txtData.TabIndex = 1;
            this.txtData.Text = "030101261234CT1       CT1-15          20090115-235959530AA_TRIP   CAM 000101Motor" +
                " Trip                                        ";
            // 
            // btnToCCTV
            // 
            this.btnToCCTV.Location = new System.Drawing.Point(402, 59);
            this.btnToCCTV.Name = "btnToCCTV";
            this.btnToCCTV.Size = new System.Drawing.Size(69, 23);
            this.btnToCCTV.TabIndex = 2;
            this.btnToCCTV.Text = ">> PLC";
            this.btnToCCTV.UseVisualStyleBackColor = true;
            this.btnToCCTV.Click += new System.EventHandler(this.btnToCCTV_Click);
            // 
            // listBox1
            // 
            this.listBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(1, 84);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(470, 199);
            this.listBox1.TabIndex = 3;
            // 
            // btnToEnge
            // 
            this.btnToEnge.Location = new System.Drawing.Point(402, 32);
            this.btnToEnge.Name = "btnToEnge";
            this.btnToEnge.Size = new System.Drawing.Size(69, 23);
            this.btnToEnge.TabIndex = 2;
            this.btnToEnge.Text = ">> Engine";
            this.btnToEnge.UseVisualStyleBackColor = true;
            this.btnToEnge.Click += new System.EventHandler(this.btnToEnge_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(472, 284);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.btnToEnge);
            this.Controls.Add(this.btnToCCTV);
            this.Controls.Add(this.txtData);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Test - SAC2PLC GW";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem debugingToolStripMenuItem;
        private System.Windows.Forms.TextBox txtData;
        private System.Windows.Forms.Button btnToCCTV;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.Button btnToEnge;
        private System.Windows.Forms.ToolStripMenuItem networkToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mnuClossEngineCom;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem mnuCloseCCTVCom;
        private System.Windows.Forms.ToolStripMenuItem testingToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem testing1byValueAndByReferenceToolStripMenuItem;
        private System.Windows.Forms.ToolStripTextBox mnuExternal;
        private System.Windows.Forms.ToolStripTextBox mnuInternal;
    }
}

