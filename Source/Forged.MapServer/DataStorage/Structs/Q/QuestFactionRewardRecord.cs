// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Forged.MapServer.DataStorage.Structs.Q
{
    public sealed record QuestFactionRewardRecord
    {
        public uint Id;
        public short[] Difficulty = new short[10];
    }
}
