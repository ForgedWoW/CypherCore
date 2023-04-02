// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellAuraRestrictionsRecord
{
    public uint CasterAuraSpell;
    public int CasterAuraState;
    public int CasterAuraType;
    public uint DifficultyID;
    public uint ExcludeCasterAuraSpell;
    public int ExcludeCasterAuraState;
    public int ExcludeCasterAuraType;
    public uint ExcludeTargetAuraSpell;
    public int ExcludeTargetAuraState;
    public int ExcludeTargetAuraType;
    public uint Id;
    public uint SpellID;
    public uint TargetAuraSpell;
    public int TargetAuraState;
    public int TargetAuraType;
}