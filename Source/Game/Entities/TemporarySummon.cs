﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Dynamic;
using Game.DataStorage;
using Game.Maps;

namespace Game.Entities
{
    public class TempSummon : Creature
    {
        public TempSummon(SummonPropertiesRecord properties, WorldObject owner, bool isWorldObject) : base(isWorldObject)
        {
            m_Properties = properties;
            m_type = TempSummonType.ManualDespawn;

            m_summonerGUID = owner != null ? owner.GetGUID() : ObjectGuid.Empty;
            UnitTypeMask |= UnitTypeMask.Summon;
            m_canFollowOwner = true;
        }

        public WorldObject GetSummoner()
        {
            return !m_summonerGUID.IsEmpty() ? Global.ObjAccessor.GetWorldObject(this, m_summonerGUID) : null;
        }

        public void SetSummonerGUID(ObjectGuid summonerGUID)
        {
            m_summonerGUID = summonerGUID;
        }

        public Unit GetSummonerUnit()
        {
            WorldObject summoner = GetSummoner();
            if (summoner != null)
                return summoner.ToUnit();

            return null;
        }

        public Creature GetSummonerCreatureBase()
        {
            return !m_summonerGUID.IsEmpty() ? ObjectAccessor.GetCreature(this, m_summonerGUID) : null;
        }

        public GameObject GetSummonerGameObject()
        {
            WorldObject summoner = GetSummoner();
            if (summoner != null)
                return summoner.ToGameObject();

            return null;
        }

        public override float GetDamageMultiplierForTarget(WorldObject target)
        {
            return 1.0f;
        }

        public override void Update(uint diff)
        {
            base.Update(diff);

            if (m_deathState == DeathState.Dead)
            {
                UnSummon();
                return;
            }

            switch (m_type)
            {
                case TempSummonType.ManualDespawn:
                case TempSummonType.DeadDespawn:
                    break;
                case TempSummonType.TimedDespawn:
                {
                    if (m_timer <= diff)
                    {
                        UnSummon();
                        return;
                    }

                    m_timer -= diff;
                    break;
                }
                case TempSummonType.TimedDespawnOutOfCombat:
                {
                    if (!IsInCombat())
                    {
                        if (m_timer <= diff)
                        {
                            UnSummon();
                            return;
                        }

                        m_timer -= diff;
                    }
                    else if (m_timer != m_lifetime)
                        m_timer = m_lifetime;

                    break;
                }

                case TempSummonType.CorpseTimedDespawn:
                {
                    if (m_deathState == DeathState.Corpse)
                    {
                        if (m_timer <= diff)
                        {
                            UnSummon();
                            return;
                        }

                        m_timer -= diff;
                    }
                    break;
                }
                case TempSummonType.CorpseDespawn:
                {
                    // if m_deathState is DEAD, CORPSE was skipped
                    if (m_deathState == DeathState.Corpse)
                    {
                        UnSummon();
                        return;
                    }

                    break;
                }
                case TempSummonType.TimedOrCorpseDespawn:
                {
                    if (m_deathState == DeathState.Corpse)
                    {
                        UnSummon();
                        return;
                    }

                    if (!IsInCombat())
                    {
                        if (m_timer <= diff)
                        {
                            UnSummon();
                            return;
                        }
                        else
                            m_timer -= diff;
                    }
                    else if (m_timer != m_lifetime)
                        m_timer = m_lifetime;
                    break;
                }
                case TempSummonType.TimedOrDeadDespawn:
                {
                    if (!IsInCombat() && IsAlive())
                    {
                        if (m_timer <= diff)
                        {
                            UnSummon();
                            return;
                        }
                        else
                            m_timer -= diff;
                    }
                    else if (m_timer != m_lifetime)
                        m_timer = m_lifetime;
                    break;
                }
                default:
                    UnSummon();
                    Log.outError(LogFilter.Unit, "Temporary summoned creature (entry: {0}) have unknown type {1} of ", GetEntry(), m_type);
                    break;
            }
        }

