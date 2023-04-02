// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Duel;

public class DuelWinner : ServerPacket
{
    public string BeatenName;
    public uint BeatenVirtualRealmAddress;
    public bool Fled;
    public string WinnerName;
    public uint WinnerVirtualRealmAddress;
    public DuelWinner() : base(ServerOpcodes.DuelWinner, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteBits(BeatenName.GetByteCount(), 6);
        WorldPacket.WriteBits(WinnerName.GetByteCount(), 6);
        WorldPacket.WriteBit(Fled);
        WorldPacket.WriteUInt32(BeatenVirtualRealmAddress);
        WorldPacket.WriteUInt32(WinnerVirtualRealmAddress);
        WorldPacket.WriteString(BeatenName);
        WorldPacket.WriteString(WinnerName);
    }
}