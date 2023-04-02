// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.NPC;

public class TrainerListSpell
{
    public uint MoneyCost;
    public uint[] ReqAbility = new uint[SharedConst.MaxTrainerspellAbilityReqs];
    public byte ReqLevel;
    public uint ReqSkillLine;
    public uint ReqSkillRank;
    public uint SpellID;
    public TrainerSpellState Usable;
}