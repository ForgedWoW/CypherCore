// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Framework.Dynamic;

namespace Forged.MapServer.Entities.Creatures;

public class AssistDelayEvent : BasicEvent
{
	readonly List<ObjectGuid> m_assistants = new();
	readonly Unit m_owner;


	readonly ObjectGuid m_victim;

	AssistDelayEvent() { }

	public AssistDelayEvent(ObjectGuid victim, Unit owner)
	{
		m_victim = victim;
		m_owner = owner;
	}

	public override bool Execute(ulong etime, uint pTime)
	{
		var victim = Global.ObjAccessor.GetUnit(m_owner, m_victim);

		if (victim != null)
			while (!m_assistants.Empty())
			{
				var assistant = m_owner.Map.GetCreature(m_assistants[0]);
				m_assistants.RemoveAt(0);

				if (assistant != null && assistant.CanAssistTo(m_owner, victim))
				{
					assistant.SetNoCallAssistance(true);
					assistant.EngageWithTarget(victim);
				}
			}

		return true;
	}

	public void AddAssistant(ObjectGuid guid)
	{
		m_assistants.Add(guid);
	}
}