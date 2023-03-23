// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Common.Networking.Packets.NPC;

public class TrainerListSpell
{
	public uint SpellID;
	public uint MoneyCost;
	public uint ReqSkillLine;
	public uint ReqSkillRank;
	public uint[] ReqAbility = new uint[SharedConst.MaxTrainerspellAbilityReqs];
	public TrainerSpellState Usable;
	public byte ReqLevel;
}
