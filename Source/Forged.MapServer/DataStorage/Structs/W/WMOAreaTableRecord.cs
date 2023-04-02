// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.W;

public sealed class WMOAreaTableRecord
{
    public ushort AmbienceID;
    public string AreaName;
    public ushort AreaTableID;
    public byte Flags;
    public uint Id;
    public ushort IntroSound;
    public byte NameSetID;
    public byte SoundProviderPref;
    public byte SoundProviderPrefUnderwater;
    public ushort UwAmbience;
    public ushort UwIntroSound;
    public uint UwZoneMusic;
    //  used in adt file
    public int WmoGroupID;

    public ushort WmoID;   //  used in root WMO
                           //  used in group WMO
    public ushort ZoneMusic;
}