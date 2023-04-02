// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Net;
using System.Net.Sockets;
using Serilog;

namespace Framework.Networking;

public abstract class SocketBase : ISocket, IDisposable
{
    private readonly SocketAsyncEventArgs _receiveSocketAsyncEventArgs;

    private readonly SocketAsyncEventArgs _receiveSocketAsyncEventArgsWithCallback;

    private readonly IPEndPoint _remoteIpEndPoint;

    private readonly Socket _socket;

    protected SocketBase(Socket socket)
    {
        _socket = socket;
        _remoteIpEndPoint = (IPEndPoint)_socket.RemoteEndPoint;

        _receiveSocketAsyncEventArgsWithCallback = new SocketAsyncEventArgs();
        _receiveSocketAsyncEventArgsWithCallback.SetBuffer(new byte[0x4000], 0, 0x4000);

        _receiveSocketAsyncEventArgs = new SocketAsyncEventArgs();
        _receiveSocketAsyncEventArgs.SetBuffer(new byte[0x4000], 0, 0x4000);
        _receiveSocketAsyncEventArgs.Completed += (_, args) => ProcessReadAsync(args);
    }

    public delegate void SocketReadCallback(SocketAsyncEventArgs args);

    public abstract void Accept();

    public void AsyncRead()
    {
        if (!IsOpen())
            return;

        _receiveSocketAsyncEventArgs.SetBuffer(0, 0x4000);

        if (!_socket.ReceiveAsync(_receiveSocketAsyncEventArgs))
            ProcessReadAsync(_receiveSocketAsyncEventArgs);
    }

    public void AsyncReadWithCallback(SocketReadCallback callback)
    {
        if (!IsOpen())
            return;

        _receiveSocketAsyncEventArgsWithCallback.Completed += (_, args) => callback(args);
        _receiveSocketAsyncEventArgsWithCallback.SetBuffer(0, 0x4000);

        if (!_socket.ReceiveAsync(_receiveSocketAsyncEventArgsWithCallback))
            callback(_receiveSocketAsyncEventArgsWithCallback);
    }

    public void AsyncWrite(byte[] data)
    {
        if (!IsOpen())
            return;

        _socket.Send(data);
    }

    public void CloseSocket()
    {
        if (_socket is not { Connected: true })
            return;

        try
        {
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }
        catch (Exception ex)
        {
            Log.Logger.Debug($"WorldSocket.CloseSocket: {GetRemoteIpAddress()} errored when shutting down socket: {ex.Message}");
        }

        OnClose();
    }

    public virtual void Dispose()
    {
        _socket.Dispose();
    }

    public IPEndPoint GetRemoteIpAddress()
    {
        return _remoteIpEndPoint;
    }

    public bool IsOpen()
    {
        return _socket.Connected;
    }

    public virtual void OnClose()
    {
        Dispose();
    }

    public abstract void ReadHandler(SocketAsyncEventArgs args);

    public void SetNoDelay(bool enable)
    {
        _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, enable);
    }

    public virtual bool Update()
    {
        return IsOpen();
    }

    private void ProcessReadAsync(SocketAsyncEventArgs args)
    {
        if (args.SocketError != SocketError.Success)
        {
            CloseSocket();

            return;
        }

        if (args.BytesTransferred == 0)
        {
            CloseSocket();

            return;
        }

        ReadHandler(args);
    }
}