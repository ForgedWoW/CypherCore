// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.J;

public sealed record JournalEncounterSectionRecord
{
    public LocalizedString BodyText;
    public sbyte DifficultyMask;
    public ushort FirstChildSectionID;
    public int Flags;
    public uint IconCreatureDisplayInfoID;
    public int IconFileDataID;
    public int IconFlags;
    public uint Id;
    public ushort JournalEncounterID;
    public ushort NextSiblingSectionID;
    public byte OrderIndex;
    public ushort ParentSectionID;
    public int SpellID;
    public LocalizedString Title;
    public byte Type;
    public int UiModelSceneID;
}