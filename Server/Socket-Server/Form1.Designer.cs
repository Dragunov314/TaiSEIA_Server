namespace Socket_Server
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
            this.btnListen = new System.Windows.Forms.Button();
            this.btnSend = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.txtSend = new System.Windows.Forms.TextBox();
            this.onlineStatusBox = new System.Windows.Forms.CheckedListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.portBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cmdBox = new System.Windows.Forms.GroupBox();
            this.HNA_ID_textBox = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.HG_ID_textBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.usrID_textBox = new System.Windows.Forms.TextBox();
            this.cmdBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnListen
            // 
            this.btnListen.Location = new System.Drawing.Point(164, 6);
            this.btnListen.Name = "btnListen";
            this.btnListen.Size = new System.Drawing.Size(75, 23);
            this.btnListen.TabIndex = 0;
            this.btnListen.Text = "Listen";
            this.btnListen.UseVisualStyleBackColor = true;
            this.btnListen.Click += new System.EventHandler(this.btnListen_Click);
            // 
            // btnSend
            // 
            this.btnSend.Location = new System.Drawing.Point(547, 155);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(75, 24);
            this.btnSend.TabIndex = 1;
            this.btnSend.Text = "Send";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // txtLog
            // 
            this.txtLog.Location = new System.Drawing.Point(9, 186);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(613, 261);
            this.txtLog.TabIndex = 2;
            // 
            // txtSend
            // 
            this.txtSend.Location = new System.Drawing.Point(9, 157);
            this.txtSend.Name = "txtSend";
            this.txtSend.Size = new System.Drawing.Size(532, 23);
            this.txtSend.TabIndex = 3;
            // 
            // onlineStatusBox
            // 
            this.onlineStatusBox.FormattingEnabled = true;
            this.onlineStatusBox.Location = new System.Drawing.Point(477, 37);
            this.onlineStatusBox.Name = "onlineStatusBox";
            this.onlineStatusBox.Size = new System.Drawing.Size(134, 76);
            this.onlineStatusBox.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(474, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(98, 15);
            this.label1.TabIndex = 5;
            this.label1.Text = "Online Status";
            // 
            // portBox
            // 
            this.portBox.Location = new System.Drawing.Point(105, 6);
            this.portBox.Name = "portBox";
            this.portBox.Size = new System.Drawing.Size(49, 23);
            this.portBox.TabIndex = 7;
            this.portBox.Text = "8080";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(15, 9);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(84, 15);
            this.label3.TabIndex = 9;
            this.label3.Text = "Server Port";
            // 
            // cmdBox
            // 
            this.cmdBox.Controls.Add(this.HNA_ID_textBox);
            this.cmdBox.Controls.Add(this.label5);
            this.cmdBox.Controls.Add(this.label4);
            this.cmdBox.Controls.Add(this.HG_ID_textBox);
            this.cmdBox.Controls.Add(this.label2);
            this.cmdBox.Controls.Add(this.usrID_textBox);
            this.cmdBox.Controls.Add(this.onlineStatusBox);
            this.cmdBox.Controls.Add(this.label1);
            this.cmdBox.Controls.Add(this.btnSend);
            this.cmdBox.Controls.Add(this.txtSend);
            this.cmdBox.Controls.Add(this.txtLog);
            this.cmdBox.Enabled = false;
            this.cmdBox.Location = new System.Drawing.Point(12, 35);
            this.cmdBox.Name = "cmdBox";
            this.cmdBox.Size = new System.Drawing.Size(628, 453);
            this.cmdBox.TabIndex = 10;
            this.cmdBox.TabStop = false;
            this.cmdBox.Text = " Command Box";
            // 
            // HNA_ID_textBox
            // 
            this.HNA_ID_textBox.Location = new System.Drawing.Point(323, 37);
            this.HNA_ID_textBox.Name = "HNA_ID_textBox";
            this.HNA_ID_textBox.Size = new System.Drawing.Size(52, 23);
            this.HNA_ID_textBox.TabIndex = 11;
            this.HNA_ID_textBox.Text = "2";
            this.HNA_ID_textBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(265, 40);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(49, 15);
            this.label5.TabIndex = 10;
            this.label5.Text = "HNA ID";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(149, 40);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(42, 15);
            this.label4.TabIndex = 9;
            this.label4.Text = "HG ID";
            // 
            // HG_ID_textBox
            // 
            this.HG_ID_textBox.Location = new System.Drawing.Point(197, 37);
            this.HG_ID_textBox.Name = "HG_ID_textBox";
            this.HG_ID_textBox.Size = new System.Drawing.Size(52, 23);
            this.HG_ID_textBox.TabIndex = 8;
            this.HG_ID_textBox.Text = "1";
            this.HG_ID_textBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(28, 40);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(56, 15);
            this.label2.TabIndex = 7;
            this.label2.Text = "USER ID";
            // 
            // usrID_textBox
            // 
            this.usrID_textBox.Location = new System.Drawing.Point(90, 37);
            this.usrID_textBox.Name = "usrID_textBox";
            this.usrID_textBox.Size = new System.Drawing.Size(52, 23);
            this.usrID_textBox.TabIndex = 6;
            this.usrID_textBox.Text = "8787";
            this.usrID_textBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(648, 495);
            this.Controls.Add(this.cmdBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.portBox);
            this.Controls.Add(this.btnListen);
            this.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Form1";
            this.Text = "Server";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.cmdBox.ResumeLayout(false);
            this.cmdBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnListen;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.TextBox txtSend;
        private System.Windows.Forms.CheckedListBox onlineStatusBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox portBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox cmdBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox HG_ID_textBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox usrID_textBox;
        private System.Windows.Forms.TextBox HNA_ID_textBox;
        private System.Windows.Forms.Label label5;
    }
}

