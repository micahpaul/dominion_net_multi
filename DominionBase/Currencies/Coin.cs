using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DominionBase.Currencies
{
	[Serializable]
	public sealed class Coin : CurrencyBase
	{
		public Coin()
			: base()
		{ }

		public Coin(int value)
			: base(value)
		{ }

		public Coin(Coin value)
			: base(value.Value)
		{ }

		public static Coin operator +(Coin x, Coin y)
		{
			Coin newCoin = new Coin(x.Value);
			newCoin.Value += y.Value;
			if (newCoin.Value < 0)
				newCoin.Value = 0;
			return newCoin;
		}

		public static Coin operator +(Coin x, int y)
		{
			Coin newCoin = new Coin(x.Value);
			newCoin.Value += y;
			if (newCoin.Value < 0)
				newCoin.Value = 0;
			return newCoin;
		}

		public static Coin operator -(Coin x, Coin y)
		{
			Coin newCoin = new Coin(x.Value);
			newCoin.Value -= y.Value;
			if (newCoin.Value < 0)
				newCoin.Value = 0;
			return newCoin;
		}

		public static Coin operator -(Coin x, int y)
		{
			return (x + -y);
		}

		public static Coin operator -(Coin c)
		{
			return new Coin() { Value = -c.Value, IsVariable = c.IsVariable };
		}

		public override string ToString()
		{
			return String.Format("<coin>{0}</coin>", base.ToString());
		}
	}
}
