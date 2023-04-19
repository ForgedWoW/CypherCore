// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Framework.Constants;

namespace Forged.MapServer.AI.CoreAI;

public class TotemAI : NullCreatureAI
{
    private ObjectGuid _victimGuid;

    public TotemAI(Creature creature) : base(creature)
    {
        _victimGuid = ObjectGuid.Empty;
    }

    public override void AttackStart(Unit victim) { }

    public override void UpdateAI(uint diff)
    {
        if (Me.ToTotem().TotemType != TotemType.Active)
            return;

        if (!Me.IsAlive || Me.IsNonMeleeSpellCast(false))
            return;

        // Search spell
        var spellInfo = Me.SpellManager.GetSpellInfo(Me.ToTotem().GetSpell(), Me.Location.Map.DifficultyID);

        if (spellInfo == null)
            return;

        // Get spell range
        var maxRange = spellInfo.GetMaxRange();

        // SpellModOp.Range not applied in this place just because not existence range mods for attacking totems

        var victim = !_victimGuid.IsEmpty ? Me.ObjectAccessor.GetUnit(Me, _victimGuid) : null;

        // Search victim if no, not attackable, or out of range, or friendly (possible in case duel end)
        if (victim == null || !victim.IsTargetableForAttack() || !Me.Location.IsWithinDistInMap(victim, maxRange) || Me.WorldObjectCombat.IsFriendlyTo(victim) || !Me.Visibility.CanSeeOrDetect(victim))
        {
            var extraSearchRadius = maxRange > 0.0f ? SharedConst.ExtraCellSearchRadius : 0.0f;
            var uCheck = new NearestAttackableUnitInObjectRangeCheck(Me, Me.CharmerOrOwnerOrSelf, maxRange);
            var checker = new UnitLastSearcher(Me, uCheck, GridType.All);
            CellCalculator.VisitGrid(Me, checker, maxRange + extraSearchRadius);
            victim = checker.GetTarget();
        }

        // If have target
        if (victim != null)
        {
            // remember
            _victimGuid = victim.GUID;

            // attack
            Me.SpellFactory.CastSpell(victim, Me.ToTotem().GetSpell());
        }
        else
            _victimGuid.Clear();
    }
}