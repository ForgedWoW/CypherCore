using Game.Entities;

namespace Game.Achievements;

public class CriteriaProgress
{
	public ulong Counter;
	public long Date;             // latest update time.
	public ObjectGuid PlayerGUID; // GUID of the player that completed this criteria (guild achievements)
	public bool Changed;
}