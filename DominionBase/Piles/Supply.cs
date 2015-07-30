using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using DominionBase.Cards;
using DominionBase.Players;

namespace DominionBase.Piles
{
	public class BuyCheckEventArgs : EventArgs
	{
		public Player CurrentPlayer;
		public Boolean Cancelled = false;
		public BuyCheckEventArgs(Player player)
		{
			CurrentPlayer = player;
		}
	}

	public class TokensChangedEventArgs : EventArgs
	{
		public Token _Token = null;
		public TokensChangedEventArgs(Token token) { _Token = token; }
	}

	public class CardGainEventArgs : EventArgs
	{
		public Card Card;
		public Boolean Cancelled = false;
		public Dictionary<Type, CardGainAction> Actions = new Dictionary<Type, CardGainAction>();
		public List<Card> HandledBy = new List<Card>();
		public CardGainEventArgs(Card card)
		{
			this.Card = card;
		}
	}
	public delegate void CardGainMethod(Player player, ref CardGainEventArgs cardGainEventArgs);
	public class CardGainAction
	{
		private Player _Player = null;
		private Card _Card = null;
		private String _Text = String.Empty;
		private CardGainMethod _Method = null;
		public Player Player { get { return _Player; } }
		public Card Card { get { return _Card; } }
		public String Text { get { return _Text; } }
		public CardGainMethod Method { get { return _Method; } }
		public CardGainAction(Player player, Card card, String text, CardGainMethod method)
		{
			_Player = player; _Card = card; _Text = text; _Method = method;
		}
	}

	public class Supply : Pile, IComparable<Supply>, ICard
	{
		public delegate void BuyCheckEventHandler(object sender, BuyCheckEventArgs e);
		public virtual event BuyCheckEventHandler BuyCheck = null;
		public delegate void TokensChangedEventHandler(object sender, TokensChangedEventArgs e);
		public virtual event TokensChangedEventHandler TokensChanged = null;

		public override event Pile.PileChangedEventHandler PileChanged;

		private Game _Game = null;
		private HashSet<Type> _CardTypes = new HashSet<Type>();
		private int _StartingStackSize = 0;
		private Type _CardClassType = null;
		private Card _CardBase = null;
		private Cost _LastComputedCost = null;

		private TokenCollection _Tokens = new TokenCollection();

		internal Supply(Game game, PlayerCollection players, Type cardType, int count)
			: base(Visibility.Top, VisibilityTo.All, null, false)
		{
			Init(game, players, cardType, count);
		}

		internal Supply(Game game, PlayerCollection players, Type cardType, Visibility visibility)
			: base(visibility, VisibilityTo.All, null, false)
		{
			Init(game, players, cardType, 0);
		}

		private void Init(Game game, PlayerCollection players, Type cardType, int count)
		{
			if (!cardType.IsSubclassOf(typeof(Card)))
				throw new System.ArgumentException("Type must be a subclass of Card class");

			_Game = game;
			_CardClassType = cardType;
			_CardBase = Card.CreateInstance(_CardClassType);
			_CardBase.AddedToSupply(this._Game, this);
			AddTo(count);

			if (players != null)
			{
				foreach (Player player in players)
					this.AddPlayer(player);
			}
		}

		internal void Empty()
		{
			_CardTypes.Clear();
			_Cards.Clear();
		}

		internal new void Clear()
		{
			this.Empty();
			_CardBase = null;
			_Game = null;
		}

		internal void AddPlayer(Player player)
		{
			player.PhaseChanged += new Player.PhaseChangedEventHandler(player_PhaseChangedEvent);
			player.PlayerModeChanged += new Player.PlayerModeChangedEventHandler(player_PlayerModeChangedEvent);
		}

		internal void RemovePlayer(Player player)
		{
			player.PhaseChanged -= new Player.PhaseChangedEventHandler(player_PhaseChangedEvent);
			player.PlayerModeChanged -= new Player.PlayerModeChangedEventHandler(player_PlayerModeChangedEvent);
		}

		internal override void TearDown()
		{
			base.TearDown();

			_Tokens.TearDown();
			_CardBase.TearDown();
			foreach (Player player in _Game.Players)
				this.RemovePlayer(player);
		}

		public override void EndChanges()
		{
			_AsynchronousChanging = false;
			if (_AsynchronousPileChangedEventArgs != null && PileChanged != null)
			{
				PileChanged(this, _AsynchronousPileChangedEventArgs);
			}
			_AsynchronousPileChangedEventArgs = null;
		}

		void player_PhaseChangedEvent(object sender, PhaseChangedEventArgs e)
		{
			if (_CardBase != null)
				_CardBase.PhaseChanged(sender, e);
			_Cards.PhaseChanged(sender, e);
		}

