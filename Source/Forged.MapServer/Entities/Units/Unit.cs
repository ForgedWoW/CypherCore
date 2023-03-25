// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.Chrono;
using Forged.MapServer.Combat;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Groups;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Maps.Workers;
using Forged.MapServer.Movement;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.BattleGround;
using Forged.MapServer.Networking.Packets.Chat;
using Forged.MapServer.Networking.Packets.Combat;
using Forged.MapServer.Networking.Packets.CombatLog;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Forged.MapServer.Scripting.Interfaces.IUnit;
using Forged.MapServer.Server;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Forged.MapServer.Text;
using Framework.Constants;

namespace Forged.MapServer.Entities.Units;

public partial class Unit : WorldObject
{
	public object SendLock = new();
	static readonly TimeSpan _despawnTime = TimeSpan.FromSeconds(2);
	private readonly object _healthLock = new();

	public bool IsInDisallowedMountForm => IsDisallowedMountForm(TransformSpell, ShapeshiftForm, DisplayId);

	public virtual bool IsLoading => false;

	public bool IsDuringRemoveFromWorld => _duringRemoveFromWorld;

	//SharedVision
	public bool HasSharedVision => !_sharedVision.Empty();

	public NPCFlags NpcFlags => (NPCFlags)UnitData.NpcFlags[0];

	public NPCFlags2 NpcFlags2 => (NPCFlags2)UnitData.NpcFlags[1];

	public bool IsVendor => HasNpcFlag(NPCFlags.Vendor);

	public bool IsTrainer => HasNpcFlag(NPCFlags.Trainer);

	public bool IsQuestGiver => HasNpcFlag(NPCFlags.QuestGiver);

	public bool IsGossip => HasNpcFlag(NPCFlags.Gossip);

	public bool IsTaxi => HasNpcFlag(NPCFlags.FlightMaster);

	public bool IsGuildMaster => HasNpcFlag(NPCFlags.Petitioner);

	public bool IsBattleMaster => HasNpcFlag(NPCFlags.BattleMaster);

	public bool IsBanker => HasNpcFlag(NPCFlags.Banker);

	public bool IsInnkeeper => HasNpcFlag(NPCFlags.Innkeeper);

	public bool IsSpiritHealer => HasNpcFlag(NPCFlags.SpiritHealer);

	public bool IsSpiritGuide => HasNpcFlag(NPCFlags.SpiritGuide);

	public bool IsTabardDesigner => HasNpcFlag(NPCFlags.TabardDesigner);

	public bool IsAuctioner => HasNpcFlag(NPCFlags.Auctioneer);

	public bool IsArmorer => HasNpcFlag(NPCFlags.Repair);

	public bool IsWildBattlePet => HasNpcFlag(NPCFlags.WildBattlePet);

	public bool IsServiceProvider => HasNpcFlag(NPCFlags.Vendor |
												NPCFlags.Trainer |
												NPCFlags.FlightMaster |
												NPCFlags.Petitioner |
												NPCFlags.BattleMaster |
												NPCFlags.Banker |
												NPCFlags.Innkeeper |
												NPCFlags.SpiritHealer |
												NPCFlags.SpiritGuide |
												NPCFlags.TabardDesigner |
												NPCFlags.Auctioneer);

	public bool IsSpiritService => HasNpcFlag(NPCFlags.SpiritHealer | NPCFlags.SpiritGuide);

	public bool IsCritter => CreatureType == CreatureType.Critter;

	public bool IsInFlight => HasUnitState(UnitState.InFlight);

	public override float CollisionHeight
	{
		get
		{
			var scaleMod = ObjectScale; // 99% sure about this

			if (IsMounted)
			{
				var mountDisplayInfo = CliDB.CreatureDisplayInfoStorage.LookupByKey(MountDisplayId);

				if (mountDisplayInfo != null)
				{
					var mountModelData = CliDB.CreatureModelDataStorage.LookupByKey(mountDisplayInfo.ModelID);

					if (mountModelData != null)
					{
						var displayInfo = CliDB.CreatureDisplayInfoStorage.LookupByKey(NativeDisplayId);
						var modelData = CliDB.CreatureModelDataStorage.LookupByKey(displayInfo.ModelID);
						var collisionHeight = scaleMod * ((mountModelData.MountHeight * mountDisplayInfo.CreatureModelScale) + (modelData.CollisionHeight * modelData.ModelScale * displayInfo.CreatureModelScale * 0.5f));

						return collisionHeight == 0.0f ? MapConst.DefaultCollesionHeight : collisionHeight;
					}
				}
			}

			//! Dismounting case - use basic default model data
			var defaultDisplayInfo = CliDB.CreatureDisplayInfoStorage.LookupByKey(NativeDisplayId);
			var defaultModelData = CliDB.CreatureModelDataStorage.LookupByKey(defaultDisplayInfo.ModelID);

			var collisionHeight1 = scaleMod * defaultModelData.CollisionHeight * defaultModelData.ModelScale * defaultDisplayInfo.CreatureModelScale;

			return collisionHeight1 == 0.0f ? MapConst.DefaultCollesionHeight : collisionHeight1;
		}
	}

	public bool IsAIEnabled => Ai != null;

	public bool IsPossessedByPlayer => HasUnitState(UnitState.Possessed) && CharmerGUID.IsPlayer;

	public bool IsPossessing
	{
		get
		{
			var u = Charmed;

			if (u != null)
				return u.IsPossessed;
			else
				return false;
		}
	}

	public bool IsCharmed => !CharmerGUID.IsEmpty;

	public bool IsPossessed => HasUnitState(UnitState.Possessed);

	public bool IsMagnet
	{
		get
		{
			// Grounding Totem
			if (UnitData.CreatedBySpell == 8177) /// @todo: find a more generic solution
				return true;

			return false;
		}
	}

	public bool IsInFeralForm
	{
		get
		{
			var form = ShapeshiftForm;

			return form == ShapeShiftForm.CatForm || form == ShapeShiftForm.BearForm || form == ShapeShiftForm.DireBearForm || form == ShapeShiftForm.GhostWolf;
		}
	}

	public bool IsControlledByPlayer => ControlledByPlayer;

	public bool IsCharmedOwnedByPlayerOrPlayer => CharmerOrOwnerOrOwnGUID.IsPlayer;

	public uint CreatureTypeMask
	{
		get
		{
			var creatureType = (uint)CreatureType;

			return (uint)(creatureType >= 1 ? (1 << (int)(creatureType - 1)) : 0);
		}
	}

	public MotionMaster MotionMaster => _motionMaster;

	public override ushort AIAnimKitId => _aiAnimKitId;

	public override ushort MovementAnimKitId => _movementAnimKitId;

	public override ushort MeleeAnimKitId => _meleeAnimKitId;

	public uint Level => UnitData.Level;

