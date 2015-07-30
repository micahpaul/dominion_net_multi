using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DominionBase.Piles;

namespace DominionBase.Cards.Universal
{
	public static class TypeClass
	{
		public static Type Dummy = typeof(Dummy);

		public static Type Copper = typeof(Copper);
		public static Type Silver = typeof(Silver);
		public static Type Gold = typeof(Gold);
		public static Type Estate = typeof(Estate);
		public static Type Duchy = typeof(Duchy);
		public static Type Province = typeof(Province);
		public static Type Curse = typeof(Curse);
	}

	public class Dummy : Card
	{
		public Dummy()
			: base("Dummy", Category.Unknown, Source.All, Location.Invisible, Group.None)
		{
		}
	}

	public class DummyRed : Card
	{
		public DummyRed()
			: base("DummyRed", Category.Unknown, Source.All, Location.Invisible, Group.None, CardBack.Red)
		{
		}
	}

	public class Copper : Card
	{
		public Copper()
			: base("Copper", Category.Treasure, Source.All, Location.General)
		{
			this.Benefit.Currency.Coin.Value = 1;
		}

		protected override Boolean AllowUndo { get { return true; } }
	}
	public class Silver : Card
	{
		public Silver()
			: base("Silver", Category.Treasure, Source.All, Location.General)
		{
			this.BaseCost = new Cost(3);
			this.Benefit.Currency.Coin.Value = 2;
		}

		protected override Boolean AllowUndo { get { return true; } }
	}
	public class Gold : Card
	{
		public Gold()
			: base("Gold", Category.Treasure, Source.All, Location.General)
		{
			this.BaseCost = new Cost(6);
			this.Benefit.Currency.Coin.Value = 3;
		}

		protected override Boolean AllowUndo { get { return true; } }
	}
	public class Curse : Card
	{
		public Curse()
			: base("Curse", Category.Curse, Source.All, Location.General)
		{
			this.VictoryPoints = -1;
		}
	}
	public class Estate : Card
	{
		public Estate()
			: base("Estate", Category.Victory, Source.All, Location.General)
		{
			this.BaseCost = new Cost(2);
			this.VictoryPoints = 1;
		}
	}
	public class Duchy : Card
	{
		public Duchy()
			: base("Duchy", Category.Victory, Source.All, Location.General)
		{
			this.BaseCost = new Cost(5);
			this.VictoryPoints = 3;
		}
	}
	public class Province : Card
	{
		public Province()
			: base("Province", Category.Victory, Source.All, Location.General)
		{
			this.BaseCost = new Cost(8);
			this.VictoryPoints = 6;
		}

		public override Boolean IsEndgameTriggered(Supply supply)
		{
			return (supply.Count == 0);
		}
	}
}
