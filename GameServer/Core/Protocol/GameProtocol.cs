using GameServer.Core.Base;
using GameServer.Handlers.Buffer;
using GameServer.Handlers.Encryption;
using GameServer.Network;
using System.Text;

namespace GameServer.Core.Protocol;

public class GameProtocol : BaseProtocol {
    public override void OnConnect(Session session) {
        Console.WriteLine($"Player connected from {session.Ip}");
    }

    public override void OnDisconnect(Session session) {
        Console.WriteLine($"Player disconnected from {session.Ip}");
    }

    public override void OnReceive(Session session, byte[] buff, int bytes) {
        if(buff == null || buff.Length < 4) {
            Console.WriteLine("Invalid packet received.");
            return;
        }

        //TO-DO: Better way to handle this
        if(buff.Length > 2 && buff[0] == 0x11 && buff[1] == 0xF3) {
            var newBuff = new byte[buff.Length - 4];
            Array.Copy(buff, 4, newBuff, 0, buff.Length - 4);
            buff = newBuff;
        }

        LogPacket("RECEIVED", buff, true);

        var packet = new PacketHandler(buff);

        try {
            if(packet.Count < 12) {
                Console.WriteLine("Packet too small.");
                return;
            }

            var size = packet.ReadUInt16();

            if(packet.Count >= size) {
                packet.Replace(packet.Buffer, 0, size);
                packet.Pos = 0;

                var isDecrypted = false;
                try {
                    var newBuffer = packet.Buffer;
                    isDecrypted = EncDec.Decrypt(ref newBuffer, newBuffer.Length);
                    packet.Replace(newBuffer);
                    packet.Pos = 0;
                }
                catch(Exception e) {
                    Console.WriteLine($"Failed to decrypt packet: {e.Message}");
                    return;
                }

                if(isDecrypted) {
                    LogPacket("RECEIVED", packet.Buffer, false);
                    packet.ReadInt32();
                    var sender = packet.ReadUInt16();
                    var opcode = packet.ReadUInt16();
                    packet.ReadInt32();

                    Console.WriteLine($"Sender: 0x{sender:X}, Opcode: 0x{opcode:X}");

                    switch(opcode) {
                        case 0xF311:
                        Console.WriteLine("Handshake recebido, aguardando login...");
                        break;
                        case 0x81:
                        HandleLogin(session, packet);
                        break;
                        case 0x685:
                        SendCharList(session);
                        break;
                        default:
                        Console.WriteLine($"Opcode desconhecido: 0x{opcode:X}");
                        break;
                    }
                }
                else {
                    Console.WriteLine("Failed to decrypt packet.");
                }
            }
            else {
                Console.WriteLine($"Packet with wrong size. ({packet.Count} >= {size})");
            }
        }
        catch(Exception e) {
            Console.WriteLine($"Error processing packet: {e.Message}");
            session.Close(); // Fecha a sessão em caso de erro
        }

        //if(opcode == 0x0001) {
        //    var response = new PacketHandler();
        //    response.Write((ushort)0x0002);
        //    session.SendPacket(response.GetBytes());
        //}
        //else if(opcode == 0x0002) {
        //    Console.WriteLine($"Pong recebido de {session.Ip}");
        //    session.LastPongTime = DateTime.UtcNow;
        //}
    }

