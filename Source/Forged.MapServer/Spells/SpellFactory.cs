using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Maps;
using Forged.MapServer.Networking.Packets.Chat;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Spells
{
    public class SpellFactory
    {
        private readonly ClassFactory _classFactory;
        private readonly SpellManager _spellManager;

        public SpellFactory(ClassFactory classFactory, SpellManager spellManager)
        {
            _classFactory = classFactory;
            _spellManager = spellManager;
        }

        public Spell NewSpell(WorldObject caster, SpellInfo info, TriggerCastFlags triggerFlags, ObjectGuid originalCasterGuid = default, ObjectGuid originalCastId = default, byte? empoweredStage = null)
        {
            return _classFactory.Resolve<Spell>(new PositionalParameter(0, caster), 
                                                new PositionalParameter(1, info), 
                                                new PositionalParameter(2, triggerFlags), 
                                                new NamedParameter(nameof(originalCasterGuid), originalCasterGuid), 
                                                new NamedParameter(nameof(originalCastId), originalCastId), 
                                                new NamedParameter(nameof(empoweredStage), empoweredStage));
        }


        public SpellCastResult CastSpell(WorldObject caster, uint spellId, bool triggered = false, byte? empowerStage = null)
        {
            return CastSpell(caster, null, spellId, triggered, empowerStage);
        }

        public SpellCastResult CastSpell<T>(WorldObject caster, WorldObject target, T spellId, bool triggered = false) where T : struct, Enum
        {
            return CastSpell(caster, target, Convert.ToUInt32(spellId), triggered);
        }

        public SpellCastResult CastSpell(WorldObject caster, WorldObject target, uint spellId, Spell triggeringSpell)
        {
            CastSpellExtraArgs args = new(true)
            {
                TriggeringSpell = triggeringSpell
            };

            return CastSpell(caster, target, spellId, args);
        }

        public SpellCastResult CastSpell(WorldObject caster, WorldObject target, uint spellId, AuraEffect triggeringAura)
        {
            CastSpellExtraArgs args = new(true)
            {
                TriggeringAura = triggeringAura
            };

            return CastSpell(caster, target, spellId, args);
        }

        public SpellCastResult CastSpell(WorldObject caster, WorldObject target, uint spellId, bool triggered = false, byte? empowerStage = null)
        {
            CastSpellExtraArgs args = new(triggered)
            {
                EmpowerStage = empowerStage
            };

            return CastSpell(caster, target, spellId, args);
        }

        public SpellCastResult CastSpell(WorldObject caster, WorldObject target, uint spellId, TriggerCastFlags triggerCastFlags, bool triggered = false)
        {
            CastSpellExtraArgs args = new(triggered)
            {
                TriggerFlags = triggerCastFlags
            };

            return CastSpell(caster, target, spellId, args);
        }

        public SpellCastResult CastSpell(WorldObject caster, WorldObject target, uint spellId, double bp0Val, bool triggered = false)
        {
            CastSpellExtraArgs args = new(triggered)
            {
                SpellValueOverrides =
            {
                [SpellValueMod.BasePoint0] = bp0Val
            }
            };

            return CastSpell(caster, target, spellId, args);
        }

        public SpellCastResult CastSpell(WorldObject caster, WorldObject target, uint spellId, SpellValueMod spellValueMod, double bp0Val, bool triggered = false)
        {
            CastSpellExtraArgs args = new(triggered)
            {
                SpellValueOverrides =
            {
                [spellValueMod] = bp0Val
            }
            };

            return CastSpell(caster, target, spellId, args);
        }

        public SpellCastResult CastSpell(WorldObject caster, SpellCastTargets targets, uint spellId, CastSpellExtraArgs args)
        {
            return CastSpell(caster, new CastSpellTargetArg(targets), spellId, args);
        }

        public SpellCastResult CastSpell(WorldObject caster, WorldObject target, uint spellId, CastSpellExtraArgs args)
        {
            return CastSpell(caster, new CastSpellTargetArg(target), spellId, args);
        }

        public SpellCastResult CastSpell(WorldObject caster, float x, float y, float z, uint spellId, bool triggered = false)
        {
            return CastSpell(caster, new Position(x, y, z), spellId, triggered);
        }

        public SpellCastResult CastSpell(WorldObject caster, float x, float y, float z, uint spellId, CastSpellExtraArgs args)
        {
            return CastSpell(caster, new Position(x, y, z), spellId, args);
        }

        public SpellCastResult CastSpell(WorldObject caster, Position dest, uint spellId, bool triggered = false)
        {
            CastSpellExtraArgs args = new(triggered);

            return CastSpell(caster, new CastSpellTargetArg(dest), spellId, args);
        }

        public SpellCastResult CastSpell(WorldObject caster, Position dest, uint spellId, CastSpellExtraArgs args)
        {
            return CastSpell(caster, new CastSpellTargetArg(dest), spellId, args);
        }

        public SpellCastResult CastSpell(WorldObject caster, CastSpellTargetArg targets, uint spellId, CastSpellExtraArgs args)
        {
            var info = _spellManager.GetSpellInfo(spellId, args.CastDifficulty != Difficulty.None ? args.CastDifficulty : caster.Map.DifficultyID);

            if (info == null)
            {
                Log.Logger.Error($"CastSpell: unknown spell {spellId} by caster {caster.GUID}");

                return SpellCastResult.SpellUnavailable;
            }

            if (targets.Targets == null)
            {
                Log.Logger.Error($"CastSpell: Invalid target passed to spell cast {spellId} by {caster.GUID}");

                return SpellCastResult.BadTargets;
            }

            Spell spell = NewSpell(caster, info, args.TriggerFlags, args.OriginalCaster, args.OriginalCastId, args.EmpowerStage);

            foreach (var pair in args.SpellValueOverrides)
                spell.SetSpellValue(pair.Key, (float)pair.Value);

            spell.CastItem = args.CastItem;

            if (args.OriginalCastItemLevel.HasValue)
                spell.CastItemLevel = args.OriginalCastItemLevel.Value;

            if (spell.CastItem == null && info.HasAttribute(SpellAttr2.RetainItemCast))
            {
                if (args.TriggeringSpell)
                {
                    spell.CastItem = args.TriggeringSpell.CastItem;
                }
                else if (args.TriggeringAura != null && !args.TriggeringAura.Base.CastItemGuid.IsEmpty)
                {
                    var triggeringAuraCaster = args.TriggeringAura.Caster?.AsPlayer;

                    if (triggeringAuraCaster != null)
                        spell.CastItem = triggeringAuraCaster.GetItemByGuid(args.TriggeringAura.Base.CastItemGuid);
                }
            }

            spell.CustomArg = args.CustomArg;

            return spell.Prepare(targets.Targets, args.TriggeringAura);
        }
    }
}
