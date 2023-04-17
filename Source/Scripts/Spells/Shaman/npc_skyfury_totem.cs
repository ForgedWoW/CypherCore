// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Scripting;

namespace Scripts.Spells.Shaman;

//105427 Skyfury Totem
[CreatureScript(105427)]
public class NPCSkyfuryTotem : ScriptedAI
{
    public uint MUIBuffTimer;
    public int MBuffDuration = 15000;

    public NPCSkyfuryTotem(Creature creature) : base(creature) { }

    public override void Reset()
    {
        MUIBuffTimer = (uint)TotemData.DELAY;
        ApplyBuff();
    }

    public override void UpdateAI(uint uiDiff)
    {
        MBuffDuration -= (int)uiDiff;

        if (MUIBuffTimer <= uiDiff)
            ApplyBuff();
        else
            MUIBuffTimer -= uiDiff;
    }

    public void ApplyBuff()
    {
        MUIBuffTimer = (uint)TotemData.DELAY;

        if (!Me)
            return;

        var targets = new List<Unit>();
        var check = new AnyFriendlyUnitInObjectRangeCheck(Me, Me, TotemData.RANGE);
        var searcher = new UnitListSearcher(Me, targets, check, Framework.Constants.GridType.All);
        Cell.VisitGrid(Me, searcher, TotemData.RANGE);

        foreach (var itr in targets)
        {
            if (!itr)
                continue;

            if (!itr.HasAura(TotemSpells.TOTEM_SKYFURY_EFFECT))
            {
                Me.SpellFactory.CastSpell(itr, TotemSpells.TOTEM_SKYFURY_EFFECT, true);
                var aura = itr.GetAura(TotemSpells.TOTEM_SKYFURY_EFFECT);

                if (aura != null)
                    aura.SetDuration(MBuffDuration);
            }
        }
    }

    public struct TotemData
    {
        public const uint TO_CAST = TotemSpells.TOTEM_SKYFURY_EFFECT;
        public const uint RANGE = 40;
        public const uint DELAY = 500;
    }
}