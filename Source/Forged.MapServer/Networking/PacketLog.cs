// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Forged.MapServer.Chrono;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Util;
using Microsoft.Extensions.Configuration;

namespace Forged.MapServer.Networking;

public class PacketLog
{
    private readonly string _fullPath;
    private readonly ConcurrentQueue<(byte[], uint, IPEndPoint, ConnectionType, bool)> _packetQueue = new();
    private readonly AutoResetEvent _queueSemaphore = new(false);

    public PacketLog(WorldManager worldManager, IConfiguration configuration)
    {
        var logsDir = AppContext.BaseDirectory + configuration.GetDefaultValue("LogsDir", "");
        var logname = configuration.GetDefaultValue("PacketLogFile", "");

        if (!string.IsNullOrEmpty(logname))
        {
            _fullPath = logsDir + @"\" + logname;
            using var writer = new BinaryWriter(File.Open(_fullPath, FileMode.Create));
            writer.Write(Encoding.ASCII.GetBytes("PKT"));
            writer.Write((ushort)769);
            writer.Write(Encoding.ASCII.GetBytes("T"));
            writer.Write(WorldManager.Realm.Build);
            writer.Write(Encoding.ASCII.GetBytes("enUS"));
            writer.Write(new byte[40]); //SessionKey
            writer.Write((uint)GameTime.CurrentTime);
            writer.Write(Time.MSTime);
            writer.Write(0);
        }

        Task.Run(() =>
        {
            using var writer = new BinaryWriter(File.Open(_fullPath, FileMode.Append), Encoding.ASCII);

            while (!worldManager.IsShuttingDown)
            {
                _queueSemaphore.WaitOne(500);

                while (_packetQueue.Count != 0)
                    if (_packetQueue.TryDequeue(out var packet))
                    {
                        writer.Write(packet.Item5 ? 0x47534d43 : 0x47534d53);
                        writer.Write((uint)packet.Item4);
                        writer.Write(Time.MSTime);

                        writer.Write(20);
                        var socketIPBytes = new byte[16];

                        Buffer.BlockCopy(packet.Item3.Address.GetAddressBytes(), 0, socketIPBytes, 0, packet.Item3.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 4 : 16);

                        var size = packet.Item1.Length;

                        if (packet.Item5)
                            size -= 2;

                        writer.Write(size + 4);
                        writer.Write(socketIPBytes);
                        writer.Write(packet.Item3.Port);
                        writer.Write(packet.Item2);

                        writer.Write(packet.Item1, packet.Item5 ? 2 : 0, size);
                    }
            }
        });
    }

    public bool CanLog()
    {
        return !string.IsNullOrEmpty(_fullPath);
    }

    public void Write(byte[] data, uint opcode, IPEndPoint endPoint, ConnectionType connectionType, bool isClientPacket)
    {
        if (!CanLog())
            return;

        _packetQueue.Enqueue((data, opcode, endPoint, connectionType, isClientPacket));
        _queueSemaphore.Set();
    }
}