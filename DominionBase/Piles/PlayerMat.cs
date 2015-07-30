using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DominionBase.Cards;
using DominionBase.Players;

namespace DominionBase.Piles
{
	public class DecksChangedEventArgs : EventArgs
	{
		public enum Operation
		{
			Reset,
			Added,
			Removed
		}
		private Player _Player = null;
		private CardCollection _AddedCards;
		private CardCollection _RemovedCards;
		private Operation _OperationPerformed;
		public Deck Deck;
		public Player Player { get { return _Player; } }
		public CardCollection AddedCards { get { return _AddedCards; } }
		public CardCollection RemovedCards { get { return _RemovedCards; } }
		public Operation OperationPerformed { get { return _OperationPerformed; } }
		public DecksChangedEventArgs(Deck deck, Operation operation)
		{
			this.Deck = deck;
			_OperationPerformed = operation;
			_AddedCards = new CardCollection();
			_RemovedCards = new CardCollection();
		}
		public DecksChangedEventArgs(Deck deck, Player player, Operation operation)
			: this(deck, operation)
		{
			_Player = player;
		}
		public DecksChangedEventArgs(Deck deck, Player player, Operation operation, Card cardChanged)
			: this(deck, player, operation)
		{
			switch (operation)
			{
				case DecksChangedEventArgs.Operation.Added:
					_AddedCards.Add(cardChanged);
					break;
				case DecksChangedEventArgs.Operation.Removed:
					_RemovedCards.Add(cardChanged);
					break;
			}
		}
		public DecksChangedEventArgs(Deck deck, Player player, Operation operation, IEnumerable<Card> cardsChanged)
			: this(deck, player, operation)
		{
			switch (operation)
			{
				case DecksChangedEventArgs.Operation.Added:
					_AddedCards.AddRange(cardsChanged);
					break;
				case DecksChangedEventArgs.Operation.Removed:
					_RemovedCards.AddRange(cardsChanged);
					break;
			}
		}
	}

	public class CardMat : Deck
	{
		private Boolean _IsObtainable = true;

		public Boolean IsObtainable
		{
			get { return _IsObtainable; }
			set { _IsObtainable = value; }
		}

		public CardMat()
			: base(Players.DeckLocation.PlayerMat)
		{
		}

		public CardMat(Visibility visibility, VisibilityTo visibilityTo)
			: base(Players.DeckLocation.PlayerMat, visibility, visibilityTo)
		{
		}

		public CardMat(Visibility visibility, VisibilityTo visibilityTo, IComparer<Cards.Card> comparer, Boolean collate)
			: base(Players.DeckLocation.PlayerMat, visibility, visibilityTo, comparer, collate)
		{
		}

		public static CardMat CreateInstance(Type type)
		{
			return (CardMat)type.GetConstructor(Type.EmptyTypes).Invoke(null);
		}
	}

	public class CardMats : Dictionary<Type, CardMat>
	{
		public delegate void DecksChangedEventHandler(object sender, DecksChangedEventArgs e);
		public event DecksChangedEventHandler DecksChanged;

		public void Add(Player player, Type cardMatType, IEnumerable<Card> cards)
		{
			if (!this.ContainsKey(cardMatType))
				this[cardMatType] = CardMat.CreateInstance(cardMatType);
			this[cardMatType].AddRange(player, cards);

			if (DecksChanged != null)
			{
				DecksChangedEventArgs pcea = new DecksChangedEventArgs(this[cardMatType], player, DecksChangedEventArgs.Operation.Added, cards);
				DecksChanged(this, pcea);
			}
		}

		public CardCollection Retrieve(Player player, Type deckType, Predicate<Card> match, int count)
		{
			Deck d = null;
			if (this.ContainsKey(deckType))
				d = this[deckType];
			else
				d = Deck.CreateInstance(deckType);

			return d.Retrieve(player, match, count);
		}
	}
}
