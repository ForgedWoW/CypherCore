// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Forged.MapServer.DataStorage.Structs.C
{
    public sealed record Cfg_RegionsRecord
    {
        public uint Id;
        public string Tag;
        public ushort RegionID;
        public uint Raidorigin;                                              // Date of first raid reset, all other resets are calculated as this date plus interval
        public byte RegionGroupMask;
        public uint ChallengeOrigin;
    }
}
