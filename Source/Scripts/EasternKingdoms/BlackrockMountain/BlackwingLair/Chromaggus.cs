// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackwingLair;

internal struct SpellIds
{
    // These spells are actually called elemental shield
    // What they do is decrease all Damage by 75% then they increase
    // One school of Damage by 1100%
    public const uint FIRE_VULNERABILITY = 22277;
    public const uint FROST_VULNERABILITY = 22278;
    public const uint SHADOW_VULNERABILITY = 22279;
    public const uint NATURE_VULNERABILITY = 22280;

    public const uint ARCANE_VULNERABILITY = 22281;

    // Other spells
    public const uint INCINERATE = 23308;    //Incinerate 23308; 23309
    public const uint TIMELAPSE = 23310;     //Time lapse 23310; 23311(old threat mod that was removed in 2.01)
    public const uint CORROSIVEACID = 23313; //Corrosive Acid 23313; 23314
    public const uint IGNITEFLESH = 23315;   //Ignite Flesh 23315; 23316

    public const uint FROSTBURN = 23187; //Frost burn 23187; 23189

    // Brood Affliction 23173 - Scripted Spell that cycles through all targets within 100 yards and has a chance to cast one of the afflictions on them
    // Since Scripted spells arn't coded I'll just write a function that does the same thing
    public const uint BROODAF_BLUE = 23153;   //Blue affliction 23153
    public const uint BROODAF_BLACK = 23154;  //Black affliction 23154
    public const uint BROODAF_RED = 23155;    //Red affliction 23155 (23168 on death)
    public const uint BROODAF_BRONZE = 23170; //Bronze Affliction  23170
    public const uint BROODAF_GREEN = 23169;  //Brood Affliction Green 23169
    public const uint CHROMATIC_MUT1 = 23174; //Spell cast on player if they get all 5 debuffs
    public const uint FRENZY = 28371;         //The frenzy spell may be wrong
    public const uint ENRAGE = 28747;
}

internal struct TextIds
{
    public const uint EMOTE_FRENZY = 0;
    public const uint EMOTE_SHIMMER = 1;
}

[Script]
internal class BossChromaggus : BossAI
{
    private readonly uint _breath1Spell;
    private readonly uint _breath2Spell;
    private uint _currentVurlnSpell;
    private bool _enraged;

    public BossChromaggus(Creature creature) : base(creature, DataTypes.CHROMAGGUS)
    {
        Initialize();

        _breath1Spell = 0;
        _breath2Spell = 0;

        // Select the 2 breaths that we are going to use until despawned
        // 5 possiblities for the first breath, 4 for the second, 20 total possiblites
        // This way we don't end up casting 2 of the same breath
        // Tl Tl would be stupid
        switch (RandomHelper.URand(0, 19))
        {
            // B1 - Incin
            case 0:
                _breath1Spell = SpellIds.INCINERATE;
                _breath2Spell = SpellIds.TIMELAPSE;

                break;
            case 1:
                _breath1Spell = SpellIds.INCINERATE;
                _breath2Spell = SpellIds.CORROSIVEACID;

                break;
            case 2:
                _breath1Spell = SpellIds.INCINERATE;
                _breath2Spell = SpellIds.IGNITEFLESH;

                break;
            case 3:
                _breath1Spell = SpellIds.INCINERATE;
                _breath2Spell = SpellIds.FROSTBURN;

                break;

            // B1 - Tl
            case 4:
                _breath1Spell = SpellIds.TIMELAPSE;
                _breath2Spell = SpellIds.INCINERATE;

                break;
            case 5:
                _breath1Spell = SpellIds.TIMELAPSE;
                _breath2Spell = SpellIds.CORROSIVEACID;

                break;
            case 6:
                _breath1Spell = SpellIds.TIMELAPSE;
                _breath2Spell = SpellIds.IGNITEFLESH;

                break;
            case 7:
                _breath1Spell = SpellIds.TIMELAPSE;
                _breath2Spell = SpellIds.FROSTBURN;

                break;

            //B1 - Acid
            case 8:
                _breath1Spell = SpellIds.CORROSIVEACID;
                _breath2Spell = SpellIds.INCINERATE;

                break;
            case 9:
                _breath1Spell = SpellIds.CORROSIVEACID;
                _breath2Spell = SpellIds.TIMELAPSE;

                break;
            case 10:
                _breath1Spell = SpellIds.CORROSIVEACID;
                _breath2Spell = SpellIds.IGNITEFLESH;

                break;
            case 11:
                _breath1Spell = SpellIds.CORROSIVEACID;
                _breath2Spell = SpellIds.FROSTBURN;

                break;

            //B1 - Ignite
            case 12:
                _breath1Spell = SpellIds.IGNITEFLESH;
                _breath2Spell = SpellIds.INCINERATE;

                break;
            case 13:
                _breath1Spell = SpellIds.IGNITEFLESH;
                _breath2Spell = SpellIds.CORROSIVEACID;

                break;
            case 14:
                _breath1Spell = SpellIds.IGNITEFLESH;
                _breath2Spell = SpellIds.TIMELAPSE;

                break;
            case 15:
                _breath1Spell = SpellIds.IGNITEFLESH;
                _breath2Spell = SpellIds.FROSTBURN;

                break;

            //B1 - Frost
            case 16:
                _breath1Spell = SpellIds.FROSTBURN;
                _breath2Spell = SpellIds.INCINERATE;

                break;
            case 17:
                _breath1Spell = SpellIds.FROSTBURN;
                _breath2Spell = SpellIds.TIMELAPSE;

                break;
            case 18:
                _breath1Spell = SpellIds.FROSTBURN;
                _breath2Spell = SpellIds.CORROSIVEACID;

                break;
            case 19:
                _breath1Spell = SpellIds.FROSTBURN;
                _breath2Spell = SpellIds.IGNITEFLESH;

                break;
        }

        EnterEvadeMode();
    }

