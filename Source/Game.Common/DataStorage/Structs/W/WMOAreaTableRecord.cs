// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage;

public sealed class WMOAreaTableRecord
{
	public string AreaName;
	public uint Id;
	public ushort WmoID;   //  used in root WMO
	public byte NameSetID; //  used in adt file
	public int WmoGroupID; //  used in group WMO
	public byte SoundProviderPref;
	public byte SoundProviderPrefUnderwater;
	public ushort AmbienceID;
	public ushort UwAmbience;
	public ushort ZoneMusic;
	public uint UwZoneMusic;
	public ushort IntroSound;
	public ushort UwIntroSound;
	public ushort AreaTableID;
	public byte Flags;
}