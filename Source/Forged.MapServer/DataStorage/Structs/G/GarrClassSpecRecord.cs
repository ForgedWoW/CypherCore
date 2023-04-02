// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.G;

public sealed class GarrClassSpecRecord
{
    public string ClassSpec;
    public string ClassSpecFemale;
    public string ClassSpecMale;
    public int Flags;
    public byte FollowerClassLimit;
    public ushort GarrFollItemSetID;
    public uint Id;
    public ushort UiTextureAtlasMemberID;
}