	public Race Race
	{
		get => (Race)(byte)UnitData.Race;
		set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Race), (byte)value);
	}


	public PlayerClass Class
	{
		get => (PlayerClass)(byte)UnitData.ClassId;
		set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ClassId), (byte)value);
	}

	public uint ClassMask => (uint)(1 << ((int)Class - 1));

	public Gender Gender
	{
		get => (Gender)(byte)UnitData.Sex;
		set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Sex), (byte)value);
	}

	public virtual Gender NativeGender
	{
		get => Gender;
		set => Gender = value;
	}

	public virtual float NativeObjectScale => 1.0f;

	public uint DisplayId => UnitData.DisplayID;

	public uint NativeDisplayId => UnitData.NativeDisplayID;

	public float NativeDisplayScale => UnitData.NativeXDisplayScale;

	public bool IsMounted => HasUnitFlag(UnitFlags.Mount);

	public uint MountDisplayId
	{
		get => UnitData.MountDisplayID;
		set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.MountDisplayID), value);
	}

	public virtual float FollowAngle => MathFunctions.PiOver2;

	public override ObjectGuid OwnerGUID => UnitData.SummonedBy;

	public ObjectGuid CreatorGUID => UnitData.CreatedBy;

	public ObjectGuid MinionGUID
	{
		get => UnitData.Summon;
		set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Summon), value);
	}

	public ObjectGuid PetGUID
	{
		get => SummonSlot[0];
		set => SummonSlot[0] = value;
	}

	public ObjectGuid CritterGUID
	{
		get => UnitData.Critter;
		set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Critter), value);
	}

	public ObjectGuid BattlePetCompanionGUID
	{
		get => UnitData.BattlePetCompanionGUID;
		set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.BattlePetCompanionGUID), value);
	}

	public ObjectGuid DemonCreatorGUID
	{
		get => UnitData.DemonCreator;
		set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.DemonCreator), value);
	}

	public ObjectGuid CharmerGUID => UnitData.CharmedBy;

	public Unit Charmer => _charmer;

	public ObjectGuid CharmedGUID => UnitData.Charm;

	public Unit Charmed => _charmed;

	public override ObjectGuid CharmerOrOwnerGUID => IsCharmed ? CharmerGUID : OwnerGUID;

	public override Unit CharmerOrOwner => IsCharmed ? Charmer : OwnerUnit;

	public uint BattlePetCompanionNameTimestamp
	{
		get => UnitData.BattlePetCompanionNameTimestamp;
		set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.BattlePetCompanionNameTimestamp), value);
	}

	public uint BattlePetCompanionExperience
	{
		get => UnitData.BattlePetCompanionExperience;
		set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.BattlePetCompanionExperience), value);
	}

	public uint WildBattlePetLevel
	{
		get => UnitData.WildBattlePetLevel;
		set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.WildBattlePetLevel), value);
	}

	public UnitDynFlags DynamicFlags => (UnitDynFlags)(uint)ObjectData.DynamicFlags;

	public Emote EmoteState
	{
		get => (Emote)(int)UnitData.EmoteState;
		set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.EmoteState), (int)value);
	}

	public SheathState Sheath
	{
		get => (SheathState)(byte)UnitData.SheatheState;
		set
		{
			SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.SheatheState), (byte)value);

			if (value == SheathState.Unarmed)
				RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Sheathing);
		}
	}

	public UnitPVPStateFlags PvpFlags => (UnitPVPStateFlags)(byte)UnitData.PvpFlags;

	public bool IsInSanctuary => HasPvpFlag(UnitPVPStateFlags.Sanctuary);

	public bool IsPvP => HasPvpFlag(UnitPVPStateFlags.PvP);

	public bool IsFFAPvP => HasPvpFlag(UnitPVPStateFlags.FFAPvp);

	public override float ObjectScale
	{
		get => base.ObjectScale;
		set
		{
			var minfo = Global.ObjectMgr.GetCreatureModelInfo(DisplayId);

			if (minfo != null)
			{
				BoundingRadius = (IsPet ? 1.0f : minfo.BoundingRadius) * ObjectScale;
				SetCombatReach((IsPet ? SharedConst.DefaultPlayerCombatReach : minfo.CombatReach) * ObjectScale);
			}

			base.ObjectScale = value;
		}
	}

	public UnitPetFlags PetFlags => (UnitPetFlags)(byte)UnitData.PetFlags;

	public ShapeShiftForm ShapeshiftForm
	{
		get => (ShapeShiftForm)(byte)UnitData.ShapeshiftForm;
		set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ShapeshiftForm), (byte)value);
	}

	public CreatureType CreatureType
	{
		get
		{
			if (IsTypeId(TypeId.Player))
			{
				var form = ShapeshiftForm;
				var ssEntry = CliDB.SpellShapeshiftFormStorage.LookupByKey((uint)form);

				if (ssEntry is { CreatureType: > 0 })
				{
					return (CreatureType)ssEntry.CreatureType;
				}
				else
				{
					var raceEntry = CliDB.ChrRacesStorage.LookupByKey(Race);

					return (CreatureType)raceEntry.CreatureType;
				}
			}
			else
			{
				return AsCreature.Template.CreatureType;
			}
		}
	}

	public bool IsAlive => DeathState == DeathState.Alive;

	public bool IsDying => DeathState == DeathState.JustDied;

	public bool IsDead => (DeathState == DeathState.Dead || DeathState == DeathState.Corpse);

	public bool IsSummon => UnitTypeMask.HasAnyFlag(UnitTypeMask.Summon);

	public bool IsGuardian => UnitTypeMask.HasAnyFlag(UnitTypeMask.Guardian);

	public bool IsPet => UnitTypeMask.HasAnyFlag(UnitTypeMask.Pet);

	public bool IsHunterPet => UnitTypeMask.HasAnyFlag(UnitTypeMask.HunterPet);

	public bool IsTotem => UnitTypeMask.HasAnyFlag(UnitTypeMask.Totem);

	public bool IsVehicle => UnitTypeMask.HasAnyFlag(UnitTypeMask.Vehicle);

	public override uint Faction
	{
		get => UnitData.FactionTemplate;
		set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.FactionTemplate), value);
	}

	public UnitStandStateType StandState => (UnitStandStateType)(byte)UnitData.StandState;

	public bool IsSitState
	{
		get
		{
			var s = StandState;

			return
				s == UnitStandStateType.SitChair ||
				s == UnitStandStateType.SitLowChair ||
				s == UnitStandStateType.SitMediumChair ||
				s == UnitStandStateType.SitHighChair ||
				s == UnitStandStateType.Sit;
		}
	}

	public bool IsStandState
	{
		get
		{
			var s = StandState;

			return !IsSitState && s != UnitStandStateType.Sleep && s != UnitStandStateType.Kneel;
		}
	}

	public AnimTier AnimTier => (AnimTier)(byte)UnitData.AnimTier;

	public uint ChannelSpellId
	{
		get => ((UnitChannel)UnitData.ChannelData).SpellID;
		set => SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.ChannelData).Value.SpellID, value);
	}

	public uint ChannelSpellXSpellVisualId => UnitData.ChannelData.GetValue().SpellVisual.SpellXSpellVisualID;

	public uint ChannelScriptVisualId => UnitData.ChannelData.GetValue().SpellVisual.ScriptVisualID;

	public Pet AsPet => this as Pet;

	public uint TransformSpell
	{
		get => _transformSpell;
		set => _transformSpell = value;
	}

	public Vehicle VehicleKit1 => VehicleKit;

	public Vehicle Vehicle1
	{
		get => Vehicle;
		set => Vehicle = value;
	}

	public Unit VehicleBase => Vehicle != null ? Vehicle.GetBase() : null;

	public Creature VehicleCreatureBase
	{
		get
		{
			var veh = VehicleBase;

			if (veh != null)
			{
				var c = veh.AsCreature;

				if (c != null)
					return c;
			}

			return null;
		}
	}

	public ITransport DirectTransport
	{
		get
		{
			var veh = Vehicle1;

			if (veh != null)
				return veh;

			return Transport;
		}
	}

	public virtual IUnitAI AI
	{
		get => Ai;
		set
		{
			PushAI(value);
			RefreshAI();
		}
	}

	public IUnitAI BaseAI => Ai;

	public Unit(bool isWorldObject) : base(isWorldObject)
	{
		MoveSpline = new MoveSpline();
		_motionMaster = new MotionMaster(this);
		_combatManager = new CombatManager(this);
		_threatManager = new ThreatManager(this);
		_spellHistory = new SpellHistory(this);

		ObjectTypeId = TypeId.Unit;
		ObjectTypeMask |= TypeMask.Unit;
		_updateFlag.MovementUpdate = true;

		ModAttackSpeedPct = new double[]
		{
			1.0f, 1.0f, 1.0f
		};

		DeathState = DeathState.Alive;

		for (byte i = 0; i < (int)SpellImmunity.Max; ++i)
			_spellImmune[i] = new MultiMap<uint, uint>();

		for (byte i = 0; i < (int)UnitMods.End; ++i)
		{
			AuraFlatModifiersGroup[i] = new double[(int)UnitModifierFlatType.End];
			AuraFlatModifiersGroup[i][(int)UnitModifierFlatType.Base] = 0.0f;
			AuraFlatModifiersGroup[i][(int)UnitModifierFlatType.BasePCTExcludeCreate] = 100.0f;
			AuraFlatModifiersGroup[i][(int)UnitModifierFlatType.Total] = 0.0f;

			AuraPctModifiersGroup[i] = new double[(int)UnitModifierPctType.End];
			AuraPctModifiersGroup[i][(int)UnitModifierPctType.Base] = 1.0f;
			AuraPctModifiersGroup[i][(int)UnitModifierPctType.Total] = 1.0f;
		}

		AuraPctModifiersGroup[(int)UnitMods.DamageOffHand][(int)UnitModifierPctType.Total] = 0.5f;

		for (byte i = 0; i < (int)WeaponAttackType.Max; ++i)
			WeaponDamage[i] = new double[]
			{
				1.0f, 2.0f
			};

		if (IsTypeId(TypeId.Player))
		{
			ModMeleeHitChance = 7.5f;
			ModRangedHitChance = 7.5f;
			ModSpellHitChance = 15.0f;
		}

		BaseSpellCritChance = 5;

		for (byte i = 0; i < (int)UnitMoveType.Max; ++i)
			SpeedRate[i] = 1.0f;

		ServerSideVisibility.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Alive);

		_splineSyncTimer = new TimeTracker();

		UnitData = new UnitData();
	}

	public override void Dispose()
	{
		// set current spells as deletable
		for (CurrentSpellTypes i = 0; i < CurrentSpellTypes.Max; ++i)
			if (CurrentSpells.ContainsKey(i))
				if (CurrentSpells[i] != null)
				{
					CurrentSpells[i].SetReferencedFromCurrent(false);
					CurrentSpells[i] = null;
				}

		Events.KillAllEvents(true);

		_DeleteRemovedAuras();

		//i_motionMaster = null;
		_charmInfo = null;
		MoveSpline = null;
		_spellHistory = null;

		/*ASSERT(!m_duringRemoveFromWorld);
		ASSERT(!m_attacking);
		ASSERT(m_attackers.empty());
		ASSERT(m_sharedVision.empty());
		ASSERT(m_Controlled.empty());
		ASSERT(m_appliedAuras.empty());
		ASSERT(m_ownedAuras.empty());
		ASSERT(m_removedAuras.empty());
		ASSERT(m_gameObj.empty());
		ASSERT(m_dynObj.empty());*/

		base.Dispose();
	}

	public override void Update(uint diff)
	{
		// WARNING! Order of execution here is important, do not change.
		// Spells must be processed with event system BEFORE they go to _UpdateSpells.
		base.Update(diff);

		if (!IsInWorld)
			return;

		_UpdateSpells(diff);

		// If this is set during update SetCantProc(false) call is missing somewhere in the code
		// Having this would prevent spells from being proced, so let's crash

		_combatManager.Update(diff);

		_lastDamagedTargetGuid = ObjectGuid.Empty;

		if (_lastExtraAttackSpell != 0)
		{
			while (!_extraAttacksTargets.Empty())
			{
				var (targetGuid, count) = _extraAttacksTargets.FirstOrDefault();
				_extraAttacksTargets.Remove(targetGuid);

				var victim = Global.ObjAccessor.GetUnit(this, targetGuid);

				if (victim != null)
					HandleProcExtraAttackFor(victim, count);
			}

			_lastExtraAttackSpell = 0;
		}

		bool spellPausesCombatTimer(CurrentSpellTypes type)
		{
			return GetCurrentSpell(type) != null && GetCurrentSpell(type).SpellInfo.HasAttribute(SpellAttr6.DelayCombatTimerDuringCast);
		}

		if (!spellPausesCombatTimer(CurrentSpellTypes.Generic) && !spellPausesCombatTimer(CurrentSpellTypes.Channeled))
		{
			var base_att = GetAttackTimer(WeaponAttackType.BaseAttack);

			if (base_att != 0)
				SetAttackTimer(WeaponAttackType.BaseAttack, (diff >= base_att ? 0 : base_att - diff));

			var ranged_att = GetAttackTimer(WeaponAttackType.RangedAttack);

			if (ranged_att != 0)
				SetAttackTimer(WeaponAttackType.RangedAttack, (diff >= ranged_att ? 0 : ranged_att - diff));

			var off_att = GetAttackTimer(WeaponAttackType.OffAttack);

			if (off_att != 0)
				SetAttackTimer(WeaponAttackType.OffAttack, (diff >= off_att ? 0 : off_att - diff));
		}

		// update abilities available only for fraction of time
		UpdateReactives(diff);

		if (IsAlive)
		{
			ModifyAuraState(AuraStateType.Wounded20Percent, HealthBelowPct(20));
			ModifyAuraState(AuraStateType.Wounded25Percent, HealthBelowPct(25));
			ModifyAuraState(AuraStateType.Wounded35Percent, HealthBelowPct(35));
			ModifyAuraState(AuraStateType.WoundHealth20_80, HealthBelowPct(20) || HealthAbovePct(80));
			ModifyAuraState(AuraStateType.Healthy75Percent, HealthAbovePct(75));
			ModifyAuraState(AuraStateType.WoundHealth35_80, HealthBelowPct(35) || HealthAbovePct(80));
		}

		UpdateSplineMovement(diff);

		MotionMaster.Update(diff);

		// Wait with the aura interrupts until we have updated our movement generators and position
		if (IsPlayer)
			InterruptMovementBasedAuras();
		else if (!MoveSpline.Finalized())
			InterruptMovementBasedAuras();

		// All position info based actions have been executed, reset info
		_positionUpdateInfo.Reset();

		if (HasScheduledAIChange() && (!IsPlayer || (IsCharmed && CharmerGUID.IsCreature)))
			UpdateCharmAI();

		RefreshAI();
	}

	public void HandleEmoteCommand(Emote emoteId, Player target = null, uint[] spellVisualKitIds = null, int sequenceVariation = 0)
	{
		EmoteMessage packet = new()
        {
            Guid = GUID,
            EmoteID = (uint)emoteId
        };

        var emotesEntry = CliDB.EmotesStorage.LookupByKey(emoteId);

		if (emotesEntry != null && spellVisualKitIds != null)
			if (emotesEntry.AnimId == (uint)Anim.MountSpecial || emotesEntry.AnimId == (uint)Anim.MountSelfSpecial)
				packet.SpellVisualKitIDs.AddRange(spellVisualKitIds);

		packet.SequenceVariation = sequenceVariation;

		if (target != null)
			target.SendPacket(packet);
		else
			SendMessageToSet(packet, true);
	}

	public void SendDurabilityLoss(Player receiver, uint percent)
	{
		DurabilityDamageDeath packet = new()
        {
            Percent = percent
        };

        receiver.SendPacket(packet);
	}

	public bool IsDisallowedMountForm(uint spellId, ShapeShiftForm form, uint displayId)
	{
		var transformSpellInfo = Global.SpellMgr.GetSpellInfo(spellId, Map.DifficultyID);

		if (transformSpellInfo != null)
			if (transformSpellInfo.HasAttribute(SpellAttr0.AllowWhileMounted))
				return false;

		if (form != 0)
		{
			var shapeshift = CliDB.SpellShapeshiftFormStorage.LookupByKey(form);

			if (shapeshift == null)
				return true;

			if (!shapeshift.Flags.HasAnyFlag(SpellShapeshiftFormFlags.Stance))
				return true;
		}

		if (displayId == NativeDisplayId)
			return false;

		var display = CliDB.CreatureDisplayInfoStorage.LookupByKey(displayId);

		if (display == null)
			return true;

		var displayExtra = CliDB.CreatureDisplayInfoExtraStorage.LookupByKey(display.ExtendedDisplayInfoID);

		if (displayExtra == null)
			return true;

		var model = CliDB.CreatureModelDataStorage.LookupByKey(display.ModelID);
		var race = CliDB.ChrRacesStorage.LookupByKey(displayExtra.DisplayRaceID);

		if (model != null && !model.GetFlags().HasFlag(CreatureModelDataFlags.CanMountWhileTransformedAsThis))
			if (race != null && !race.GetFlags().HasFlag(ChrRacesFlag.CanMount))
				return true;

		return false;
	}

	public void SendClearTarget()
	{
		BreakTarget breakTarget = new()
        {
            UnitGUID = GUID
        };

        SendMessageToSet(breakTarget, false);
	}

	public List<Player> GetSharedVisionList()
	{
		return _sharedVision;
	}

	public void AddPlayerToVision(Player player)
	{
		if (_sharedVision.Empty())
		{
			SetActive(true);
			SetWorldObject(true);
		}

		_sharedVision.Add(player);
	}

	// only called in Player.SetSeer
	public void RemovePlayerFromVision(Player player)
	{
		_sharedVision.Remove(player);

		if (_sharedVision.Empty())
		{
			SetActive(false);
			SetWorldObject(false);
		}
	}

	public virtual void Talk(string text, ChatMsg msgType, Language language, float textRange, WorldObject target)
	{
		var builder = new CustomChatTextBuilder(this, msgType, text, language, target);
		var localizer = new LocalizedDo(builder);
		var worker = new PlayerDistWorker(this, textRange, localizer, GridType.World);
		Cell.VisitGrid(this, worker, textRange);
	}

	public virtual void Say(string text, Language language, WorldObject target = null)
	{
		Talk(text, ChatMsg.MonsterSay, language, WorldConfig.GetFloatValue(WorldCfg.ListenRangeSay), target);
	}

	public virtual void Yell(string text, Language language = Language.Universal, WorldObject target = null)
	{
		Talk(text, ChatMsg.MonsterYell, language, WorldConfig.GetFloatValue(WorldCfg.ListenRangeYell), target);
	}

	public virtual void TextEmote(string text, WorldObject target = null, bool isBossEmote = false)
	{
		Talk(text, isBossEmote ? ChatMsg.RaidBossEmote : ChatMsg.MonsterEmote, Language.Universal, WorldConfig.GetFloatValue(WorldCfg.ListenRangeTextemote), target);
	}

	public virtual void Whisper(string text, Player target, bool isBossWhisper = false)
	{
		Whisper(text, Language.Universal, target, isBossWhisper);
	}

	public virtual void Whisper(string text, Language language, Player target, bool isBossWhisper = false)
	{
		if (!target)
			return;

		var locale = target.Session.SessionDbLocaleIndex;
		ChatPkt data = new();
		data.Initialize(isBossWhisper ? ChatMsg.RaidBossWhisper : ChatMsg.MonsterWhisper, Language.Universal, this, target, text, 0, "", locale);
		target.SendPacket(data);
	}

	public void Talk(uint textId, ChatMsg msgType, float textRange, WorldObject target)
	{
		if (!CliDB.BroadcastTextStorage.ContainsKey(textId))
		{
			Log.Logger.Error("Unit.Talk: `broadcast_text` (Id: {0}) was not found", textId);

			return;
		}

		var builder = new BroadcastTextBuilder(this, msgType, textId, Gender, target);
		var localizer = new LocalizedDo(builder);
		var worker = new PlayerDistWorker(this, textRange, localizer, GridType.World);
		Cell.VisitGrid(this, worker, textRange);
	}

	public virtual void Say(uint textId, WorldObject target = null)
	{
		Talk(textId, ChatMsg.MonsterSay, WorldConfig.GetFloatValue(WorldCfg.ListenRangeSay), target);
	}

	public virtual void Yell(uint textId, WorldObject target = null)
	{
		Talk(textId, ChatMsg.MonsterYell, WorldConfig.GetFloatValue(WorldCfg.ListenRangeYell), target);
	}

	public virtual void TextEmote(uint textId, WorldObject target = null, bool isBossEmote = false)
	{
		Talk(textId, isBossEmote ? ChatMsg.RaidBossEmote : ChatMsg.MonsterEmote, WorldConfig.GetFloatValue(WorldCfg.ListenRangeTextemote), target);
	}

	public virtual void Whisper(uint textId, Player target, bool isBossWhisper = false)
	{
		if (!target)
			return;

		var bct = CliDB.BroadcastTextStorage.LookupByKey(textId);

		if (bct == null)
		{
			Log.Logger.Error("Unit.Whisper: `broadcast_text` was not {0} found", textId);

			return;
		}

		var locale = target.Session.SessionDbLocaleIndex;
		ChatPkt data = new();
		data.Initialize(isBossWhisper ? ChatMsg.RaidBossWhisper : ChatMsg.MonsterWhisper, Language.Universal, this, target, Global.DB2Mgr.GetBroadcastTextValue(bct, locale, Gender), 0, "", locale);
		target.SendPacket(data);
	}

	public override void UpdateObjectVisibility(bool forced = true)
	{
		if (!forced)
		{
			AddToNotify(NotifyFlags.VisibilityChanged);
		}
		else
		{
			base.UpdateObjectVisibility(true);
			// call MoveInLineOfSight for nearby creatures
			AIRelocationNotifier notifier = new(this, GridType.All);
			Cell.VisitGrid(this, notifier, VisibilityRange);
		}
	}

	public override void AddToWorld()
	{
		base.AddToWorld();
		_motionMaster.AddToWorld();

		RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.EnterWorld);
	}

	public override void RemoveFromWorld()
	{
		// cleanup

		if (IsInWorld)
		{
			_duringRemoveFromWorld = true;
			var ai = AI;

			if (ai != null)
				ai.OnDespawn();

			if (IsVehicle)
				RemoveVehicleKit(true);

			RemoveCharmAuras();
			RemoveAurasByType(AuraType.BindSight);
			RemoveNotOwnSingleTargetAuras();
			RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.LeaveWorld);

			RemoveAllGameObjects();
			RemoveAllDynObjects();
			RemoveAllAreaTriggers();

			ExitVehicle(); // Remove applied auras with SPELL_AURA_CONTROL_VEHICLE
			UnsummonAllTotems();
			RemoveAllControlled();

			RemoveAreaAurasDueToLeaveWorld();

			RemoveAllFollowers();

			if (IsCharmed)
				RemoveCharmedBy(null);

			var owner = OwnerUnit;

			if (owner != null)
				if (owner.Controlled.Contains(this))
					Log.Logger.Fatal("Unit {0} is in controlled list of {1} when removed from world", Entry, owner.Entry);

			base.RemoveFromWorld();
			_duringRemoveFromWorld = false;
		}
	}

	public void CleanupBeforeRemoveFromMap(bool finalCleanup)
	{
		// This needs to be before RemoveFromWorld to make GetCaster() return a valid for aura removal
		InterruptNonMeleeSpells(true);

		if (IsInWorld)
			RemoveFromWorld();

		// A unit may be in removelist and not in world, but it is still in grid
		// and may have some references during delete
		RemoveAllAuras();
		RemoveAllGameObjects();

		if (finalCleanup)
			_cleanupDone = true;

		CombatStop();
	}

	public override void CleanupsBeforeDelete(bool finalCleanup = true)
	{
		CleanupBeforeRemoveFromMap(finalCleanup);

		base.CleanupsBeforeDelete(finalCleanup);
	}

	public void _RegisterDynObject(DynamicObject dynObj)
	{
		DynamicObjects.Add(dynObj);

		if (IsTypeId(TypeId.Unit) && IsAIEnabled)
			AsCreature.AI.JustRegisteredDynObject(dynObj);
	}

	public void _UnregisterDynObject(DynamicObject dynObj)
	{
		DynamicObjects.Remove(dynObj);

		if (IsTypeId(TypeId.Unit) && IsAIEnabled)
			AsCreature.AI.JustUnregisteredDynObject(dynObj);
	}

	public DynamicObject GetDynObject(uint spellId)
	{
		return GetDynObjects(spellId).FirstOrDefault();
	}

	public void RemoveDynObject(uint spellId)
	{
		for (var i = 0; i < DynamicObjects.Count; ++i)
		{
			var dynObj = DynamicObjects[i];

			if (dynObj.GetSpellId() == spellId)
				dynObj.Remove();
		}
	}

	public void RemoveAllDynObjects()
	{
		while (!DynamicObjects.Empty())
			DynamicObjects.First().Remove();
	}

	public GameObject GetGameObject(uint spellId)
	{
		return GetGameObjects(spellId).FirstOrDefault();
	}

	public void AddGameObject(GameObject gameObj)
	{
		if (gameObj == null || !gameObj.OwnerGUID.IsEmpty)
			return;

		GameObjects.Add(gameObj);
		gameObj.SetOwnerGUID(GUID);

		if (gameObj.SpellId != 0)
		{
			var createBySpell = Global.SpellMgr.GetSpellInfo(gameObj.SpellId, Map.DifficultyID);

			// Need disable spell use for owner
			if (createBySpell != null && createBySpell.IsCooldownStartedOnEvent)
				// note: item based cooldowns and cooldown spell mods with charges ignored (unknown existing cases)
				SpellHistory.StartCooldown(createBySpell, 0, null, true);
		}

		if (IsTypeId(TypeId.Unit) && AsCreature.IsAIEnabled)
			AsCreature.AI.JustSummonedGameobject(gameObj);
	}

	public void RemoveGameObject(GameObject gameObj, bool del)
	{
		if (gameObj == null || gameObj.OwnerGUID != GUID)
			return;

		gameObj.SetOwnerGUID(ObjectGuid.Empty);

		for (byte i = 0; i < SharedConst.MaxGameObjectSlot; ++i)
			if (ObjectSlot[i] == gameObj.GUID)
			{
				ObjectSlot[i].Clear();

				break;
			}

		// GO created by some spell
		var spellid = gameObj.SpellId;

		if (spellid != 0)
		{
			RemoveAura(spellid);

			var createBySpell = Global.SpellMgr.GetSpellInfo(spellid, Map.DifficultyID);

			// Need activate spell use for owner
			if (createBySpell != null && createBySpell.IsCooldownStartedOnEvent)
				// note: item based cooldowns and cooldown spell mods with charges ignored (unknown existing cases)
				SpellHistory.SendCooldownEvent(createBySpell);
		}

		GameObjects.Remove(gameObj);

		if (IsTypeId(TypeId.Unit) && AsCreature.IsAIEnabled)
			AsCreature.AI.SummonedGameobjectDespawn(gameObj);

		if (del)
		{
			gameObj.SetRespawnTime(0);
			gameObj.Delete();
		}
	}

	public void RemoveGameObject(uint spellid, bool del)
	{
		if (GameObjects.Empty())
			return;

		for (var i = 0; i < GameObjects.Count; ++i)
		{
			var obj = GameObjects[i];

			if (spellid == 0 || obj.SpellId == spellid)
			{
				obj.SetOwnerGUID(ObjectGuid.Empty);

				if (del)
				{
					obj.SetRespawnTime(0);
					obj.Delete();
				}

				GameObjects.Remove(obj);
			}
		}
	}

	public void RemoveAllGameObjects()
	{
		// remove references to unit
		while (!GameObjects.Empty())
		{
			var obj = GameObjects.First();
			obj.SetOwnerGUID(ObjectGuid.Empty);
			obj.SetRespawnTime(0);
			obj.Delete();
			GameObjects.Remove(obj);
		}
	}

	public void _RegisterAreaTrigger(AreaTrigger areaTrigger)
	{
		_areaTrigger.Add(areaTrigger);

		if (IsTypeId(TypeId.Unit) && IsAIEnabled)
			AsCreature.AI.JustRegisteredAreaTrigger(areaTrigger);
	}

	public void _UnregisterAreaTrigger(AreaTrigger areaTrigger)
	{
		_areaTrigger.Remove(areaTrigger);

		if (IsTypeId(TypeId.Unit) && IsAIEnabled)
			AsCreature.AI.JustUnregisteredAreaTrigger(areaTrigger);
	}

	public AreaTrigger GetAreaTrigger(uint spellId)
	{
		var areaTriggers = GetAreaTriggers(spellId);

		return areaTriggers.Empty() ? null : areaTriggers[0];
	}

	public List<AreaTrigger> GetAreaTriggers(uint spellId)
	{
		return _areaTrigger.Where(trigger => trigger.SpellId == spellId).ToList();
	}

	public void RemoveAreaTrigger(uint spellId)
	{
		if (_areaTrigger.Empty())
			return;

		for (var i = 0; i < _areaTrigger.Count; ++i)
		{
			var areaTrigger = _areaTrigger[i];

			if (areaTrigger.SpellId == spellId)
				areaTrigger.Remove();
		}
	}

	public void RemoveAreaTrigger(AuraEffect aurEff)
	{
		if (_areaTrigger.Empty())
			return;

		foreach (var areaTrigger in _areaTrigger)
			if (areaTrigger.AuraEff == aurEff)
			{
				areaTrigger.Remove();

				break; // There can only be one AreaTrigger per AuraEffect
			}
	}

	public void RemoveAllAreaTriggers()
	{
		while (!_areaTrigger.Empty())
			_areaTrigger[0].Remove();
	}

	public bool HasNpcFlag(NPCFlags flags)
	{
		return (UnitData.NpcFlags[0] & (uint)flags) != 0;
	}

	public void SetNpcFlag(NPCFlags flags)
	{
		SetUpdateFieldFlagValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.NpcFlags, 0), (uint)flags);
	}

	public void RemoveNpcFlag(NPCFlags flags)
	{
		RemoveUpdateFieldFlagValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.NpcFlags, 0), (uint)flags);
	}

	public void ReplaceAllNpcFlags(NPCFlags flags)
	{
		SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.NpcFlags, 0), (uint)flags);
	}

	public bool HasNpcFlag2(NPCFlags2 flags)
	{
		return (UnitData.NpcFlags[1] & (uint)flags) != 0;
	}

	public void SetNpcFlag2(NPCFlags2 flags)
	{
		SetUpdateFieldFlagValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.NpcFlags, 1), (uint)flags);
	}

	public void RemoveNpcFlag2(NPCFlags2 flags)
	{
		RemoveUpdateFieldFlagValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.NpcFlags, 1), (uint)flags);
	}

	public void ReplaceAllNpcFlags2(NPCFlags2 flags)
	{
		SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.NpcFlags, 1), (uint)flags);
	}

	public bool IsContestedGuard()
	{
		var entry = GetFactionTemplateEntry();

		if (entry != null)
			return entry.IsContestedGuardFaction();

		return false;
	}

	public void SetHoverHeight(float hoverHeight)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.HoverHeight), hoverHeight);
	}

	public override string GetDebugInfo()
	{
		var str = $"{base.GetDebugInfo()}\nIsAIEnabled: {IsAIEnabled} DeathState: {DeathState} UnitMovementFlags: {GetUnitMovementFlags()} UnitMovementFlags2: {GetUnitMovementFlags2()} Class: {Class}\n" +
				$" {(MoveSpline != null ? MoveSpline.ToString() : "Movespline: <none>\n")} GetCharmedGUID(): {CharmedGUID}\nGetCharmerGUID(): {CharmerGUID}\n{(VehicleKit1 != null ? VehicleKit1.GetDebugInfo() : "No vehicle kit")}\n" +
				$"m_Controlled size: {Controlled.Count}";

		var controlledCount = 0;

		foreach (var controlled in Controlled)
		{
			++controlledCount;
			str += $"\nm_Controlled {controlledCount} : {controlled.GUID}";
		}

		return str;
	}

	public Guardian GetGuardianPet()
	{
		var pet_guid = PetGUID;

		if (!pet_guid.IsEmpty)
		{
			var pet = ObjectAccessor.GetCreatureOrPetOrVehicle(this, pet_guid);

			if (pet != null)
				if (pet.HasUnitTypeMask(UnitTypeMask.Guardian))
					return (Guardian)pet;

			Log.Logger.Fatal("Unit:GetGuardianPet: Guardian {0} not exist.", pet_guid);
			PetGUID = ObjectGuid.Empty;
		}

		return null;
	}

	public Creature GetSummonedCreatureByEntry(uint entry)
	{
		foreach (var sum in SummonSlot)
		{
			var cre = ObjectAccessor.GetCreature(this, sum);

			if (cre.Entry == entry)
				return cre;
		}

		return null;
	}


	public Unit SelectNearbyTarget(Unit exclude = null, float dist = SharedConst.NominalMeleeRange)
	{
		bool AddUnit(Unit u)
		{
			if (Victim == u)
				return false;

			if (exclude == u)
				return false;

			// remove not LoS targets
			if (!IsWithinLOSInMap(u) || u.IsTotem || u.IsSpiritService || u.IsCritter)
				return false;

			return true;
		}

		List<Unit> targets = new();
		var u_check = new AnyUnfriendlyUnitInObjectRangeCheck(this, this, dist, AddUnit);
		var searcher = new UnitListSearcher(this, targets, u_check, GridType.All);
		Cell.VisitGrid(this, searcher, dist);

		// no appropriate targets
		if (targets.Empty())
			return null;

		// select random
		return targets.SelectRandom();
	}

	public Unit SelectNearbyAllyUnit(List<Unit> exclude, float dist = SharedConst.NominalMeleeRange)
	{
		List<Unit> targets = new();
		var u_check = new AnyFriendlyUnitInObjectRangeCheck(this, this, dist);
		var searcher = new UnitListSearcher(this, targets, u_check, GridType.All);
		Cell.VisitGrid(this, searcher, dist);

		// no appropriate targets
		targets.RemoveAll(k => exclude.Contains(k));

		if (targets.Empty())
			return null;

		return targets.SelectRandom();
	}

	public void EnterVehicle(Unit baseUnit, sbyte seatId = -1)
	{
		CastSpellExtraArgs args = new(TriggerCastFlags.IgnoreCasterMountedOrOnVehicle);
		args.AddSpellMod(SpellValueMod.BasePoint0, seatId + 1);
		CastSpell(baseUnit, SharedConst.VehicleSpellRideHardcoded, args);
	}

	public void _EnterVehicle(Vehicle vehicle, sbyte seatId, AuraApplication aurApp)
	{
		if (!IsAlive || VehicleKit1 == vehicle || vehicle.GetBase().IsOnVehicle(this))
			return;

		if (Vehicle != null)
		{
			if (Vehicle != vehicle)
			{
				Log.Logger.Debug("EnterVehicle: {0} exit {1} and enter {2}.", Entry, Vehicle.GetBase().Entry, vehicle.GetBase().Entry);
				ExitVehicle();
			}
			else if (seatId >= 0 && seatId == TransSeat)
			{
				return;
			}
			else
			{
				//Exit the current vehicle because unit will reenter in a new seat.
				Vehicle.GetBase().RemoveAurasByType(AuraType.ControlVehicle, GUID, aurApp.Base);
			}
		}

		if (aurApp.HasRemoveMode)
			return;

		var player = AsPlayer;

		if (player != null)
		{
			if (vehicle.GetBase().IsTypeId(TypeId.Player) && player.IsInCombat)
			{
				vehicle.GetBase().RemoveAura(aurApp);

				return;
			}

			if (vehicle.GetBase().IsCreature)
			{
				// If a player entered a vehicle that is part of a formation, remove it from said formation
				var creatureGroup = vehicle.GetBase().AsCreature.Formation;

				if (creatureGroup != null)
					creatureGroup.RemoveMember(vehicle.GetBase().AsCreature);
			}
		}

		vehicle.AddVehiclePassenger(this, seatId);
	}

	public void ChangeSeat(sbyte seatId, bool next = true)
	{
		if (Vehicle == null)
			return;

		// Don't change if current and new seat are identical
		if (seatId == TransSeat)
			return;

		var seat = (seatId < 0 ? Vehicle.GetNextEmptySeat(TransSeat, next) : Vehicle.Seats.LookupByKey(seatId));

		// The second part of the check will only return true if seatId >= 0. @Vehicle.GetNextEmptySeat makes sure of that.
		if (seat == null || !seat.IsEmpty())
			return;

		AuraEffect rideVehicleEffect = null;
		var vehicleAuras = Vehicle.GetBase().GetAuraEffectsByType(AuraType.ControlVehicle);

		foreach (var eff in vehicleAuras)
		{
			if (eff.CasterGuid != GUID)
				continue;

			rideVehicleEffect = eff;
		}

		rideVehicleEffect.ChangeAmount((seatId < 0 ? TransSeat : seatId) + 1);
	}

	public virtual void ExitVehicle(Position exitPosition = null)
	{
		//! This function can be called at upper level code to initialize an exit from the passenger's side.
		if (Vehicle == null)
			return;

		VehicleBase.RemoveAurasByType(AuraType.ControlVehicle, GUID);
		//! The following call would not even be executed successfully as the
		//! SPELL_AURA_CONTROL_VEHICLE unapply handler already calls _ExitVehicle without
		//! specifying an exitposition. The subsequent call below would return on if (!m_vehicle).

		//! To do:
		//! We need to allow SPELL_AURA_CONTROL_VEHICLE unapply handlers in spellscripts
		//! to specify exit coordinates and either store those per passenger, or we need to
		//! init spline movement based on those coordinates in unapply handlers, and
		//! relocate exiting passengers based on Unit.moveSpline data. Either way,
		//! Coming Soon(TM)
	}

	public void _ExitVehicle(Position exitPosition = null)
	{
		// It's possible m_vehicle is NULL, when this function is called indirectly from @VehicleJoinEvent.Abort.
		// In that case it was not possible to add the passenger to the vehicle. The vehicle aura has already been removed
		// from the target in the aforementioned function and we don't need to do anything else at this point.
		if (Vehicle == null)
			return;

		// This should be done before dismiss, because there may be some aura removal
		var seatAddon = Vehicle.GetSeatAddonForSeatOfPassenger(this);
		var vehicle = (Vehicle)Vehicle.RemovePassenger(this);

		if (vehicle == null)
		{
			Log.Logger.Error($"RemovePassenger() couldn't remove current unit from vehicle. Debug info: {GetDebugInfo()}");

			return;
		}

		var player = AsPlayer;

		// If the player is on mounted duel and exits the mount, he should immediatly lose the duel
		if (player && player.Duel is { IsMounted: true })
			player.DuelComplete(DuelCompleteType.Fled);

		SetControlled(false, UnitState.Root); // SMSG_MOVE_FORCE_UNROOT, ~MOVEMENTFLAG_ROOT

		AddUnitState(UnitState.Move);

		if (player != null)
			player.SetFallInformation(0, Location.Z);

		Position pos;

		// If we ask for a specific exit position, use that one. Otherwise allow scripts to modify it
		if (exitPosition != null)
		{
			pos = exitPosition;
		}
		else
		{
			// Set exit position to vehicle position and use the current orientation
			pos = vehicle.GetBase().Location;
			pos.Orientation = Location.Orientation;

			// Change exit position based on seat entry addon data
			if (seatAddon != null)
			{
				if (seatAddon.ExitParameter == VehicleExitParameters.VehicleExitParamOffset)
					pos.RelocateOffset(new Position(seatAddon.ExitParameterX, seatAddon.ExitParameterY, seatAddon.ExitParameterZ, seatAddon.ExitParameterO));
				else if (seatAddon.ExitParameter == VehicleExitParameters.VehicleExitParamDest)
					pos.Relocate(new Position(seatAddon.ExitParameterX, seatAddon.ExitParameterY, seatAddon.ExitParameterZ, seatAddon.ExitParameterO));
			}
		}

		var initializer = (MoveSplineInit init) =>
		{
			var height = pos.Z + vehicle.GetBase().CollisionHeight;

			// Creatures without inhabit type air should begin falling after exiting the vehicle
			if (IsTypeId(TypeId.Unit) && !CanFly && height > Map.GetWaterOrGroundLevel(PhaseShift, pos.X, pos.Y, pos.Z + vehicle.GetBase().CollisionHeight, ref height))
				init.SetFall();

			init.MoveTo(pos.X, pos.Y, height, false);
			init.SetFacing(pos.Orientation);
			init.SetTransportExit();
		};

		MotionMaster.LaunchMoveSpline(initializer, EventId.VehicleExit, MovementGeneratorPriority.Highest);

		if (player != null)
			player.ResummonPetTemporaryUnSummonedIfAny();

		if (vehicle.GetBase().HasUnitTypeMask(UnitTypeMask.Minion) && vehicle.GetBase().IsTypeId(TypeId.Unit))
			if (((Minion)vehicle.GetBase()).OwnerUnit == this)
				vehicle.GetBase().AsCreature.DespawnOrUnsummon(vehicle.GetDespawnDelay());

		if (HasUnitTypeMask(UnitTypeMask.Accessory))
		{
			// Vehicle just died, we die too
			if (vehicle.GetBase().DeathState == DeathState.JustDied)
				SetDeathState(DeathState.JustDied);
			// If for other reason we as minion are exiting the vehicle (ejected, master dismounted) - unsummon
			else
				ToTempSummon().UnSummon(_despawnTime); // Approximation
		}
	}

	public void UnsummonAllTotems()
	{
		for (byte i = 0; i < SharedConst.MaxSummonSlot; ++i)
		{
			if (SummonSlot[i].IsEmpty)
				continue;

			var OldTotem = Map.GetCreature(SummonSlot[i]);

			if (OldTotem is { IsSummon: true })
                OldTotem.ToTempSummon().UnSummon();
        }
	}

	public bool IsOnVehicle(Unit vehicle)
	{
		return Vehicle != null && Vehicle == vehicle.VehicleKit1;
	}

	public bool TryGetAI(out IUnitAI ai)
	{
		ai = BaseAI;

		return ai != null;
	}

	public bool TryGetCreatureAI(out CreatureAI ai)
	{
		ai = AI as CreatureAI;

		return ai != null;
	}

	public IUnitAI GetTopAI()
	{
		lock (UnitAis)
		{
			return UnitAis.Count == 0 ? null : UnitAis.Peek();
		}
	}

	public void AIUpdateTick(uint diff)
	{
		var ai = AI;

		if (ai != null)
			lock (UnitAis)
			{
				ai.UpdateAI(diff);
			}
	}

	public void PushAI(IUnitAI newAI)
	{
		lock (UnitAis)
		{
			UnitAis.Push(newAI);
		}
	}

	public bool PopAI()
	{
		lock (UnitAis)
		{
			if (UnitAis.Count != 0)
			{
				UnitAis.Pop();

				return true;
			}
			else
			{
				return false;
			}
		}
	}

	public void RefreshAI()
	{
		lock (UnitAis)
		{
			if (UnitAis.Count == 0)
				Ai = null;
			else
				Ai = UnitAis.Peek();
		}
	}

	public void ScheduleAIChange()
	{
		var charmed = IsCharmed;

		if (charmed)
		{
			PushAI(GetScheduledChangeAI());
		}
		else
		{
			RestoreDisabledAI();
			PushAI(GetScheduledChangeAI()); //This could actually be PopAI() to get the previous AI but it's required atm to trigger UpdateCharmAI()
		}
	}

	public virtual void OnPhaseChange() { }

	public uint GetModelForForm(ShapeShiftForm form, uint spellId)
	{
		// Hardcoded cases
		switch (spellId)
		{
			case 7090: // Bear Form
				return 29414;
			case 35200: // Roc Form
				return 4877;
			case 24858: // Moonkin Form
			{
				if (HasAura(114301)) // Glyph of Stars
					return 0;

				break;
			}
			default:
				break;
		}

		var thisPlayer = AsPlayer;

		if (thisPlayer != null)
		{
			var artifactAura = GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);

			if (artifactAura != null)
			{
				var artifact = AsPlayer.GetItemByGuid(artifactAura.CastItemGuid);

				if (artifact != null)
				{
					var artifactAppearance = CliDB.ArtifactAppearanceStorage.LookupByKey(artifact.GetModifier(ItemModifier.ArtifactAppearanceId));

					if (artifactAppearance != null)
						if ((ShapeShiftForm)artifactAppearance.OverrideShapeshiftFormID == form)
							return artifactAppearance.OverrideShapeshiftDisplayID;
				}
			}

			var formModelData = Global.DB2Mgr.GetShapeshiftFormModelData(Race, thisPlayer.NativeGender, form);

			if (formModelData != null)
			{
				var useRandom = false;

				switch (form)
				{
					case ShapeShiftForm.CatForm:
						useRandom = HasAura(210333);

						break; // Glyph of the Feral Chameleon
					case ShapeShiftForm.TravelForm:
						useRandom = HasAura(344336);

						break; // Glyph of the Swift Chameleon
					case ShapeShiftForm.AquaticForm:
						useRandom = HasAura(344338);

						break; // Glyph of the Aquatic Chameleon
					case ShapeShiftForm.BearForm:
						useRandom = HasAura(107059);

						break; // Glyph of the Ursol Chameleon
					case ShapeShiftForm.FlightFormEpic:
					case ShapeShiftForm.FlightForm:
						useRandom = HasAura(344342);

						break; // Glyph of the Aerial Chameleon
					default:
						break;
				}

				if (useRandom)
				{
					List<uint> displayIds = new();

					for (var i = 0; i < formModelData.Choices.Count; ++i)
					{
						var displayInfo = formModelData.Displays[i];

						if (displayInfo != null)
						{
							var choiceReq = CliDB.ChrCustomizationReqStorage.LookupByKey(formModelData.Choices[i].ChrCustomizationReqID);

							if (choiceReq == null || thisPlayer.Session.MeetsChrCustomizationReq(choiceReq, Class, false, thisPlayer.PlayerData.Customizations))
								displayIds.Add(displayInfo.DisplayID);
						}
					}

					if (!displayIds.Empty())
						return displayIds.SelectRandom();
				}
				else
				{
					var formChoice = thisPlayer.GetCustomizationChoice(formModelData.OptionID);

					if (formChoice != 0)
					{
						var choiceIndex = formModelData.Choices.FindIndex(choice => { return choice.Id == formChoice; });

						if (choiceIndex != -1)
						{
							var displayInfo = formModelData.Displays[choiceIndex];

							if (displayInfo != null)
								return displayInfo.DisplayID;
						}
					}
				}
			}

			switch (form)
			{
				case ShapeShiftForm.GhostWolf:
					if (HasAura(58135)) // Glyph of Spectral Wolf
						return 60247;

					break;
				default:
					break;
			}
		}

		uint modelid = 0;
		var formEntry = CliDB.SpellShapeshiftFormStorage.LookupByKey(form);

		if (formEntry != null && formEntry.CreatureDisplayID[0] != 0)
		{
			// Take the alliance modelid as default
			if (TypeId != TypeId.Player)
			{
				return formEntry.CreatureDisplayID[0];
			}
			else
			{
				if (Player.TeamForRace(Race) == TeamFaction.Alliance)
					modelid = formEntry.CreatureDisplayID[0];
				else
					modelid = formEntry.CreatureDisplayID[1];

				// If the player is horde but there are no values for the horde modelid - take the alliance modelid
				if (modelid == 0 && Player.TeamForRace(Race) == TeamFaction.Horde)
					modelid = formEntry.CreatureDisplayID[0];
			}
		}

		return modelid;
	}

	public Totem ToTotem()
	{
		return IsTotem ? (this as Totem) : null;
	}

	public TempSummon ToTempSummon()
	{
		return IsSummon ? (this as TempSummon) : null;
	}

	public virtual void SetDeathState(DeathState s)
	{
		// Death state needs to be updated before RemoveAllAurasOnDeath() is called, to prevent entering combat
		DeathState = s;

		var isOnVehicle = Vehicle1 != null;

		if (s != DeathState.Alive && s != DeathState.JustRespawned)
		{
			CombatStop();

			if (IsNonMeleeSpellCast(false))
				InterruptNonMeleeSpells(false);

			ExitVehicle(); // Exit vehicle before calling RemoveAllControlled
			// vehicles use special type of charm that is not removed by the next function
			// triggering an assert
			UnsummonAllTotems();
			RemoveAllControlled();
			RemoveAllAurasOnDeath();
		}

		if (s == DeathState.JustDied)
		{
			// remove aurastates allowing special moves
			ClearAllReactives();
			_diminishing.Clear();

			// Don't clear the movement if the Unit was on a vehicle as we are exiting now
			if (!isOnVehicle)
				if (MotionMaster.StopOnDeath())
					DisableSpline();

			// without this when removing IncreaseMaxHealth aura player may stuck with 1 hp
			// do not why since in IncreaseMaxHealth currenthealth is checked
			SetHealth(0);
			SetPower(DisplayPowerType, 0);
			EmoteState = Emote.OneshotNone;

			// players in instance don't have ZoneScript, but they have InstanceScript
			var zoneScript = ZoneScript1 != null ? ZoneScript1 : InstanceScript;

			if (zoneScript != null)
				zoneScript.OnUnitDeath(this);
		}
		else if (s == DeathState.JustRespawned)
		{
			RemoveUnitFlag(UnitFlags.Skinnable); // clear skinnable for creature and player (at Battleground)
		}
	}

	public bool IsVisible() => ServerSideVisibility.GetValue(ServerSideVisibilityType.GM) <= (uint)AccountTypes.Player;

	public void SetVisible(bool val)
	{
		if (!val)
			ServerSideVisibility.SetValue(ServerSideVisibilityType.GM, AccountTypes.GameMaster);
		else
			ServerSideVisibility.SetValue(ServerSideVisibilityType.GM, AccountTypes.Player);

		UpdateObjectVisibility();
	}

	// creates aura application instance and registers it in lists
	// aura application effects are handled separately to prevent aura list corruption
	public AuraApplication _CreateAuraApplication(Aura aura, HashSet<int> effMask)
	{
		// just return if the aura has been already removed
		// this can happen if OnEffectHitTarget() script hook killed the unit or the aura owner (which can be different)
		if (aura.IsRemoved)
		{
			Log.Logger.Error($"Unit::_CreateAuraApplication() called with a removed aura. Check if OnEffectHitTarget() is triggering any spell with apply aura effect (that's not allowed!)\nUnit: {GetDebugInfo()}\nAura: {aura.GetDebugInfo()}");

			return null;
		}

		var aurSpellInfo = aura.SpellInfo;

		// ghost spell check, allow apply any auras at player loading in ghost mode (will be cleanup after load)
		if (!IsAlive &&
			!aurSpellInfo.IsDeathPersistent &&
			(!IsTypeId(TypeId.Player) || !AsPlayer.Session.PlayerLoading))
			return null;

		var caster = aura.Caster;

		AuraApplication aurApp = new(this, caster, aura, effMask);
		_appliedAuras.Add(aurApp);

		if (aurSpellInfo.HasAnyAuraInterruptFlag)
		{
			_interruptableAuras.Add(aurApp);
			AddInterruptMask(aurSpellInfo.AuraInterruptFlags, aurSpellInfo.AuraInterruptFlags2);
		}

		var aState = aura.SpellInfo.GetAuraState();

		if (aState != 0)
			_auraStateAuras.Add(aState, aurApp);

		aura._ApplyForTarget(this, caster, aurApp);

		return aurApp;
	}

	public void AddInterruptMask(SpellAuraInterruptFlags flags, SpellAuraInterruptFlags2 flags2)
	{
		_interruptMask |= flags;
		_interruptMask2 |= flags2;
	}

	public void UpdateDisplayPower()
	{
		var displayPower = PowerType.Mana;

		switch (ShapeshiftForm)
		{
			case ShapeShiftForm.Ghoul:
			case ShapeShiftForm.CatForm:
				displayPower = PowerType.Energy;

				break;
			case ShapeShiftForm.BearForm:
				displayPower = PowerType.Rage;

				break;
			case ShapeShiftForm.TravelForm:
			case ShapeShiftForm.GhostWolf:
				displayPower = PowerType.Mana;

				break;
			default:
			{
				var powerTypeAuras = GetAuraEffectsByType(AuraType.ModPowerDisplay);

				if (!powerTypeAuras.Empty())
				{
					var powerTypeAura = powerTypeAuras.First();
					displayPower = (PowerType)powerTypeAura.MiscValue;
				}
				else if (TypeId == TypeId.Player)
				{
					var cEntry = CliDB.ChrClassesStorage.LookupByKey(Class);

					if (cEntry is { DisplayPower: < PowerType.Max })
						displayPower = cEntry.DisplayPower;
				}
				else if (TypeId == TypeId.Unit)
				{
					var vehicle = VehicleKit1;

					if (vehicle)
					{
						var powerDisplay = CliDB.PowerDisplayStorage.LookupByKey(vehicle.GetVehicleInfo().PowerDisplayID[0]);

						if (powerDisplay != null)
							displayPower = (PowerType)powerDisplay.ActualType;
						else if (Class == PlayerClass.Rogue)
							displayPower = PowerType.Energy;
					}
					else
					{
						var pet = AsPet;

						if (pet)
						{
							if (pet.PetType == PetType.Hunter) // Hunter pets have focus
								displayPower = PowerType.Focus;
							else if (pet.IsPetGhoul() || pet.IsPetAbomination()) // DK pets have energy
								displayPower = PowerType.Energy;
						}
					}
				}

				break;
			}
		}

		SetPowerType(displayPower);
	}

	public void FollowerAdded(AbstractFollower f)
	{
		_followingMe.Add(f);
	}

	public void FollowerRemoved(AbstractFollower f)
	{
		_followingMe.Remove(f);
	}

	public void PlayOneShotAnimKitId(ushort animKitId)
	{
		if (!CliDB.AnimKitStorage.ContainsKey(animKitId))
		{
			Log.Logger.Error("Unit.PlayOneShotAnimKitId using invalid AnimKit ID: {0}", animKitId);

			return;
		}

		PlayOneShotAnimKit packet = new()
        {
            Unit = GUID,
            AnimKitID = animKitId
        };

        SendMessageToSet(packet, true);
	}

	public void SetAIAnimKitId(ushort animKitId)
	{
		if (_aiAnimKitId == animKitId)
			return;

		if (animKitId != 0 && !CliDB.AnimKitStorage.ContainsKey(animKitId))
			return;

		_aiAnimKitId = animKitId;

		SetAIAnimKit data = new()
        {
            Unit = GUID,
            AnimKitID = animKitId
        };

        SendMessageToSet(data, true);
	}

	public void SetMovementAnimKitId(ushort animKitId)
	{
		if (_movementAnimKitId == animKitId)
			return;

		if (animKitId != 0 && !CliDB.AnimKitStorage.ContainsKey(animKitId))
			return;

		_movementAnimKitId = animKitId;

		SetMovementAnimKit data = new()
        {
            Unit = GUID,
            AnimKitID = animKitId
        };

        SendMessageToSet(data, true);
	}

	public void SetMeleeAnimKitId(ushort animKitId)
	{
		if (_meleeAnimKitId == animKitId)
			return;

		if (animKitId != 0 && !CliDB.AnimKitStorage.ContainsKey(animKitId))
			return;

		_meleeAnimKitId = animKitId;

		SetMeleeAnimKit data = new()
        {
            Unit = GUID,
            AnimKitID = animKitId
        };

        SendMessageToSet(data, true);
	}


	public bool HasExtraUnitMovementFlag(MovementFlag2 f)
	{
		return MovementInfo.HasMovementFlag2(f);
	}

	public uint GetVirtualItemId(int slot)
	{
		if (slot >= SharedConst.MaxEquipmentItems)
			return 0;

		return UnitData.VirtualItems[slot].ItemID;
	}

	public ushort GetVirtualItemAppearanceMod(uint slot)
	{
		if (slot >= SharedConst.MaxEquipmentItems)
			return 0;

		return UnitData.VirtualItems[(int)slot].ItemAppearanceModID;
	}

	public void SetVirtualItem(uint slot, uint itemId, ushort appearanceModId = 0, ushort itemVisual = 0)
	{
		if (slot >= SharedConst.MaxEquipmentItems)
			return;

		var virtualItemField = Values.ModifyValue(UnitData).ModifyValue(UnitData.VirtualItems, (int)slot);
		SetUpdateFieldValue(virtualItemField.ModifyValue(virtualItemField.ItemID), itemId);
		SetUpdateFieldValue(virtualItemField.ModifyValue(virtualItemField.ItemAppearanceModID), appearanceModId);
		SetUpdateFieldValue(virtualItemField.ModifyValue(virtualItemField.ItemVisual), itemVisual);
	}

	//Unit
	public void SetLevel(uint lvl, bool sendUpdate = true)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Level), lvl);

		if (!sendUpdate)
			return;

		var player = AsPlayer;

		if (player != null)
		{
			if (player.Group)
				player.SetGroupUpdateFlag(GroupUpdateFlags.Level);

			Global.CharacterCacheStorage.UpdateCharacterLevel(AsPlayer.GUID, (byte)lvl);
		}
	}

	public override uint GetLevelForTarget(WorldObject target)
	{
		return Level;
	}

	public bool IsAlliedRace()
	{
		var player = AsPlayer;

		if (player == null)
			return false;

		var race = Race;

		/* pandaren death knight (basically same thing as allied death knight) */
		if ((race == Race.PandarenAlliance || race == Race.PandarenHorde || race == Race.PandarenNeutral) && Class == PlayerClass.Deathknight)
			return true;

		/* other allied races */
		switch (race)
		{
			case Race.Nightborne:
			case Race.HighmountainTauren:
			case Race.VoidElf:
			case Race.LightforgedDraenei:
			case Race.ZandalariTroll:
			case Race.KulTiran:
			case Race.DarkIronDwarf:
			case Race.Vulpera:
			case Race.MagharOrc:
			case Race.MechaGnome:
				return true;
			default:
				return false;
		}
	}

	public void RecalculateObjectScale()
	{
		var scaleAuras = GetTotalAuraModifier(AuraType.ModScale) + GetTotalAuraModifier(AuraType.ModScale2);
		var scale = NativeObjectScale + MathFunctions.CalculatePct(1.0f, scaleAuras);
		var scaleMin = IsPlayer ? 0.1f : 0.01f;
		ObjectScale = (float)Math.Max(scale, scaleMin);
	}

	public virtual void SetDisplayId(uint modelId, float displayScale = 1f)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.DisplayID), modelId);
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.DisplayScale), displayScale);
		// Set Gender by modelId
		var minfo = Global.ObjectMgr.GetCreatureModelInfo(modelId);

		if (minfo != null)
			Gender = (Gender)minfo.Gender;
	}

	public void RestoreDisplayId(bool ignorePositiveAurasPreventingMounting = false)
	{
		AuraEffect handledAura = null;
		// try to receive model from transform auras
		var transforms = GetAuraEffectsByType(AuraType.Transform);

		if (!transforms.Empty())
			// iterate over already applied transform auras - from newest to oldest
			foreach (var eff in transforms)
			{
				var aurApp = eff.Base.GetApplicationOfTarget(GUID);

				if (aurApp != null)
				{
					if (handledAura == null)
					{
						if (!ignorePositiveAurasPreventingMounting)
						{
							handledAura = eff;
						}
						else
						{
							var ci = Global.ObjectMgr.GetCreatureTemplate((uint)eff.MiscValue);

							if (ci != null)
								if (!IsDisallowedMountForm(eff.Id, ShapeShiftForm.None, GameObjectManager.ChooseDisplayId(ci).CreatureDisplayId))
									handledAura = eff;
						}
					}

					// prefer negative auras
					if (!aurApp.IsPositive)
					{
						handledAura = eff;

						break;
					}
				}
			}

		var shapeshiftAura = GetAuraEffectsByType(AuraType.ModShapeshift);

		// transform aura was found
		if (handledAura != null)
		{
			handledAura.HandleEffect(this, AuraEffectHandleModes.SendForClient, true);

			return;
		}
		// we've found shapeshift
		else if (!shapeshiftAura.Empty()) // we've found shapeshift
		{
			// only one such aura possible at a time
			var modelId = GetModelForForm(ShapeshiftForm, shapeshiftAura[0].Id);

			if (modelId != 0)
			{
				if (!ignorePositiveAurasPreventingMounting || !IsDisallowedMountForm(0, ShapeshiftForm, modelId))
					SetDisplayId(modelId);
				else
					SetDisplayId(NativeDisplayId);

				return;
			}
		}

		// no auras found - set modelid to default
		SetDisplayId(NativeDisplayId);
	}

	public void SetNativeDisplayId(uint displayId, float displayScale = 1f)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.NativeDisplayID), displayId);
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.NativeXDisplayScale), displayScale);
	}

	public void SetCosmeticMountDisplayId(uint mountDisplayId)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.CosmeticMountDisplayID), mountDisplayId);
	}

	public void SetOwnerGUID(ObjectGuid owner)
	{
		if (OwnerGUID == owner)
			return;

		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.SummonedBy), owner);

		if (owner.IsEmpty)
			return;

		// Update owner dependent fields
		var player = Global.ObjAccessor.GetPlayer(this, owner);

		if (player == null || !player.HaveAtClient(this)) // if player cannot see this unit yet, he will receive needed data with create object
			return;

		UpdateData udata = new(Location.MapId);
		BuildValuesUpdateBlockForPlayerWithFlag(udata, UpdateFieldFlag.Owner, player);
		udata.BuildPacket(out var packet);
		player.SendPacket(packet);
	}

	public void SetCreatorGUID(ObjectGuid creator)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.CreatedBy), creator);
	}

	public bool HasUnitFlag(UnitFlags flags)
	{
		return (UnitData.Flags & (uint)flags) != 0;
	}

	public void SetUnitFlag(UnitFlags flags)
	{
		SetUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Flags), (uint)flags);
	}

	public void RemoveUnitFlag(UnitFlags flags)
	{
		RemoveUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Flags), (uint)flags);
	}

	public void ReplaceAllUnitFlags(UnitFlags flags)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Flags), (uint)flags);
	}

	public bool HasUnitFlag2(UnitFlags2 flags)
	{
		return (UnitData.Flags2 & (uint)flags) != 0;
	}

	public void SetUnitFlag2(UnitFlags2 flags)
	{
		SetUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Flags2), (uint)flags);
	}

	public void RemoveUnitFlag2(UnitFlags2 flags)
	{
		RemoveUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Flags2), (uint)flags);
	}

	public void ReplaceAllUnitFlags2(UnitFlags2 flags)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Flags2), (uint)flags);
	}

	public bool HasUnitFlag3(UnitFlags3 flags)
	{
		return (UnitData.Flags3 & (uint)flags) != 0;
	}

	public void SetUnitFlag3(UnitFlags3 flags)
	{
		SetUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Flags3), (uint)flags);
	}

	public void RemoveUnitFlag3(UnitFlags3 flags)
	{
		RemoveUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Flags3), (uint)flags);
	}

	public void ReplaceAllUnitFlags3(UnitFlags3 flags)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Flags3), (uint)flags);
	}

	public bool HasDynamicFlag(UnitDynFlags flag)
	{
		return (ObjectData.DynamicFlags & (uint)flag) != 0;
	}

	public void SetDynamicFlag(UnitDynFlags flag)
	{
		SetUpdateFieldFlagValue(Values.ModifyValue(ObjectData).ModifyValue(ObjectData.DynamicFlags), (uint)flag);
	}

	public void RemoveDynamicFlag(UnitDynFlags flag)
	{
		RemoveUpdateFieldFlagValue(Values.ModifyValue(ObjectData).ModifyValue(ObjectData.DynamicFlags), (uint)flag);
	}

	public void ReplaceAllDynamicFlags(UnitDynFlags flag)
	{
		SetUpdateFieldValue(Values.ModifyValue(ObjectData).ModifyValue(ObjectData.DynamicFlags), (uint)flag);
	}

	public void SetCreatedBySpell(uint spellId)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.CreatedBySpell), spellId);
	}

	public void SetNameplateAttachToGUID(ObjectGuid guid)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.NameplateAttachToGUID), guid);
	}

	public bool HasPvpFlag(UnitPVPStateFlags flags)
	{
		return (UnitData.PvpFlags & (uint)flags) != 0;
	}

	public void SetPvpFlag(UnitPVPStateFlags flags)
	{
		SetUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.PvpFlags), (byte)flags);
	}

	public void RemovePvpFlag(UnitPVPStateFlags flags)
	{
		RemoveUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.PvpFlags), (byte)flags);
	}

	public void ReplaceAllPvpFlags(UnitPVPStateFlags flags)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.PvpFlags), (byte)flags);
	}

	public bool HasPetFlag(UnitPetFlags flags)
	{
		return (UnitData.PetFlags & (byte)flags) != 0;
	}

	public void SetPetFlag(UnitPetFlags flags)
	{
		SetUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.PetFlags), (byte)flags);
	}

	public void RemovePetFlag(UnitPetFlags flags)
	{
		RemoveUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.PetFlags), (byte)flags);
	}

	public void ReplaceAllPetFlags(UnitPetFlags flags)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.PetFlags), (byte)flags);
	}

	public void SetPetNumberForClient(uint petNumber)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.PetNumber), petNumber);
	}

	public void SetPetNameTimestamp(uint timestamp)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.PetNameTimestamp), timestamp);
	}

	public void DeMorph()
	{
		SetDisplayId(NativeDisplayId);
	}

	public bool HasUnitTypeMask(UnitTypeMask mask)
	{
		return Convert.ToBoolean(mask & UnitTypeMask);
	}

	public void AddUnitTypeMask(UnitTypeMask mask)
	{
		UnitTypeMask |= mask;
	}

	public void AddUnitState(UnitState f)
	{
		_state |= f;
	}

	public bool HasUnitState(UnitState f)
	{
		return _state.HasAnyFlag(f);
	}

	public void ClearUnitState(UnitState f)
	{
		_state &= ~f;
	}

	public override bool IsAlwaysVisibleFor(WorldObject seer)
	{
		if (base.IsAlwaysVisibleFor(seer))
			return true;

		// Always seen by owner
		var guid = CharmerOrOwnerGUID;

		if (!guid.IsEmpty)
			if (seer.GUID == guid)
				return true;

		var seerPlayer = seer.AsPlayer;

		if (seerPlayer != null)
		{
			var owner = OwnerUnit;

			if (owner != null)
			{
				var ownerPlayer = owner.AsPlayer;

				if (ownerPlayer)
					if (ownerPlayer.IsGroupVisibleFor(seerPlayer))
						return true;
			}
		}

		return false;
	}

	public void RestoreFaction()
	{
		if (HasAuraType(AuraType.ModFaction))
		{
			Faction = (uint)GetAuraEffectsByType(AuraType.ModFaction).LastOrDefault().MiscValue;

			return;
		}

		if (IsTypeId(TypeId.Player))
		{
			AsPlayer.SetFactionForRace(Race);
		}
		else
		{
			if (HasUnitTypeMask(UnitTypeMask.Minion))
			{
				var owner = OwnerUnit;

				if (owner)
				{
					Faction = owner.Faction;

					return;
				}
			}

			var cinfo = AsCreature.Template;

			if (cinfo != null) // normal creature
				Faction = cinfo.Faction;
		}
	}

	public bool IsInPartyWith(Unit unit)
	{
		if (this == unit)
			return true;

		var u1 = CharmerOrOwnerOrSelf;
		var u2 = unit.CharmerOrOwnerOrSelf;

		if (u1 == u2)
			return true;

		if (u1.IsTypeId(TypeId.Player) && u2.IsTypeId(TypeId.Player))
			return u1.AsPlayer.IsInSameGroupWith(u2.AsPlayer);
		else if ((u2.IsTypeId(TypeId.Player) && u1.IsTypeId(TypeId.Unit) && u1.AsCreature.Template.TypeFlags.HasAnyFlag(CreatureTypeFlags.TreatAsRaidUnit)) ||
				(u1.IsTypeId(TypeId.Player) && u2.IsTypeId(TypeId.Unit) && u2.AsCreature.Template.TypeFlags.HasAnyFlag(CreatureTypeFlags.TreatAsRaidUnit)))
			return true;

		return u1.TypeId == TypeId.Unit && u2.TypeId == TypeId.Unit && u1.Faction == u2.Faction;
	}

	public bool IsInRaidWith(Unit unit)
	{
		if (this == unit)
			return true;

		var u1 = CharmerOrOwnerOrSelf;
		var u2 = unit.CharmerOrOwnerOrSelf;

		if (u1 == u2)
			return true;

		if (u1.IsTypeId(TypeId.Player) && u2.IsTypeId(TypeId.Player))
			return u1.AsPlayer.IsInSameRaidWith(u2.AsPlayer);
		else if ((u2.IsTypeId(TypeId.Player) && u1.IsTypeId(TypeId.Unit) && u1.AsCreature.Template.TypeFlags.HasAnyFlag(CreatureTypeFlags.TreatAsRaidUnit)) ||
				(u1.IsTypeId(TypeId.Player) && u2.IsTypeId(TypeId.Unit) && u2.AsCreature.Template.TypeFlags.HasAnyFlag(CreatureTypeFlags.TreatAsRaidUnit)))
			return true;

		// else u1.GetTypeId() == u2.GetTypeId() == TYPEID_UNIT
		return u1.Faction == u2.Faction;
	}

	public void GetPartyMembers(List<Unit> TagUnitMap)
	{
		var owner = CharmerOrOwnerOrSelf;
		PlayerGroup group = null;

		if (owner.TypeId == TypeId.Player)
			group = owner.AsPlayer.Group;

		if (group != null)
		{
			var subgroup = owner.AsPlayer.SubGroup;

			for (var refe = group.FirstMember; refe != null; refe = refe.Next())
			{
				var target = refe.Source;

				// IsHostileTo check duel and controlled by enemy
				if (target != null && target.IsInMap(owner) && target.SubGroup == subgroup && !IsHostileTo(target))
				{
					if (target.IsAlive)
						TagUnitMap.Add(target);

					var pet = target.GetGuardianPet();

					if (target.GetGuardianPet())
						if (pet.IsAlive)
							TagUnitMap.Add(pet);
				}
			}
		}
		else
		{
			if ((owner == this || IsInMap(owner)) && owner.IsAlive)
				TagUnitMap.Add(owner);

			var pet = owner.GetGuardianPet();

			if (owner.GetGuardianPet() != null)
				if ((pet == this || IsInMap(pet)) && pet.IsAlive)
					TagUnitMap.Add(pet);
		}
	}

	public void SetVisFlag(UnitVisFlags flags)
	{
		SetUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.VisFlags), (byte)flags);
	}

	public void RemoveVisFlag(UnitVisFlags flags)
	{
		RemoveUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.VisFlags), (byte)flags);
	}

	public void ReplaceAllVisFlags(UnitVisFlags flags)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.VisFlags), (byte)flags);
	}

	public void SetStandState(UnitStandStateType state, uint animKitId = 0)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.StandState), (byte)state);

		if (IsStandState)
			RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Standing);

		if (IsTypeId(TypeId.Player))
		{
			StandStateUpdate packet = new(state, animKitId);
			AsPlayer.SendPacket(packet);
		}
	}

	public void SetAnimTier(AnimTier animTier, bool notifyClient = true)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.AnimTier), (byte)animTier);

		if (notifyClient)
		{
			SetAnimTier setAnimTier = new()
            {
                Unit = GUID,
                Tier = (int)animTier
            };

            SendMessageToSet(setAnimTier, true);
		}
	}

	public void SetChannelVisual(SpellCastVisualField channelVisual)
	{
		UnitChannel unitChannel = Values.ModifyValue(UnitData).ModifyValue(UnitData.ChannelData);
		SetUpdateFieldValue(ref unitChannel.SpellVisual, channelVisual);
	}

	public void AddChannelObject(ObjectGuid guid)
	{
		AddDynamicUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ChannelObjects), guid);
	}

	public void SetChannelObject(int slot, ObjectGuid guid)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ChannelObjects, slot), guid);
	}

	public void ClearChannelObjects()
	{
		ClearDynamicUpdateFieldValues(Values.ModifyValue(UnitData).ModifyValue(UnitData.ChannelObjects));
	}

	public void RemoveChannelObject(ObjectGuid guid)
	{
		var index = UnitData.ChannelObjects.FindIndex(guid);

		if (index >= 0)
			RemoveDynamicUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ChannelObjects), index);
	}

	public static bool IsDamageReducedByArmor(SpellSchoolMask schoolMask, SpellInfo spellInfo = null)
	{
		// only physical spells damage gets reduced by armor
		if ((schoolMask & SpellSchoolMask.Normal) == 0)
			return false;

		return spellInfo == null || !spellInfo.HasAttribute(SpellCustomAttributes.IgnoreArmor);
	}

	public override UpdateFieldFlag GetUpdateFieldFlagsFor(Player target)
	{
		var flags = UpdateFieldFlag.None;

		if (target == this || OwnerGUID == target.GUID)
			flags |= UpdateFieldFlag.Owner;

		if (HasDynamicFlag(UnitDynFlags.SpecialInfo))
			if (HasAuraTypeWithCaster(AuraType.Empathy, target.GUID))
				flags |= UpdateFieldFlag.Empath;

		return flags;
	}

	public override void BuildValuesCreate(WorldPacket data, Player target)
	{
		var flags = GetUpdateFieldFlagsFor(target);
		WorldPacket buffer = new();

		buffer.WriteUInt8((byte)flags);
		ObjectData.WriteCreate(buffer, flags, this, target);
		UnitData.WriteCreate(buffer, flags, this, target);

		data.WriteUInt32(buffer.GetSize());
		data.WriteBytes(buffer);
	}

	public override void BuildValuesUpdate(WorldPacket data, Player target)
	{
		var flags = GetUpdateFieldFlagsFor(target);
		WorldPacket buffer = new();

		buffer.WriteUInt32(Values.GetChangedObjectTypeMask());

		if (Values.HasChanged(TypeId.Object))
			ObjectData.WriteUpdate(buffer, flags, this, target);

		if (Values.HasChanged(TypeId.Unit))
			UnitData.WriteUpdate(buffer, flags, this, target);

		data.WriteUInt32(buffer.GetSize());
		data.WriteBytes(buffer);
	}

	public override void BuildValuesUpdateWithFlag(WorldPacket data, UpdateFieldFlag flags, Player target)
	{
		UpdateMask valuesMask = new(14);
		valuesMask.Set((int)TypeId.Unit);

		WorldPacket buffer = new();

		UpdateMask mask = new(194);
		UnitData.AppendAllowedFieldsMaskForFlag(mask, flags);
		UnitData.WriteUpdate(buffer, mask, true, this, target);

		data.WriteUInt32(buffer.GetSize());
		data.WriteUInt32(valuesMask.GetBlock(0));
		data.WriteBytes(buffer);
	}

	public void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedUnitMask, Player target)
	{
		var flags = GetUpdateFieldFlagsFor(target);
		UpdateMask valuesMask = new((int)TypeId.Max);

		if (requestedObjectMask.IsAnySet())
			valuesMask.Set((int)TypeId.Object);

		UnitData.FilterDisallowedFieldsMaskForFlag(requestedUnitMask, flags);

		if (requestedUnitMask.IsAnySet())
			valuesMask.Set((int)TypeId.Unit);

		WorldPacket buffer = new();
		buffer.WriteUInt32(valuesMask.GetBlock(0));

		if (valuesMask[(int)TypeId.Object])
			ObjectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

		if (valuesMask[(int)TypeId.Unit])
			UnitData.WriteUpdate(buffer, requestedUnitMask, true, this, target);

		WorldPacket buffer1 = new();
		buffer1.WriteUInt8((byte)UpdateType.Values);
		buffer1.WritePackedGuid(GUID);
		buffer1.WriteUInt32(buffer.GetSize());
		buffer1.WriteBytes(buffer.GetData());

		data.AddUpdateBlock(buffer1);
	}

	public override void ClearUpdateMask(bool remove)
	{
		Values.ClearChangesMask(UnitData);
		base.ClearUpdateMask(remove);
	}

	public override void DestroyForPlayer(Player target)
	{
		var bg = target.Battleground;

		if (bg != null)
			if (bg.IsArena())
			{
				DestroyArenaUnit destroyArenaUnit = new()
                {
                    Guid = GUID
                };

                target.SendPacket(destroyArenaUnit);
			}

		base.DestroyForPlayer(target);
	}

	public virtual void SetCanDualWield(bool value)
	{
		CanDualWield = value;
	}

	public bool HaveOffhandWeapon()
	{
		if (IsTypeId(TypeId.Player))
			return AsPlayer.GetWeaponForAttack(WeaponAttackType.OffAttack, true) != null;
		else
			return CanDualWield;
	}

	public static void DealDamageMods(Unit attacker, Unit victim, ref double damage)
	{
		if (victim == null || !victim.IsAlive || victim.HasUnitState(UnitState.InFlight) || (victim.IsTypeId(TypeId.Unit) && victim.AsCreature.IsInEvadeMode))
			damage = 0;
	}

	public static bool CheckEvade(Unit attacker, Unit victim, ref double damage, ref double absorb)
	{
		if (victim == null || !victim.IsAlive || victim.HasUnitState(UnitState.InFlight) || (victim.IsTypeId(TypeId.Unit) && victim.AsCreature.IsEvadingAttacks))
		{
			absorb += damage;
			damage = 0;

			return true;
		}

		return false;
	}

	public static void ScaleDamage(Unit attacker, Unit victim, ref double damage)
	{
		if (attacker != null)
			damage = damage * attacker.GetDamageMultiplierForTarget(victim);
	}

	public static void DealDamageMods(Unit attacker, Unit victim, ref double damage, ref double absorb)
	{
		if (!CheckEvade(attacker, victim, ref damage, ref absorb))
			ScaleDamage(attacker, victim, ref damage);
	}

	public static double DealDamage(Unit attacker, Unit victim, double damage, CleanDamage cleanDamage = null, DamageEffectType damagetype = DamageEffectType.Direct, SpellSchoolMask damageSchoolMask = SpellSchoolMask.Normal, SpellInfo spellProto = null, bool durabilityLoss = true)
	{
		var damageDone = damage;
		var damageTaken = damage;

		if (attacker != null)
			damageTaken = (damage / victim.GetHealthMultiplierForTarget(attacker));

		// call script hooks
		{
			var tmpDamage = damageTaken;

			victim.AI?.DamageTaken(attacker, ref tmpDamage, damagetype, spellProto);

			attacker?.AI?.DamageDealt(victim, ref tmpDamage, damagetype);

			// Hook for OnDamage Event
			Global.ScriptMgr.ForEach<IUnitOnDamage>(p => p.OnDamage(attacker, victim, ref tmpDamage));

			// if any script modified damage, we need to also apply the same modification to unscaled damage value
			if (tmpDamage != damageTaken)
			{
				if (attacker)
					damageDone = (uint)(tmpDamage * victim.GetHealthMultiplierForTarget(attacker));
				else
					damageDone = tmpDamage;

				damageTaken = tmpDamage;
			}
		}

		// Signal to pets that their owner was attacked - except when DOT.
		if (attacker != victim && damagetype != DamageEffectType.DOT)
			foreach (var controlled in victim.Controlled)
			{
				var cControlled = controlled.AsCreature;

				if (cControlled != null)
				{
					var controlledAI = cControlled.AI;

					if (controlledAI != null)
						controlledAI.OwnerAttackedBy(attacker);
				}
			}

		var player = victim.AsPlayer;

		if (player != null && player.GetCommandStatus(PlayerCommandStates.God))
			return 0;

		if (damagetype != DamageEffectType.NoDamage)
		{
			// interrupting auras with SpellAuraInterruptFlags.Damage before checking !damage (absorbed damage breaks that type of auras)
			if (spellProto != null)
			{
				if (!spellProto.HasAttribute(SpellAttr4.ReactiveDamageProc))
					victim.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Damage, spellProto);
			}
			else
			{
				victim.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Damage);
			}

			if (damageTaken == 0 && damagetype != DamageEffectType.DOT && cleanDamage != null && cleanDamage.AbsorbedDamage != 0)
				if (victim != attacker && victim.IsPlayer)
				{
					var spell = victim.GetCurrentSpell(CurrentSpellTypes.Generic);

					if (spell != null)
						if (spell.State == SpellState.Preparing && spell.SpellInfo.InterruptFlags.HasAnyFlag(SpellInterruptFlags.DamageAbsorb))
							victim.InterruptNonMeleeSpells(false);
				}

			// We're going to call functions which can modify content of the list during iteration over it's elements
			// Let's copy the list so we can prevent iterator invalidation
			var vCopyDamageCopy = victim.GetAuraEffectsByType(AuraType.ShareDamagePct);

			// copy damage to casters of this aura
			foreach (var aura in vCopyDamageCopy)
			{
				// Check if aura was removed during iteration - we don't need to work on such auras
				if (!aura.Base.IsAppliedOnTarget(victim.GUID))
					continue;

				// check damage school mask
				if ((aura.MiscValue & (int)damageSchoolMask) == 0)
					continue;

				var shareDamageTarget = aura.Caster;

				if (shareDamageTarget == null)
					continue;

				var spell = aura.SpellInfo;

				var share = MathFunctions.CalculatePct(damageDone, aura.Amount);

				// @todo check packets if damage is done by victim, or by attacker of victim
				DealDamageMods(attacker, shareDamageTarget, ref share);
				DealDamage(attacker, shareDamageTarget, share, null, DamageEffectType.NoDamage, spell.GetSchoolMask(), spell, false);
			}
		}

		// Rage from Damage made (only from direct weapon damage)
		if (attacker != null && cleanDamage != null && (cleanDamage.AttackType == WeaponAttackType.BaseAttack || cleanDamage.AttackType == WeaponAttackType.OffAttack) && damagetype == DamageEffectType.Direct && attacker != victim && attacker.DisplayPowerType == PowerType.Rage)
		{
			var rage = (uint)(attacker.GetBaseAttackTime(cleanDamage.AttackType) / 1000.0f * 1.75f);

			if (cleanDamage.AttackType == WeaponAttackType.OffAttack)
				rage /= 2;

			attacker.RewardRage(rage);
		}

		if (damageDone == 0)
			return 0;

		var health = (uint)victim.Health;

		// duel ends when player has 1 or less hp
		var duel_hasEnded = false;
		var duel_wasMounted = false;

		if (victim.IsPlayer && victim.AsPlayer.Duel != null && damageTaken >= (health - 1))
		{
			if (!attacker)
				return 0;

			// prevent kill only if killed in duel and killed by opponent or opponent controlled creature
			if (victim.AsPlayer.Duel.Opponent == attacker.GetControllingPlayer())
				damageTaken = health - 1;

			duel_hasEnded = true;
		}
		else if (victim.TryGetAsCreature(out var creature) && damageTaken >= health && creature.StaticFlags.HasFlag(CreatureStaticFlags.UNKILLABLE))
		{
			damageTaken = health - 1;
		}
		else if (victim.IsVehicle && damageTaken >= (health - 1) && victim.Charmer != null && victim.Charmer.IsTypeId(TypeId.Player))
		{
			var victimRider = victim.Charmer.AsPlayer;

			if (victimRider is { Duel: { IsMounted: true } })
			{
				if (!attacker)
					return 0;

				// prevent kill only if killed in duel and killed by opponent or opponent controlled creature
				if (victimRider.Duel.Opponent == attacker.GetControllingPlayer())
					damageTaken = health - 1;

				duel_wasMounted = true;
				duel_hasEnded = true;
			}
		}

		if (attacker != null && attacker != victim)
		{
			var killer = attacker.AsPlayer;

			if (killer != null)
			{
				// in bg, count dmg if victim is also a player
				if (victim.IsPlayer)
				{
					var bg = killer.Battleground;

					if (bg != null)
						bg.UpdatePlayerScore(killer, ScoreType.DamageDone, (uint)damageDone);
				}

				killer.UpdateCriteria(CriteriaType.DamageDealt, health > damageDone ? damageDone : health, 0, 0, victim);
				killer.UpdateCriteria(CriteriaType.HighestDamageDone, damageDone);
			}
		}

		if (victim.IsPlayer)
			victim.AsPlayer.UpdateCriteria(CriteriaType.HighestDamageTaken, damageTaken);

		if (victim.TypeId != TypeId.Player && (!victim.IsControlledByPlayer || victim.IsVehicle))
		{
			victim.AsCreature.SetTappedBy(attacker);

			if (attacker == null || attacker.IsControlledByPlayer)
				victim.AsCreature.LowerPlayerDamageReq(health < damageTaken ? health : damageTaken);
		}

		var killed = false;
		var skipSettingDeathState = false;

		if (health <= damageTaken)
		{
			killed = true;

			if (victim.IsPlayer && victim != attacker)
				victim.AsPlayer.UpdateCriteria(CriteriaType.TotalDamageTaken, health);

			if (damagetype != DamageEffectType.NoDamage && damagetype != DamageEffectType.Self && victim.HasAuraType(AuraType.SchoolAbsorbOverkill))
			{
				var vAbsorbOverkill = victim.GetAuraEffectsByType(AuraType.SchoolAbsorbOverkill);
				DamageInfo damageInfo = new(attacker, victim, damageTaken, spellProto, damageSchoolMask, damagetype, cleanDamage != null ? cleanDamage.AttackType : WeaponAttackType.BaseAttack);

				foreach (var absorbAurEff in vAbsorbOverkill)
				{
					var baseAura = absorbAurEff.Base;
					var aurApp = baseAura.GetApplicationOfTarget(victim.GUID);

					if (aurApp == null)
						continue;

					if ((absorbAurEff.MiscValue & (int)damageInfo.SchoolMask) == 0)
						continue;

					// cannot absorb over limit
					if (damageTaken >= victim.CountPctFromMaxHealth(100 + absorbAurEff.MiscValueB))
						continue;

					// get amount which can be still absorbed by the aura
					var currentAbsorb = absorbAurEff.Amount;

					// aura with infinite absorb amount - let the scripts handle absorbtion amount, set here to 0 for safety
					if (currentAbsorb < 0)
						currentAbsorb = 0;

					var tempAbsorb = currentAbsorb;

					// This aura type is used both by Spirit of Redemption (death not really prevented, must grant all credit immediately) and Cheat Death (death prevented)
					// repurpose PreventDefaultAction for this
					var deathFullyPrevented = false;

					absorbAurEff.Base.CallScriptEffectAbsorbHandlers(absorbAurEff, aurApp, damageInfo, ref tempAbsorb, ref deathFullyPrevented);
					currentAbsorb = tempAbsorb;

					// absorb must be smaller than the damage itself
					currentAbsorb = MathFunctions.RoundToInterval(ref currentAbsorb, 0, damageInfo.Damage);
					damageInfo.AbsorbDamage(currentAbsorb);

					if (deathFullyPrevented)
						killed = false;

					skipSettingDeathState = true;

					if (currentAbsorb != 0)
					{
						SpellAbsorbLog absorbLog = new()
                        {
                            Attacker = attacker != null ? attacker.GUID : ObjectGuid.Empty,
                            Victim = victim.GUID,
                            Caster = baseAura.CasterGuid,
                            AbsorbedSpellID = spellProto != null ? spellProto.Id : 0,
                            AbsorbSpellID = baseAura.Id,
                            Absorbed = (int)currentAbsorb,
                            OriginalDamage = (uint)damageInfo.OriginalDamage
                        };

                        absorbLog.LogData.Initialize(victim);
						victim.SendCombatLogMessage(absorbLog);
					}
				}

				damageTaken = damageInfo.Damage;
			}
		}

		if (spellProto != null && spellProto.HasAttribute(SpellAttr3.NoDurabilityLoss))
			durabilityLoss = false;

		if (killed)
		{
			Kill(attacker, victim, durabilityLoss, skipSettingDeathState);
		}
		else
		{
			if (victim.IsTypeId(TypeId.Player))
				victim.AsPlayer.UpdateCriteria(CriteriaType.TotalDamageTaken, damageTaken);

			victim.ModifyHealth(-(int)damageTaken);

			if (damagetype == DamageEffectType.Direct || damagetype == DamageEffectType.SpellDirect)
				victim.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.NonPeriodicDamage, spellProto);

			if (!victim.IsTypeId(TypeId.Player))
			{
				// Part of Evade mechanics. DoT's and Thorns / Retribution Aura do not contribute to this
				if (damagetype != DamageEffectType.DOT && damageTaken > 0 && !victim.OwnerGUID.IsPlayer && (spellProto == null || !spellProto.HasAura(AuraType.DamageShield)))
					victim.AsCreature.LastDamagedTime = GameTime.GetGameTime() + SharedConst.MaxAggroResetTime;

				if (attacker != null && (spellProto == null || !spellProto.HasAttribute(SpellAttr4.NoHarmfulThreat)))
					victim.GetThreatManager().AddThreat(attacker, damageTaken, spellProto);
			}
			else // victim is a player
			{
				// random durability for items (HIT TAKEN)
				if (durabilityLoss && WorldConfig.GetFloatValue(WorldCfg.RateDurabilityLossDamage) > RandomHelper.randChance())
				{
					var slot = (byte)RandomHelper.IRand(0, EquipmentSlot.End - 1);
					victim.AsPlayer.DurabilityPointLossForEquipSlot(slot);
				}
			}

			if (attacker is { IsPlayer: true })
				// random durability for items (HIT DONE)
				if (durabilityLoss && RandomHelper.randChance(WorldConfig.GetFloatValue(WorldCfg.RateDurabilityLossDamage)))
				{
					var slot = (byte)RandomHelper.IRand(0, EquipmentSlot.End - 1);
					attacker.AsPlayer.DurabilityPointLossForEquipSlot(slot);
				}

			if (damagetype != DamageEffectType.NoDamage && damagetype != DamageEffectType.DOT)
			{
				if (victim != attacker && (spellProto == null || !(spellProto.HasAttribute(SpellAttr6.NoPushback) || spellProto.HasAttribute(SpellAttr7.NoPushbackOnDamage) || spellProto.HasAttribute(SpellAttr3.TreatAsPeriodic))))
				{
					var spell = victim.GetCurrentSpell(CurrentSpellTypes.Generic);

					if (spell is { State: SpellState.Preparing })
                    {
                        bool isCastInterrupted()
                        {
                            if (damageTaken == 0)
                                return spell.SpellInfo.InterruptFlags.HasAnyFlag(SpellInterruptFlags.ZeroDamageCancels);

                            if (victim.IsPlayer && spell.SpellInfo.InterruptFlags.HasAnyFlag(SpellInterruptFlags.DamageCancelsPlayerOnly))
                                return true;

                            if (spell.SpellInfo.InterruptFlags.HasAnyFlag(SpellInterruptFlags.DamageCancels))
                                return true;

                            return false;
                        }

                        ;

                        bool isCastDelayed()
                        {
                            if (damageTaken == 0)
                                return false;

                            if (victim.IsPlayer && spell.SpellInfo.InterruptFlags.HasAnyFlag(SpellInterruptFlags.DamagePushbackPlayerOnly))
                                return true;

                            if (spell.SpellInfo.InterruptFlags.HasAnyFlag(SpellInterruptFlags.DamagePushback))
                                return true;

                            return false;
                        }

                        if (isCastInterrupted())
                            victim.InterruptNonMeleeSpells(false);
                        else if (isCastDelayed())
                            spell.Delayed();
                    }
                }

				if (damageTaken != 0 && victim.IsPlayer)
				{
					var spell1 = victim.GetCurrentSpell(CurrentSpellTypes.Channeled);

					if (spell1 != null)
						if (spell1.State == SpellState.Casting && spell1.SpellInfo.HasChannelInterruptFlag(SpellAuraInterruptFlags.DamageChannelDuration))
							spell1.DelayedChannel();
				}
			}

			// last damage from duel opponent
			if (duel_hasEnded)
			{
				var he = duel_wasMounted ? victim.Charmer.AsPlayer : victim.AsPlayer;

				if (duel_wasMounted) // In this case victim==mount
					victim.SetHealth(1);
				else
					he.SetHealth(1);

				he.Duel.Opponent.CombatStopWithPets(true);
				he.CombatStopWithPets(true);

				he.CastSpell(he, 7267, true); // beg
				he.DuelComplete(DuelCompleteType.Won);
			}
		}

		// logging uses damageDone
		if (victim.IsPlayer)
		{
			player = victim.AsPlayer;
			ScriptManager.Instance.ForEach<IPlayerOnTakeDamage>(player.Class, a => a.OnPlayerTakeDamage(player, damageDone, damageSchoolMask));
		}

		// make player victims stand up automatically
		if (victim.StandState != 0 && victim.IsPlayer)
			victim.SetStandState(UnitStandStateType.Stand);

		if (player != null)
			victim.SaveDamageHistory(damageDone);

		return damageDone;
	}

	public long ModifyHealth(double dval)
	{
		return ModifyHealth((long)dval);
	}

	public long ModifyHealth(long dVal)
	{
		long gain = 0;

		if (dVal == 0)
			return 0;

		lock (_healthLock)
		{
			var curHealth = Health;

			var val = dVal + curHealth;

			if (val <= 0)
			{
				SetHealth(0);

				return -curHealth;
			}

			var maxHealth = MaxHealth;

			if (val < maxHealth)
			{
				SetHealth(val);
				gain = val - curHealth;
			}
			else if (curHealth != maxHealth)
			{
				SetHealth(maxHealth);
				gain = maxHealth - curHealth;
			}
		}

		if (dVal < 0)
		{
			HealthUpdate packet = new()
            {
                Guid = GUID,
                Health = Health
            };

            var player = CharmerOrOwnerPlayerOrPlayerItself;

			if (player)
				player.SendPacket(packet);
		}

		return gain;
	}

	public long GetHealthGain(double dVal)
	{
		return GetHealthGain((long)dVal);
	}

	public long GetHealthGain(long dVal)
	{
		long gain = 0;

		if (dVal == 0)
			return 0;

		var curHealth = Health;

		var val = dVal + curHealth;

		if (val <= 0)
			return -curHealth;

		var maxHealth = MaxHealth;

		if (val < maxHealth)
			gain = dVal;
		else if (curHealth != maxHealth)
			gain = maxHealth - curHealth;

		return gain;
	}

	public bool IsImmuneToAll()
	{
		return IsImmuneToPC() && IsImmuneToNPC();
	}

	public void SetImmuneToAll(bool apply, bool keepCombat)
	{
		if (apply)
		{
			SetUnitFlag(UnitFlags.ImmuneToPc | UnitFlags.ImmuneToNpc);
			ValidateAttackersAndOwnTarget();

			if (!keepCombat)
				_combatManager.EndAllCombat();
		}
		else
		{
			RemoveUnitFlag(UnitFlags.ImmuneToPc | UnitFlags.ImmuneToNpc);
		}
	}

	public virtual void SetImmuneToAll(bool apply)
	{
		SetImmuneToAll(apply, false);
	}

	public bool IsImmuneToPC()
	{
		return HasUnitFlag(UnitFlags.ImmuneToPc);
	}

	public void SetImmuneToPC(bool apply, bool keepCombat)
	{
		if (apply)
		{
			SetUnitFlag(UnitFlags.ImmuneToPc);
			ValidateAttackersAndOwnTarget();

			if (!keepCombat)
			{
				List<CombatReference> toEnd = new();

				foreach (var pair in _combatManager.PvECombatRefs)
					if (pair.Value.GetOther(this).HasUnitFlag(UnitFlags.PlayerControlled))
						toEnd.Add(pair.Value);

				foreach (var pair in _combatManager.PvPCombatRefs)
					if (pair.Value.GetOther(this).HasUnitFlag(UnitFlags.PlayerControlled))
						toEnd.Add(pair.Value);

				foreach (var refe in toEnd)
					refe.EndCombat();
			}
		}
		else
		{
			RemoveUnitFlag(UnitFlags.ImmuneToPc);
		}
	}

	public virtual void SetImmuneToPC(bool apply)
	{
		SetImmuneToPC(apply, false);
	}

	public bool IsImmuneToNPC()
	{
		return HasUnitFlag(UnitFlags.ImmuneToNpc);
	}

	public void SetImmuneToNPC(bool apply, bool keepCombat)
	{
		if (apply)
		{
			SetUnitFlag(UnitFlags.ImmuneToNpc);
			ValidateAttackersAndOwnTarget();

			if (!keepCombat)
			{
				List<CombatReference> toEnd = new();

				foreach (var pair in _combatManager.PvECombatRefs)
					if (!pair.Value.GetOther(this).HasUnitFlag(UnitFlags.PlayerControlled))
						toEnd.Add(pair.Value);

				foreach (var pair in _combatManager.PvPCombatRefs)
					if (!pair.Value.GetOther(this).HasUnitFlag(UnitFlags.PlayerControlled))
						toEnd.Add(pair.Value);

				foreach (var refe in toEnd)
					refe.EndCombat();
			}
		}
		else
		{
			RemoveUnitFlag(UnitFlags.ImmuneToNpc);
		}
	}

	public virtual void SetImmuneToNPC(bool apply)
	{
		SetImmuneToNPC(apply, false);
	}

	public virtual float GetBlockPercent(uint attackerLevel)
	{
		return 30.0f;
	}

	public void RewardRage(uint baseRage)
	{
		double addRage = baseRage;

		// talent who gave more rage on attack
		MathFunctions.AddPct(ref addRage, GetTotalAuraModifier(AuraType.ModRageFromDamageDealt));

		addRage *= WorldConfig.GetFloatValue(WorldCfg.RatePowerRageIncome);

		ModifyPower(PowerType.Rage, (int)(addRage * 10));
	}

	public float GetPPMProcChance(uint WeaponSpeed, float PPM, SpellInfo spellProto)
	{
		// proc per minute chance calculation
		if (PPM <= 0)
			return 0.0f;

		// Apply chance modifer aura
		if (spellProto != null)
		{
			var modOwner = SpellModOwner;

			if (modOwner != null)
				modOwner.ApplySpellMod(spellProto, SpellModOp.ProcFrequency, ref PPM);
		}

		return (float)Math.Floor((WeaponSpeed * PPM) / 600.0f); // result is chance in percents (probability = Speed_in_sec * (PPM / 60))
	}

	public Unit GetNextRandomRaidMemberOrPet(float radius)
	{
		Player player = null;

		if (IsTypeId(TypeId.Player))
			player = AsPlayer;
		// Should we enable this also for charmed units?
		else if (IsTypeId(TypeId.Unit) && IsPet)
			player = OwnerUnit.AsPlayer;

		if (player == null)
			return null;

		var group = player.Group;

		// When there is no group check pet presence
		if (!group)
		{
			// We are pet now, return owner
			if (player != this)
				return IsWithinDistInMap(player, radius) ? player : null;

			Unit pet = GetGuardianPet();

			// No pet, no group, nothing to return
			if (pet == null)
				return null;

			// We are owner now, return pet
			return IsWithinDistInMap(pet, radius) ? pet : null;
		}

		List<Unit> nearMembers = new();
		// reserve place for players and pets because resizing vector every unit push is unefficient (vector is reallocated then)

		for (var refe = group.FirstMember; refe != null; refe = refe.Next())
		{
			var target = refe.Source;

			if (target)
			{
				// IsHostileTo check duel and controlled by enemy
				if (target != this && IsWithinDistInMap(target, radius) && target.IsAlive && !IsHostileTo(target))
					nearMembers.Add(target);

				// Push player's pet to vector
				Unit pet = target.GetGuardianPet();

				if (pet)
					if (pet != this && IsWithinDistInMap(pet, radius) && pet.IsAlive && !IsHostileTo(pet))
						nearMembers.Add(pet);
			}
		}

		if (nearMembers.Empty())
			return null;

		var randTarget = RandomHelper.IRand(0, nearMembers.Count - 1);

		return nearMembers[randTarget];
	}

	public uint GetComboPoints()
	{
		return (uint)GetPower(PowerType.ComboPoints);
	}

	public void AddComboPoints(sbyte count, Spell spell = null)
	{
		if (count == 0)
			return;

		var comboPoints = (sbyte)(spell != null ? spell.ComboPointGain : GetPower(PowerType.ComboPoints));

		comboPoints += count;

		if (comboPoints > 5)
			comboPoints = 5;
		else if (comboPoints < 0)
			comboPoints = 0;

		if (!spell)
			SetPower(PowerType.ComboPoints, comboPoints);
		else
			spell.ComboPointGain = comboPoints;
	}

	public void ClearComboPoints()
	{
		SetPower(PowerType.ComboPoints, 0);
	}

	public void ClearAllReactives()
	{
		for (ReactiveType i = 0; i < ReactiveType.Max; ++i)
			_reactiveTimer[i] = 0;

		if (HasAuraState(AuraStateType.Defensive))
			ModifyAuraState(AuraStateType.Defensive, false);

		if (HasAuraState(AuraStateType.Defensive2))
			ModifyAuraState(AuraStateType.Defensive2, false);
	}

	public virtual void SetPvP(bool state)
	{
		if (state)
			SetPvpFlag(UnitPVPStateFlags.PvP);
		else
			RemovePvpFlag(UnitPVPStateFlags.PvP);
	}

	public static void CalcAbsorbResist(DamageInfo damageInfo, Spell spell = null)
	{
		if (!damageInfo.Victim || !damageInfo.Victim.IsAlive || damageInfo.Damage == 0)
			return;

		var resistedDamage = CalcSpellResistedDamage(damageInfo.Attacker, damageInfo.Victim, damageInfo.Damage, damageInfo.SchoolMask, damageInfo.SpellInfo);

		// Ignore Absorption Auras
		double auraAbsorbMod = 0f;

		var attacker = damageInfo.Attacker;

		if (attacker != null)
			auraAbsorbMod = attacker.GetMaxPositiveAuraModifierByMiscMask(AuraType.ModTargetAbsorbSchool, (uint)damageInfo.SchoolMask);

		MathFunctions.RoundToInterval(ref auraAbsorbMod, 0.0f, 100.0f);

		var absorbIgnoringDamage = MathFunctions.CalculatePct(damageInfo.Damage, auraAbsorbMod);

		if (spell != null)
			spell.CallScriptOnResistAbsorbCalculateHandlers(damageInfo, ref resistedDamage, ref absorbIgnoringDamage);

		damageInfo.ResistDamage(resistedDamage);

		// We're going to call functions which can modify content of the list during iteration over it's elements
		// Let's copy the list so we can prevent iterator invalidation
		var vSchoolAbsorbCopy = damageInfo.Victim.GetAuraEffectsByType(AuraType.SchoolAbsorb);
		vSchoolAbsorbCopy.Sort(new AbsorbAuraOrderPred());

		// absorb without mana cost
		for (var i = 0; i < vSchoolAbsorbCopy.Count && (damageInfo.Damage > 0); ++i)
		{
			var absorbAurEff = vSchoolAbsorbCopy[i];

			// Check if aura was removed during iteration - we don't need to work on such auras
			var aurApp = absorbAurEff.Base.GetApplicationOfTarget(damageInfo.Victim.GUID);

			if (aurApp == null)
				continue;

			if ((absorbAurEff.MiscValue & (int)damageInfo.SchoolMask) == 0)
				continue;

			// get amount which can be still absorbed by the aura
			var currentAbsorb = absorbAurEff.Amount;

			// aura with infinite absorb amount - let the scripts handle absorbtion amount, set here to 0 for safety
			if (currentAbsorb < 0)
				currentAbsorb = 0;

			if (!absorbAurEff.SpellInfo.HasAttribute(SpellAttr6.AbsorbCannotBeIgnore))
				damageInfo.ModifyDamage(-absorbIgnoringDamage);

			var tempAbsorb = currentAbsorb;

			var defaultPrevented = false;

			absorbAurEff.Base.CallScriptEffectAbsorbHandlers(absorbAurEff, aurApp, damageInfo, ref tempAbsorb, ref defaultPrevented);
			currentAbsorb = (int)tempAbsorb;

			if (!defaultPrevented)
			{
				// absorb must be smaller than the damage itself
				currentAbsorb = MathFunctions.RoundToInterval(ref currentAbsorb, 0, damageInfo.Damage);

				damageInfo.AbsorbDamage(currentAbsorb);

				tempAbsorb = (uint)currentAbsorb;
				absorbAurEff.Base.CallScriptEffectAfterAbsorbHandlers(absorbAurEff, aurApp, damageInfo, ref tempAbsorb);

				// Check if our aura is using amount to count heal
				if (absorbAurEff.Amount >= 0)
				{
					// Reduce shield amount
					absorbAurEff.ChangeAmount(absorbAurEff.Amount - currentAbsorb);

					// Aura cannot absorb anything more - remove it
					if (absorbAurEff.Amount <= 0 && !absorbAurEff.Base.SpellInfo.HasAttribute(SpellAttr0.Passive))
						absorbAurEff.Base.Remove(AuraRemoveMode.EnemySpell);
				}
			}

			if (!absorbAurEff.SpellInfo.HasAttribute(SpellAttr6.AbsorbCannotBeIgnore))
				damageInfo.ModifyDamage(absorbIgnoringDamage);

			if (currentAbsorb != 0)
			{
				SpellAbsorbLog absorbLog = new()
                {
                    Attacker = damageInfo.Attacker != null ? damageInfo.Attacker.GUID : ObjectGuid.Empty,
                    Victim = damageInfo.Victim.GUID,
                    Caster = absorbAurEff.Base.CasterGuid,
                    AbsorbedSpellID = damageInfo.SpellInfo != null ? damageInfo.SpellInfo.Id : 0,
                    AbsorbSpellID = absorbAurEff.Id,
                    Absorbed = (int)currentAbsorb,
                    OriginalDamage = (uint)damageInfo.OriginalDamage
                };

                absorbLog.LogData.Initialize(damageInfo.Victim);
				damageInfo.Victim.SendCombatLogMessage(absorbLog);
			}
		}

		// absorb by mana cost
		var vManaShieldCopy = damageInfo.Victim.GetAuraEffectsByType(AuraType.ManaShield);

		foreach (var absorbAurEff in vManaShieldCopy)
		{
			if (damageInfo.Damage == 0)
				break;

			// Check if aura was removed during iteration - we don't need to work on such auras
			var aurApp = absorbAurEff.Base.GetApplicationOfTarget(damageInfo.Victim.GUID);

			if (aurApp == null)
				continue;

			// check damage school mask
			if (!Convert.ToBoolean(absorbAurEff.MiscValue & (int)damageInfo.SchoolMask))
				continue;

			// get amount which can be still absorbed by the aura
			var currentAbsorb = absorbAurEff.Amount;

			// aura with infinite absorb amount - let the scripts handle absorbtion amount, set here to 0 for safety
			if (currentAbsorb < 0)
				currentAbsorb = 0;

			if (!absorbAurEff.SpellInfo.HasAttribute(SpellAttr6.AbsorbCannotBeIgnore))
				damageInfo.ModifyDamage(-absorbIgnoringDamage);

			var tempAbsorb = currentAbsorb;

			var defaultPrevented = false;

			absorbAurEff.Base.CallScriptEffectManaShieldHandlers(absorbAurEff, aurApp, damageInfo, ref tempAbsorb, ref defaultPrevented);
			currentAbsorb = (int)tempAbsorb;

			if (!defaultPrevented)
			{
				// absorb must be smaller than the damage itself
				currentAbsorb = MathFunctions.RoundToInterval(ref currentAbsorb, 0, damageInfo.Damage);

				var manaReduction = currentAbsorb;

				// lower absorb amount by talents
				var manaMultiplier = absorbAurEff.GetSpellEffectInfo().CalcValueMultiplier(absorbAurEff.Caster);

				if (manaMultiplier != 0)
					manaReduction = (int)(manaReduction * manaMultiplier);

				var manaTaken = -damageInfo.Victim.ModifyPower(PowerType.Mana, -manaReduction);

				// take case when mana has ended up into account
				currentAbsorb = currentAbsorb != 0 ? (currentAbsorb * (manaTaken / manaReduction)) : 0;

				damageInfo.AbsorbDamage((uint)currentAbsorb);

				tempAbsorb = (uint)currentAbsorb;
				absorbAurEff.Base.CallScriptEffectAfterManaShieldHandlers(absorbAurEff, aurApp, damageInfo, ref tempAbsorb);

				// Check if our aura is using amount to count damage
				if (absorbAurEff.Amount >= 0)
				{
					absorbAurEff.ChangeAmount(absorbAurEff.Amount - currentAbsorb);

					if ((absorbAurEff.Amount <= 0))
						absorbAurEff.Base.Remove(AuraRemoveMode.EnemySpell);
				}
			}

			if (!absorbAurEff.SpellInfo.HasAttribute(SpellAttr6.AbsorbCannotBeIgnore))
				damageInfo.ModifyDamage(absorbIgnoringDamage);

			if (currentAbsorb != 0)
			{
				SpellAbsorbLog absorbLog = new()
                {
                    Attacker = damageInfo.Attacker != null ? damageInfo.Attacker.GUID : ObjectGuid.Empty,
                    Victim = damageInfo.Victim.GUID,
                    Caster = absorbAurEff.Base.CasterGuid,
                    AbsorbedSpellID = damageInfo.SpellInfo != null ? damageInfo.SpellInfo.Id : 0,
                    AbsorbSpellID = absorbAurEff.Id,
                    Absorbed = (int)currentAbsorb,
                    OriginalDamage = (uint)damageInfo.OriginalDamage
                };

                absorbLog.LogData.Initialize(damageInfo.Victim);
				damageInfo.Victim.SendCombatLogMessage(absorbLog);
			}
		}

		// split damage auras - only when not damaging self
		if (damageInfo.Victim != damageInfo.Attacker)
		{
			// We're going to call functions which can modify content of the list during iteration over it's elements
			// Let's copy the list so we can prevent iterator invalidation
			var vSplitDamagePctCopy = damageInfo.Victim.GetAuraEffectsByType(AuraType.SplitDamagePct);

			foreach (var itr in vSplitDamagePctCopy)
			{
				if (damageInfo.Damage == 0)
					break;

				// Check if aura was removed during iteration - we don't need to work on such auras
				var aurApp = itr.Base.GetApplicationOfTarget(damageInfo.Victim.GUID);

				if (aurApp == null)
					continue;

				// check damage school mask
				if (!Convert.ToBoolean(itr.MiscValue & (int)damageInfo.SchoolMask))
					continue;

				// Damage can be splitted only if aura has an alive caster
				var caster = itr.Caster;

				if (!caster || (caster == damageInfo.Victim) || !caster.IsInWorld || !caster.IsAlive)
					continue;

				var splitDamage = MathFunctions.CalculatePct(damageInfo.Damage, itr.Amount);

				itr.Base.CallScriptEffectSplitHandlers(itr, aurApp, damageInfo, ref splitDamage);

				// absorb must be smaller than the damage itself
				splitDamage = MathFunctions.RoundToInterval(ref splitDamage, 0, damageInfo.Damage);

				damageInfo.AbsorbDamage(splitDamage);

				// check if caster is immune to damage
				if (caster.IsImmunedToDamage(damageInfo.SchoolMask))
				{
					damageInfo.Victim.SendSpellMiss(caster, itr.SpellInfo.Id, SpellMissInfo.Immune);

					continue;
				}

				double split_absorb = 0;
				DealDamageMods(damageInfo.Attacker, caster, ref splitDamage, ref split_absorb);

				SpellNonMeleeDamage log = new(damageInfo.Attacker, caster, itr.SpellInfo, itr.Base.SpellVisual, damageInfo.SchoolMask, itr.Base.CastId);
				CleanDamage cleanDamage = new(splitDamage, 0, WeaponAttackType.BaseAttack, MeleeHitOutcome.Normal);
				splitDamage = DealDamage(damageInfo.Attacker, caster, splitDamage, cleanDamage, DamageEffectType.Direct, damageInfo.SchoolMask, itr.SpellInfo, false);
				log.Damage = splitDamage;
				log.OriginalDamage = splitDamage;
				log.Absorb = split_absorb;
				log.HitInfo |= (int)SpellHitType.Split;

				caster.SendSpellNonMeleeDamageLog(log);

				// break 'Fear' and similar auras
				ProcSkillsAndAuras(damageInfo.Attacker, caster, new ProcFlagsInit(ProcFlags.None), new ProcFlagsInit(ProcFlags.TakeHarmfulSpell), ProcFlagsSpellType.Damage, ProcFlagsSpellPhase.Hit, ProcFlagsHit.None, null, damageInfo, null);
			}
		}
	}

	public static void CalcHealAbsorb(HealInfo healInfo)
	{
		if (healInfo.Heal == 0)
			return;

		// Need remove expired auras after
		var existExpired = false;

		// absorb without mana cost
		var vHealAbsorb = healInfo.Target.GetAuraEffectsByType(AuraType.SchoolHealAbsorb);

		for (var i = 0; i < vHealAbsorb.Count && healInfo.Heal > 0; ++i)
		{
			var absorbAurEff = vHealAbsorb[i];
			// Check if aura was removed during iteration - we don't need to work on such auras
			var aurApp = absorbAurEff.Base.GetApplicationOfTarget(healInfo.Target.GUID);

			if (aurApp == null)
				continue;

			if ((absorbAurEff.MiscValue & (int)healInfo.SchoolMask) == 0)
				continue;

			// get amount which can be still absorbed by the aura
			var currentAbsorb = absorbAurEff.Amount;

			// aura with infinite absorb amount - let the scripts handle absorbtion amount, set here to 0 for safety
			if (currentAbsorb < 0)
				currentAbsorb = 0;

			var tempAbsorb = currentAbsorb;

			var defaultPrevented = false;

			absorbAurEff.Base.CallScriptEffectAbsorbHandlers(absorbAurEff, aurApp, healInfo, ref tempAbsorb, ref defaultPrevented);
			currentAbsorb = tempAbsorb;

			if (!defaultPrevented)
			{
				// absorb must be smaller than the heal itself
				currentAbsorb = MathFunctions.RoundToInterval(ref currentAbsorb, 0, healInfo.Heal);

				healInfo.AbsorbHeal((uint)currentAbsorb);

				tempAbsorb = currentAbsorb;
				absorbAurEff.Base.CallScriptEffectAfterAbsorbHandlers(absorbAurEff, aurApp, healInfo, ref tempAbsorb);

				// Check if our aura is using amount to count heal
				if (absorbAurEff.Amount >= 0)
				{
					// Reduce shield amount
					absorbAurEff.ChangeAmount(absorbAurEff.Amount - currentAbsorb);

					// Aura cannot absorb anything more - remove it
					if (absorbAurEff.Amount <= 0)
						existExpired = true;
				}
			}

			if (currentAbsorb != 0)
			{
				SpellHealAbsorbLog absorbLog = new()
                {
                    Healer = healInfo.Healer ? healInfo.Healer.GUID : ObjectGuid.Empty,
                    Target = healInfo.Target.GUID,
                    AbsorbCaster = absorbAurEff.Base.CasterGuid,
                    AbsorbedSpellID = (int)(healInfo.SpellInfo != null ? healInfo.SpellInfo.Id : 0),
                    AbsorbSpellID = (int)absorbAurEff.Id,
                    Absorbed = (int)currentAbsorb,
                    OriginalHeal = (int)healInfo.OriginalHeal
                };

                healInfo.Target.SendMessageToSet(absorbLog, true);
			}
		}

		// Remove all expired absorb auras
		if (existExpired)
			for (var i = 0; i < vHealAbsorb.Count;)
			{
				var auraEff = vHealAbsorb[i];
				++i;

				if (auraEff.Amount <= 0)
				{
					var removedAuras = healInfo.Target._removedAurasCount;
					auraEff.Base.Remove(AuraRemoveMode.EnemySpell);

					if (removedAuras + 1 < healInfo.Target._removedAurasCount)
						i = 0;
				}
			}
	}

	public static double CalcArmorReducedDamage(Unit attacker, Unit victim, double damage, SpellInfo spellInfo, WeaponAttackType attackType = WeaponAttackType.Max, uint attackerLevel = 0)
	{
		double armor = victim.GetArmor();

		if (attacker != null)
		{
			armor *= victim.GetArmorMultiplierForTarget(attacker);

			// bypass enemy armor by SPELL_AURA_BYPASS_ARMOR_FOR_CASTER
			double armorBypassPct = 0;
			var reductionAuras = victim.GetAuraEffectsByType(AuraType.BypassArmorForCaster);

			foreach (var eff in reductionAuras)
				if (eff.CasterGuid == attacker.GUID)
					armorBypassPct += eff.Amount;

			armor = MathFunctions.CalculatePct(armor, 100 - Math.Min(armorBypassPct, 100));

			// Ignore enemy armor by SPELL_AURA_MOD_TARGET_RESISTANCE aura
			armor += attacker.GetTotalAuraModifierByMiscMask(AuraType.ModTargetResistance, (int)SpellSchoolMask.Normal);

			if (spellInfo != null)
			{
				var modOwner = attacker.SpellModOwner;

				if (modOwner != null)
					modOwner.ApplySpellMod(spellInfo, SpellModOp.TargetResistance, ref armor);
			}

			var resIgnoreAuras = attacker.GetAuraEffectsByType(AuraType.ModIgnoreTargetResist);

			foreach (var eff in resIgnoreAuras)
				if (eff.MiscValue.HasAnyFlag((int)SpellSchoolMask.Normal) && eff.IsAffectingSpell(spellInfo))
					armor = (float)Math.Floor(MathFunctions.AddPct(ref armor, -eff.Amount));

			// Apply Player CR_ARMOR_PENETRATION rating
			if (attacker.IsPlayer)
			{
				var arpPct = attacker.AsPlayer.GetRatingBonusValue(CombatRating.ArmorPenetration);

				// no more than 100%
				MathFunctions.RoundToInterval(ref arpPct, 0.0f, 100.0f);

				double maxArmorPen;

				if (victim.GetLevelForTarget(attacker) < 60)
					maxArmorPen = 400 + 85 * victim.GetLevelForTarget(attacker);
				else
					maxArmorPen = 400 + 85 * victim.GetLevelForTarget(attacker) + 4.5f * 85 * (victim.GetLevelForTarget(attacker) - 59);

				// Cap armor penetration to this number
				maxArmorPen = Math.Min((armor + maxArmorPen) / 3.0f, armor);
				// Figure out how much armor do we ignore
				armor -= MathFunctions.CalculatePct(maxArmorPen, arpPct);
			}
		}

		if (MathFunctions.fuzzyLe(armor, 0.0f))
			return damage;

		var attackerClass = PlayerClass.Warrior;

		if (attacker != null)
		{
			attackerLevel = attacker.GetLevelForTarget(victim);
			attackerClass = attacker.Class;
		}

		// Expansion and ContentTuningID necessary? Does Player get a ContentTuningID too ?
		var armorConstant = Global.DB2Mgr.EvaluateExpectedStat(ExpectedStatType.ArmorConstant, attackerLevel, -2, 0, attackerClass);

		if ((armor + armorConstant) == 0)
			return damage;

		var mitigation = Math.Min(armor / (armor + armorConstant), 0.85f);

		return Math.Max(damage * (1.0f - mitigation), 0.0f);
	}

	public double MeleeDamageBonusDone(Unit victim, double damage, WeaponAttackType attType, DamageEffectType damagetype, SpellInfo spellProto = null, SpellEffectInfo spellEffectInfo = null, SpellSchoolMask damageSchoolMask = SpellSchoolMask.Normal)
	{
		if (victim == null || damage == 0)
			return 0;

		var creatureTypeMask = victim.CreatureTypeMask;

		// Done fixed damage bonus auras
		double DoneFlatBenefit = 0;

		// ..done
		DoneFlatBenefit += GetTotalAuraModifierByMiscMask(AuraType.ModDamageDoneCreature, (int)creatureTypeMask);

		// ..done
		// SPELL_AURA_MOD_DAMAGE_DONE included in weapon damage

		// ..done (base at attack power for marked target and base at attack power for creature type)
		double APbonus = 0;

		if (attType == WeaponAttackType.RangedAttack)
		{
			APbonus += victim.GetTotalAuraModifier(AuraType.RangedAttackPowerAttackerBonus);

			// ..done (base at attack power and creature type)
			APbonus += GetTotalAuraModifierByMiscMask(AuraType.ModRangedAttackPowerVersus, (int)creatureTypeMask);
		}
		else
		{
			APbonus += victim.GetTotalAuraModifier(AuraType.MeleeAttackPowerAttackerBonus);

			// ..done (base at attack power and creature type)
			APbonus += GetTotalAuraModifierByMiscMask(AuraType.ModMeleeAttackPowerVersus, (int)creatureTypeMask);
		}

		if (APbonus != 0) // Can be negative
		{
			var normalized = spellProto != null && spellProto.HasEffect(SpellEffectName.NormalizedWeaponDmg);
			DoneFlatBenefit += (int)(APbonus / 3.5f * GetAPMultiplier(attType, normalized));
		}

		// Done total percent damage auras
		double DoneTotalMod = 1.0f;

		var schoolMask = spellProto != null ? spellProto.GetSchoolMask() : damageSchoolMask;

		if ((schoolMask & SpellSchoolMask.Normal) == 0)
			// Some spells don't benefit from pct done mods
			// mods for SPELL_SCHOOL_MASK_NORMAL are already factored in base melee damage calculation
			if (spellProto == null || !spellProto.HasAttribute(SpellAttr6.IgnoreCasterDamageModifiers))
			{
				double maxModDamagePercentSchool = 0.0f;
				var thisPlayer = AsPlayer;

				if (thisPlayer != null)
				{
					for (var i = SpellSchools.Holy; i < SpellSchools.Max; ++i)
						if (Convert.ToBoolean((int)schoolMask & (1 << (int)i)))
							maxModDamagePercentSchool = Math.Max(maxModDamagePercentSchool, thisPlayer.ActivePlayerData.ModDamageDonePercent[(int)i]);
				}
				else
				{
					maxModDamagePercentSchool = GetTotalAuraMultiplierByMiscMask(AuraType.ModDamagePercentDone, (uint)schoolMask);
				}

				DoneTotalMod *= maxModDamagePercentSchool;
			}

		if (spellProto == null)
			// melee attack
			foreach (var autoAttackDamage in GetAuraEffectsByType(AuraType.ModAutoAttackDamage))
				MathFunctions.AddPct(ref DoneTotalMod, autoAttackDamage.Amount);

		DoneTotalMod *= GetTotalAuraMultiplierByMiscMask(AuraType.ModDamageDoneVersus, creatureTypeMask);

		// bonus against aurastate
		DoneTotalMod *= GetTotalAuraMultiplier(AuraType.ModDamageDoneVersusAurastate,
												aurEff =>
												{
													if (victim.HasAuraState((AuraStateType)aurEff.MiscValue))
														return true;

													return false;
												});

		// Add SPELL_AURA_MOD_DAMAGE_DONE_FOR_MECHANIC percent bonus
		if (spellEffectInfo != null && spellEffectInfo.Mechanic != 0)
			MathFunctions.AddPct(ref DoneTotalMod, GetTotalAuraModifierByMiscValue(AuraType.ModDamageDoneForMechanic, (int)spellEffectInfo.Mechanic));
		else if (spellProto != null && spellProto.Mechanic != 0)
			MathFunctions.AddPct(ref DoneTotalMod, GetTotalAuraModifierByMiscValue(AuraType.ModDamageDoneForMechanic, (int)spellProto.Mechanic));

		var damageF = damage;

		// apply spellmod to Done damage
		if (spellProto != null)
		{
			var modOwner = SpellModOwner;

			if (modOwner != null)
				modOwner.ApplySpellMod(spellProto, damagetype == DamageEffectType.DOT ? SpellModOp.PeriodicHealingAndDamage : SpellModOp.HealingAndDamage, ref damageF);
		}

		damageF = (damageF + DoneFlatBenefit) * DoneTotalMod;

		// bonus result can be negative
		return Math.Max(damageF, 0.0f);
	}

	public double MeleeDamageBonusTaken(Unit attacker, double pdamage, WeaponAttackType attType, DamageEffectType damagetype, SpellInfo spellProto = null, SpellSchoolMask damageSchoolMask = SpellSchoolMask.Normal)
	{
		if (pdamage == 0)
			return 0;

		double TakenFlatBenefit = 0;

		// ..taken
		TakenFlatBenefit += GetTotalAuraModifierByMiscMask(AuraType.ModDamageTaken, (int)attacker.GetMeleeDamageSchoolMask());

		if (attType != WeaponAttackType.RangedAttack)
			TakenFlatBenefit += GetTotalAuraModifier(AuraType.ModMeleeDamageTaken);
		else
			TakenFlatBenefit += GetTotalAuraModifier(AuraType.ModRangedDamageTaken);

		if ((TakenFlatBenefit < 0) && (pdamage < -TakenFlatBenefit))
			return 0;

		// Taken total percent damage auras
		double TakenTotalMod = 1.0f;

		// ..taken
		TakenTotalMod *= GetTotalAuraMultiplierByMiscMask(AuraType.ModDamagePercentTaken, (uint)attacker.GetMeleeDamageSchoolMask());

		// .. taken pct (special attacks)
		if (spellProto != null)
		{
			// From caster spells
			TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModSchoolMaskDamageFromCaster, aurEff => { return aurEff.CasterGuid == attacker.GUID && (aurEff.MiscValue & (int)spellProto.GetSchoolMask()) != 0; });

			TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModSpellDamageFromCaster, aurEff => { return aurEff.CasterGuid == attacker.GUID && aurEff.IsAffectingSpell(spellProto); });

			// Mod damage from spell mechanic
			var mechanicMask = spellProto.GetAllEffectsMechanicMask();

			// Shred, Maul - "Effects which increase Bleed damage also increase Shred damage"
			if (spellProto.SpellFamilyName == SpellFamilyNames.Druid && spellProto.SpellFamilyFlags[0].HasAnyFlag(0x00008800u))
				mechanicMask |= (1 << (int)Mechanics.Bleed);

			if (mechanicMask != 0)
				TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModMechanicDamageTakenPercent,
														aurEff =>
														{
															if ((mechanicMask & (1ul << (aurEff.MiscValue))) != 0)
																return true;

															return false;
														});

			if (damagetype == DamageEffectType.DOT)
				TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModPeriodicDamageTaken, aurEff => (aurEff.MiscValue & (uint)spellProto.GetSchoolMask()) != 0);
		}
		else // melee attack
		{
			TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModMeleeDamageFromCaster, aurEff => { return aurEff.CasterGuid == attacker.GUID; });
		}

		var cheatDeath = GetAuraEffect(45182, 0);

		if (cheatDeath != null)
			MathFunctions.AddPct(ref TakenTotalMod, cheatDeath.Amount);

		if (attType != WeaponAttackType.RangedAttack)
			TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModMeleeDamageTakenPct);
		else
			TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModRangedDamageTakenPct);

		// Versatility
		var modOwner = SpellModOwner;

		if (modOwner)
		{
			// only 50% of SPELL_AURA_MOD_VERSATILITY for damage reduction
			var versaBonus = modOwner.GetTotalAuraModifier(AuraType.ModVersatility) / 2.0f;
			MathFunctions.AddPct(ref TakenTotalMod, -(modOwner.GetRatingBonusValue(CombatRating.VersatilityDamageTaken) + versaBonus));
		}

		// Sanctified Wrath (bypass damage reduction)
		if (TakenTotalMod < 1.0f)
		{
			var attackSchoolMask = spellProto != null ? spellProto.GetSchoolMask() : damageSchoolMask;

			var damageReduction = 1.0f - TakenTotalMod;
			var casterIgnoreResist = attacker.GetAuraEffectsByType(AuraType.ModIgnoreTargetResist);

			foreach (var aurEff in casterIgnoreResist)
			{
				if ((aurEff.MiscValue & (int)attackSchoolMask) == 0)
					continue;

				MathFunctions.AddPct(ref damageReduction, -aurEff.Amount);
			}

			TakenTotalMod = 1.0f - damageReduction;
		}

		var tmpDamage = (pdamage + TakenFlatBenefit) * TakenTotalMod;

		return Math.Max(tmpDamage, 0.0f);
	}


	public void SaveDamageHistory(double damage)
	{
		var currentTime = GameTime.GetDateAndTime();
		var maxPastTime = currentTime - MAX_DAMAGE_HISTORY_DURATION;

		// Remove damages older than maxPastTime, can be increased if required
		foreach (var kvp in DamageTakenHistory)
			if (kvp.Key < maxPastTime)
				DamageTakenHistory.QueueRemove(kvp.Key);
			else
				break;

		DamageTakenHistory.ExecuteRemove();

		DamageTakenHistory.TryGetValue(currentTime, out var existing);
		DamageTakenHistory[currentTime] = existing + damage;
	}

	public double GetDamageOverLastSeconds(uint seconds)
	{
		var maxPastTime = GameTime.GetDateAndTime() - TimeSpan.FromSeconds(seconds);

		double damageOverLastSeconds = 0;

		foreach (var itr in DamageTakenHistory)
			if (itr.Key >= maxPastTime)
				damageOverLastSeconds += itr.Value;
			else
				break;

		return damageOverLastSeconds;
	}

	public virtual SpellSchoolMask GetMeleeDamageSchoolMask(WeaponAttackType attackType = WeaponAttackType.BaseAttack)
	{
		return SpellSchoolMask.None;
	}

	public virtual void UpdateDamageDoneMods(WeaponAttackType attackType, int skipEnchantSlot = -1)
	{
		var unitMod = attackType switch
		{
			WeaponAttackType.BaseAttack   => UnitMods.DamageMainHand,
			WeaponAttackType.OffAttack    => UnitMods.DamageOffHand,
			WeaponAttackType.RangedAttack => UnitMods.DamageRanged,
			_                             => throw new NotImplementedException(),
		};

		var amount = GetTotalAuraModifier(AuraType.ModDamageDone,
										aurEff =>
										{
											if ((aurEff.MiscValue & (int)SpellSchoolMask.Normal) == 0)
												return false;

											return CheckAttackFitToAuraRequirement(attackType, aurEff);
										});

		SetStatFlatModifier(unitMod, UnitModifierFlatType.Total, amount);
	}

	public void UpdateAllDamageDoneMods()
	{
		for (var attackType = WeaponAttackType.BaseAttack; attackType < WeaponAttackType.Max; ++attackType)
			UpdateDamageDoneMods(attackType);
	}

	public void UpdateDamagePctDoneMods(WeaponAttackType attackType)
	{
		(UnitMods unitMod, double factor) = attackType switch
		{
			WeaponAttackType.BaseAttack   => (UnitMods.DamageMainHand, 1.0f),
			WeaponAttackType.OffAttack    => (UnitMods.DamageOffHand, 0.5f),
			WeaponAttackType.RangedAttack => (UnitMods.DamageRanged, 1.0f),
			_                             => throw new NotImplementedException(),
		};

		factor *= GetTotalAuraMultiplier(AuraType.ModDamagePercentDone,
										aurEff =>
										{
											if (!aurEff.MiscValue.HasAnyFlag((int)SpellSchoolMask.Normal))
												return false;

											return CheckAttackFitToAuraRequirement(attackType, aurEff);
										});

		if (attackType == WeaponAttackType.OffAttack)
			factor *= GetTotalAuraMultiplier(AuraType.ModOffhandDamagePct, auraEffect => CheckAttackFitToAuraRequirement(attackType, auraEffect));

		SetStatPctModifier(unitMod, UnitModifierPctType.Total, factor);
	}

	public void UpdateAllDamagePctDoneMods()
	{
		for (var attackType = WeaponAttackType.BaseAttack; attackType < WeaponAttackType.Max; ++attackType)
			UpdateDamagePctDoneMods(attackType);
	}

	public void GetAnyUnitListInRange(List<Unit> list, float fMaxSearchRange)
	{
		var p = new CellCoord(GridDefines.ComputeCellCoord(Location.X, Location.Y));
		var cell = new Cell(p);
		cell.SetNoCreate();

		var u_check = new AnyUnitInObjectRangeCheck(this, fMaxSearchRange);
		var searcher = new UnitListSearcher(this, list, u_check, GridType.All);

		cell.Visit(p, searcher, Map, this, fMaxSearchRange);
	}

	public void GetAttackableUnitListInRange(List<Unit> list, float fMaxSearchRange)
	{
		var p = new CellCoord(GridDefines.ComputeCellCoord(Location.X, Location.Y));
		var cell = new Cell(p);
		cell.SetNoCreate();

		var u_check = new NearestAttackableUnitInObjectRangeCheck(this, this, fMaxSearchRange);
		var searcher = new UnitListSearcher(this, list, u_check, GridType.All);

		cell.Visit(p, searcher, Map, this, fMaxSearchRange);
	}

	public void GetFriendlyUnitListInRange(List<Unit> list, float fMaxSearchRange, bool exceptSelf = false)
	{
		var p = new CellCoord(GridDefines.ComputeCellCoord(Location.X, Location.Y));
		var cell = new Cell(p);
		cell.SetNoCreate();

		var u_check = new AnyFriendlyUnitInObjectRangeCheck(this, this, fMaxSearchRange, false, exceptSelf);
		var searcher = new UnitListSearcher(this, list, u_check, GridType.All);

		cell.Visit(p, searcher, Map, this, fMaxSearchRange);
	}

	public CombatManager GetCombatManager()
	{
		return _combatManager;
	}

	// Exposes the threat manager directly - be careful when interfacing with this
	// As a general rule of thumb, any unit pointer MUST be null checked BEFORE passing it to threatmanager methods
	// threatmanager will NOT null check your pointers for you - misuse = crash
	public ThreatManager GetThreatManager()
	{
		return _threatManager;
	}

	public double GetTotalSpellPowerValue(SpellSchoolMask mask, bool heal)
	{
		if (!IsPlayer)
		{
			if (OwnerUnit)
			{
				var ownerPlayer = OwnerUnit.AsPlayer;

				if (ownerPlayer != null)
				{
					if (IsTotem)
					{
						return OwnerUnit.GetTotalSpellPowerValue(mask, heal);
					}
					else
					{
						if (IsPet)
							return ownerPlayer.ActivePlayerData.PetSpellPower.GetValue();
						else if (IsGuardian)
							return ((Guardian)this).GetBonusDamage();
					}
				}
			}

			if (heal)
				return SpellBaseHealingBonusDone(mask);
			else
				return SpellBaseDamageBonusDone(mask);
		}

		var thisPlayer = AsPlayer;

		var sp = 0;

		if (heal)
		{
			sp = thisPlayer.ActivePlayerData.ModHealingDonePos;
		}
		else
		{
			var counter = 0;

			for (var i = 1; i < (int)SpellSchools.Max; i++)
				if (((int)mask & (1 << i)) > 0)
				{
					sp += thisPlayer.ActivePlayerData.ModDamageDonePos[i];
					counter++;
				}

			if (counter > 0)
				sp /= counter;
		}

		return Math.Max(sp, 0); //avoid negative spell power
	}

	void _UpdateSpells(uint diff)
	{
		_spellHistory.Update();

		if (GetCurrentSpell(CurrentSpellTypes.AutoRepeat) != null)
			_UpdateAutoRepeatSpell();

		for (CurrentSpellTypes i = 0; i < CurrentSpellTypes.Max; ++i)
			if (GetCurrentSpell(i) != null && CurrentSpells[i].State == SpellState.Finished)
			{
				CurrentSpells[i].SetReferencedFromCurrent(false);
				CurrentSpells[i] = null;
			}

		List<Aura> toRemove = new();

		foreach (var aura in _ownedAuras.Auras)
		{
			if (aura == null)
				continue;

			aura.UpdateOwner(diff, this);

			if (aura.IsExpired)
				toRemove.Add(aura);

			if (aura.SpellInfo.IsChanneled &&
				aura.CasterGuid != GUID &&
				!Global.ObjAccessor.GetWorldObject(this, aura.CasterGuid))
				toRemove.Add(aura);
		}

		// remove expired auras - do that after updates(used in scripts?)
		foreach (var pair in toRemove)
			RemoveOwnedAura(pair.Id, pair, AuraRemoveMode.Expire);

		lock (_visibleAurasToUpdate)
		{
			foreach (var aura in _visibleAurasToUpdate)
				aura.ClientUpdate();

			_visibleAurasToUpdate.Clear();
		}

		_DeleteRemovedAuras();

		if (!GameObjects.Empty())
			for (var i = 0; i < GameObjects.Count; ++i)
			{
				var go = GameObjects[i];

				if (!go.IsSpawned)
				{
					go.SetOwnerGUID(ObjectGuid.Empty);
					go.SetRespawnTime(0);
					go.Delete();
					GameObjects.Remove(go);
				}
			}
	}

	Unit GetVehicleRoot()
	{
		var vehicleRoot = VehicleBase;

		if (!vehicleRoot)
			return null;

		for (;;)
		{
			if (!vehicleRoot.VehicleBase)
				return vehicleRoot;

			vehicleRoot = vehicleRoot.VehicleBase;
		}
	}

	List<DynamicObject> GetDynObjects(uint spellId)
	{
		List<DynamicObject> dynamicobjects = new();

		foreach (var obj in DynamicObjects)
			if (obj.GetSpellId() == spellId)
				dynamicobjects.Add(obj);

		return dynamicobjects;
	}

	List<GameObject> GetGameObjects(uint spellId)
	{
		List<GameObject> gameobjects = new();

		foreach (var obj in GameObjects)
			if (obj.SpellId == spellId)
				gameobjects.Add(obj);

		return gameobjects;
	}

	void CancelSpellMissiles(uint spellId, bool reverseMissile = false)
	{
		var hasMissile = false;

		Events.ScheduleAbortOnAllMatchingEvents(e =>
		{
			var spell = Spell.ExtractSpellFromEvent(e);

			if (spell != null)
				if (spell.SpellInfo.Id == spellId)
				{
					hasMissile = true;

					return true;
				}

			return false;
		});

		if (hasMissile)
		{
			MissileCancel packet = new()
            {
                OwnerGUID = GUID,
                SpellID = spellId,
                Reverse = reverseMissile
            };

            SendMessageToSet(packet, false);
		}
	}

	void RestoreDisabledAI()
	{
		// Keep popping the stack until we either reach the bottom or find a valid AI
		while (PopAI())
			if (GetTopAI() != null && GetTopAI() is not ScheduledChangeAI)
				return;
	}

	UnitAI GetScheduledChangeAI()
	{
		var creature = AsCreature;

		if (creature != null)
			return new ScheduledChangeAI(creature);
		else
			return null;
	}

	bool HasScheduledAIChange()
	{
		var ai = AI;

		if (ai != null)
			return ai is ScheduledChangeAI;
		else
			return true;
	}

	void RemoveAllFollowers()
	{
		while (!_followingMe.Empty())
			_followingMe[0].SetTarget(null);
	}

	bool HasInterruptFlag(SpellAuraInterruptFlags flags)
	{
		return _interruptMask.HasAnyFlag(flags);
	}

	bool HasInterruptFlag(SpellAuraInterruptFlags2 flags)
	{
		return _interruptMask2.HasAnyFlag(flags);
	}

	void _UpdateAutoRepeatSpell()
	{
		var autoRepeatSpellInfo = CurrentSpells[CurrentSpellTypes.AutoRepeat].SpellInfo;

		// check "realtime" interrupts
		// don't cancel spells which are affected by a SPELL_AURA_CAST_WHILE_WALKING effect
		if ((IsMoving && GetCurrentSpell(CurrentSpellTypes.AutoRepeat).CheckMovement() != SpellCastResult.SpellCastOk) || IsNonMeleeSpellCast(false, false, true, autoRepeatSpellInfo.Id == 75))
		{
			// cancel wand shoot
			if (autoRepeatSpellInfo.Id != 75)
				InterruptSpell(CurrentSpellTypes.AutoRepeat);

			return;
		}

		// castroutine
		if (IsAttackReady(WeaponAttackType.RangedAttack) && GetCurrentSpell(CurrentSpellTypes.AutoRepeat).State != SpellState.Preparing)
		{
			// Check if able to cast
			var currentSpell = CurrentSpells[CurrentSpellTypes.AutoRepeat];

			if (currentSpell != null)
			{
				var result = currentSpell.CheckCast(true);

				if (result != SpellCastResult.SpellCastOk)
				{
					if (autoRepeatSpellInfo.Id != 75)
						InterruptSpell(CurrentSpellTypes.AutoRepeat);
					else if (TypeId == TypeId.Player)
						Spell.SendCastResult(AsPlayer, autoRepeatSpellInfo, currentSpell.SpellVisual, currentSpell.CastId, result);

					return;
				}

				// we want to shoot
				Spell spell = new(this, autoRepeatSpellInfo, TriggerCastFlags.IgnoreGCD);
				spell.Prepare(currentSpell.Targets);
			}
		}
	}

	uint GetCosmeticMountDisplayId()
	{
		return UnitData.CosmeticMountDisplayID;
	}

	Player GetControllingPlayer()
	{
		var guid = CharmerOrOwnerGUID;

		if (!guid.IsEmpty)
		{
			var master = Global.ObjAccessor.GetUnit(this, guid);

			if (master != null)
				return master.GetControllingPlayer();

			return null;
		}
		else
		{
			return AsPlayer;
		}
	}

	void StartReactiveTimer(ReactiveType reactive)
	{
		_reactiveTimer[reactive] = 4000;
	}

	void DealMeleeDamage(CalcDamageInfo damageInfo, bool durabilityLoss)
	{
		var victim = damageInfo.Target;

		if (!victim.IsAlive || victim.HasUnitState(UnitState.InFlight) || (victim.IsTypeId(TypeId.Unit) && victim.AsCreature.IsEvadingAttacks))
			return;

		if (damageInfo.TargetState == VictimState.Parry &&
			(!victim.IsCreature || victim.AsCreature.Template.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.NoParryHasten)))
		{
			// Get attack timers
			float offtime = victim.GetAttackTimer(WeaponAttackType.OffAttack);
			float basetime = victim.GetAttackTimer(WeaponAttackType.BaseAttack);

			// Reduce attack time
			if (victim.HaveOffhandWeapon() && offtime < basetime)
			{
				var percent20 = victim.GetBaseAttackTime(WeaponAttackType.OffAttack) * 0.20f;
				var percent60 = 3.0f * percent20;

				if (offtime > percent20 && offtime <= percent60)
				{
					victim.SetAttackTimer(WeaponAttackType.OffAttack, (uint)percent20);
				}
				else if (offtime > percent60)
				{
					offtime -= 2.0f * percent20;
					victim.SetAttackTimer(WeaponAttackType.OffAttack, (uint)offtime);
				}
			}
			else
			{
				var percent20 = victim.GetBaseAttackTime(WeaponAttackType.BaseAttack) * 0.20f;
				var percent60 = 3.0f * percent20;

				if (basetime > percent20 && basetime <= percent60)
				{
					victim.SetAttackTimer(WeaponAttackType.BaseAttack, (uint)percent20);
				}
				else if (basetime > percent60)
				{
					basetime -= 2.0f * percent20;
					victim.SetAttackTimer(WeaponAttackType.BaseAttack, (uint)basetime);
				}
			}
		}

		// Call default DealDamage
		CleanDamage cleanDamage = new(damageInfo.CleanDamage, damageInfo.Absorb, damageInfo.AttackType, damageInfo.HitOutCome);
		damageInfo.Damage = DealDamage(this, victim, damageInfo.Damage, cleanDamage, DamageEffectType.Direct, (SpellSchoolMask)damageInfo.DamageSchoolMask, null, durabilityLoss);

		// If this is a creature and it attacks from behind it has a probability to daze it's victim
		if ((damageInfo.HitOutCome == MeleeHitOutcome.Crit || damageInfo.HitOutCome == MeleeHitOutcome.Crushing || damageInfo.HitOutCome == MeleeHitOutcome.Normal || damageInfo.HitOutCome == MeleeHitOutcome.Glancing) &&
			!IsTypeId(TypeId.Player) &&
			!AsCreature.IsControlledByPlayer &&
			!victim.Location.HasInArc(MathFunctions.PI, Location) &&
			(victim.IsTypeId(TypeId.Player) || !victim.AsCreature.IsWorldBoss) &&
			!victim.IsVehicle)
		{
			// 20% base chance
			var chance = 20.0f;

			// there is a newbie protection, at level 10 just 7% base chance; assuming linear function
			if (victim.Level < 30)
				chance = 0.65f * victim.GetLevelForTarget(this) + 0.5f;

			uint victimDefense = victim.GetMaxSkillValueForLevel(this);
			uint attackerMeleeSkill = GetMaxSkillValueForLevel();

			chance *= attackerMeleeSkill / victimDefense * 0.16f;

			// -probability is between 0% and 40%
			MathFunctions.RoundToInterval(ref chance, 0.0f, 40.0f);

			if (RandomHelper.randChance(chance))
				CastSpell(victim, 1604, true);
		}

		if (IsTypeId(TypeId.Player))
		{
			DamageInfo dmgInfo = new(damageInfo);
			AsPlayer.CastItemCombatSpell(dmgInfo);
		}

		// Do effect if any damage done to target
		if (damageInfo.Damage != 0)
		{
			// We're going to call functions which can modify content of the list during iteration over it's elements
			// Let's copy the list so we can prevent iterator invalidation
			var vDamageShieldsCopy = victim.GetAuraEffectsByType(AuraType.DamageShield);

			foreach (var dmgShield in vDamageShieldsCopy)
			{
				var spellInfo = dmgShield.SpellInfo;

				// Damage shield can be resisted...
				var missInfo = victim.SpellHitResult(this, spellInfo, false);

				if (missInfo != SpellMissInfo.None)
				{
					victim.SendSpellMiss(this, spellInfo.Id, missInfo);

					continue;
				}

				// ...or immuned
				if (IsImmunedToDamage(spellInfo))
				{
					victim.SendSpellDamageImmune(this, spellInfo.Id, false);

					continue;
				}

				var damage = dmgShield.Amount;
				var caster = dmgShield.Caster;

				if (caster)
				{
					damage = caster.SpellDamageBonusDone(this, spellInfo, damage, DamageEffectType.SpellDirect, dmgShield.GetSpellEffectInfo());
					damage = SpellDamageBonusTaken(caster, spellInfo, damage, DamageEffectType.SpellDirect);
				}

				DamageInfo damageInfo1 = new(this, victim, damage, spellInfo, spellInfo.GetSchoolMask(), DamageEffectType.SpellDirect, WeaponAttackType.BaseAttack);
				CalcAbsorbResist(damageInfo1);
				damage = damageInfo1.Damage;

				DealDamageMods(victim, this, ref damage);

				SpellDamageShield damageShield = new()
                {
                    Attacker = victim.GUID,
                    Defender = GUID,
                    SpellID = spellInfo.Id,
                    TotalDamage = (uint)damage,
                    OriginalDamage = (int)damageInfo.OriginalDamage,
                    OverKill = (uint)Math.Max(damage - Health, 0),
                    SchoolMask = (uint)spellInfo.SchoolMask,
                    LogAbsorbed = (uint)damageInfo1.Absorb
                };

                DealDamage(victim, this, damage, null, DamageEffectType.SpellDirect, spellInfo.GetSchoolMask(), spellInfo, true);
				damageShield.LogData.Initialize(this);

				victim.SendCombatLogMessage(damageShield);
			}
		}
	}

	void TriggerOnHealthChangeAuras(long oldVal, long newVal)
	{
		foreach (var effect in GetAuraEffectsByType(AuraType.TriggerSpellOnHealthPct))
		{
			var triggerHealthPct = effect.Amount;
			var triggerSpell = effect.GetSpellEffectInfo().TriggerSpell;
			var threshold = CountPctFromMaxHealth(triggerHealthPct);

			switch ((AuraTriggerOnHealthChangeDirection)effect.MiscValue)
			{
				case AuraTriggerOnHealthChangeDirection.Above:
					if (newVal < threshold || oldVal > threshold)
						continue;

					break;
				case AuraTriggerOnHealthChangeDirection.Below:
					if (newVal > threshold || oldVal < threshold)
						continue;

					break;
				default:
					break;
			}

			CastSpell(this, triggerSpell, new CastSpellExtraArgs(effect));
		}
	}

	void UpdateReactives(uint p_time)
	{
		for (ReactiveType reactive = 0; reactive < ReactiveType.Max; ++reactive)
		{
			if (!_reactiveTimer.ContainsKey(reactive))
				continue;

			if (_reactiveTimer[reactive] <= p_time)
			{
				_reactiveTimer[reactive] = 0;

				switch (reactive)
				{
					case ReactiveType.Defense:
						if (HasAuraState(AuraStateType.Defensive))
							ModifyAuraState(AuraStateType.Defensive, false);

						break;
					case ReactiveType.Defense2:
						if (HasAuraState(AuraStateType.Defensive2))
							ModifyAuraState(AuraStateType.Defensive2, false);

						break;
				}
			}
			else
			{
				_reactiveTimer[reactive] -= p_time;
			}
		}
	}

	void GainSpellComboPoints(sbyte count)
	{
		if (count == 0)
			return;

		var cp = (sbyte)GetPower(PowerType.ComboPoints);

		cp += count;

		if (cp > 5) cp = 5;
		else if (cp < 0) cp = 0;

		SetPower(PowerType.ComboPoints, cp);
	}

	static double CalcSpellResistedDamage(Unit attacker, Unit victim, double damage, SpellSchoolMask schoolMask, SpellInfo spellInfo)
	{
		// Magic damage, check for resists
		if (!Convert.ToBoolean(schoolMask & SpellSchoolMask.Magic))
			return 0;

		// Npcs can have holy resistance
		if (schoolMask.HasAnyFlag(SpellSchoolMask.Holy) && victim.TypeId != TypeId.Unit)
			return 0;

		var averageResist = CalculateAverageResistReduction(attacker, schoolMask, victim, spellInfo);

		var discreteResistProbability = new double[11];

		if (averageResist <= 0.1f)
		{
			discreteResistProbability[0] = 1.0f - 7.5f * averageResist;
			discreteResistProbability[1] = 5.0f * averageResist;
			discreteResistProbability[2] = 2.5f * averageResist;
		}
		else
		{
			for (uint i = 0; i < 11; ++i)
				discreteResistProbability[i] = Math.Max(0.5f - 2.5f * Math.Abs(0.1f * i - averageResist), 0.0f);
		}

		var roll = RandomHelper.NextDouble();
		double probabilitySum = 0.0f;

		uint resistance = 0;

		for (; resistance < 11; ++resistance)
			if (roll < (probabilitySum += discreteResistProbability[resistance]))
				break;

		var damageResisted = damage * resistance / 10f;

		if (damageResisted > 0.0f) // if any damage was resisted
		{
			double ignoredResistance = 0;

			if (attacker != null)
				ignoredResistance += attacker.GetTotalAuraModifierByMiscMask(AuraType.ModIgnoreTargetResist, (int)schoolMask);

			ignoredResistance = Math.Min(ignoredResistance, 100);
			MathFunctions.ApplyPct(ref damageResisted, 100 - ignoredResistance);

			// Spells with melee and magic school mask, decide whether resistance or armor absorb is higher
			if (spellInfo != null && spellInfo.HasAttribute(SpellCustomAttributes.SchoolmaskNormalWithMagic))
			{
				var damageAfterArmor = CalcArmorReducedDamage(attacker, victim, damage, spellInfo, spellInfo.GetAttackType());
				var armorReduction = damage - damageAfterArmor;

				// pick the lower one, the weakest resistance counts
				damageResisted = Math.Min(damageResisted, armorReduction);
			}
		}

		damageResisted = Math.Max(damageResisted, 0.0f);

		return damageResisted;
	}

	static double CalculateAverageResistReduction(WorldObject caster, SpellSchoolMask schoolMask, Unit victim, SpellInfo spellInfo = null)
	{
		double victimResistance = victim.GetResistance(schoolMask);

		if (caster != null)
		{
			// pets inherit 100% of masters penetration
			var player = caster.SpellModOwner;

			if (player != null)
			{
				victimResistance += player.GetTotalAuraModifierByMiscMask(AuraType.ModTargetResistance, (int)schoolMask);
				victimResistance -= player.GetSpellPenetrationItemMod();
			}
			else
			{
				var unitCaster = caster.AsUnit;

				if (unitCaster != null)
					victimResistance += unitCaster.GetTotalAuraModifierByMiscMask(AuraType.ModTargetResistance, (int)schoolMask);
			}
		}

		// holy resistance exists in pve and comes from level difference, ignore template values
		if (schoolMask.HasAnyFlag(SpellSchoolMask.Holy))
			victimResistance = 0.0f;

		// Chaos Bolt exception, ignore all target resistances (unknown attribute?)
		if (spellInfo is { SpellFamilyName: SpellFamilyNames.Warlock, Id: 116858 })
			victimResistance = 0.0f;

		victimResistance = Math.Max(victimResistance, 0.0f);

		// level-based resistance does not apply to binary spells, and cannot be overcome by spell penetration
		// gameobject caster -- should it have level based resistance?
		if (caster is { IsGameObject: false } && (spellInfo == null || !spellInfo.HasAttribute(SpellCustomAttributes.BinarySpell)))
			victimResistance += Math.Max(((float)victim.GetLevelForTarget(caster) - (float)caster.GetLevelForTarget(victim)) * 5.0f, 0.0f);

		uint bossLevel = 83;
		var bossResistanceConstant = 510.0f;
		var level = caster ? victim.GetLevelForTarget(caster) : victim.Level;
		float resistanceConstant;

		if (level == bossLevel)
			resistanceConstant = bossResistanceConstant;
		else
			resistanceConstant = level * 5.0f;

		return victimResistance / (victimResistance + resistanceConstant);
	}

	bool IsBlockCritical()
	{
		if (RandomHelper.randChance(GetTotalAuraModifier(AuraType.ModBlockCritChance)))
			return true;

		return false;
	}
}