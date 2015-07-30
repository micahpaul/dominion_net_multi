using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using DominionBase.Cards;
using DominionBase.Players;

namespace DominionBase.Piles
{
	public class CardMatsChangedEventArgs : EventArgs
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
		public CardMat CardMat;
		public Player Player { get { return _Player; } }
		public CardCollection AddedCards { get { return _AddedCards; } }
		public CardCollection RemovedCards { get { return _RemovedCards; } }
		public Operation OperationPerformed { get { return _OperationPerformed; } }
		public CardMatsChangedEventArgs(CardMat cardMat, Operation operation)
		{
			this.CardMat = cardMat;
			_OperationPerformed = operation;
			_AddedCards = new CardCollection();
			_RemovedCards = new CardCollection();
		}
		public CardMatsChangedEventArgs(CardMat cardMat, Player player, Operation operation)
			: this(cardMat, operation)
		{
			_Player = player;
		}
		public CardMatsChangedEventArgs(CardMat cardMat, Player player, Operation operation, Card cardChanged)
			: this(cardMat, player, operation)
		{
			switch (operation)
			{
				case CardMatsChangedEventArgs.Operation.Added:
					_AddedCards.Add(cardChanged);
					break;
				case CardMatsChangedEventArgs.Operation.Removed:
					_RemovedCards.Add(cardChanged);
					break;
			}
		}
		public CardMatsChangedEventArgs(CardMat cardMat, Player player, Operation operation, IEnumerable<Card> cardsChanged)
			: this(cardMat, player, operation)
		{
			switch (operation)
			{
				case CardMatsChangedEventArgs.Operation.Added:
					_AddedCards.AddRange(cardsChanged);
					break;
				case CardMatsChangedEventArgs.Operation.Removed:
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

		public static new CardMat CreateInstance(Type type)
		{
			return (CardMat)type.GetConstructor(Type.EmptyTypes).Invoke(null);
		}

		internal XmlNode GenerateXml(XmlDocument doc)
		{
			XmlElement xeCardMat = doc.CreateElement("cardmat");

			XmlElement xe = doc.CreateElement("type");
			xe.InnerText = this.GetType().ToString();
			xeCardMat.AppendChild(xe);

			xe = doc.CreateElement("isobtainable");
			xe.InnerText = this.IsObtainable.ToString();
			xeCardMat.AppendChild(xe);

			xeCardMat.AppendChild(this.LookThrough(c => true).GenerateXml(doc, "cards"));

			return xeCardMat;
		}

		internal static CardMat Load(XmlNode xnCardMat)
		{
			XmlNode xnType = xnCardMat.SelectSingleNode("type");

			if (xnType == null)
				return null;

			Type type = Type.GetType(xnType.InnerText);

			CardMat cardMat = CardMat.CreateInstance(type);
			cardMat.LoadInstance(xnCardMat);

			return cardMat;
		}

		internal void LoadInstance(XmlNode xnCardMat)
		{
			XmlNode xnIsObtainable = xnCardMat.SelectSingleNode("isobtainable");
			if (xnIsObtainable != null)
			{
				Boolean value;
				if (Boolean.TryParse(xnIsObtainable.Value, out value))
					this.IsObtainable = value;
			}

			foreach (XmlNode xnCard in xnCardMat.SelectNodes("cards/card"))
			{
				Type cardType = Type.GetType(xnCard.Attributes["type"].Value);
				this._Cards.Add(Card.CreateInstance(cardType));
			}
		}
	}

	public class CardMats : SerializableDictionary<Type, CardMat>
	{
		public delegate void CardMatsChangedEventHandler(object sender, CardMatsChangedEventArgs e);
		public event CardMatsChangedEventHandler CardMatsChanged;

		public void Add(Player player, Type cardMatType, IEnumerable<Card> cards)
		{
			if (!this.ContainsKey(cardMatType))
				this[cardMatType] = CardMat.CreateInstance(cardMatType);
			this[cardMatType].AddRange(player, cards);

			if (CardMatsChanged != null)
			{
				CardMatsChangedEventArgs pcea = new CardMatsChangedEventArgs(this[cardMatType], player, CardMatsChangedEventArgs.Operation.Added, cards);
				CardMatsChanged(this, pcea);
			}
		}

		public CardCollection Retrieve(Player player, Type cardMatType, Predicate<Card> match, int count)
		{
			CardMat c = null;
			if (this.ContainsKey(cardMatType))
				c = this[cardMatType];
			else
				c = CardMat.CreateInstance(cardMatType);

			CardCollection cc = c.Retrieve(player, DeckPosition.Automatic, match, count);

			if (CardMatsChanged != null)
			{
				CardMatsChangedEventArgs pcea = new CardMatsChangedEventArgs(this[cardMatType], player, CardMatsChangedEventArgs.Operation.Removed, cc);
				CardMatsChanged(this, pcea);
			}

			return cc;
		}

		internal void TearDown()
		{
			foreach (CardMat mat in this.Values)
				mat.TearDown();
		}

		internal XmlNode GenerateXml(XmlDocument doc, String nodeName)
		{
			XmlElement xeCardMats = doc.CreateElement(nodeName);
			foreach (KeyValuePair<Type, CardMat> kvpCardMat in this)
				xeCardMats.AppendChild(kvpCardMat.Value.GenerateXml(doc));

			return xeCardMats;
		}

		internal void Load(XmlNode xnCardMats)
		{
			if (xnCardMats == null)
				return;

			foreach (XmlNode xnCardMat in xnCardMats.SelectNodes("cardmat"))
			{
				CardMat cardMat = CardMat.Load(xnCardMat);
				this.Add(cardMat.GetType(), cardMat);
			}
		}
	}
}
