// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.IO;
using System.Numerics;

namespace Forged.MapServer.DataStorage.Structs
{
    public struct M2SplineKey
    {
        public M2SplineKey(BinaryReader reader)
        {
            p0 = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            p1 = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            p2 = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public Vector3 p0;
        public Vector3 p1;
        public Vector3 p2;
    }
}
