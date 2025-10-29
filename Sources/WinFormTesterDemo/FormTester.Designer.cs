namespace WinFormTesterDemo
{
    partial class FormTester
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
            splitContainer1 = new SplitContainer();
            tcMain = new TabControl();
            tpSerial = new TabPage();
            propertyGrid2 = new PropertyGrid();
            btnSerStop = new Button();
            bynSerStart = new Button();
            label3 = new Label();
            label2 = new Label();
            label1 = new Label();
            propertyGrid1 = new PropertyGrid();
            textBox3 = new TextBox();
            textBox2 = new TextBox();
            textBox1 = new TextBox();
            tabPage2 = new TabPage();
            lbLog = new ListBox();
            oTimerLog = new System.Windows.Forms.Timer(components);
            tableLayoutPanel1 = new TableLayoutPanel();
            panel1 = new Panel();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            tcMain.SuspendLayout();
            tpSerial.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(tcMain);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(lbLog);
            splitContainer1.Size = new Size(1010, 450);
            splitContainer1.SplitterDistance = 266;
            splitContainer1.TabIndex = 0;
            // 
            // tcMain
            // 
            tcMain.Controls.Add(tpSerial);
            tcMain.Controls.Add(tabPage2);
            tcMain.Dock = DockStyle.Fill;
            tcMain.Location = new Point(0, 0);
            tcMain.Name = "tcMain";
            tcMain.SelectedIndex = 0;
            tcMain.Size = new Size(1010, 266);
            tcMain.TabIndex = 0;
            // 
            // tpSerial
            // 
            tpSerial.Controls.Add(tableLayoutPanel1);
            tpSerial.Location = new Point(4, 24);
            tpSerial.Name = "tpSerial";
            tpSerial.Padding = new Padding(3);
            tpSerial.Size = new Size(1002, 238);
            tpSerial.TabIndex = 0;
            tpSerial.Text = "Serial Test";
            tpSerial.UseVisualStyleBackColor = true;
            // 
            // propertyGrid2
            // 
            propertyGrid2.Dock = DockStyle.Fill;
            propertyGrid2.Location = new Point(667, 3);
            propertyGrid2.Name = "propertyGrid2";
            propertyGrid2.Size = new Size(326, 226);
            propertyGrid2.TabIndex = 9;
            // 
            // btnSerStop
            // 
            btnSerStop.Location = new Point(93, 191);
            btnSerStop.Name = "btnSerStop";
            btnSerStop.Size = new Size(75, 23);
            btnSerStop.TabIndex = 8;
            btnSerStop.Text = "Stop";
            btnSerStop.UseVisualStyleBackColor = true;
            btnSerStop.Click += btnSerStop_Click;
            // 
            // bynSerStart
            // 
            bynSerStart.Location = new Point(12, 191);
            bynSerStart.Name = "bynSerStart";
            bynSerStart.Size = new Size(75, 23);
            bynSerStart.TabIndex = 7;
            bynSerStart.Text = "Start";
            bynSerStart.UseVisualStyleBackColor = true;
            bynSerStart.Click += btnSerStart_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 98);
            label3.Name = "label3";
            label3.Size = new Size(53, 15);
            label3.TabIndex = 6;
            label3.Text = "Message";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 56);
            label2.Name = "label2";
            label2.Size = new Size(47, 15);
            label2.TabIndex = 5;
            label2.Text = "COM-B";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 15);
            label1.Name = "label1";
            label1.Size = new Size(48, 15);
            label1.TabIndex = 4;
            label1.Text = "COM-A";
            // 
            // propertyGrid1
            // 
            propertyGrid1.Dock = DockStyle.Fill;
            propertyGrid1.Location = new Point(335, 3);
            propertyGrid1.Name = "propertyGrid1";
            propertyGrid1.Size = new Size(326, 226);
            propertyGrid1.TabIndex = 3;
            // 
            // textBox3
            // 
            textBox3.Location = new Point(71, 95);
            textBox3.Name = "textBox3";
            textBox3.Size = new Size(232, 23);
            textBox3.TabIndex = 2;
            textBox3.Text = "Hello World";
            // 
            // textBox2
            // 
            textBox2.Location = new Point(71, 53);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(232, 23);
            textBox2.TabIndex = 1;
            textBox2.Text = "COM2,9600,ODD,7,1,RTS";
            // 
            // textBox1
            // 
            textBox1.Location = new Point(71, 12);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(232, 23);
            textBox1.TabIndex = 0;
            textBox1.Text = "COM1,9600,ODD,7,1,RTS";
            // 
            // tabPage2
            // 
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(792, 238);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "tabPage2";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // lbLog
            // 
            lbLog.Dock = DockStyle.Fill;
            lbLog.FormattingEnabled = true;
            lbLog.ItemHeight = 15;
            lbLog.Location = new Point(0, 0);
            lbLog.Name = "lbLog";
            lbLog.Size = new Size(1010, 180);
            lbLog.TabIndex = 0;
            // 
            // oTimerLog
            // 
            oTimerLog.Enabled = true;
            oTimerLog.Interval = 500;
            oTimerLog.Tick += oTimerLog_Tick;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 3;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.Controls.Add(propertyGrid2, 2, 0);
            tableLayoutPanel1.Controls.Add(propertyGrid1, 1, 0);
            tableLayoutPanel1.Controls.Add(panel1, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(3, 3);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(996, 232);
            tableLayoutPanel1.TabIndex = 10;
            // 
            // panel1
            // 
            panel1.Controls.Add(label1);
            panel1.Controls.Add(btnSerStop);
            panel1.Controls.Add(textBox1);
            panel1.Controls.Add(bynSerStart);
            panel1.Controls.Add(textBox2);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(textBox3);
            panel1.Controls.Add(label2);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(3, 3);
            panel1.Name = "panel1";
            panel1.Size = new Size(326, 226);
            panel1.TabIndex = 10;
            // 
            // FormTester
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1010, 450);
            Controls.Add(splitContainer1);
            Name = "FormTester";
            Text = "Form1";
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            tcMain.ResumeLayout(false);
            tpSerial.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitContainer1;
        private TabControl tcMain;
        private TabPage tpSerial;
        private Label label3;
        private Label label2;
        private Label label1;
        private PropertyGrid propertyGrid1;
        private TextBox textBox3;
        private TextBox textBox2;
        private TextBox textBox1;
        private TabPage tabPage2;
        private Button btnSerStop;
        private Button bynSerStart;
        private PropertyGrid propertyGrid2;
        private ListBox lbLog;
        private System.Windows.Forms.Timer oTimerLog;
        private TableLayoutPanel tableLayoutPanel1;
        private Panel panel1;
    }
}
