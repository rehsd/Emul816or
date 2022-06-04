namespace Emul816or
{
    partial class VideoDebug
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
            this.tilesPictureBox = new System.Windows.Forms.PictureBox();
            this.spritesPictureBox = new System.Windows.Forms.PictureBox();
            this.videoOutLabel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.videoOutRefreshTimer = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.tilesPictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.spritesPictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tilesPictureBox
            // 
            this.tilesPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tilesPictureBox.Location = new System.Drawing.Point(0, 43);
            this.tilesPictureBox.Name = "tilesPictureBox";
            this.tilesPictureBox.Size = new System.Drawing.Size(497, 441);
            this.tilesPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.tilesPictureBox.TabIndex = 18;
            this.tilesPictureBox.TabStop = false;
            // 
            // spritesPictureBox
            // 
            this.spritesPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.spritesPictureBox.Location = new System.Drawing.Point(7, 43);
            this.spritesPictureBox.Name = "spritesPictureBox";
            this.spritesPictureBox.Size = new System.Drawing.Size(486, 444);
            this.spritesPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.spritesPictureBox.TabIndex = 19;
            this.spritesPictureBox.TabStop = false;
            // 
            // videoOutLabel
            // 
            this.videoOutLabel.AutoSize = true;
            this.videoOutLabel.Location = new System.Drawing.Point(1, 15);
            this.videoOutLabel.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            this.videoOutLabel.Name = "videoOutLabel";
            this.videoOutLabel.Size = new System.Drawing.Size(100, 25);
            this.videoOutLabel.TabIndex = 20;
            this.videoOutLabel.Text = "Page: TILES";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 15);
            this.label1.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(123, 25);
            this.label1.TabIndex = 21;
            this.label1.Text = "Page: SPRITES";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(12, 12);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.tilesPictureBox);
            this.splitContainer1.Panel1.Controls.Add(this.videoOutLabel);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.spritesPictureBox);
            this.splitContainer1.Panel2.Controls.Add(this.label1);
            this.splitContainer1.Size = new System.Drawing.Size(1000, 490);
            this.splitContainer1.SplitterDistance = 500;
            this.splitContainer1.TabIndex = 22;
            // 
            // videoOutRefreshTimer
            // 
            this.videoOutRefreshTimer.Interval = 250;
            this.videoOutRefreshTimer.Tick += new System.EventHandler(this.videoOutRefreshTimer_Tick);
            // 
            // VideoDebug
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1028, 512);
            this.Controls.Add(this.splitContainer1);
            this.Name = "VideoDebug";
            this.Text = "VideoDebug";
            this.Load += new System.EventHandler(this.VideoDebug_Load);
            ((System.ComponentModel.ISupportInitialize)(this.tilesPictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.spritesPictureBox)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.PictureBox tilesPictureBox;
        public System.Windows.Forms.PictureBox spritesPictureBox;
        private System.Windows.Forms.Label videoOutLabel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Timer videoOutRefreshTimer;
    }
}