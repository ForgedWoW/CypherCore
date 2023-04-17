// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.EasternKingdoms.AlteracValley;

internal struct SpellIds
{
    public const uint CHARGE = 22911;
    public const uint CLEAVE = 40504;
    public const uint DEMORALIZING_SHOUT = 23511;
    public const uint ENRAGE = 8599;
    public const uint WHIRLWIND = 13736;

    public const uint NORTH_MARSHAL = 45828;
    public const uint SOUTH_MARSHAL = 45829;
    public const uint STONEHEARTH_MARSHAL = 45830;
    public const uint ICEWING_MARSHAL = 45831;
    public const uint ICEBLOOD_WARMASTER = 45822;
    public const uint TOWER_POINT_WARMASTER = 45823;
    public const uint WEST_FROSTWOLF_WARMASTER = 45824;
    public const uint EAST_FROSTWOLF_WARMASTER = 45826;
}

internal struct CreatureIds
{
    public const uint NORTH_MARSHAL = 14762;
    public const uint SOUTH_MARSHAL = 14763;
    public const uint ICEWING_MARSHAL = 14764;
    public const uint STONEHEARTH_MARSHAL = 14765;
    public const uint EAST_FROSTWOLF_WARMASTER = 14772;
    public const uint ICEBLOOD_WARMASTER = 14773;
    public const uint TOWER_POINT_WARMASTER = 14776;
    public const uint WEST_FROSTWOLF_WARMASTER = 14777;
}

[Script]
internal class NPCAvMarshalOrWarmaster : ScriptedAI
{
    private readonly (uint npcEntry, uint spellId)[] _auraPairs =
    {
        new(CreatureIds.NORTH_MARSHAL, SpellIds.NORTH_MARSHAL), new(CreatureIds.SOUTH_MARSHAL, SpellIds.SOUTH_MARSHAL), new(CreatureIds.STONEHEARTH_MARSHAL, SpellIds.STONEHEARTH_MARSHAL), new(CreatureIds.ICEWING_MARSHAL, SpellIds.ICEWING_MARSHAL), new(CreatureIds.EAST_FROSTWOLF_WARMASTER, SpellIds.EAST_FROSTWOLF_WARMASTER), new(CreatureIds.WEST_FROSTWOLF_WARMASTER, SpellIds.WEST_FROSTWOLF_WARMASTER), new(CreatureIds.TOWER_POINT_WARMASTER, SpellIds.TOWER_POINT_WARMASTER), new(CreatureIds.ICEBLOOD_WARMASTER, SpellIds.ICEBLOOD_WARMASTER)
    };

    private bool _hasAura;

    public NPCAvMarshalOrWarmaster(Creature creature) : base(creature)
    {
        Initialize();
    }

    public override void Reset()
    {
        Initialize();

        Scheduler.CancelAll();

        Scheduler.Schedule(TimeSpan.FromSeconds(2),
                           TimeSpan.FromSeconds(12),
                           task =>
                           {
                               DoCastVictim(SpellIds.CHARGE);
                               task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(25));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(1),
                           TimeSpan.FromSeconds(11),
                           task =>
                           {
                               DoCastVictim(SpellIds.CLEAVE);
                               task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(16));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(2),
                           task =>
                           {
                               DoCast(Me, SpellIds.DEMORALIZING_SHOUT);
                               task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(5),
                           TimeSpan.FromSeconds(20),
                           task =>
                           {
                               DoCast(Me, SpellIds.WHIRLWIND);
                               task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(25));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(5),
                           TimeSpan.FromSeconds(20),
                           task =>
                           {
                               DoCast(Me, SpellIds.ENRAGE);
                               task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(5),
                           (Action<Framework.Dynamic.TaskContext>)(task =>
                                                                      {
                                                                          var homePosition = Me.HomePosition;

                                                                          if (Me.GetDistance2d(homePosition.X, homePosition.Y) > 50.0f)
                                                                          {
                                                                              base.EnterEvadeMode();

                                                                              return;
                                                                          }

                                                                          task.Repeat(TimeSpan.FromSeconds(5));
                                                                      }));
    }

    public override void JustAppeared()
    {
        Reset();
    }

    public override void UpdateAI(uint diff)
    {
        // I have a feeling this isn't blizzlike, but owell, I'm only passing by and cleaning up.
        if (!_hasAura)
        {
            for (byte i = 0; i < _auraPairs.Length; ++i)
                if (_auraPairs[i].npcEntry == Me.Entry)
                    DoCast(Me, _auraPairs[i].spellId);

            _hasAura = true;
        }

        if (!UpdateVictim())
            return;

        Scheduler.Update(diff);

        if (Me.HasUnitState(UnitState.Casting))
            return;

        DoMeleeAttackIfReady();
    }

    private void Initialize()
    {
        _hasAura = false;
    }
}