		void player_PlayerModeChangedEvent(object sender, PlayerModeChangedEventArgs e)
		{
			if (_CardBase != null)
				_CardBase.PlayerModeChanged(sender, e);
			_Cards.PlayerModeChanged(sender, e);
		}

		public int StartingStackSize { get { return _StartingStackSize; } private set { _StartingStackSize = value; } }
		public TokenCollection Tokens { get { return _Tokens; } }
		public void AddToken(Token token)
		{
			_Tokens.Add(token);

			if (TokensChanged != null)
			{
				TokensChangedEventArgs etcea = new TokensChangedEventArgs(token);
				TokensChanged(this, etcea);
			}

			PileChangedEventArgs pcea = new PileChangedEventArgs(PileChangedEventArgs.Operation.Reset);
			if (_AsynchronousChanging)
			{
				_AsynchronousPileChangedEventArgs = pcea;
			}
			else if (PileChanged != null)
			{
				PileChanged(this, pcea);
			}
		}
		public Category Category
		{
			get
			{
				return this.Randomizer.Category;
			}
		}
		public Source Source { get { return this.Randomizer.Source; } }
		public Location Location { get { return this.Randomizer.Location; } }
		public CardBack CardBack { get { return this.Randomizer.CardBack; } }
		public Card Randomizer { get { return _CardBase; } }
		public Card TopCard
		{
			get
			{
				if (this.Count > 0)
					return this.First();
				else
					return null;
			}
		}
		public Type SupplyCardType { get { return CardType; } }
		public Type CardType { get { return this.Randomizer.CardType; } }
		public IEnumerable<Type> CardTypes { get { return _CardTypes; } }
		public CardBenefit Benefit
		{
			get { return _CardBase.Benefit; }
		}

		public Boolean IsEndgameTriggered { get { return this.Randomizer.IsEndgameTriggered(this); } }

		internal void AddTo(IEnumerable<Card> cards)
		{
			foreach (Card card in cards)
				AddTo(card);
		}
		internal void AddTo(Card card)
		{
			if (card.Category == Cards.Category.Unknown)
				throw new Exception("Cannot add card of a different type to Supply pile");
			_Cards.Insert(0, card);
			card.AddedToSupply(this._Game, this);
			if (PileChanged != null)
			{
				lock (PileChanged)
				{
					PileChangedEventArgs pcea = new PileChangedEventArgs(null, PileChangedEventArgs.Operation.Added, new CardCollection() { card });
					PileChanged(this, pcea);
				}
			}
		}

		internal void AddTo(int count)
		{
			if (_CardClassType == null)
				throw new Exception("Cannot use this method without a proper Card Type");
			for (int i = 0; i < count; i++)
				AddTo(Card.CreateInstance(_CardClassType));
		}

		public void Bought(Player player)
		{
			foreach (Token token in _Tokens)
			{
				if (token.Buying(_Game.Table, player))
				{
					_Tokens.Remove(token);
					_Game.Table.TokenPiles.Add(token);
					if (TokensChanged != null)
					{
						TokensChangedEventArgs etcea = new TokensChangedEventArgs(token);
						TokensChanged(this, etcea);
					}
				}
			}
		}

		public Card Take() { return Take(this.TopCard.CardType, 1).ElementAt(0); }
		public IEnumerable<Card> Take(int count) { return Take(this.TopCard.CardType, count); }
		public Card Take(Type cardType) { return Take(cardType, 1).ElementAt(0); }
		public IEnumerable<Card> Take(Type cardType, int count)
		{
			IEnumerable<Card> returnCards = _Cards.FindAll(card => card.CardType == cardType).Take(count);
			if (count == 0)
				return returnCards;
			if (returnCards.Count() == 0)
				throw new Exception("Nothing to take!");

			if (_Game.State != GameState.Setup)
			{
				for (int index = _Tokens.Count - 1; index >= 0; index--)
				{
					Token token = _Tokens[index];
					if (token.Gaining())
					{
						_Tokens.Remove(token);
						_Game.Table.TokenPiles.Add(token);
						if (TokensChanged != null)
						{
							TokensChangedEventArgs etcea = new TokensChangedEventArgs(token);
							TokensChanged(this, etcea);
						}
					}
				}
			}

			_Cards.RemoveAll(c => returnCards.Contains(c));

			if (_Game.State != GameState.Setup)
			{
				if (_AsynchronousChanging)
				{
					if (_AsynchronousPileChangedEventArgs == null)
						_AsynchronousPileChangedEventArgs = new PileChangedEventArgs(null, PileChangedEventArgs.Operation.Removed, returnCards);
					else
						_AsynchronousPileChangedEventArgs.RemovedCards.AddRange(returnCards);
				}
				else if (PileChanged != null)
				{
					PileChangedEventArgs pcea = new PileChangedEventArgs(null, PileChangedEventArgs.Operation.Removed, returnCards);
					PileChanged(this, pcea);
				}
			}

			return returnCards;
		}

