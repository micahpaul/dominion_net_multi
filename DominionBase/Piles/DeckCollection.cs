using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using DominionBase.Cards;
using DominionBase.Players;

namespace DominionBase.Piles
{
	public class DeckCollection : IPile
	{
		private List<Deck> _Decks = new List<Deck>();

		public DeckCollection(Deck deck)
		{
			this.Add(deck);
		}

		public DeckCollection(IEnumerable<Deck> collection)
		{
			_Decks.AddRange(collection);
		}

		public DeckCollection(params Deck[] collection)
		{
			_Decks.AddRange(collection);
		}

		public void Add(Deck deck)
		{
			_Decks.Add(deck);
		}

		public CardCollection this[Category type]
		{
			get
			{
				return new CardCollection(_Decks.SelectMany(d => d[type]));
			}
		}

		public CardCollection this[Type type]
		{
			get
			{
				return new CardCollection(_Decks.SelectMany(d => d[type]));
			}
		}

		public CardCollection this[String name]
		{
			get
			{
				return new CardCollection(_Decks.SelectMany(d => d[name]));
			}
		}

		public CardCollection this[Predicate<Card> predicate]
		{
			get
			{
				return new CardCollection(_Decks.SelectMany(d => d[predicate]));
			}
		}

		public int Count
		{
			get { return _Decks.Sum(d => d.Count); }
		}
	}
}
