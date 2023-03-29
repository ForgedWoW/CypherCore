// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Net.Sockets;
using Serilog;

namespace Framework.Networking;

public class SocketManager<TSocketType> where TSocketType : ISocket
{
    public AsyncAcceptor Acceptor;
    private NetworkThread<TSocketType>[] _threads;
    private int _threadCount;

    public virtual bool StartNetwork(string bindIp, int port, int threadCount = 1)
    {
        if (threadCount <= 0)
            threadCount = 1;

        Acceptor = new AsyncAcceptor();

        if (!Acceptor.Start(bindIp, port))
        {
            Log.Logger.Error("StartNetwork failed to Start AsyncAcceptor");

            return false;
        }

        _threadCount = threadCount;
        _threads = new NetworkThread<TSocketType>[_threadCount];

        for (var i = 0; i < _threadCount; ++i)
        {
            _threads[i] = new NetworkThread<TSocketType>();
            _threads[i].Start();
        }

        Acceptor.AsyncAcceptSocket(OnSocketOpen);

        return true;
    }

    public virtual void StopNetwork()
    {
        Acceptor.Close();

        if (_threadCount != 0)
            for (var i = 0; i < _threadCount; ++i)
                _threads[i].Stop();

        Wait();

        Acceptor = null;
        _threads = null;
        _threadCount = 0;
    }

    public virtual void OnSocketOpen(Socket sock)
    {
        try
        {
            var newSocket = (TSocketType)Activator.CreateInstance(typeof(TSocketType), sock);
            newSocket.Accept();
            var thread = _threads[SelectThreadWithMinConnections()];

            if (thread != null)
                thread.AddSocket(newSocket);
        }
        catch (Exception err)
        {
            Log.Logger.Error(err, "");
        }
    }

    private void Wait()
    {
        if (_threadCount != 0)
            for (var i = 0; i < _threadCount; ++i)
                _threads[i].Wait();
    }

    private uint SelectThreadWithMinConnections()
    {
        uint min = 0;

        for (uint i = 1; i < _threadCount; ++i)
            if (_threads[i].GetConnectionCount() < _threads[min].GetConnectionCount())
                min = i;

        return min;
    }
}