// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Combat;

public class CombatReference
{
    public Unit First;
    public Unit Second;
    public bool IsPvP;

    private bool _suppressFirst;
    private bool _suppressSecond;

    public CombatReference(Unit a, Unit b, bool pvp = false)
    {
        First = a;
        Second = b;
        IsPvP = pvp;
    }

    public void EndCombat()
    {
        // sequencing matters here - AI might do nasty stuff, so make sure refs are in a consistent state before you hand off!

        // first, get rid of any threat that still exists...
        First.GetThreatManager().ClearThreat(Second);
        Second.GetThreatManager().ClearThreat(First);

        // ...then, remove the references from both managers...
        First.GetCombatManager().PurgeReference(Second.GUID, IsPvP);
        Second.GetCombatManager().PurgeReference(First.GUID, IsPvP);

        // ...update the combat state, which will potentially remove IN_COMBAT...
        var needFirstAI = First.GetCombatManager().UpdateOwnerCombatState();
        var needSecondAI = Second.GetCombatManager().UpdateOwnerCombatState();

        // ...and if that happened, also notify the AI of it...
        if (needFirstAI)
        {
            var firstAI = First.AI;

            firstAI?.JustExitedCombat();
        }

        if (needSecondAI)
        {
            var secondAI = Second.AI;

            secondAI?.JustExitedCombat();
        }
    }

    public void Refresh()
    {
        bool needFirstAI = false, needSecondAI = false;

        if (_suppressFirst)
        {
            _suppressFirst = false;
            needFirstAI = First.GetCombatManager().UpdateOwnerCombatState();
        }

        if (_suppressSecond)
        {
            _suppressSecond = false;
            needSecondAI = Second.GetCombatManager().UpdateOwnerCombatState();
        }

        if (needFirstAI)
            CombatManager.NotifyAICombat(First, Second);

        if (needSecondAI)
            CombatManager.NotifyAICombat(Second, First);
    }

    public void SuppressFor(Unit who)
    {
        Suppress(who);

        if (who.GetCombatManager().UpdateOwnerCombatState())
        {
            var ai = who.AI;

            ai?.JustExitedCombat();
        }
    }

    // suppressed combat refs do not generate a combat state for one side of the relation
    // (used by: vanish, feign death)
    public bool IsSuppressedFor(Unit who)
    {
        return (who == First) ? _suppressFirst : _suppressSecond;
    }

    public void Suppress(Unit who)
    {
        if (who == First)
            _suppressFirst = true;
        else
            _suppressSecond = true;
    }

    public Unit GetOther(Unit me)
    {
        return (First == me) ? Second : First;
    }
}