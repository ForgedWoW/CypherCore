// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking;
using Forged.MapServer.Spells;
using Framework.Collections;
using Framework.Constants;

namespace Forged.MapServer.Entities.Units;

public class CharmInfo
{
    private readonly UnitActionBarEntry[] _charmspells = new UnitActionBarEntry[4];
    private readonly ReactStates _oldReactState;
    private readonly UnitActionBarEntry[] _petActionBar = new UnitActionBarEntry[SharedConst.ActionBarIndexMax];
    private readonly Unit _unit;
    private float _stayX;
    private float _stayY;
    private float _stayZ;

    public CharmInfo(Unit unit)
    {
        _unit = unit;
        CommandState = CommandStates.Follow;
        PetNumber = 0;
        _oldReactState = ReactStates.Passive;

        for (byte i = 0; i < SharedConst.MaxSpellCharm; ++i)
        {
            _charmspells[i] = new UnitActionBarEntry();
            _charmspells[i].SetActionAndType(0, ActiveStates.Disabled);
        }

        for (var i = 0; i < SharedConst.ActionBarIndexMax; ++i)
            _petActionBar[i] = new UnitActionBarEntry();

        var creature = _unit.AsCreature;

        if (creature == null)
            return;

        _oldReactState = creature.ReactState;
        creature.ReactState = ReactStates.Passive;
    }

    public CommandStates CommandState { get; set; }

    public bool IsAtStay { get; set; }

    public bool IsCommandAttack { get; set; }

    public bool IsCommandFollow { get; set; }

    public bool IsFollowing { get; set; }

    public bool IsReturning { get; set; }

    public uint PetNumber { get; private set; }

    public bool AddSpellToActionBar(SpellInfo spellInfo, ActiveStates newstate = ActiveStates.Decide, int preferredSlot = 0)
    {
        var spellID = spellInfo.Id;
        var firstID = spellInfo.FirstRankSpell.Id;

        // new spell rank can be already listed
        for (byte i = 0; i < SharedConst.ActionBarIndexMax; ++i)
        {
            var action = _petActionBar[i].Action;

            if (action == 0)
                continue;

            if (!_petActionBar[i].IsActionBarForSpell || _unit.SpellManager.GetFirstSpellInChain(action) != firstID)
                continue;

            _petActionBar[i].SetAction(spellID);

            return true;
        }

        // or use empty slot in other case
        for (byte i = 0; i < SharedConst.ActionBarIndexMax; ++i)
        {
            var j = (byte)((preferredSlot + i) % SharedConst.ActionBarIndexMax);

            if (_petActionBar[j].Action != 0 || !_petActionBar[j].IsActionBarForSpell)
                continue;

            SetActionBar(j, spellID, newstate == ActiveStates.Decide ? spellInfo.IsAutocastable ? ActiveStates.Disabled : ActiveStates.Passive : newstate);

            return true;
        }

        return false;
    }

    public void BuildActionBar(WorldPacket data)
    {
        for (var i = 0; i < SharedConst.ActionBarIndexMax; ++i)
            data.WriteUInt32(_petActionBar[i].PackedData);
    }

    public UnitActionBarEntry GetActionBarEntry(byte index)
    {
        return _petActionBar[index];
    }

    public UnitActionBarEntry GetCharmSpell(byte index)
    {
        return _charmspells[index];
    }

    public void GetStayPosition(out float x, out float y, out float z)
    {
        x = _stayX;
        y = _stayY;
        z = _stayZ;
    }

    public bool HasCommandState(CommandStates state)
    {
        return CommandState == state;
    }

