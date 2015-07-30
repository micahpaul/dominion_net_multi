using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DominionBase
{
	[Serializable]
	public class Currency : IDisposable
	{
		private static Regex _RegexParser = new Regex("(<coin>(?<coinValue>\\d+)</coin>)?(<potion>(?<potionValue>\\d+)</potion>)?");
		private Currencies.Coin _Coin = new Currencies.Coin();
		private Currencies.Potion _Potion = new Currencies.Potion();

		public Currency() { }
		public Currency(Currency currency)
			: this(currency.Coin, currency.Potion)
		{ }
		public Currency(Cards.Cost cost)
			: this(cost.Coin, cost.Potion)
		{ }
		public Currency(Currencies.Coin coin, Currencies.Potion potion)
		{
			_Coin.Value = coin.Value;
			_Potion.Value = potion.Value;
		}

		public Currency(int coin, int potion)
		{
			_Coin.Value = coin;
			_Potion.Value = potion;
		}

		public Currency(int coin)
		{
			_Coin.Value = coin;
		}

		public Currency(String currency)
		{
			Match matched = _RegexParser.Match(currency);
			if (!matched.Success)
				throw new ArgumentException("Could not parse currency!");
			if (matched.Groups["coinValue"].Success)
				_Coin.Value = int.Parse(matched.Groups["coinValue"].Value);
			if (matched.Groups["potionValue"].Success)
				_Potion.Value = int.Parse(matched.Groups["potionValue"].Value);
		}

		#region IDisposable variables, properties, & methods
		// Track whether Dispose has been called.
		private bool disposed = false;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			// Check to see if Dispose has already been called.
			if (!this.disposed)
			{
				// If disposing equals true, dispose all managed
				// and unmanaged resources.
				if (disposing)
				{
					// Dispose managed resources.
					_Coin = null;
					_Potion = null;
				}

				// Call the appropriate methods to clean up
				// unmanaged resources here.
				// If disposing is false,
				// only the following code is executed.

				// Note disposing has been done.
				disposed = true;
			}
		}

		~Currency()
		{
			Dispose(false);
		}
		#endregion

		public Currencies.Coin Coin
		{
			get { return _Coin; }
			set { _Coin = value; }
		}

		public Currencies.Potion Potion
		{
			get { return _Potion; }
			set { _Potion = value; }
		}

		public Boolean IsVariable
		{
			get { return this.Coin.IsVariable || this.Potion.IsVariable; }
		}

		public Boolean IsBlank
		{
			get { return this.Coin.Value == 0 && this.Potion.Value == 0; }
		}

		internal void Add(Currency currency)
		{
			_Coin += currency.Coin;
			_Potion += currency.Potion;
		}

		public static Currency operator +(Currency x, Currency y)
		{
			Currency newCurrency = new Currency(x);
			newCurrency.Coin += y.Coin;
			newCurrency.Potion += y.Potion;
			return newCurrency;
		}

		public static Currency operator +(Currency x, Cards.Cost y)
		{
			Currency newCurrency = new Currency(x);
			newCurrency.Coin += y.Coin;
			newCurrency.Potion += y.Potion;
			return newCurrency;
		}

		public static Currency operator +(Currency x, Currencies.Coin y)
		{
			Currency newCurrency = new Currency(x);
			newCurrency.Coin += y;
			return newCurrency;
		}

		public static Currency operator +(Currency x, Currencies.Potion y)
		{
			Currency newCurrency = new Currency(x);
			newCurrency.Potion += y;
			return newCurrency;
		}

		public static Currency operator -(Currency x, Currency y)
		{
			Currency newCurrency = new Currency(x);
			newCurrency.Coin -= y.Coin;
			newCurrency.Potion -= y.Potion;
			return newCurrency;
		}

		public static Currency operator -(Currency x, Cards.Cost y)
		{
			Currency newCurrency = new Currency(x);
			newCurrency.Coin -= y.Coin;
			newCurrency.Potion -= y.Potion;
			return newCurrency;
		}

		public static Currency operator -(Currency x, Currencies.Coin y)
		{
			Currency newCurrency = new Currency(x);
			newCurrency.Coin -= y;
			return newCurrency;
		}

		public static Currency operator -(Currency x, Currencies.Potion y)
		{
			Currency newCurrency = new Currency(x);
			newCurrency.Potion -= y;
			return newCurrency;
		}

		public static Currency operator -(Currency c)
		{
			return new Currency(-c.Coin, -c.Potion);
		}

		public override int GetHashCode()
		{
			return this.Coin.GetHashCode() + 15 * this.Potion.GetHashCode();
		}

		public static bool operator ==(Currency x, Currency y)
		{
			if (System.Object.ReferenceEquals(x, y))
				return true;
			// If one is null, but not both, return false.
			if (((object)x == null) || ((object)y == null))
				return false;
			return x.Equals(y);
		}

		public static bool operator !=(Currency x, Currency y)
		{
			return !(x == y);
		}

		public static bool operator ==(Currency x, Cards.Cost y)
		{
			// If one is null, but not both, return false.
			if (((object)x == null) || ((object)y == null))
				return false;
			return x.Equals(y);
		}

		public static bool operator !=(Currency x, Cards.Cost y)
		{
			return !(x == y);
		}

		public static bool operator <(Currency x, Currency y)
		{
			return (x.Coin < y.Coin && x.Potion == y.Potion) || (x.Coin == y.Coin && x.Potion < y.Potion) || (x.Coin < y.Coin && x.Potion < y.Potion);
		}

		public static bool operator >(Currency x, Currency y)
		{
			return (y < x);
		}

		public static bool operator <=(Currency x, Currency y)
		{
			return (x == y || x < y);
		}

		public static bool operator >=(Currency x, Currency y)
		{
			return (x == y || x > y);
		}

		public static bool operator <(Currency x, Cards.Cost y)
		{
			return (x.Coin < y.Coin && x.Potion == y.Potion) || (x.Coin == y.Coin && x.Potion < y.Potion) || (x.Coin < y.Coin && x.Potion < y.Potion);
		}

		public static bool operator >(Currency x, Cards.Cost y)
		{
			return (y < x);
		}

		public static bool operator <=(Currency x, Cards.Cost y)
		{
			return (x == y || x < y);
		}

		public static bool operator >=(Currency x, Cards.Cost y)
		{
			return (x == y || x > y);
		}

		public override bool Equals(object obj)
		{
			if (obj is Cards.Cost)
				return this.Coin.Equals(((Cards.Cost)obj).Coin) && this.Potion.Equals(((Cards.Cost)obj).Potion);
			else if (obj is Currency)
				return this.Coin.Equals(((Currency)obj).Coin) && this.Potion.Equals(((Currency)obj).Potion);
			else
				return false;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat("<b>{0}</b><coin/>", _Coin.Value);

			if (_Potion.Value > 0)
				sb.AppendFormat(", <b>{0}</b><potion/>", _Potion.Value);
			return sb.ToString();
		}

		public string ToStringInline()
		{
			String s = Coin.ToString();

			if (Potion.Value > 0)
			{
				if (Coin.Value == 0)
					s = String.Empty;
				s += Potion.ToString();
			}
			return s;
		}
	}
}
