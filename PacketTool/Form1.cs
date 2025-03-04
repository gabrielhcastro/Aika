using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PacketTool;

public partial class PacketToolDesign : Form {
    private TcpClient _client;
    private NetworkStream _stream;
    private Socket _snifferSocket;
    private Thread _snifferThread;

    public PacketToolDesign() {
        InitializeComponent();
    }

    private void ConnectBtn_Click(object sender, EventArgs e) {
        try {
            _client = new TcpClient("127.0.0.1", 8822);
            _stream = _client.GetStream();

            MessageBox.Show("Conectado ao servidor!");
            ConnectionStatusLbl.Text = "CONECTADO";
            ConnectionStatusLbl.ForeColor = Color.Green;

            ConnectBtn.Enabled = false;
            DisconnectBtn.Enabled = true;

            StartPacketSniffer();
            _ = Task.Run(() => ListenForPackets());
        }
        catch(Exception ex) {
            MessageBox.Show($"Erro ao conectar: {ex.Message}");
            ConnectionStatusLbl.Text = "NÃO CONECTADO";
            ConnectionStatusLbl.ForeColor = Color.Red;

            ConnectBtn.Enabled = true;
            DisconnectBtn.Enabled = false;
        }
    }

    private void DisconnectBtn_Click(object sender, EventArgs e) {
        Disconnect();
    }

