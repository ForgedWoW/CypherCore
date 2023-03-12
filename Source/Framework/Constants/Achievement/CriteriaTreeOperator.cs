// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum CriteriaTreeOperator
{
	Complete = 0,        // Complete
	NotComplete = 1,     // Not Complete
	CompleteAll = 4,     // Complete All
	Sum = 5,             // Sum Of Criteria Is
	Highest = 6,         // Highest Criteria Is
	StartedAtLeast = 7,  // Started At Least
	CompleteAtLeast = 8, // Complete At Least
	ProgressBar = 9      // Progress Bar
}