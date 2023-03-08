using Framework.Dynamic;

namespace Game.Entities;

public class ForcedUnsummonDelayEvent : BasicEvent
{
	readonly TempSummon _owner;

	public ForcedUnsummonDelayEvent(TempSummon owner)
	{
		_owner = owner;
	}

	public override bool Execute(ulong etime, uint pTime)
	{
		_owner.UnSummon();

		return true;
	}
}