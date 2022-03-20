namespace Emul816or
{
    partial class MemoryViewer
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
            this.memoryDeviceCombo = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.rtb = new System.Windows.Forms.RichTextBox();
            this.noteLabel = new System.Windows.Forms.Label();
            this.jumpButton = new System.Windows.Forms.Button();
            this.jumpTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // memoryDeviceCombo
            // 
            this.memoryDeviceCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.memoryDeviceCombo.FormattingEnabled = true;
            this.memoryDeviceCombo.Items.AddRange(new object[] {
            "",
            "ROM",
            "RAM",
            "ERAM",
            "VIDEO"});
            this.memoryDeviceCombo.Location = new System.Drawing.Point(278, 20);
            this.memoryDeviceCombo.Name = "memoryDeviceCombo";
            this.memoryDeviceCombo.Size = new System.Drawing.Size(190, 49);
            this.memoryDeviceCombo.TabIndex = 0;
            this.memoryDeviceCombo.SelectedIndexChanged += new System.EventHandler(this.memoryDeviceCombo_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(32, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(226, 41);
            this.label1.TabIndex = 1;
            this.label1.Text = "Memory Device";
            // 
            // rtb
            // 
            this.rtb.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtb.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.rtb.Location = new System.Drawing.Point(32, 89);
            this.rtb.Name = "rtb";
            this.rtb.Size = new System.Drawing.Size(1088, 820);
            this.rtb.TabIndex = 2;
            this.rtb.Text = "";
            // 
            // noteLabel
            // 
            this.noteLabel.AutoSize = true;
            this.noteLabel.Font = new System.Drawing.Font("Segoe UI", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.noteLabel.Location = new System.Drawing.Point(483, 8);
            this.noteLabel.Name = "noteLabel";
            this.noteLabel.Size = new System.Drawing.Size(24, 32);
            this.noteLabel.TabIndex = 3;
            this.noteLabel.Text = "-";
            // 
            // jumpButton
            // 
            this.jumpButton.Font = new System.Drawing.Font("Segoe UI", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.jumpButton.Location = new System.Drawing.Point(983, 20);
            this.jumpButton.Name = "jumpButton";
            this.jumpButton.Size = new System.Drawing.Size(102, 53);
            this.jumpButton.TabIndex = 4;
            this.jumpButton.Text = "Jump";
            this.jumpButton.UseVisualStyleBackColor = true;
            this.jumpButton.Click += new System.EventHandler(this.jumpButton_Click);
            // 
            // jumpTextBox
            // 
            this.jumpTextBox.Location = new System.Drawing.Point(865, 22);
            this.jumpTextBox.Name = "jumpTextBox";
            this.jumpTextBox.Size = new System.Drawing.Size(112, 47);
            this.jumpTextBox.TabIndex = 5;
            // 
            // MemoryViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(17F, 41F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1152, 921);
            this.Controls.Add(this.jumpTextBox);
            this.Controls.Add(this.jumpButton);
            this.Controls.Add(this.noteLabel);
            this.Controls.Add(this.rtb);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.memoryDeviceCombo);
            this.Name = "MemoryViewer";
            this.Text = "MemoryViewer";
            this.Load += new System.EventHandler(this.MemoryViewer_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox memoryDeviceCombo;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox rtb;
        private System.Windows.Forms.Label noteLabel;
        private System.Windows.Forms.Button jumpButton;
        private System.Windows.Forms.TextBox jumpTextBox;
    }
}