    public void InitCharmCreateSpells()
    {
        if (_unit.IsTypeId(TypeId.Player)) // charmed players don't have spells
        {
            InitEmptyActionBar();

            return;
        }

        InitPetActionBar();

        for (uint x = 0; x < SharedConst.MaxSpellCharm; ++x)
        {
            var spellId = _unit.AsCreature.Spells[x];
            var spellInfo = _unit.SpellManager.GetSpellInfo(spellId, _unit.Location.Map.DifficultyID);

            if (spellInfo == null)
            {
                _charmspells[x].SetActionAndType(spellId, ActiveStates.Disabled);

                continue;
            }

            if (spellInfo.HasAttribute(SpellAttr5.NotAvailableWhileCharmed))
                continue;

            if (spellInfo.IsPassive)
            {
                _unit.SpellFactory.CastSpell(_unit, spellInfo.Id, new CastSpellExtraArgs(true));
                _charmspells[x].SetActionAndType(spellId, ActiveStates.Passive);
            }
            else
            {
                _charmspells[x].SetActionAndType(spellId, ActiveStates.Disabled);

                ActiveStates newstate;

                if (!spellInfo.IsAutocastable)
                    newstate = ActiveStates.Passive;
                else
                {
                    if (spellInfo.NeedsExplicitUnitTarget)
                    {
                        newstate = ActiveStates.Enabled;
                        ToggleCreatureAutocast(spellInfo, true);
                    }
                    else
                        newstate = ActiveStates.Disabled;
                }

                AddSpellToActionBar(spellInfo, newstate);
            }
        }
    }

    public void InitEmptyActionBar(bool withAttack = true)
    {
        if (withAttack)
            SetActionBar(SharedConst.ActionBarIndexStart, (uint)CommandStates.Attack, ActiveStates.Command);
        else
            SetActionBar(SharedConst.ActionBarIndexStart, 0, ActiveStates.Passive);

        for (byte x = SharedConst.ActionBarIndexStart + 1; x < SharedConst.ActionBarIndexEnd; ++x)
            SetActionBar(x, 0, ActiveStates.Passive);
    }

    public void InitPetActionBar()
    {
        // the first 3 SpellOrActions are attack, follow and stay
        for (byte i = 0; i < SharedConst.ActionBarIndexPetSpellStart - SharedConst.ActionBarIndexStart; ++i)
            SetActionBar((byte)(SharedConst.ActionBarIndexStart + i), (uint)CommandStates.Attack - i, ActiveStates.Command);

        // middle 4 SpellOrActions are spells/special attacks/abilities
        for (byte i = 0; i < SharedConst.ActionBarIndexPetSpellEnd - SharedConst.ActionBarIndexPetSpellStart; ++i)
            SetActionBar((byte)(SharedConst.ActionBarIndexPetSpellStart + i), 0, ActiveStates.Passive);

        // last 3 SpellOrActions are reactions
        for (byte i = 0; i < SharedConst.ActionBarIndexEnd - SharedConst.ActionBarIndexPetSpellEnd; ++i)
            SetActionBar((byte)(SharedConst.ActionBarIndexPetSpellEnd + i), (uint)CommandStates.Attack - i, ActiveStates.Reaction);
    }

    public void InitPossessCreateSpells()
    {
        if (_unit.IsTypeId(TypeId.Unit))
        {
            // Adding switch until better way is found. Malcrom
            // Adding entrys to this switch will prevent COMMAND_ATTACK being added to pet bar.
            switch (_unit.Entry)
            {
                case 23575: // Mindless Abomination
                case 24783: // Trained Rock Falcon
                case 27664: // Crashin' Thrashin' Racer
                case 40281: // Crashin' Thrashin' Racer
                case 28511: // Eye of Acherus
                    break;

                default:
                    InitEmptyActionBar();

                    break;
            }

            for (byte i = 0; i < SharedConst.MaxCreatureSpells; ++i)
            {
                var spellId = _unit.AsCreature.Spells[i];
                var spellInfo = _unit.SpellManager.GetSpellInfo(spellId, _unit.Location.Map.DifficultyID);

                if (spellInfo == null)
                    continue;

                if (spellInfo.HasAttribute(SpellAttr5.NotAvailableWhileCharmed))
                    continue;

                if (spellInfo.IsPassive)
                    _unit.SpellFactory.CastSpell(_unit, spellInfo.Id, new CastSpellExtraArgs(true));
                else
                    AddSpellToActionBar(spellInfo, ActiveStates.Passive, i % SharedConst.ActionBarIndexMax);
            }
        }
        else
            InitEmptyActionBar();
    }

