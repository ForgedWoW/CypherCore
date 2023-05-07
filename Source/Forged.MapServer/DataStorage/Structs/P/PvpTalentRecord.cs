// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.P;

public sealed record PvpTalentRecord
{
    public int ActionBarSpellID;
    public string Description;
    public int Flags;
    public uint Id;
    public int LevelRequired;
    public uint OverridesSpellID;
    public int PlayerConditionID;
    public int PvpTalentCategoryID;
    public int SpecID;
    public uint SpellID;
}