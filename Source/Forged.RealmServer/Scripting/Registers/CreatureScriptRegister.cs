// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.RealmServer.Scripting.Interfaces;

namespace Forged.RealmServer.Scripting.Registers;

public class CreatureScriptRegister : IScriptRegister
{
	public Type AttributeType => typeof(CreatureScriptAttribute);

	public void Register(ScriptAttribute attribute, IScriptObject script, string scriptName)
	{
		if (attribute is CreatureScriptAttribute creatureScript && creatureScript.CreatureIds != null)
			foreach (var id in creatureScript.CreatureIds)
			{
				var creatureTemplate = _gameObjectManager.GetCreatureTemplate(id);

				if (creatureTemplate == null)
				{
					Log.Logger.Error($"CreatureScriptAttribute: Unknown creature id {id} for script name {scriptName}");

					continue;
				}

				if (creatureTemplate.ScriptID == 0) // dont override database
					creatureTemplate.ScriptID = _gameObjectManager.GetScriptId(scriptName);
			}
	}
}