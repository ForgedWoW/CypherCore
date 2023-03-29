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
        var nameLength = _worldPacket.ReadBits<uint>(6);
        var hasTemplateSet = _worldPacket.HasBit();
        CreateInfo.IsTrialBoost = _worldPacket.HasBit();
        CreateInfo.UseNPE = _worldPacket.HasBit();

        CreateInfo.RaceId = (Race)_worldPacket.ReadUInt8();
        CreateInfo.ClassId = (PlayerClass)_worldPacket.ReadUInt8();
        CreateInfo.Sex = (Gender)_worldPacket.ReadUInt8();
        var customizationCount = _worldPacket.ReadUInt32();

        CreateInfo.Name = _worldPacket.ReadString(nameLength);

        if (CreateInfo.TemplateSet.HasValue)
            CreateInfo.TemplateSet = _worldPacket.ReadUInt32();

        for (var i = 0; i < customizationCount; ++i)
            CreateInfo.Customizations[i] = new ChrCustomizationChoice()
            {
                ChrCustomizationOptionID = _worldPacket.ReadUInt32(),
                ChrCustomizationChoiceID = _worldPacket.ReadUInt32()
            };

        CreateInfo.Customizations.Sort();
    }
}