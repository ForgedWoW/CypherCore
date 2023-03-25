// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Achievements;

public class AchievementReward
{
	public uint[] TitleId = new uint[2];
	public uint ItemId;
	public uint SenderCreatureId;
	public string Subject;
	public string Body;
	public uint MailTemplateId;
}