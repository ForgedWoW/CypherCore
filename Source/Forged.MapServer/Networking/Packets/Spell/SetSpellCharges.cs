// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public class SetSpellCharges : ServerPacket
{
    public bool IsPet;
    public uint Category;
    public uint NextRecoveryTime;
    public byte ConsumedCharges;
    public float ChargeModRate = 1.0f;
    public SetSpellCharges() : base(ServerOpcodes.SetSpellCharges) { }

    public override void Write()
    {
        _worldPacket.WriteUInt32(Category);
        _worldPacket.WriteUInt32(NextRecoveryTime);
        _worldPacket.WriteUInt8(ConsumedCharges);
        _worldPacket.WriteFloat(ChargeModRate);
        _worldPacket.WriteBit(IsPet);
        _worldPacket.FlushBits();
    }
}