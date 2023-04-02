// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects.Update;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

public class CreateCharacter : ClientPacket
{
    public CharacterCreateInfo CreateInfo;
    public CreateCharacter(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        CreateInfo = new CharacterCreateInfo();
        var nameLength = WorldPacket.ReadBits<uint>(6);
        var hasTemplateSet = WorldPacket.HasBit();
        CreateInfo.IsTrialBoost = WorldPacket.HasBit();
        CreateInfo.UseNPE = WorldPacket.HasBit();

        CreateInfo.RaceId = (Race)WorldPacket.ReadUInt8();
        CreateInfo.ClassId = (PlayerClass)WorldPacket.ReadUInt8();
        CreateInfo.Sex = (Gender)WorldPacket.ReadUInt8();
        var customizationCount = WorldPacket.ReadUInt32();

        CreateInfo.Name = WorldPacket.ReadString(nameLength);

        if (CreateInfo.TemplateSet.HasValue)
            CreateInfo.TemplateSet = WorldPacket.ReadUInt32();

        for (var i = 0; i < customizationCount; ++i)
            CreateInfo.Customizations[i] = new ChrCustomizationChoice()
            {
                ChrCustomizationOptionID = WorldPacket.ReadUInt32(),
                ChrCustomizationChoiceID = WorldPacket.ReadUInt32()
            };

        CreateInfo.Customizations.Sort();
    }
}