        public virtual void InitStats(uint duration)
        {
            Cypher.Assert(!IsPet());

            m_timer = duration;
            m_lifetime = duration;

            if (m_type == TempSummonType.ManualDespawn)
                m_type = (duration == 0) ? TempSummonType.DeadDespawn : TempSummonType.TimedDespawn;

            Unit owner = GetSummonerUnit();
            if (owner != null && IsTrigger() && m_spells[0] != 0)
                if (owner.IsTypeId(TypeId.Player))
                    m_ControlledByPlayer = true;

            if (owner != null && owner.IsPlayer())
            {
                CreatureSummonedData summonedData = Global.ObjectMgr.GetCreatureSummonedData(GetEntry());
                if (summonedData != null)
                {
                    m_creatureIdVisibleToSummoner = summonedData.CreatureIDVisibleToSummoner;
                    if (summonedData.CreatureIDVisibleToSummoner.HasValue)
                    {
                        CreatureTemplate creatureTemplateVisibleToSummoner = Global.ObjectMgr.GetCreatureTemplate(summonedData.CreatureIDVisibleToSummoner.Value);
                        m_displayIdVisibleToSummoner = ObjectManager.ChooseDisplayId(creatureTemplateVisibleToSummoner, null).CreatureDisplayID;
                    }
                }
            }

            if (m_Properties == null)
                return;

            if (owner != null)
            {
                int slot = m_Properties.Slot;
                if (slot > 0)
                {
                    if (!owner.m_SummonSlot[slot].IsEmpty() && owner.m_SummonSlot[slot] != GetGUID())
                    {
                        Creature oldSummon = GetMap().GetCreature(owner.m_SummonSlot[slot]);
                        if (oldSummon != null && oldSummon.IsSummon())
                            oldSummon.ToTempSummon().UnSummon();
                    }
                    owner.m_SummonSlot[slot] = GetGUID();
                }

                if (!m_Properties.GetFlags().HasFlag(SummonPropertiesFlags.UseCreatureLevel))
                    SetLevel(owner.GetLevel());
            }

            uint faction = m_Properties.Faction;
            if (owner && m_Properties.GetFlags().HasFlag(SummonPropertiesFlags.UseSummonerFaction)) // TODO: Determine priority between faction and flag
                faction = owner.GetFaction();

            if (faction != 0)
                SetFaction(faction);

            if (m_Properties.GetFlags().HasFlag(SummonPropertiesFlags.SummonFromBattlePetJournal))
                RemoveNpcFlag(NPCFlags.WildBattlePet);
        }

        public virtual void InitSummon()
        {
            WorldObject owner = GetSummoner();
            if (owner != null)
            {
                if (owner.IsCreature())
                    owner.ToCreature().GetAI()?.JustSummoned(this);
                else if (owner.IsGameObject())
                    owner.ToGameObject().GetAI()?.JustSummoned(this);

                if (IsAIEnabled())
                    GetAI().IsSummonedBy(owner);
            }
        }

        public override void UpdateObjectVisibilityOnCreate()
        {
            List<WorldObject> objectsToUpdate = new();
            objectsToUpdate.Add(this);

            SmoothPhasing smoothPhasing = GetSmoothPhasing();
            if (smoothPhasing != null)
            {
                SmoothPhasingInfo infoForSeer = smoothPhasing.GetInfoForSeer(GetDemonCreatorGUID());
                if (infoForSeer != null && infoForSeer.ReplaceObject.HasValue && smoothPhasing.IsReplacing(infoForSeer.ReplaceObject.Value))
                {
                    WorldObject original = Global.ObjAccessor.GetWorldObject(this, infoForSeer.ReplaceObject.Value);
                    if (original != null)
                        objectsToUpdate.Add(original);
                }
            }

            VisibleChangesNotifier notifier = new(objectsToUpdate, GridType.World);
            Cell.VisitGrid(this, notifier, GetVisibilityRange());
        }

        public override void UpdateObjectVisibilityOnDestroy()
        {
            List<WorldObject> objectsToUpdate = new();
            objectsToUpdate.Add(this);

            WorldObject original = null;
            SmoothPhasing smoothPhasing = GetSmoothPhasing();
            if (smoothPhasing != null)
            {
                SmoothPhasingInfo infoForSeer = smoothPhasing.GetInfoForSeer(GetDemonCreatorGUID());
                if (infoForSeer != null && infoForSeer.ReplaceObject.HasValue && smoothPhasing.IsReplacing(infoForSeer.ReplaceObject.Value))
                    original = Global.ObjAccessor.GetWorldObject(this, infoForSeer.ReplaceObject.Value);

                if (original != null)
                {
                    objectsToUpdate.Add(original);

                    // disable replacement without removing - it is still needed for next step (visibility update)
                    SmoothPhasing originalSmoothPhasing = original.GetSmoothPhasing();
                    if (originalSmoothPhasing != null)
                        originalSmoothPhasing.DisableReplacementForSeer(GetDemonCreatorGUID());
                }
            }

            VisibleChangesNotifier notifier = new(objectsToUpdate, GridType.World);
            Cell.VisitGrid(this, notifier, GetVisibilityRange());

            if (original != null) // original is only != null when it was replaced
            {
                SmoothPhasing originalSmoothPhasing = original.GetSmoothPhasing();
                if (originalSmoothPhasing != null)
                    originalSmoothPhasing.ClearViewerDependentInfo(GetDemonCreatorGUID());
            }
        }

        public void SetTempSummonType(TempSummonType type)
        {
            m_type = type;
        }

        public virtual void UnSummon()
        {
            UnSummon(TimeSpan.Zero);
        }

        public virtual void UnSummon(TimeSpan msTime)
        {
            if (msTime != TimeSpan.Zero)
            {
                ForcedUnsummonDelayEvent pEvent = new(this);

                m_Events.AddEvent(pEvent, m_Events.CalculateTime(msTime));
                return;
            }

            Cypher.Assert(!IsPet());
            if (IsPet())
            {
                ToPet().Remove(PetSaveMode.NotInSlot);
                Cypher.Assert(!IsInWorld);
                return;
            }

            WorldObject owner = GetSummoner();
            if (owner != null)
            {
                if (owner.IsCreature())
                    owner.ToCreature().GetAI()?.SummonedCreatureDespawn(this);
                else if (owner.IsGameObject())
                    owner.ToGameObject().GetAI()?.SummonedCreatureDespawn(this);
            }

            AddObjectToRemoveList();
        }

