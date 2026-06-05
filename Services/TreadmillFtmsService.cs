using System.Net;
using System.Net.Sockets;
using System.Buffers.Binary;

namespace TreadmillController.Services;

/// <summary>
/// FTMS (TNP) service over TCP for WiFi treadmills.
/// Exposes events RawTelemetryReceived and ErrorOccurred and methods
/// to discover, connect, disconnect and send control commands.
/// </summary>
public sealed class TreadmillFtmsService : IDisposable
{
    // FTMS / TNP constants
    private const int FtmsPort = 36866;

    private const byte TnpVersion = 0x01;
    private const byte ID_WRITE_CHARACTERISTIC = 0x04;
    private const byte ID_ENABLE_NOTIFICATIONS = 0x05;
    private const byte ID_NOTIFICATION = 0x06;

    private static readonly byte[] TreadmillDataUuid =
    {
        0x00, 0x00, 0x2A, 0xCD,
        0x00, 0x00,
        0x10, 0x00,
        0x80, 0x00,
        0x00, 0x80,
        0x5F, 0x9B, 0x34, 0xFB
    };

    private static readonly byte[] ControlPointUuid =
    {
        0x00, 0x00, 0x2A, 0xD9,
        0x00, 0x00,
        0x10, 0x00,
        0x80, 0x00,
        0x00, 0x80,
        0x5F, 0x9B, 0x34, 0xFB
    };

    // FTMS TCP fields
    private TcpClient? _tcpClient;
    private NetworkStream? _tcpStream;
    private CancellationTokenSource? _tcpCts;
    private byte _sequence = 1;
    private string? _connectedIp;

    public event Action<string, IPEndPoint>? RawTelemetryReceived;

    public event Action<string>? ErrorOccurred;

    public bool IsConnected =>
        _tcpClient != null && _tcpClient.Connected;

    /// <summary>
    /// Attempt to discover a treadmill on the local subnet by scanning
    /// TCP port 36866. Returns the first responsive IP or null.
    /// </summary>
    public async Task<string?> DiscoverTreadmillAsync(TimeSpan? timeout = null)
    {
        string subnet = GetLocalSubnet();

        if (string.IsNullOrWhiteSpace(subnet))
            return null;

        using CancellationTokenSource cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(5));

        var tasks = new List<Task<string?>>();

        for (int i = 1; i <= 254; i++)
        {
            string ip = $"{subnet}.{i}";

            tasks.Add(CheckHostAsync(ip, cts.Token));
        }

        while (tasks.Count > 0)
        {
            Task<string?> finished = await Task.WhenAny(tasks);

            tasks.Remove(finished);

            string? result = await finished;

            if (result != null)
            {
                cts.Cancel();
                return result;
            }
        }

