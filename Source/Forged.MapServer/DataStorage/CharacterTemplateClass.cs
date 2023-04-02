// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage;

public struct CharacterTemplateClass
{
    public byte ClassID;

    public FactionMasks FactionGroup;

    public CharacterTemplateClass(FactionMasks factionGroup, byte classID)
    {
        FactionGroup = factionGroup;
        ClassID = classID;
    }
}