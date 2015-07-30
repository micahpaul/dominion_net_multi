using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DominionBase.Players
{
	public class Human : Player
	{
		public Human(Game game, String name)
			: base(game, name)
		{
			_PlayerType = PlayerType.Human;
		}
	}
}
