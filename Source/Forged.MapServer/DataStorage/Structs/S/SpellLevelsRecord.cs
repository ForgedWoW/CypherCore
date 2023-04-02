// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellLevelsRecord
{
    public ushort BaseLevel;
    public byte DifficultyID;
    public uint Id;
    public ushort MaxLevel;
    public byte MaxPassiveAuraLevel;
    public uint SpellID;
    public ushort SpellLevel;
}