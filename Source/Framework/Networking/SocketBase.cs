// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Net;
using System.Net.Sockets;
using Serilog;

namespace Framework.Networking;

public abstract class SocketBase : ISocket, IDisposable
{
	public delegate void SocketReadCallback(SocketAsyncEventArgs args);

	readonly Socket _socket;
	readonly IPEndPoint _remoteIpEndPoint;
	readonly SocketAsyncEventArgs _receiveSocketAsyncEventArgsWithCallback;
	readonly SocketAsyncEventArgs _receiveSocketAsyncEventArgs;

	protected SocketBase(Socket socket)
	{
		_socket = socket;
		_remoteIpEndPoint = (IPEndPoint)_socket.RemoteEndPoint;

		_receiveSocketAsyncEventArgsWithCallback = new SocketAsyncEventArgs();
		_receiveSocketAsyncEventArgsWithCallback.SetBuffer(new byte[0x4000], 0, 0x4000);

		_receiveSocketAsyncEventArgs = new SocketAsyncEventArgs();
		_receiveSocketAsyncEventArgs.SetBuffer(new byte[0x4000], 0, 0x4000);
		_receiveSocketAsyncEventArgs.Completed += (sender, args) => ProcessReadAsync(args);
	}

	public virtual void Dispose()
	{
		_socket.Dispose();
	}

	public abstract void Accept();

	public virtual bool Update()
	{
		return IsOpen();
	}

	public void CloseSocket()
	{
		if (_socket == null || !_socket.Connected)
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

	public bool IsOpen()
	{
		return _socket.Connected;
	}

	public IPEndPoint GetRemoteIpAddress()
	{
		return _remoteIpEndPoint;
	}

	public void AsyncReadWithCallback(SocketReadCallback callback)
	{
		if (!IsOpen())
			return;

		_receiveSocketAsyncEventArgsWithCallback.Completed += (sender, args) => callback(args);
		_receiveSocketAsyncEventArgsWithCallback.SetBuffer(0, 0x4000);

		if (!_socket.ReceiveAsync(_receiveSocketAsyncEventArgsWithCallback))
			callback(_receiveSocketAsyncEventArgsWithCallback);
	}

	public void AsyncRead()
	{
		if (!IsOpen())
			return;

		_receiveSocketAsyncEventArgs.SetBuffer(0, 0x4000);

		if (!_socket.ReceiveAsync(_receiveSocketAsyncEventArgs))
			ProcessReadAsync(_receiveSocketAsyncEventArgs);
	}

	public abstract void ReadHandler(SocketAsyncEventArgs args);

	public void AsyncWrite(byte[] data)
	{
		if (!IsOpen())
			return;

		_socket.Send(data);
	}

	public virtual void OnClose()
	{
		Dispose();
	}

	public void SetNoDelay(bool enable)
	{
		_socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, enable);
	}

	void ProcessReadAsync(SocketAsyncEventArgs args)
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