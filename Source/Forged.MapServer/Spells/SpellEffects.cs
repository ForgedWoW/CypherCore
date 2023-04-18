// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Forged.MapServer.Chat.Commands;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Events;
using Forged.MapServer.Groups;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Maps.Workers;
using Forged.MapServer.Movement;
using Forged.MapServer.Networking.Packets.Combat;
using Forged.MapServer.Networking.Packets.CombatLog;
using Forged.MapServer.Networking.Packets.Duel;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Networking.Packets.Talent;
using Forged.MapServer.Networking.Packets.Trait;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Forged.MapServer.Scripting.Interfaces.IQuest;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Dynamic;
using Framework.Util;
using Serilog;

namespace Forged.MapServer.Spells;

public partial class Spell
{
    public void DoCreateItem(uint itemId, ItemContext context = 0, List<uint> bonusListIds = null)
    {
        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        var player = UnitTarget.AsPlayer;

        var newitemid = itemId;
        var pProto = _gameObjectManager.GetItemTemplate(newitemid);

        if (pProto == null)
        {
            player.SendEquipError(InventoryResult.ItemNotFound);

            return;
        }

        var numToAdd = (uint)Damage;

        if (numToAdd < 1)
            numToAdd = 1;

        if (numToAdd > pProto.MaxStackSize)
            numToAdd = pProto.MaxStackSize;

        // this is bad, should be done using spell_loot_template (and conditions)

        // the chance of getting a perfect result
        double perfectCreateChance = 0.0f;
        // the resulting perfect item if successful
        var perfectItemType = itemId;

        // get perfection capability and chance
        if (_skillPerfectItems.CanCreatePerfectItem(player, SpellInfo.Id, ref perfectCreateChance, ref perfectItemType))
            if (RandomHelper.randChance(perfectCreateChance)) // if the roll succeeds...
                newitemid = perfectItemType;                  // the perfect item replaces the regular one

        // init items_count to 1, since 1 item will be created regardless of specialization
        var itemsCount = 1;
        // the chance to create additional items
        double additionalCreateChance = 0.0f;
        // the maximum number of created additional items
        byte additionalMaxNum = 0;

        // get the chance and maximum number for creating extra items
        if (_skillExtraItems.CanCreateExtraItems(player, SpellInfo.Id, ref additionalCreateChance, ref additionalMaxNum))
            // roll with this chance till we roll not to create or we create the max num
            while (RandomHelper.randChance(additionalCreateChance) && itemsCount <= additionalMaxNum)
                ++itemsCount;

        // really will be created more items
        numToAdd *= (uint)itemsCount;

        // can the player store the new item?
        List<ItemPosCount> dest = new();
        var msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, newitemid, numToAdd, out var noSpace);

        if (msg != InventoryResult.Ok)
        {
            // convert to possible store amount
            if (msg is InventoryResult.InvFull or InventoryResult.ItemMaxCount)
                numToAdd -= noSpace;
            else
            {
                // if not created by another reason from full inventory or unique items amount limitation
                player.SendEquipError(msg, null, null, newitemid);

                return;
            }
        }

