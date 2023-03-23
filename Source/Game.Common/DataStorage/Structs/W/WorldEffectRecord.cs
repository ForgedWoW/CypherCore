using Game.DataStorage;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.W;

public sealed class WorldEffectRecord
{
	public uint Id;
	public uint QuestFeedbackEffectID;
	public byte WhenToDisplay;
	public byte TargetType;
	public int TargetAsset;
	public uint PlayerConditionID;
	public ushort CombatConditionID;
}
