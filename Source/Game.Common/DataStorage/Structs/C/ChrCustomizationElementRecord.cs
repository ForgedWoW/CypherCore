using Game.DataStorage;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.C;

public sealed class ChrCustomizationElementRecord
{
	public uint Id;
	public uint ChrCustomizationChoiceID;
	public int RelatedChrCustomizationChoiceID;
	public int ChrCustomizationGeosetID;
	public int ChrCustomizationSkinnedModelID;
	public int ChrCustomizationMaterialID;
	public int ChrCustomizationBoneSetID;
	public int ChrCustomizationCondModelID;
	public int ChrCustomizationDisplayInfoID;
	public int ChrCustItemGeoModifyID;
	public int ChrCustomizationVoiceID;
}
