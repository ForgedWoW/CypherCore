﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Globals;

public class LanguageDesc
{
    public uint SkillId;
    public uint SpellId;
    public LanguageDesc() { }

    public LanguageDesc(uint spellId, uint skillId)
    {
        SpellId = spellId;
        SkillId = skillId;
    }

    public static bool operator !=(LanguageDesc left, LanguageDesc right)
    {
        return !(left == right);
    }

    public static bool operator ==(LanguageDesc left, LanguageDesc right)
    {
        return left.SpellId == right.SpellId && left.SkillId == right.SkillId;
    }

    public override bool Equals(object obj)
    {
        if (obj is LanguageDesc)
            return (LanguageDesc)obj == this;

        return false;
    }

    public override int GetHashCode()
    {
        return SpellId.GetHashCode() ^ SkillId.GetHashCode();
    }
}