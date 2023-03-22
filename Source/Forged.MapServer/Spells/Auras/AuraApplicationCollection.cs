// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;

namespace Game.Spells.Auras;

public class AuraApplicationCollection
{
	private readonly Dictionary<Guid, AuraApplication> _auras = new(); // To keep this thread safe we have the guid as the key to all auras, The aura may be removed while preforming a query.
	private readonly MultiMapHashSet<uint, Guid> _aurasBySpellId = new();
	private readonly MultiMapHashSet<ObjectGuid, Guid> _byCasterGuid = new();
	private readonly MultiMapHashSet<ObjectGuid, Guid> _byCastItemGuid = new();
	private readonly MultiMapHashSet<ObjectGuid, Guid> _byCastId = new();
	private readonly MultiMapHashSet<WorldObject, Guid> _byOwner = new();
	private readonly MultiMapHashSet<bool, Guid> _isSingleTarget = new();
	private readonly MultiMapHashSet<uint, Guid> _labelMap = new();
	private readonly MultiMapHashSet<bool, Guid> _canBeSaved = new();
	private readonly MultiMapHashSet<bool, Guid> _isgroupBuff = new();
	private readonly MultiMapHashSet<bool, Guid> _isPassive = new();
	private readonly MultiMapHashSet<bool, Guid> _isDeathPersistant = new();
	private readonly MultiMapHashSet<bool, Guid> _isRequiringDeadTarget = new();
	private readonly MultiMapHashSet<bool, Guid> _isPlayer = new();
	private readonly MultiMapHashSet<bool, Guid> _isPerm = new();
	private readonly MultiMapHashSet<bool, Guid> _isPositive = new();
	private readonly MultiMapHashSet<bool, Guid> _onlyIndoors = new();
	private readonly MultiMapHashSet<bool, Guid> _onlyOutdoors = new();
	private readonly MultiMapHashSet<SpellFamilyNames, Guid> _spellFamily = new();
	private readonly MultiMapHashSet<AuraObjectType, Guid> _typeMap = new();
	private readonly MultiMapHashSet<int, Guid> _effectIndex = new();
	private readonly MultiMapHashSet<DiminishingGroup, Guid> _deminishGroup = new();
	private readonly MultiMapHashSet<AuraStateType, Guid> _casterAuraState = new();
	private readonly MultiMapHashSet<DispelType, Guid> _dispelType = new();
	private readonly HashSet<Guid> _hasNegitiveFlag = new();

	public HashSet<AuraApplication> AuraApplications
	{
		get
		{
			lock (_auras)
			{
				return _auras.Values.ToHashSet();
			}
		}
	}

	public int Count
	{
		get
		{
			lock (_auras)
			{
				return _auras.Count;
			}
		}
	}

	public void Add(AuraApplication auraApp)
	{
		lock (_auras)
		{
			if (_auras.ContainsKey(auraApp.Guid))
				return;

			var aura = auraApp.Base;
			var si = aura.SpellInfo;
			_auras[auraApp.Guid] = auraApp;
			_aurasBySpellId.Add(aura.Id, auraApp.Guid);

			var casterGuid = aura.CasterGuid;

			if (!casterGuid.IsEmpty)
				_byCasterGuid.Add(casterGuid, auraApp.Guid);

			if (!aura.CastItemGuid.IsEmpty)
				_byCastItemGuid.Add(aura.CastItemGuid, auraApp.Guid);

			_isSingleTarget.Add(aura.IsSingleTarget, auraApp.Guid);

			foreach (var label in aura.SpellInfo.Labels)
				_labelMap.Add(label, auraApp.Guid);


			_canBeSaved.Add(aura.CanBeSaved(), auraApp.Guid);
			_isgroupBuff.Add(aura.SpellInfo.IsGroupBuff, auraApp.Guid);
			_isPassive.Add(aura.IsPassive, auraApp.Guid);
			_isDeathPersistant.Add(aura.IsDeathPersistent, auraApp.Guid);
			_isRequiringDeadTarget.Add(aura.SpellInfo.IsRequiringDeadTarget, auraApp.Guid);
			_isPositive.Add(auraApp.IsPositive, auraApp.Guid);

			var owner = aura.Owner;

			if (owner != null)
				_byOwner.Add(owner, auraApp.Guid);

			var castId = aura.CastId;

			if (!castId.IsEmpty)
				_byCastId.Add(castId, auraApp.Guid);

			var caster = aura.Caster;

			if (caster != null)
				_isPlayer.Add(caster.IsPlayer, auraApp.Guid);

			_deminishGroup.Add(si.DiminishingReturnsGroupForSpell, auraApp.Guid);
			_casterAuraState.Add(si.CasterAuraState, auraApp.Guid);
			_typeMap.Add(aura.AuraObjType, auraApp.Guid);
			_dispelType.Add(si.Dispel, auraApp.Guid);

			var flags = auraApp.Flags;

			if (flags.HasFlag(AuraFlags.Negative))
				_hasNegitiveFlag.Add(auraApp.Guid);

			_isPerm.Add(aura.IsPermanent, auraApp.Guid);

			foreach (var eff in auraApp.EffectMask)
				_effectIndex.Add(eff, auraApp.Guid);

			_onlyIndoors.Add(si.HasAttribute(SpellAttr0.OnlyIndoors), auraApp.Guid);
			_onlyOutdoors.Add(si.HasAttribute(SpellAttr0.OnlyOutdoors), auraApp.Guid);
			_spellFamily.Add(si.SpellFamilyName, auraApp.Guid);
		}
	}

