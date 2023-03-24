// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Common.DataStorage.Structs.T;

public sealed class TraitDefinitionEffectPointsRecord
{
	public uint Id;
	public int TraitDefinitionID;
	public int EffectIndex;
	public int OperationType;
	public int CurveID;

	public TraitPointsOperationType GetOperationType()
	{
		return (TraitPointsOperationType)OperationType;
	}
}
