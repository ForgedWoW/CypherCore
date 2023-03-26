namespace Forged.MapServer.Globals;

public class MailLevelReward
{
    public ulong RaceMask;
    public uint MailTemplateId;
    public uint SenderEntry;

    public MailLevelReward(ulong raceMask = 0, uint mailTemplateId = 0, uint senderEntry = 0)
    {
        RaceMask = raceMask;
        MailTemplateId = mailTemplateId;
        SenderEntry = senderEntry;
    }
}