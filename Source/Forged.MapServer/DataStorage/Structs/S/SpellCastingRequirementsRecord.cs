// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellCastingRequirementsRecord
{
    public byte FacingCasterFlags;
    public uint Id;
    public ushort MinFactionID;
    public int MinReputation;
    public ushort RequiredAreasID;
    public byte RequiredAuraVision;
    public ushort RequiresSpellFocus;
    public uint SpellID;
}