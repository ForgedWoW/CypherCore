// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Forged.MapServer.DataStorage.Structs.G
{
    public sealed record GameObjectArtKitRecord
    {
        public uint Id;
        public int AttachModelFileID;
        public int[] TextureVariationFileID = new int[3];
    }
}
