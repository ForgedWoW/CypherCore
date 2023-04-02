// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects.Update;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

public class CharCustomize : ClientPacket
{
    public CharCustomizeInfo CustomizeInfo;
    public CharCustomize(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        CustomizeInfo = new CharCustomizeInfo
        {
            CharGUID = WorldPacket.ReadPackedGuid(),
            SexID = (Gender)WorldPacket.ReadUInt8()
        };

        var customizationCount = WorldPacket.ReadUInt32();

        for (var i = 0; i < customizationCount; ++i)
            CustomizeInfo.Customizations[i] = new ChrCustomizationChoice()
            {
                ChrCustomizationOptionID = WorldPacket.ReadUInt32(),
                ChrCustomizationChoiceID = WorldPacket.ReadUInt32()
            };

        CustomizeInfo.Customizations.Sort();

        CustomizeInfo.CharName = WorldPacket.ReadString(WorldPacket.ReadBits<uint>(6));
    }
}