    public override void Reset()
    {
        _Reset();

        Initialize();
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);

        SchedulerProtected.Schedule(TimeSpan.FromSeconds(0),
                                    (Action<Framework.Dynamic.TaskContext>)(task =>
                                                                               {
                                                                                   // Remove old vulnerabilty spell
                                                                                   if (_currentVurlnSpell != 0)
                                                                                       Me.RemoveAura(_currentVurlnSpell);

                                                                                   // Cast new random vulnerabilty on self
                                                                                   var spell = RandomHelper.RAND(SpellIds.FIRE_VULNERABILITY, SpellIds.FROST_VULNERABILITY, SpellIds.SHADOW_VULNERABILITY, SpellIds.NATURE_VULNERABILITY, SpellIds.ARCANE_VULNERABILITY);
                                                                                   DoCast(Me, spell);
                                                                                   _currentVurlnSpell = spell;
                                                                                   Talk(TextIds.EMOTE_SHIMMER);
                                                                                   task.Repeat(TimeSpan.FromSeconds(45));
                                                                               }));

        SchedulerProtected.Schedule(TimeSpan.FromSeconds(30),
                                    task =>
                                    {
                                        DoCastVictim(_breath1Spell);
                                        task.Repeat(TimeSpan.FromSeconds(60));
                                    });

        SchedulerProtected.Schedule(TimeSpan.FromSeconds(60),
                                    task =>
                                    {
                                        DoCastVictim(_breath2Spell);
                                        task.Repeat(TimeSpan.FromSeconds(60));
                                    });

        SchedulerProtected.Schedule(TimeSpan.FromSeconds(10),
                                    task =>
                                    {
                                        var players = Me.Map.Players;

                                        foreach (var player in players)
                                            if (player)
                                            {
                                                DoCast(player, RandomHelper.RAND(SpellIds.BROODAF_BLUE, SpellIds.BROODAF_BLACK, SpellIds.BROODAF_RED, SpellIds.BROODAF_BRONZE, SpellIds.BROODAF_GREEN), new CastSpellExtraArgs(true));

                                                if (player.HasAura(SpellIds.BROODAF_BLUE) &&
                                                    player.HasAura(SpellIds.BROODAF_BLACK) &&
                                                    player.HasAura(SpellIds.BROODAF_RED) &&
                                                    player.HasAura(SpellIds.BROODAF_BRONZE) &&
                                                    player.HasAura(SpellIds.BROODAF_GREEN))
                                                    DoCast(player, SpellIds.CHROMATIC_MUT1);
                                            }

                                        task.Repeat(TimeSpan.FromSeconds(10));
                                    });

        SchedulerProtected.Schedule(TimeSpan.FromSeconds(15),
                                    task =>
                                    {
                                        DoCast(Me, SpellIds.FRENZY);
                                        task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15));
                                    });
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        SchedulerProtected.Update(diff);

        // Enrage if not already enraged and below 20%
        if (!_enraged &&
            HealthBelowPct(20))
        {
            DoCast(Me, SpellIds.ENRAGE);
            _enraged = true;
        }

        DoMeleeAttackIfReady();
    }

    private void Initialize()
    {
        _currentVurlnSpell = 0; // We use this to store our last vulnerabilty spell so we can remove it later
        _enraged = false;
    }
}

[Script]
internal class GOChromaggusLever : GameObjectAI
{
    private readonly InstanceScript _instance;

    public GOChromaggusLever(GameObject go) : base(go)
    {
        _instance = go.InstanceScript;
    }

    public override bool OnGossipHello(Player player)
    {
        if (_instance.GetBossState(DataTypes.CHROMAGGUS) != EncounterState.Done &&
            _instance.GetBossState(DataTypes.CHROMAGGUS) != EncounterState.InProgress)
        {
            _instance.SetBossState(DataTypes.CHROMAGGUS, EncounterState.InProgress);

            var creature = _instance.GetCreature(DataTypes.CHROMAGGUS);

            if (creature)
                creature.AI.JustEngagedWith(player);

            var go = _instance.GetGameObject(DataTypes.GO_CHROMAGGUS_DOOR);

            if (go)
                _instance.HandleGameObject(ObjectGuid.Empty, true, go);
        }

        Me.SetFlag(GameObjectFlags.NotSelectable | GameObjectFlags.InUse);
        Me.SetGoState(GameObjectState.Active);

        return true;
    }
}