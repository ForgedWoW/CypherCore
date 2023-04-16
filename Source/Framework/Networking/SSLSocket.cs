// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Serilog;

namespace Framework.Networking;

public abstract class SSLSocket : ISocket, IDisposable
{
    internal SslStream Stream;
    private readonly IPEndPoint _remoteEndPoint;
    private readonly Socket _socket;
    private byte[] _receiveBuffer;

    protected SSLSocket(Socket socket)
    {
        _socket = socket;
        _remoteEndPoint = (IPEndPoint)_socket.RemoteEndPoint;
        _receiveBuffer = new byte[ushort.MaxValue];

        Stream = new SslStream(new NetworkStream(socket), false);
    }

    public abstract void Accept();

    public async Task AsyncHandshake(X509Certificate2 certificate)
    {
        try
        {
            await Stream.AuthenticateAsServerAsync(certificate, false, SslProtocols.Tls12, false);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "");
            CloseSocket();

            return;
        }

        await AsyncRead();
    }

    public async Task AsyncRead()
    {
        if (!IsOpen)
            return;

        try
        {
            var result = await Stream.ReadAsync(_receiveBuffer, 0, _receiveBuffer.Length);

            if (result == 0)
            {
                CloseSocket();

                return;
            }

            ReadHandler(_receiveBuffer, result);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "");
        }
    }

    public async Task AsyncWrite(byte[] data)
    {
        if (!IsOpen)
            return;

        try
        {
            await Stream.WriteAsync(data, 0, data.Length);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "");
        }
    }

    public void CloseSocket()
    {
        try
        {
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }
        catch (Exception ex)
        {
            Log.Logger.Debug($"WorldSocket.CloseSocket: {GetRemoteIpEndPoint()} errored when shutting down socket: {ex.Message}");
        }
    }

    public virtual void Dispose()
    {
        _receiveBuffer = null;
        Stream.Dispose();
    }

    public IPEndPoint GetRemoteIpEndPoint()
    {
        return _remoteEndPoint;
    }

    public bool IsOpen => _socket.Connected;

    public virtual void OnClose()
    {
        Dispose();
    }

    public abstract void ReadHandler(byte[] data, int receivedLength);

    public void SetNoDelay(bool enable)
    {
        _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, enable);
    }

    public virtual bool Update()
    {
        return _socket.Connected;
    }
}