    public void LoadPetActionBar(string data)
    {
        InitPetActionBar();

        var tokens = new StringArray(data, ' ');

        if (tokens.Length != (SharedConst.ActionBarIndexEnd - SharedConst.ActionBarIndexStart) * 2)
            return; // non critical, will reset to default

        byte index = 0;

        for (byte i = 0; i < tokens.Length && index < SharedConst.ActionBarIndexEnd; ++i, ++index)
        {
            var type = tokens[i++].ToEnum<ActiveStates>();
            uint.TryParse(tokens[i], out var action);

            _petActionBar[index].SetActionAndType(action, type);

            // check correctness
            if (!_petActionBar[index].IsActionBarForSpell)
                continue;

            var spelInfo = _unit.SpellManager.GetSpellInfo(_petActionBar[index].Action, _unit.Location.Map.DifficultyID);

            if (spelInfo == null)
                SetActionBar(index, 0, ActiveStates.Passive);
            else if (!spelInfo.IsAutocastable)
                SetActionBar(index, _petActionBar[index].Action, ActiveStates.Passive);
        }
    }

    public bool RemoveSpellFromActionBar(uint spellID)
    {
        var firstID = _unit.SpellManager.GetFirstSpellInChain(spellID);

        for (byte i = 0; i < SharedConst.ActionBarIndexMax; ++i)
        {
            var action = _petActionBar[i].Action;

            if (action == 0)
                continue;

            if (!_petActionBar[i].IsActionBarForSpell || _unit.SpellManager.GetFirstSpellInChain(action) != firstID)
                continue;

            SetActionBar(i, 0, ActiveStates.Passive);

            return true;
        }

        return false;
    }

    public void RestoreState()
    {
        if (!_unit.IsTypeId(TypeId.Unit))
            return;

        var creature = _unit.AsCreature;

        if (creature != null)
            creature.ReactState = _oldReactState;
    }

    public void SaveStayPosition()
    {
        //! At this point a new spline destination is enabled because of Unit.StopMoving()
        var stayPos = new Position(_unit.MoveSpline.FinalDestination);

        if (_unit.MoveSpline.OnTransport)
        {
            var transport = _unit.DirectTransport;

            transport?.CalculatePassengerPosition(stayPos);
        }

        _stayX = stayPos.X;
        _stayY = stayPos.Y;
        _stayZ = stayPos.Z;
    }

    public void SetActionBar(byte index, uint spellOrAction, ActiveStates type)
    {
        _petActionBar[index].SetActionAndType(spellOrAction, type);
    }

    public void SetPetNumber(uint petnumber, bool statwindow)
    {
        PetNumber = petnumber;

        _unit.SetPetNumberForClient(statwindow ? PetNumber : 0);
    }

    public void SetSpellAutocast(SpellInfo spellInfo, bool state)
    {
        for (byte i = 0; i < SharedConst.ActionBarIndexMax; ++i)
            if (spellInfo.Id == _petActionBar[i].Action && _petActionBar[i].IsActionBarForSpell)
            {
                _petActionBar[i].SetType(state ? ActiveStates.Enabled : ActiveStates.Disabled);

                break;
            }
    }

    public void ToggleCreatureAutocast(SpellInfo spellInfo, bool apply)
    {
        if (spellInfo.IsPassive)
            return;

        for (uint x = 0; x < SharedConst.MaxSpellCharm; ++x)
            if (spellInfo.Id == _charmspells[x].Action)
                _charmspells[x].SetType(apply ? ActiveStates.Enabled : ActiveStates.Disabled);
    }
}