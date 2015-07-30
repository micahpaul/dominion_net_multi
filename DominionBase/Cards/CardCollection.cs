using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

using DominionBase.Players;

namespace DominionBase.Cards
{
	public class CardCollection : List<Card>
	{
		public CardCollection() : base() { }
		public CardCollection(int capacity) : base(capacity) { }
		public CardCollection(IEnumerable<Card> collection) : base(collection) { }

		public int VictoryPoints
		{
			get
			{
				int value = 0;
				lock (this)
					value = this.Sum(c => c.GetVictoryPoints(this));
				return value;
			}
		}

		public void Play(Player player)
		{
			foreach (Card card in this)
				card.Play(player);
		}

		internal void PhaseChanged(object sender, PhaseChangedEventArgs e)
		{
			foreach (Card card in this)
				card.PhaseChanged(sender, e);
		}

		internal void PlayerModeChanged(object sender, PlayerModeChangedEventArgs e)
		{
			foreach (Card card in this)
				card.PlayerModeChanged(sender, e);
		}

		internal void ObtainedBy(Player player)
		{
			foreach (Card card in this)
				card.ObtainedBy(player);
		}

		internal void LostBy(Player player)
		{
			foreach (Card card in this)
				card.LostBy(player);
		}

		internal void ReceivedBy(Player player)
		{
			foreach (Card card in this)
				card.ReceivedBy(player);
		}

		internal void TrashedBy(Player player)
		{
			foreach (Card card in this)
				card.TrashedBy(player);
		}

		internal void AddedTo(DeckLocation location, Player player)
		{
			foreach (Card card in this)
				card.AddedTo(location, player);
		}

		internal void RemovedFrom(DeckLocation location, Player player)
		{
			foreach (Card card in this)
				card.RemovedFrom(location, player);
		}

		internal void AddedTo(Type deckType, Player player)
		{
			foreach (Card card in this)
				card.AddedTo(deckType, player);
		}

		internal void RemovedFrom(Type deckType, Player player)
		{
			foreach (Card card in this)
				card.RemovedFrom(deckType, player);
		}

		public new CardCollection FindAll(Predicate<Card> match)
		{
			return new CardCollection(base.FindAll(match));
		}

		public new CardCollection GetRange(int index, int count)
		{
			return new CardCollection(base.GetRange(index, count));
		}

		public CardCollection Except(CardCollection second)
		{

			return new CardCollection(this.Except<Card>(second));
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			foreach (Card c in this)
			{
				if (sb.Length > 0)
					sb.Append(", ");
				sb.Append(c.ToString());
			}
			return sb.ToString();
		}

		public String ToString(Boolean isCollated)
		{
			if (!isCollated)
				return this.ToString();

			StringBuilder sb = new StringBuilder();

			Card _previousCard = null;
			int count = 0;
			lock (this)
			{
				for (int cIndex = 0; cIndex < this.Count; cIndex++)
				{
					lock (this[cIndex])
					{
						if (_previousCard == null)
							_previousCard = this[cIndex];
						if (_previousCard.CardType != this[cIndex].CardType)
						{
							if (sb.Length != 0)
								sb.Append(", ");
							if (count > 1)
								sb.AppendFormat("{0}x {1}", count, _previousCard);
							else
								sb.Append(_previousCard);
							_previousCard = this[cIndex];
							count = 0;
						}
					}
					count++;
				}
			}
			if (_previousCard != null)
			{
				if (sb.Length != 0)
					sb.Append(", ");
				if (count > 1)
					sb.AppendFormat("{0}x {1}", count, _previousCard);
				else
					sb.Append(_previousCard);
			}
			return sb.ToString();
		}

		public static CardCollection GetAllCards(Func<Card, Boolean> predicate)
		{
			CardCollection _Cards = new CardCollection();
			IEnumerable<Type> _CardSets = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.Namespace.StartsWith("DominionBase.Cards") && x.Name == "TypeClass");
			foreach (Type cardSet in _CardSets)
			{
				foreach (FieldInfo fi in cardSet.GetFields())
				{
					// These aren't the ones we're looking for
					if (!fi.IsPublic || !fi.IsStatic)
						continue;

					Type t = (Type)fi.GetValue(null);
					if (t.IsSubclassOf(typeof(Card)))
					{
						Card c = Card.CreateInstance(t);

						if (predicate(c))
							_Cards.Add(c);
					}
				}
			}

			return _Cards;
		}


		internal void TearDown()
		{
			foreach (Card card in this)
				card.TearDown();
		}

		internal XmlNode GenerateXml(XmlDocument doc, String nodeName)
		{
			XmlElement xePile = doc.CreateElement(nodeName);
			foreach (Cards.Card card in this)
				xePile.AppendChild(card.GenerateXml(doc, "card"));

			return xePile;
		}

		internal static CardCollection Load(XmlNode xnCards)
		{
			CardCollection cc = new CardCollection();
			foreach (XmlNode xnCard in xnCards.SelectNodes("card"))
				cc.Add(Card.Load(xnCard));

			return cc;
		}
	}
}

namespace DominionBase.Cards.Sorting
{
	public enum SortDirection
	{
		Ascending = 1,
		Descending = -1
	}