	public bool Remove(AuraApplication auraApp)
	{
		bool removed;

		lock (_auras)
		{
			var aura = auraApp.Base;
			var si = aura.SpellInfo;
			removed = _auras.Remove(auraApp.Guid);
			_aurasBySpellId.Remove(aura.Id, auraApp.Guid);

			var caster = aura.Caster;

			if (caster != null)
				_isPlayer.Remove(aura.Caster.IsPlayer, auraApp.Guid);

			_deminishGroup.Remove(si.DiminishingReturnsGroupForSpell, auraApp.Guid);
			_casterAuraState.Remove(si.CasterAuraState, auraApp.Guid);
			_typeMap.Remove(aura.AuraObjType, auraApp.Guid);
			_dispelType.Remove(si.Dispel, auraApp.Guid);
			_canBeSaved.Remove(aura.CanBeSaved(), auraApp.Guid);
			_isgroupBuff.Remove(aura.SpellInfo.IsGroupBuff, auraApp.Guid);
			_isPassive.Remove(aura.IsPassive, auraApp.Guid);
			_isDeathPersistant.Remove(aura.IsDeathPersistent, auraApp.Guid);
			_isRequiringDeadTarget.Remove(aura.SpellInfo.IsRequiringDeadTarget, auraApp.Guid);
			_onlyIndoors.Remove(si.HasAttribute(SpellAttr0.OnlyIndoors), auraApp.Guid);
			_onlyOutdoors.Remove(si.HasAttribute(SpellAttr0.OnlyOutdoors), auraApp.Guid);
			_spellFamily.Remove(si.SpellFamilyName, auraApp.Guid);
			_isPositive.Remove(auraApp.IsPositive, auraApp.Guid);

			if (!aura.CastItemGuid.IsEmpty)
				_byCastItemGuid.Remove(aura.CastItemGuid, auraApp.Guid);

			_isSingleTarget.Remove(aura.IsSingleTarget, auraApp.Guid);

			foreach (var label in aura.SpellInfo.Labels)
				_labelMap.Remove(label, auraApp.Guid);

			var casterGuid = aura.CasterGuid;

			if (!casterGuid.IsEmpty)
				_byCasterGuid.Remove(casterGuid);

			var owner = aura.Owner;

			if (owner != null)
				_byOwner.Remove(owner, auraApp.Guid);

			var castId = aura.CastId;

			if (!castId.IsEmpty)
				_byCastId.Remove(castId, auraApp.Guid);

			var flags = auraApp.Flags;

			if (flags.HasFlag(AuraFlags.Negative))
				_hasNegitiveFlag.Remove(auraApp.Guid);

			_isPerm.Remove(aura.IsPermanent, auraApp.Guid);

			foreach (var eff in auraApp.EffectMask)
				_effectIndex.Remove(eff, auraApp.Guid);
		}

		return removed;
	}

	public AuraApplication GetByGuid(Guid guid)
	{
		lock (_auras)
		{
			if (_auras.TryGetValue(guid, out var ret))
				return ret;
		}

		return null;
	}

	public bool TryGetAuraByGuid(Guid guid, out AuraApplication aura)
	{
		lock (_auras)
		{
			return _auras.TryGetValue(guid, out aura);
		}
	}

	public bool Contains(AuraApplication aura)
	{
		lock (_auras)
		{
			return _auras.ContainsKey(aura.Guid);
		}
	}

	public bool Empty()
	{
		lock (_auras)
		{
			return _auras.Count == 0;
		}
	}

	public AuraApplicationQuery Query()
	{
		return new AuraApplicationQuery(this);
	}

	public class AuraApplicationQuery
	{
		readonly AuraApplicationCollection _collection;
		bool _hasLoaded;

		public HashSet<Guid> Results { get; private set; } = new();

