// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Spells;

public class SkillStatusData
{
    public byte Pos;
    public SkillState State;

    public SkillStatusData(uint pos, SkillState state)
    {
        Pos = (byte)pos;
        State = state;
    }
}