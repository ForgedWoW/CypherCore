﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Framework.Constants;
using Framework.Dynamic;
using Scripts.Spells.Warlock;

namespace Scripts.Pets
{
    namespace Warlock
    {
        // 59262
        // 59271
        [CreatureScript(47319, 59271, 59262)]
        public class NPCWarlDemonicGateway : CreatureAI
        {
            public EventMap Events = new();
            public bool FirstTick = true;

            readonly uint[] _aurasToCheck =
            {
                121164, 121175, 121176, 121177
            }; // Orbs of Power @ Temple of Kotmogu

            public NPCWarlDemonicGateway(Creature creature) : base(creature) { }

            public override void UpdateAI(uint unnamedParameter)
            {
                if (FirstTick)
                {
                    Me.SpellFactory.CastSpell(Me, WarlockSpells.DEMONIC_GATEWAY_VISUAL, true);

                    Me.SetUnitFlag(UnitFlags.NonAttackable);
                    Me.SetNpcFlag(NPCFlags.SpellClick);
                    Me.ReactState = ReactStates.Passive;
                    Me.SetControlled(true, UnitState.Root);

                    FirstTick = false;
                }
            }

            public override void OnSpellClick(Unit clicker, ref bool spellClickHandled)
            {
                if (clicker.TryGetAsPlayer(out var player))
                {
                    // don't allow using the gateway while having specific Auras
                    foreach (var auraToCheck in _aurasToCheck)
                        if (player.HasAura(auraToCheck))
                            return;

                    TeleportTarget(player, true);
                }

                return;
            }

            public void TeleportTarget(Unit target, bool allowAnywhere)
            {
                var owner = Me.OwnerUnit;

                if (owner == null)
                    return;

                // only if Target stepped through the portal
                if (!allowAnywhere &&
                    Me.GetDistance2d(target) > 1.0f)
                    return;

                // check if Target wasn't recently teleported
                if (target.HasAura(WarlockSpells.DEMONIC_GATEWAY_DEBUFF))
                    return;

                // only if in same party
                if (!target.IsInRaidWith(owner))
                    return;

                // not allowed while CC'ed
                if (!target.CanFreeMove())
                    return;

                var otherGateway = Me.Entry == WarlockSpells.NPC_WARLOCK_DEMONIC_GATEWAY_GREEN ? WarlockSpells.NPC_WARLOCK_DEMONIC_GATEWAY_PURPLE : WarlockSpells.NPC_WARLOCK_DEMONIC_GATEWAY_GREEN;
                var teleportSpell = Me.Entry == WarlockSpells.NPC_WARLOCK_DEMONIC_GATEWAY_GREEN ? WarlockSpells.DEMONIC_GATEWAY_JUMP_GREEN : WarlockSpells.DEMONIC_GATEWAY_JUMP_PURPLE;

                var gateways = Me.GetCreatureListWithEntryInGrid(otherGateway, 100.0f);

                foreach (var gateway in gateways)
                {
                    if (gateway.OwnerGUID != Me.OwnerGUID)
                        continue;

                    target.SetFacingToUnit(gateway);
                    target.SpellFactory.CastSpell(gateway.Location, teleportSpell, true);

                    if (target.HasAura(WarlockSpells.PLANESWALKER))
                        target.SpellFactory.CastSpell(target, WarlockSpells.PLANESWALKER_BUFF, true);

                    // Item - Warlock PvP Set 4P Bonus: "Your allies can use your Demonic Gateway again 15 sec sooner"
                    if (owner.TryGetAuraEffect(WarlockSpells.PVP_4_P_BONUS, 0, out var eff))
                    {
                        var aura = target.GetAura(WarlockSpells.DEMONIC_GATEWAY_DEBUFF);

                        aura?.SetDuration(aura.Duration - eff.Amount * Time.IN_MILLISECONDS);
                    }

                    break;
                }
            }
        }
    }
}