        if (numToAdd != 0)
        {
            // create the new item and store it
            var pItem = player.StoreNewItem(dest, newitemid, true, _itemEnchantmentManager.GenerateItemRandomBonusListId(newitemid), null, context, bonusListIds);

            // was it successful? return error if not
            if (pItem == null)
            {
                player.SendEquipError(InventoryResult.ItemNotFound);

                return;
            }

            // set the "Crafted by ..." property of the item
            if (pItem.Template.HasSignature)
                pItem.SetCreator(player.GUID);

            // send info to the client
            player.SendNewItem(pItem, numToAdd, true, true);

            if (pItem.Quality > ItemQuality.Epic || (pItem.Quality == ItemQuality.Epic && pItem.GetItemLevel(player) >= GuildConst.MinNewsItemLevel))
            {
                var guild = player.Guild;

                guild?.AddGuildNews(GuildNews.ItemCrafted, player.GUID, 0, pProto.Id);
            }

            // we succeeded in creating at least one item, so a levelup is possible
            player.UpdateCraftSkill(SpellInfo);
        }
    }

    [SpellEffectHandler(SpellEffectName.ChangeItemBonuses)]
    public void EffectChangeItemBonuses()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        var player = Caster.AsPlayer;

        if (player == null)
            return;

        var item = Targets.ItemTarget;

        if (item is not { IsSoulBound: true })
            return;

        var oldItemBonusTree = EffectInfo.MiscValue;
        var newItemBonusTree = EffectInfo.MiscValue;

        if (oldItemBonusTree == newItemBonusTree) // Not release
            return;

        var oldBonusTree = _db2Manager.GetItemBonusSet((uint)oldItemBonusTree);
        var newBonusTre = _db2Manager.GetItemBonusSet((uint)newItemBonusTree);

        if (oldBonusTree == null || newBonusTre == null)
            return;

        var bonuses = newBonusTre.Select(s => s.ChildItemBonusListID).ToList();

        var found = false;
        uint treeMod = 0;

        foreach (var bonus in bonuses)
        {
            foreach (var oldBonus in oldBonusTree)
                if (bonus == oldBonus.ChildItemBonusListID)
                {
                    found = true;
                    treeMod = oldBonus.ItemContext;

                    break;
                }
        }

        if (!found)
            return;

        var bonusesNew = new List<uint>();

        foreach (var bonus in bonuses)
        {
            var bonusDel = false;

            foreach (var oldBonus in oldBonusTree)
                if (bonus == oldBonus.ChildItemBonusListID && treeMod == oldBonus.ItemContext)
                {
                    bonusDel = true;

                    break;
                }

            if (!bonusDel)
                bonusesNew.Add(bonus);
        }

        item.BonusData = new BonusData(item.Template);

        foreach (var newBonus in newBonusTre)
            if (treeMod == newBonus.ItemContext)
                bonusesNew.Add(newBonus.ChildItemBonusListID);

        foreach (var bonusId in bonusesNew)
            item.AddBonuses(bonusId);

        item.SetState(ItemUpdateState.Changed, player);
    }

    [SpellEffectHandler(SpellEffectName.ChangePartyMembers)]
    public void EffectJoinOrLeavePlayerParty()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || Caster == null || !UnitTarget.IsPlayer)
            return;

        var player = UnitTarget.AsPlayer;

        var group = player.Group;
        var creature = Caster.AsCreature;

        if (creature == null)
            return;

        if (group == null)
        {
            group = _classFactory.Resolve<PlayerGroup>();
            group.Create(player);
            // group->ConvertToLFG(dungeon);
            group.SetDungeonDifficultyID(SpellInfo.Difficulty);
            _groupManager.AddGroup(group);
        }
        else if (group.IsMember(creature.GUID))
            return;

        /* if (m_spellInfo->GetEffect(effIndex, m_diffMode)->MiscValue == 1)
            group->AddCreatureMember(creature);
        else
            group->RemoveCreatureMember(creature->GetGUID());*/
    }

    [SpellEffectHandler(SpellEffectName.SetCovenant)]
    public void EffectSetCovenant()
    {
        if (!UnitTarget.TryGetAsPlayer(out var player))
            return;

        sbyte covenantId = 0; // TODO

        player.SetCovenant(covenantId);
    }

    private void CalculateJumpSpeeds(SpellEffectInfo effInfo, float dist, out float speedXY, out float speedZ)
    {
        var unitCaster = UnitCasterForEffectHandlers;
        var runSpeed = unitCaster.ControlledByPlayer ? SharedConst.playerBaseMoveSpeed[(int)UnitMoveType.Run] : SharedConst.baseMoveSpeed[(int)UnitMoveType.Run];
        var creature = unitCaster.AsCreature;

        if (creature != null)
            runSpeed *= creature.Template.SpeedRun;

        var multiplier = (float)effInfo.Amplitude;

        if (multiplier <= 0.0f)
            multiplier = 1.0f;

        speedXY = Math.Min(runSpeed * 3.0f * multiplier, Math.Max(28.0f, unitCaster.GetSpeed(UnitMoveType.Run) * 4.0f));

        var duration = dist / speedXY;
        var durationSqr = duration * duration;
        var minHeight = effInfo.MiscValue != 0 ? effInfo.MiscValue / 10.0f : 0.5f;      // Lower bound is blizzlike
        var maxHeight = effInfo.MiscValueB != 0 ? effInfo.MiscValueB / 10.0f : 1000.0f; // Upper bound is unknown
        float height;

        if (durationSqr < minHeight * 8 / MotionMaster.GRAVITY)
            height = minHeight;
        else if (durationSqr > maxHeight * 8 / MotionMaster.GRAVITY)
            height = maxHeight;
        else
            height = (float)(MotionMaster.GRAVITY * durationSqr / 8);

        speedZ = MathF.Sqrt((float)(2 * MotionMaster.GRAVITY * height));
    }

    [SpellEffectHandler(SpellEffectName.ActivateGarrisonBuilding)]
    private void EffectActivateGarrisonBuilding()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        var garrison = UnitTarget.AsPlayer.Garrison;

        garrison?.ActivateBuilding((uint)EffectInfo.MiscValue);
    }

    [SpellEffectHandler(SpellEffectName.ActivateObject)]
    private void EffectActivateObject()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        GameObjTarget?.ActivateObject((GameObjectActions)EffectInfo.MiscValue, EffectInfo.MiscValueB, Caster, SpellInfo.Id, EffectInfo.EffectIndex);
    }

    [SpellEffectHandler(SpellEffectName.TalentSpecSelect)]
    private void EffectActivateSpec()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        var player = UnitTarget.AsPlayer;
        var specID = SpellMisc.SpecializationId;
        var spec = _cliDb.ChrSpecializationStorage.LookupByKey(specID);

        // Safety checks done in Spell::CheckCast
        if (!spec.IsPetSpecialization())
            player.ActivateTalentGroup(spec);
        else
            player.CurrentPet.SetSpecialization(specID);
    }

    [SpellEffectHandler(SpellEffectName.AddExtraAttacks)]
    private void EffectAddExtraAttacks()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget is not { IsAlive: true })
            return;

        UnitTarget.AddExtraAttacks((uint)Damage);

        ExecuteLogEffectExtraAttacks(EffectInfo.Effect, UnitTarget, (uint)Damage);
    }

    [SpellEffectHandler(SpellEffectName.AddFarsight)]
    private void EffectAddFarsight()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        var player = Caster.AsPlayer;

        if (player == null)
            return;

        var radius = EffectInfo.CalcRadius();
        var duration = SpellInfo.CalcDuration(Caster);

        // Caster not in world, might be spell triggered from aura removal
        if (!player.Location.IsInWorld)
            return;

        DynamicObject dynObj = new(true);

        if (!dynObj.CreateDynamicObject(player.Location.Map.GenerateLowGuid(HighGuid.DynamicObject), player, SpellInfo, DestTarget, radius, DynamicObjectType.FarsightFocus, SpellVisual))
            return;

        dynObj.SetDuration(duration);
        dynObj.SetCasterViewpoint();
    }

    [SpellEffectHandler(SpellEffectName.AddGarrisonFollower)]
    private void EffectAddGarrisonFollower()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        var garrison = UnitTarget.AsPlayer.Garrison;

        garrison?.AddFollower((uint)EffectInfo.MiscValue);
    }

    [SpellEffectHandler(SpellEffectName.ApplyAura)]
    [SpellEffectHandler(SpellEffectName.ApplyAuraOnPet)]
    private void EffectApplyAura()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (SpellAura == null || UnitTarget == null)
            return;

        SpellAura.EmpoweredStage = EmpoweredStage;

        // register target/effect on aura
        var aurApp = SpellAura.GetApplicationOfTarget(UnitTarget.GUID);

        if (aurApp == null)
            aurApp = UnitTarget._CreateAuraApplication(SpellAura,
                                                       new HashSet<int>
                                                       {
                                                           EffectInfo.EffectIndex
                                                       });
        else
        {
            aurApp.EffectsToApply.Add(EffectInfo.EffectIndex);
            aurApp.UpdateApplyEffectMask(aurApp.EffectsToApply, false);
        }

        if (TryGetTotalEmpowerDuration(true, out var dur))
            SpellAura.SetDuration(dur, false, true);
    }

    [SpellEffectHandler(SpellEffectName.ApplyEnchantIllusion)]
    private void EffectApplyEnchantIllusion()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (ItemTarget == null)
            return;

        var player = Caster.AsPlayer;

        if (player == null || player.GUID != ItemTarget.OwnerGUID)
            return;

        ItemTarget.SetState(ItemUpdateState.Changed, player);
        ItemTarget.SetModifier(ItemModifier.EnchantIllusionAllSpecs, (uint)EffectInfo.MiscValue);

        if (ItemTarget.IsEquipped)
            player.SetVisibleItemSlot(ItemTarget.Slot, ItemTarget);

        player.RemoveTradeableItem(ItemTarget);
        ItemTarget.ClearSoulboundTradeable(player);
    }

    [SpellEffectHandler(SpellEffectName.ApplyGlyph)]
    private void EffectApplyGlyph()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        var player = Caster.AsPlayer;

        if (player == null)
            return;

        var glyphs = player.GetGlyphs(player.GetActiveTalentGroup());
        var replacedGlyph = glyphs.Count;

        for (var i = 0; i < glyphs.Count; ++i)
        {
            var activeGlyphBindableSpells = player.DB2Manager.GetGlyphBindableSpells(glyphs[i]);

            if (!activeGlyphBindableSpells.Contains(SpellMisc.SpellId))
                continue;

            replacedGlyph = i;
            player.RemoveAura(_cliDb.GlyphPropertiesStorage.LookupByKey(glyphs[i]).SpellID);

            break;
        }

        var glyphId = (uint)EffectInfo.MiscValue;

        if (replacedGlyph < glyphs.Count)
        {
            if (glyphId != 0)
                glyphs[replacedGlyph] = glyphId;
            else
                glyphs.RemoveAt(replacedGlyph);
        }
        else if (glyphId != 0)
            glyphs.Add(glyphId);

        player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags2.ChangeGlyph);

        if (_cliDb.GlyphPropertiesStorage.TryGetValue(glyphId, out var glyphProperties))
            player.SpellFactory.CastSpell(player, glyphProperties.SpellID, new CastSpellExtraArgs(this));

        ActiveGlyphs activeGlyphs = new();
        activeGlyphs.Glyphs.Add(new GlyphBinding(SpellMisc.SpellId, (ushort)glyphId));
        activeGlyphs.IsFullUpdate = false;
        player.SendPacket(activeGlyphs);
    }

    [SpellEffectHandler(SpellEffectName.Bind)]
    private void EffectBind()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        var player = UnitTarget.AsPlayer;

        WorldLocation homeLoc = new();
        var areaId = player.Location.Area;

        if (EffectInfo.MiscValue != 0)
            areaId = (uint)EffectInfo.MiscValue;

        if (Targets.HasDst)
            homeLoc.WorldRelocate(DestTarget);
        else
            homeLoc = new WorldLocation(player.Location);

        player.SetHomebind(homeLoc, areaId);
        player.SendBindPointUpdate();

        Log.Logger.Debug($"EffectBind: New homebind: {homeLoc}, AreaId: {areaId}");

        // zone update
        player.SendPlayerBound(Caster.GUID, areaId);
    }

    [SpellEffectHandler(SpellEffectName.Block)]
    private void EffectBlock()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        if (Caster.IsTypeId(TypeId.Player))
            Caster.AsPlayer.SetCanBlock(true);
    }

    [SpellEffectHandler(SpellEffectName.CancelConversation)]
    private void EffectCancelConversation()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null)
            return;

        List<WorldObject> objs = new();
        ObjectEntryAndPrivateOwnerIfExistsCheck check = new(UnitTarget.GUID, (uint)EffectInfo.MiscValue);
        WorldObjectListSearcher checker = new(UnitTarget, objs, check, GridMapTypeMask.Conversation, GridType.Grid);
        _cellCalculator.VisitGrid(UnitTarget, checker, 100.0f);

        foreach (var obj in objs)
        {
            var convo = obj.AsConversation;

            convo?.Remove();
        }
    }

    [SpellEffectHandler(SpellEffectName.CastButton)]
    private void EffectCastButtons()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        var player = Caster.AsPlayer;

        if (player == null)
            return;

        var buttonID = EffectInfo.MiscValue + 132;
        var nButtons = EffectInfo.MiscValueB;

        for (; nButtons != 0; --nButtons, ++buttonID)
        {
            var ab = player.GetActionButton((byte)buttonID);

            if (ab == null || ab.GetButtonType() != ActionButtonType.Spell)
                continue;

            //! Action button data is unverified when it's set so it can be "hacked"
            //! to contain invalid spells, so filter here.
            var spellID = (uint)ab.GetAction();

            if (spellID == 0)
                continue;

            var spellInfo = player.SpellManager.GetSpellInfo(spellID, CastDifficulty);

            if (spellInfo == null)
                continue;

            if (!player.HasSpell(spellID) || player.SpellHistory.HasCooldown(spellID))
                continue;

            if (!spellInfo.HasAttribute(SpellAttr9.SummonPlayerTotem))
                continue;

            CastSpellExtraArgs args = new(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.CastDirectly | TriggerCastFlags.DontReportCastError)
            {
                OriginalCastId = CastId,
                CastDifficulty = CastDifficulty
            };

            Caster.SpellFactory.CastSpell(Caster, spellInfo.Id, args);
        }
    }

    [SpellEffectHandler(SpellEffectName.ChangeActiveCombatTraitConfig)]
    private void EffectChangeActiveCombatTraitConfig()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var target = UnitTarget?.AsPlayer;

        if (target == null)
            return;

        if (CustomArg is not TraitConfigPacket arg)
            return;

        target.UpdateTraitConfig(arg, (int)Damage, false);
    }

    [SpellEffectHandler(SpellEffectName.ChangeBattlepetQuality)]
    private void EffectChangeBattlePetQuality()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var playerCaster = Caster.AsPlayer;

        if (playerCaster == null)
            return;

        if (UnitTarget is not { IsCreature: true })
            return;

        var qualityRecord = _cliDb.BattlePetBreedQualityStorage.Values.FirstOrDefault(a1 => a1.MaxQualityRoll < Damage);

        var quality = BattlePetBreedQuality.Poor;

        if (qualityRecord != null)
            quality = (BattlePetBreedQuality)qualityRecord.QualityEnum;

        playerCaster.Session.BattlePetMgr.ChangeBattlePetQuality(UnitTarget.BattlePetCompanionGUID, quality);
    }

    [SpellEffectHandler(SpellEffectName.ChangeRaidMarker)]
    private void EffectChangeRaidMarker()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        var player = Caster.AsPlayer;

        if (player == null || !Targets.HasDst)
            return;

        var group = player.Group;

        if (group == null || (group.IsRaidGroup && !group.IsLeader(player.GUID) && !group.IsAssistant(player.GUID)))
            return;

        group.AddRaidMarker((byte)Damage, player.Location.MapId, DestTarget.X, DestTarget.Y, DestTarget.Z);
    }

    [SpellEffectHandler(SpellEffectName.Charge)]
    private void EffectCharge()
    {
        if (UnitTarget == null)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster == null)
            return;

        if (_effectHandleMode == SpellEffectHandleMode.LaunchTarget)
        {
            // charge changes fall time
            if (unitCaster.IsPlayer)
                unitCaster.AsPlayer.SetFallInformation(0, Caster.Location.Z);

            var speed = MathFunctions.fuzzyGt(SpellInfo.Speed, 0.0f) ? SpellInfo.Speed : MotionMaster.SPEED_CHARGE;
            SpellEffectExtraData spellEffectExtraData = null;

            if (EffectInfo.MiscValueB != 0)
                spellEffectExtraData = new SpellEffectExtraData
                {
                    Target = UnitTarget.GUID,
                    SpellVisualId = (uint)EffectInfo.MiscValueB
                };

            // Spell is not using explicit target - no generated path
            if (_preGeneratedPath == null)
            {
                var pos = UnitTarget.Location.GetFirstCollisionPosition(UnitTarget.CombatReach, UnitTarget.Location.GetRelativeAngle(Caster.Location));

                if (MathFunctions.fuzzyGt(SpellInfo.Speed, 0.0f) && SpellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
                    speed = pos.GetExactDist(Caster.Location) / speed;

                unitCaster.MotionMaster.MoveCharge(pos.X, pos.Y, pos.Z, speed, EventId.Charge, false, UnitTarget, spellEffectExtraData);
            }
            else
            {
                if (MathFunctions.fuzzyGt(SpellInfo.Speed, 0.0f) && SpellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
                {
                    var pos = _preGeneratedPath.ActualEndPosition;
                    speed = new Position(pos.X, pos.Y, pos.Z).GetExactDist(Caster.Location) / speed;
                }

                unitCaster.MotionMaster.MoveCharge(_preGeneratedPath, speed, UnitTarget, spellEffectExtraData);
            }
        }

        if (_effectHandleMode == SpellEffectHandleMode.HitTarget)
        {
            // not all charge effects used in negative spells
            if (!SpellInfo.IsPositive && Caster.IsTypeId(TypeId.Player))
                unitCaster.Attack(UnitTarget, true);

            if (EffectInfo.TriggerSpell != 0)
                Caster.SpellFactory.CastSpell(UnitTarget,
                                              EffectInfo.TriggerSpell,
                                              new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                                                  .SetOriginalCaster(_originalCasterGuid)
                                                  .SetTriggeringSpell(this));
        }
    }

    [SpellEffectHandler(SpellEffectName.ChargeDest)]
    private void EffectChargeDest()
    {
        if (DestTarget == null)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster == null)
            return;

        if (_effectHandleMode == SpellEffectHandleMode.Launch)
        {
            var pos = DestTarget.Copy();

            if (!unitCaster.Location.IsWithinLOS(pos))
            {
                var angle = unitCaster.Location.GetRelativeAngle(pos.X, pos.Y);
                var dist = unitCaster.Location.GetDistance(pos);
                pos = unitCaster.Location.GetFirstCollisionPosition(dist, angle);
            }

            unitCaster.MotionMaster.MoveCharge(pos.X, pos.Y, pos.Z);
        }
        else if (_effectHandleMode == SpellEffectHandleMode.Hit)
            if (EffectInfo.TriggerSpell != 0)
                Caster.SpellFactory.CastSpell(DestTarget,
                                              EffectInfo.TriggerSpell,
                                              new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                                                  .SetOriginalCaster(_originalCasterGuid)
                                                  .SetTriggeringSpell(this));
    }

    [SpellEffectHandler(SpellEffectName.CreateAreaTrigger)]
    private void EffectCreateAreaTrigger()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster == null || !Targets.HasDst)
            return;

        var duration = SpellInfo.CalcDuration(Caster);
        AreaTrigger.CreateAreaTrigger(_classFactory, (uint)EffectInfo.MiscValue, unitCaster, null, SpellInfo, DestTarget, duration, SpellVisual, CastId);
    }

    [SpellEffectHandler(SpellEffectName.CreateConversation)]
    private void EffectCreateConversation()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster == null || !Targets.HasDst)
            return;

        Conversation.CreateConversation((uint)EffectInfo.MiscValue, unitCaster, DestTarget, ObjectGuid.Empty, SpellInfo);
    }

    [SpellEffectHandler(SpellEffectName.CreateGarrison)]
    private void EffectCreateGarrison()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        UnitTarget.AsPlayer.CreateGarrison((uint)EffectInfo.MiscValue);
    }

    [SpellEffectHandler(SpellEffectName.CreateHeirloomItem)]
    private void EffectCreateHeirloomItem()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var player = Caster.AsPlayer;

        if (player == null)
            return;

        var collectionMgr = player.Session.CollectionMgr;

        if (collectionMgr == null)
            return;

        List<uint> bonusList = new()
        {
            collectionMgr.GetHeirloomBonus(SpellMisc.Data0)
        };

        DoCreateItem(SpellMisc.Data0, ItemContext.None, bonusList);
        ExecuteLogEffectCreateItem(EffectInfo.Effect, SpellMisc.Data0);
    }

    [SpellEffectHandler(SpellEffectName.CreateItem)]
    private void EffectCreateItem()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        DoCreateItem(EffectInfo.ItemType, SpellInfo.HasAttribute(SpellAttr0.IsTradeskill) ? ItemContext.TradeSkill : ItemContext.None);
        ExecuteLogEffectCreateItem(EffectInfo.Effect, EffectInfo.ItemType);
    }

    [SpellEffectHandler(SpellEffectName.CreateLoot)]
    private void EffectCreateItem2()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        var player = UnitTarget.AsPlayer;

        var context = SpellInfo.HasAttribute(SpellAttr0.IsTradeskill) ? ItemContext.TradeSkill : ItemContext.None;

        // Pick a random item from spell_loot_template
        if (SpellInfo.IsLootCrafting)
        {
            player.AutoStoreLoot(SpellInfo.Id, _lootStoreBox.Spell, context, false, true);
            player.UpdateCraftSkill(SpellInfo);
        }
        else // If there's no random loot entries for this spell, pick the item associated with this spell
        {
            var itemId = EffectInfo.ItemType;

            if (itemId != 0)
                DoCreateItem(itemId, context);
        }

        // @todo ExecuteLogEffectCreateItem(i, GetEffect(i].ItemType);
    }

    [SpellEffectHandler(SpellEffectName.CreatePrivateConversation)]
    private void EffectCreatePrivateConversation()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster is not { IsPlayer: true })
            return;

        Conversation.CreateConversation((uint)EffectInfo.MiscValue, unitCaster, DestTarget, unitCaster.GUID, SpellInfo);
    }

    [SpellEffectHandler(SpellEffectName.CreatePersonalSceneObject)]
    private void EffectCreatePrivateSceneObject()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster == null || !Targets.HasDst)
            return;

        var sceneObject = SceneObject.CreateSceneObject((uint)EffectInfo.MiscValue, unitCaster, DestTarget, unitCaster.GUID);

        if (sceneObject == null)
            return;

        var hasAuraTargetingCaster = UniqueTargetInfo.Any(target => IsUnitTargetSceneObjectAura(this, target));

        if (hasAuraTargetingCaster)
            sceneObject.SetCreatedBySpellCast(CastId);
    }

    [SpellEffectHandler(SpellEffectName.CreateRandomItem)]
    private void EffectCreateRandomItem()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        var player = UnitTarget.AsPlayer;

        // create some random items
        player.AutoStoreLoot(SpellInfo.Id, _lootStoreBox.Spell, SpellInfo.HasAttribute(SpellAttr0.IsTradeskill) ? ItemContext.TradeSkill : ItemContext.None);
        // @todo ExecuteLogEffectCreateItem(i, GetEffect(i].ItemType);
    }

    [SpellEffectHandler(SpellEffectName.CreateSceneObject)]
    private void EffectCreateSceneObject()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster == null || !Targets.HasDst)
            return;

        var sceneObject = SceneObject.CreateSceneObject((uint)EffectInfo.MiscValue, unitCaster, DestTarget, ObjectGuid.Empty);

        if (sceneObject == null)
            return;

        var hasAuraTargetingCaster = UniqueTargetInfo.Any(target => IsUnitTargetSceneObjectAura(this, target));

        if (hasAuraTargetingCaster)
            sceneObject.SetCreatedBySpellCast(CastId);
    }

    [SpellEffectHandler(SpellEffectName.CreateTamedPet)]
    private void EffectCreateTamedPet()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player) || !UnitTarget.PetGUID.IsEmpty || UnitTarget.Class != PlayerClass.Hunter)
            return;

        var creatureEntry = (uint)EffectInfo.MiscValue;
        var pet = UnitTarget.CreateTamedPetFrom(creatureEntry, SpellInfo.Id);

        if (pet == null)
            return;

        // relocate
        var pos = new Position();
        UnitTarget.Location.GetClosePoint(pos, pet.CombatReach, SharedConst.PetFollowDist, pet.FollowAngle);
        pos.Orientation = UnitTarget.Location.Orientation;
        pet.Location.Relocate(pos);

        // add to world
        pet.Location.
            // add to world
            Map.AddToMap(pet.AsCreature);

        // unitTarget has pet now
        UnitTarget.SetMinion(pet, true);

        if (UnitTarget.IsTypeId(TypeId.Player))
        {
            pet.SavePetToDB(PetSaveMode.AsCurrent);
            UnitTarget.AsPlayer.PetSpellInitialize();
        }
    }

    [SpellEffectHandler(SpellEffectName.CreateTraitTreeConfig)]
    private void EffectCreateTraitTreeConfig()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var target = UnitTarget?.AsPlayer;

        if (target == null)
            return;

        TraitConfigPacket newConfig = new()
        {
            Type = _traitMgr.GetConfigTypeForTree(EffectInfo.MiscValue)
        };

        if (newConfig.Type != TraitConfigType.Generic)
            return;

        newConfig.TraitSystemID = _cliDb.TraitTreeStorage.LookupByKey(EffectInfo.MiscValue).TraitSystemID;
        target.CreateTraitConfig(newConfig);
    }

    [SpellEffectHandler(SpellEffectName.DamageFromMaxHealthPCT)]
    private void EffectDamageFromMaxHealthPct()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null)
            return;

        DamageInEffects += (int)UnitTarget.CountPctFromMaxHealth(Damage);
    }

    [SpellEffectHandler(SpellEffectName.DestroyAllTotems)]
    private void EffectDestroyAllTotems()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster == null)
            return;

        var mana = 0;

        for (byte slot = (int)SummonSlot.Totem; slot < SharedConst.MaxTotemSlot; ++slot)
        {
            if (unitCaster.SummonSlot[slot].IsEmpty)
                continue;

            var totem = unitCaster.Location.Map.GetCreature(unitCaster.SummonSlot[slot]);

            if (totem is { IsTotem: true })
            {
                uint spellID = totem.UnitData.CreatedBySpell;
                var spellInfo = _spellManager.GetSpellInfo(spellID, CastDifficulty);

                if (spellInfo != null)
                {
                    var costs = spellInfo.CalcPowerCost(unitCaster, spellInfo.SchoolMask);
                    var m = costs.Find(cost => cost.Power == PowerType.Mana);

                    if (m != null)
                        mana += m.Amount;
                }

                totem.ToTotem().UnSummon();
            }
        }

        MathFunctions.ApplyPct(ref mana, Damage);

        if (mana != 0)
        {
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.SetTriggeringSpell(this);
            args.AddSpellMod(SpellValueMod.BasePoint0, mana);
            unitCaster.SpellFactory.CastSpell(Caster, 39104, args);
        }
    }

    [SpellEffectHandler(SpellEffectName.DestroyItem)]
    private void EffectDestroyItem()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        var player = UnitTarget.AsPlayer;
        var item = player.GetItemByEntry(EffectInfo.ItemType);

        if (item != null)
            player.DestroyItem(item.BagSlot, item.Slot, true);
    }

    [SpellEffectHandler(SpellEffectName.DiscoverTaxi)]
    private void EffectDiscoverTaxi()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        var nodeid = (uint)EffectInfo.MiscValue;

        if (_cliDb.TaxiNodesStorage.ContainsKey(nodeid))
            UnitTarget.AsPlayer.Session.SendDiscoverNewTaxiNode(nodeid);
    }

    [SpellEffectHandler(SpellEffectName.Disenchant)]
    private void EffectDisEnchant()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var caster = Caster.AsPlayer;

        if (caster != null)
        {
            caster.UpdateCraftSkill(SpellInfo);
            ItemTarget.Loot = _lootFactory.GenerateLoot(caster.Location.Map, ItemTarget.GUID, LootType.Disenchanting, ItemTarget.GetDisenchantLoot(caster).Id, LootStorageType.Disenchant, caster, true);
            caster.SendLoot(ItemTarget.Loot);
        }

        // item will be removed at disenchanting end
    }

    [SpellEffectHandler(SpellEffectName.DismissPet)]
    private void EffectDismissPet()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget is not { IsPet: true })
            return;

        var pet = UnitTarget.AsPet;

        ExecuteLogEffectUnsummonObject(EffectInfo.Effect, pet);
        pet.Remove(PetSaveMode.NotInSlot);
    }

    [SpellEffectHandler(SpellEffectName.Dispel)]
    private void EffectDispel()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null)
            return;

        // Create dispel mask by dispel type
        var dispelType = (uint)EffectInfo.MiscValue;
        var dispelMask = SpellInfo.GetDispelMask((DispelType)dispelType);

        var dispelList = UnitTarget.GetDispellableAuraList(Caster, dispelMask, TargetMissInfo == SpellMissInfo.Reflect);

        if (dispelList.Empty())
            return;

        var remaining = dispelList.Count;

        // Ok if exist some buffs for dispel try dispel it
        List<DispelableAura> successList = new();

        DispelFailed dispelFailed = new()
        {
            CasterGUID = Caster.GUID,
            VictimGUID = UnitTarget.GUID,
            SpellID = SpellInfo.Id
        };

        // dispel N = damage buffs (or while exist buffs for dispel)
        for (var count = 0; count < Damage && remaining > 0;)
        {
            // Random select buff for dispel
            var dispelableAura = dispelList[RandomHelper.IRand(0, remaining - 1)];

            if (dispelableAura.RollDispel())
            {
                var successAura = successList.Find(dispelAura =>
                {
                    if (dispelAura.Aura.Id == dispelableAura.Aura.Id && dispelAura.Aura.Caster == dispelableAura.Aura.Caster)
                        return true;

                    return false;
                });

                byte dispelledCharges = 1;

                if (dispelableAura.Aura.SpellInfo.HasAttribute(SpellAttr1.DispelAllStacks))
                    dispelledCharges = dispelableAura.DispelCharges;

                if (successAura == null)
                    successList.Add(new DispelableAura(dispelableAura.Aura, 0, dispelledCharges));
                else
                    successAura.IncrementCharges();

                if (!dispelableAura.DecrementCharge(dispelledCharges))
                {
                    --remaining;
                    dispelList[remaining] = dispelableAura;
                }
            }
            else
                dispelFailed.FailedSpells.Add(dispelableAura.Aura.Id);

            ++count;
        }

        if (!dispelFailed.FailedSpells.Empty())
            Caster.SendMessageToSet(dispelFailed, true);

        if (successList.Empty())
            return;

        SpellDispellLog spellDispellLog = new()
        {
            IsBreak = false, // TODO: use me
            IsSteal = false,
            TargetGUID = UnitTarget.GUID,
            CasterGUID = Caster.GUID,
            DispelledBySpellID = SpellInfo.Id
        };

        foreach (var dispelableAura in successList)
        {
            var dispellData = new SpellDispellData
            {
                SpellID = dispelableAura.Aura.Id,
                Harmful = false // TODO: use me
            };

            UnitTarget.RemoveAurasDueToSpellByDispel(dispelableAura.Aura.Id, SpellInfo.Id, dispelableAura.Aura.CasterGuid, Caster, dispelableAura.DispelCharges);

            spellDispellLog.DispellData.Add(dispellData);
        }

        Caster.SendMessageToSet(spellDispellLog, true);

        CallScriptSuccessfulDispel(EffectInfo.EffectIndex);
    }

    [SpellEffectHandler(SpellEffectName.DispelMechanic)]
    private void EffectDispelMechanic()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null)
            return;

        var mechanic = EffectInfo.MiscValue;

        List<KeyValuePair<uint, ObjectGuid>> dispelList = new();

        foreach (var aura in UnitTarget.OwnedAurasList)
        {
            if (aura.GetApplicationOfTarget(UnitTarget.GUID) == null)
                continue;

            if (RandomHelper.randChance(aura.CalcDispelChance(UnitTarget, !UnitTarget.WorldObjectCombat.IsFriendlyTo(Caster))))
                if ((aura.SpellInfo.GetAllEffectsMechanicMask() & (1ul << mechanic)) != 0)
                    dispelList.Add(new KeyValuePair<uint, ObjectGuid>(aura.Id, aura.CasterGuid));
        }

        while (!dispelList.Empty())
        {
            UnitTarget.RemoveAura(dispelList[0].Key, dispelList[0].Value, AuraRemoveMode.EnemySpell);
            dispelList.RemoveAt(0);
        }
    }

    [SpellEffectHandler(SpellEffectName.Distract)]
    private void EffectDistract()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        // Check for possible target
        if (UnitTarget == null || UnitTarget.IsEngaged)
            return;

        // target must be OK to do this
        if (UnitTarget.HasUnitState(UnitState.Confused | UnitState.Stunned | UnitState.Fleeing))
            return;

        UnitTarget.MotionMaster.MoveDistract((uint)(Damage * Time.IN_MILLISECONDS), UnitTarget.Location.GetAbsoluteAngle(DestTarget));
    }

    [SpellEffectHandler(SpellEffectName.DualWield)]
    private void EffectDualWield()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        UnitTarget.SetCanDualWield(true);

        if (UnitTarget.IsTypeId(TypeId.Unit))
            UnitTarget.AsCreature.UpdateDamagePhysical(WeaponAttackType.OffAttack);
    }

    [SpellEffectHandler(SpellEffectName.Duel)]
    private void EffectDuel()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !Caster.IsTypeId(TypeId.Player) || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        var caster = Caster.AsPlayer;
        var target = UnitTarget.AsPlayer;

        // caster or target already have requested duel
        if (caster.Duel != null || target.Duel != null || target.Social == null || target.Social.HasIgnore(caster.GUID, caster.Session.AccountGUID))
            return;

        // Players can only fight a duel in zones with this Id
        var casterAreaEntry = _cliDb.AreaTableStorage.LookupByKey(caster.Location.Area);

        if (casterAreaEntry != null && !casterAreaEntry.HasFlag(AreaFlags.AllowDuels))
        {
            SendCastResult(SpellCastResult.NoDueling); // Dueling isn't allowed here

            return;
        }

        var targetAreaEntry = _cliDb.AreaTableStorage.LookupByKey(target.Location.Area);

        if (targetAreaEntry != null && !targetAreaEntry.HasFlag(AreaFlags.AllowDuels))
        {
            SendCastResult(SpellCastResult.NoDueling); // Dueling isn't allowed here

            return;
        }

        //CREATE DUEL FLAG OBJECT
        var map = caster.Location.Map;

        Position pos = new()
        {
            X = caster.Location.X + (UnitTarget.Location.X - caster.Location.X) / 2,
            Y = caster.Location.Y + (UnitTarget.Location.Y - caster.Location.Y) / 2,
            Z = caster.Location.Z,
            Orientation = caster.Location.Orientation
        };

        var rotation = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(pos.Orientation, 0.0f, 0.0f));

        var go = _gameObjectFactory.CreateGameObject((uint)EffectInfo.MiscValue, map, pos, rotation, 0, GameObjectState.Ready);

        if (go == null)
            return;

        _phasingHandler.InheritPhaseShift(go, caster);

        go.Faction = caster.Faction;
        go.SetLevel(caster.Level + 1);
        var duration = SpellInfo.CalcDuration(caster);
        go.SetRespawnTime(duration > 0 ? duration / Time.IN_MILLISECONDS : 0);
        go.SpellId = SpellInfo.Id;

        ExecuteLogEffectSummonObject(EffectInfo.Effect, go);

        caster.AddGameObject(go);
        map.AddToMap(go);
        //END

        // Send request
        DuelRequested packet = new()
        {
            ArbiterGUID = go.GUID,
            RequestedByGUID = caster.GUID,
            RequestedByWowAccount = caster.Session.AccountGUID
        };

        caster.SendPacket(packet);
        target.SendPacket(packet);

        // create duel-info
        var isMounted = SpellInfo.Id == 62875;
        caster.Duel = new DuelInfo(target, caster, isMounted);
        target.Duel = new DuelInfo(caster, caster, isMounted);

        caster.SetDuelArbiter(go.GUID);
        target.SetDuelArbiter(go.GUID);

        _scriptManager.ForEach<IPlayerOnDuelRequest>(p => p.OnDuelRequest(target, caster));
    }

    [SpellEffectHandler(SpellEffectName.Dummy)]
    private void EffectDummy()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null && GameObjTarget == null && ItemTarget == null && CorpseTarget == null)
            return;

        // pet auras
        if (Caster.TypeId == TypeId.Player)
        {
            var petSpell = _spellManager.GetPetAura(SpellInfo.Id, (byte)EffectInfo.EffectIndex);

            if (petSpell != null)
            {
                Caster.AsPlayer.AddPetAura(petSpell);

                return;
            }
        }

        // normal DB scripted effect
        Log.Logger.Debug("Spell ScriptStart spellid {0} in EffectDummy({1})", SpellInfo.Id, EffectInfo.EffectIndex);
        Caster.Location.Map.ScriptsStart(ScriptsType.Spell, (uint)((int)SpellInfo.Id | EffectInfo.EffectIndex << 24), Caster, UnitTarget);
    }

    [SpellEffectHandler(SpellEffectName.DurabilityDamage)]
    private void EffectDurabilityDamage()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        var slot = EffectInfo.MiscValue;

        // -1 means all player equipped items and -2 all items
        if (slot < 0)
        {
            UnitTarget.AsPlayer.DurabilityPointsLossAll(Damage, slot < -1);
            ExecuteLogEffectDurabilityDamage(EffectInfo.Effect, UnitTarget, -1, -1);

            return;
        }

        // invalid slot value
        if (slot >= InventorySlots.BagEnd)
            return;

        var item = UnitTarget.AsPlayer.GetItemByPos(InventorySlots.Bag0, (byte)slot);

        if (item != null)
        {
            UnitTarget.AsPlayer.DurabilityPointsLoss(item, Damage);
            ExecuteLogEffectDurabilityDamage(EffectInfo.Effect, UnitTarget, (int)item.Entry, slot);
        }
    }

    [SpellEffectHandler(SpellEffectName.DurabilityDamagePct)]
    private void EffectDurabilityDamagePct()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        var slot = EffectInfo.MiscValue;

        // FIXME: some spells effects have value -1/-2
        // Possibly its mean -1 all player equipped items and -2 all items
        if (slot < 0)
        {
            UnitTarget.AsPlayer.DurabilityLossAll(Damage / 100.0f, slot < -1);

            return;
        }

        // invalid slot value
        if (slot >= InventorySlots.BagEnd)
            return;

        if (Damage <= 0)
            return;

        var item = UnitTarget.AsPlayer.GetItemByPos(InventorySlots.Bag0, (byte)slot);

        if (item != null)
            UnitTarget.AsPlayer.DurabilityLoss(item, Damage / 100.0f);
    }

    [SpellEffectHandler(SpellEffectName.EnableBattlePets)]
    private void EffectEnableBattlePets()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget is not { IsPlayer: true })
            return;

        var player = UnitTarget.AsPlayer;
        player.SetPlayerFlag(PlayerFlags.PetBattlesUnlocked);
        player.Session.BattlePetMgr.UnlockSlot(BattlePetSlots.Slot0);
    }

    [SpellEffectHandler(SpellEffectName.EnchantHeldItem)]
    private void EffectEnchantHeldItem()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        // this is only item spell effect applied to main-hand weapon of target player (players in area)
        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        var itemOwner = UnitTarget.AsPlayer;
        var item = itemOwner.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);

        if (item is not { IsEquipped: true })
            return;

        // must be equipped

        if (EffectInfo.MiscValue != 0)
        {
            var enchantID = (uint)EffectInfo.MiscValue;
            var duration = SpellInfo.Duration; //Try duration index first ..

            if (duration == 0)
                duration = (int)Damage; //+1;            //Base points after ..

            if (duration == 0)
                duration = 10 * Time.IN_MILLISECONDS; //10 seconds for enchants which don't have listed duration

            if (SpellInfo.Id == 14792) // Venomhide Poison
                duration = 5 * Time.MINUTE * Time.IN_MILLISECONDS;

            if (!_cliDb.SpellItemEnchantmentStorage.ContainsKey(enchantID))
                return;

            // Always go to temp enchantment slot
            var slot = EnchantmentSlot.Temp;

            // Enchantment will not be applied if a different one already exists
            if (item.GetEnchantmentId(slot) != 0 && item.GetEnchantmentId(slot) != enchantID)
                return;

            // Apply the temporary enchantment
            item.SetEnchantment(slot, enchantID, (uint)duration, 0, Caster.GUID);
            itemOwner.ApplyEnchantment(item, slot, true);
        }
    }

    [SpellEffectHandler(SpellEffectName.EnchantItem)]
    private void EffectEnchantItemPerm()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (ItemTarget == null)
            return;

        var player = Caster.AsPlayer;

        if (player == null)
            return;

        // Handle vellums
        if (ItemTarget.IsVellum)
        {
            // destroy one vellum from stack
            uint count = 1;
            player.DestroyItemCount(ItemTarget, ref count, true);
            UnitTarget = player;
            // and add a scroll
            Damage = 1;
            DoCreateItem(EffectInfo.ItemType, SpellInfo.HasAttribute(SpellAttr0.IsTradeskill) ? ItemContext.TradeSkill : ItemContext.None);
            ItemTarget = null;
            Targets.ItemTarget = null;
        }
        else
        {
            // do not increase skill if vellum used
            if (!(CastItem != null && CastItem.Template.HasFlag(ItemFlags.NoReagentCost)))
                player.UpdateCraftSkill(SpellInfo);

            var enchantID = (uint)EffectInfo.MiscValue;

            if (enchantID == 0)
                return;

            if (!_cliDb.SpellItemEnchantmentStorage.ContainsKey(enchantID))
                return;

            // item can be in trade slot and have owner diff. from caster
            var itemOwner = ItemTarget.OwnerUnit;

            if (itemOwner == null)
                return;

            if (itemOwner != player && player.Session.HasPermission(RBACPermissions.LogGmTrade))
                Log.Logger.ForContext<GMCommands>()
                   .Information("GM {0} (Account: {1}) enchanting(perm): {2} (Entry: {3}) for player: {4} (Account: {5})",
                                player.GetName(),
                                player.Session.AccountId,
                                ItemTarget.Template.GetName(),
                                ItemTarget.Entry,
                                itemOwner.GetName(),
                                itemOwner.Session.AccountId);

            // remove old enchanting before applying new if equipped
            itemOwner.ApplyEnchantment(ItemTarget, EnchantmentSlot.Perm, false);

            ItemTarget.SetEnchantment(EnchantmentSlot.Perm, enchantID, 0, 0, Caster.GUID);

            // add new enchanting if equipped
            itemOwner.ApplyEnchantment(ItemTarget, EnchantmentSlot.Perm, true);

            itemOwner.RemoveTradeableItem(ItemTarget);
            ItemTarget.ClearSoulboundTradeable(itemOwner);
        }
    }

    [SpellEffectHandler(SpellEffectName.EnchantItemPrismatic)]
    private void EffectEnchantItemPrismatic()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (ItemTarget == null)
            return;

        var player = Caster.AsPlayer;

        if (player == null)
            return;

        var enchantId = (uint)EffectInfo.MiscValue;

        if (enchantId == 0)
            return;

        if (!_cliDb.SpellItemEnchantmentStorage.TryGetValue(enchantId, out var enchant))
            return;

        // support only enchantings with add socket in this slot
        {
            var addSocket = false;

            for (byte i = 0; i < ItemConst.MaxItemEnchantmentEffects; ++i)
                if (enchant.Effect[i] == ItemEnchantmentType.PrismaticSocket)
                {
                    addSocket = true;

                    break;
                }

            if (!addSocket)
            {
                Log.Logger.Error("Spell.EffectEnchantItemPrismatic: attempt apply enchant spell {0} with SPELL_EFFECT_ENCHANT_ITEM_PRISMATIC ({1}) but without ITEM_ENCHANTMENT_TYPE_PRISMATIC_SOCKET ({2}), not suppoted yet.",
                                 SpellInfo.Id,
                                 SpellEffectName.EnchantItemPrismatic,
                                 ItemEnchantmentType.PrismaticSocket);

                return;
            }
        }

        // item can be in trade slot and have owner diff. from caster
        var itemOwner = ItemTarget.OwnerUnit;

        if (itemOwner == null)
            return;

        if (itemOwner != player && player.Session.HasPermission(RBACPermissions.LogGmTrade))
            Log.Logger.ForContext<GMCommands>()
               .Information("GM {0} (Account: {1}) enchanting(perm): {2} (Entry: {3}) for player: {4} (Account: {5})",
                            player.GetName(),
                            player.Session.AccountId,
                            ItemTarget.Template.GetName(),
                            ItemTarget.Entry,
                            itemOwner.GetName(),
                            itemOwner.Session.AccountId);

        // remove old enchanting before applying new if equipped
        itemOwner.ApplyEnchantment(ItemTarget, EnchantmentSlot.Prismatic, false);

        ItemTarget.SetEnchantment(EnchantmentSlot.Prismatic, enchantId, 0, 0, Caster.GUID);

        // add new enchanting if equipped
        itemOwner.ApplyEnchantment(ItemTarget, EnchantmentSlot.Prismatic, true);

        itemOwner.RemoveTradeableItem(ItemTarget);
        ItemTarget.ClearSoulboundTradeable(itemOwner);
    }

    [SpellEffectHandler(SpellEffectName.EnchantItemTemporary)]
    private void EffectEnchantItemTmp()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (ItemTarget == null)
            return;

        var player = Caster.AsPlayer;

        if (player == null)
            return;

        var enchantID = (uint)EffectInfo.MiscValue;

        if (enchantID == 0)
        {
            Log.Logger.Error("Spell {0} Effect {1} (SPELL_EFFECT_ENCHANT_ITEM_TEMPORARY) have 0 as enchanting id", SpellInfo.Id, EffectInfo.EffectIndex);

            return;
        }

        if (!_cliDb.SpellItemEnchantmentStorage.TryGetValue(enchantID, out var pEnchant))
        {
            Log.Logger.Error("Spell {0} Effect {1} (SPELL_EFFECT_ENCHANT_ITEM_TEMPORARY) have not existed enchanting id {2}", SpellInfo.Id, EffectInfo.EffectIndex, enchantID);

            return;
        }

        // select enchantment duration
        var duration = (uint)pEnchant.Duration;

        // item can be in trade slot and have owner diff. from caster
        var itemOwner = ItemTarget.OwnerUnit;

        if (itemOwner == null)
            return;

        if (itemOwner != player && player.Session.HasPermission(RBACPermissions.LogGmTrade))
            Log.Logger.ForContext<GMCommands>()
               .Information("GM {0} (Account: {1}) enchanting(temp): {2} (Entry: {3}) for player: {4} (Account: {5})",
                            player.GetName(),
                            player.Session.AccountId,
                            ItemTarget.Template.GetName(),
                            ItemTarget.Entry,
                            itemOwner.GetName(),
                            itemOwner.Session.AccountId);

        // remove old enchanting before applying new if equipped
        itemOwner.ApplyEnchantment(ItemTarget, EnchantmentSlot.Temp, false);

        ItemTarget.SetEnchantment(EnchantmentSlot.Temp, enchantID, duration * 1000, 0, Caster.GUID);

        // add new enchanting if equipped
        itemOwner.ApplyEnchantment(ItemTarget, EnchantmentSlot.Temp, true);
    }

    [SpellEffectHandler(SpellEffectName.Energize)]
    private void EffectEnergize()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster == null || UnitTarget == null)
            return;

        if (!UnitTarget.IsAlive)
            return;

        if (EffectInfo.MiscValue is < 0 or >= (byte)PowerType.Max)
            return;

        var power = (PowerType)EffectInfo.MiscValue;

        if (UnitTarget.GetMaxPower(power) == 0)
            return;

        var dmg = Damage;
        ForEachSpellScript<ISpellEnergizedBySpell>(a => a.EnergizeBySpell(UnitTarget, SpellInfo, ref dmg, power));
        Damage = dmg;

        unitCaster.EnergizeBySpell(UnitTarget, SpellInfo, Damage, power);
    }

    [SpellEffectHandler(SpellEffectName.EnergizePct)]
    private void EffectEnergizePct()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster == null || UnitTarget == null)
            return;

        if (!UnitTarget.IsAlive)
            return;

        if (EffectInfo.MiscValue is < 0 or >= (byte)PowerType.Max)
            return;

        var power = (PowerType)EffectInfo.MiscValue;
        var maxPower = (uint)UnitTarget.GetMaxPower(power);

        if (maxPower == 0)
            return;

        var gain = (int)MathFunctions.CalculatePct(maxPower, Damage);
        unitCaster.EnergizeBySpell(UnitTarget, SpellInfo, gain, power);
    }

    [SpellEffectHandler(SpellEffectName.EnvironmentalDamage)]
    private void EffectEnvironmentalDmg()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget is not { IsAlive: true })
            return;

        // CalcAbsorbResist already in Player::EnvironmentalDamage
        if (UnitTarget.IsTypeId(TypeId.Player))
            UnitTarget.AsPlayer.EnvironmentalDamage(EnviromentalDamage.Fire, Damage);
        else
        {
            var unitCaster = UnitCasterForEffectHandlers;
            DamageInfo damageInfo = new(unitCaster, UnitTarget, Damage, SpellInfo, SpellInfo.SchoolMask, DamageEffectType.SpellDirect, WeaponAttackType.BaseAttack);
            _combatHelpers.CalcAbsorbResist(damageInfo);

            SpellNonMeleeDamage log = new(unitCaster, UnitTarget, SpellInfo, SpellVisual, SpellInfo.SchoolMask, CastId)
            {
                Damage = damageInfo.Damage,
                OriginalDamage = Damage,
                Absorb = damageInfo.Absorb,
                Resist = damageInfo.Resist
            };

            unitCaster?.SendSpellNonMeleeDamageLog(log);
        }
    }

    [SpellEffectHandler(SpellEffectName.FeedPet)]
    private void EffectFeedPet()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var player = Caster.AsPlayer;

        if (player == null)
            return;

        var foodItem = ItemTarget;

        if (foodItem == null)
            return;

        var pet = player.CurrentPet;

        if (pet is not { IsAlive: true })
            return;

        ExecuteLogEffectDestroyItem(EffectInfo.Effect, foodItem.Entry);

        int pct;
        var levelDiff = (int)pet.Level - (int)foodItem.Template.BaseItemLevel;

        if (levelDiff >= 30)
            return;

        if (levelDiff >= 20)
            pct = (int)12.5; // we can't pass double so keeping the cast here for future references
        else if (levelDiff >= 10)
            pct = 25;
        else
            pct = 50;

        uint count = 1;
        player.DestroyItemCount(foodItem, ref count, true);
        // @todo fix crash when a spell has two effects, both pointed at the same item target

        CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
        args.SetTriggeringSpell(this);
        args.AddSpellMod(SpellValueMod.BasePoint0, pct);
        Caster.SpellFactory.CastSpell(pet, EffectInfo.TriggerSpell, args);
    }

    [SpellEffectHandler(SpellEffectName.ForceCast)]
    [SpellEffectHandler(SpellEffectName.ForceCastWithValue)]
    [SpellEffectHandler(SpellEffectName.ForceCast2)]
    private void EffectForceCast()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null)
            return;

        var triggeredSpellID = EffectInfo.TriggerSpell;

        if (triggeredSpellID == 0)
        {
            Log.Logger.Warning($"Spell::EffectForceCast: Spell {SpellInfo.Id} [EffectIndex: {EffectInfo.EffectIndex}] does not have triggered spell.");

            return;
        }

        // normal case
        var spellInfo = _spellManager.GetSpellInfo(triggeredSpellID, CastDifficulty);

        if (spellInfo == null)
        {
            Log.Logger.Error("Spell.EffectForceCast of spell {0}: triggering unknown spell id {1}", SpellInfo.Id, triggeredSpellID);

            return;
        }

        if (EffectInfo.Effect == SpellEffectName.ForceCast && Damage != 0)
            switch (SpellInfo.Id)
            {
                case 52588: // Skeletal Gryphon Escape
                case 48598: // Ride Flamebringer Cue
                    UnitTarget.RemoveAura((uint)Damage);

                    break;

                case 52463: // Hide In Mine Car
                case 52349: // Overtake
                {
                    CastSpellExtraArgs args1 = new(TriggerCastFlags.FullMask);
                    args1.SetOriginalCaster(_originalCasterGuid);
                    args1.SetTriggeringSpell(this);
                    args1.AddSpellMod(SpellValueMod.BasePoint0, Damage);
                    UnitTarget.SpellFactory.CastSpell(UnitTarget, spellInfo.Id, args1);

                    return;
                }
            }

        switch (spellInfo.Id)
        {
            case 72298: // Malleable Goo Summon
                UnitTarget.SpellFactory.CastSpell(UnitTarget,
                                                  spellInfo.Id,
                                                  new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                                                      .SetOriginalCaster(_originalCasterGuid)
                                                      .SetTriggeringSpell(this));

                return;
        }

        CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
        args.SetTriggeringSpell(this);

        // set basepoints for trigger with value effect
        if (EffectInfo.Effect == SpellEffectName.ForceCastWithValue)
            foreach (var eff in spellInfo.Effects)
                args.AddSpellMod(SpellValueMod.BasePoint0 + eff.EffectIndex, Damage);

        UnitTarget.SpellFactory.CastSpell(Caster, spellInfo.Id, args);
    }

    [SpellEffectHandler(SpellEffectName.ForceDeselect)]
    private void EffectForceDeselect()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster == null)
            return;

        var dist = Caster.Visibility.VisibilityRange;

        // clear focus
        PacketSenderOwning<BreakTarget> breakTarget = new()
        {
            Data =
            {
                UnitGUID = Caster.GUID
            }
        };

        breakTarget.Data.Write();

        var notifierBreak = new MessageDistDelivererToHostile<PacketSenderOwning<BreakTarget>>(unitCaster, breakTarget, dist, GridType.World);
        _cellCalculator.VisitGrid(Caster, notifierBreak, dist);

        // and selection
        PacketSenderOwning<ClearTarget> clearTarget = new()
        {
            Data =
            {
                Guid = Caster.GUID
            }
        };

        clearTarget.Data.Write();
        var notifierClear = new MessageDistDelivererToHostile<PacketSenderOwning<ClearTarget>>(unitCaster, clearTarget, dist, GridType.World);
        _cellCalculator.VisitGrid(Caster, notifierClear, dist);

        // we should also force pets to remove us from current target
        List<Unit> attackerSet = new();

        foreach (var unit in unitCaster.Attackers)
            if (unit.TypeId == TypeId.Unit && !unit.CanHaveThreatList)
                attackerSet.Add(unit);

        foreach (var unit in attackerSet)
            unit.AttackStop();
    }

    [SpellEffectHandler(SpellEffectName.GameObjectDamage)]
    private void EffectGameObjectDamage()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (GameObjTarget == null)
            return;

        var casterFaction = Caster.WorldObjectCombat.GetFactionTemplateEntry();
        var targetFaction = _cliDb.FactionTemplateStorage.LookupByKey(GameObjTarget.Faction);

        // Do not allow to damage GO's of friendly factions (ie: Wintergrasp Walls/Ulduar Storm Beacons)
        if (targetFaction == null || (casterFaction != null && !casterFaction.IsFriendlyTo(targetFaction)))
            GameObjTarget.ModifyHealth(-Damage, Caster, SpellInfo.Id);
    }

    [SpellEffectHandler(SpellEffectName.GameobjectRepair)]
    private void EffectGameObjectRepair()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        GameObjTarget?.ModifyHealth(Damage, Caster);
    }

    [SpellEffectHandler(SpellEffectName.GameobjectSetDestructionState)]
    private void EffectGameObjectSetDestructionState()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        GameObjTarget?.SetDestructibleState((GameObjectDestructibleState)EffectInfo.MiscValue, Caster, true);
    }

    [SpellEffectHandler(SpellEffectName.GiveArtifactPower)]
    private void EffectGiveArtifactPower()
    {
        if (_effectHandleMode != SpellEffectHandleMode.LaunchTarget)
            return;

        var playerCaster = Caster.AsPlayer;

        var artifactAura = playerCaster?.GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);

        if (artifactAura != null)
            playerCaster.GetItemByGuid(artifactAura.CastItemGuid)?.GiveArtifactXp((ulong)Damage, CastItem, (ArtifactCategory)EffectInfo.MiscValue);
    }

    [SpellEffectHandler(SpellEffectName.GiveArtifactPowerNoBonus)]
    private void EffectGiveArtifactPowerNoBonus()
    {
        if (_effectHandleMode != SpellEffectHandleMode.LaunchTarget)
            return;

        if (UnitTarget == null || !Caster.IsTypeId(TypeId.Player))
            return;

        var artifactAura = UnitTarget.GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);

        if (artifactAura != null)
            UnitTarget.AsPlayer?.GetItemByGuid(artifactAura.CastItemGuid)?.GiveArtifactXp((ulong)Damage, CastItem, 0);
    }

    [SpellEffectHandler(SpellEffectName.GiveCurrency)]
    private void EffectGiveCurrency()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        if (!_cliDb.CurrencyTypesStorage.ContainsKey(EffectInfo.MiscValue))
            return;

        UnitTarget.AsPlayer.ModifyCurrency((uint)EffectInfo.MiscValue, (int)Damage, CurrencyGainSource.Spell, CurrencyDestroyReason.Spell);
    }

    [SpellEffectHandler(SpellEffectName.GiveExperience)]
    private void EffectGiveExperience()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var playerTarget = UnitTarget?.AsPlayer;

        if (playerTarget == null)
            return;

        var xp = Quest.Quest.XPValue(playerTarget, (uint)EffectInfo.MiscValue, (uint)EffectInfo.MiscValueB);
        playerTarget.GiveXP(xp, null);
    }

    [SpellEffectHandler(SpellEffectName.GiveHonor)]
    private void EffectGiveHonor()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget is not { TypeId: TypeId.Player })
            return;

        PvPCredit packet = new()
        {
            Honor = (int)Damage,
            OriginalHonor = (int)Damage
        };

        var playerTarget = UnitTarget.AsPlayer;
        playerTarget.AddHonorXp((uint)Damage);
        playerTarget.SendPacket(packet);
    }

    [SpellEffectHandler(SpellEffectName.GiveRestedEcperienceBonus)]
    private void EffectGiveRestedExperience()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var playerTarget = UnitTarget?.AsPlayer;

        // effect value is number of resting hours
        playerTarget?.RestMgr.AddRestBonus(RestTypes.XP, Damage * Time.HOUR * playerTarget.RestMgr.CalcExtraPerSec(RestTypes.XP, 0.125f));
    }

    [SpellEffectHandler(SpellEffectName.GrantBattlepetExperience)]
    private void EffectGrantBattlePetExperience()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var playerCaster = Caster.AsPlayer;

        if (playerCaster == null)
            return;

        if (UnitTarget is not { IsCreature: true })
            return;

        playerCaster.Session.BattlePetMgr.GrantBattlePetExperience(UnitTarget.BattlePetCompanionGUID, (ushort)Damage, BattlePetXpSource.SpellEffect);
    }

    [SpellEffectHandler(SpellEffectName.GrantBattlepetLevel)]
    private void EffectGrantBattlePetLevel()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var playerCaster = Caster.AsPlayer;

        if (playerCaster == null)
            return;

        if (UnitTarget is not { IsCreature: true })
            return;

        playerCaster.Session.BattlePetMgr.GrantBattlePetLevel(UnitTarget.BattlePetCompanionGUID, (ushort)Damage);
    }

    [SpellEffectHandler(SpellEffectName.Heal)]
    private void EffectHeal()
    {
        if (_effectHandleMode != SpellEffectHandleMode.LaunchTarget)
            return;

        if (UnitTarget is not { IsAlive: true } || Damage < 0)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        // Skip if m_originalCaster not available
        if (unitCaster == null)
            return;

        var addhealth = Damage;

        // Vessel of the Naaru (Vial of the Sunwell trinket)
        //@todo: move this to scripts
        if (SpellInfo.Id == 45064)
        {
            // Amount of heal - depends from stacked Holy Energy
            double damageAmount = 0;
            var aurEff = unitCaster.GetAuraEffect(45062, 0);

            if (aurEff != null)
            {
                damageAmount += aurEff.Amount;
                unitCaster.RemoveAura(45062);
            }

            addhealth += damageAmount;
        }
        // Death Pact - return pct of max health to caster
        else if (SpellInfo.SpellFamilyName == SpellFamilyNames.Deathknight && SpellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00080000u))
            addhealth = unitCaster.SpellHealingBonusDone(UnitTarget, SpellInfo, unitCaster.CountPctFromMaxHealth(Damage), DamageEffectType.Heal, EffectInfo, 1, this);
        else
        {
            var bonus = unitCaster.SpellHealingBonusDone(UnitTarget, SpellInfo, addhealth, DamageEffectType.Heal, EffectInfo, 1, this);
            addhealth = bonus + bonus * Variance;
        }

        addhealth = (int)UnitTarget.SpellHealingBonusTaken(unitCaster, SpellInfo, addhealth, DamageEffectType.Heal);

        // Remove Grievious bite if fully healed
        if (UnitTarget.HasAura(48920) && UnitTarget.Health + addhealth >= UnitTarget.MaxHealth)
            UnitTarget.RemoveAura(48920);

        HealingInEffects += addhealth;
    }

    [SpellEffectHandler(SpellEffectName.HealBattlepetPct)]
    private void EffectHealBattlePetPct()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        var battlePetMgr = UnitTarget.AsPlayer.Session.BattlePetMgr;

        battlePetMgr?.HealBattlePetsPct((byte)Damage);
    }

    [SpellEffectHandler(SpellEffectName.HealMaxHealth)]
    private void EffectHealMaxHealth()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster == null)
            return;

        if (UnitTarget is not { IsAlive: true })
            return;

        int addhealth;

        // damage == 0 - heal for caster max health
        if (Damage == 0)
            addhealth = (int)unitCaster.MaxHealth;
        else
            addhealth = (int)(UnitTarget.MaxHealth - UnitTarget.Health);

        HealingInEffects += addhealth;
    }

    [SpellEffectHandler(SpellEffectName.HealMechanical)]
    private void EffectHealMechanical()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget is not { IsAlive: true } || Damage < 0)
            return;

        var unitCaster = UnitCasterForEffectHandlers;
        var heal = Damage;

        if (unitCaster != null)
            heal = unitCaster.SpellHealingBonusDone(UnitTarget, SpellInfo, heal, DamageEffectType.Heal, EffectInfo, 1, this);

        heal += heal * Variance;

        if (unitCaster != null)
            heal = UnitTarget.SpellHealingBonusTaken(unitCaster, SpellInfo, heal, DamageEffectType.Heal);

        HealingInEffects += heal;
    }

    [SpellEffectHandler(SpellEffectName.HealPct)]
    private void EffectHealPct()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget is not { IsAlive: true } || Damage < 0)
            return;

        var heal = (double)UnitTarget.CountPctFromMaxHealth(Damage);
        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster != null)
        {
            heal = unitCaster.SpellHealingBonusDone(UnitTarget, SpellInfo, heal, DamageEffectType.Heal, EffectInfo, 1, this);
            heal = UnitTarget.SpellHealingBonusTaken(unitCaster, SpellInfo, heal, DamageEffectType.Heal);
        }

        HealingInEffects += heal;
    }

    [SpellEffectHandler(SpellEffectName.HealthLeech)]
    private void EffectHealthLeech()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget is not { IsAlive: true } || Damage < 0)
            return;

        var unitCaster = UnitCasterForEffectHandlers;
        uint bonus = 0;

        unitCaster?.SpellDamageBonusDone(UnitTarget, SpellInfo, Damage, DamageEffectType.SpellDirect, EffectInfo, 1, this);

        Damage = bonus + bonus * Variance;

        if (unitCaster != null)
            Damage = UnitTarget.SpellDamageBonusTaken(unitCaster, SpellInfo, Damage, DamageEffectType.SpellDirect);

        Log.Logger.Debug("HealthLeech :{0}", Damage);

        var healMultiplier = EffectInfo.CalcValueMultiplier(unitCaster, this);

        DamageInEffects += Damage;

        DamageInfo damageInfo = new(unitCaster, UnitTarget, Damage, SpellInfo, SpellInfo.SchoolMask, DamageEffectType.Direct, WeaponAttackType.BaseAttack);
        _combatHelpers.CalcAbsorbResist(damageInfo);
        var absorb = damageInfo.Absorb;
        Damage -= absorb;

        // get max possible damage, don't count overkill for heal
        var healthGain = -UnitTarget.GetHealthGain(-Damage) * healMultiplier;

        if (unitCaster is not { IsAlive: true })
            return;

        healthGain = unitCaster.SpellHealingBonusDone(unitCaster, SpellInfo, healthGain, DamageEffectType.Heal, EffectInfo, 1, this);
        healthGain = unitCaster.SpellHealingBonusTaken(unitCaster, SpellInfo, healthGain, DamageEffectType.Heal);

        HealInfo healInfo = new(unitCaster, unitCaster, healthGain, SpellInfo, SpellSchoolMask);
        unitCaster.HealBySpell(healInfo);
    }

    [SpellEffectHandler(SpellEffectName.IncreseCurrencyCap)]
    private void EffectIncreaseCurrencyCap()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (Damage <= 0)
            return;

        UnitTarget.AsPlayer?.IncreaseCurrencyCap((uint)EffectInfo.MiscValue, (uint)Damage);
    }

    [SpellEffectHandler(SpellEffectName.Inebriate)]
    private void EffectInebriate()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        var player = UnitTarget.AsPlayer;
        var currentDrunk = player.DrunkValue;
        var drunkMod = Damage;

        if (currentDrunk + drunkMod > 100)
        {
            currentDrunk = 100;

            if (RandomHelper.randChance() < 25.0f)
                player.SpellFactory.CastSpell(player, 67468, new CastSpellExtraArgs().SetTriggeringSpell(this)); // Drunken Vomit
        }
        else
            currentDrunk += (byte)drunkMod;

        player.SetDrunkValue(currentDrunk, CastItem?.Entry ?? 0);
    }

    [SpellEffectHandler(SpellEffectName.Instakill)]
    private void EffectInstaKill()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget is not { IsAlive: true })
            return;

        if (UnitTarget.IsTypeId(TypeId.Player))
            if (UnitTarget.AsPlayer.GetCommandStatus(PlayerCommandStates.God))
                return;

        if (Caster == UnitTarget) // prevent interrupt message
            Finish();

        SpellInstakillLog data = new()
        {
            Target = UnitTarget.GUID,
            Caster = Caster.GUID,
            SpellID = SpellInfo.Id
        };

        Caster.SendMessageToSet(data, true);

        _combatHelpers.Kill(UnitCasterForEffectHandlers, UnitTarget, false);
    }

    [SpellEffectHandler(SpellEffectName.InterruptCast)]
    private void EffectInterruptCast()
    {
        if (_effectHandleMode != SpellEffectHandleMode.LaunchTarget)
            return;

        if (UnitTarget is not { IsAlive: true })
            return;

        // @todo not all spells that used this effect apply cooldown at school spells
        // also exist case: apply cooldown to interrupted cast only and to all spells
        // there is no CURRENT_AUTOREPEAT_SPELL spells that can be interrupted
        for (var i = CurrentSpellTypes.Generic; i < CurrentSpellTypes.AutoRepeat; ++i)
        {
            var spell = UnitTarget.GetCurrentSpell(i);

            if (spell == null)
                continue;

            var curSpellInfo = spell.SpellInfo;

            // check if we can interrupt spell
            if ((spell.State != SpellState.Casting && (spell.State != SpellState.Preparing || !(spell.CastTime > 0.0f))) || !curSpellInfo.CanBeInterrupted(Caster, UnitTarget))
                continue;

            var duration = SpellInfo.Duration;
            duration = ModSpellDuration(SpellInfo, UnitTarget, duration, false, EffectInfo.EffectIndex);
            UnitTarget.SpellHistory.LockSpellSchool(curSpellInfo.SchoolMask, TimeSpan.FromMilliseconds(duration));
            HitMask |= ProcFlagsHit.Interrupt;
            SendSpellInterruptLog(UnitTarget, curSpellInfo.Id);
            var interuptedSpell = UnitTarget.InterruptSpell(i, false, true, spell);

            if (interuptedSpell != null)
                ForEachSpellScript<ISpellOnSucessfulInterrupt>(s => s.SucessfullyInterrupted(interuptedSpell));
        }
    }

    [SpellEffectHandler(SpellEffectName.Jump)]
    private void EffectJump()
    {
        if (_effectHandleMode != SpellEffectHandleMode.LaunchTarget)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster == null)
            return;

        if (unitCaster.IsInFlight)
            return;

        if (UnitTarget == null)
            return;

        CalculateJumpSpeeds(EffectInfo, unitCaster.Location.GetExactDist2d(UnitTarget.Location), out var speedXY, out var speedZ);

        JumpArrivalCastArgs arrivalCast = new()
        {
            SpellId = EffectInfo.TriggerSpell,
            Target = UnitTarget.GUID
        };

        unitCaster.MotionMaster.MoveJump(UnitTarget.Location, speedXY, speedZ, EventId.Jump, false, arrivalCast);
    }

    [SpellEffectHandler(SpellEffectName.JumpCharge)]
    private void EffectJumpCharge()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Launch)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster == null)
            return;

        if (unitCaster.IsInFlight)
            return;

        var jumpParams = _gameObjectManager.GetJumpChargeParams(EffectInfo.MiscValue);

        if (jumpParams == null)
            return;

        var speed = jumpParams.Speed;

        if (jumpParams.TreatSpeedAsMoveTimeSeconds)
            speed = unitCaster.Location.GetExactDist(DestTarget) / jumpParams.Speed;

        JumpArrivalCastArgs arrivalCast = null;

        if (EffectInfo.TriggerSpell != 0)
            arrivalCast = new JumpArrivalCastArgs
            {
                SpellId = EffectInfo.TriggerSpell
            };

        SpellEffectExtraData effectExtra = null;

        if (jumpParams.SpellVisualId.HasValue || jumpParams.ProgressCurveId.HasValue || jumpParams.ParabolicCurveId.HasValue)
        {
            effectExtra = new SpellEffectExtraData();

            if (jumpParams.SpellVisualId.HasValue)
                effectExtra.SpellVisualId = jumpParams.SpellVisualId.Value;

            if (jumpParams.ProgressCurveId.HasValue)
                effectExtra.ProgressCurveId = jumpParams.ProgressCurveId.Value;

            if (jumpParams.ParabolicCurveId.HasValue)
                effectExtra.ParabolicCurveId = jumpParams.ParabolicCurveId.Value;
        }

        unitCaster.MotionMaster.MoveJumpWithGravity(DestTarget, speed, jumpParams.JumpGravity, EventId.Jump, false, arrivalCast, effectExtra);
    }

    [SpellEffectHandler(SpellEffectName.JumpDest)]
    private void EffectJumpDest()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Launch)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster == null)
            return;

        if (unitCaster.IsInFlight)
            return;

        if (!Targets.HasDst)
            return;

        CalculateJumpSpeeds(EffectInfo, unitCaster.Location.GetExactDist2d(DestTarget), out var speedXY, out var speedZ);

        JumpArrivalCastArgs arrivalCast = new()
        {
            SpellId = EffectInfo.TriggerSpell
        };

        unitCaster.MotionMaster.MoveJump(DestTarget, speedXY, speedZ, EventId.Jump, !Targets.ObjectTargetGUID.IsEmpty, arrivalCast);
    }

    [SpellEffectHandler(SpellEffectName.KillCredit2)]
    private void EffectKillCredit()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        var creatureEntry = EffectInfo.MiscValue;

        if (creatureEntry != 0)
            UnitTarget.AsPlayer.RewardPlayerAndGroupAtEvent((uint)creatureEntry, UnitTarget);
    }

    [SpellEffectHandler(SpellEffectName.KillCredit)]
    private void EffectKillCreditPersonal()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        UnitTarget.AsPlayer.KilledMonsterCredit((uint)EffectInfo.MiscValue);
    }

    [SpellEffectHandler(SpellEffectName.KnockBack)]
    [SpellEffectHandler(SpellEffectName.KnockBackDest)]
    private void EffectKnockBack()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null)
            return;

        if (Caster.AffectingPlayer != null)
        {
            var creatureTarget = UnitTarget.AsCreature;

            if (creatureTarget != null)
                if (creatureTarget.IsWorldBoss || creatureTarget.IsDungeonBoss)
                    return;
        }

        // Spells with SPELL_EFFECT_KNOCK_BACK (like Thunderstorm) can't knockback target if target has ROOT/STUN
        if (UnitTarget.HasUnitState(UnitState.Root | UnitState.Stunned))
            return;

        // Instantly interrupt non melee spells being casted
        if (UnitTarget.IsNonMeleeSpellCast(true))
            UnitTarget.InterruptNonMeleeSpells(true);

        var ratio = 0.1f;
        var speedxy = EffectInfo.MiscValue * ratio;
        var speedz = Damage * ratio;

        if (speedxy < 0.01f && speedz < 0.01f)
            return;

        Position origin;

        if (EffectInfo.Effect == SpellEffectName.KnockBackDest)
        {
            if (Targets.HasDst)
                origin = DestTarget.Copy();
            else
                return;
        }
        else //if (effectInfo.Effect == SPELL_EFFECT_KNOCK_BACK)
            origin = new Position(Caster.Location);

        UnitTarget.KnockbackFrom(origin, speedxy, (float)speedz);

        _combatHelpers.ProcSkillsAndAuras(UnitCasterForEffectHandlers, UnitTarget, new ProcFlagsInit(), new ProcFlagsInit(ProcFlags.None, ProcFlags2.Knockback), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.Hit, ProcFlagsHit.None, null, null, null);
    }

    [SpellEffectHandler(SpellEffectName.LaunchQuestChoice)]
    private void EffectLaunchQuestChoice()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget is not { IsPlayer: true })
            return;

        UnitTarget.AsPlayer.SendPlayerChoice(Caster.GUID, EffectInfo.MiscValue);
    }

    [SpellEffectHandler(SpellEffectName.Leap)]
    private void EffectLeap()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || UnitTarget.IsInFlight)
            return;

        if (!Targets.HasDst)
            return;

        UnitTarget.NearTeleportTo(DestTarget.X, DestTarget.Y, DestTarget.Z, DestTarget.Orientation, UnitTarget == Caster);
    }

    [SpellEffectHandler(SpellEffectName.LeapBack)]
    private void EffectLeapBack()
    {
        if (_effectHandleMode != SpellEffectHandleMode.LaunchTarget)
            return;

        if (UnitTarget == null)
            return;

        var speedxy = EffectInfo.MiscValue / 10.0f;
        var speedz = Damage / 10.0f;
        // Disengage
        UnitTarget.JumpTo(speedxy, (float)speedz, EffectInfo.PositionFacing);

        // changes fall time
        if (Caster.TypeId == TypeId.Player)
            Caster.AsPlayer.SetFallInformation(0, Caster.Location.Z);
    }

    [SpellEffectHandler(SpellEffectName.LearnAzeriteEssencePower)]
    private void EffectLearnAzeriteEssencePower()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var playerTarget = UnitTarget?.AsPlayer;

        if (playerTarget == null)
            return;

        var heartOfAzeroth = playerTarget.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);

        var azeriteItem = heartOfAzeroth?.AsAzeriteItem;

        if (azeriteItem == null)
            return;

        // remove old rank and apply new one
        if (azeriteItem.IsEquipped)
        {
            var selectedEssences = azeriteItem.GetSelectedAzeriteEssences();

            if (selectedEssences != null)
                for (var slot = 0; slot < SharedConst.MaxAzeriteEssenceSlot; ++slot)
                    if (selectedEssences.AzeriteEssenceID[slot] == EffectInfo.MiscValue)
                    {
                        var major = (AzeriteItemMilestoneType)playerTarget.DB2Manager.GetAzeriteItemMilestonePower(slot).Type == AzeriteItemMilestoneType.MajorEssence;
                        playerTarget.ApplyAzeriteEssence(azeriteItem, (uint)EffectInfo.MiscValue, SharedConst.MaxAzeriteEssenceRank, major, false);
                        playerTarget.ApplyAzeriteEssence(azeriteItem, (uint)EffectInfo.MiscValue, (uint)EffectInfo.MiscValueB, major, false);

                        break;
                    }
        }

        azeriteItem.SetEssenceRank((uint)EffectInfo.MiscValue, (uint)EffectInfo.MiscValueB);
        azeriteItem.SetState(ItemUpdateState.Changed, playerTarget);
    }

    [SpellEffectHandler(SpellEffectName.LearnGarrisonBuilding)]
    private void EffectLearnGarrisonBuilding()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        var garrison = UnitTarget.AsPlayer.Garrison;

        garrison?.LearnBlueprint((uint)EffectInfo.MiscValue);
    }

    [SpellEffectHandler(SpellEffectName.LearnPetSpell)]
    private void EffectLearnPetSpell()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null)
            return;

        if (UnitTarget.AsPlayer != null)
        {
            EffectLearnSpell();

            return;
        }

        var pet = UnitTarget.AsPet;

        if (pet == null)
            return;

        var learnSpellproto = _spellManager.GetSpellInfo(EffectInfo.TriggerSpell);

        if (learnSpellproto == null)
            return;

        pet.LearnSpell(learnSpellproto.Id);
        pet.SavePetToDB(PetSaveMode.AsCurrent);
        pet.OwningPlayer.PetSpellInitialize();
    }

    [SpellEffectHandler(SpellEffectName.SkillStep)]
    private void EffectLearnSkill()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (!UnitTarget.IsTypeId(TypeId.Player))
            return;

        if (Damage < 1)
            return;

        var skillid = (uint)EffectInfo.MiscValue;
        var rcEntry = UnitTarget.AsPlayer.DB2Manager.GetSkillRaceClassInfo(skillid, UnitTarget.Race, UnitTarget.Class);

        if (rcEntry == null)
            return;

        var tier = _gameObjectManager.GetSkillTier(rcEntry.SkillTierID);

        if (tier == null)
            return;

        var skillval = UnitTarget.AsPlayer.GetPureSkillValue((SkillType)skillid);
        UnitTarget.AsPlayer.SetSkill(skillid, (uint)Damage, Math.Max(skillval, (ushort)1), tier.Value[(int)Damage - 1]);
    }

    [SpellEffectHandler(SpellEffectName.LearnSpell)]
    private void EffectLearnSpell()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null)
            return;

        if (!UnitTarget.IsTypeId(TypeId.Player))
        {
            if (UnitTarget.IsPet)
                EffectLearnPetSpell();

            return;
        }

        var player = UnitTarget.AsPlayer;

        if (CastItem != null && EffectInfo.TriggerSpell == 0)
            foreach (var itemEffect in CastItem.Effects)
            {
                if (itemEffect.TriggerType != ItemSpelltriggerType.OnLearn)
                    continue;

                var dependent = false;

                var speciesEntry = _battlePetMgrData.GetBattlePetSpeciesBySpell((uint)itemEffect.SpellID);

                if (speciesEntry != null)
                {
                    player.Session.BattlePetMgr.AddPet(speciesEntry.Id, _battlePetMgrData.SelectPetDisplay(speciesEntry), _battlePetMgrData.RollPetBreed(speciesEntry.Id), _battlePetMgrData.GetDefaultPetQuality(speciesEntry.Id));
                    // If the spell summons a battle pet, we fake that it has been learned and the battle pet is added
                    // marking as dependent prevents saving the spell to database (intended)
                    dependent = true;
                }

                player.LearnSpell((uint)itemEffect.SpellID, dependent);
            }

        if (EffectInfo.TriggerSpell == 0)
            return;

        player.LearnSpell(EffectInfo.TriggerSpell, false);
        Log.Logger.Debug($"Spell: {player.GUID} has learned spell {EffectInfo.TriggerSpell} from {Caster.GUID}");
    }

    [SpellEffectHandler(SpellEffectName.LearnTransmogIllusion)]
    private void EffectLearnTransmogIllusion()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var player = UnitTarget?.AsPlayer;

        if (player == null)
            return;

        var illusionId = (uint)EffectInfo.MiscValue;

        if (!_cliDb.TransmogIllusionStorage.ContainsKey(illusionId))
            return;

        player.Session.CollectionMgr.AddTransmogIllusion(illusionId);
    }

    [SpellEffectHandler(SpellEffectName.LearnTransmogSet)]
    private void EffectLearnTransmogSet()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget is not { IsPlayer: true })
            return;

        UnitTarget.AsPlayer.Session.CollectionMgr.AddTransmogSet((uint)EffectInfo.MiscValue);
    }

    [SpellEffectHandler(SpellEffectName.Milling)]
    private void EffectMilling()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var player = Caster.AsPlayer;

        if (player == null)
            return;

        if (ItemTarget == null || !ItemTarget.Template.HasFlag(ItemFlags.IsMillable))
            return;

        if (ItemTarget.Count < 5)
            return;

        if (Caster.Configuration.GetDefaultValue("SkillChance:Milling", false))
        {
            uint skillValue = player.GetPureSkillValue(SkillType.Inscription);
            var reqSkillValue = ItemTarget.Template.RequiredSkillRank;
            player.UpdateGatherSkill(SkillType.Inscription, skillValue, reqSkillValue);
        }

        ItemTarget.Loot = _lootFactory.GenerateLoot(player.Location.Map, ItemTarget.GUID, LootType.Milling, ItemTarget.Entry, LootStorageType.Milling, player, true);

        player.SendLoot(ItemTarget.Loot);
    }

    [SpellEffectHandler(SpellEffectName.ModifyAuraStacks)]
    private void EffectModifyAuraStacks()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var targetAura = UnitTarget.GetAura(EffectInfo.TriggerSpell);

        if (targetAura == null)
            return;

        switch (EffectInfo.MiscValue)
        {
            case 0:
                targetAura.ModStackAmount(Damage);

                break;

            case 1:
                targetAura.SetStackAmount((byte)Damage);

                break;
        }
    }

    [SpellEffectHandler(SpellEffectName.ModifyCooldown)]
    private void EffectModifyCooldown()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        UnitTarget.SpellHistory.ModifyCooldown(EffectInfo.TriggerSpell, TimeSpan.FromMilliseconds(Damage));
    }

    [SpellEffectHandler(SpellEffectName.ModifyCooldowns)]
    private void EffectModifyCooldowns()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        UnitTarget.SpellHistory
                  .ModifyCoooldowns(itr =>
                                    {
                                        var spellOnCooldown = _spellManager.GetSpellInfo(itr.SpellId);

                                        if ((int)spellOnCooldown.SpellFamilyName != EffectInfo.MiscValue)
                                            return false;

                                        var bitIndex = EffectInfo.MiscValueB - 1;

                                        if (bitIndex is < 0 or >= sizeof(uint) * 8)
                                            return false;

                                        FlagArray128 reqFlag = new()
                                        {
                                            [bitIndex / 32] = 1u << (bitIndex % 32)
                                        };

                                        return spellOnCooldown.SpellFamilyFlags & reqFlag;
                                    },
                                    TimeSpan.FromMilliseconds(Damage));
    }

    [SpellEffectHandler(SpellEffectName.ModifyCooldownsByCategory)]
    private void EffectModifyCooldownsByCategory()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        UnitTarget.SpellHistory.ModifyCoooldowns(itr => _spellManager.GetSpellInfo(itr.SpellId).CategoryId == EffectInfo.MiscValue, TimeSpan.FromMilliseconds(Damage));
    }

    [SpellEffectHandler(SpellEffectName.ModifyCharges)]
    private void EffectModifySpellCharges()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        for (var i = 0; i < Damage; ++i)
            UnitTarget.SpellHistory.RestoreCharge((uint)EffectInfo.MiscValue);
    }

    [SpellEffectHandler(SpellEffectName.ModifyThreatPercent)]
    private void EffectModifyThreatPercent()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster == null || UnitTarget == null)
            return;

        UnitTarget.GetThreatManager().ModifyThreatByPercent(unitCaster, Damage);
    }

    [SpellEffectHandler(SpellEffectName.OpenLock)]
    private void EffectOpenLock()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (!Caster.IsTypeId(TypeId.Player))
        {
            Log.Logger.Debug("WORLD: Open Lock - No Player Caster!");

            return;
        }

        var player = Caster.AsPlayer;

        uint lockId;

        // Get lockId
        if (GameObjTarget != null)
        {
            var goInfo = GameObjTarget.Template;

            if (goInfo.GetNoDamageImmune() != 0 && player.HasUnitFlag(UnitFlags.Immune))
                return;

            switch (goInfo.type)
            {
                // Arathi Basin banner opening. // @todo Verify correctness of this check
                case GameObjectTypes.Button when goInfo.Button.noDamageImmune != 0:
                case GameObjectTypes.Goober when goInfo.Goober.requireLOS != 0:
                {
                    //CanUseBattlegroundObject() already called in CheckCast()
                    // in Battlegroundcheck
                    var bg = player.Battleground;

                    if (bg != null)
                    {
                        bg.EventPlayerClickedOnFlag(player, GameObjTarget);

                        return;
                    }

                    break;
                }
                case GameObjectTypes.CapturePoint:
                    GameObjTarget.AssaultCapturePoint(player);

                    return;

                case GameObjectTypes.FlagStand:
                {
                    //CanUseBattlegroundObject() already called in CheckCast()
                    // in Battlegroundcheck
                    var bg = player.Battleground;

                    if (bg != null)
                    {
                        if (bg.GetTypeID(true) == BattlegroundTypeId.EY)
                            bg.EventPlayerClickedOnFlag(player, GameObjTarget);

                        return;
                    }

                    break;
                }
                case GameObjectTypes.NewFlag:
                    GameObjTarget.Use(player);

                    return;

                default:
                {
                    if (SpellInfo.Id == 1842 && GameObjTarget.Template.type == GameObjectTypes.Trap && GameObjTarget.OwnerUnit != null)
                    {
                        GameObjTarget.SetLootState(LootState.JustDeactivated);

                        return;
                    }
                    // @todo Add script for spell 41920 - Filling, becouse server it freze when use this spell
                    // handle outdoor pvp object opening, return true if go was registered for handling
                    // these objects must have been spawned by outdoorpvp!

                    if (GameObjTarget.Template.type == GameObjectTypes.Goober && _outdoorPvPManager.HandleOpenGo(player, GameObjTarget))
                        return;

                    break;
                }
            }

            lockId = goInfo.GetLockId();
        }
        else if (ItemTarget != null)
            lockId = ItemTarget.Template.LockID;
        else
        {
            Log.Logger.Debug("WORLD: Open Lock - No GameObject/Item Target!");

            return;
        }

        var skillId = SkillType.None;
        var reqSkillValue = 0;
        var skillValue = 0;

        var res = CanOpenLock(EffectInfo, lockId, ref skillId, ref reqSkillValue, ref skillValue);

        if (res != SpellCastResult.SpellCastOk)
        {
            SendCastResult(res);

            return;
        }

        if (GameObjTarget != null)
            GameObjTarget.Use(player);
        else if (ItemTarget != null)
        {
            ItemTarget.SetItemFlag(ItemFieldFlags.Unlocked);
            ItemTarget.SetState(ItemUpdateState.Changed, ItemTarget.OwnerUnit);
        }

        // not allow use skill grow at item base open
        if (CastItem == null && skillId != SkillType.None)
        {
            // update skill if really known
            uint pureSkillValue = player.GetPureSkillValue(skillId);

            if (pureSkillValue != 0)
            {
                if (GameObjTarget != null)
                {
                    // Allow one skill-up until respawned
                    if (!GameObjTarget.IsInSkillupList(player.GUID) &&
                        player.UpdateGatherSkill(skillId, pureSkillValue, (uint)reqSkillValue, 1, GameObjTarget))
                        GameObjTarget.AddToSkillupList(player.GUID);
                }
                else if (ItemTarget != null)
                    // Do one skill-up
                    player.UpdateGatherSkill(skillId, pureSkillValue, (uint)reqSkillValue);
            }
        }

        ExecuteLogEffectOpenLock(EffectInfo.Effect, GameObjTarget ?? (WorldObject)ItemTarget);
    }

    [SpellEffectHandler(SpellEffectName.Parry)]
    private void EffectParry()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        if (Caster.IsTypeId(TypeId.Player))
            Caster.AsPlayer.SetCanParry(true);
    }

    [SpellEffectHandler(SpellEffectName.PersistentAreaAura)]
    private void EffectPersistentAa()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster == null)
            return;

        // only handle at last effect
        for (var i = EffectInfo.EffectIndex + 1; i < SpellInfo.Effects.Count; ++i)
            if (SpellInfo.GetEffect(i).IsEffectName(SpellEffectName.PersistentAreaAura))
                return;

        var radius = EffectInfo.CalcRadius(unitCaster);

        // Caster not in world, might be spell triggered from aura removal
        if (!unitCaster.Location.IsInWorld)
            return;

        DynamicObject dynObj = new(false);

        if (!dynObj.CreateDynamicObject(unitCaster.Location.Map.GenerateLowGuid(HighGuid.DynamicObject), unitCaster, SpellInfo, DestTarget, radius, DynamicObjectType.AreaSpell, SpellVisual))
        {
            dynObj.Dispose();

            return;
        }

        AuraCreateInfo createInfo = new(CastId, SpellInfo, CastDifficulty, SpellConst.MaxEffects, dynObj);
        createInfo.Caster = unitCaster;
        createInfo.SetBaseAmount(SpellValue.EffectBasePoints);
        createInfo.SetCastItem(CastItemGuid, CastItemEntry, CastItemLevel);

        var aura = Aura.TryCreate(createInfo);

        if (aura != null)
        {
            DynObjAura = aura.ToDynObjAura();
            DynObjAura._RegisterForTargets();
        }
        else
            return;

        DynObjAura._ApplyEffectForTargets(EffectInfo.EffectIndex);
    }

    [SpellEffectHandler(SpellEffectName.Pickpocket)]
    private void EffectPickPocket()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var player = Caster.AsPlayer;

        if (player == null)
            return;

        var creature = UnitTarget?.AsCreature;

        if (creature == null)
            return;

        if (creature.CanGeneratePickPocketLoot)
        {
            creature.StartPickPocketRefillTimer();

            creature.Loot = _lootFactory.GenerateLoot(creature.Location.Map, creature.GUID, LootType.Pickpocketing);
            var lootid = creature.Template.PickPocketId;

            if (lootid != 0)
                creature.Loot.FillLoot(lootid, LootStorageType.Pickpocketing, player, true);

            // Generate extra money for pick pocket loot
            var a = RandomHelper.URand(0, creature.Level / 2);
            var b = RandomHelper.URand(0, player.Level / 2);
            creature.Loot.Gold = 10 * (a + b) * Caster.Configuration.GetDefaultValue("Rate:Drop:Money", 1u);
        }
        else if (creature.Loot != null)
        {
            if (creature.Loot.LootType == LootType.Pickpocketing && creature.Loot.IsLooted())
                player.SendLootError(creature.Loot.Guid, creature.GUID, LootError.AlreadPickPocketed);

            return;
        }

        player.SendLoot(creature.Loot);
    }

    [SpellEffectHandler(SpellEffectName.PlayMovie)]
    private void EffectPlayMovie()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (!UnitTarget.IsTypeId(TypeId.Player))
            return;

        var movieId = (uint)EffectInfo.MiscValue;

        if (!_cliDb.MovieStorage.ContainsKey(movieId))
            return;

        UnitTarget.AsPlayer.SendMovieStart(movieId);
    }

    [SpellEffectHandler(SpellEffectName.PlayMusic)]
    private void EffectPlayMusic()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        var soundid = (uint)EffectInfo.MiscValue;

        if (!_cliDb.SoundKitStorage.ContainsKey(soundid))
        {
            Log.Logger.Error("EffectPlayMusic: Sound (Id: {0}) not exist in spell {1}.", soundid, SpellInfo.Id);

            return;
        }

        UnitTarget.AsPlayer.SendPacket(new PlayMusic(soundid));
    }

    [SpellEffectHandler(SpellEffectName.PlayScene)]
    private void EffectPlayScene()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        if (Caster.TypeId != TypeId.Player)
            return;

        Caster.AsPlayer.SceneMgr.PlayScene((uint)EffectInfo.MiscValue, DestTarget);
    }

    [SpellEffectHandler(SpellEffectName.PlaySceneScriptPackage)]
    private void EffectPlaySceneScriptPackage()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        if (!Caster.IsTypeId(TypeId.Player))
            return;

        Caster.AsPlayer.SceneMgr.PlaySceneByPackageId((uint)EffectInfo.MiscValue, SceneFlags.PlayerNonInteractablePhased, DestTarget);
    }

    [SpellEffectHandler(SpellEffectName.PlaySound)]
    private void EffectPlaySound()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null)
            return;

        var player = UnitTarget.AsPlayer;

        if (player == null)
            return;

        switch (SpellInfo.Id)
        {
            case 91604: // Restricted Flight Area
                player.Session.SendNotification(CypherStrings.ZoneNoflyzone);

                break;
        }

        var soundId = (uint)EffectInfo.MiscValue;

        if (!_cliDb.SoundKitStorage.ContainsKey(soundId))
        {
            Log.Logger.Error("EffectPlaySound: Sound (Id: {0}) not exist in spell {1}.", soundId, SpellInfo.Id);

            return;
        }

        player.PlayDirectSound(soundId, player);
    }

    [SpellEffectHandler(SpellEffectName.PowerBurn)]
    private void EffectPowerBurn()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (EffectInfo.MiscValue is < 0 or >= (int)PowerType.Max)
            return;

        var powerType = (PowerType)EffectInfo.MiscValue;

        if (UnitTarget is not { IsAlive: true } || UnitTarget.DisplayPowerType != powerType || Damage < 0)
            return;

        double newDamage = -UnitTarget.ModifyPower(powerType, -Damage);

        // NO - Not a typo - EffectPowerBurn uses effect value multiplier - not effect damage multiplier
        var dmgMultiplier = EffectInfo.CalcValueMultiplier(UnitCasterForEffectHandlers, this);

        // add log data before multiplication (need power amount, not damage)
        ExecuteLogEffectTakeTargetPower(EffectInfo.Effect, UnitTarget, powerType, (uint)newDamage, 0.0f);

        newDamage = newDamage * dmgMultiplier;

        DamageInEffects += newDamage;
    }

    [SpellEffectHandler(SpellEffectName.PowerDrain)]
    private void EffectPowerDrain()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (EffectInfo.MiscValue is < 0 or >= (byte)PowerType.Max)
            return;

        var powerType = (PowerType)EffectInfo.MiscValue;

        if (UnitTarget is not { IsAlive: true } || UnitTarget.DisplayPowerType != powerType || Damage < 0)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        // add spell damage bonus
        if (unitCaster != null)
        {
            var bonus = unitCaster.SpellDamageBonusDone(UnitTarget, SpellInfo, Damage, DamageEffectType.SpellDirect, EffectInfo, 1, this);
            Damage = bonus + bonus * Variance;
            Damage = UnitTarget.SpellDamageBonusTaken(unitCaster, SpellInfo, Damage, DamageEffectType.SpellDirect);
        }

        double newDamage = -UnitTarget.ModifyPower(powerType, -Damage);

        // Don't restore from self drain
        double gainMultiplier = 0.0f;

        if (unitCaster != null && unitCaster != UnitTarget)
        {
            gainMultiplier = EffectInfo.CalcValueMultiplier(unitCaster, this);
            var gain = newDamage * gainMultiplier;

            unitCaster.EnergizeBySpell(unitCaster, SpellInfo, gain, powerType);
        }

        ExecuteLogEffectTakeTargetPower(EffectInfo.Effect, UnitTarget, powerType, (uint)newDamage, gainMultiplier);
    }

    [SpellEffectHandler(SpellEffectName.Proficiency)]
    private void EffectProficiency()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        if (!Caster.IsTypeId(TypeId.Player))
            return;

        var pTarget = Caster.AsPlayer;

        var subClassMask = (uint)SpellInfo.EquippedItemSubClassMask;

        if (SpellInfo.EquippedItemClass == ItemClass.Weapon && !Convert.ToBoolean(pTarget.GetWeaponProficiency() & subClassMask))
        {
            pTarget.AddWeaponProficiency(subClassMask);
            pTarget.SendProficiency(ItemClass.Weapon, pTarget.GetWeaponProficiency());
        }

        if (SpellInfo.EquippedItemClass == ItemClass.Armor && !Convert.ToBoolean(pTarget.GetArmorProficiency() & subClassMask))
        {
            pTarget.AddArmorProficiency(subClassMask);
            pTarget.SendProficiency(ItemClass.Armor, pTarget.GetArmorProficiency());
        }
    }

    [SpellEffectHandler(SpellEffectName.Prospecting)]
    private void EffectProspecting()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var player = Caster.AsPlayer;

        if (player == null)
            return;

        if (ItemTarget == null || !ItemTarget.Template.HasFlag(ItemFlags.IsProspectable))
            return;

        if (ItemTarget.Count < 5)
            return;

        if (Caster.Configuration.GetDefaultValue("SkillChance:Prospecting", false))
        {
            uint skillValue = player.GetPureSkillValue(SkillType.Jewelcrafting);
            var reqSkillValue = ItemTarget.Template.RequiredSkillRank;
            player.UpdateGatherSkill(SkillType.Jewelcrafting, skillValue, reqSkillValue);
        }

        ItemTarget.Loot = _lootFactory.GenerateLoot(player.Location.Map, ItemTarget.GUID, LootType.Prospecting, ItemTarget.Entry, LootStorageType.Prospecting, player, true);

        player.SendLoot(ItemTarget.Loot);
    }

    [SpellEffectHandler(SpellEffectName.PullTowards)]
    private void EffectPullTowards()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null)
            return;

        var pos = Caster.Location.GetFirstCollisionPosition(Caster.CombatReach, Caster.Location.GetRelativeAngle(UnitTarget.Location));

        // This is a blizzlike mistake: this should be 2D distance according to projectile motion formulas, but Blizzard erroneously used 3D distance.
        var distXY = UnitTarget.Location.GetExactDist(pos);

        // Avoid division by 0
        if (distXY < 0.001)
            return;

        var distZ = pos.Z - UnitTarget.Location.Z;
        var speedXY = EffectInfo.MiscValue != 0 ? EffectInfo.MiscValue / 10.0f : 30.0f;
        var speedZ = (float)((2 * speedXY * speedXY * distZ + MotionMaster.GRAVITY * distXY * distXY) / (2 * speedXY * distXY));

        if (!float.IsFinite(speedZ))
        {
            Log.Logger.Error($"Spell {SpellInfo.Id} with SPELL_EFFECT_PULL_TOWARDS called with invalid speedZ. {GetDebugInfo()}");

            return;
        }

        UnitTarget.JumpTo(speedXY, speedZ, 0.0f, pos);
    }

    [SpellEffectHandler(SpellEffectName.PullTowardsDest)]
    private void EffectPullTowardsDest()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null)
            return;

        if (!Targets.HasDst)
        {
            Log.Logger.Error($"Spell {SpellInfo.Id} with SPELL_EFFECT_PULL_TOWARDS_DEST has no dest target");

            return;
        }

        Position pos = Targets.DstPos;
        // This is a blizzlike mistake: this should be 2D distance according to projectile motion formulas, but Blizzard erroneously used 3D distance
        var distXY = UnitTarget.Location.GetExactDist(pos);

        // Avoid division by 0
        if (distXY < 0.001)
            return;

        var distZ = pos.Z - UnitTarget.Location.Z;

        var speedXY = EffectInfo.MiscValue != 0 ? EffectInfo.MiscValue / 10.0f : 30.0f;
        var speedZ = (float)((2 * speedXY * speedXY * distZ + MotionMaster.GRAVITY * distXY * distXY) / (2 * speedXY * distXY));

        if (!float.IsFinite(speedZ))
        {
            Log.Logger.Error($"Spell {SpellInfo.Id} with SPELL_EFFECT_PULL_TOWARDS_DEST called with invalid speedZ. {GetDebugInfo()}");

            return;
        }

        UnitTarget.JumpTo(speedXY, speedZ, 0.0f, pos);
    }

    [SpellEffectHandler(SpellEffectName.ClearQuest)]
    private void EffectQuestClear()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        var player = UnitTarget.AsPlayer;

        var questID = (uint)EffectInfo.MiscValue;

        var quest = _gameObjectManager.GetQuestTemplate(questID);

        if (quest == null)
            return;

        var oldStatus = player.GetQuestStatus(questID);

        // Player has never done this quest
        if (oldStatus == QuestStatus.None)
            return;

        // remove all quest entries for 'entry' from quest log
        for (byte slot = 0; slot < SharedConst.MaxQuestLogSize; ++slot)
        {
            var logQuest = player.GetQuestSlotQuestId(slot);

            if (logQuest == questID)
            {
                player.SetQuestSlot(slot, 0);

                // we ignore unequippable quest items in this case, it's still be equipped
                player.TakeQuestSourceItem(logQuest, false);

                if (quest.HasFlag(QuestFlags.Pvp))
                {
                    player.PvpInfo.IsHostile = player.PvpInfo.IsInHostileArea || player.HasPvPForcingQuest();
                    player.UpdatePvPState();
                }
            }
        }

        player.RemoveActiveQuest(questID, false);
        player.RemoveRewardedQuest(questID);

        _scriptManager.ForEach<IPlayerOnQuestStatusChange>(p => p.OnQuestStatusChange(player, questID));
        _scriptManager.RunScript<IQuestOnQuestStatusChange>(script => script.OnQuestStatusChange(player, quest, oldStatus, QuestStatus.None), quest.ScriptId);
    }

    [SpellEffectHandler(SpellEffectName.QuestComplete)]
    private void EffectQuestComplete()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        var player = UnitTarget.AsPlayer;

        var questId = (uint)EffectInfo.MiscValue;

        if (questId == 0)
            return;

        var quest = _gameObjectManager.GetQuestTemplate(questId);

        if (quest == null)
            return;

        var logSlot = player.FindQuestSlot(questId);

        if (logSlot < SharedConst.MaxQuestLogSize)
            player.AreaExploredOrEventHappens(questId);
        else if (quest.HasFlag(QuestFlags.Tracking)) // Check if the quest is used as a serverside Id.
            player.SetRewardedQuest(questId);        // If so, set status to rewarded without broadcasting it to client.
    }

    [SpellEffectHandler(SpellEffectName.QuestFail)]
    private void EffectQuestFail()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        UnitTarget.AsPlayer.FailQuest((uint)EffectInfo.MiscValue);
    }

    [SpellEffectHandler(SpellEffectName.QuestStart)]
    private void EffectQuestStart()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var player = UnitTarget?.AsPlayer;

        if (player == null)
            return;

        var quest = _gameObjectManager.GetQuestTemplate((uint)EffectInfo.MiscValue);

        if (quest == null)
            return;

        if (!player.CanTakeQuest(quest, false))
            return;

        if (quest.IsAutoAccept && player.CanAddQuest(quest, false))
        {
            player.AddQuestAndCheckCompletion(quest, null);
            player.PlayerTalkClass.SendQuestGiverQuestDetails(quest, player.GUID, true, true);
        }
        else
            player.PlayerTalkClass.SendQuestGiverQuestDetails(quest, player.GUID, true, false);
    }

    [SpellEffectHandler(SpellEffectName.RechargeItem)]
    private void EffectRechargeItem()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var player = UnitTarget?.AsPlayer;

        var item = player?.GetItemByEntry(EffectInfo.ItemType);

        if (item == null)
            return;

        foreach (var itemEffect in item.Effects)
            if (itemEffect.LegacySlotIndex <= item.ItemData.SpellCharges.GetSize())
                item.SetSpellCharges(itemEffect.LegacySlotIndex, itemEffect.Charges);

        item.SetState(ItemUpdateState.Changed, player);
    }

    [SpellEffectHandler(SpellEffectName.RedirectThreat)]
    private void EffectRedirectThreat()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster == null)
            return;

        if (UnitTarget != null)
            unitCaster.GetThreatManager().RegisterRedirectThreat(SpellInfo.Id, UnitTarget.GUID, (uint)Damage);
    }

    [SpellEffectHandler(SpellEffectName.RemoveAura)]
    [SpellEffectHandler(SpellEffectName.RemoveAura2)]
    private void EffectRemoveAura()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        // there may be need of specifying casterguid of removed auras
        UnitTarget?.RemoveAura(EffectInfo.TriggerSpell);
    }

    [SpellEffectHandler(SpellEffectName.RemoveAuraBySApellLabel)]
    private void EffectRemoveAuraBySpellLabel()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null)
            return;

        UnitTarget.GetAppliedAurasQuery().HasLabel((uint)EffectInfo.MiscValue).Execute(UnitTarget.RemoveAura);
    }

    [SpellEffectHandler(SpellEffectName.RemoveTalent)]
    private void EffectRemoveTalent()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (!_cliDb.TalentStorage.TryGetValue(SpellMisc.TalentId, out var talent))
            return;

        var player = UnitTarget?.AsPlayer;

        if (player == null)
            return;

        player.RemoveTalent(talent);
        player.SendTalentsInfoData();
    }

    [SpellEffectHandler(SpellEffectName.AllowRenamePet)]
    private void EffectRenamePet()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null ||
            !UnitTarget.IsTypeId(TypeId.Unit) ||
            !UnitTarget.IsPet ||
            UnitTarget.AsPet.PetType != PetType.Hunter)
            return;

        UnitTarget.SetPetFlag(UnitPetFlags.CanBeRenamed);
    }

    [SpellEffectHandler(SpellEffectName.Reputation)]
    private void EffectReputation()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        var player = UnitTarget.AsPlayer;

        var repChange = (int)Damage;

        var factionId = EffectInfo.MiscValue;

        if (!_cliDb.FactionStorage.TryGetValue((uint)factionId, out var factionEntry))
            return;

        repChange = player.CalculateReputationGain(ReputationSource.Spell, 0, repChange, factionId);

        player.ReputationMgr.ModifyReputation(factionEntry, repChange);
    }

    [SpellEffectHandler(SpellEffectName.Resurrect)]
    private void EffectResurrect()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (CorpseTarget == null && UnitTarget == null)
            return;

        Player player = null;

        if (CorpseTarget != null)
            player = _objectAccessor.FindPlayer(CorpseTarget.OwnerGUID);
        else if (UnitTarget != null)
            player = UnitTarget.AsPlayer;

        if (player == null || player.IsAlive || !player.Location.IsInWorld)
            return;

        if (player.IsResurrectRequested) // already have one active request
            return;

        var health = (uint)player.CountPctFromMaxHealth(Damage);
        var mana = (uint)MathFunctions.CalculatePct(player.GetMaxPower(PowerType.Mana), Damage);

        ExecuteLogEffectResurrect(EffectInfo.Effect, player);

        player.SetResurrectRequestData(Caster, health, mana, 0);
        SendResurrectRequest(player);
    }

    private void EffectResurrectNew()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (CorpseTarget == null && UnitTarget == null)
            return;

        Player player = null;

        if (CorpseTarget != null)
            player = _objectAccessor.FindPlayer(CorpseTarget.OwnerGUID);
        else if (UnitTarget != null)
            player = UnitTarget.AsPlayer;

        if (player == null || player.IsAlive || !player.Location.IsInWorld)
            return;

        if (player.IsResurrectRequested) // already have one active request
            return;

        ExecuteLogEffectResurrect(EffectInfo.Effect, player);
        player.SetResurrectRequestData(Caster, (uint)Damage, (uint)EffectInfo.MiscValue, 0);
        SendResurrectRequest(player);
    }

    [SpellEffectHandler(SpellEffectName.ResurrectPet)]
    private void EffectResurrectPet()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        if (Damage < 0)
            return;

        var player = Caster.AsPlayer;

        if (player == null)
            return;

        // Maybe player dismissed dead pet or pet despawned?
        var hadPet = true;

        if (player.CurrentPet == null)
        {
            var petStable = player.PetStable;
            var deadPetIndex = Array.FindIndex(petStable.ActivePets, petInfo => petInfo?.Health == 0);

            var slot = (PetSaveMode)deadPetIndex;

            player.SummonPet(0, slot, new Position(), 0);
            hadPet = false;
        }

        // TODO: Better to fail Hunter's "Revive Pet" at cast instead of here when casting ends
        var pet = player.CurrentPet; // Attempt to get current pet

        if (pet == null || pet.IsAlive)
            return;

        // If player did have a pet before reviving, teleport it
        if (hadPet)
        {
            // Reposition the pet's corpse before reviving so as not to grab aggro
            // We can use a different, more accurate version of GetClosePoint() since we have a pet
            // Will be used later to reposition the pet if we have one
            var closePoint = new Position();
            player.Location.GetClosePoint(closePoint, pet.CombatReach, SharedConst.PetFollowDist, pet.FollowAngle);
            closePoint.Orientation = player.Location.Orientation;
            pet.NearTeleportTo(closePoint);
            pet.Location.Relocate(closePoint); // This is needed so SaveStayPosition() will get the proper coords.
        }

        pet.ReplaceAllDynamicFlags(UnitDynFlags.None);
        pet.RemoveUnitFlag(UnitFlags.Skinnable);
        pet.SetDeathState(DeathState.Alive);
        pet.ClearUnitState(UnitState.AllErasable);
        pet.SetHealth(pet.CountPctFromMaxHealth(Damage));

        // Reset things for when the AI to takes over
        var ci = pet.GetCharmInfo();

        if (ci != null)
        {
            // In case the pet was at stay, we don't want it running back
            ci.SaveStayPosition();
            ci.SetIsAtStay(ci.HasCommandState(CommandStates.Stay));

            ci.SetIsFollowing(false);
            ci.SetIsCommandAttack(false);
            ci.SetIsCommandFollow(false);
            ci.SetIsReturning(false);
        }

        pet.SavePetToDB(PetSaveMode.AsCurrent);
    }

    [SpellEffectHandler(SpellEffectName.ResurrectWithAura)]
    private void EffectResurrectWithAura()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.Location.IsInWorld)
            return;

        var target = UnitTarget.AsPlayer;

        if (target == null)
            return;

        if (UnitTarget.IsAlive)
            return;

        if (target.IsResurrectRequested) // already have one active request
            return;

        var health = (uint)target.CountPctFromMaxHealth(Damage);
        var mana = (uint)MathFunctions.CalculatePct(target.GetMaxPower(PowerType.Mana), Damage);
        uint resurrectAura = 0;

        if (_spellManager.HasSpellInfo(EffectInfo.TriggerSpell))
            resurrectAura = EffectInfo.TriggerSpell;

        if (resurrectAura != 0 && target.HasAura(resurrectAura))
            return;

        ExecuteLogEffectResurrect(EffectInfo.Effect, target);
        target.SetResurrectRequestData(Caster, health, mana, resurrectAura);
        SendResurrectRequest(target);
    }

    [SpellEffectHandler(SpellEffectName.Sanctuary)]
    [SpellEffectHandler(SpellEffectName.Sanctuary2)]
    private void EffectSanctuary()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null)
            return;

        if (UnitTarget.IsPlayer && !UnitTarget.Location.Map.IsDungeon)
            // stop all pve combat for players outside dungeons, suppress pvp combat
            UnitTarget.CombatStop(false, false);
        else
            // in dungeons (or for nonplayers), reset this unit on all enemies' threat lists
            foreach (var pair in UnitTarget.GetThreatManager().ThreatenedByMeList)
                pair.Value.ScaleThreat(0.0f);

        // makes spells cast before this time fizzle
        UnitTarget.LastSanctuaryTime = GameTime.CurrentTimeMS;
    }

    [SpellEffectHandler(SpellEffectName.SchoolDamage)]
    private void EffectSchoolDmg()
    {
        if (_effectHandleMode != SpellEffectHandleMode.LaunchTarget)
            return;

        if (UnitTarget is { IsAlive: true })
        {
            // Meteor like spells (divided damage to targets)
            if (SpellInfo.HasAttribute(SpellCustomAttributes.ShareDamage))
            {
                var count = GetUnitTargetCountForEffect(EffectInfo.EffectIndex);

                // divide to all targets
                if (count != 0)
                    Damage /= count;
            }

            var unitCaster = UnitCasterForEffectHandlers;

            if (unitCaster != null)
            {
                var bonus = unitCaster.SpellDamageBonusDone(UnitTarget, SpellInfo, Damage, DamageEffectType.SpellDirect, EffectInfo, 1, this);
                Damage = bonus + bonus * Variance;
                Damage = UnitTarget.SpellDamageBonusTaken(unitCaster, SpellInfo, Damage, DamageEffectType.SpellDirect);
            }

            DamageInEffects += Damage;
        }
    }

    [SpellEffectHandler(SpellEffectName.ScriptEffect)]
    private void EffectScriptEffect()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        // @todo we must implement hunter pet summon at login there (spell 6962)
        /// @todo: move this to scripts
        switch (SpellInfo.SpellFamilyName)
        {
            case SpellFamilyNames.Generic:
            {
                switch (SpellInfo.Id)
                {
                    case 45204: // Clone Me!
                        Caster.SpellFactory.CastSpell(UnitTarget, (uint)Damage, new CastSpellExtraArgs(true));

                        break;
                    // Shadow Flame (All script effects, not just end ones to prevent player from dodging the last triggered spell)
                    case 22539:
                    case 22972:
                    case 22975:
                    case 22976:
                    case 22977:
                    case 22978:
                    case 22979:
                    case 22980:
                    case 22981:
                    case 22982:
                    case 22983:
                    case 22984:
                    case 22985:
                    {
                        if (UnitTarget is not { IsAlive: true })
                            return;

                        // Onyxia Scale Cloak
                        if (UnitTarget.HasAura(22683))
                            return;

                        // Shadow Flame
                        Caster.SpellFactory.CastSpell(UnitTarget, 22682, new CastSpellExtraArgs(this));

                        return;
                    }
                    // Mug Transformation
                    case 41931:
                    {
                        if (!Caster.IsTypeId(TypeId.Player))
                            return;

                        byte bag = 19;
                        byte slot = 0;
                        Item item;

                        while (bag != 0) // 256 = 0 due to var type
                        {
                            item = Caster.AsPlayer.GetItemByPos(bag, slot);

                            if (item is { Entry: 38587 })
                                break;

                            ++slot;

                            if (slot == 39)
                            {
                                slot = 0;
                                ++bag;
                            }
                        }

                        if (bag != 0)
                        {
                            if (Caster.AsPlayer.GetItemByPos(bag, slot).Count == 1) Caster.AsPlayer.RemoveItem(bag, slot, true);
                            else Caster.AsPlayer.GetItemByPos(bag, slot).SetCount(Caster.AsPlayer.GetItemByPos(bag, slot).Count - 1);

                            // Spell 42518 (Braufest - Gratisprobe des Braufest herstellen)
                            Caster.SpellFactory.CastSpell(Caster, 42518, new CastSpellExtraArgs(this));

                            return;
                        }

                        break;
                    }
                    // Brutallus - Burn
                    case 45141:
                    case 45151:
                    {
                        //Workaround for Range ... should be global for every ScriptEffect
                        var radius = EffectInfo.CalcRadius();

                        if (UnitTarget != null && UnitTarget.IsTypeId(TypeId.Player) && UnitTarget.Location.GetDistance(Caster) >= radius && !UnitTarget.HasAura(46394) && UnitTarget != Caster)
                            UnitTarget.SpellFactory.CastSpell(UnitTarget, 46394, new CastSpellExtraArgs(this));

                        break;
                    }
                    // Emblazon Runeblade
                    case 51770:
                    {
                        if (OriginalCaster == null)
                            return;

                        OriginalCaster.SpellFactory.CastSpell(OriginalCaster, (uint)Damage, new CastSpellExtraArgs(false));

                        break;
                    }
                    // Summon Ghouls On Scarlet Crusade
                    case 51904:
                    {
                        if (!Targets.HasDst)
                            return;

                        var radius = EffectInfo.CalcRadius();

                        for (byte i = 0; i < 15; ++i)
                            Caster.SpellFactory.CastSpell(Caster.Location.GetRandomPoint(DestTarget, radius), 54522, new CastSpellExtraArgs(this));

                        break;
                    }
                    case 52173: // Coyote Spirit Despawn
                    case 60243: // Blood Parrot Despawn
                        if (UnitTarget.IsTypeId(TypeId.Unit) && UnitTarget.AsCreature.IsSummon)
                            UnitTarget.ToTempSummon().UnSummon();

                        return;

                    case 57347: // Retrieving (Wintergrasp RP-GG pickup spell)
                    {
                        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Unit) || !Caster.IsTypeId(TypeId.Player))
                            return;

                        UnitTarget.AsCreature.DespawnOrUnsummon();

                        return;
                    }
                    case 57349: // Drop RP-GG (Wintergrasp RP-GG at death drop spell)
                    {
                        if (!Caster.IsTypeId(TypeId.Player))
                            return;

                        // Delete item from inventory at death
                        Caster.
                            // Delete item from inventory at death
                            AsPlayer.DestroyItemCount((uint)Damage, 5, true);

                        return;
                    }
                    case 58941: // Rock Shards
                        if (UnitTarget != null && OriginalCaster != null)
                        {
                            for (uint i = 0; i < 3; ++i)
                            {
                                OriginalCaster.SpellFactory.CastSpell(UnitTarget, 58689, new CastSpellExtraArgs(true));
                                OriginalCaster.SpellFactory.CastSpell(UnitTarget, 58692, new CastSpellExtraArgs(true));
                            }

                            if (OriginalCaster.Location.Map.DifficultyID == Difficulty.None)
                            {
                                OriginalCaster.SpellFactory.CastSpell(UnitTarget, 58695, new CastSpellExtraArgs(true));
                                OriginalCaster.SpellFactory.CastSpell(UnitTarget, 58696, new CastSpellExtraArgs(true));
                            }
                            else
                            {
                                OriginalCaster.SpellFactory.CastSpell(UnitTarget, 60883, new CastSpellExtraArgs(true));
                                OriginalCaster.SpellFactory.CastSpell(UnitTarget, 60884, new CastSpellExtraArgs(true));
                            }
                        }

                        return;

                    case 62482: // Grab Crate
                    {
                        if (unitCaster == null)
                            return;

                        if (UnitTarget != null)
                        {
                            var seat = unitCaster.VehicleBase;

                            var parent = seat?.VehicleBase;

                            if (parent != null)
                            {
                                // @todo a hack, range = 11, should after some time cast, otherwise too far
                                unitCaster.SpellFactory.CastSpell(parent, 62496, new CastSpellExtraArgs(this));
                                UnitTarget.SpellFactory.CastSpell(parent, (uint)Damage, new CastSpellExtraArgs().SetTriggeringSpell(this)); // DIFFICULTY_NONE, so effect always valid
                            }
                        }

                        return;
                    }
                }

                break;
            }
        }

        // normal DB scripted effect
        Log.Logger.Debug("Spell ScriptStart spellid {0} in EffectScriptEffect({1})", SpellInfo.Id, EffectInfo.EffectIndex);
        Caster.Location.Map.ScriptsStart(ScriptsType.Spell, (uint)((int)SpellInfo.Id | EffectInfo.EffectIndex << 24), Caster, UnitTarget);
    }

    [SpellEffectHandler(SpellEffectName.SelfResurrect)]
    private void EffectSelfResurrect()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        var player = Caster.AsPlayer;

        if (player == null || !player.Location.IsInWorld || player.IsAlive)
            return;

        uint health;
        var mana = 0;

        // flat case
        if (Damage < 0)
        {
            health = (uint)-Damage;
            mana = EffectInfo.MiscValue;
        }
        // percent case
        else
        {
            health = (uint)player.CountPctFromMaxHealth(Damage);

            if (player.GetMaxPower(PowerType.Mana) > 0)
                mana = MathFunctions.CalculatePct(player.GetMaxPower(PowerType.Mana), Damage);
        }

        player.ResurrectPlayer(0.0f);

        player.SetHealth(health);
        player.SetPower(PowerType.Mana, mana);
        player.SetPower(PowerType.Rage, 0);
        player.SetFullPower(PowerType.Energy);
        player.SetPower(PowerType.Focus, 0);

        player.SpawnCorpseBones();
    }

    [SpellEffectHandler(SpellEffectName.SendChatMessage)]
    private void EffectSendChatMessage()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster == null)
            return;

        var broadcastTextId = (uint)EffectInfo.MiscValue;

        if (!_cliDb.BroadcastTextStorage.ContainsKey(broadcastTextId))
            return;

        var chatType = (ChatMsg)EffectInfo.MiscValueB;
        unitCaster.Talk(broadcastTextId, chatType, _creatureTextManager.GetRangeForChatType(chatType), UnitTarget);
    }

    [SpellEffectHandler(SpellEffectName.SendEvent)]
    private void EffectSendEvent()
    {
        // we do not handle a Id dropping or clicking on Id in Battlegroundby sendevent system
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget && _effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        WorldObject target = null;

        // call events for object target if present
        if (_effectHandleMode == SpellEffectHandleMode.HitTarget)
        {
            if (UnitTarget != null)
                target = UnitTarget;
            else if (GameObjTarget != null)
                target = GameObjTarget;
            else if (CorpseTarget != null)
                target = CorpseTarget;
        }
        else // if (effectHandleMode == SpellEffectHandleMode.Hit)
        {
            // let's prevent executing effect handler twice in case when spell effect is capable of targeting an object
            // this check was requested by scripters, but it has some downsides:
            // now it's impossible to script (using sEventScripts) a cast which misses all targets
            // or to have an ability to script the moment spell hits dest (in a case when there are object targets present)
            if (EffectInfo.ProvidedTargetMask.HasAnyFlag(SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.GameobjectMask))
                return;

            // some spells have no target entries in dbc and they use focus target
            if (_focusObject != null)
                target = _focusObject;
            // @todo there should be a possibility to pass dest target to event script
        }

        Log.Logger.Debug("Spell ScriptStart {0} for spellid {1} in EffectSendEvent ", EffectInfo.MiscValue, SpellInfo.Id);

        GameEvents.Trigger((uint)EffectInfo.MiscValue, Caster, target);
    }

    [SpellEffectHandler(SpellEffectName.SendTaxi)]
    private void EffectSendTaxi()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        UnitTarget.AsPlayer.ActivateTaxiPathTo((uint)EffectInfo.MiscValue, SpellInfo.Id);
    }

    [SpellEffectHandler(SpellEffectName.Skill)]
    private void EffectSkill()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        Log.Logger.Debug("WORLD: SkillEFFECT");
    }

    [SpellEffectHandler(SpellEffectName.Skinning)]
    private void EffectSkinning()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (!UnitTarget.IsTypeId(TypeId.Unit))
            return;

        var player = Caster.AsPlayer;

        if (player == null)
            return;

        var creature = UnitTarget.AsCreature;
        var targetLevel = (int)creature.GetLevelForTarget(Caster);

        var skill = creature.Template.GetRequiredLootSkill();

        creature.SetUnitFlag3(UnitFlags3.AlreadySkinned);
        creature.SetDynamicFlag(UnitDynFlags.Lootable);
        var loot = _lootFactory.GenerateLoot(creature.Location.Map, creature.GUID, LootType.Skinning);

        if (loot != null)
            creature.PersonalLoot[player.GUID] = loot;

        loot.FillLoot(creature.Template.SkinLootId, _lootStoreBox.Skinning, player, true);
        player.SendLoot(loot);

        if (skill == SkillType.Skinning)
        {
            var reqValue = targetLevel switch
            {
                <= 10 => 1,
                < 16  => (targetLevel - 10) * 10,
                <= 23 => (int)(targetLevel * 4.8),
                < 39  => targetLevel * 10 - 205,
                <= 44 => targetLevel * 5 + 5,
                <= 52 => targetLevel * 5,
                _     => 300
            };

            // TODO: Specialize skillid for each expansion
            // new db field?
            // tied to one of existing expansion fields in creature_template?

            // Double chances for elites
            Caster.
                // TODO: Specialize skillid for each expansion
                // new db field?
                // tied to one of existing expansion fields in creature_template?

                // Double chances for elites
                AsPlayer.UpdateGatherSkill(skill, (uint)Damage, (uint)reqValue, (uint)(creature.IsElite ? 2 : 1));
        }
    }

    // remove insignia spell effect
    [SpellEffectHandler(SpellEffectName.SkinPlayerCorpse)]
    private void EffectSkinPlayerCorpse()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        Log.Logger.Debug("Effect: SkinPlayerCorpse");

        var player = Caster.AsPlayer;
        Player target = null;

        if (UnitTarget != null)
            target = UnitTarget.AsPlayer;
        else if (CorpseTarget != null)
            target = _objectAccessor.FindPlayer(CorpseTarget.OwnerGUID);

        if (player == null || target == null || target.IsAlive)
            return;

        target.RemovedInsignia(player);
    }

    private void EffectSpiritHeal()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;
    }

    [SpellEffectHandler(SpellEffectName.StealBeneficialBuff)]
    private void EffectStealBeneficialBuff()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        Log.Logger.Debug("Effect: StealBeneficialBuff");

        if (UnitTarget == null || UnitTarget == Caster) // can't steal from self
            return;

        List<DispelableAura> stealList = new();

        // Create dispel mask by dispel type
        var dispelMask = SpellInfo.GetDispelMask((DispelType)EffectInfo.MiscValue);

        foreach (var aura in UnitTarget.OwnedAurasList)
        {
            var aurApp = aura.GetApplicationOfTarget(UnitTarget.GUID);

            if (aurApp == null)
                continue;

            if (!Convert.ToBoolean(aura.SpellInfo.DispelMask & dispelMask))
                continue;

            // Need check for passive? this
            if (!aurApp.IsPositive || aura.IsPassive || aura.SpellInfo.HasAttribute(SpellAttr4.CannotBeStolen))
                continue;

            // 2.4.3 Patch Notes: "Dispel effects will no longer attempt to remove effects that have 100% dispel resistance."
            var chance = aura.CalcDispelChance(UnitTarget, !UnitTarget.WorldObjectCombat.IsFriendlyTo(Caster));

            if (chance == 0)
                continue;

            // The charges / stack amounts don't count towards the total number of auras that can be dispelled.
            // Ie: A dispel on a target with 5 stacks of Winters Chill and a Polymorph has 1 / (1 + 1) . 50% chance to dispell
            // Polymorph instead of 1 / (5 + 1) . 16%.
            var dispelCharges = aura.SpellInfo.HasAttribute(SpellAttr7.DispelCharges);
            var charges = dispelCharges ? aura.Charges : aura.StackAmount;

            if (charges > 0)
                stealList.Add(new DispelableAura(aura, chance, charges));
        }

        if (stealList.Empty())
            return;

        var remaining = stealList.Count;

        // Ok if exist some buffs for dispel try dispel it
        List<Tuple<uint, ObjectGuid, int>> successList = new();

        DispelFailed dispelFailed = new()
        {
            CasterGUID = Caster.GUID,
            VictimGUID = UnitTarget.GUID,
            SpellID = SpellInfo.Id
        };

        // dispel N = damage buffs (or while exist buffs for dispel)
        for (var count = 0; count < Damage && remaining > 0;)
        {
            // Random select buff for dispel
            var dispelableAura = stealList[RandomHelper.IRand(0, remaining - 1)];

            if (dispelableAura.RollDispel())
            {
                byte stolenCharges = 1;

                if (dispelableAura.Aura.SpellInfo.HasAttribute(SpellAttr1.DispelAllStacks))
                    stolenCharges = dispelableAura.DispelCharges;

                successList.Add(Tuple.Create(dispelableAura.Aura.Id, dispelableAura.Aura.CasterGuid, (int)stolenCharges));

                if (!dispelableAura.DecrementCharge(stolenCharges))
                {
                    --remaining;
                    stealList[remaining] = dispelableAura;
                }
            }
            else
                dispelFailed.FailedSpells.Add(dispelableAura.Aura.Id);

            ++count;
        }

        if (!dispelFailed.FailedSpells.Empty())
            Caster.SendMessageToSet(dispelFailed, true);

        if (successList.Empty())
            return;

        SpellDispellLog spellDispellLog = new()
        {
            IsBreak = false, // TODO: use me
            IsSteal = true,
            TargetGUID = UnitTarget.GUID,
            CasterGUID = Caster.GUID,
            DispelledBySpellID = SpellInfo.Id
        };

        foreach (var (spellId, auraCaster, stolenCharges) in successList)
        {
            var dispellData = new SpellDispellData
            {
                SpellID = spellId,
                Harmful = false // TODO: use me
            };

            UnitTarget.RemoveAurasDueToSpellBySteal(spellId, auraCaster, Caster, stolenCharges);

            spellDispellLog.DispellData.Add(dispellData);
        }

        Caster.SendMessageToSet(spellDispellLog, true);
    }

    [SpellEffectHandler(SpellEffectName.Stuck)]
    private void EffectStuck()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        if (!Caster.Configuration.GetDefaultValue("CastUnstuck", true))
            return;

        var player = Caster.AsPlayer;

        if (player == null)
            return;

        Log.Logger.Debug("Spell Effect: Stuck");
        Log.Logger.Information("Player {0} (guid {1}) used auto-unstuck future at map {2} ({3}, {4}, {5})", player.GetName(), player.GUID.ToString(), player.Location.MapId, player.Location.X, player.Location.Y, player.Location.Z);

        if (player.IsInFlight)
            return;

        // if player is dead without death timer is teleported to graveyard, otherwise not apply the effect
        if (player.IsDead)
        {
            if (player.DeathTimer == 0)
                player.RepopAtGraveyard();

            return;
        }

        // the player dies if hearthstone is in cooldown, else the player is teleported to home
        if (player.SpellHistory.HasCooldown(8690))
        {
            player.KillSelf();

            return;
        }

        player.TeleportTo(player.Homebind, TeleportToOptions.Spell);

        // Stuck spell trigger Hearthstone cooldown
        var spellInfo = _spellManager.GetSpellInfo(8690, CastDifficulty);

        if (spellInfo == null)
            return;

        var spell = player.SpellFactory.NewSpell(spellInfo, TriggerCastFlags.FullMask);
        spell.SendSpellCooldown();
    }

    [SpellEffectHandler(SpellEffectName.SummonChangeItem)]
    private void EffectSummonChangeItem()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        if (!Caster.IsTypeId(TypeId.Player))
            return;

        var player = Caster.AsPlayer;

        // applied only to using item
        if (CastItem == null)
            return;

        // ... only to item in own inventory/bank/equip_slot
        if (CastItem.OwnerGUID != player.GUID)
            return;

        var newitemid = EffectInfo.ItemType;

        if (newitemid == 0)
            return;

        var pos = CastItem.Pos;

        var pNewItem = Item.CreateItem(newitemid, 1, CastItem.GetContext(), player);

        if (pNewItem == null)
            return;

        for (var j = EnchantmentSlot.Perm; j <= EnchantmentSlot.Temp; ++j)
            if (CastItem.GetEnchantmentId(j) != 0)
                pNewItem.SetEnchantment(j, CastItem.GetEnchantmentId(j), CastItem.GetEnchantmentDuration(j), (uint)CastItem.GetEnchantmentCharges(j));

        if (CastItem.ItemData.Durability < CastItem.ItemData.MaxDurability)
        {
            double lossPercent = 1 - CastItem.ItemData.Durability / CastItem.ItemData.MaxDurability;
            player.DurabilityLoss(pNewItem, lossPercent);
        }

        if (player.IsInventoryPos(pos))
        {
            List<ItemPosCount> dest = new();
            var msg = player.CanStoreItem(CastItem.BagSlot, CastItem.Slot, dest, pNewItem, true);

            if (msg == InventoryResult.Ok)
            {
                player.DestroyItem(CastItem.BagSlot, CastItem.Slot, true);

                // prevent crash at access and unexpected charges counting with item update queue corrupt
                if (CastItem == Targets.ItemTarget)
                    Targets.ItemTarget = null;

                CastItem = null;
                CastItemGuid.Clear();
                CastItemEntry = 0;
                CastItemLevel = -1;

                player.StoreItem(dest, pNewItem, true);
                player.SendNewItem(pNewItem, 1, true, false);
                player.ItemAddedQuestCheck(newitemid, 1);
            }
        }
        else if (_playerComputators.IsBankPos(pos))
        {
            List<ItemPosCount> dest = new();
            var msg = player.CanBankItem(CastItem.BagSlot, CastItem.Slot, dest, pNewItem, true);

            if (msg == InventoryResult.Ok)
            {
                player.DestroyItem(CastItem.BagSlot, CastItem.Slot, true);

                // prevent crash at access and unexpected charges counting with item update queue corrupt
                if (CastItem == Targets.ItemTarget)
                    Targets.ItemTarget = null;

                CastItem = null;
                CastItemGuid.Clear();
                CastItemEntry = 0;
                CastItemLevel = -1;

                player.BankItem(dest, pNewItem, true);
            }
        }
        else if (_playerComputators.IsEquipmentPos(pos))
        {
            player.DestroyItem(CastItem.BagSlot, CastItem.Slot, true);

            var msg = player.CanEquipItem(CastItem.Slot, out var dest, pNewItem, true);

            if (msg is InventoryResult.Ok or InventoryResult.ClientLockedOut)
            {
                if (msg == InventoryResult.ClientLockedOut)
                    dest = EquipmentSlot.MainHand;

                // prevent crash at access and unexpected charges counting with item update queue corrupt
                if (CastItem == Targets.ItemTarget)
                    Targets.ItemTarget = null;

                CastItem = null;
                CastItemGuid.Clear();
                CastItemEntry = 0;
                CastItemLevel = -1;

                player.EquipItem(dest, pNewItem, true);
                player.AutoUnequipOffhandIfNeed();
                player.SendNewItem(pNewItem, 1, true, false);
                player.ItemAddedQuestCheck(newitemid, 1);
            }
        }
    }

    [SpellEffectHandler(SpellEffectName.SummonObjectSlot1)]
    private void EffectSummonObject()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster == null)
            return;

        var slot = (byte)(EffectInfo.Effect - SpellEffectName.SummonObjectSlot1);
        var guid = unitCaster.ObjectSlot[slot];

        if (!guid.IsEmpty)
        {
            var obj = unitCaster.Location.Map.GetGameObject(guid);

            if (obj != null)
            {
                // Recast case - null spell id to make auras not be removed on object remove from world
                if (SpellInfo.Id == obj.SpellId)
                    obj.SpellId = 0;

                unitCaster.RemoveGameObject(obj, true);
            }

            unitCaster.ObjectSlot[slot].Clear();
        }

        var pos = new Position();

        // If dest location if present
        if (Targets.HasDst)
            pos = DestTarget.Copy();
        // Summon in random point all other units if location present
        else
        {
            unitCaster.Location.GetClosePoint(pos, SharedConst.DefaultPlayerBoundingRadius);
            pos.Orientation = unitCaster.Location.Orientation;
        }

        var map = Caster.Location.Map;
        var rotation = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(pos.Orientation, 0.0f, 0.0f));
        var go = _gameObjectFactory.CreateGameObject((uint)EffectInfo.MiscValue, map, pos, rotation, 255, GameObjectState.Ready);

        if (go == null)
            return;

        _phasingHandler.InheritPhaseShift(go, Caster);

        go.Faction = unitCaster.Faction;
        go.SetLevel(unitCaster.Level);
        var duration = SpellInfo.CalcDuration(Caster);
        go.SetRespawnTime(duration > 0 ? duration / Time.IN_MILLISECONDS : 0);
        go.SpellId = SpellInfo.Id;
        unitCaster.AddGameObject(go);

        ExecuteLogEffectSummonObject(EffectInfo.Effect, go);

        map.AddToMap(go);

        unitCaster.ObjectSlot[slot] = go.GUID;
    }

    [SpellEffectHandler(SpellEffectName.SummonObjectWild)]
    private void EffectSummonObjectWild()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        var target = _focusObject ?? Caster;

        var pos = new Position();

        if (Targets.HasDst)
            pos = DestTarget.Copy();
        else
        {
            Caster.Location.GetClosePoint(pos, SharedConst.DefaultPlayerBoundingRadius);
            pos.Orientation = target.Location.Orientation;
        }

        var map = target.Location.Map;

        var rotation = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(pos.Orientation, 0.0f, 0.0f));
        var go = _gameObjectFactory.CreateGameObject((uint)EffectInfo.MiscValue, map, pos, rotation, 255, GameObjectState.Ready);

        if (go == null)
            return;

        _phasingHandler.InheritPhaseShift(go, Caster);

        var duration = SpellInfo.CalcDuration(Caster);

        go.SetRespawnTime(duration > 0 ? duration / Time.IN_MILLISECONDS : 0);
        go.SpellId = SpellInfo.Id;

        ExecuteLogEffectSummonObject(EffectInfo.Effect, go);

        // Wild object not have owner and check clickable by players
        map.AddToMap(go);

        if (go.GoType == GameObjectTypes.FlagDrop)
        {
            var player = Caster.AsPlayer;

            var bg = player?.Battleground;

            bg?.SetDroppedFlagGUID(go.GUID, bg.GetPlayerTeam(player.GUID) == TeamFaction.Alliance ? TeamIds.Horde : TeamIds.Alliance);
        }

        var linkedTrap = go.LinkedTrap;

        if (linkedTrap == null)
            return;

        _phasingHandler.InheritPhaseShift(linkedTrap, Caster);
        linkedTrap.SetRespawnTime(duration > 0 ? duration / Time.IN_MILLISECONDS : 0);
        linkedTrap.SpellId = SpellInfo.Id;

        ExecuteLogEffectSummonObject(EffectInfo.Effect, linkedTrap);
    }

    [SpellEffectHandler(SpellEffectName.SummonPersonalGameobject)]
    private void EffectSummonPersonalGameObject()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        var goId = (uint)EffectInfo.MiscValue;

        if (goId == 0)
            return;

        Position pos = new();

        if (Targets.HasDst)
            pos = DestTarget.Copy();
        else
        {
            Caster.Location.GetClosePoint(pos, SharedConst.DefaultPlayerBoundingRadius);
            pos.Orientation = Caster.Location.Orientation;
        }

        var map = Caster.Location.Map;
        var rot = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(pos.Orientation, 0.0f, 0.0f));
        var go = _gameObjectFactory.CreateGameObject(goId, map, pos, rot, 255, GameObjectState.Ready);

        if (go == null)
        {
            Log.Logger.Warning($"SpellEffect Failed to summon personal gameobject. SpellId {SpellInfo.Id}, effect {EffectInfo.EffectIndex}");

            return;
        }

        _phasingHandler.InheritPhaseShift(go, Caster);

        var duration = SpellInfo.CalcDuration(Caster);

        go.SetRespawnTime(duration > 0 ? duration / Time.IN_MILLISECONDS : 0);
        go.SpellId = SpellInfo.Id;
        go.PrivateObjectOwner = Caster.GUID;

        ExecuteLogEffectSummonObject(EffectInfo.Effect, go);

        map.AddToMap(go);

        var linkedTrap = go.LinkedTrap;

        if (linkedTrap == null)
            return;

        _phasingHandler.InheritPhaseShift(linkedTrap, Caster);

        linkedTrap.SetRespawnTime(duration > 0 ? duration / Time.IN_MILLISECONDS : 0);
        linkedTrap.SpellId = SpellInfo.Id;

        ExecuteLogEffectSummonObject(EffectInfo.Effect, linkedTrap);
    }

    [SpellEffectHandler(SpellEffectName.SummonPet)]
    private void EffectSummonPet()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        Player owner = null;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster != null)
        {
            owner = unitCaster.AsPlayer;

            if (owner == null && unitCaster.IsTotem)
                owner = unitCaster.CharmerOrOwnerPlayerOrPlayerItself;
        }

        var petentry = (uint)EffectInfo.MiscValue;

        if (owner == null)
        {
            if (_cliDb.SummonPropertiesStorage.TryGetValue(67, out var properties))
                SummonGuardian(EffectInfo, petentry, properties, 1, ObjectGuid.Empty);

            return;
        }

        var oldSummon = owner.CurrentPet;

        // if pet requested type already exist
        if (oldSummon != null)
        {
            if (petentry == 0 || oldSummon.Entry == petentry)
            {
                // pet in corpse state can't be summoned
                if (oldSummon.IsDead)
                    return;

                var newPos = new Position();
                owner.Location.GetClosePoint(newPos, oldSummon.CombatReach);
                newPos.Orientation = oldSummon.Location.Orientation;

                oldSummon.NearTeleportTo(newPos);

                if (owner.IsTypeId(TypeId.Player) && oldSummon.IsControlled)
                    owner.AsPlayer.PetSpellInitialize();

                return;
            }

            if (owner.IsTypeId(TypeId.Player))
                owner.AsPlayer.RemovePet(oldSummon, PetSaveMode.NotInSlot);
            else
                return;
        }

        PetSaveMode? petSlot = null;

        if (petentry == 0)
            petSlot = (PetSaveMode)Damage;

        var combatPos = new Position();
        owner.Location.GetClosePoint(combatPos, owner.CombatReach);
        combatPos.Orientation = owner.Location.Orientation;
        var pet = owner.SummonPet(petentry, petSlot, combatPos, 0, out var isNew);

        if (pet == null)
            return;

        if (isNew)
        {
            if (Caster.IsCreature)
                pet.ReactState = Caster.AsCreature.IsTotem ? ReactStates.Aggressive : ReactStates.Defensive;

            pet.SetCreatedBySpell(SpellInfo.Id);

            // generate new name for summon pet
            var newName = _gameObjectManager.GeneratePetName(petentry);

            if (!string.IsNullOrEmpty(newName))
                pet.SetName(newName);
        }

        ExecuteLogEffectSummonObject(EffectInfo.Effect, pet);
    }

    [SpellEffectHandler(SpellEffectName.SummonPlayer)]
    private void EffectSummonPlayer()
    {
        // workaround - this effect should not use target map
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster == null)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        UnitTarget.AsPlayer.SendSummonRequestFrom(unitCaster);
    }

    [SpellEffectHandler(SpellEffectName.SummonRafFriend)]
    private void EffectSummonRaFFriend()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (!Caster.IsTypeId(TypeId.Player) || UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        Caster.SpellFactory.CastSpell(UnitTarget, EffectInfo.TriggerSpell, new CastSpellExtraArgs(this));
    }

    [SpellEffectHandler(SpellEffectName.Summon)]
    private void EffectSummonType()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        var entry = (uint)EffectInfo.MiscValue;

        if (entry == 0)
            return;

        if (!_cliDb.SummonPropertiesStorage.TryGetValue((uint)EffectInfo.MiscValueB, out var properties))
        {
            Log.Logger.Error("EffectSummonType: Unhandled summon type {0}", EffectInfo.MiscValueB);

            return;
        }

        var caster = Caster;

        if (OriginalCaster != null)
            caster = OriginalCaster;

        var privateObjectOwner = caster.GUID;

        if (!properties.GetFlags().HasAnyFlag(SummonPropertiesFlags.OnlyVisibleToSummoner | SummonPropertiesFlags.OnlyVisibleToSummonerGroup))
            privateObjectOwner = ObjectGuid.Empty;

        if (caster.IsPrivateObject)
            privateObjectOwner = caster.PrivateObjectOwner;

        if (properties.GetFlags().HasFlag(SummonPropertiesFlags.OnlyVisibleToSummonerGroup))
            if (caster.IsPlayer && OriginalCaster?.AsPlayer.Group != null)
                privateObjectOwner = caster.AsPlayer.Group.GUID;

        var duration = SpellInfo.CalcDuration(caster);

        if (SpellValue.SummonDuration.HasValue)
            duration = (int)SpellValue.SummonDuration.Value;

        var unitCaster = UnitCasterForEffectHandlers;

        TempSummon summon = null;

        // determine how many units should be summoned

        // some spells need to summon many units, for those spells number of summons is stored in effect value
        // however so far noone found a generic check to find all of those (there's no related data in summonproperties.dbc
        // and in spell attributes, possibly we need to add a table for those)
        // so here's a list of MiscValueB values, which is currently most generic check
        uint numSummons = EffectInfo.MiscValueB switch
        {
            64   => (uint)(Damage > 0 ? Damage : 1),
            61   => (uint)(Damage > 0 ? Damage : 1),
            1101 => (uint)(Damage > 0 ? Damage : 1),
            66   => (uint)(Damage > 0 ? Damage : 1),
            648  => (uint)(Damage > 0 ? Damage : 1),
            2301 => (uint)(Damage > 0 ? Damage : 1),
            1061 => (uint)(Damage > 0 ? Damage : 1),
            1261 => (uint)(Damage > 0 ? Damage : 1),
            629  => (uint)(Damage > 0 ? Damage : 1),
            181  => (uint)(Damage > 0 ? Damage : 1),
            715  => (uint)(Damage > 0 ? Damage : 1),
            1562 => (uint)(Damage > 0 ? Damage : 1),
            833  => (uint)(Damage > 0 ? Damage : 1),
            1161 => (uint)(Damage > 0 ? Damage : 1),
            713  => (uint)(Damage > 0 ? Damage : 1),
            _    => 1
        };

        switch (properties.Control)
        {
            case SummonCategory.Wild:
            case SummonCategory.Ally:
            case SummonCategory.Unk:
                if (properties.GetFlags().HasFlag(SummonPropertiesFlags.JoinSummonerSpawnGroup))
                {
                    SummonGuardian(EffectInfo, entry, properties, numSummons, privateObjectOwner);

                    break;
                }

                switch (properties.Title)
                {
                    case SummonTitle.Pet:
                    case SummonTitle.Guardian:
                    case SummonTitle.Runeblade:
                    case SummonTitle.Minion:
                        SummonGuardian(EffectInfo, entry, properties, numSummons, privateObjectOwner);

                        break;
                    // Summons a vehicle, but doesn't force anyone to enter it (see SUMMON_CATEGORY_VEHICLE)
                    case SummonTitle.Vehicle:
                    case SummonTitle.Mount:
                    {
                        if (unitCaster == null)
                            return;

                        summon = unitCaster.Location.Map.SummonCreature(entry, DestTarget, properties, (uint)duration, unitCaster, SpellInfo.Id);

                        break;
                    }
                    case SummonTitle.LightWell:
                    case SummonTitle.Totem:
                    {
                        if (unitCaster == null)
                            return;

                        summon = unitCaster.Location.Map.SummonCreature(entry, DestTarget, properties, (uint)duration, unitCaster, SpellInfo.Id, 0, privateObjectOwner);

                        if (summon is not { IsTotem: true })
                            return;

                        if (Damage != 0) // if not spell info, DB values used
                        {
                            summon.SetMaxHealth((uint)Damage);
                            summon.SetHealth((uint)Damage);
                        }

                        break;
                    }
                    case SummonTitle.Companion:
                    {
                        if (unitCaster == null)
                            return;

                        summon = unitCaster.Location.Map.SummonCreature(entry, DestTarget, properties, (uint)duration, unitCaster, SpellInfo.Id, 0, privateObjectOwner);

                        if (summon == null || !summon.HasUnitTypeMask(UnitTypeMask.Minion))
                            return;

                        summon.SetImmuneToAll(true);

                        break;
                    }
                    default:
                    {
                        var radius = EffectInfo.CalcRadius();

                        var summonType = duration == 0 ? TempSummonType.DeadDespawn : TempSummonType.TimedDespawn;

                        for (uint count = 0; count < numSummons; ++count)
                        {
                            Position pos;

                            if (count == 0)
                                pos = DestTarget;
                            else
                                // randomize position for multiple summons
                                pos = caster.Location.GetRandomPoint(DestTarget, radius);

                            summon = caster.Location.Map.SummonCreature(entry, pos, properties, (uint)duration, unitCaster, SpellInfo.Id, 0, privateObjectOwner);

                            if (summon == null)
                                continue;

                            summon.SetTempSummonType(summonType);

                            if (properties.Control == SummonCategory.Ally)
                                summon.SetOwnerGUID(caster.GUID);

                            ExecuteLogEffectSummonObject(EffectInfo.Effect, summon);
                        }

                        return;
                    }
                } //switch

                break;

            case SummonCategory.Pet:
                SummonGuardian(EffectInfo, entry, properties, numSummons, privateObjectOwner);

                break;

            case SummonCategory.Puppet:
            {
                if (unitCaster == null)
                    return;

                summon = unitCaster.Location.Map.SummonCreature(entry, DestTarget, properties, (uint)duration, unitCaster, SpellInfo.Id, 0, privateObjectOwner);

                break;
            }
            case SummonCategory.Vehicle:
            {
                if (unitCaster == null)
                    return;

                // Summoning spells (usually triggered by npc_spellclick) that spawn a vehicle and that cause the clicker
                // to cast a ride vehicle spell on the summoned unit.
                summon = unitCaster.Location.Map.SummonCreature(entry, DestTarget, properties, (uint)duration, unitCaster, SpellInfo.Id);

                if (summon is not { IsVehicle: true })
                    return;

                // The spell that this effect will trigger. It has SPELL_AURA_CONTROL_VEHICLE
                uint spellId = SharedConst.VehicleSpellRideHardcoded;
                var basePoints = EffectInfo.CalcValue();

                if (basePoints > SharedConst.MaxVehicleSeats)
                {
                    var spellInfo = _spellManager.GetSpellInfo((uint)basePoints, CastDifficulty);

                    if (spellInfo != null && spellInfo.HasAura(AuraType.ControlVehicle))
                        spellId = spellInfo.Id;
                }

                CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                args.SetTriggeringSpell(this);

                // if we have small value, it indicates seat position
                if (basePoints is > 0 and < SharedConst.MaxVehicleSeats)
                    args.AddSpellMod(SpellValueMod.BasePoint0, basePoints);

                unitCaster.SpellFactory.CastSpell(summon, spellId, args);

                break;
            }
        }

        if (summon != null)
        {
            summon.SetCreatorGUID(caster.GUID);
            ExecuteLogEffectSummonObject(EffectInfo.Effect, summon);
        }
    }

    [SpellEffectHandler(SpellEffectName.Tamecreature)]
    private void EffectTameCreature()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster == null || !unitCaster.PetGUID.IsEmpty)
            return;

        if (UnitTarget == null)
            return;

        if (!UnitTarget.IsTypeId(TypeId.Unit))
            return;

        var creatureTarget = UnitTarget.AsCreature;

        if (creatureTarget.IsPet)
            return;

        if (unitCaster.Class != PlayerClass.Hunter)
            return;

        // cast finish successfully
        Finish();

        var pet = unitCaster.CreateTamedPetFrom(creatureTarget, SpellInfo.Id);

        if (pet == null) // in very specific state like near world end/etc.
            return;

        // "kill" original creature
        creatureTarget.DespawnOrUnsummon();

        var level = creatureTarget.GetLevelForTarget(Caster) < Caster.GetLevelForTarget(creatureTarget) - 5 ? Caster.GetLevelForTarget(creatureTarget) - 5 : creatureTarget.GetLevelForTarget(Caster);

        // prepare visual effect for levelup
        pet.SetLevel(level - 1);

        // add to world
        pet.Location.
            // add to world
            Map.AddToMap(pet.AsCreature);

        // visual effect for levelup
        pet.SetLevel(level);

        // caster have pet now
        unitCaster.SetMinion(pet, true);

        if (Caster.IsTypeId(TypeId.Player))
        {
            pet.SavePetToDB(PetSaveMode.AsCurrent);
            unitCaster.AsPlayer.PetSpellInitialize();
        }
    }

    [SpellEffectHandler(SpellEffectName.AttackMe)]
    private void EffectTaunt()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster == null)
            return;

        // this effect use before aura Taunt apply for prevent taunt already attacking target
        // for spell as marked "non effective at already attacking target"
        if (UnitTarget == null || UnitTarget.IsTotem)
        {
            SendCastResult(SpellCastResult.DontReport);

            return;
        }

        // Hand of Reckoning can hit some entities that can't have a threat list (including players' pets)
        if (SpellInfo.Id == 62124)
            if (!UnitTarget.IsPlayer && UnitTarget.Target != unitCaster.GUID)
                unitCaster.SpellFactory.CastSpell(UnitTarget, 67485, true);

        if (!UnitTarget.CanHaveThreatList)
        {
            SendCastResult(SpellCastResult.DontReport);

            return;
        }

        var mgr = UnitTarget.GetThreatManager();

        if (mgr.CurrentVictim == unitCaster)
        {
            SendCastResult(SpellCastResult.DontReport);

            return;
        }

        if (!mgr.IsThreatListEmpty())
            // Set threat equal to highest threat currently on target
            mgr.MatchUnitThreatToHighestThreat(unitCaster);
    }

    [SpellEffectHandler(SpellEffectName.TeleportToReturnPoint)]
    private void EffectTeleportToReturnPoint()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var player = UnitTarget.AsPlayer;

        var dest = player?.GetStoredAuraTeleportLocation((uint)EffectInfo.MiscValue);

        if (dest != null)
            player.TeleportTo(dest, UnitTarget == Caster ? TeleportToOptions.Spell | TeleportToOptions.NotLeaveCombat : 0);
    }

    [SpellEffectHandler(SpellEffectName.TeleportUnits)]
    private void EffectTeleportUnits()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || UnitTarget.IsInFlight)
            return;

        // If not exist data for dest location - return
        if (!Targets.HasDst)
        {
            Log.Logger.Error("Spell.EffectTeleportUnits - does not have a destination for spellId {0}.", SpellInfo.Id);

            return;
        }

        // Init dest coordinates
        WorldLocation targetDest = new(DestTarget);

        if (targetDest.MapId == 0xFFFFFFFF)
            targetDest.Map = UnitTarget.Location.Map;

        if (targetDest.Orientation == 0 && Targets.UnitTarget != null)
            targetDest.Orientation = Targets.UnitTarget.Location.Orientation;

        var player = UnitTarget.AsPlayer;

        if (player != null)
        {
            // Custom loading screen
            var customLoadingScreenId = (uint)EffectInfo.MiscValue;

            if (customLoadingScreenId != 0)
                player.SendPacket(new CustomLoadScreen(SpellInfo.Id, customLoadingScreenId));
        }

        if (targetDest.MapId == UnitTarget.Location.MapId)
            UnitTarget.NearTeleportTo(targetDest, UnitTarget == Caster);
        else if (player != null)
            player.TeleportTo(targetDest, UnitTarget == Caster ? TeleportToOptions.Spell : 0);
        else
            Log.Logger.Error("Spell.EffectTeleportUnits - spellId {0} attempted to teleport creature to a different map.", SpellInfo.Id);
    }

    [SpellEffectHandler(SpellEffectName.TeleportWithSpellVisualKitLoadingScreen)]
    private void EffectTeleportUnitsWithVisualLoadingScreen()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null)
            return;

        // If not exist data for dest location - return
        if (!Targets.HasDst)
        {
            Log.Logger.Error($"Spell::EffectTeleportUnitsWithVisualLoadingScreen - does not have a destination for spellId {SpellInfo.Id}.");

            return;
        }

        // Init dest coordinates
        WorldLocation targetDest = new(DestTarget);

        if (targetDest.MapId == 0xFFFFFFFF)
            targetDest.Map = UnitTarget.Location.Map;

        if (targetDest.Orientation == 0 && Targets.UnitTarget != null)
            targetDest.Orientation = Targets.UnitTarget.Location.Orientation;

        if (EffectInfo.MiscValueB != 0)
        {
            var playerTarget = UnitTarget.AsPlayer;

            playerTarget?.SendPacket(new SpellVisualLoadScreen(EffectInfo.MiscValueB, EffectInfo.MiscValue));
        }

        UnitTarget.Events.AddEventAtOffset(new DelayedSpellTeleportEvent(UnitTarget, targetDest, UnitTarget == Caster ? TeleportToOptions.Spell : 0, SpellInfo.Id), TimeSpan.FromMilliseconds(EffectInfo.MiscValue));
    }

    [SpellEffectHandler(SpellEffectName.TeleportUnitsFaceCaster)]
    private void EffectTeleUnitsFaceCaster()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null)
            return;

        if (UnitTarget.IsInFlight)
            return;

        if (Targets.HasDst)
            UnitTarget.NearTeleportTo(DestTarget.X, DestTarget.Y, DestTarget.Z, DestTarget.GetAbsoluteAngle(Caster.Location), UnitTarget == Caster);
    }

    [SpellEffectHandler(SpellEffectName.Threat)]
    private void EffectThreat()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster is not { IsAlive: true })
            return;

        if (UnitTarget is not { CanHaveThreatList: true })
            return;

        UnitTarget.GetThreatManager().AddThreat(unitCaster, Damage, SpellInfo, true);
    }

    [SpellEffectHandler(SpellEffectName.TitanGrip)]
    private void EffectTitanGrip()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        if (Caster.IsTypeId(TypeId.Player))
            Caster.AsPlayer.SetCanTitanGrip(true, (uint)EffectInfo.MiscValue);
    }

    [SpellEffectHandler(SpellEffectName.TradeSkill)]
    private void EffectTradeSkill()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        if (!Caster.IsTypeId(TypeId.Player))
            return;
        // uint skillid =  GetEffect(i].MiscValue;
        // ushort skillmax = unitTarget.ToPlayer().(skillid);
        // m_caster.ToPlayer().SetSkill(skillid, skillval?skillval:1, skillmax+75);
    }

    [SpellEffectHandler(SpellEffectName.TransDoor)]
    private void EffectTransmitted()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster == null)
            return;

        var nameID = (uint)EffectInfo.MiscValue;

        var overrideSummonedGameObjects = unitCaster.GetAuraEffectsByType(AuraType.OverrideSummonedObject);

        foreach (var aurEff in overrideSummonedGameObjects)
            if (aurEff.MiscValue == nameID)
            {
                nameID = (uint)aurEff.MiscValueB;

                break;
            }

        var goinfo = _gameObjectManager.GetGameObjectTemplate(nameID);

        if (goinfo == null)
        {
            Log.Logger.Error("Gameobject (Entry: {0}) not exist and not created at spell (ID: {1}) cast", nameID, SpellInfo.Id);

            return;
        }

        var pos = new Position();

        if (Targets.HasDst)
            pos = DestTarget.Copy();
        //FIXME: this can be better check for most objects but still hack
        else if (EffectInfo.HasRadius && SpellInfo.Speed == 0)
        {
            var dis = EffectInfo.CalcRadius(unitCaster);
            unitCaster.Location.GetClosePoint(pos, SharedConst.DefaultPlayerBoundingRadius, dis);
            pos.Orientation = unitCaster.Location.Orientation;
        }
        else
        {
            //GO is always friendly to it's creator, get range for friends
            var minDis = SpellInfo.GetMinRange(true);
            var maxDis = SpellInfo.GetMaxRange(true);
            var dis = (float)RandomHelper.NextDouble() * (maxDis - minDis) + minDis;

            unitCaster.Location.GetClosePoint(pos, SharedConst.DefaultPlayerBoundingRadius, dis);
            pos.Orientation = unitCaster.Location.Orientation;
        }

        var cMap = unitCaster.Location.Map;

        // if gameobject is summoning object, it should be spawned right on caster's position
        if (goinfo.type == GameObjectTypes.Ritual)
            pos.Relocate(unitCaster.Location);

        var rotation = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(pos.Orientation, 0.0f, 0.0f));

        var go = _gameObjectFactory.CreateGameObject(nameID, cMap, pos, rotation, 255, GameObjectState.Ready);

        if (go == null)
            return;

        _phasingHandler.InheritPhaseShift(go, Caster);

        var duration = SpellInfo.CalcDuration(Caster);

        switch (goinfo.type)
        {
            case GameObjectTypes.FishingNode:
            {
                go.Faction = unitCaster.Faction;
                var bobberGuid = go.GUID;
                // client requires fishing bobber guid in channel object slot 0 to be usable
                unitCaster.SetChannelObject(0, bobberGuid);
                unitCaster.AddGameObject(go); // will removed at spell cancel

                // end time of range when possible catch fish (FISHING_BOBBER_READY_TIME..GetDuration(m_spellInfo))
                // start time == fish-FISHING_BOBBER_READY_TIME (0..GetDuration(m_spellInfo)-FISHING_BOBBER_READY_TIME)

                var lastSec = RandomHelper.IRand(0, 2) switch
                {
                    0 => 3,
                    1 => 7,
                    2 => 13,
                    _ => 0
                };

                // Duration of the fishing bobber can't be higher than the Fishing channeling duration
                duration = Math.Min(duration, duration - lastSec * Time.IN_MILLISECONDS + 5 * Time.IN_MILLISECONDS);

                break;
            }
            case GameObjectTypes.Ritual:
            {
                if (unitCaster.IsPlayer)
                {
                    go.AddUniqueUse(unitCaster.AsPlayer);
                    unitCaster.AddGameObject(go); // will be removed at spell cancel
                }

                break;
            }
            case GameObjectTypes.DuelArbiter: // 52991
                unitCaster.AddGameObject(go);

                break;
        }

        go.SetRespawnTime(duration > 0 ? duration / Time.IN_MILLISECONDS : 0);
        go.SetOwnerGUID(unitCaster.GUID);
        go.SpellId = SpellInfo.Id;

        ExecuteLogEffectSummonObject(EffectInfo.Effect, go);

        Log.Logger.Debug("AddObject at SpellEfects.cpp EffectTransmitted");

        cMap.AddToMap(go);
        var linkedTrap = go.LinkedTrap;

        if (linkedTrap != null)
        {
            _phasingHandler.InheritPhaseShift(linkedTrap, Caster);
            linkedTrap.SetRespawnTime(duration > 0 ? duration / Time.IN_MILLISECONDS : 0);
            linkedTrap.SpellId = SpellInfo.Id;
            linkedTrap.SetOwnerGUID(unitCaster.GUID);

            ExecuteLogEffectSummonObject(EffectInfo.Effect, linkedTrap);
        }
    }

    [SpellEffectHandler(SpellEffectName.TriggerMissile)]
    [SpellEffectHandler(SpellEffectName.TriggerMissileSpellWithValue)]
    private void EffectTriggerMissileSpell()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget && _effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        var triggeredSpellID = EffectInfo.TriggerSpell;

        if (triggeredSpellID == 0)
        {
            Log.Logger.Warning($"Spell::EffectTriggerMissileSpell: Spell {SpellInfo.Id} [EffectIndex: {EffectInfo.EffectIndex}] does not have triggered spell.");

            return;
        }

        // normal case
        var spellInfo = _spellManager.GetSpellInfo(triggeredSpellID, CastDifficulty);

        if (spellInfo == null)
        {
            Log.Logger.Debug("Spell.EffectTriggerMissileSpell spell {0} tried to trigger unknown spell {1}", SpellInfo.Id, triggeredSpellID);

            return;
        }

        SpellCastTargets targets = new();

        if (_effectHandleMode == SpellEffectHandleMode.HitTarget)
        {
            if (!spellInfo.NeedsToBeTriggeredByCaster(SpellInfo))
                return;

            targets.UnitTarget = UnitTarget;
        }
        else //if (effectHandleMode == SpellEffectHandleMode.Hit)
        {
            if (spellInfo.NeedsToBeTriggeredByCaster(SpellInfo) && EffectInfo.ProvidedTargetMask.HasAnyFlag(SpellCastTargetFlags.UnitMask))
                return;

            if (spellInfo.ExplicitTargetMask.HasAnyFlag(SpellCastTargetFlags.DestLocation))
                targets.SetDst(Targets);

            var unit = Caster.AsUnit;

            if (unit != null)
                targets.UnitTarget = unit;
            else
            {
                var go = Caster.AsGameObject;

                if (go != null)
                    targets.GOTarget = go;
            }
        }

        CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
        args.SetOriginalCaster(_originalCasterGuid);
        args.SetTriggeringSpell(this);

        // set basepoints for trigger with value effect
        if (EffectInfo.Effect == SpellEffectName.TriggerMissileSpellWithValue)
            foreach (var eff in spellInfo.Effects)
                args.AddSpellMod(SpellValueMod.BasePoint0 + eff.EffectIndex, Damage);

        // original caster guid only for GO cast
        Caster.SpellFactory.CastSpell(targets, spellInfo.Id, args);
    }

    [SpellEffectHandler(SpellEffectName.TriggerSpell2)]
    private void EffectTriggerRitualOfSummoning()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        var triggeredSpellID = EffectInfo.TriggerSpell;

        if (triggeredSpellID == 0)
        {
            Log.Logger.Warning($"Spell::EffectTriggerRitualOfSummoning: Spell {SpellInfo.Id} [EffectIndex: {EffectInfo.EffectIndex}] does not have triggered spell.");

            return;
        }

        var spellInfo = _spellManager.GetSpellInfo(triggeredSpellID, CastDifficulty);

        if (spellInfo == null)
        {
            Log.Logger.Error($"EffectTriggerRitualOfSummoning of spell {SpellInfo.Id}: triggering unknown spell id {triggeredSpellID}");

            return;
        }

        Finish();

        Caster.SpellFactory.CastSpell((Unit)null, spellInfo.Id, new CastSpellExtraArgs().SetTriggeringSpell(this));
    }

    [SpellEffectHandler(SpellEffectName.TriggerSpell)]
    [SpellEffectHandler(SpellEffectName.TriggerSpellWithValue)]
    private void EffectTriggerSpell()
    {
        if (_effectHandleMode != SpellEffectHandleMode.LaunchTarget && _effectHandleMode != SpellEffectHandleMode.Launch)
            return;

        var triggeredSpellID = EffectInfo.TriggerSpell;

        // @todo move those to spell scripts
        if (EffectInfo.Effect == SpellEffectName.TriggerSpell && _effectHandleMode == SpellEffectHandleMode.LaunchTarget)
            // special cases
            switch (triggeredSpellID)
            {
                // Demonic Empowerment -- succubus
                case 54437:
                {
                    UnitTarget.RemoveMovementImpairingAuras(true);
                    UnitTarget.RemoveAurasByType(AuraType.ModStalked);
                    UnitTarget.RemoveAurasByType(AuraType.ModStun);

                    // Cast Lesser Invisibility
                    UnitTarget.SpellFactory.CastSpell(UnitTarget, 7870, new CastSpellExtraArgs(this));

                    return;
                }
                // Brittle Armor - (need add max stack of 24575 Brittle Armor)
                case 29284:
                {
                    // Brittle Armor
                    var spell = _spellManager.GetSpellInfo(24575, CastDifficulty);

                    if (spell == null)
                        return;

                    for (uint j = 0; j < spell.StackAmount; ++j)
                        Caster.SpellFactory.CastSpell(UnitTarget, spell.Id, new CastSpellExtraArgs(this));

                    return;
                }
                // Mercurial Shield - (need add max stack of 26464 Mercurial Shield)
                case 29286:
                {
                    // Mercurial Shield
                    var spell = _spellManager.GetSpellInfo(26464, CastDifficulty);

                    if (spell == null)
                        return;

                    for (uint j = 0; j < spell.StackAmount; ++j)
                        Caster.SpellFactory.CastSpell(UnitTarget, spell.Id, new CastSpellExtraArgs(this));

                    return;
                }
            }

        if (triggeredSpellID == 0)
        {
            Log.Logger.Warning($"Spell::EffectTriggerSpell: Spell {SpellInfo.Id} [EffectIndex: {EffectInfo.EffectIndex}] does not have triggered spell.");

            return;
        }

        // normal case
        var spellInfo = _spellManager.GetSpellInfo(triggeredSpellID, CastDifficulty);

        if (spellInfo == null)
        {
            Log.Logger.Debug("Spell.EffectTriggerSpell spell {0} tried to trigger unknown spell {1}", SpellInfo.Id, triggeredSpellID);

            return;
        }

        SpellCastTargets targets = new();

        if (_effectHandleMode == SpellEffectHandleMode.LaunchTarget)
        {
            if (!spellInfo.NeedsToBeTriggeredByCaster(SpellInfo))
                return;

            targets.UnitTarget = UnitTarget;
        }
        else //if (effectHandleMode == SpellEffectHandleMode.Launch)
        {
            if (spellInfo.NeedsToBeTriggeredByCaster(SpellInfo) && EffectInfo.ProvidedTargetMask.HasAnyFlag(SpellCastTargetFlags.UnitMask))
                return;

            if (spellInfo.ExplicitTargetMask.HasAnyFlag(SpellCastTargetFlags.DestLocation))
                targets.SetDst(Targets);

            var target = Targets.UnitTarget;

            if (target != null)
                targets.UnitTarget = target;
            else
            {
                var unit = Caster.AsUnit;

                if (unit != null)
                    targets.UnitTarget = unit;
                else
                {
                    var go = Caster.AsGameObject;

                    if (go != null)
                        targets.GOTarget = go;
                }
            }
        }

        var delay = TimeSpan.Zero;

        if (EffectInfo.Effect == SpellEffectName.TriggerSpell)
            delay = TimeSpan.FromMilliseconds(EffectInfo.MiscValue);

        var caster = Caster;
        var originalCaster = _originalCasterGuid;
        var castItemGuid = CastItemGuid;
        var originalCastId = CastId;
        var triggerSpell = EffectInfo.TriggerSpell;
        var effect = EffectInfo.Effect;
        var value = Damage;
        var itemLevel = CastItemLevel;

        Caster.Events.AddEventAtOffset(() =>
                                       {
                                           targets.Update(caster); // refresh pointers stored in targets

                                           // original caster guid only for GO cast
                                           CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                                           args.SetOriginalCaster(originalCaster);
                                           args.OriginalCastId = originalCastId;
                                           args.OriginalCastItemLevel = itemLevel;

                                           var triggerSpellInfo = _spellManager.GetSpellInfo(triggerSpell, caster.Location.Map.DifficultyID);

                                           if (!castItemGuid.IsEmpty && triggerSpellInfo.HasAttribute(SpellAttr2.RetainItemCast))
                                           {
                                               var triggeringAuraCaster = caster?.AsPlayer;

                                               if (triggeringAuraCaster != null)
                                                   args.CastItem = triggeringAuraCaster.GetItemByGuid(castItemGuid);
                                           }

                                           // set basepoints for trigger with value effect
                                           if (effect == SpellEffectName.TriggerSpellWithValue)
                                               foreach (var eff in triggerSpellInfo.Effects)
                                                   args.AddSpellMod(SpellValueMod.BasePoint0 + eff.EffectIndex, value);

                                           caster.SpellFactory.CastSpell(targets, triggerSpell, args);
                                       },
                                       delay);
    }

    [SpellEffectHandler(SpellEffectName.UncageBattlepet)]
    private void EffectUncageBattlePet()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        if (CastItem == null || Caster == null || !Caster.IsTypeId(TypeId.Player))
            return;

        var speciesId = CastItem.GetModifier(ItemModifier.BattlePetSpeciesId);
        var breed = (ushort)(CastItem.GetModifier(ItemModifier.BattlePetBreedData) & 0xFFFFFF);
        var quality = (BattlePetBreedQuality)((CastItem.GetModifier(ItemModifier.BattlePetBreedData) >> 24) & 0xFF);
        var level = (ushort)CastItem.GetModifier(ItemModifier.BattlePetLevel);
        var displayId = CastItem.GetModifier(ItemModifier.BattlePetDisplayId);

        if (!_cliDb.BattlePetSpeciesStorage.TryGetValue(speciesId, out var speciesEntry))
            return;

        var player = Caster.AsPlayer;
        var battlePetMgr = player.Session.BattlePetMgr;

        if (battlePetMgr == null)
            return;

        if (battlePetMgr.GetMaxPetLevel() < level)
        {
            battlePetMgr.SendError(BattlePetError.TooHighLevelToUncage, speciesEntry.CreatureID);
            SendCastResult(SpellCastResult.CantAddBattlePet);

            return;
        }

        if (battlePetMgr.HasMaxPetCount(speciesEntry, player.GUID))
        {
            battlePetMgr.SendError(BattlePetError.CantHaveMorePetsOfThatType, speciesEntry.CreatureID);
            SendCastResult(SpellCastResult.CantAddBattlePet);

            return;
        }

        battlePetMgr.AddPet(speciesId, displayId, breed, quality, level);

        player.WorldObjectCombat.SendPlaySpellVisual(player, SharedConst.SpellVisualUncagePet, 0, 0, 0.0f);

        player.DestroyItem(CastItem.BagSlot, CastItem.Slot, true);
        CastItem = null;
    }

    [SpellEffectHandler(SpellEffectName.UnlearnSpecialization)]
    private void EffectUnlearnSpecialization()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        var player = UnitTarget.AsPlayer;
        var spellToUnlearn = EffectInfo.TriggerSpell;

        player.RemoveSpell(spellToUnlearn);

        Log.Logger.Debug("Spell: Player {0} has unlearned spell {1} from NpcGUID: {2}", player.GUID.ToString(), spellToUnlearn, Caster.GUID.ToString());
    }

    [SpellEffectHandler(SpellEffectName.UnlockGuildVaultTab)]
    private void EffectUnlockGuildVaultTab()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        // Safety checks done in Spell.CheckCast
        var caster = Caster.AsPlayer;
        var guild = caster.Guild;

        guild?.HandleBuyBankTab(caster.Session, (byte)(Damage - 1)); // Bank tabs start at zero internally
    }

    [SpellEffectHandler(SpellEffectName.UntrainTalents)]
    private void EffectUntrainTalents()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || Caster.IsTypeId(TypeId.Player))
            return;

        var guid = Caster.GUID;

        if (!guid.IsEmpty) // the trainer is the caster
            UnitTarget.AsPlayer.SendRespecWipeConfirm(guid, UnitTarget.AsPlayer.GetNextResetTalentsCost(), SpecResetType.Talents);
    }

    [SpellEffectHandler(SpellEffectName.None)]
    [SpellEffectHandler(SpellEffectName.Portal)]
    [SpellEffectHandler(SpellEffectName.BindSight)]
    [SpellEffectHandler(SpellEffectName.CallPet)]
    [SpellEffectHandler(SpellEffectName.PortalTeleport)]
    [SpellEffectHandler(SpellEffectName.Dodge)]
    [SpellEffectHandler(SpellEffectName.Evade)]
    [SpellEffectHandler(SpellEffectName.Weapon)]
    [SpellEffectHandler(SpellEffectName.Defense)]
    [SpellEffectHandler(SpellEffectName.SpellDefense)]
    [SpellEffectHandler(SpellEffectName.Language)]
    [SpellEffectHandler(SpellEffectName.Spawn)]
    [SpellEffectHandler(SpellEffectName.Stealth)]
    [SpellEffectHandler(SpellEffectName.Detect)]
    [SpellEffectHandler(SpellEffectName.ForceCriticalHit)]
    [SpellEffectHandler(SpellEffectName.Attack)]
    [SpellEffectHandler(SpellEffectName.ThreatAll)]
    [SpellEffectHandler(SpellEffectName.Effect112)]
    [SpellEffectHandler(SpellEffectName.TeleportGraveyard)]
    [SpellEffectHandler(SpellEffectName.Effect122)]
    [SpellEffectHandler(SpellEffectName.Effect175)]
    [SpellEffectHandler(SpellEffectName.Effect178)]
    private void EffectUnused() { }

    [SpellEffectHandler(SpellEffectName.UpdatePlayerPhase)]
    private void EffectUpdatePlayerPhase()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        _phasingHandler.OnConditionChange(UnitTarget);
    }

    [SpellEffectHandler(SpellEffectName.UpdateZoneAurasPhases)]
    private void EffectUpdateZoneAurasAndPhases()
    {
        if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
            return;

        if (UnitTarget == null || !UnitTarget.IsTypeId(TypeId.Player))
            return;

        UnitTarget.AsPlayer.UpdateAreaDependentAuras(UnitTarget.Location.Area);
    }

    [SpellEffectHandler(SpellEffectName.UpgradeHeirloom)]
    private void EffectUpgradeHeirloom()
    {
        if (_effectHandleMode != SpellEffectHandleMode.Hit)
            return;

        var player = Caster.AsPlayer;

        if (player == null)
            return;

        var collectionMgr = player.Session.CollectionMgr;

        collectionMgr?.UpgradeHeirloom(SpellMisc.Data0, CastItemEntry);
    }

    [SpellEffectHandler(SpellEffectName.WeaponDamageNoSchool)]
    [SpellEffectHandler(SpellEffectName.WeaponPercentDamage)]
    [SpellEffectHandler(SpellEffectName.WeaponDamage)]
    [SpellEffectHandler(SpellEffectName.NormalizedWeaponDmg)]
    private void EffectWeaponDmg()
    {
        if (_effectHandleMode != SpellEffectHandleMode.LaunchTarget)
            return;

        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster == null)
            return;

        if (UnitTarget is not { IsAlive: true })
            return;

        // multiple weapon dmg effect workaround
        // execute only the last weapon damage
        // and handle all effects at once
        for (var j = EffectInfo.EffectIndex + 1; j < SpellInfo.Effects.Count; ++j)
            switch (SpellInfo.GetEffect(j).Effect)
            {
                case SpellEffectName.WeaponDamage:
                case SpellEffectName.WeaponDamageNoSchool:
                case SpellEffectName.NormalizedWeaponDmg:
                case SpellEffectName.WeaponPercentDamage:
                    return; // we must calculate only at last weapon effect
            }

        // some spell specific modifiers
        double totalDamagePercentMod = 1.0f; // applied to final bonus+weapon damage
        double fixedBonus = 0;
        double spellBonus = 0; // bonus specific for spell

        switch (SpellInfo.SpellFamilyName)
        {
            case SpellFamilyNames.Shaman:
            {
                // Skyshatter Harness item set bonus
                // Stormstrike
                var aurEff = unitCaster.IsScriptOverriden(SpellInfo, 5634);

                if (aurEff != null)
                    unitCaster.SpellFactory.CastSpell((WorldObject)null, 38430, new CastSpellExtraArgs(aurEff));

                break;
            }
        }

        var normalized = false;
        double weaponDamagePercentMod = 1.0f;

        foreach (var spellEffectInfo in SpellInfo.Effects)
            switch (spellEffectInfo.Effect)
            {
                case SpellEffectName.WeaponDamage:
                case SpellEffectName.WeaponDamageNoSchool:
                    fixedBonus += CalculateDamage(spellEffectInfo, UnitTarget);

                    break;

                case SpellEffectName.NormalizedWeaponDmg:
                    fixedBonus += CalculateDamage(spellEffectInfo, UnitTarget);
                    normalized = true;

                    break;

                case SpellEffectName.WeaponPercentDamage:
                    MathFunctions.ApplyPct(ref weaponDamagePercentMod, CalculateDamage(spellEffectInfo, UnitTarget));

                    break;
                // not weapon damage effect, just skip
            }

        // if (addPctMods) { percent mods are added in Unit::CalculateDamage } else { percent mods are added in Unit::MeleeDamageBonusDone }
        // this distinction is neccessary to properly inform the client about his autoattack damage values from Script_UnitDamage
        var addPctMods = !SpellInfo.HasAttribute(SpellAttr6.IgnoreCasterDamageModifiers) && SpellSchoolMask.HasAnyFlag(SpellSchoolMask.Normal);

        if (addPctMods)
        {
            var unitMod = AttackType switch
            {
                WeaponAttackType.BaseAttack   => UnitMods.DamageMainHand,
                WeaponAttackType.OffAttack    => UnitMods.DamageOffHand,
                WeaponAttackType.RangedAttack => UnitMods.DamageRanged,
                _                             => UnitMods.DamageMainHand
            };

            var weaponTotalPct = unitCaster.GetPctModifierValue(unitMod, UnitModifierPctType.Total);

            if (fixedBonus != 0)
                fixedBonus = fixedBonus * weaponTotalPct;

            if (spellBonus != 0)
                spellBonus = spellBonus * weaponTotalPct;
        }

        var weaponDamage = unitCaster.CalculateDamage(AttackType, normalized, addPctMods);

        // Sequence is important
        foreach (var spellEffectInfo in SpellInfo.Effects)
            // We assume that a spell have at most one fixed_bonus
            // and at most one weaponDamagePercentMod
            switch (spellEffectInfo.Effect)
            {
                case SpellEffectName.WeaponDamage:
                case SpellEffectName.WeaponDamageNoSchool:
                case SpellEffectName.NormalizedWeaponDmg:
                    weaponDamage += fixedBonus;

                    break;

                case SpellEffectName.WeaponPercentDamage:
                    weaponDamage = weaponDamage * weaponDamagePercentMod;

                    break;
                // not weapon damage effect, just skip
            }

        weaponDamage += spellBonus;
        weaponDamage = weaponDamage * totalDamagePercentMod;

        // prevent negative damage
        weaponDamage = Math.Max(weaponDamage, 0);

        // Add melee damage bonuses (also check for negative)
        weaponDamage = unitCaster.MeleeDamageBonusDone(UnitTarget, weaponDamage, AttackType, DamageEffectType.SpellDirect, SpellInfo, EffectInfo);
        DamageInEffects += UnitTarget.MeleeDamageBonusTaken(unitCaster, weaponDamage, AttackType, DamageEffectType.SpellDirect, SpellInfo);
    }

    /* There is currently no need for this effect. We handle it in Battleground.cpp
        If we would handle the resurrection here, the spiritguide would instantly disappear as the
        player revives, and so we wouldn't see the spirit heal visual effect on the npc.
        This is why we use a half sec delay between the visual effect and the resurrection itself */

    private bool IsUnitTargetSceneObjectAura(Spell spell, TargetInfo target)
    {
        if (target.TargetGuid != spell.Caster.GUID)
            return false;

        foreach (var spellEffectInfo in spell.SpellInfo.Effects)
            if (target.Effects.Contains(spellEffectInfo.EffectIndex) && spellEffectInfo.IsUnitOwnedAuraEffect)
                return true;

        return false;
    }

    private void SummonGuardian(SpellEffectInfo effect, uint entry, SummonPropertiesRecord properties, uint numGuardians, ObjectGuid privateObjectOwner)
    {
        var unitCaster = UnitCasterForEffectHandlers;

        if (unitCaster == null)
            return;

        if (unitCaster.IsTotem)
            unitCaster = unitCaster.ToTotem().OwnerUnit;

        // in another case summon new
        var radius = 5.0f;
        var duration = SpellInfo.CalcDuration(OriginalCaster);

        //TempSummonType summonType = (duration == 0) ? TempSummonType.DeadDespawn : TempSummonType.TimedDespawn;
        var map = unitCaster.Location.Map;

        for (uint count = 0; count < numGuardians; ++count)
        {
            var pos = count == 0
                          ? DestTarget
                          :
                          // randomize position for multiple summons
                          unitCaster.Location.GetRandomPoint(DestTarget, radius);

            var summon = map.SummonCreature(entry, pos, properties, (uint)duration, unitCaster, SpellInfo.Id, 0, privateObjectOwner);

            if (summon == null)
                return;

            if (summon.HasUnitTypeMask(UnitTypeMask.Guardian))
            {
                var level = summon.Level;

                if (properties != null && !properties.GetFlags().HasFlag(SummonPropertiesFlags.UseCreatureLevel))
                    level = unitCaster.Level;

                // level of pet summoned using engineering item based at engineering skill level
                if (CastItem != null && unitCaster.IsPlayer)
                {
                    var proto = CastItem.Template;

                    if (proto is { RequiredSkill: (uint)SkillType.Engineering })
                    {
                        var skill202 = unitCaster.AsPlayer.GetSkillValue(SkillType.Engineering);

                        if (skill202 != 0)
                            level = skill202 / 5u;
                    }
                }

                ((Guardian)summon).InitStatsForLevel(level);
            }

            if (summon.HasUnitTypeMask(UnitTypeMask.Minion) && Targets.HasDst)
                ((Minion)summon).SetFollowAngle(unitCaster.Location.GetAbsoluteAngle(summon.Location));

            if (summon.Entry == 27893)
            {
                var weapon = Caster.AsPlayer.PlayerData.VisibleItems[EquipmentSlot.MainHand];

                if (weapon.ItemID != 0)
                {
                    summon.SetDisplayId(11686);
                    summon.SetVirtualItem(0, weapon.ItemID, weapon.ItemAppearanceModID, weapon.ItemVisual);
                }
                else
                    summon.SetDisplayId(1126);
            }

            ExecuteLogEffectSummonObject(effect.Effect, summon);
        }
    }
}