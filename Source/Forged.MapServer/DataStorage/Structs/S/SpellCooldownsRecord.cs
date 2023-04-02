// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellCooldownsRecord
{
    public uint AuraSpellID;
    public uint CategoryRecoveryTime;
    public byte DifficultyID;
    public uint Id;
    public uint RecoveryTime;
    public uint SpellID;
    public uint StartRecoveryTime;
}