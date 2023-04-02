// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

internal class ResurrectRequest : ServerPacket
{
    public string Name;
    public uint PetNumber;
    public ObjectGuid ResurrectOffererGUID;
    public uint ResurrectOffererVirtualRealmAddress;
    public bool Sickness;
    public uint SpellID;
    public bool UseTimer;
    public ResurrectRequest() : base(ServerOpcodes.ResurrectRequest) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(ResurrectOffererGUID);
        _worldPacket.WriteUInt32(ResurrectOffererVirtualRealmAddress);
        _worldPacket.WriteUInt32(PetNumber);
        _worldPacket.WriteUInt32(SpellID);
        _worldPacket.WriteBits(Name.GetByteCount(), 11);
        _worldPacket.WriteBit(UseTimer);
        _worldPacket.WriteBit(Sickness);
        _worldPacket.FlushBits();

        _worldPacket.WriteString(Name);
    }
}