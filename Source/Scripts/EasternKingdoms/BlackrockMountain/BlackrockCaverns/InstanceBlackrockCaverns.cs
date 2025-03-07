// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.BaseScripts;
using Game.Scripting.Interfaces.IMap;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockCaverns;

internal struct DataTypes
{
	// Encounter States // Boss GUIDs
	public const uint RomoggBonecrusher = 0;
	public const uint Corla = 1;
	public const uint KarshSteelbender = 2;
	public const uint Beauty = 3;
	public const uint AscendantLordObsidius = 4;

	// Additional Objects
	public const uint RazTheCrazed = 5;
}

internal struct CreatureIds
{
	public const uint TwilightFlameCaller = 39708;
	public const uint RazTheCrazed = 39670;
	public const uint RomoggBonecrusher = 39665;
}

[Script]
internal class instance_blackrock_caverns : InstanceMapScript, IInstanceMapGetInstanceScript
{
	private static readonly ObjectData[] creatureData =
	{
		new(CreatureIds.RazTheCrazed, DataTypes.RazTheCrazed)
	};

	private static readonly DungeonEncounterData[] encounters =
	{
		new(DataTypes.RomoggBonecrusher, 1040), new(DataTypes.Corla, 1038), new(DataTypes.KarshSteelbender, 1039), new(DataTypes.Beauty, 1037), new(DataTypes.AscendantLordObsidius, 1036)
	};

	public instance_blackrock_caverns() : base(nameof(instance_blackrock_caverns), 645) { }

	public InstanceScript GetInstanceScript(InstanceMap map)
	{
		return new instance_blackrock_caverns_InstanceMapScript(map);
	}

	private class instance_blackrock_caverns_InstanceMapScript : InstanceScript
	{
		public instance_blackrock_caverns_InstanceMapScript(InstanceMap map) : base(map)
		{
			SetHeaders("BRC");
			SetBossNumber(5);
			LoadObjectData(creatureData, null);
			LoadDungeonEncounterData(encounters);
		}

		public override bool SetBossState(uint type, EncounterState state)
		{
			if (!base.SetBossState(type, state))
				return false;

			switch (type)
			{
				case DataTypes.RomoggBonecrusher:
				case DataTypes.Corla:
				case DataTypes.KarshSteelbender:
				case DataTypes.Beauty:
				case DataTypes.AscendantLordObsidius:
					break;
				default:
					break;
			}

			return true;
		}
	}
}