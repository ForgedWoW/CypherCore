// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public class SetSpellCharges : ServerPacket
{
    public uint Category;
    public float ChargeModRate = 1.0f;
    public byte ConsumedCharges;
    public bool IsPet;
    public uint NextRecoveryTime;
    public SetSpellCharges() : base(ServerOpcodes.SetSpellCharges) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(Category);
        WorldPacket.WriteUInt32(NextRecoveryTime);
        WorldPacket.WriteUInt8(ConsumedCharges);
        WorldPacket.WriteFloat(ChargeModRate);
        WorldPacket.WriteBit(IsPet);
        WorldPacket.FlushBits();
    }
}