// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Trait;

internal class TraitConfigCommitFailed : ServerPacket
{
    public int ConfigID;
    public int Reason;
    public uint SpellID;

    public TraitConfigCommitFailed(int configId = 0, uint spellId = 0, int reason = 0) : base(ServerOpcodes.TraitConfigCommitFailed)
    {
        ConfigID = configId;
        SpellID = spellId;
        Reason = reason;
    }

    public override void Write()
    {
        WorldPacket.WriteInt32(ConfigID);
        WorldPacket.WriteUInt32(SpellID);
        WorldPacket.WriteBits(Reason, 4);
        WorldPacket.FlushBits();
    }
}