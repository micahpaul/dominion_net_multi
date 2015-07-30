using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DominionBase.Cards
{
	public class CardBenefit
	{
		private int _Cards = 0;
		private int _Actions = 0;
		private int _Buys = 0;
		private Currency _Currency = new Currency();
		private int _VictoryPoints = 0;
		private Boolean _IsTreasure = false;
		private String _FlavorText = String.Empty;

		public CardBenefit() { }
		public CardBenefit(Boolean isTreasure)
		{
			_IsTreasure = isTreasure;
		}

		public int Cards
		{
			get { return _Cards; }
			internal set { _Cards = value; }
		}

		public int Actions
		{
			get { return _Actions; }
			internal set { _Actions = value; }
		}

		public int Buys
		{
			get { return _Buys; }
			internal set { _Buys = value; }
		}

		public Currency Currency
		{
			get { return _Currency; }
			internal set { _Currency = value; }
		}

		public int VictoryPoints
		{
			get { return _VictoryPoints; }
			internal set { _VictoryPoints = value; }
		}

		public String FlavorText
		{
			get { return _FlavorText; }
			internal set { _FlavorText = value; }
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as CardBenefit);
		}

		public bool Equals(CardBenefit other)
		{
			// Check for null
			if (ReferenceEquals(other, null))
				return false;

			// Check for same reference
			if (ReferenceEquals(this, other))
				return true;

			return _Cards.Equals(other.Cards) &&
				_Actions.Equals(other.Actions) &&
				_Buys.Equals(other.Buys) &&
				_Currency.Equals(other.Currency) &&
				_VictoryPoints.Equals(other.VictoryPoints) &&
				_IsTreasure.Equals(other._IsTreasure);
		}

		public override int GetHashCode()
		{
			int hash = this.Cards;
			hash = hash * 7 + this.Actions;
			hash = hash * 7 + this.Buys;
			hash = hash * 7 + this.Currency.GetHashCode();
			hash = hash * 7 + this.VictoryPoints;
			hash = hash * 7 + (this._IsTreasure ? 1 : 0);
			return hash;
		}

		public String Text
		{
			get
			{
				List<String> lines = new List<string>();
				StringBuilder sb = new StringBuilder();
				if (_IsTreasure)
				{
					if (!_Currency.Coin.IsVariable && (_Currency.Coin.Value > 0 || _Currency.Potion.Value == 0))
						lines.Add(String.Format("<coinlg>{0}</coinlg>", _Currency.Coin.Value));
					if (!_Currency.Potion.IsVariable && _Currency.Potion.Value > 0)
						lines.Add(String.Format("<potionlg>{0}</potionlg>", _Currency.Potion.Value == 1 ? "" : _Currency.Potion.Value.ToString()));
				}
				if (_Cards > 0)
					lines.Add(String.Format("<b>+{0} Card{1}</b>", _Cards, _Cards == 1 ? "" : "s"));
				if (_Actions > 0)
					lines.Add(String.Format("<b>+{0} Action{1}</b>", _Actions, _Actions == 1 ? "" : "s"));
				if (_Buys > 0)
					lines.Add(String.Format("<b>+{0} Buy{1}</b>", _Buys, _Buys == 1 ? "" : "s"));
				if (!_IsTreasure)
				{
					if (_Currency.Coin.Value > 0)
						lines.Add(String.Format("<b>+</b><coin>{0}</coin>", _Currency.Coin.Value));
					if (_Currency.Potion.Value > 0)
						lines.Add(String.Format("<b>+</b><potion>{0}</potion>", _Currency.Potion.Value == 1 ? "" : _Currency.Potion.Value.ToString()));
				}
				if (_Cards < 0)
					lines.Add(String.Format("<b>Discard {0} Card{1}</b>", -_Cards, (-_Cards) == 1 ? "" : "s"));
				if (_VictoryPoints > 0)
				{
					lines.Add(String.Format("<b>+{0}</b><vp/>", _VictoryPoints));
				}
				sb.Append(String.Join(System.Environment.NewLine, lines.ToArray()));
				return sb.ToString();
			}
		}

		public Boolean Any
		{
			get { return _Cards != 0 || _Actions != 0 || _Buys != 0 || _Currency.Coin.Value != 0 || _Currency.Potion.Value != 0 || _VictoryPoints != 0; }
		}
	}
}