        public override void RemoveFromWorld()
        {
            if (!IsInWorld)
                return;

            if (m_Properties != null)
            {
                int slot = m_Properties.Slot;
                if (slot > 0)
                {
                    Unit owner = GetSummonerUnit();
                    if (owner != null)
                        if (owner.m_SummonSlot[slot] == GetGUID())
                            owner.m_SummonSlot[slot].Clear();
                }
            }

            if (!GetOwnerGUID().IsEmpty())
                Log.outError(LogFilter.Unit, "Unit {0} has owner guid when removed from world", GetEntry());

            base.RemoveFromWorld();
        }

        public override string GetDebugInfo()
        {
            return $"{base.GetDebugInfo()}\nTempSummonType : {GetSummonType()} Summoner: {GetSummonerGUID()} Timer: {GetTimer()}";
        }
        
        public override void SaveToDB(uint mapid, List<Difficulty> spawnDifficulties) { }

        public ObjectGuid GetSummonerGUID() { return m_summonerGUID; }

        TempSummonType GetSummonType() { return m_type; }

        public uint GetTimer() { return m_timer; }

        public uint? GetCreatureIdVisibleToSummoner() { return m_creatureIdVisibleToSummoner; }
        public uint? GetDisplayIdVisibleToSummoner() { return m_displayIdVisibleToSummoner; }
        
        public bool CanFollowOwner() { return m_canFollowOwner; }
        public void SetCanFollowOwner(bool can) { m_canFollowOwner = can; }

        public SummonPropertiesRecord m_Properties;
        TempSummonType m_type;
        uint m_timer;
        uint m_lifetime;
        ObjectGuid m_summonerGUID;
        uint? m_creatureIdVisibleToSummoner;
        uint? m_displayIdVisibleToSummoner;
        bool m_canFollowOwner;
    }

    public class Minion : TempSummon
    {
        public Minion(SummonPropertiesRecord properties, Unit owner, bool isWorldObject)
            : base(properties, owner, isWorldObject)
        {
            m_owner = owner;
            Cypher.Assert(m_owner);
            UnitTypeMask |= UnitTypeMask.Minion;
            m_followAngle = SharedConst.PetFollowAngle;
            /// @todo: Find correct way
            InitCharmInfo();
        }

        public override void InitStats(uint duration)
        {
            base.InitStats(duration);

            SetReactState(ReactStates.Passive);

            SetCreatorGUID(GetOwner().GetGUID());
            SetFaction(GetOwner().GetFaction());// TODO: Is this correct? Overwrite the use of SummonPropertiesFlags::UseSummonerFaction

            GetOwner().SetMinion(this, true);
        }

        public override void RemoveFromWorld()
        {
            if (!IsInWorld)
                return;

            GetOwner().SetMinion(this, false);
            base.RemoveFromWorld();
        }

        public override void SetDeathState(DeathState s)
        {
            base.SetDeathState(s);
            if (s != DeathState.JustDied || !IsGuardianPet())
                return;

            Unit owner = GetOwner();
            if (owner == null || !owner.IsPlayer() || owner.GetMinionGUID() != GetGUID())
                return;

            foreach (Unit controlled in owner.m_Controlled)
            {
                if (controlled.GetEntry() == GetEntry() && controlled.IsAlive())
                {
                    owner.SetMinionGUID(controlled.GetGUID());
                    owner.SetPetGUID(controlled.GetGUID());
                    owner.ToPlayer().CharmSpellInitialize();
                    break;
                }
            }
        }

        public bool IsGuardianPet()
        {
            return IsPet() || (m_Properties != null && m_Properties.Control == SummonCategory.Pet);
        }

        public override string GetDebugInfo()
        {
            return $"{base.GetDebugInfo()}\nOwner: {(GetOwner() ? GetOwner().GetGUID() : "")}";
        }
        
        public override Unit GetOwner() { return m_owner; }

        public override float GetFollowAngle() { return m_followAngle; }

        public void SetFollowAngle(float angle) { m_followAngle = angle; }

        // Warlock pets
        public bool IsPetImp() { return GetEntry() == (uint)PetEntry.Imp; }
        public bool IsPetFelhunter() { return GetEntry() == (uint)PetEntry.FelHunter; }
        public bool IsPetVoidwalker() { return GetEntry() == (uint)PetEntry.VoidWalker; }
        public bool IsPetSuccubus() { return GetEntry() == (uint)PetEntry.Succubus; }
        public bool IsPetDoomguard() { return GetEntry() == (uint)PetEntry.Doomguard; }
        public bool IsPetFelguard() { return GetEntry() == (uint)PetEntry.Felguard; }

        // Death Knight pets
        public bool IsPetGhoul() { return GetEntry() == (uint)PetEntry.Ghoul; } // Ghoul may be guardian or pet
        public bool IsPetAbomination() { return GetEntry() == (uint)PetEntry.Abomination; } // Sludge Belcher dk talent

        // Shaman pet
        public bool IsSpiritWolf() { return GetEntry() == (uint)PetEntry.SpiritWolf; } // Spirit wolf from feral spirits

        protected Unit m_owner;
        float m_followAngle;
    }

