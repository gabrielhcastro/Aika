namespace PacketTool;

partial class PacketToolDesign {
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing) {
        if(disposing && (components != null)) {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PacketToolDesign));
        ConnectBtn = new Button();
        label1 = new Label();
        ConnectionStatusLbl = new Label();
        PastePacketRichTxt = new RichTextBox();
        EncryptBtn = new Button();
        label2 = new Label();
        PacketStatusLbl = new Label();
        DecryptBtn = new Button();
        PacketResultRichTxt = new RichTextBox();
        DisconnectBtn = new Button();
        SendPacketBtn = new Button();
        OnGoingPacketsRichTxt = new RichTextBox();
        panel1 = new Panel();
        menuStrip1 = new MenuStrip();
        inicioToolStripMenuItem = new ToolStripMenuItem();
        toolStripTextBox1 = new ToolStripTextBox();
        Assign = new Label();
        label3 = new Label();
        panel1.SuspendLayout();
        menuStrip1.SuspendLayout();
        SuspendLayout();
        // 
        // ConnectBtn
        // 
        ConnectBtn.Location = new Point(4, 30);
        ConnectBtn.Name = "ConnectBtn";
        ConnectBtn.Size = new Size(219, 23);
        ConnectBtn.TabIndex = 0;
        ConnectBtn.Text = "Connect";
        ConnectBtn.UseVisualStyleBackColor = true;
        ConnectBtn.Click += ConnectBtn_Click;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(4, 12);
        label1.Name = "label1";
        label1.Size = new Size(42, 15);
        label1.TabIndex = 1;
        label1.Text = "Status:";
        // 
        // ConnectionStatusLbl
        // 
        ConnectionStatusLbl.AutoSize = true;
        ConnectionStatusLbl.ForeColor = Color.Red;
        ConnectionStatusLbl.Location = new Point(52, 12);
        ConnectionStatusLbl.Name = "ConnectionStatusLbl";
        ConnectionStatusLbl.Size = new Size(106, 15);
        ConnectionStatusLbl.TabIndex = 2;
        ConnectionStatusLbl.Text = "NÃO CONECTADO";
        // 
        // PastePacketRichTxt
        // 
        PastePacketRichTxt.Location = new Point(483, 34);
        PastePacketRichTxt.Name = "PastePacketRichTxt";
        PastePacketRichTxt.Size = new Size(576, 162);
        PastePacketRichTxt.TabIndex = 3;
        PastePacketRichTxt.Text = "";
        PastePacketRichTxt.TextChanged += PastePacketRichTxt_TextChanged;
        // 
        // EncryptBtn
        // 
        EncryptBtn.Enabled = false;
        EncryptBtn.Location = new Point(903, 202);
        EncryptBtn.Name = "EncryptBtn";
        EncryptBtn.Size = new Size(75, 23);
        EncryptBtn.TabIndex = 4;
        EncryptBtn.Text = "Encrypt";
        EncryptBtn.UseVisualStyleBackColor = true;
        EncryptBtn.Click += EncryptBtn_Click;
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new Point(889, 16);
        label2.Name = "label2";
        label2.Size = new Size(80, 15);
        label2.TabIndex = 5;
        label2.Text = "Packet Status:";
        // 
        // PacketStatusLbl
        // 
        PacketStatusLbl.AutoSize = true;
        PacketStatusLbl.ForeColor = Color.Black;
        PacketStatusLbl.Location = new Point(975, 16);
        PacketStatusLbl.Name = "PacketStatusLbl";
        PacketStatusLbl.Size = new Size(82, 15);
        PacketStatusLbl.TabIndex = 6;
        PacketStatusLbl.Text = "Aguardando...";
        // 
        // DecryptBtn
        // 
        DecryptBtn.Enabled = false;
        DecryptBtn.Location = new Point(984, 202);
        DecryptBtn.Name = "DecryptBtn";
        DecryptBtn.Size = new Size(75, 23);
        DecryptBtn.TabIndex = 7;
        DecryptBtn.Text = "Decrypt";
        DecryptBtn.UseVisualStyleBackColor = true;
        DecryptBtn.Click += DecryptBtn_Click;
        // 
        // PacketResultRichTxt
        // 
        PacketResultRichTxt.Location = new Point(483, 231);
        PacketResultRichTxt.Name = "PacketResultRichTxt";
        PacketResultRichTxt.ReadOnly = true;
        PacketResultRichTxt.Size = new Size(576, 163);
        PacketResultRichTxt.TabIndex = 8;
        PacketResultRichTxt.Text = "";
        // 
        // DisconnectBtn
        // 
        DisconnectBtn.Location = new Point(229, 30);
        DisconnectBtn.Name = "DisconnectBtn";
        DisconnectBtn.Size = new Size(219, 23);
        DisconnectBtn.TabIndex = 9;
        DisconnectBtn.Text = "Disconnect";
        DisconnectBtn.UseVisualStyleBackColor = true;
        DisconnectBtn.Click += DisconnectBtn_Click;
        // 
        // SendPacketBtn
        // 
        SendPacketBtn.Enabled = false;
        SendPacketBtn.Location = new Point(903, 400);
        SendPacketBtn.Name = "SendPacketBtn";
        SendPacketBtn.Size = new Size(156, 23);
        SendPacketBtn.TabIndex = 10;
        SendPacketBtn.Text = "Send Packet";
        SendPacketBtn.UseVisualStyleBackColor = true;
        SendPacketBtn.Click += SendPacketBtn_Click;
        // 
        // OnGoingPacketsRichTxt
        // 
        OnGoingPacketsRichTxt.BackColor = SystemColors.ControlText;
        OnGoingPacketsRichTxt.Font = new Font("Cambria", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        OnGoingPacketsRichTxt.ForeColor = Color.White;
        OnGoingPacketsRichTxt.Location = new Point(12, 114);
        OnGoingPacketsRichTxt.Name = "OnGoingPacketsRichTxt";
        OnGoingPacketsRichTxt.ReadOnly = true;
        OnGoingPacketsRichTxt.Size = new Size(465, 280);
        OnGoingPacketsRichTxt.TabIndex = 12;
        OnGoingPacketsRichTxt.Text = "";
        // 
        // panel1
        // 
        panel1.Controls.Add(label1);
        panel1.Controls.Add(ConnectionStatusLbl);
        panel1.Controls.Add(ConnectBtn);
        panel1.Controls.Add(DisconnectBtn);
        panel1.Location = new Point(12, 34);
        panel1.Name = "panel1";
        panel1.Size = new Size(465, 74);
        panel1.TabIndex = 13;
        // 
        // menuStrip1
        // 
        menuStrip1.Items.AddRange(new ToolStripItem[] { inicioToolStripMenuItem });
        menuStrip1.Location = new Point(0, 0);
        menuStrip1.Name = "menuStrip1";
        menuStrip1.Size = new Size(1069, 24);
        menuStrip1.TabIndex = 14;
        menuStrip1.Text = "menuStrip1";
        // 
        // inicioToolStripMenuItem
        // 
        inicioToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { toolStripTextBox1 });
        inicioToolStripMenuItem.Name = "inicioToolStripMenuItem";
        inicioToolStripMenuItem.Size = new Size(48, 20);
        inicioToolStripMenuItem.Text = "Inicio";
        // 
        // toolStripTextBox1
        // 
        toolStripTextBox1.Name = "toolStripTextBox1";
        toolStripTextBox1.Size = new Size(100, 23);
        toolStripTextBox1.Text = "MadeBy Root4";
        // 
        // Assign
        // 
        Assign.AutoSize = true;
        Assign.Location = new Point(12, 408);
        Assign.Name = "Assign";
        Assign.Size = new Size(53, 15);
        Assign.TabIndex = 15;
        Assign.Text = "MadeBy:";
        // 
        // label3
        // 
        label3.AutoSize = true;
        label3.Font = new Font("Writeline", 11.999999F, FontStyle.Bold, GraphicsUnit.Point, 0);
        label3.Location = new Point(64, 403);
        label3.Name = "label3";
        label3.Size = new Size(55, 23);
        label3.TabIndex = 16;
        label3.Text = "Root4";
        // 
        // PacketToolDesign
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1069, 432);
        Controls.Add(label3);
        Controls.Add(Assign);
        Controls.Add(panel1);
        Controls.Add(OnGoingPacketsRichTxt);
        Controls.Add(SendPacketBtn);
        Controls.Add(PacketResultRichTxt);
        Controls.Add(DecryptBtn);
        Controls.Add(PacketStatusLbl);
        Controls.Add(label2);
        Controls.Add(EncryptBtn);
        Controls.Add(PastePacketRichTxt);
        Controls.Add(menuStrip1);
        FormBorderStyle = FormBorderStyle.Fixed3D;
        Icon = (Icon)resources.GetObject("$this.Icon");
        MainMenuStrip = menuStrip1;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "PacketToolDesign";
        Text = "PacketTool";
        FormClosing += PacketToolDesign_FormClosing;
        panel1.ResumeLayout(false);
        panel1.PerformLayout();
        menuStrip1.ResumeLayout(false);
        menuStrip1.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Button ConnectBtn;
    private Label label1;
    private Label ConnectionStatusLbl;
    private RichTextBox PastePacketRichTxt;
    private Button EncryptBtn;
    private Label label2;
    private Label PacketStatusLbl;
    private Button DecryptBtn;
    private RichTextBox PacketResultRichTxt;
    private Button DisconnectBtn;
    private Button SendPacketBtn;
    private RichTextBox OnGoingPacketsRichTxt;
    private Panel panel1;
    private MenuStrip menuStrip1;
    private ToolStripMenuItem inicioToolStripMenuItem;
    private ToolStripTextBox toolStripTextBox1;
    private Label Assign;
    private Label label3;
}
