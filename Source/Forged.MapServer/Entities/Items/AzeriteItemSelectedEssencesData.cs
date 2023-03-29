// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Entities.Items;

public class AzeriteItemSelectedEssencesData
{
    public uint SpecializationId;
    public uint[] AzeriteEssenceId = new uint[SharedConst.MaxAzeriteEssenceSlot];
}