    private void HandleLogin(Session session, PacketHandler packet) {
        string username = Encoding.ASCII.GetString(packet.ReadBytes(32)).TrimEnd('\0');
        string token = Encoding.ASCII.GetString(packet.ReadBytes(32)).TrimEnd('\0');

        Console.WriteLine($"Usuário tentando login: {username}, Token: {token}");

        PacketHandler response = new PacketHandler();

        Console.WriteLine("Login bem-sucedido!");

        //Packet em delphi: ((25, 0, 0, 0, 130, 0), 1, 11981171, None, 0)

        response.Write((ushort)0);  // Size (preenchido depois)
        response.Write((byte)0x00); // Key (pode precisar de ajuste)
        response.Write((byte)0x00); // ChkSum (se necessário, calcule depois)
        response.Write((ushort)0x0000); // Index (ID fictício do jogador)
        response.Write((ushort)0x82);  // Opcode de resposta
        response.Write((uint)11981171); // Timestamp do login

        // Criando o Corpo do Pacote (TResponseLoginPacket)
        response.Write((uint)12345); // ID fictício do jogador
        response.Write((uint)11981171); // Timestamp do login
        //response.Write((uint)Environment.TickCount); // Timestamp do login
        response.Write((ushort)0);  // Nação (exemplo)
        response.Write((uint)0);    // Null_1 (padding)

        // Preenchendo o tamanho correto do pacote no Header
        ushort packetSize = (ushort)response.Count;
        response.Buffer[0] = (byte)(0x19);
        response.Buffer[1] = (byte)(0x00);

        // Enviando o pacote com Header
        byte[] packetData = response.GetBytes();
        Console.WriteLine($"HandleLogin enviado: {BitConverter.ToString(packetData)}");

        session.SendPacket(packetData);
    }

    public void SendCharList(Session session) {
        // Cria o pacote
        var packet = new PacketHandler();

        packet.Write((ushort)0);  // Size (preenchido depois)
        packet.Write((byte)0x00); // Key (pode precisar de ajuste)
        packet.Write((byte)0x00); // ChkSum (se necessário, calcule depois)
        packet.Write((ushort)0x0000); // Index (ID fictício do jogador)
        packet.Write((ushort)0x901);  // Opcode de resposta
        packet.Write((uint)11981171); // Timestamp do login

        // Dados da conta
        packet.Write((uint)1); // AccountID (fictício)
        packet.Write((uint)0); // Campo desconhecido (Unk)
        packet.Write((uint)0); // Campo não utilizado (NotUse)

        // Dados dos personagens (3 personagens)
        packet.Write(new byte[16]); // Nome vazio (16 bytes)
        packet.Write((ushort)0); // Nação
        packet.Write(new byte[16]); // Equipamentos zerados
        packet.Write((byte)0); // Refinamento

        // **Atributos zerados**
        packet.Write(new byte[sizeof(ushort) * 6]); // Str, Agi, Int, Cons, Luck, Status

        packet.Write((byte)0); // Numeric Token
        packet.Write((byte)0); // Numeric Error

        // **Tamanho padrão (07 77 77)**
        packet.Write(new byte[] { 0x07, 0x77, 0x77, 0x00 });

        packet.Write((uint)0); // Ouro
        packet.Write((uint)0); // Exp
        packet.Write((ushort)0); // Classe
        packet.Write((ushort)0); // Nível
        packet.Write((ushort)0); // Equip extra
        packet.Write((ushort)0);
        packet.Write((uint)0); // Sem tempo de exclusão

        // Preenche o tamanho correto do pacote no Header
        ushort packetSize = (ushort)packet.Count;
        packet.Buffer[0] = (byte)(packetSize & 0xFF); // Byte menos significativo
        packet.Buffer[1] = (byte)(packetSize >> 8);   // Byte mais significativo

        byte[] packetData = packet.GetBytes();
        LogPacket("SENT", packetData, false);
        EncDec.Encrypt(ref packetData, packetData.Length);
        LogPacket("SENT", packetData, true);
        session.SendPacket(packetData);
    }

    private void LogPacket(string direction, byte[] rawData, bool isEncrypted) {
        try {
            string logDir = "GameServer/Logs";
            if(!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            string logPath = Path.Combine(logDir, "packets_log.txt");
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string encryptedStatus = isEncrypted ? "ENCRYPTED" : "DECRYPTED";

            StringBuilder log = new StringBuilder();
            log.AppendLine($"[{timestamp}] {direction} - {encryptedStatus}");
            log.AppendLine(BitConverter.ToString(rawData));
            log.AppendLine(new string('-', 80));

            File.AppendAllText(logPath, log.ToString());
        }
        catch(Exception ex) {
            Console.WriteLine($"Erro ao salvar log de pacotes: {ex.Message}");
        }
    }

}
