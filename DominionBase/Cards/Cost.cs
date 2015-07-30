using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DominionBase.Cards
{
	[Serializable]
	public class Cost : IComparable
	{
		private Currency _Cost = new Currency();
		private Boolean _Special = false;
		private Boolean _CanOverpay = false;

		public Cost() { }

		public Cost(Currencies.Coin coinCost) { _Cost.Coin = coinCost; }
		public Cost(Currencies.Coin coinCost, Boolean special, Boolean canOverpay)
			: this(coinCost)
		{
			_Special = special;
			_CanOverpay = canOverpay;
		}

		public Cost(Currencies.Potion potionCost) { _Cost.Potion = potionCost; }
		public Cost(Currencies.Potion potionCost, Boolean special, Boolean canOverpay)
			: this(potionCost)
		{
			_Special = special;
			_CanOverpay = canOverpay;
		}

		public Cost(Currencies.Coin coinCost, Currencies.Potion potionCost)
			: this(coinCost)
		{
			_Cost.Potion = potionCost;
		}
		public Cost(Currencies.Coin coinCost, Currencies.Potion potionCost, Boolean special, Boolean canOverpay)
			: this(coinCost, potionCost)
		{
			_Special = special;
			_CanOverpay = canOverpay;
		}

		public Cost(Currency cost) : this(cost.Coin, cost.Potion) { }
		public Cost(Currency cost, Boolean special, Boolean canOverpay) : this(cost.Coin, cost.Potion, special, canOverpay) { }

		public Cost(int coinCost, int potionCost)
		{
			_Cost.Coin = new Currencies.Coin(coinCost);
			_Cost.Potion = new Currencies.Potion(potionCost);
		}
		public Cost(int coinCost, int potionCost, Boolean special, Boolean canOverpay)
			: this(coinCost, potionCost)
		{
			_Special = special;
			_CanOverpay = canOverpay;
		}

		public Cost(int coinCost) { _Cost.Coin = new Currencies.Coin(coinCost); }
		public Cost(int coinCost, Boolean special, Boolean canOverpay)
			: this(coinCost)
		{
			_Special = special;
			_CanOverpay = canOverpay;
		}

		public Cost Clone()
		{
			return new Cost(this._Cost.Coin, this._Cost.Potion, this.Special, this.CanOverpay);
		}

		public Currencies.Coin Coin { get { return _Cost.Coin; } set { _Cost.Coin = value; } }
		public Currencies.Potion Potion { get { return _Cost.Potion; } set { _Cost.Potion = value; } }
		public Boolean Special { get { return _Special; } }
		public Boolean CanOverpay { get { return _CanOverpay; } }

		public override string ToString()
		{
			return ToString(String.Empty);
		}

		public string ToString(String separator)
		{
			String s = this.Coin.ToString();
			if (this.Potion.Value > 0)
			{
				if (this.Coin.Value == 0)
					s = String.Empty;
				else
					s += separator;
				s += this.Potion.ToString();
			}
			return s;
		}

		public override int GetHashCode()
		{
			return this.Coin.GetHashCode() + 15 * this.Potion.GetHashCode();
		}

		public static Cost operator +(Cost x, Cost y)
		{
			Cost c = new Cost(x.Coin, x.Potion);
			c.Coin += y.Coin;
			c.Potion += y.Potion;
			return c;
		}

		public static Cost operator -(Cost x, Cost y)
		{
			Cost c = new Cost(x.Coin, x.Potion);
			c.Coin -= y.Coin;
			c.Potion -= y.Potion;
			return c;
		}

		public static Cost operator +(Cost x, DominionBase.Currency y)
		{
			Cost c = new Cost(x.Coin, x.Potion);
			c.Coin += y.Coin;
			c.Potion += y.Potion;
			return c;
		}

		public static Cost operator -(Cost x, DominionBase.Currency y)
		{
			Cost c = new Cost(x.Coin, x.Potion);
			c.Coin -= y.Coin;
			c.Potion -= y.Potion;
			return c;
		}

		public static Cost operator +(Cost x, DominionBase.Currencies.Coin y)
		{
			Cost c = new Cost(x.Coin, x.Potion);
			c.Coin += y;
			return c;
		}

		public static Cost operator -(Cost x, DominionBase.Currencies.Coin y)
		{
			Cost c = new Cost(x.Coin, x.Potion);
			c.Coin -= y;
			return c;
		}

		public static Cost operator +(Cost x, DominionBase.Currencies.Potion y)
		{
			Cost c = new Cost(x.Coin, x.Potion);
			c.Potion += y;
			return c;
		}

		public static Cost operator -(Cost x, DominionBase.Currencies.Potion y)
		{
			Cost c = new Cost(x.Coin, x.Potion);
			c.Potion -= y;
			return c;
		}

		public static bool operator ==(Cost x, Cost y)
		{
			if (System.Object.ReferenceEquals(x, y))
				return true;
			// If one is null, but not both, return false.
			if (((object)x == null) || ((object)y == null))
				return false;
			return x.Equals(y);
		}

		public static bool operator !=(Cost x, Cost y)
		{
			return !(x == y);
		}

		public static bool operator ==(Cost x, Currency y)
		{
			// If one is null, but not both, return false.
			if (((object)x == null) || ((object)y == null))
				return false;
			return x.Equals(y);
		}

		public static bool operator !=(Cost x, Currency y)
		{
			return !(x == y);
		}

		public static bool operator <(Cost x, Cost y)
		{
			return ((x.Coin < y.Coin) && (x.Potion == y.Potion)) || ((x.Coin == y.Coin) && (x.Potion < y.Potion) || ((x.Coin < y.Coin) && (x.Potion < y.Potion)));
		}

		public static bool operator >(Cost x, Cost y)
		{
			return (y < x);
		}

		public static bool operator <(Cost x, Currency y)
		{
			return ((x.Coin < y.Coin) && (x.Potion == y.Potion)) || ((x.Coin == y.Coin) && (x.Potion < y.Potion) || ((x.Coin < y.Coin) && (x.Potion < y.Potion)));
		}

		public static bool operator >(Cost x, Currency y)
		{
			return (y < x);
		}

		public static bool operator <=(Cost x, Cost y)
		{
			return (x == y || x < y);
		}

		public static bool operator >=(Cost x, Cost y)
		{
			return (x == y || x > y);
		}

		public static bool operator <=(Cost x, Currency y)
		{
			return (x == y || x < y);
		}

		public static bool operator >=(Cost x, Currency y)
		{
			return (x == y || x > y);
		}

		public static bool operator <=(Cost x, Currencies.Coin y)
		{
			return (x.Coin <= y) && (x.Potion <= 0);
		}

		public static bool operator >=(Cost x, Currencies.Coin y)
		{
			return (x.Coin >= y) && (x.Potion >= 0);
		}

		public override bool Equals(object obj)
		{
			if (obj is Cost)
				return this.Coin.Equals(((Cost)obj).Coin) && this.Potion.Equals(((Cost)obj).Potion);
			else if (obj is Currency)
				return this.Coin.Equals(((Currency)obj).Coin) && this.Potion.Equals(((Currency)obj).Potion);
			else
				return false;
		}

		public int CompareTo(object obj)
		{
			return CompareTo(obj as Cost);
		}

		internal int CompareTo(Cost other)
		{
			// Check for null
			if (ReferenceEquals(other, null))
				return -1;

			// Check for same reference
			if (ReferenceEquals(this, other))
				return 0;

			if (this.Coin == other.Coin)
				return this.Potion.CompareTo(other.Potion);
			return this.Coin.CompareTo(other.Coin);
		}
	}
}
