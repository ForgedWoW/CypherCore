// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed class TraitDefinitionEffectPointsRecord
{
    public int CurveID;
    public int EffectIndex;
    public uint Id;
    public int OperationType;
    public int TraitDefinitionID;
    public TraitPointsOperationType GetOperationType()
    {
        return (TraitPointsOperationType)OperationType;
    }
}