    public class Guardian : Minion
    {
        public Guardian(SummonPropertiesRecord properties, Unit owner, bool isWorldObject)
            : base(properties, owner, isWorldObject)
        {
            m_bonusSpellDamage = 0;

            UnitTypeMask |= UnitTypeMask.Guardian;
            if (properties != null && (properties.Title == SummonTitle.Pet || properties.Control == SummonCategory.Pet))
            {
                UnitTypeMask |= UnitTypeMask.ControlableGuardian;
                InitCharmInfo();
            }
        }

        public override void InitStats(uint duration)
        {
            base.InitStats(duration);

            InitStatsForLevel(GetOwner().GetLevel());

            if (GetOwner().IsTypeId(TypeId.Player) && HasUnitTypeMask(UnitTypeMask.ControlableGuardian))
                GetCharmInfo().InitCharmCreateSpells();

            SetReactState(ReactStates.Aggressive);
        }

        public override void InitSummon()
        {
            base.InitSummon();

            if (GetOwner().IsTypeId(TypeId.Player) && GetOwner().GetMinionGUID() == GetGUID()
                && GetOwner().GetCharmedGUID().IsEmpty())
            {
                GetOwner().ToPlayer().CharmSpellInitialize();
            }
        }

        // @todo Move stat mods code to pet passive auras
        public bool InitStatsForLevel(uint petlevel)
        {
            CreatureTemplate cinfo = GetCreatureTemplate();
            Cypher.Assert(cinfo != null);

            SetLevel(petlevel);

            //Determine pet type
            PetType petType = PetType.Max;
            if (IsPet() && GetOwner().IsTypeId(TypeId.Player))
            {
                if (GetOwner().GetClass() == Class.Warlock
                        || GetOwner().GetClass() == Class.Shaman        // Fire Elemental
                        || GetOwner().GetClass() == Class.Deathknight) // Risen Ghoul
                {
                    petType = PetType.Summon;
                }
                else if (GetOwner().GetClass() == Class.Hunter)
                {
                    petType = PetType.Hunter;
                    UnitTypeMask |= UnitTypeMask.HunterPet;
                }
                else
                {
                    Log.outError(LogFilter.Unit, "Unknown type pet {0} is summoned by player class {1}", GetEntry(), GetOwner().GetClass());
                }
            }

            uint creature_ID = (petType == PetType.Hunter) ? 1 : cinfo.Entry;

            SetMeleeDamageSchool((SpellSchools)cinfo.DmgSchool);

            SetStatFlatModifier(UnitMods.Armor, UnitModifierFlatType.Base, (float)petlevel * 50);

            SetBaseAttackTime(WeaponAttackType.BaseAttack, SharedConst.BaseAttackTime);
            SetBaseAttackTime(WeaponAttackType.OffAttack, SharedConst.BaseAttackTime);
            SetBaseAttackTime(WeaponAttackType.RangedAttack, SharedConst.BaseAttackTime);

            //scale
            SetObjectScale(GetNativeObjectScale());

            // Resistance
            // Hunters pet should not inherit resistances from creature_template, they have separate auras for that
            if (!IsHunterPet())
            {
                for (int i = (int)SpellSchools.Holy; i < (int)SpellSchools.Max; ++i)
                    SetStatFlatModifier(UnitMods.ResistanceStart + i, UnitModifierFlatType.Base, cinfo.Resistance[i]);
            }

            // Health, Mana or Power, Armor
            PetLevelInfo pInfo = Global.ObjectMgr.GetPetLevelInfo(creature_ID, petlevel);
            if (pInfo != null)                                      // exist in DB
            {
                SetCreateHealth(pInfo.health);
                SetCreateMana(pInfo.mana);

                if (pInfo.armor > 0)
                    SetStatFlatModifier(UnitMods.Armor, UnitModifierFlatType.Base, pInfo.armor);

                for (byte stat = 0; stat < (int)Stats.Max; ++stat)
                    SetCreateStat((Stats)stat, pInfo.stats[stat]);
            }
            else                                            // not exist in DB, use some default fake data
            {
                // remove elite bonuses included in DB values
                CreatureBaseStats stats = Global.ObjectMgr.GetCreatureBaseStats(petlevel, cinfo.UnitClass);
                ApplyLevelScaling();

                SetCreateHealth((uint)(Global.DB2Mgr.EvaluateExpectedStat(ExpectedStatType.CreatureHealth, petlevel, cinfo.GetHealthScalingExpansion(), m_unitData.ContentTuningID, (Class)cinfo.UnitClass) * cinfo.ModHealth * cinfo.ModHealthExtra * GetHealthMod(cinfo.Rank)));
                SetCreateMana(stats.GenerateMana(cinfo));

                SetCreateStat(Stats.Strength, 22);
                SetCreateStat(Stats.Agility, 22);
                SetCreateStat(Stats.Stamina, 25);
                SetCreateStat(Stats.Intellect, 28);
            }

            // Power
            if (petType == PetType.Hunter) // Hunter pets have focus
                SetPowerType(PowerType.Focus);
            else if (IsPetGhoul() || IsPetAbomination()) // DK pets have energy
            {
                SetPowerType(PowerType.Energy);
                SetFullPower(PowerType.Energy);
            }
            else if (IsPetImp() || IsPetFelhunter() || IsPetVoidwalker() || IsPetSuccubus() || IsPetDoomguard() || IsPetFelguard()) // Warlock pets have energy (since 5.x)
                SetPowerType(PowerType.Energy);
            else
                SetPowerType(PowerType.Mana);

            // Damage
            SetBonusDamage(0);
            switch (petType)
            {
                case PetType.Summon:
                {
                    // the damage bonus used for pets is either fire or shadow damage, whatever is higher
                    int fire = GetOwner().ToPlayer().m_activePlayerData.ModDamageDonePos[(int)SpellSchools.Fire];
                    int shadow = GetOwner().ToPlayer().m_activePlayerData.ModDamageDonePos[(int)SpellSchools.Shadow];
                    int val = (fire > shadow) ? fire : shadow;
                    if (val < 0)
                        val = 0;

                    SetBonusDamage((int)(val * 0.15f));

                    SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, petlevel - (petlevel / 4));
                    SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, petlevel + (petlevel / 4));
                    break;
                }
                case PetType.Hunter:
                {
                    ToPet().SetPetNextLevelExperience((uint)(Global.ObjectMgr.GetXPForLevel(petlevel) * 0.05f));
                    //these formula may not be correct; however, it is designed to be close to what it should be
                    //this makes dps 0.5 of pets level
                    SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, petlevel - (petlevel / 4));
                    //damage range is then petlevel / 2
                    SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, petlevel + (petlevel / 4));
                    //damage is increased afterwards as strength and pet scaling modify attack power
                    break;
                }
                default:
                {
                    switch (GetEntry())
                    {
                        case 510: // mage Water Elemental
                        {
                            SetBonusDamage((int)(GetOwner().SpellBaseDamageBonusDone(SpellSchoolMask.Frost) * 0.33f));
                            break;
                        }
                        case 1964: //force of nature
                        {
                            if (pInfo == null)
                                SetCreateHealth(30 + 30 * petlevel);
                                    var bonusDmg = GetOwner().SpellBaseDamageBonusDone(SpellSchoolMask.Nature) * 0.15f;
                            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, petlevel * 2.5f - ((float)petlevel / 2) + bonusDmg);
                            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, petlevel * 2.5f + ((float)petlevel / 2) + bonusDmg);
                            break;
                        }
                        case 15352: //earth elemental 36213
                        {
                            if (pInfo == null)
                                SetCreateHealth(100 + 120 * petlevel);
                            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, petlevel - (petlevel / 4));
                            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, petlevel + (petlevel / 4));
                            break;
                        }
                        case 15438: //fire elemental
                        {
                            if (pInfo == null)
                            {
                                SetCreateHealth(40 * petlevel);
                                SetCreateMana(28 + 10 * petlevel);
                            }
                            SetBonusDamage((int)(GetOwner().SpellBaseDamageBonusDone(SpellSchoolMask.Fire) * 0.5f));
                            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, petlevel * 4 - petlevel);
                            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, petlevel * 4 + petlevel);
                            break;
                        }
                        case 19668: // Shadowfiend
                        {
                            if (pInfo == null)
                            {
                                SetCreateMana(28 + 10 * petlevel);
                                SetCreateHealth(28 + 30 * petlevel);
                            }
                            int bonus_dmg = (int)(GetOwner().SpellBaseDamageBonusDone(SpellSchoolMask.Shadow) * 0.3f);
                            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, (petlevel * 4 - petlevel) + bonus_dmg);
                            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, (petlevel * 4 + petlevel) + bonus_dmg);
                            break;
                        }
                        case 19833: //Snake Trap - Venomous Snake
                        {
                            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, (petlevel / 2) - 25);
                            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, (petlevel / 2) - 18);
                            break;
                        }
                        case 19921: //Snake Trap - Viper
                        {
                            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, petlevel / 2 - 10);
                            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, petlevel / 2);
                            break;
                        }
                        case 29264: // Feral Spirit
                        {
                            if (pInfo == null)
                                SetCreateHealth(30 * petlevel);

                            // wolf attack speed is 1.5s
                            SetBaseAttackTime(WeaponAttackType.BaseAttack, cinfo.BaseAttackTime);

                            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, (petlevel * 4 - petlevel));
                            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, (petlevel * 4 + petlevel));

                            SetStatFlatModifier(UnitMods.Armor, UnitModifierFlatType.Base, GetOwner().GetArmor() * 0.35f);  // Bonus Armor (35% of player armor)
                            SetStatFlatModifier(UnitMods.StatStamina, UnitModifierFlatType.Base, GetOwner().GetStat(Stats.Stamina) * 0.3f);  // Bonus Stamina (30% of player stamina)
                            if (!HasAura(58877))//prevent apply twice for the 2 wolves
                                AddAura(58877, this);//Spirit Hunt, passive, Spirit Wolves' attacks heal them and their master for 150% of damage done.
                            break;
                        }
                        case 31216: // Mirror Image
                        {
                            SetBonusDamage((int)(GetOwner().SpellBaseDamageBonusDone(SpellSchoolMask.Frost) * 0.33f));
                            SetDisplayId(GetOwner().GetDisplayId());
                            if (pInfo == null)
                            {
                                SetCreateMana(28 + 30 * petlevel);
                                SetCreateHealth(28 + 10 * petlevel);
                            }
                            break;
                        }
                        case 27829: // Ebon Gargoyle
                        {
                            if (pInfo == null)
                            {
                                SetCreateMana(28 + 10 * petlevel);
                                SetCreateHealth(28 + 30 * petlevel);
                            }
                            SetBonusDamage((int)(GetOwner().GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 0.5f));
                            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, petlevel - (petlevel / 4));
                            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, petlevel + (petlevel / 4));
                            break;
                        }
                        case 28017: // Bloodworms
                        {
                            SetCreateHealth(4 * petlevel);
                            SetBonusDamage((int)(GetOwner().GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 0.006f));
                            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, petlevel - 30 - (petlevel / 4));
                            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, petlevel - 30 + (petlevel / 4));
                            break;
                        }
                        default:
                        {
                            /* ToDo: Check what 5f5d2028 broke/fixed and how much of Creature::UpdateLevelDependantStats()
                             * should be copied here (or moved to another method or if that function should be called here
                             * or not just for this default case)
                             */
                            float basedamage = GetBaseDamageForLevel(petlevel);

                            float weaponBaseMinDamage = basedamage;
                            float weaponBaseMaxDamage = basedamage * 1.5f;

                            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, weaponBaseMinDamage);
                            SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, weaponBaseMaxDamage);
                            break;
                        }
                    }
                    break;
                }
            }

            UpdateAllStats();

            SetFullHealth();
            SetFullPower(PowerType.Mana);
            return true;
        }

        const int ENTRY_IMP = 416;
        const int ENTRY_VOIDWALKER = 1860;
        const int ENTRY_SUCCUBUS = 1863;
        const int ENTRY_FELHUNTER = 417;
        const int ENTRY_FELGUARD = 17252;
        const int ENTRY_WATER_ELEMENTAL = 510;
        const int ENTRY_TREANT = 1964;
        const int ENTRY_FIRE_ELEMENTAL = 15438;
        const int ENTRY_GHOUL = 26125;
        const int ENTRY_BLOODWORM = 28017;

        public override bool UpdateStats(Stats stat)
        {
            var value = GetTotalStatValue(stat);
            UpdateStatBuffMod(stat);
            float ownersBonus = 0.0f;

            Unit owner = GetOwner();
            // Handle Death Knight Glyphs and Talents
            float mod = 0.75f;
            if (IsPetGhoul() && (stat == Stats.Stamina || stat == Stats.Strength))
            {
                switch (stat)
                {
                    case Stats.Stamina:
                        mod = 0.3f;
                        break;                // Default Owner's Stamina scale
                    case Stats.Strength:
                        mod = 0.7f;
                        break;                // Default Owner's Strength scale
                    default: break;
                }

                ownersBonus = owner.GetStat(stat) * mod;
                value += ownersBonus;
            }
            else if (stat == Stats.Stamina)
            {
                ownersBonus = MathFunctions.CalculatePct(owner.GetStat(Stats.Stamina), 30);
                value += ownersBonus;
            }
            //warlock's and mage's pets gain 30% of owner's intellect
            else if (stat == Stats.Intellect)
            {
                if (owner.GetClass() == Class.Warlock || owner.GetClass() == Class.Mage)
                {
                    ownersBonus = MathFunctions.CalculatePct(owner.GetStat(stat), 30);
                    value += ownersBonus;
                }
            }

            SetStat(stat, (int)value);
            m_statFromOwner[(int)stat] = ownersBonus;
            UpdateStatBuffMod(stat);

            switch (stat)
            {
                case Stats.Strength:
                    UpdateAttackPowerAndDamage();
                    break;
                case Stats.Agility:
                    UpdateArmor();
                    break;
                case Stats.Stamina:
                    UpdateMaxHealth();
                    break;
                case Stats.Intellect:
                    UpdateMaxPower(PowerType.Mana);
                    break;
                default:
                    break;
            }

            return true;
        }

        public override bool UpdateAllStats()
        {
            UpdateMaxHealth();

            for (var i = Stats.Strength; i < Stats.Max; ++i)
                UpdateStats(i);

            for (var i = PowerType.Mana; i < PowerType.Max; ++i)
                UpdateMaxPower(i);

            UpdateAllResistances();

            return true;
        }

        public override void UpdateResistances(SpellSchools school)
        {
            if (school > SpellSchools.Normal)
            {
                var baseValue = GetFlatModifierValue(UnitMods.ResistanceStart + (int)school, UnitModifierFlatType.Base);
                var bonusValue = GetTotalAuraModValue(UnitMods.ResistanceStart + (int)school) - baseValue;

                // hunter and warlock pets gain 40% of owner's resistance
                if (IsPet())
                {
                    baseValue += MathFunctions.CalculatePct(m_owner.GetResistance(school), 40);
                    bonusValue += MathFunctions.CalculatePct(m_owner.GetBonusResistanceMod(school), 40);
                }

                SetResistance(school, (int)baseValue);
                SetBonusResistanceMod(school, (int)bonusValue);
            }
            else
                UpdateArmor();
        }

        public override void UpdateArmor()
        {
            float bonus_armor = 0.0f;
            UnitMods unitMod = UnitMods.Armor;

            // hunter pets gain 35% of owner's armor value, warlock pets gain 100% of owner's armor
            if (IsHunterPet())
                bonus_armor = MathFunctions.CalculatePct(GetOwner().GetArmor(), 70);
            else if (IsPet())
                bonus_armor = GetOwner().GetArmor();

            var value = GetFlatModifierValue(unitMod, UnitModifierFlatType.Base);
            var baseValue = value;
            value *= GetPctModifierValue(unitMod, UnitModifierPctType.Base);
            value += GetFlatModifierValue(unitMod, UnitModifierFlatType.Total) + bonus_armor;
            value *= GetPctModifierValue(unitMod, UnitModifierPctType.Total);

            SetArmor((int)baseValue, (int)(value - baseValue));
        }

        public override void UpdateMaxHealth()
        {
            UnitMods unitMod = UnitMods.Health;
            float stamina = GetStat(Stats.Stamina) - GetCreateStat(Stats.Stamina);

            float multiplicator;
            switch (GetEntry())
            {
                case ENTRY_IMP:
                    multiplicator = 8.4f;
                    break;
                case ENTRY_VOIDWALKER:
                    multiplicator = 11.0f;
                    break;
                case ENTRY_SUCCUBUS:
                    multiplicator = 9.1f;
                    break;
                case ENTRY_FELHUNTER:
                    multiplicator = 9.5f;
                    break;
                case ENTRY_FELGUARD:
                    multiplicator = 11.0f;
                    break;
                case ENTRY_BLOODWORM:
                    multiplicator = 1.0f;
                    break;
                default:
                    multiplicator = 10.0f;
                    break;
            }

            var value = GetFlatModifierValue(unitMod, UnitModifierFlatType.Base) + GetCreateHealth();
            value *= GetPctModifierValue(unitMod, UnitModifierPctType.Base);
            value += GetFlatModifierValue(unitMod, UnitModifierFlatType.Total) + stamina * multiplicator;
            value *= GetPctModifierValue(unitMod, UnitModifierPctType.Total);

            SetMaxHealth((uint)value);
        }

        public override void UpdateMaxPower(PowerType power)
        {
            if (GetPowerIndex(power) == (uint)PowerType.Max)
                return;

            UnitMods unitMod = UnitMods.PowerStart + (int)power;

            var value = GetFlatModifierValue(unitMod, UnitModifierFlatType.Base) + GetCreatePowerValue(power);
            value *= GetPctModifierValue(unitMod, UnitModifierPctType.Base);
            value += GetFlatModifierValue(unitMod, UnitModifierFlatType.Total);
            value *= GetPctModifierValue(unitMod, UnitModifierPctType.Total);

            SetMaxPower(power, (int)value);
        }

        public override void UpdateAttackPowerAndDamage(bool ranged = false)
        {
            if (ranged)
                return;

            float val;
            double bonusAP = 0.0f;
            UnitMods unitMod = UnitMods.AttackPower;

            if (GetEntry() == ENTRY_IMP)                                   // imp's attack power
                val = GetStat(Stats.Strength) - 10.0f;
            else
                val = 2 * GetStat(Stats.Strength) - 20.0f;

            Player owner = GetOwner() ? GetOwner().ToPlayer() : null;
            if (owner != null)
            {
                if (IsHunterPet())                      //hunter pets benefit from owner's attack power
                {
                    float mod = 1.0f;                                                 //Hunter contribution modifier
                    bonusAP = owner.GetTotalAttackPowerValue(WeaponAttackType.RangedAttack) * 0.22f * mod;
                    SetBonusDamage((int)(owner.GetTotalAttackPowerValue(WeaponAttackType.RangedAttack) * 0.1287f * mod));
                }
                else if (IsPetGhoul()) //ghouls benefit from deathknight's attack power (may be summon pet or not)
                {
                    bonusAP = owner.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 0.22f;
                    SetBonusDamage((int)(owner.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 0.1287f));
                }
                else if (IsSpiritWolf()) //wolf benefit from shaman's attack power
                {
                    float dmg_multiplier = 0.31f;
                    bonusAP = owner.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * dmg_multiplier;
                    SetBonusDamage((int)(owner.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * dmg_multiplier));
                }
                //demons benefit from warlocks shadow or fire damage
                else if (IsPet())
                {
                    int fire = owner.m_activePlayerData.ModDamageDonePos[(int)SpellSchools.Fire] - owner.m_activePlayerData.ModDamageDoneNeg[(int)SpellSchools.Fire];
                    int shadow = owner.m_activePlayerData.ModDamageDonePos[(int)SpellSchools.Shadow] - owner.m_activePlayerData.ModDamageDoneNeg[(int)SpellSchools.Shadow];
                    int maximum = (fire > shadow) ? fire : shadow;
                    if (maximum < 0)
                        maximum = 0;
                    SetBonusDamage((int)(maximum * 0.15f));
                    bonusAP = maximum * 0.57f;
                }
                //water elementals benefit from mage's frost damage
                else if (GetEntry() == ENTRY_WATER_ELEMENTAL)
                {
                    int frost = owner.m_activePlayerData.ModDamageDonePos[(int)SpellSchools.Frost] - owner.m_activePlayerData.ModDamageDoneNeg[(int)SpellSchools.Frost];
                    if (frost < 0)
                        frost = 0;
                    SetBonusDamage((int)(frost * 0.4f));
                }
            }

            SetStatFlatModifier(UnitMods.AttackPower, UnitModifierFlatType.Base, val + bonusAP);

            //in BASE_VALUE of UNIT_MOD_ATTACK_POWER for creatures we store data of meleeattackpower field in DB
            var base_attPower = GetFlatModifierValue(unitMod, UnitModifierFlatType.Base) * GetPctModifierValue(unitMod, UnitModifierPctType.Base);
            var attPowerMultiplier = GetPctModifierValue(unitMod, UnitModifierPctType.Total) - 1.0f;

            SetAttackPower((int)base_attPower);
            SetAttackPowerMultiplier((float)attPowerMultiplier);

            //automatically update weapon damage after attack power modification
            UpdateDamagePhysical(WeaponAttackType.BaseAttack);
        }

        public override void UpdateDamagePhysical(WeaponAttackType attType)
        {
            if (attType > WeaponAttackType.BaseAttack)
                return;

            float bonusDamage = 0.0f;
            Player playerOwner = m_owner.ToPlayer();
            if (playerOwner != null)
            {
                //force of nature
                if (GetEntry() == ENTRY_TREANT)
                {
                    int spellDmg = playerOwner.m_activePlayerData.ModDamageDonePos[(int)SpellSchools.Nature] - playerOwner.m_activePlayerData.ModDamageDoneNeg[(int)SpellSchools.Nature];
                    if (spellDmg > 0)
                        bonusDamage = spellDmg * 0.09f;
                }
                //greater fire elemental
                else if (GetEntry() == ENTRY_FIRE_ELEMENTAL)
                {
                    int spellDmg = playerOwner.m_activePlayerData.ModDamageDonePos[(int)SpellSchools.Fire] - playerOwner.m_activePlayerData.ModDamageDoneNeg[(int)SpellSchools.Fire];
                    if (spellDmg > 0)
                        bonusDamage = spellDmg * 0.4f;
                }
            }

            UnitMods unitMod = UnitMods.DamageMainHand;

            double att_speed = GetBaseAttackTime(WeaponAttackType.BaseAttack) / 1000.0f;

            var base_value = GetFlatModifierValue(unitMod, UnitModifierFlatType.Base) + GetTotalAttackPowerValue(attType, false) / 3.5f * att_speed + bonusDamage;
            var base_pct = GetPctModifierValue(unitMod, UnitModifierPctType.Base);
            var total_value = GetFlatModifierValue(unitMod, UnitModifierFlatType.Total);
            var total_pct = GetPctModifierValue(unitMod, UnitModifierPctType.Total);

            var weapon_mindamage = GetWeaponDamageRange(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage);
            var weapon_maxdamage = GetWeaponDamageRange(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage);

            var mindamage = ((base_value + weapon_mindamage) * base_pct + total_value) * total_pct;
            var maxdamage = ((base_value + weapon_maxdamage) * base_pct + total_value) * total_pct;

            SetUpdateFieldStatValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.MinDamage), (float)mindamage);
            SetUpdateFieldStatValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.MaxDamage), (float)maxdamage);
        }

        void SetBonusDamage(float damage)
        {
            m_bonusSpellDamage = damage;
            Player playerOwner = GetOwner().ToPlayer();
            if (playerOwner != null)
                playerOwner.SetPetSpellPower((uint)damage);
        }

        public float GetBonusDamage() { return m_bonusSpellDamage; }
        public float GetBonusStatFromOwner(Stats stat) { return m_statFromOwner[(int)stat]; }

        float m_bonusSpellDamage;
        readonly float[] m_statFromOwner = new float[(int)Stats.Max];
    }

    public class Puppet : Minion
    {
        public Puppet(SummonPropertiesRecord properties, Unit owner) : base(properties, owner, false)
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
            {
                if (!IsAlive())
                {
                    UnSummon();
                    // @todo why long distance .die does not remove it
                }
            }
        }
    }

    public class ForcedUnsummonDelayEvent : BasicEvent
    {
        public ForcedUnsummonDelayEvent(TempSummon owner)
        {
            m_owner = owner;
        }
        public override bool Execute(ulong e_time, uint p_time)
        {
            m_owner.UnSummon();
            return true;
        }

        readonly TempSummon m_owner;
    }

    public class TempSummonData
    {
        public uint entry;        // Entry of summoned creature
        public Position pos;        // Position, where should be creature spawned
        public TempSummonType type; // Summon type, see TempSummonType for available types
        public uint time;         // Despawn time, usable only with certain temp summon types
    }

    enum PetEntry
    {
        // Warlock pets
        Imp = 416,
        FelHunter = 691,
        VoidWalker = 1860,
        Succubus = 1863,
        Doomguard = 18540,
        Felguard = 30146,

        // Death Knight pets
        Ghoul = 26125,
        Abomination = 106848,

        // Shaman pet
        SpiritWolf = 29264
    }
}
