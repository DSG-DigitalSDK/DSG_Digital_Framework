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
            tableLayoutPanel1 = new TableLayoutPanel();
            pgSer2 = new PropertyGrid();
            pgSer1 = new PropertyGrid();
            panel1 = new Panel();
            label1 = new Label();
            btnSerStop = new Button();
            textBox1 = new TextBox();
            bynSerStart = new Button();
            textBox2 = new TextBox();
            label3 = new Label();
            textBox3 = new TextBox();
            label2 = new Label();
            tabPage2 = new TabPage();
            tableLayoutPanel2 = new TableLayoutPanel();
            lbPlc = new ListBox();
            pgPLC = new PropertyGrid();
            panel2 = new Panel();
            button2 = new Button();
            btnPlcTest = new Button();
            lbLog = new ListBox();
            oTimerLog = new System.Windows.Forms.Timer(components);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            tcMain.SuspendLayout();
            tpSerial.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            panel1.SuspendLayout();
            tabPage2.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            panel2.SuspendLayout();
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
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 3;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.Controls.Add(pgSer2, 2, 0);
            tableLayoutPanel1.Controls.Add(pgSer1, 1, 0);
            tableLayoutPanel1.Controls.Add(panel1, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(3, 3);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(996, 232);
            tableLayoutPanel1.TabIndex = 10;
            // 
            // pgSer2
            // 
            pgSer2.Dock = DockStyle.Fill;
            pgSer2.Location = new Point(667, 3);
            pgSer2.Name = "pgSer2";
            pgSer2.Size = new Size(326, 226);
            pgSer2.TabIndex = 9;
            // 
            // pgSer1
            // 
            pgSer1.Dock = DockStyle.Fill;
            pgSer1.Location = new Point(335, 3);
            pgSer1.Name = "pgSer1";
            pgSer1.Size = new Size(326, 226);
            pgSer1.TabIndex = 3;
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
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 15);
            label1.Name = "label1";
            label1.Size = new Size(48, 15);
            label1.TabIndex = 4;
            label1.Text = "COM-A";
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
            // textBox1
            // 
            textBox1.Location = new Point(71, 12);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(232, 23);
            textBox1.TabIndex = 0;
            textBox1.Text = "COM1,9600,ODD,7,1,RTS";
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
            // textBox2
            // 
            textBox2.Location = new Point(71, 53);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(232, 23);
            textBox2.TabIndex = 1;
            textBox2.Text = "COM2,9600,ODD,7,1,RTS";
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
            // textBox3
            // 
            textBox3.Location = new Point(71, 95);
            textBox3.Name = "textBox3";
            textBox3.Size = new Size(232, 23);
            textBox3.TabIndex = 2;
            textBox3.Text = "Hello World";
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
            // tabPage2
            // 
            tabPage2.Controls.Add(tableLayoutPanel2);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(1002, 238);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "tabPage2";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 3;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel2.Controls.Add(lbPlc, 2, 0);
            tableLayoutPanel2.Controls.Add(pgPLC, 1, 0);
            tableLayoutPanel2.Controls.Add(panel2, 0, 0);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(3, 3);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 1;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Size = new Size(996, 232);
            tableLayoutPanel2.TabIndex = 0;
            // 
            // lbPlc
            // 
            lbPlc.Dock = DockStyle.Fill;
            lbPlc.FormattingEnabled = true;
            lbPlc.ItemHeight = 15;
            lbPlc.Location = new Point(667, 3);
            lbPlc.Name = "lbPlc";
            lbPlc.Size = new Size(326, 226);
            lbPlc.TabIndex = 0;
            // 
            // pgPLC
            // 
            pgPLC.Dock = DockStyle.Fill;
            pgPLC.Location = new Point(335, 3);
            pgPLC.Name = "pgPLC";
            pgPLC.Size = new Size(326, 226);
            pgPLC.TabIndex = 1;
            // 
            // panel2
            // 
            panel2.Controls.Add(button2);
            panel2.Controls.Add(btnPlcTest);
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new Point(3, 3);
            panel2.Name = "panel2";
            panel2.Size = new Size(326, 226);
            panel2.TabIndex = 2;
            // 
            // button2
            // 
            button2.Location = new Point(26, 118);
            button2.Name = "button2";
            button2.Size = new Size(75, 23);
            button2.TabIndex = 1;
            button2.Text = "button2";
            button2.UseVisualStyleBackColor = true;
            // 
            // btnPlcTest
            // 
            btnPlcTest.Location = new Point(26, 14);
            btnPlcTest.Name = "btnPlcTest";
            btnPlcTest.Size = new Size(75, 23);
            btnPlcTest.TabIndex = 0;
            btnPlcTest.Text = "Test PLC";
            btnPlcTest.UseVisualStyleBackColor = true;
            btnPlcTest.Click += btnPlcTest_Click;
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
            tabPage2.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            panel2.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitContainer1;
        private TabControl tcMain;
        private TabPage tpSerial;
        private Label label3;
        private Label label2;
        private Label label1;
        private PropertyGrid pgSer1;
        private TextBox textBox3;
        private TextBox textBox2;
        private TextBox textBox1;
        private TabPage tabPage2;
        private Button btnSerStop;
        private Button bynSerStart;
        private PropertyGrid pgSer2;
        private ListBox lbLog;
        private System.Windows.Forms.Timer oTimerLog;
        private TableLayoutPanel tableLayoutPanel1;
        private Panel panel1;
        private TableLayoutPanel tableLayoutPanel2;
        private ListBox lbPlc;
        private PropertyGrid pgPLC;
        private Panel panel2;
        private Button button2;
        private Button btnPlcTest;
    }
}
