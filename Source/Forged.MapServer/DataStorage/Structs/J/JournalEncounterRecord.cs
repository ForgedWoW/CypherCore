// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Numerics;
using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.J
{
    public sealed class JournalEncounterRecord
    {
        public LocalizedString Name;
        public LocalizedString Description;
        public Vector2 Map;
        public uint Id;
        public ushort JournalInstanceID;
        public ushort DungeonEncounterID;
        public uint OrderIndex;
        public ushort FirstSectionID;
        public ushort UiMapID;
        public uint MapDisplayConditionID;
        public int Flags;
        public sbyte DifficultyMask;
    }
}
