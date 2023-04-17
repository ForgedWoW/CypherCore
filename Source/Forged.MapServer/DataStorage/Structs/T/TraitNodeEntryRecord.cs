// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed class TraitNodeEntryRecord
{
    public uint Id;
    public int MaxRanks;
    public byte NodeEntryType;
    public int TraitDefinitionID;

    public TraitNodeEntryType GetNodeEntryType()
    {
        return (TraitNodeEntryType)NodeEntryType;
    }
}