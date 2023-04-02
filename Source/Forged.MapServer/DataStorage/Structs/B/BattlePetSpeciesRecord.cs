// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.B;

public sealed class BattlePetSpeciesRecord
{
    public int CardUIModelSceneID;
    public int CovenantID;
    public uint CreatureID;
    public string Description;
    public int Flags;
    public int IconFileDataID;
    public uint Id;
    public int LoadoutUIModelSceneID;
    public sbyte PetTypeEnum;
    public string SourceText;
    public sbyte SourceTypeEnum;
    public uint SummonSpellID;
    public BattlePetSpeciesFlags GetFlags()
    {
        return (BattlePetSpeciesFlags)Flags;
    }
}