		internal AuraApplicationQuery(AuraApplicationCollection auraCollection)
		{
			_collection = auraCollection;
		}

		public AuraApplicationQuery HasSpellId(uint spellId)
		{
			lock (_collection._auras)
			{
				if (_collection._aurasBySpellId.TryGetValue(spellId, out var result))
					Sync(result);
			}

			_hasLoaded = true;

			return this;
		}

		public AuraApplicationQuery HasSpellIds(params uint[] spellId)
		{
			lock (_collection._auras)
			{
				var guids = new HashSet<Guid>[spellId.Length];
				var i = 0;

				foreach (var id in spellId)
					if (_collection._aurasBySpellId.TryGetValue(id, out var auraGuid))
					{
						guids[i] = auraGuid;
						i++;
					}

				SyncOr(guids);
			}

			_hasLoaded = true;

			return this;
		}

		public AuraApplicationQuery HasCasterGuid(ObjectGuid caster)
		{
			lock (_collection._auras)
			{
				if (!caster.IsEmpty && _collection._byCasterGuid.TryGetValue(caster, out var result))
					Sync(result);
			}

			_hasLoaded = true;

			return this;
		}

		public AuraApplicationQuery HasCastItemGuid(ObjectGuid item)
		{
			lock (_collection._auras)
			{
				if (!item.IsEmpty && _collection._byCastItemGuid.TryGetValue(item, out var result))
					Sync(result);
			}

			_hasLoaded = true;

			return this;
		}

		public AuraApplicationQuery HasCastId(ObjectGuid id)
		{
			lock (_collection._auras)
			{
				if (!id.IsEmpty && _collection._byCastId.TryGetValue(id, out var result))
					Sync(result);
			}

			_hasLoaded = true;

			return this;
		}

		public AuraApplicationQuery HasOwner(WorldObject owner)
		{
			lock (_collection._auras)
			{
				if (owner != null && _collection._byOwner.TryGetValue(owner, out var result))
					Sync(result);
			}

			_hasLoaded = true;

			return this;
		}

		public AuraApplicationQuery IsPassiveOrPerm()
		{
			lock (_collection._auras)
			{
				SyncOr(_collection._isPassive.LookupByKey(true), _collection._isPerm.LookupByKey(true));
			}

			_hasLoaded = true;

			return this;
		}

		public AuraApplicationQuery HasLabel(uint label)
		{
			lock (_collection._auras)
			{
				if (_collection._labelMap.TryGetValue(label, out var result))
					Sync(result);
			}

			_hasLoaded = true;

			return this;
		}

		public AuraApplicationQuery HasDiminishGroup(DiminishingGroup group)
		{
			lock (_collection._auras)
			{
				if (_collection._deminishGroup.TryGetValue(group, out var result))
					Sync(result);
			}

			_hasLoaded = true;

			return this;
		}


		public AuraApplicationQuery HasAuraType(AuraObjectType auraType)
		{
			lock (_collection._auras)
			{
				if (_collection._typeMap.TryGetValue(auraType, out var result))
					Sync(result);
			}

			_hasLoaded = true;

			return this;
		}

		public AuraApplicationQuery HasCasterAuraState(AuraStateType state)
		{
			lock (_collection._auras)
			{
				if (_collection._casterAuraState.TryGetValue(state, out var result))
					Sync(result);
			}

			_hasLoaded = true;

			return this;
		}

		public AuraApplicationQuery HasDispelType(DispelType dispellType)
		{
			lock (_collection._auras)
			{
				if (_collection._dispelType.TryGetValue(dispellType, out var result))
					Sync(result);
			}

			_hasLoaded = true;

			return this;
		}

		public AuraApplicationQuery HasSpellFamily(SpellFamilyNames spellFamily)
		{
			lock (_collection._auras)
			{
				if (_collection._spellFamily.TryGetValue(spellFamily, out var result))
					Sync(result);
			}

			_hasLoaded = true;

			return this;
		}

		public AuraApplicationQuery HasEffectIndex(int effectIndex)
		{
			lock (_collection._auras)
			{
				if (_collection._effectIndex.TryGetValue(effectIndex, out var result))
					Sync(result);
			}

			_hasLoaded = true;

			return this;
		}

		public AuraApplicationQuery IsSingleTarget(bool t = true)
		{
			lock (_collection._auras)
			{
				if (_collection._isSingleTarget.TryGetValue(t, out var result))
					Sync(result);
			}

			_hasLoaded = true;

			return this;
		}

		public AuraApplicationQuery IsPositive(bool t = true)
		{
			lock (_collection._auras)
			{
				if (_collection._isPositive.TryGetValue(t, out var result))
					Sync(result);
			}

			_hasLoaded = true;

			return this;
		}