		public Cards.Cost BaseCost 
		{
			get
			{
				if (this.TopCard != null)
					return this.TopCard.BaseCost;
				return _CardBase.BaseCost;
			}
		}
		public Cards.Cost CurrentCost
		{
			get
			{
				Cost currentCost = _Game.ComputeCost(_CardBase);
				if (this.TopCard != null)
					currentCost = _Game.ComputeCost(this.TopCard);
				if (_LastComputedCost == (Cost)null)
					_LastComputedCost = currentCost;
				if (_LastComputedCost != currentCost)
				{
					PileChangedEventArgs pcea = new PileChangedEventArgs(PileChangedEventArgs.Operation.Reset);
					if (_AsynchronousChanging)
					{
						_AsynchronousPileChangedEventArgs = pcea;
					}
					else if (PileChanged != null)
					{
						PileChanged(this, pcea);
					}
				}
				_LastComputedCost = currentCost;
				return currentCost;
			}
		}

		public override void Reset()
		{
			base.Reset();

			if (_Tokens.RemoveAll(token => token.IsTemporary) > 0)
			{
				if (TokensChanged != null)
				{
					TokensChangedEventArgs tcea = new TokensChangedEventArgs(null);
					TokensChanged(this, tcea);
				}
			}

			PileChangedEventArgs pcea = new PileChangedEventArgs(PileChangedEventArgs.Operation.Reset);
			if (_AsynchronousChanging)
			{
				_AsynchronousPileChangedEventArgs = pcea;
			}
			else if (PileChanged != null)
			{
				PileChanged(this, pcea);
			}
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			switch (this.Visibility)
			{
				case Visibility.None:
					return String.Format("{0} Cards", Count);
				case Visibility.All:
					sb.Append(_Cards.ToString());
					break;
				case Visibility.Top:
					sb.AppendFormat("{0}: Cost:{1}; {2} Cards", _CardBase, this.CurrentCost, Count);
					break;
				default:
					return "<<Unknown>>";
			}

			return sb.ToString();
		}

		public Boolean CanBuy(Player player)
		{
			return CanBuy(player, player.Currency);
		}

		public Boolean CanBuy(Player player, Currency currency)
		{
			if (this.CanGain() && currency >= this.CurrentCost)
			{
				if (BuyCheck != null)
				{
					BuyCheckEventArgs bcea = new BuyCheckEventArgs(player);
					BuyCheck(this, bcea);
					if (bcea.Cancelled)
						return false;
				}
				return _CardBase.CanBuy(player);
			}

			return false;
		}

		public Boolean CanGain()
		{
			if (this.TopCard == null)
				return false;
			return CanGain(this.TopCard.CardType);
		}

		public Boolean CanGain(Type cardType)
		{
			Card gainCard = _Cards.Find(card => card.CardType == cardType);
			if (gainCard == null)
				return false;

			return this.Count > 0 && gainCard.CanGain();
		}

		public int CompareTo(Supply obj)
		{
			return _CardBase.CompareTo(obj._CardBase);
		}

		public String Text { get { return _CardBase.Text; } }
		public String Name { get { return _CardBase.Name; } }

		internal void FullSetup()
		{
			Setup();
			SnapshotSetup();
			FinalizeSetup();
		}

		internal void Setup()
		{
			if (_CardBase != null)
				_CardBase.Setup(this._Game, this);
		}

		internal void SnapshotSetup()
		{
			if (this.StartingStackSize > 0)
				throw new Exception("Cannot call this method more than once!");

			foreach (Card card in _Cards)
				_CardTypes.Add(card.CardType);
			this.StartingStackSize = this.Count;
		}

		internal void FinalizeSetup()
		{
			if (_CardBase != null)
				_CardBase.Finalize(this._Game, this);
		}

		internal XmlNode GenerateXml(XmlDocument doc)
		{
			XmlElement xeSupply = doc.CreateElement("supply");

			XmlElement xe = doc.CreateElement("type");
			xe.InnerText = this._CardClassType.ToString();
			xeSupply.AppendChild(xe);

			xe = doc.CreateElement("starting_stack_size");
			xe.InnerText = this.StartingStackSize.ToString();
			xeSupply.AppendChild(xe);

			xe = doc.CreateElement("visibility");
			xe.InnerText = this.Visibility.ToString();
			xeSupply.AppendChild(xe);

			xeSupply.AppendChild(this.LookThrough(c => true).GenerateXml(doc, "cards"));

			XmlElement xeTypes = doc.CreateElement("types");
			xeSupply.AppendChild(xeTypes);
			foreach (Type type in this._CardTypes)
			{
				XmlElement xeType = doc.CreateElement("type");
				xeType.InnerText = type.ToString();
				xeTypes.AppendChild(xeType);
			}

			xeSupply.AppendChild(this.Tokens.GenerateXml(doc, "tokens"));

			return xeSupply;
		}

