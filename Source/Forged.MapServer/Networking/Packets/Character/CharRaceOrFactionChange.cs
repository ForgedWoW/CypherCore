// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects.Update;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

public class CharRaceOrFactionChange : ClientPacket
{
    public CharRaceOrFactionChangeInfo RaceOrFactionChangeInfo;
    public CharRaceOrFactionChange(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        RaceOrFactionChangeInfo = new CharRaceOrFactionChangeInfo
        {
            FactionChange = WorldPacket.HasBit()
        };

        var nameLength = WorldPacket.ReadBits<uint>(6);

        RaceOrFactionChangeInfo.Guid = WorldPacket.ReadPackedGuid();
        RaceOrFactionChangeInfo.SexID = (Gender)WorldPacket.ReadUInt8();
        RaceOrFactionChangeInfo.RaceID = (Race)WorldPacket.ReadUInt8();
        RaceOrFactionChangeInfo.InitialRaceID = (Race)WorldPacket.ReadUInt8();
        var customizationCount = WorldPacket.ReadUInt32();
        RaceOrFactionChangeInfo.Name = WorldPacket.ReadString(nameLength);

        for (var i = 0; i < customizationCount; ++i)
            RaceOrFactionChangeInfo.Customizations[i] = new ChrCustomizationChoice()
            {
                ChrCustomizationOptionID = WorldPacket.ReadUInt32(),
                ChrCustomizationChoiceID = WorldPacket.ReadUInt32()
            };

        RaceOrFactionChangeInfo.Customizations.Sort();
    }
}