// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.PyroguardEmberseer;

internal struct SpellIds
{
	public const uint EncagedEmberseer = 15282;        // Self on spawn
	public const uint FireShieldTrigger = 13377;       // Self on spawn missing from 335 dbc triggers public const uint FireShield every 3 sec
	public const uint FireShield = 13376;              // Triggered by public const uint FireShieldTrigger
	public const uint FreezeAnim = 16245;              // Self on event start
	public const uint EmberseerGrowing = 16048;        // Self on event start
	public const uint EmberseerGrowingTrigger = 16049; // Triggered by public const uint EmberseerGrowing
	public const uint EmberseerFullStrength = 16047;   // Emberseer Full Strength
	public const uint Firenova = 23462;                // Combat
	public const uint Flamebuffet = 23341;             // Combat

	public const uint Pyroblast = 17274; // Combat

	// Blackhand Incarcerator public const uint s
	public const uint EncageEmberseer = 15281; // Emberseer on spawn
	public const uint Strike = 15580;          // Combat

	public const uint Encage = 16045; // Combat

	// Cast on player by altar
	public const uint EmberseerObjectVisual = 16532;
}

internal struct TextIds
{
	public const uint EmoteOneStack = 0;
	public const uint EmoteTenStack = 1;
	public const uint EmoteFreeOfBonds = 2;
	public const uint YellFreeOfBonds = 3;
}

[Script]
internal class boss_pyroguard_emberseer : BossAI
{
	public boss_pyroguard_emberseer(Creature creature) : base(creature, DataTypes.PyrogaurdEmberseer) { }

	public override void Reset()
	{
		Me.SetUnitFlag(UnitFlags.Uninteractible);
		Me.SetImmuneToPC(true);
		Scheduler.CancelAll();
		// Apply Auras on spawn and reset
		// DoCast(me, SpellFireShieldTrigger); // Need to find this in old Dbc if possible
		Me.RemoveAura(SpellIds.EmberseerFullStrength);
		Me.RemoveAura(SpellIds.EmberseerGrowing);
		Me.RemoveAura(SpellIds.EmberseerGrowingTrigger);

		Scheduler.Schedule(TimeSpan.FromSeconds(5),
							task =>
							{
								Instance.SetData(DataTypes.BlackhandIncarcerator, 1);
								Instance.SetBossState(DataTypes.PyrogaurdEmberseer, EncounterState.NotStarted);
							});

		// Hack for missing trigger spell
		Scheduler.Schedule(TimeSpan.FromSeconds(3),
							task =>
							{
								// #### Spell isn't doing any Damage ??? ####
								DoCast(Me, SpellIds.FireShield);
								task.Repeat(TimeSpan.FromSeconds(3));
							});
	}

	public override void SetData(uint type, uint data)
	{
		switch (data)
		{
			case 1:
				Scheduler.Schedule(TimeSpan.FromSeconds(5),
									task =>
									{
										// As of Patch 3.0.8 only one person needs to channel the altar
										var _hasAura = false;
										var players = Me.Map.Players;

										foreach (var player in players)
											if (player != null &&
												player.HasAura(SpellIds.EmberseerObjectVisual))
											{
												_hasAura = true;

												break;
											}

										if (_hasAura)
										{
											task.Schedule(TimeSpan.FromSeconds(1),
														preFlightTask1 =>
														{
															// Set data on all Blackhand Incarcerators
															var creatureList = Me.GetCreatureListWithEntryInGrid(CreaturesIds.BlackhandIncarcerator, 35.0f);

															foreach (var creature in creatureList)
																if (creature)
																{
																	creature.SetImmuneToAll(false);
																	creature.InterruptSpell(CurrentSpellTypes.Channeled);
																	DoZoneInCombat(creature);
																}

															Me.RemoveAura(SpellIds.EncagedEmberseer);

															preFlightTask1.Schedule(TimeSpan.FromSeconds(32),
																					preFlightTask2 =>
																					{
																						Me.CastSpell(Me, SpellIds.FreezeAnim);
																						Me.CastSpell(Me, SpellIds.EmberseerGrowing);
																						Talk(TextIds.EmoteOneStack);
																					});
														});

											Instance.SetBossState(DataTypes.PyrogaurdEmberseer, EncounterState.InProgress);
										}
									});

				break;
			default:
				break;
		}
	}

