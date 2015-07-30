using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DominionBase.Players
{
	public enum PhaseEnum
	{
		Waiting,			// Waiting for things to start
		Action,				// Able to play Action cards
		ActionTreasure,		// Able to play Treasure cards during the Action phase
		Buy,				// Able to buy cards from the supply
		BuyTreasure,		// Able to play Treasure cards during the Buy phase
		Cleanup,			// Performing cleanup
		Endgame,			// End of the game
		Starting			// Starting player's turn
	}

	public enum PlayerMode
	{
		Normal, 
		Waiting,
		Choosing,
		Playing,
		Buying
	}
}
