// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Framework.Networking;

public class NetworkThread<TSocketType> where TSocketType : ISocket
{
	readonly List<TSocketType> _sockets = new();
	readonly List<TSocketType> _newSockets = new();
	int _connections;
	volatile bool _stopped;

	Task _thread;

	public void Stop()
	{
		_stopped = true;
	}

	public bool Start()
	{
		if (_thread != null)
			return false;

		_thread = Task.Run(Run);

		return true;
	}

	public void Wait()
	{
		_thread.Wait();
		_thread = null;
	}

	public int GetConnectionCount()
	{
		return _connections;
	}

	public virtual void AddSocket(TSocketType sock)
	{
		Interlocked.Increment(ref _connections);
		lock (_newSockets)
			_newSockets.Add(sock);
		SocketAdded(sock);
	}

	void AddNewSockets()
	{
		lock (_newSockets)
		{
			if (_newSockets.Empty())
				return;

			foreach (var socket in _newSockets.ToList())
				if (!socket.IsOpen())
				{
					SocketRemoved(socket);

					Interlocked.Decrement(ref _connections);
				}
				else
				{
					_sockets.Add(socket);
				}

			_newSockets.Clear();
		}
	}

	void Run()
	{
		Log.Logger.Debug("Network Thread Starting");

		var sleepTime = 1;

		while (!_stopped)
		{
			Thread.Sleep(sleepTime);

			var tickStart = Time.MSTime;

			AddNewSockets();

			for (var i = _sockets.Count - 1; i >= 0 ; --i)
			{
				var socket = _sockets[i];

				if (!socket.Update())
				{
					if (socket.IsOpen())
						socket.CloseSocket();

					SocketRemoved(socket);

					Interlocked.Decrement(ref _connections);
					_sockets.Remove(socket);
				}
			}

			var diff = Time.GetMSTimeDiffToNow(tickStart);
			sleepTime = (int)(diff > 1 ? 0 : 1 - diff);
		}

		Log.Logger.Debug("Network Thread exits");
        lock (_newSockets)
            _newSockets.Clear();
		_sockets.Clear();
	}

	protected virtual void SocketAdded(TSocketType sock) { }

	protected virtual void SocketRemoved(TSocketType sock) { }
}