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
            memoryDeviceCombo = new System.Windows.Forms.ComboBox();
            label1 = new System.Windows.Forms.Label();
            rtb = new System.Windows.Forms.RichTextBox();
            noteLabel = new System.Windows.Forms.Label();
            jumpButton = new System.Windows.Forms.Button();
            jumpTextBox = new System.Windows.Forms.TextBox();
            SuspendLayout();
            // 
            // memoryDeviceCombo
            // 
            memoryDeviceCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            memoryDeviceCombo.FormattingEnabled = true;
            memoryDeviceCombo.Items.AddRange(new object[] { "", "ROM", "RAM", "ERAM", "VIDEO" });
            memoryDeviceCombo.Location = new System.Drawing.Point(114, 7);
            memoryDeviceCombo.Margin = new System.Windows.Forms.Padding(1, 1, 1, 1);
            memoryDeviceCombo.Name = "memoryDeviceCombo";
            memoryDeviceCombo.Size = new System.Drawing.Size(81, 23);
            memoryDeviceCombo.TabIndex = 0;
            memoryDeviceCombo.SelectedIndexChanged += memoryDeviceCombo_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(13, 7);
            label1.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(90, 15);
            label1.TabIndex = 1;
            label1.Text = "Memory Device";
            // 
            // rtb
            // 
            rtb.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            rtb.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            rtb.Location = new System.Drawing.Point(13, 33);
            rtb.Margin = new System.Windows.Forms.Padding(1, 1, 1, 1);
            rtb.Name = "rtb";
            rtb.Size = new System.Drawing.Size(450, 266);
            rtb.TabIndex = 2;
            rtb.Text = "";
            // 
            // noteLabel
            // 
            noteLabel.AutoSize = true;
            noteLabel.Font = new System.Drawing.Font("Segoe UI", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            noteLabel.Location = new System.Drawing.Point(199, 3);
            noteLabel.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            noteLabel.Name = "noteLabel";
            noteLabel.Size = new System.Drawing.Size(9, 12);
            noteLabel.TabIndex = 3;
            noteLabel.Text = "-";
            // 
            // jumpButton
            // 
            jumpButton.Font = new System.Drawing.Font("Segoe UI", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            jumpButton.Location = new System.Drawing.Point(405, 7);
            jumpButton.Margin = new System.Windows.Forms.Padding(1, 1, 1, 1);
            jumpButton.Name = "jumpButton";
            jumpButton.Size = new System.Drawing.Size(42, 19);
            jumpButton.TabIndex = 4;
            jumpButton.Text = "Jump";
            jumpButton.UseVisualStyleBackColor = true;
            jumpButton.Click += jumpButton_Click;
            // 
            // jumpTextBox
            // 
            jumpTextBox.Location = new System.Drawing.Point(356, 8);
            jumpTextBox.Margin = new System.Windows.Forms.Padding(1, 1, 1, 1);
            jumpTextBox.Name = "jumpTextBox";
            jumpTextBox.Size = new System.Drawing.Size(48, 23);
            jumpTextBox.TabIndex = 5;
            // 
            // MemoryViewer
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(474, 309);
            Controls.Add(jumpTextBox);
            Controls.Add(jumpButton);
            Controls.Add(noteLabel);
            Controls.Add(rtb);
            Controls.Add(label1);
            Controls.Add(memoryDeviceCombo);
            Margin = new System.Windows.Forms.Padding(1, 1, 1, 1);
            Name = "MemoryViewer";
            Text = "MemoryViewer";
            Load += MemoryViewer_Load;
            ResumeLayout(false);
            PerformLayout();
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