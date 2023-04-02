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
        WorldPacket.WritePackedGuid(ResurrectOffererGUID);
        WorldPacket.WriteUInt32(ResurrectOffererVirtualRealmAddress);
        WorldPacket.WriteUInt32(PetNumber);
        WorldPacket.WriteUInt32(SpellID);
        WorldPacket.WriteBits(Name.GetByteCount(), 11);
        WorldPacket.WriteBit(UseTimer);
        WorldPacket.WriteBit(Sickness);
        WorldPacket.FlushBits();

        WorldPacket.WriteString(Name);
    }
}