    private void StartPacketSniffer() {
        try {
            _snifferSocket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
            _snifferSocket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8822));
            _snifferSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);

            byte[] optionInValue = new byte[4] { 1, 0, 0, 0 };
            byte[] optionOutValue = new byte[4] { 1, 0, 0, 0 };

            _snifferSocket.IOControl(IOControlCode.ReceiveAll, optionInValue, optionOutValue);

            _snifferThread = new Thread(SniffPackets);
            _snifferThread.Start();
        }
        catch(Exception ex) {
            MessageBox.Show($"Erro ao iniciar o sniffer: {ex.Message}");
        }
    }

    private void SniffPackets() {
        try {
            byte[] buffer = new byte[4096];

            while(true) {
                int bytesRead = _snifferSocket.Receive(buffer);

                if(bytesRead > 0) {
                    string hexDump = BitConverter.ToString(buffer, 0, bytesRead).Replace("-", " ");
                    LogPacket("Recv", Encoding.UTF8.GetBytes(hexDump));
                }
            }
        }
        catch(Exception ex) {
            LogPacket("Erro", Encoding.UTF8.GetBytes("Falha ao capturar pacotes: " + ex.Message));
        }
    }

    private void EncryptBtn_Click(object sender, EventArgs e) {
        SendPacketBtn.Enabled = true;
        PacketResultRichTxt.Clear();

        try {
            byte[] packet = StringToByteArray(PastePacketRichTxt.Text);
            EncDec.Encrypt(ref packet, packet.Length);
            PacketResultRichTxt.Text = BitConverter.ToString(packet).Replace("-", " ");
            PacketStatusLbl.Text = "Criptografado";
            PacketStatusLbl.ForeColor = Color.Blue;
        }
        catch {
            MessageBox.Show("Erro ao criptografar o pacote.");
        }
    }

    private void DecryptBtn_Click(object sender, EventArgs e) {
        PacketResultRichTxt.Clear();

        try {
            byte[] packet = StringToByteArray(PastePacketRichTxt.Text);
            if(EncDec.Decrypt(ref packet, packet.Length)) {
                PacketResultRichTxt.Text = BitConverter.ToString(packet).Replace("-", " ");
                PacketStatusLbl.Text = "Descriptografado!";
                PacketStatusLbl.ForeColor = Color.Green;
            }
            else {
                MessageBox.Show("Falha na descriptografia.");
            }
        }
        catch {
            MessageBox.Show("Erro ao descriptografar o pacote.");
        }
    }

    private void PastePacketRichTxt_TextChanged(object sender, EventArgs e) {
        EncryptBtn.Enabled = true;
        DecryptBtn.Enabled = true;

        if(PastePacketRichTxt.Text.Length == 0) {
            EncryptBtn.Enabled = false;
            DecryptBtn.Enabled = false;
        }

        // PacketStatusLbl.Text = Encriptado/Desencriptado
    }

    private void SendPacketBtn_Click(object sender, EventArgs e) {
        try {
            if(_client == null || !_client.Connected) {
                MessageBox.Show("Não está conectado ao servidor!");
                return;
            }

            if(PastePacketRichTxt.Text.Length < 12) {
                MessageBox.Show("Packet Inválido!");
                return;
            }

            byte[] packet = StringToByteArray(PastePacketRichTxt.Text.Trim());

            ushort packetSize = (ushort)packet.Length;
            packet[0] = (byte)(packetSize & 0xFF);
            packet[1] = (byte)(packetSize >> 8);

            EncDec.Encrypt(ref packet, packet.Length);

            _stream.Write(packet, 0, packet.Length);
            LogPacket("Sent", packet);
        }
        catch(Exception ex) {
            MessageBox.Show($"Erro ao enviar pacote: {ex.Message}");
        }
    }

    private static byte[] StringToByteArray(string hex) {
        hex = hex.Replace(" ", "").Replace("-", "");

        if(hex.Length % 2 != 0) {
            throw new ArgumentException("O número de caracteres deve ser par.");
        }

        int length = hex.Length / 2;
        byte[] data = new byte[length];

        for(int i = 0; i < length; i++) {
            if(!byte.TryParse(hex.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber, null, out data[i])) {
                throw new ArgumentException($"Entrada inválida: {hex.Substring(i * 2, 2)} não é um byte válido.");
            }
        }
        return data;
    }


    private async Task ListenForPackets() {
        try {
            byte[] buffer = new byte[4096];
            int bufferOffset = 0;

            while(_client.Connected) {
                int bytesRead = await _stream.ReadAsync(buffer, bufferOffset, buffer.Length - bufferOffset);
                if(bytesRead > 0) {
                    bufferOffset += bytesRead;

                    while(bufferOffset >= 12) {
                        ushort packetSize = (ushort)(buffer[0] | (buffer[1] << 8));

                        if(bufferOffset >= packetSize) {
                            byte[] packet = buffer.Take(packetSize).ToArray();

                            LogPacket("Recv", packet);

                            Array.Copy(buffer, packetSize, buffer, 0, bufferOffset - packetSize);
                            bufferOffset -= packetSize;
                        }
                        else {
                            break;
                        }
                    }
                }
            }
        }
        catch(Exception) {
            LogPacket("Erro", Encoding.UTF8.GetBytes("Conexão perdida."));
        }
    }


    private void LogPacket(string direction, byte[] data) {
        if(InvokeRequired) {
            Invoke(new Action(() => LogPacket(direction, data)));
            return;
        }

        string hexDump = BitConverter.ToString(data).Replace("-", " ");
        string logEntry = $"{direction}: {hexDump}\n";

        Color directionColor = direction == "Recv" ? Color.Green : Color.Blue;
        OnGoingPacketsRichTxt.SelectionColor = directionColor;

        OnGoingPacketsRichTxt.AppendText(logEntry);

        if(OnGoingPacketsRichTxt.Lines.Length > 1000) {
            OnGoingPacketsRichTxt.Lines = OnGoingPacketsRichTxt.Lines.Skip(OnGoingPacketsRichTxt.Lines.Length - 1000).ToArray();
        }

        OnGoingPacketsRichTxt.SelectionStart = OnGoingPacketsRichTxt.Text.Length;
        OnGoingPacketsRichTxt.ScrollToCaret();
    }

    private void Disconnect() {
        try {
            _stream?.Close();
            _client?.Close();

            _snifferSocket?.Close();

            MessageBox.Show("Desconectado do servidor!");
            ConnectionStatusLbl.Text = "NÃO CONECTADO";
            ConnectionStatusLbl.ForeColor = Color.Red;

            ConnectBtn.Enabled = true;
            DisconnectBtn.Enabled = false;
        }
        catch(Exception ex) {
            MessageBox.Show($"Erro ao desconectar: {ex.Message}");
        }
    }

    private void PacketToolDesign_FormClosing(object sender, FormClosingEventArgs e) {
        if(_client?.Connected == true) {
            Disconnect();
        }

        if(_snifferThread != null && _snifferThread.IsAlive) {
            _snifferThread.Interrupt();
            _snifferThread.Join();
            _snifferSocket?.Close();
        }
    }
}
