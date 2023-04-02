// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SkillLineRecord
{
    public string AlternateVerb;
    public sbyte CanLink;
    public SkillCategory CategoryID;
    public string Description;
    public LocalizedString DisplayName;
    public int ExpansionNameSharedStringID;
    public ushort Flags;
    public string HordeDisplayName;
    public int HordeExpansionNameSharedStringID;
    public uint Id;
    public string OverrideSourceInfoDisplayName;
    public uint ParentSkillLineID;
    public int ParentTierIndex;
    public int SpellBookSpellID;
    public int SpellIconFileID;
    public SkillLineFlags GetFlags() => (SkillLineFlags)Flags;
}