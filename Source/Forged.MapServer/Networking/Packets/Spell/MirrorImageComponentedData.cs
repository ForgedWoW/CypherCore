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
        _worldPacket.WritePackedGuid(UnitGUID);
        _worldPacket.WriteInt32(DisplayID);
        _worldPacket.WriteInt32(SpellVisualKitID);
        _worldPacket.WriteUInt8(RaceID);
        _worldPacket.WriteUInt8(Gender);
        _worldPacket.WriteUInt8(ClassID);
        _worldPacket.WriteInt32(Customizations.Count);
        _worldPacket.WritePackedGuid(GuildGUID);
        _worldPacket.WriteInt32(ItemDisplayID.Count);

        foreach (var customization in Customizations)
        {
            _worldPacket.WriteUInt32(customization.ChrCustomizationOptionID);
            _worldPacket.WriteUInt32(customization.ChrCustomizationChoiceID);
        }

        foreach (var itemDisplayId in ItemDisplayID)
            _worldPacket.WriteInt32(itemDisplayId);
    }
}