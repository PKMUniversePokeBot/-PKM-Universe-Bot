// PKM Universe Bot - Switch Connection
// Written by PKM Universe - 2025

using System.Net.Sockets;
using PKMUniverse.Core.Logging;

namespace PKMUniverse.Switch.Connection;

public class SwitchConnection : IDisposable
{
    private readonly string _ip;
    private readonly int _port;
    private TcpClient? _client;
    private NetworkStream? _stream;

    public bool IsConnected => _client?.Connected ?? false;

    public SwitchConnection(string ip, int port)
    {
        _ip = ip;
        _port = port;
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_ip, _port);
            _stream = _client.GetStream();
            Logger.Info("Connection", $"Connected to {_ip}:{_port}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Connection", $"Failed to connect: {ex.Message}");
            return false;
        }
    }

    public void Disconnect()
    {
        _stream?.Dispose();
        _client?.Dispose();
        _stream = null;
        _client = null;
        Logger.Info("Connection", "Disconnected");
    }

    public async Task<byte[]> ReadBytesMainAsync(ulong offset, int length, CancellationToken token)
    {
        var cmd = $"peekMain 0x{offset:X} {length}\r\n";
        return await SendCommandAsync(cmd, length, token);
    }

    public async Task WriteBytesMainAsync(ulong offset, byte[] data, CancellationToken token)
    {
        var hex = BitConverter.ToString(data).Replace("-", "");
        var cmd = $"pokeMain 0x{offset:X} 0x{hex}\r\n";
        await SendCommandAsync(cmd, token);
    }

    public async Task<byte[]> ReadBytesAbsoluteAsync(ulong offset, int length, CancellationToken token)
    {
        var cmd = $"peekAbsolute 0x{offset:X} {length}\r\n";
        return await SendCommandAsync(cmd, length, token);
    }

    public async Task WriteBytesAbsoluteAsync(ulong offset, byte[] data, CancellationToken token)
    {
        var hex = BitConverter.ToString(data).Replace("-", "");
        var cmd = $"pokeAbsolute 0x{offset:X} 0x{hex}\r\n";
        await SendCommandAsync(cmd, token);
    }

    public async Task ClickAsync(SwitchButton button, CancellationToken token)
    {
        var cmd = $"click {button}\r\n";
        await SendCommandAsync(cmd, token);
    }

    public async Task HoldAsync(SwitchButton button, int holdMs, CancellationToken token)
    {
        var press = $"press {button}\r\n";
        await SendCommandAsync(press, token);
        await Task.Delay(holdMs, token);
        var release = $"release {button}\r\n";
        await SendCommandAsync(release, token);
    }

    public async Task SetStickAsync(SwitchStick stick, short x, short y, CancellationToken token)
    {
        var cmd = $"setStick {stick} {x} {y}\r\n";
        await SendCommandAsync(cmd, token);
    }

    private async Task SendCommandAsync(string command, CancellationToken token)
    {
        if (_stream == null) return;
        var bytes = System.Text.Encoding.ASCII.GetBytes(command);
        await _stream.WriteAsync(bytes, token);
        await _stream.FlushAsync(token);
    }

    private async Task<byte[]> SendCommandAsync(string command, int expectedLength, CancellationToken token)
    {
        if (_stream == null) return Array.Empty<byte>();

        var bytes = System.Text.Encoding.ASCII.GetBytes(command);
        await _stream.WriteAsync(bytes, token);
        await _stream.FlushAsync(token);

        var buffer = new byte[expectedLength * 2 + 1];
        var read = await _stream.ReadAsync(buffer, token);

        if (read == 0) return Array.Empty<byte>();

        var hex = System.Text.Encoding.ASCII.GetString(buffer, 0, read).Trim();
        return ConvertHexToBytes(hex);
    }

    private static byte[] ConvertHexToBytes(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return Array.Empty<byte>();
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return bytes;
    }

    public void Dispose()
    {
        Disconnect();
    }
}

public enum SwitchButton
{
    A, B, X, Y,
    DDOWN, DUP, DLEFT, DRIGHT,
    L, R, ZL, ZR,
    PLUS, MINUS,
    LSTICK, RSTICK,
    HOME, CAPTURE
}

public enum SwitchStick
{
    LEFT, RIGHT
}
