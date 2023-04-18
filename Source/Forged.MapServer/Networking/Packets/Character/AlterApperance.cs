// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects.Update;

namespace Forged.MapServer.Networking.Packets.Character;

public class AlterApperance : ClientPacket
{
    public Array<ChrCustomizationChoice> Customizations = new(72);
    public int CustomizedRace;
    public byte NewSex;
    public AlterApperance(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        var customizationCount = WorldPacket.ReadUInt32();
        NewSex = WorldPacket.ReadUInt8();
        CustomizedRace = WorldPacket.ReadInt32();

        for (var i = 0; i < customizationCount; ++i)
            Customizations[i] = new ChrCustomizationChoice
            {
                ChrCustomizationOptionID = WorldPacket.ReadUInt32(),
                ChrCustomizationChoiceID = WorldPacket.ReadUInt32()
            };

        Customizations.Sort();
    }
}