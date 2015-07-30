using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using DominionBase.Cards;
using DominionBase.Players;

namespace DominionBase.Piles
{
	public enum DeckPosition
	{
		Top,
		Bottom,
		Automatic
	}

	public class Deck : Pile
	{
		private DeckLocation _DeckLocation;
		public Deck(DeckLocation deckLocation)
			: base()
		{
			_DeckLocation = deckLocation;
		}
		public Deck(DeckLocation deckLocation, Visibility visibility, VisibilityTo visibilityTo) : this(deckLocation, visibility, visibilityTo, null, false) { }
		public Deck(DeckLocation deckLocation, Visibility visibility, VisibilityTo visibilityTo, IComparer<Card> comparer, Boolean collate)
			: base(visibility, visibilityTo, comparer, collate)
		{
			_DeckLocation = deckLocation;
		}

		public static Deck CreateInstance(Type type)
		{
			return (Deck)type.GetConstructor(Type.EmptyTypes).Invoke(null);
		}

		public override event Pile.PileChangedEventHandler PileChanged;

		public DeckLocation DeckLocation { get { return _DeckLocation; } }

		internal override void Clear()
		{
			base.Clear();
#if DEBUG
			this.TestFireAllEvents();
#endif
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

		public void AddRange(Player player, IEnumerable<Card> cardCollection) { AddRange(player, cardCollection, DeckPosition.Top); }
		public void AddRange(Player player, IEnumerable<Card> cardCollection, DeckPosition deckPosition)
		{
			switch (deckPosition)
			{
				case DeckPosition.Top:
					_Cards.InsertRange(0, cardCollection.Reverse());
					break;
				case DeckPosition.Bottom:
					_Cards.AddRange(cardCollection);
					break;
			}
			this.Sort();

			if (cardCollection.Count() > 0)
			{
				if (_AsynchronousChanging)
				{
					if (_AsynchronousPileChangedEventArgs == null)
						_AsynchronousPileChangedEventArgs = new PileChangedEventArgs(player, PileChangedEventArgs.Operation.Added, cardCollection);
					else
						_AsynchronousPileChangedEventArgs.AddedCards.AddRange(cardCollection);
				}
				else if (PileChanged != null)
				{
					lock (PileChanged)
					{
						PileChangedEventArgs pcea = new PileChangedEventArgs(player, PileChangedEventArgs.Operation.Added, cardCollection);
						PileChanged(this, pcea);
					}
				}
			}
		}

		public void Refresh(Player player)
		{
			PileChangedEventArgs pcea = new PileChangedEventArgs(player, PileChangedEventArgs.Operation.Reset);
			if (_AsynchronousChanging)
			{
				_AsynchronousPileChangedEventArgs = pcea;
			}
			else if (PileChanged != null)
			{
				PileChanged(this, pcea);
			}
		}

		internal Card Retrieve(Player player)
		{
			CardCollection cc = Retrieve(player, 1);
			if (cc.Count == 0)
				throw new Exception("No Cards to draw!");
			return cc[0];
		}

		internal Card Retrieve(Player player, Card card)
		{
			CardCollection cc = Retrieve(player, c => c == card);
			if (cc.Count == 0)
				throw new Exception(String.Format("Cannot find card {0}", card));
			return cc[0];
		}

		internal CardCollection Retrieve(Player player, int count)
		{
			return Retrieve(player, DeckPosition.Automatic, c => true, count);
		}

		internal CardCollection Retrieve(Player player, Predicate<Card> match)
		{
			return Retrieve(player, DeckPosition.Automatic, match, -1);
		}

		internal CardCollection Retrieve(Player player, Category cardType)
		{
			return Retrieve(player, cardType, -1);
		}
		internal CardCollection Retrieve(Player player, Type type)
		{
			return Retrieve(player, DeckPosition.Automatic, c => c.CardType == type, -1);
		}
		internal CardCollection Retrieve(Player player, Category cardType, int count)
		{
			return Retrieve(player, DeckPosition.Automatic, c => (c.Category & cardType) == cardType, count);
		}
		internal CardCollection Retrieve(Player player, Type type, int count)
		{
			return Retrieve(player, DeckPosition.Automatic, c => c.CardType == type, count);
		}

		internal CardCollection Retrieve(Player player, DeckPosition position, Predicate<Card> match, int count)
		{
			CardCollection matching = _Cards.FindAll(match);
			if (count >= 0 && count < matching.Count)
			{
				if (position == DeckPosition.Bottom)
					matching.RemoveRange(0, matching.Count - count);
				else
					matching.RemoveRange(count, matching.Count - count);
				if (matching.Count != count)
					throw new Exception("Incorrect number of cards drawn!");
			}
			_Cards.RemoveAll(c => matching.Contains(c));

			if (matching.Count > 0)
			{
				if (_AsynchronousChanging)
				{
					if (_AsynchronousPileChangedEventArgs == null)
						_AsynchronousPileChangedEventArgs = new PileChangedEventArgs(player, PileChangedEventArgs.Operation.Removed, matching);
					else
						_AsynchronousPileChangedEventArgs.AddedCards.AddRange(matching);
				}
				else if (PileChanged != null)
				{
					PileChangedEventArgs pcea = new PileChangedEventArgs(player, PileChangedEventArgs.Operation.Removed, matching);
					PileChanged(this, pcea);
				}
			}
			return matching;
		}

		public void Shuffle()
		{
			Utilities.Shuffler.Shuffle<Card>(_Cards);
		}

		internal void End(Player player)
		{
			// Create a copy to enumerate over
			CardCollection cards = new CardCollection(_Cards);
			foreach (Card card in cards)
				card.End(player, this);
		}

		public override void TestFireAllEvents()
		{
			if (PileChanged != null)
				PileChanged(this, new PileChangedEventArgs(PileChangedEventArgs.Operation.Reset));
		}
	}
}
