// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects.Update;

namespace Forged.MapServer.Networking.Packets.Character;

public class AlterApperance : ClientPacket
{
    public byte NewSex;
    public Array<ChrCustomizationChoice> Customizations = new(72);
    public int CustomizedRace;
    public AlterApperance(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        var customizationCount = _worldPacket.ReadUInt32();
        NewSex = _worldPacket.ReadUInt8();
        CustomizedRace = _worldPacket.ReadInt32();

        for (var i = 0; i < customizationCount; ++i)
            Customizations[i] = new ChrCustomizationChoice()
            {
                ChrCustomizationOptionID = _worldPacket.ReadUInt32(),
                ChrCustomizationChoiceID = _worldPacket.ReadUInt32()
            };

        Customizations.Sort();
    }
}