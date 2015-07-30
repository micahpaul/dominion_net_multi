using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using DominionBase.Players;
using DominionBase.Cards;

namespace DominionBase.Piles
{

	// Special class just for Trash
	public class Trash : Pile
	{
		public Trash()
			: base(Visibility.All, VisibilityTo.All, new DominionBase.Cards.Sorting.ByTypeName(DominionBase.Cards.Sorting.SortDirection.Descending), true)
		{
		}

		public override event Pile.PileChangedEventHandler PileChanged;

		public override void EndChanges()
		{
			_AsynchronousChanging = false;
			if (_AsynchronousPileChangedEventArgs != null && PileChanged != null)
			{
				PileChanged(this, _AsynchronousPileChangedEventArgs);
			}
			_AsynchronousPileChangedEventArgs = null;
		}

		internal void Add(Cards.Card card)
		{
			_Cards.Insert(0, card);
			this.Sort();

			if (_AsynchronousChanging)
			{
				if (_AsynchronousPileChangedEventArgs == null)
					_AsynchronousPileChangedEventArgs = new PileChangedEventArgs(null, PileChangedEventArgs.Operation.Added, card);
				else
					_AsynchronousPileChangedEventArgs.AddedCards.Add(card);
			}
			else if (PileChanged != null)
			{
				PileChangedEventArgs pcea = new PileChangedEventArgs(null, PileChangedEventArgs.Operation.Added, card);
				PileChanged(this, pcea);
			}
		}

		internal void AddRange(Cards.CardCollection cards)
		{
			_Cards.InsertRange(0, cards);
			this.Sort();

			if (_AsynchronousChanging)
			{
				if (_AsynchronousPileChangedEventArgs == null)
					_AsynchronousPileChangedEventArgs = new PileChangedEventArgs(null, PileChangedEventArgs.Operation.Added, cards);
				else
					_AsynchronousPileChangedEventArgs.AddedCards.AddRange(cards);
			}
			else if (PileChanged != null)
			{
				PileChangedEventArgs pcea = new PileChangedEventArgs(null, PileChangedEventArgs.Operation.Added, cards);
				PileChanged(this, pcea);
			}
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
			return Retrieve(player, c => true, count);
		}

		internal CardCollection Retrieve(Player player, Predicate<Card> match)
		{
			return Retrieve(player, match, -1);
		}

		internal CardCollection Retrieve(Player player, Category cardType)
		{
			return Retrieve(player, cardType, -1);
		}
		internal CardCollection Retrieve(Player player, Category cardType, int count)
		{
			return Retrieve(player, c => (c.Category & cardType) == cardType, count);
		}
		internal CardCollection Retrieve(Player player, Type type, int count)
		{
			return Retrieve(player, c => c.CardType == type, count);
		}

		internal CardCollection Retrieve(Player player, Predicate<Card> match, int count)
		{
			CardCollection matching = _Cards.FindAll(match);
			if (count >= 0 && count < matching.Count)
			{
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

		public override void TestFireAllEvents()
		{
			if (PileChanged != null)
				PileChanged(this, new PileChangedEventArgs(PileChangedEventArgs.Operation.Reset));
		}

		internal XmlNode GenerateXml(XmlDocument doc)
		{
			XmlElement xeTrash = doc.CreateElement("trash");
			xeTrash.AppendChild(this.LookThrough(c => true).GenerateXml(doc, "cards"));

			return xeTrash;
		}

		internal void Load(XmlNode xnRoot)
		{
			XmlNode xnTrash = xnRoot.SelectSingleNode("trash");
			if (xnTrash == null)
				return;

			foreach (XmlNode xnCard in xnTrash.SelectNodes("cards/card"))
			{
				Type cardType = Type.GetType(xnCard.Attributes["type"].Value);
				this.Add(Card.CreateInstance(cardType));
			}
		}
	}
}