	public class ByVictoryPoints : IComparer<Card>
	{
		private SortDirection _SortDirection;

		public ByVictoryPoints()
			: this(SortDirection.Ascending)
		{

		}

		public ByVictoryPoints(SortDirection sortDirection)
		{
			_SortDirection = sortDirection;
		}

		public int Compare(Card a, Card b)
		{
			if (ReferenceEquals(a, b))
				return 0;

			if (a == null && b == null)
				return 0;

			if (a == null)
				return (int)_SortDirection * 1;

			if (b == null)
				return (int)_SortDirection * -1;

			if (((a.Category & Category.Victory) == Category.Victory ||
				(a.Category & Category.Curse) == Category.Curse) &&
				((b.Category & Category.Victory) != Category.Victory &&
				(b.Category & Category.Curse) != Category.Curse))
				return (int)_SortDirection * 1;

			if (((a.Category & Category.Victory) != Category.Victory &&
				(a.Category & Category.Curse) != Category.Curse) &&
				((b.Category & Category.Victory) == Category.Victory ||
				(b.Category & Category.Curse) == Category.Curse))
				return (int)_SortDirection * -1;

			if (a.VictoryPoints != b.VictoryPoints)
				return (int)_SortDirection * a.VictoryPoints.CompareTo(b.VictoryPoints);

			if (a.Name != b.Name)
				return (int)_SortDirection * a.Name.CompareTo(b.Name);

			return (int)_SortDirection * a.UniqueId.CompareTo(b.UniqueId);
		}
	}

	public class ByTypeName : IComparer<Card>
	{
		private SortDirection _SortDirection;

		public ByTypeName()
			: this(SortDirection.Ascending)
		{

		}

		public ByTypeName(SortDirection sortDirection)
		{
			_SortDirection = sortDirection;
		}

		public int Compare(Card a, Card b)
		{
			if (ReferenceEquals(a, b))
				return 0;

			if (a == null && b == null)
				return 0;

			if (a == null)
				return (int)_SortDirection * 1;

			if (b == null)
				return (int)_SortDirection * -1;

			if ((a.Category & Category.Action) == Category.Action &&
				(b.Category & Category.Action) != Category.Action)
				return (int)_SortDirection * 1;

			if ((a.Category & Category.Action) != Category.Action &&
				(b.Category & Category.Action) == Category.Action)
				return (int)_SortDirection * -1;

			if ((a.Category & Category.Treasure) == Category.Treasure &&
				(b.Category & Category.Treasure) != Category.Treasure)
				return (int)_SortDirection * 1;

			if ((a.Category & Category.Treasure) != Category.Treasure &&
				(b.Category & Category.Treasure) == Category.Treasure)
				return (int)_SortDirection * -1;

			if (a.BaseCost != b.BaseCost)
				return (int)_SortDirection * a.BaseCost.CompareTo(b.BaseCost);

			if (a.Name != b.Name)
				return (int)_SortDirection * a.Name.CompareTo(b.Name);

			return (int)_SortDirection * a.UniqueId.CompareTo(b.UniqueId);
		}
	}
	public class ByCost : IComparer<Card>
	{
		private SortDirection _SortDirection;

		public ByCost()
			: this(SortDirection.Ascending)
		{

		}

		public ByCost(SortDirection sortDirection)
		{
			_SortDirection = sortDirection;
		}

		public int Compare(Card a, Card b)
		{
			if (ReferenceEquals(a, b))
				return 0;

			if (a == null && b == null)
				return 0;

			if (a == null)
				return (int)_SortDirection * 1;

			if (b == null)
				return (int)_SortDirection * -1;

			if (a.BaseCost == b.BaseCost)
				return (int)_SortDirection * a.Name.CompareTo(b.Name);

			return (int)_SortDirection * (a.BaseCost.Coin.Value + 2.5 * a.BaseCost.Potion.Value).CompareTo(b.BaseCost.Coin.Value + 2.5 * b.BaseCost.Potion.Value);
		}
	}
	public class ForEndgame : IComparer<Card>
	{
		private SortDirection _SortDirection = SortDirection.Descending;

		public ForEndgame()
		{

		}

		public int Compare(Card a, Card b)
		{
			if (ReferenceEquals(a, b))
				return 0;

			if (a == null && b == null)
				return 0;

			if (a == null)
				return -1;

			if (b == null)
				return 1;

			if (((a.Category & Category.Victory) == Category.Victory ||
				(a.Category & Category.Curse) == Category.Curse) &&
				((b.Category & Category.Victory) != Category.Victory &&
				(b.Category & Category.Curse) != Category.Curse))
				return -1;

			if (((a.Category & Category.Victory) != Category.Victory &&
				(a.Category & Category.Curse) != Category.Curse) &&
				((b.Category & Category.Victory) == Category.Victory ||
				(b.Category & Category.Curse) == Category.Curse))
				return 1;

			if (a.VictoryPoints != b.VictoryPoints)
				return b.VictoryPoints.CompareTo(a.VictoryPoints);

			if (a.Name != b.Name)
				return a.Name.CompareTo(b.Name);

			return a.UniqueId.CompareTo(b.UniqueId);
		}
	}
}