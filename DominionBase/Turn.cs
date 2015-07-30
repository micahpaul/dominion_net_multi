using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using DominionBase.Cards;
using DominionBase.Players;

namespace DominionBase
{
	public class Turn : IDisposable
	{
		private Player _Player = null;
		private Card _GrantedBy = null;
		private Player _NextPlayer = null;
		private Card _NextGrantedBy = null;
		private CardCollection _CardsPlayed = new CardCollection();
		private CardCollection _CardsResolved = new CardCollection();
		private CardCollection _CardsBought = new CardCollection();
		private CardCollection _CardsGained = new CardCollection();
		private CardCollection _CardsTrashed = new CardCollection();
		private CardCollection _CardsPassed = new CardCollection();
		private CardCollection _CardsReceived = new CardCollection();
		private CardCollection _CardsGainedAfter = new CardCollection();
		private CardCollection _CardsTrashedAfter = new CardCollection();
		private CardCollection _CardsPassedAfter = new CardCollection();
		private CardCollection _CardsReceivedAfter = new CardCollection();
		private Boolean _IsTurnFinished = false;
		private Boolean _ModifiedTurn = false;

		public Player Player { get { return _Player; } }
		public Card GrantedBy { get { return _GrantedBy; } set { _GrantedBy = value; } }
		public Player NextPlayer { get { return _NextPlayer; } set { _NextPlayer = value; } }
		public Card NextGrantedBy { get { return _NextGrantedBy; } set { _NextGrantedBy = value; } }
		public CardCollection CardsPlayed { get { return _CardsPlayed; } }
		public CardCollection CardsResolved { get { return _CardsResolved; } }
		public CardCollection CardsBought { get { return _CardsBought; } }
		public CardCollection CardsGained { get { return _CardsGained; } }
		public CardCollection CardsTrashed { get { return _CardsTrashed; } }
		public CardCollection CardsPassed { get { return _CardsPassed; } }
		public CardCollection CardsReceived { get { return _CardsReceived; } }
		public CardCollection CardsGainedAfter { get { return _CardsGainedAfter; } }
		public CardCollection CardsTrashedAfter { get { return _CardsTrashedAfter; } }
		public CardCollection CardsPassedAfter { get { return _CardsPassedAfter; } }
		public CardCollection CardsReceivedAfter { get { return _CardsReceivedAfter; } }
		public Boolean IsTurnFinished { get { return _IsTurnFinished; } private set { _IsTurnFinished = value; } }
		public Boolean ModifiedTurn { get { return _ModifiedTurn; } set { _ModifiedTurn = value; } }

		public void Clear()
		{
			this.CardsPlayed.Clear();
			this.CardsResolved.Clear();
			this.CardsBought.Clear();
			this.CardsGained.Clear();
			this.CardsTrashed.Clear();
			this.CardsPassed.Clear();
			this.CardsReceived.Clear();
			this.CardsGainedAfter.Clear();
			this.CardsTrashedAfter.Clear();
			this.CardsPassedAfter.Clear();
			this.CardsReceivedAfter.Clear();
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
					this._Player = null;
					this._GrantedBy = null;
					this._NextPlayer = null;
					this._NextGrantedBy = null;

					this._CardsPlayed.Clear();
					this._CardsResolved.Clear();
					this._CardsBought.Clear();
					this._CardsGained.Clear();
					this._CardsTrashed.Clear();
					this._CardsPassed.Clear();
					this._CardsReceived.Clear();
					this._CardsGainedAfter.Clear();
					this._CardsTrashedAfter.Clear();
					this._CardsPassedAfter.Clear();
					this._CardsReceivedAfter.Clear();
				}

				// Call the appropriate methods to clean up
				// unmanaged resources here.
				// If disposing is false,
				// only the following code is executed.

