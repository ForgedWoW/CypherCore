// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.G;

public sealed class GameObjectArtKitRecord
{
    public int AttachModelFileID;
    public uint Id;
    public int[] TextureVariationFileID = new int[3];
}