using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using DominionBase.Cards;
using DominionBase.Piles;
using DominionBase.Players;

namespace DominionBase
{
	public class Table : IDisposable
	{
		private Game _Game = null;
		private SupplyCollection _SupplyPiles = new SupplyCollection();
		private SupplyCollection _SpecialPiles = new SupplyCollection();
		private TokenCollections _TokenPiles = new TokenCollections();
		private List<Type> _SupplyKeys = null;
		private Trash _Trash = new Trash();

		private int _NumPlayers = 0;
		private int _BaseVictoryCards = 12;

		public Table(Game game)
		{
			_Game = game;
		}

		public Table(Game game, int numPlayers)
			: this(game)
		{
			_NumPlayers = numPlayers;

			int multiplier = _NumPlayers >= 5 ? 2 : 1;
			Copper = new Supply(game, null, Cards.Universal.TypeClass.Copper, 60 * multiplier);
			Silver = new Supply(game, null, Cards.Universal.TypeClass.Silver, 40 * multiplier);
			Gold = new Supply(game, null, Cards.Universal.TypeClass.Gold, 30 * multiplier);

			int extraProvinceCards = 0;
			switch (_NumPlayers)
			{
				case 1:
				case 2:
					_BaseVictoryCards = 8;
					break;
				case 5:
					extraProvinceCards = 3;
					break;
				case 6:
					extraProvinceCards = 6;
					break;
			}
			Estate = new Supply(game, null, Cards.Universal.TypeClass.Estate, 3 * _NumPlayers + _BaseVictoryCards);
			Duchy = new Supply(game, null, Cards.Universal.TypeClass.Duchy, _BaseVictoryCards);
			Province = new Supply(game, null, Cards.Universal.TypeClass.Province, _BaseVictoryCards + extraProvinceCards);

			Curse = new Supply(game, null, Cards.Universal.TypeClass.Curse, 10 * Math.Max(_NumPlayers - 1, 1));
		}

		public void Clear()
		{
			foreach (Supply supply in this.Supplies.Values)
				supply.Clear();
			this.Supplies.Clear();
			foreach (Supply supply in this.SpecialPiles.Values)
				supply.Clear();
			this.SpecialPiles.Clear();
			this.TokenPiles.Clear();
			this.Trash.Clear();
			this._SupplyKeys = null;

#if DEBUG
			this.Trash.TestFireAllEvents();
#endif
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
					this._Game = null;
					this._SupplyPiles = null;
					this._SpecialPiles = null;
					this._TokenPiles = null;
					this._SupplyKeys = null;
					this._Trash = null;
				}

				// Call the appropriate methods to clean up
				// unmanaged resources here.
				// If disposing is false,
				// only the following code is executed.

