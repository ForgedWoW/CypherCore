// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed class ChrSpecializationRecord
{
    public int AnimReplacements;
    public byte ClassID;
    public string Description;
    public string FemaleName;
    public ChrSpecializationFlag Flags;
    public uint Id;
    public uint[] MasterySpellID = new uint[PlayerConst.MaxMasterySpells];
    public LocalizedString Name;
    public byte OrderIndex;
    public sbyte PetTalentType;
    public sbyte PrimaryStatPriority;
    public sbyte Role;
    public int SpellIconFileID;

    public bool IsPetSpecialization()
    {
        return ClassID == 0;
    }
}