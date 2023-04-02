// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

internal class LogXPGain : ServerPacket
{
    public int Amount;
    public float GroupBonus;
    public int Original;
    public PlayerLogXPReason Reason;
    public ObjectGuid Victim;
    public LogXPGain() : base(ServerOpcodes.LogXpGain) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Victim);
        WorldPacket.WriteInt32(Original);
        WorldPacket.WriteUInt8((byte)Reason);
        WorldPacket.WriteInt32(Amount);
        WorldPacket.WriteFloat(GroupBonus);
    }
}