				// Note disposing has been done.
				disposed = true;
			}
		}

		~Table()
		{
			Dispose(false);
		}
		#endregion

		public int NumPlayers { get { return _NumPlayers; } }
		public TokenCollections TokenPiles { get { return _TokenPiles; } }

		public Supply Copper { get { return _SupplyPiles[Cards.Universal.TypeClass.Copper]; } private set { _SupplyPiles[Cards.Universal.TypeClass.Copper] = value; } }
		public Supply Silver { get { return _SupplyPiles[Cards.Universal.TypeClass.Silver]; } private set { _SupplyPiles[Cards.Universal.TypeClass.Silver] = value; } }
		public Supply Gold { get { return _SupplyPiles[Cards.Universal.TypeClass.Gold]; } private set { _SupplyPiles[Cards.Universal.TypeClass.Gold] = value; } }
		//public Supply Platinum { get { return _SupplyPiles[Cards.Prosperity.TypeClass.Platinum]; } private set { _SupplyPiles[Cards.Prosperity.TypeClass.Platinum] = value; } }
		public Supply Potion { get { return _SupplyPiles[Cards.Alchemy.TypeClass.Potion]; } private set { _SupplyPiles[Cards.Alchemy.TypeClass.Potion] = value; } }
		public Supply Estate { get { return _SupplyPiles[Cards.Universal.TypeClass.Estate]; } private set { _SupplyPiles[Cards.Universal.TypeClass.Estate] = value; } }
		public Supply Duchy { get { return _SupplyPiles[Cards.Universal.TypeClass.Duchy]; } private set { _SupplyPiles[Cards.Universal.TypeClass.Duchy] = value; } }
		public Supply Province { get { return _SupplyPiles[Cards.Universal.TypeClass.Province]; } private set { _SupplyPiles[Cards.Universal.TypeClass.Province] = value; } }
		//public Supply Colony { get { return _SupplyPiles[Cards.Prosperity.TypeClass.Colony]; } private set { _SupplyPiles[Cards.Prosperity.TypeClass.Colony] = value; } }
		public Supply Curse { get { return _SupplyPiles[Cards.Universal.TypeClass.Curse]; } private set { _SupplyPiles[Cards.Universal.TypeClass.Curse] = value; } }
		public Supply FindSupplyPileByCardType(Type cardType, Boolean includeSpecialPiles)
		{
			if (this.Supplies.ContainsKey(cardType))
				return this.Supplies[cardType];
			if (includeSpecialPiles && this.SpecialPiles.ContainsKey(cardType))
				return this.SpecialPiles[cardType];
			SupplyCollection sc = this.Supplies.FindAll(s => s.TopCard != null && s.TopCard.CardType == cardType);
			if (sc.Count == 1)
				return sc.ElementAt(0).Value;
			if (includeSpecialPiles)
			{
				sc = this.SpecialPiles.FindAll(s => s.TopCard != null && s.TopCard.CardType == cardType);
				if (sc.Count == 1)
					return sc.ElementAt(0).Value;
			}
			return null;
		}
		public Supply FindSupplyPileByCard(Cards.Card card)
		{
			if (this.Supplies.ContainsKey(card))
				return this.Supplies[card];
			SupplyCollection sc = this.Supplies.FindAll(s => s.TopCard != null && s.TopCard.Name == card.Name);
			if (sc.Count == 1)
				return sc.ElementAt(0).Value;
			return null;
		}
		public Supply this[Type cardType] { get { return _SupplyPiles[cardType]; } }
		public Supply this[Cards.Card card] { get { return _SupplyPiles[card]; } }
		public SupplyCollection Supplies { get { return _SupplyPiles; } }
		public List<Type> SupplyKeysOrdered
		{
			get
			{
				if (_SupplyKeys == null)
				{
					_SupplyKeys = new List<Type>(Supplies.Keys);
					_SupplyKeys.Sort(delegate(Type t1, Type t2) { return _SupplyPiles[t1].CompareTo(_SupplyPiles[t2]); });
				}
				return _SupplyKeys;
			}
		}

		public SupplyCollection SpecialPiles { get { return _SpecialPiles; } }
		public Trash Trash { get { return _Trash; } }

		public void AddKingdomSupply(PlayerCollection players, Type cardType)
		{
			// Minimum required -- Need to do this first to figure out what kind of card it is (silly, I know)
			Supply newSupply = new Supply(this._Game, players, cardType, 8);
			if ((newSupply.Category & Category.Victory) == Category.Victory)
			{
				// Victory supply piles should be 12 in 3+ player games
				if (NumPlayers > 2)
					newSupply.AddTo(_BaseVictoryCards - 8);
			}
			else  // Not a Victory card, so there should be 10
			{
				newSupply.AddTo(2);
			}

			_SupplyPiles[cardType] = newSupply;
			_SupplyKeys = null;
		}

		internal void Reset()
		{
			_SupplyPiles.Reset();
		}

		internal void AddPlayer(Player player)
		{
			this.Supplies.AddPlayer(player);
			this.SpecialPiles.AddPlayer(player);
		}

		internal void RemovePlayer(Player player)
		{
			this.Supplies.RemovePlayer(player);
			this.SpecialPiles.RemovePlayer(player);
		}

		internal void SetupSupplies(Game game)
		{
			// Check for addition of Platinum/Colony
			Boolean useColonyPlatinum = false;
			switch (game.Settings.ColonyPlatinumUsage)
			{
				case ColonyPlatinumUsage.Standard:
				case ColonyPlatinumUsage.Used:
				case ColonyPlatinumUsage.NotUsed:
					if (game.RNG.Next(1, _SupplyPiles.Values.Count(s => s.Location == Location.Kingdom) + 1) <=
						_SupplyPiles.Values.Count(s => s.Location == Location.Kingdom && s.Source == Source.Prosperity))
						// We have a winner!
						useColonyPlatinum = true;
					break;

				case ColonyPlatinumUsage.Always:
					useColonyPlatinum = true;
					break;
			}
			if (useColonyPlatinum)
			{
				this.Supplies[Cards.Prosperity.TypeClass.Platinum] = new Supply(game, null, Cards.Prosperity.TypeClass.Platinum, 12);
				this.Supplies[Cards.Prosperity.TypeClass.Colony] = new Supply(game, null, Cards.Prosperity.TypeClass.Colony, _BaseVictoryCards);
				game.Settings.ColonyPlatinumUsage = ColonyPlatinumUsage.Used;
			}
			else
			{
				game.Settings.ColonyPlatinumUsage = ColonyPlatinumUsage.NotUsed;
			}

			// Check for addition of Shelters
			Boolean useShelter = false;
			switch (game.Settings.ShelterUsage)
			{
				case ShelterUsage.Standard:
				case ShelterUsage.Used:
				case ShelterUsage.NotUsed:
					if (game.RNG.Next(1, _SupplyPiles.Values.Count(s => s.Location == Location.Kingdom) + 1) <=
						_SupplyPiles.Values.Count(s => s.Location == Location.Kingdom && s.Source == Source.DarkAges))
						// We have a winner!
						useShelter = true;
					break;

				case ShelterUsage.Always:
					useShelter = true;
					break;
			}
			if (useShelter)
			{
				this.Estate.Take(3 * game.Players.Count);
				this.Supplies[Cards.DarkAges.TypeClass.Shelters] = new Supply(game, null, Cards.DarkAges.TypeClass.Shelters, Visibility.Top);

				game.Settings.ShelterUsage = ShelterUsage.Used;
			}
			else
			{
				game.Settings.ShelterUsage = ShelterUsage.NotUsed;
			}

			_SupplyPiles.Setup();

			foreach (Supply supply in _SupplyPiles.Values.Concat(SpecialPiles.Values))
			{
				if (supply.CurrentCost.Potion.Value > 0 && !_SupplyPiles.ContainsKey(Cards.Alchemy.TypeClass.Potion))
				{
					Potion = new Supply(game, game.Players, Cards.Alchemy.TypeClass.Potion, 16);
					break;
				}

				if (supply.CardTypes.Count() > 1)
				{
					foreach (Type cardType in supply.CardTypes)
					{
						Card card = Card.CreateInstance(cardType);
						if (game.ComputeCost(card).Potion.Value > 0 && !_SupplyPiles.ContainsKey(Cards.Alchemy.TypeClass.Potion))
						{
							Potion = new Supply(game, game.Players, Cards.Alchemy.TypeClass.Potion, 16);
							break;
						}
					}
				}

				if (_SupplyPiles.ContainsKey(Cards.Alchemy.TypeClass.Potion))
					break;
			}
		}

		internal void FinalizeSupplies(Game game)
		{
			_SupplyPiles.FinalizeSetup();
		}

		internal void TearDown()
		{
			this.Supplies.TearDown();
			this.SpecialPiles.TearDown();
			this.TokenPiles.TearDown();
			this.Trash.TearDown();
		}

		internal XmlNode GenerateXml(XmlDocument doc, String nodeName)
		{
			XmlElement xeTable = doc.CreateElement(nodeName);

			xeTable.AppendChild(this.Supplies.GenerateXml(doc, "supplies"));
			xeTable.AppendChild(this.SpecialPiles.GenerateXml(doc, "specials"));

			xeTable.AppendChild(this.TokenPiles.GenerateXml(doc, "tokenpiles"));

			xeTable.AppendChild(this.Trash.GenerateXml(doc));

			return xeTable;
		}

		internal void Load(XmlNode xnTable)
		{
			this.Supplies.Load(this._Game, xnTable.SelectSingleNode("supplies"));
			this.SpecialPiles.Load(this._Game, xnTable.SelectSingleNode("specials"));

			this.TokenPiles.Load(xnTable.SelectSingleNode("tokenpiles"));

			this.Trash.Load(xnTable);
		}
	}
}
