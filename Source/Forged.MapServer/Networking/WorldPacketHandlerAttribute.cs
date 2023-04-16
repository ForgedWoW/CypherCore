// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Networking;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class WorldPacketHandlerAttribute : Attribute
{
    public WorldPacketHandlerAttribute(ClientOpcodes opcode)
    {
        Opcode = opcode;
        Status = SessionStatus.Loggedin;
        Processing = PacketProcessing.ThreadUnsafe;
    }

    public ClientOpcodes Opcode { get; private set; }
    public PacketProcessing Processing { get; set; }
    public SessionStatus Status { get; set; }
}