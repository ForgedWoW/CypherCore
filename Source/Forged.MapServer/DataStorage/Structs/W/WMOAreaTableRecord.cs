// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Forged.MapServer.DataStorage.Structs.W
{
    public sealed record WMOAreaTableRecord
    {
        public string AreaName;
        public uint Id;
        public ushort WmoID;                                                   //  used in root WMO
        public byte NameSetID;                                                //  used in adt file
        public int WmoGroupID;                                               //  used in group WMO
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
}
