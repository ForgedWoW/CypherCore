// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Forged.MapServer.DataStorage.Structs.R
{
    public sealed record RandPropPointsRecord
    {
        public uint Id;
        public float DamageReplaceStatF;
        public float DamageSecondaryF;
        public int DamageReplaceStat;
        public int DamageSecondary;
        public float[] EpicF = new float[5];
        public float[] SuperiorF = new float[5];
        public float[] GoodF = new float[5];
        public uint[] Epic = new uint[5];
        public uint[] Superior = new uint[5];
        public uint[] Good = new uint[5];
    }
}
