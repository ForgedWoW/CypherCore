namespace Game.Achievements;

public class AchievementReward
{
	public uint[] TitleId = new uint[2];
	public uint ItemId;
	public uint SenderCreatureId;
	public string Subject;
	public string Body;
	public uint MailTemplateId;
}