				// Note disposing has been done.
				disposed = true;
			}
		}

		~Turn()
		{
			Dispose(false);
		}
		#endregion

		public Turn(Player player)
		{
			_Player = player;
		}

		public void Played(Card card)
		{
			if (!_CardsPlayed.Contains(card))
				_CardsPlayed.Add(card);
			_CardsResolved.Add(card);
		}

		public void Played(IEnumerable<Card> cards)
		{
			foreach (Card card in cards)
				this.Played(card);
		}

		public void UndoPlayed(Card card)
		{
			if (_CardsPlayed.Contains(card))
				_CardsPlayed.Remove(card);
			_CardsResolved.Remove(card);
		}

		public void UndoPlayed(IEnumerable<Card> cards)
		{
			foreach (Card card in cards)
				this.UndoPlayed(card);
		}

		public void Bought(Card card)
		{
			_CardsBought.Add(card);
		}

		public void Bought(IEnumerable<Card> cards)
		{
			_CardsBought.AddRange(cards);
		}

		public void Gained(Card card)
		{
			if (!this.IsTurnFinished)
				_CardsGained.Add(card);
			else
				_CardsGainedAfter.Add(card);
		}

		public void Gained(IEnumerable<Card> cards)
		{
			if (!this.IsTurnFinished)
				_CardsGained.AddRange(cards);
			else
				_CardsGainedAfter.AddRange(cards);
		}

		public void Trashed(Card card)
		{
			if (!this.IsTurnFinished)
				_CardsTrashed.Add(card);
			else
				_CardsTrashedAfter.Add(card);
		}

		public void Trashed(IEnumerable<Card> cards)
		{
			if (!this.IsTurnFinished)
				_CardsTrashed.AddRange(cards);
			else
				_CardsTrashedAfter.AddRange(cards);
		}

		public void Passed(Card card)
		{
			if (!this.IsTurnFinished)
				_CardsPassed.Add(card);
			else
				_CardsPassedAfter.Add(card);
		}

		public void Passed(IEnumerable<Card> cards)
		{
			if (!this.IsTurnFinished)
				_CardsPassed.AddRange(cards);
			else
				_CardsPassedAfter.AddRange(cards);
		}

		public void Received(Card card)
		{
			if (!this.IsTurnFinished)
				_CardsReceived.Add(card);
			else
				_CardsReceivedAfter.Add(card);
		}

		public void Received(IEnumerable<Card> cards)
		{
			if (!this.IsTurnFinished)
				_CardsReceived.AddRange(cards);
			else
				_CardsReceivedAfter.AddRange(cards);
		}

		public void Finished()
		{
			_IsTurnFinished = true;
		}

		internal XmlNode GenerateXml(XmlDocument doc, String nodeName)
		{
			XmlElement xeTurn = doc.CreateElement(nodeName);

			xeTurn.AppendChild(this.CardsBought.GenerateXml(doc, "cardsbought"));
			xeTurn.AppendChild(this.CardsGained.GenerateXml(doc, "cardsgained"));
			xeTurn.AppendChild(this.CardsGainedAfter.GenerateXml(doc, "cardsgainedafter"));
			xeTurn.AppendChild(this.CardsPassed.GenerateXml(doc, "cardspassed"));
			xeTurn.AppendChild(this.CardsPassedAfter.GenerateXml(doc, "cardspassedafter"));
			xeTurn.AppendChild(this.CardsPlayed.GenerateXml(doc, "cardsplayed"));
			xeTurn.AppendChild(this.CardsReceived.GenerateXml(doc, "cardsreceived"));
			xeTurn.AppendChild(this.CardsReceivedAfter.GenerateXml(doc, "cardsreceivedafter"));
			xeTurn.AppendChild(this.CardsResolved.GenerateXml(doc, "cardsresolved"));
			xeTurn.AppendChild(this.CardsTrashed.GenerateXml(doc, "cardstrashed"));
			xeTurn.AppendChild(this.CardsTrashedAfter.GenerateXml(doc, "cardstrashedafter"));

			XmlElement xe = doc.CreateElement("grantedby");
			xe.InnerText = String.Format("{0}", this.GrantedBy);
			xeTurn.AppendChild(xe);

			xe = doc.CreateElement("isturnfinished");
			xe.InnerText = String.Format("{0}", this.IsTurnFinished);
			xeTurn.AppendChild(xe);

			xe = doc.CreateElement("modifiedturn");
			xe.InnerText = String.Format("{0}", this.ModifiedTurn);
			xeTurn.AppendChild(xe);

			xe = doc.CreateElement("nextgrantedby");
			xe.InnerText = String.Format("{0}", this.NextGrantedBy);
			xeTurn.AppendChild(xe);

			xe = doc.CreateElement("nextplayer");
			xe.InnerText = this.NextPlayer == null ? "" : String.Format("{0}", this.NextPlayer.UniqueId);
			xeTurn.AppendChild(xe);

			xe = doc.CreateElement("player");
			xe.InnerText = this.Player == null ? "" : String.Format("{0}", this.Player.UniqueId);
			xeTurn.AppendChild(xe);

			return xeTurn;
		}

		internal static Turn Load(Game game, XmlNode xnTurn)
		{
			XmlNode xnPlayer = xnTurn.SelectSingleNode("player");
			if (xnPlayer == null)
				return null;
			Turn turn = new Turn(game.Players.FirstOrDefault(p => p.UniqueId == new Guid(xnPlayer.InnerText)));
			turn.Load(xnTurn);

			XmlNode xnNextPlayer = xnTurn.SelectSingleNode("nextplayer");
			if (xnNextPlayer == null)
				return null;
			if (!String.IsNullOrEmpty(xnNextPlayer.InnerText))
				turn.NextPlayer = game.Players.FirstOrDefault(p => p.UniqueId == new Guid(xnNextPlayer.InnerText));

			return turn;
		}

		internal void Load(XmlNode xnTurn)
		{
			this.CardsBought.AddRange(CardCollection.Load(xnTurn.SelectSingleNode("cardsbought")));
			this.CardsGained.AddRange(CardCollection.Load(xnTurn.SelectSingleNode("cardsgained")));
			this.CardsGainedAfter.AddRange(CardCollection.Load(xnTurn.SelectSingleNode("cardsgainedafter")));
			this.CardsPassed.AddRange(CardCollection.Load(xnTurn.SelectSingleNode("cardspassed")));
			this.CardsPassedAfter.AddRange(CardCollection.Load(xnTurn.SelectSingleNode("cardspassedafter")));
			this.CardsPlayed.AddRange(CardCollection.Load(xnTurn.SelectSingleNode("cardsplayed")));
			this.CardsReceived.AddRange(CardCollection.Load(xnTurn.SelectSingleNode("cardsreceived")));
			this.CardsReceivedAfter.AddRange(CardCollection.Load(xnTurn.SelectSingleNode("cardsreceivedafter")));
			this.CardsResolved.AddRange(CardCollection.Load(xnTurn.SelectSingleNode("cardsresolved")));
			this.CardsTrashed.AddRange(CardCollection.Load(xnTurn.SelectSingleNode("cardstrashed")));
			this.CardsTrashedAfter.AddRange(CardCollection.Load(xnTurn.SelectSingleNode("cardstrashedafter")));

			XmlNode xnGrantedBy = xnTurn.SelectSingleNode("grantedby");
			if (!string.IsNullOrEmpty(xnGrantedBy.InnerText))
				this.GrantedBy = Card.Load(xnGrantedBy);

			XmlNode xnIsTurnFinished = xnTurn.SelectSingleNode("isturnfinished");
			this.IsTurnFinished = Boolean.Parse(xnIsTurnFinished.InnerText);
			XmlNode xnModifiedTurn = xnTurn.SelectSingleNode("modifiedturn");
			this.ModifiedTurn = Boolean.Parse(xnModifiedTurn.InnerText);

			XmlNode xnNextGrantedBy = xnTurn.SelectSingleNode("nextgrantedby");
			if (!string.IsNullOrEmpty(xnNextGrantedBy.InnerText))
				this.NextGrantedBy = Card.Load(xnNextGrantedBy);
		}
	}

	public class TurnList : List<Turn>
	{
		public int TurnNumber(Player player)
		{
			return this.Count(t => t.Player == player && t.GrantedBy == null);
		}

		internal XmlNode GenerateXml(XmlDocument doc, String nodeName)
		{
			XmlElement xeTurns = doc.CreateElement(nodeName);
			foreach (Turn turn in this)
				xeTurns.AppendChild(turn.GenerateXml(doc, "turn"));

			return xeTurns;
		}

		internal void Load(Game game, XmlNode xnTurns)
		{
			foreach (XmlNode xnTurn in xnTurns.SelectNodes("turn"))
				this.Add(Turn.Load(game, xnTurn));
		}
	}
}
