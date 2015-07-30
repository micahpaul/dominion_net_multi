using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DominionBase.Currencies
{
	[Serializable]
	public sealed class Potion : CurrencyBase
	{
		public Potion()
			: base()
		{ }

		public Potion(int value)
			: base(value)
		{ }

		public Potion(Potion value)
			: base(value.Value)
		{ }

		public static Potion operator +(Potion x, Potion y)
		{
			Potion newPotion = new Potion(x.Value);
			newPotion.Value += y.Value;
			if (newPotion.Value < 0)
				newPotion.Value = 0;
			return newPotion;
		}

		public static Potion operator +(Potion x, int y)
		{
			Potion newPotion = new Potion(x.Value);
			newPotion.Value += y;
			if (newPotion.Value < 0)
				newPotion.Value = 0;
			return newPotion;
		}

		public static Potion operator -(Potion x, Potion y)
		{
			Potion newPotion = new Potion(x.Value);
			newPotion.Value -= y.Value;
			if (newPotion.Value < 0)
				newPotion.Value = 0;
			return newPotion;
		}

		public static Potion operator -(Potion x, int y)
		{
			return (x + -y);
		}

		public static Potion operator -(Potion c)
		{
			return new Potion() { Value = -c.Value, IsVariable = c.IsVariable };
		}

		public override string ToString()
		{
			return String.Format("<potion>{0}</potion>", base.ToString());
		}
	}
}