	public override void JustEngagedWith(Unit who)
	{
		// ### Todo Check combat timing ###
		Scheduler.Schedule(TimeSpan.FromSeconds(6),
							task =>
							{
								DoCast(Me, SpellIds.Firenova);
								task.Repeat(TimeSpan.FromSeconds(6));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(3),
							task =>
							{
								DoCast(Me, SpellIds.Flamebuffet);
								task.Repeat(TimeSpan.FromSeconds(14));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(14),
							task =>
							{
								var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

								if (target)
									DoCast(target, SpellIds.Pyroblast);

								task.Repeat(TimeSpan.FromSeconds(15));
							});
	}

	public override void JustDied(Unit killer)
	{
		// Activate all the runes
		UpdateRunes(GameObjectState.Ready);
		// Complete encounter
		Instance.SetBossState(DataTypes.PyrogaurdEmberseer, EncounterState.Done);
	}

	public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
	{
		if (spellInfo.Id == SpellIds.EncageEmberseer)
			if (Me.GetAuraCount(SpellIds.EncagedEmberseer) == 0)
			{
				Me.CastSpell(Me, SpellIds.EncagedEmberseer);
				Reset();
			}

		if (spellInfo.Id == SpellIds.EmberseerGrowingTrigger)
		{
			if (Me.GetAuraCount(SpellIds.EmberseerGrowingTrigger) == 10)
				Talk(TextIds.EmoteTenStack);

			if (Me.GetAuraCount(SpellIds.EmberseerGrowingTrigger) == 20)
			{
				Me.RemoveAura(SpellIds.FreezeAnim);
				Me.CastSpell(Me, SpellIds.EmberseerFullStrength);
				Talk(TextIds.EmoteFreeOfBonds);
				Talk(TextIds.YellFreeOfBonds);
				Me.RemoveUnitFlag(UnitFlags.Uninteractible);
				Me.SetImmuneToPC(false);
				Scheduler.Schedule(TimeSpan.FromSeconds(2), task => { AttackStart(Me.SelectNearestPlayer(30.0f)); });
			}
		}
	}

	public override void UpdateAI(uint diff)
	{
		Scheduler.Update(diff);

		if (!UpdateVictim())
			return;

		DoMeleeAttackIfReady();
	}

	private void UpdateRunes(GameObjectState state)
	{
		// update all runes
		var rune1 = ObjectAccessor.GetGameObject(Me, Instance.GetGuidData(GameObjectsIds.EmberseerRune1));

		if (rune1)
			rune1.SetGoState(state);

		var rune2 = ObjectAccessor.GetGameObject(Me, Instance.GetGuidData(GameObjectsIds.EmberseerRune2));

		if (rune2)
			rune2.SetGoState(state);

		var rune3 = ObjectAccessor.GetGameObject(Me, Instance.GetGuidData(GameObjectsIds.EmberseerRune3));

		if (rune3)
			rune3.SetGoState(state);

		var rune4 = ObjectAccessor.GetGameObject(Me, Instance.GetGuidData(GameObjectsIds.EmberseerRune4));

		if (rune4)
			rune4.SetGoState(state);

		var rune5 = ObjectAccessor.GetGameObject(Me, Instance.GetGuidData(GameObjectsIds.EmberseerRune5));

		if (rune5)
			rune5.SetGoState(state);

		var rune6 = ObjectAccessor.GetGameObject(Me, Instance.GetGuidData(GameObjectsIds.EmberseerRune6));

		if (rune6)
			rune6.SetGoState(state);

		var rune7 = ObjectAccessor.GetGameObject(Me, Instance.GetGuidData(GameObjectsIds.EmberseerRune7));

		if (rune7)
			rune7.SetGoState(state);
	}
}

[Script]
internal class npc_blackhand_incarcerator : ScriptedAI
{
	public npc_blackhand_incarcerator(Creature creature) : base(creature) { }

	public override void JustAppeared()
	{
		DoCast(SpellIds.EncageEmberseer);
	}

	public override void JustEngagedWith(Unit who)
	{
		// Had to do this because CallForHelp will ignore any npcs without Los
		var creatureList = Me.GetCreatureListWithEntryInGrid(CreaturesIds.BlackhandIncarcerator, 60.0f);

		foreach (var creature in creatureList)
			if (creature)
				DoZoneInCombat(creature); // GetAI().AttackStart(me.Victim);

		Scheduler.Schedule(TimeSpan.FromSeconds(8),
							TimeSpan.FromSeconds(16),
							task =>
							{
								DoCastVictim(SpellIds.Strike, new CastSpellExtraArgs(true));
								task.Repeat(TimeSpan.FromSeconds(14), TimeSpan.FromSeconds(23));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(10),
							TimeSpan.FromSeconds(20),
							task =>
							{
								DoCast(SelectTarget(SelectTargetMethod.Random, 0, 100, true), SpellIds.Encage, new CastSpellExtraArgs(true));
								task.Repeat(TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(12));
							});
	}

	public override void JustReachedHome()
	{
		DoCast(SpellIds.EncageEmberseer);

		Me.SetImmuneToAll(true);
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		Scheduler.Update(diff, () => DoMeleeAttackIfReady());
	}
}