		internal static Supply Load(Game game, XmlNode xnSupply)
		{
			XmlNode xnType = xnSupply.SelectSingleNode("type");

			if (xnType == null)
				return null;

			Type type = Type.GetType(xnType.InnerText);

			Visibility visibility = Visibility.Top;
			XmlNode xnVisibility = xnSupply.SelectSingleNode("visibility");
			if (xnVisibility != null)
				visibility = (Visibility)Enum.Parse(typeof(Visibility), xnVisibility.InnerText, true);

			Supply supply = new Supply(game, null, type, visibility);
			supply.Load(xnSupply);

			return supply;
		}

		internal void Load(XmlNode xnSupply)
		{
			// This needs to be done in reverse order, as AddTo adds cards one at a time on top of the pile instead of the bottom
			foreach (XmlNode xnCard in xnSupply.SelectNodes("cards/card").Cast<XmlNode>().Reverse())
			{
				Type cardType = Type.GetType(xnCard.Attributes["type"].Value);
				this.AddTo(Card.CreateInstance(cardType));
			}

			XmlNode xnSSS = xnSupply.SelectSingleNode("starting_stack_size");
			if (xnSSS != null)
				this.StartingStackSize = int.Parse(xnSSS.InnerText);

			foreach (XmlNode xnType in xnSupply.SelectNodes("types/type"))
			{
				Type type = Type.GetType(xnType.InnerText);
				this._CardTypes.Add(type);
			}

			this.Tokens.AddRange(TokenCollection.Load(xnSupply.SelectSingleNode("tokens")));
		}
	}

	public class SupplyCollection : Dictionary<Type, Supply>
	{
		public SupplyCollection() { }
		public SupplyCollection(IEnumerable<KeyValuePair<Type, Supply>> keyValuePairs)
		{
			foreach (KeyValuePair<Type, Supply> kvp in keyValuePairs)
				this[kvp.Key] = kvp.Value;
		}

		public Supply this[Card card]
		{
			get { return this[card.BaseType]; }
		}

		internal void AddPlayer(Player player)
		{
			List<Supply> supplies = new List<Supply>(this.Values);
			foreach (Supply supply in supplies)
				supply.AddPlayer(player);
		}

		internal void RemovePlayer(Player player)
		{
			List<Supply> supplies = new List<Supply>(this.Values);
			foreach (Supply supply in supplies)
				supply.RemovePlayer(player);
		}

		public int EmptySupplyPiles
		{
			get
			{
				return this.Values.Count(s => s.Count == 0 && (s.Randomizer.Location == Location.General || s.Randomizer.Location == Location.Kingdom));
			}
		}

		public void Reset()
		{
			foreach (Supply supply in this.Values)
				supply.Reset();
		}

		internal void Setup()
		{
			List<Supply> supplies = new List<Supply>(this.Values);
			foreach (Supply supply in supplies)
				supply.Setup();
			foreach (Supply supply in supplies)
				supply.SnapshotSetup();
		}

		internal void FinalizeSetup()
		{
			List<Supply> supplies = new List<Supply>(this.Values);
			foreach (Supply supply in supplies)
				supply.FinalizeSetup();
		}

		public SupplyCollection FindAll(Func<Supply, bool> predicate)
		{
			SupplyCollection supplies = new SupplyCollection();
			foreach (Supply supply in this.Values.Where(predicate))
				supplies[supply.SupplyCardType] = supply;
			return supplies;
		}

		public Boolean ContainsKey(Card card)
		{
			return this.ContainsKey(card.BaseType);
		}

		internal void TearDown()
		{
			foreach (Supply supply in this.Values)
				supply.TearDown();
		}

		internal XmlNode GenerateXml(XmlDocument doc, String nodeName)
		{
			XmlElement xeSupplies = doc.CreateElement(nodeName);
			foreach (KeyValuePair<Type, Supply> supply in this)
				xeSupplies.AppendChild(supply.Value.GenerateXml(doc));

			return xeSupplies;
		}

		internal void Load(Game game, XmlNode xnSupplies)
		{
			if (xnSupplies == null)
				return;

			foreach (XmlNode xnSupply in xnSupplies.SelectNodes("supply"))
			{
				Supply supply = Supply.Load(game, xnSupply);
				this.Add(supply.CardType, supply);
			}
		}
	}
}
