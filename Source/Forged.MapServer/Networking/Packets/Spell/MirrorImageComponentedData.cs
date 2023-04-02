// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

internal class MirrorImageComponentedData : ServerPacket
{
    public byte ClassID;
    public List<ChrCustomizationChoice> Customizations = new();
    public int DisplayID;
    public byte Gender;
    public ObjectGuid GuildGUID;
    public List<int> ItemDisplayID = new();
    public byte RaceID;
    public int SpellVisualKitID;
    public ObjectGuid UnitGUID;
    public MirrorImageComponentedData() : base(ServerOpcodes.MirrorImageComponentedData) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(UnitGUID);
        WorldPacket.WriteInt32(DisplayID);
        WorldPacket.WriteInt32(SpellVisualKitID);
        WorldPacket.WriteUInt8(RaceID);
        WorldPacket.WriteUInt8(Gender);
        WorldPacket.WriteUInt8(ClassID);
        WorldPacket.WriteInt32(Customizations.Count);
        WorldPacket.WritePackedGuid(GuildGUID);
        WorldPacket.WriteInt32(ItemDisplayID.Count);

        foreach (var customization in Customizations)
        {
            WorldPacket.WriteUInt32(customization.ChrCustomizationOptionID);
            WorldPacket.WriteUInt32(customization.ChrCustomizationChoiceID);
        }

        foreach (var itemDisplayId in ItemDisplayID)
            WorldPacket.WriteInt32(itemDisplayId);
    }
}