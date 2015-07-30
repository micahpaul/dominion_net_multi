using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DominionBase.Currencies
{
	[Serializable]
	public class CurrencyBase : IComparable
	{
		private int _Value = 0;
		private Boolean _IsVariable = false;

		public int Value
		{
			get { return _Value; }
			set { _Value = value; }
		}

		public Boolean IsVariable
		{
			get { return _IsVariable; }
			set { _IsVariable = value; }
		}

		public CurrencyBase()
		{ }

		public CurrencyBase(int value)
		{
			Value = value;
		}

		public CurrencyBase(int value, Boolean isVariable)
			: this(value)
		{
			IsVariable = isVariable;
		}

		public static CurrencyBase operator +(CurrencyBase x, CurrencyBase y)
		{
			if (x.IsVariable || y.IsVariable)
				throw new ArithmeticException("Cannot add variable Currency!");

			CurrencyBase cb = new CurrencyBase(x.Value);
			cb.Value += y.Value;
			if (cb.Value < 0)
				cb.Value = 0;
			return cb;
		}

		public override string ToString()
		{
			if (IsVariable)
				return "?";
			else
				return Value.ToString();
		}

		public override bool Equals(object obj)
		{
			return this.Value.Equals(((CurrencyBase)obj).Value) && this.IsVariable.Equals(((CurrencyBase)obj).IsVariable);
		}

		public override int GetHashCode()
		{
			return this.Value.GetHashCode() + 7 * (IsVariable ? 1 : 0);
		}

		public static bool operator ==(CurrencyBase x, CurrencyBase y)
		{
			if (System.Object.ReferenceEquals(x, y))
				return true;
			// If one is null, but not both, return false.
			if (((object)x == null) || ((object)y == null))
				return false;
			return x.Equals(y);
		}

		public static bool operator !=(CurrencyBase x, CurrencyBase y)
		{
			return !(x == y);
		}

		public static bool operator ==(CurrencyBase x, int y)
		{
			return x.Value.Equals(y);
		}

		public static bool operator !=(CurrencyBase x, int y)
		{
			return !x.Value.Equals(y);
		}

		public static bool operator <(CurrencyBase x, CurrencyBase y)
		{
			return x.Value < y.Value;
		}

		public static bool operator >(CurrencyBase x, CurrencyBase y)
		{
			return x.Value > y.Value;
		}

		public static bool operator <=(CurrencyBase x, CurrencyBase y)
		{
			return x.Value <= y.Value;
		}

		public static bool operator >=(CurrencyBase x, CurrencyBase y)
		{
			return x.Value >= y.Value;
		}

		public static bool operator <(CurrencyBase x, int y)
		{
			return x.Value < y;
		}

		public static bool operator >(CurrencyBase x, int y)
		{
			return x.Value > y;
		}

		public static bool operator <=(CurrencyBase x, int y)
		{
			return x.Value <= y;
		}

		public static bool operator >=(CurrencyBase x, int y)
		{
			return x.Value >= y;
		}

		public static CurrencyBase operator -(CurrencyBase cb)
		{
			return new CurrencyBase() { Value = -cb.Value, IsVariable = cb.IsVariable };
		}

		public int CompareTo(object obj)
		{
			return CompareTo(obj as CurrencyBase);
		}

		public int CompareTo(CurrencyBase other)
		{
			// Check for null
			if (ReferenceEquals(other, null))
				return -1;

			// Check for same reference
			if (ReferenceEquals(this, other))
				return 0;

			if (this.IsVariable != other.IsVariable)
				return this.IsVariable.CompareTo(other.IsVariable);

			return this.Value.CompareTo(other.Value);
		}
	}
}
