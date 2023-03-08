using Framework.Constants;
using Game.DataStorage;

namespace Game.Entities;

public class Puppet : Minion
{
	public Puppet(SummonPropertiesRecord propertiesRecord, Unit owner) : base(propertiesRecord, owner, false)
	{
		Cypher.Assert(owner.IsTypeId(TypeId.Player));
		UnitTypeMask |= UnitTypeMask.Puppet;
	}

	public override void InitStats(uint duration)
	{
		base.InitStats(duration);

		SetLevel(GetOwner().GetLevel());
		SetReactState(ReactStates.Passive);
	}

	public override void InitSummon()
	{
		base.InitSummon();

		if (!SetCharmedBy(GetOwner(), CharmType.Possess))
			Cypher.Assert(false);
	}

	public override void Update(uint diff)
	{
		base.Update(diff);

		//check if caster is channelling?
		if (IsInWorld)
			if (!IsAlive())
				UnSummon();
		// @todo why long distance .die does not remove it
	}
}