        return null;
    }

    private async Task<string?> CheckHostAsync(string ip, CancellationToken token)
    {
        try
        {
            using TcpClient client = new();

            var connectTask = client.ConnectAsync(ip, FtmsPort);

            var timeoutTask = Task.Delay(500, token);

            var completed = await Task.WhenAny(connectTask, timeoutTask);

            if (completed != connectTask)
                return null;

            if (!client.Connected)
                return null;

            Console.WriteLine($"FOUND PORT {FtmsPort}: {ip}");

            return ip;
        }
        catch
        {
            return null;
        }
    }

    private string GetLocalSubnet()
    {
        foreach (var ip in System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName()))
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                string[] parts = ip.ToString().Split('.');

                if (parts.Length == 4)
                {
                    return $"{parts[0]}.{parts[1]}.{parts[2]}";
                }
            }
        }

        return string.Empty;
    }

    public async Task ConnectFtmsAsync(string ip)
    {
        if (IsConnected && _connectedIp == ip)
            return;

        await DisconnectFtmsAsync();

        _tcpClient = new TcpClient { NoDelay = true };
        _tcpCts = new CancellationTokenSource();

        try
        {
            await _tcpClient.ConnectAsync(ip, FtmsPort);

            _tcpStream = _tcpClient.GetStream();
            _connectedIp = ip;

            Console.WriteLine($"FTMS CONNECTED: {ip}");

            // Start receive loop
            _ = Task.Run(() => FtmsReceiveLoop(_tcpCts.Token));

            // Enable notifications
            await EnableNotifications(_tcpCts.Token);
            await Task.Delay(300);

            // Request control (required for speed/incline writes)
            await RequestControl(_tcpCts.Token);
            await Task.Delay(300);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke($"FTMS connect failed: {ex.Message}");
            await DisconnectFtmsAsync();
            throw;
        }
    }

    public async Task DisconnectFtmsAsync()
    {
        try
        {
            _tcpCts?.Cancel();
        }
        catch { }

        try
        {
            _tcpStream?.Dispose();
        }
        catch { }

        try
        {
            _tcpClient?.Close();
        }
        catch { }

        _tcpStream = null;
        _tcpClient = null;
        _tcpCts = null;
        _connectedIp = null;
    }

    /// <summary>
    /// Send a control command (speed/incline) using FTMS control point.
    /// Message format expected: "<speed>;<incline>" where -1 means ignore.
    /// </summary>
    public async Task SendCommandAsync(string message, string ip)
    {
        try
        {
            await ConnectFtmsAsync(ip);

            if (_tcpStream == null)
                return;

            string[] parts = message.Split(';');

            double.TryParse(parts.ElementAtOrDefault(0) ?? "-1",
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double speedPart);

            double.TryParse(parts.ElementAtOrDefault(1) ?? "-1",
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double inclinePart);

            if (speedPart >= 0)
            {
                ushort rawSpeed = (ushort)(speedPart * 100);

                byte[] ftms = new byte[]
                {
                    0x02,
                    (byte)(rawSpeed & 0xFF),
                    (byte)((rawSpeed >> 8) & 0xFF)
                };

                await WriteControlPoint(ftms, _tcpCts!.Token);

                Console.WriteLine($"TX SPEED {speedPart:F2} ({rawSpeed})");
            }

            await Task.Delay(100);

            if (inclinePart >= 0)
            {
                short rawIncline = (short)(inclinePart * 10);

                byte[] ftms = new byte[]
                {
                    0x03,
                    (byte)(rawIncline & 0xFF),
                    (byte)((rawIncline >> 8) & 0xFF)
                };

                await WriteControlPoint(ftms, _tcpCts!.Token);

                Console.WriteLine($"TX INCLINE {inclinePart:F1}");
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke($"Send error: {ex.Message}");
        }
    }

    private async Task EnableNotifications(CancellationToken token)
    {
        byte[] payload = new byte[17];

        Buffer.BlockCopy(TreadmillDataUuid, 0, payload, 0, 16);
        payload[16] = 0x01;

        await SendTnpPacket(ID_ENABLE_NOTIFICATIONS, payload, token);

        Console.WriteLine("TX ENABLE NOTIFICATIONS");
    }

    private async Task RequestControl(CancellationToken token)
    {
        byte[] ftms = new byte[] { 0x00 };

        await WriteControlPoint(ftms, token);

        Console.WriteLine("TX REQUEST CONTROL");
    }

    private async Task WriteControlPoint(byte[] ftmsPayload, CancellationToken token)
    {
        byte[] payload = new byte[16 + ftmsPayload.Length];

        Buffer.BlockCopy(ControlPointUuid, 0, payload, 0, 16);
        Buffer.BlockCopy(ftmsPayload, 0, payload, 16, ftmsPayload.Length);

        await SendTnpPacket(ID_WRITE_CHARACTERISTIC, payload, token);
    }

    private async Task SendTnpPacket(byte identifier, byte[] payload, CancellationToken token)
    {
        if (_tcpStream == null)
            return;

        byte[] header = new byte[6];

        header[0] = TnpVersion;
        header[1] = identifier;
        header[2] = _sequence++;
        header[3] = 0x00;

        header[4] = (byte)(payload.Length >> 8);
        header[5] = (byte)(payload.Length & 0xFF);

        Console.WriteLine($"TX HEADER: {BitConverter.ToString(header)}");
        Console.WriteLine($"TX PAYLOAD: {BitConverter.ToString(payload)}");

        await _tcpStream.WriteAsync(header, token);
        await _tcpStream.WriteAsync(payload, token);
        await _tcpStream.FlushAsync(token);
    }

    private async Task FtmsReceiveLoop(CancellationToken token)
    {
        if (_tcpStream == null)
            return;

        try
        {
            while (!token.IsCancellationRequested)
            {
                byte[] header = await ReadExactAsync(6, token);

                byte identifier = header[1];

                ushort payloadLength = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(4, 2));

                byte[] payload = payloadLength > 0
                    ? await ReadExactAsync(payloadLength, token)
                    : Array.Empty<byte>();

                Console.WriteLine($"RX ID={identifier:X2} LEN={payloadLength}");
                Console.WriteLine($"RX PAYLOAD: {BitConverter.ToString(payload)}");

                if (identifier == ID_NOTIFICATION)
                {
                    ProcessNotification(payload);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException)
        {
            // connection lost
            await DisconnectFtmsAsync();
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke($"FTMS receive loop error: {ex.Message}");
            await DisconnectFtmsAsync();
        }
    }

    private void ProcessNotification(byte[] payload)
    {
        try
        {
            if (payload.Length < 16)
                return;

            byte[] ftms = new byte[payload.Length - 16];
            Buffer.BlockCopy(payload, 16, ftms, 0, ftms.Length);

            Console.WriteLine($"FTMS DATA: {BitConverter.ToString(ftms)}");

            // Decode treadmill data and raise textual messages compatible with TelemetryParser
            DecodeTreadmillData(ftms);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void DecodeTreadmillData(byte[] data)
    {
        if (data.Length < 4)
            return;

        int offset = 0;

        ushort flags = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset, 2));
        offset += 2;

        ushort speedRaw = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset, 2));
        offset += 2;

        float speed = speedRaw / 100f; // km/h

        float incline = 0;
        float distance = 0;

        bool distancePresent = (flags & (1 << 2)) != 0;

        if (distancePresent && data.Length >= offset + 3)
        {
            int distanceRaw = data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16);
            distance = distanceRaw;
            offset += 3;
        }

        bool inclinePresent = (flags & (1 << 3)) != 0;

        if (inclinePresent && data.Length >= offset + 4)
        {
            short inclineRaw = BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(offset, 2));
            incline = inclineRaw / 10f;
            offset += 4;
        }

        // Build messages compatible with TelemetryParser
        if (!string.IsNullOrEmpty(_connectedIp))
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(_connectedIp), FtmsPort);

            string speedMsg = $"Changed KPH to: {speed.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}";
            RawTelemetryReceived?.Invoke(speedMsg, ep);

            string inclineMsg = $"Changed Grade to: {incline.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}";
            RawTelemetryReceived?.Invoke(inclineMsg, ep);

            if (distance > 0)
            {
                string distMsg = $"Distance to: {distance:F0}";
                RawTelemetryReceived?.Invoke(distMsg, ep);
            }
        }
    }

    private async Task<byte[]> ReadExactAsync(int count, CancellationToken token)
    {
        if (_tcpStream == null)
            throw new InvalidOperationException();

        byte[] buffer = new byte[count];

        int offset = 0;

        while (offset < count)
        {
            int read = await _tcpStream.ReadAsync(buffer.AsMemory(offset, count - offset), token);

            if (read == 0)
                throw new IOException("Socket closed");

            offset += read;
        }

        return buffer;
    }

    public void Dispose()
    {
        try
        {
            _tcpCts?.Cancel();
        }
        catch { }

        try
        {
            _tcpStream?.Dispose();
        }
        catch { }

        try
        {
            _tcpClient?.Close();
        }
        catch { }
    }
}
