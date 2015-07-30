using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DominionBase.Cards
{
	public class CardMovement
	{
		private Card _Card;
		private Players.DeckLocation _CurrentLocation;
		private Players.DeckLocation _Destination;
		private Piles.DeckPosition _DestinationDeckPosition = Piles.DeckPosition.Automatic;

		public Card Card { get { return _Card; } private set { _Card = value; } }
		public Players.DeckLocation CurrentLocation { get { return _CurrentLocation; } private set { _CurrentLocation = value; } }
		public Players.DeckLocation Destination { get { return _Destination; } set { _Destination = value; } }
		public Piles.DeckPosition DestinationDeckPosition { get { return _DestinationDeckPosition; } set { _DestinationDeckPosition = value; } }

		public CardMovement(Card card, Players.DeckLocation currentLocation, Players.DeckLocation destination)
		{
			_Card = card;
			_CurrentLocation = currentLocation;
			_Destination = destination;
		}

		public override string ToString()
		{
			return String.Format("Moving {0}: {1} -> {2} ({3})", this.Card, this.CurrentLocation, this.Destination, this.DestinationDeckPosition);
		}
	}

	public class CardMovementCollection : List<CardMovement>
	{
		public CardMovementCollection() : base() { }
		public CardMovementCollection(int capacity) : base(capacity) { }
		public CardMovementCollection(IEnumerable<CardMovement> collection) : base(collection) { }
		public CardMovementCollection(Piles.Pile pile, Players.DeckLocation currentLocation, Players.DeckLocation destination)
			: base()
		{
			this.AddRange(pile, c => true, currentLocation, destination);
		}

		public CardMovementCollection(Piles.Pile pile, Predicate<Card> predicate, Players.DeckLocation currentLocation, Players.DeckLocation destination)
			: base()
		{
			this.AddRange(pile, predicate, currentLocation, destination);
		}

		public void AddRange(Piles.Pile pile, Players.DeckLocation currentLocation, Players.DeckLocation destination)
		{
			AddRange(pile, c => true, currentLocation, destination);
		}

		public void AddRange(Piles.Pile pile, Predicate<Card> predicate, Players.DeckLocation currentLocation, Players.DeckLocation destination)
		{
			foreach (Card card in pile[predicate])
				this.Add(new CardMovement(card, currentLocation, destination));
		}

		public Boolean Contains(Card card)
		{
			return this.Exists(cm => cm.Card == card);
		}
		public CardMovement this[Card card] { get { return this.Find(cm => cm.Card == card); } }

		public void MoveToEnd(Card card)
		{
			if (!this.Contains(card))
				throw new KeyNotFoundException(String.Format("Cannot find card {0}", card));

			CardMovement cm = this[card];
			this.Remove(cm);
			this.Add(cm);
		}
	}
}