		public AuraApplicationQuery CanBeSaved(bool t = true)
		{
			lock (_collection._auras)
			{
				if (_collection._canBeSaved.TryGetValue(t, out var result))
					Sync(result);
			}

			_hasLoaded = true;

			return this;
		}

		public AuraApplicationQuery IsGroupBuff(bool t = true)
		{
			lock (_collection._auras)
			{
				if (_collection._isgroupBuff.TryGetValue(t, out var result))
					Sync(result);
			}

			_hasLoaded = true;

			return this;
		}

		public AuraApplicationQuery IsPassive(bool t = true)
		{
			lock (_collection._auras)
			{
				if (_collection._isPassive.TryGetValue(t, out var result))
					Sync(result);
			}

			_hasLoaded = true;

			return this;
		}

		public AuraApplicationQuery IsDeathPersistant(bool t = true)
		{
			lock (_collection._auras)
			{
				if (_collection._isDeathPersistant.TryGetValue(t, out var result))
					Sync(result);
			}

			_hasLoaded = true;

			return this;
		}

		public AuraApplicationQuery IsRequiringDeadTarget(bool t = true)
		{
			lock (_collection._auras)
			{
				if (_collection._isRequiringDeadTarget.TryGetValue(t, out var result))
					Sync(result);
			}

			_hasLoaded = true;

			return this;
		}

		public AuraApplicationQuery IsPlayer(bool t = true)
		{
			lock (_collection._auras)
			{
				if (_collection._isPlayer.TryGetValue(t, out var result))
					Sync(result);
			}

			_hasLoaded = true;

			return this;
		}

		public AuraApplicationQuery HasNegitiveFlag(bool t = true)
		{
			lock (_collection._auras)
			{
				Sync(_collection._hasNegitiveFlag, t);
			}

			_hasLoaded = true;

			return this;
		}

		public AuraApplicationQuery IsPermanent(bool t = true)
		{
			lock (_collection._auras)
			{
				if (_collection._isPerm.TryGetValue(t, out var result))
					Sync(result);
			}

			_hasLoaded = true;

			return this;
		}

		public AuraApplicationQuery OnlyIndoors(bool t = true)
		{
			lock (_collection._auras)
			{
				if (_collection._onlyIndoors.TryGetValue(t, out var result))
					Sync(result);
			}

			_hasLoaded = true;

			return this;
		}

		public AuraApplicationQuery OnlyOutdoors(bool t = true)
		{
			lock (_collection._auras)
			{
				if (_collection._onlyOutdoors.TryGetValue(t, out var result))
					Sync(result);
			}

			_hasLoaded = true;

			return this;
		}

		public AuraApplicationQuery Execute(Action<AuraApplication, AuraRemoveMode> action, AuraRemoveMode auraRemoveMode = AuraRemoveMode.Default)
		{
			foreach (var aura in Results)
				if (_collection._auras.TryGetValue(aura, out var result))
					action(result, auraRemoveMode);

			_hasLoaded = true;

			return this;
		}

		public IEnumerable<AuraApplication> GetResults()
		{
			foreach (var aura in Results)
				if (_collection._auras.TryGetValue(aura, out var result))
					yield return result;
		}

		public AuraApplicationQuery ForEachResult(Action<AuraApplication> action)
		{
			foreach (var aura in Results)
				if (_collection._auras.TryGetValue(aura, out var result))
					action(result);

			return this;
		}

		public AuraApplicationQuery AlsoMatches(Func<AuraApplication, bool> predicate)
		{
			if (!_hasLoaded)
				lock (_collection._auras)
				{
					Results = _collection._auras.Keys.ToHashSet();
				}

			Results.RemoveWhere(g =>
			{
				if (_collection._auras.TryGetValue(g, out var result))
					return !predicate(result);

				return true;
			});

			_hasLoaded = true;

			return this;
		}

		private void Sync(HashSet<Guid> collection, bool contains = true)
		{
			if (!_hasLoaded)
			{
				if (collection != null && collection.Count != 0)
					foreach (var a in collection)
						Results.Add(a);
			}
			else if (Results.Count != 0)
			{
				Results.RemoveWhere(r => collection.Contains(r) != contains);
			}
		}

		private void SyncOr(params HashSet<Guid>[] collections)
		{
			if (!_hasLoaded)
			{
				foreach (var collection in collections)
					if (collection != null && collection.Count != 0)
						foreach (var a in collection)
							Results.Add(a);
			}
			else if (Results.Count != 0)
			{
				Results.RemoveWhere(r =>
				{
					var contained = false;

					foreach (var a in collections)
						if (a != null && a.Count != 0 && a.Contains(r))
							contained = true;

					return !contained;
				});
			}
		}
	}
}