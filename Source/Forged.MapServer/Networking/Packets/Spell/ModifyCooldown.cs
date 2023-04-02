// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public class ModifyCooldown : ServerPacket
{
    public int DeltaTime;
    public bool IsPet;
    public uint SpellID;
    public bool WithoutCategoryCooldown;
    public ModifyCooldown() : base(ServerOpcodes.ModifyCooldown, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(SpellID);
        WorldPacket.WriteInt32(DeltaTime);
        WorldPacket.WriteBit(IsPet);
        WorldPacket.WriteBit(WithoutCategoryCooldown);
        WorldPacket.FlushBits();
    }
}