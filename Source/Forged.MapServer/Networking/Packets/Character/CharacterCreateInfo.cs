// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects.Update;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

public class CharacterCreateInfo
{
    // Server side data
    public byte CharCount = 0;

    public PlayerClass ClassId = PlayerClass.None;

    public Array<ChrCustomizationChoice> Customizations = new(125);

    public bool IsTrialBoost;

    public string Name;

    // User specified variables
    public Race RaceId = Race.None;
    public Gender Sex = Gender.None;
    public uint? TemplateSet;
    public bool UseNPE;
}