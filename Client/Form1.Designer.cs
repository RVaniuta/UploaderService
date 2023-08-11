namespace Client
{
    partial class Form1
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
            label1 = new Label();
            button_up = new Button();
            button_down = new Button();
            startAt = new Label();
            endAt = new Label();
            totalR = new Label();
            totalU = new Label();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(59, 55);
            label1.Name = "label1";
            label1.Size = new Size(27, 32);
            label1.TabIndex = 0;
            label1.Text = "0";
            label1.Click += label1_Click;
            // 
            // button_up
            // 
            button_up.Location = new Point(170, 12);
            button_up.Name = "button_up";
            button_up.Size = new Size(150, 46);
            button_up.TabIndex = 1;
            button_up.Text = "UP";
            button_up.UseVisualStyleBackColor = true;
            button_up.Click += button1_Click;
            // 
            // button_down
            // 
            button_down.Location = new Point(170, 82);
            button_down.Name = "button_down";
            button_down.Size = new Size(150, 46);
            button_down.TabIndex = 2;
            button_down.Text = "DOWN";
            button_down.UseVisualStyleBackColor = true;
            button_down.Click += button_down_Click;
            // 
            // startAt
            // 
            startAt.AutoSize = true;
            startAt.Location = new Point(662, 26);
            startAt.Name = "startAt";
            startAt.Size = new Size(78, 32);
            startAt.TabIndex = 4;
            startAt.Text = "label2";
            // 
            // endAt
            // 
            endAt.AutoSize = true;
            endAt.Location = new Point(666, 71);
            endAt.Name = "endAt";
            endAt.Size = new Size(78, 32);
            endAt.TabIndex = 5;
            endAt.Text = "label2";
            // 
            // totalR
            // 
            totalR.AutoSize = true;
            totalR.Location = new Point(1054, 28);
            totalR.Name = "totalR";
            totalR.Size = new Size(78, 32);
            totalR.TabIndex = 6;
            totalR.Text = "label2";
            // 
            // totalU
            // 
            totalU.AutoSize = true;
            totalU.Location = new Point(1061, 70);
            totalU.Name = "totalU";
            totalU.Size = new Size(78, 32);
            totalU.TabIndex = 7;
            totalU.Text = "label2";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1579, 911);
            Controls.Add(totalU);
            Controls.Add(totalR);
            Controls.Add(endAt);
            Controls.Add(startAt);
            Controls.Add(button_down);
            Controls.Add(button_up);
            Controls.Add(label1);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Button button_up;
        private Button button_down;
        private Label startAt;
        private Label endAt;
        private Label totalR;
        private Label totalU;
    }
}