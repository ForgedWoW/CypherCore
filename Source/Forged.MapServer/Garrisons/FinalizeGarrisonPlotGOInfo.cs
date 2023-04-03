// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Garrisons;

public class FinalizeGarrisonPlotGOInfo
{
    public FactionInfoModel[] FactionInfo = new FactionInfoModel[2];

    public struct FactionInfoModel
    {
        public ushort AnimKitId;
        public uint GameObjectId;
        public Position Pos;
    }
}