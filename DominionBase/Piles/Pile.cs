using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DominionBase.Cards;
using DominionBase.Players;

namespace DominionBase.Piles
{
	public class PileChangedEventArgs : EventArgs
	{
		public enum Operation
		{
			Reset,
			Added,
			Removed
		}
		private Visual.VisualPlayer _Player = null;
		private CardCollection _AddedCards;
		private CardCollection _RemovedCards;
		private Operation _OperationPerformed;
		public Visual.VisualPlayer Player { get { return _Player; } }
		public CardCollection AddedCards { get { return _AddedCards; } }
		public CardCollection RemovedCards { get { return _RemovedCards; } }
		public Operation OperationPerformed { get { return _OperationPerformed; } }
		public PileChangedEventArgs(Operation operation)
		{
			_OperationPerformed = operation;
			_AddedCards = new CardCollection();
			_RemovedCards = new CardCollection();
		}
		public PileChangedEventArgs(Player player, Operation operation)
			: this(operation)
		{
			_Player = new Visual.VisualPlayer(player);
		}
		public PileChangedEventArgs(Player player, Operation operation, Card cardChanged)
			: this(player, operation)
		{
			switch (operation)
			{
				case PileChangedEventArgs.Operation.Added:
					_AddedCards.Add(cardChanged);
					break;
				case PileChangedEventArgs.Operation.Removed:
					_RemovedCards.Add(cardChanged);
					break;
			}
		}
		public PileChangedEventArgs(Player player, Operation operation, IEnumerable<Card> cardsChanged)
			: this(player, operation)
		{
			switch (operation)
			{
				case PileChangedEventArgs.Operation.Added:
					_AddedCards.AddRange(cardsChanged);
					break;
				case PileChangedEventArgs.Operation.Removed:
					_RemovedCards.AddRange(cardsChanged);
					break;
			}
		}
	}

	public enum Visibility
	{
		None,
		Top,
		All
	}

	public enum VisibilityTo
	{
		Owner,
		All
	}

	public abstract class Pile : IEnumerable<Card>, IEnumerable, IDisposable, IPile
	{
		public delegate void PileChangedEventHandler(object sender, PileChangedEventArgs e);
		public virtual event PileChangedEventHandler PileChanged;

		protected CardCollection _Cards = new CardCollection();
		private Visibility _Visibility = Visibility.None;
		private IComparer<Card> _Comparer = null;
		private Boolean _Collate = false;
		private VisibilityTo _VisibilityTo = VisibilityTo.Owner;
		protected Boolean _AsynchronousChanging = false;
		protected PileChangedEventArgs _AsynchronousPileChangedEventArgs = null;

		private Guid _UniqueId = Guid.NewGuid();

		public Pile() : this(Visibility.None, VisibilityTo.Owner, null, false) { }
		public Pile(Visibility visibility, VisibilityTo visibilityTo, IComparer<Card> comparer, Boolean collate)
		{
			_Visibility = visibility;
			_VisibilityTo = visibilityTo;
			_Comparer = comparer;
			_Collate = collate;
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
					this._Cards = null;
					this._Comparer = null;
				}

				// Call the appropriate methods to clean up
				// unmanaged resources here.
				// If disposing is false,
				// only the following code is executed.

				// Note disposing has been done.
				disposed = true;
			}
		}

		~Pile()
		{
			Dispose(false);
		}
		#endregion

		public Visibility Visibility { get { return _Visibility; } private set { _Visibility = value; } }
		internal IComparer<Card> Comparer { get { return _Comparer; } set { _Comparer = value; } }
		internal Boolean Collate { get { return _Collate; } set { _Collate = value; } }
		public Guid UniqueId { get { return _UniqueId; } }

		internal virtual void Clear()
		{
			_Cards.Clear();

#if DEBUG
			TestFireAllEvents();
#endif
		}

		internal virtual void TearDown()
		{
			foreach (Card card in _Cards)
				card.TearDown();
		}

		public virtual void BeginChanges()
		{
			_AsynchronousChanging = true;
			_AsynchronousPileChangedEventArgs = null;
		}

		public virtual void EndChanges()
		{
			_AsynchronousChanging = false;
			if (_AsynchronousPileChangedEventArgs != null && PileChanged != null)
			{
				PileChanged(this, _AsynchronousPileChangedEventArgs);
			}
			_AsynchronousPileChangedEventArgs = null;
		}

		public Card First()
		{
			if (Visibility == Visibility.None)
				throw new AccessViolationException("This pile is hidden!");
			Card c = null;
			lock (_Cards)
				c = _Cards.FirstOrDefault();
			return c;
		}

		public Card First(Func<Card, bool> predicate)
		{
			if (Visibility == Visibility.None)
				throw new AccessViolationException("This pile is hidden!");
			Card c = null;
			lock(_Cards)
				c = _Cards.FirstOrDefault(predicate);
			return c;
		}

		public Card Last()
		{
			if (Visibility != Visibility.All)
				throw new AccessViolationException("This pile is hidden!");
			Card c = null;
			lock (_Cards)
				c = _Cards.LastOrDefault();
			return c;
		}

		public Card Last(Func<Card, bool> predicate)
		{
			if (Visibility != Visibility.All)
				throw new AccessViolationException("This pile is hidden!");
			Card c = null;
			lock (_Cards)
				c = _Cards.LastOrDefault(predicate);
			return c;
		}

		public IEnumerable<IGrouping<TKey, Card>> GroupBy<TKey>(Func<Card, TKey> keySelector)
		{
			return _Cards.GroupBy<Card, TKey>(keySelector);
		}

		public Boolean Contains(Card card)
		{
			return _Cards.Contains(card);
		}

		public CardCollection LookThrough(Predicate<Card> predicate)
		{
			Visibility oldViz = this.Visibility;
			this.Visibility = Visibility.All;
			CardCollection cards = this[predicate];
			this.Visibility = oldViz;
			return cards;
		}

		public CardCollection this[Category type]
		{
			get
			{
				return this[c => (c.Category & type) == type];
			}
		}

		public CardCollection this[Type type]
		{
			get
			{
				return this[c => c.CardType == type];
			}
		}

		public CardCollection this[String name]
		{
			get
			{
				return this[c => c.Name == name];
			}
		}

		public CardCollection this[Predicate<Card> predicate]
		{
			get
			{
				if (_Visibility == Visibility.None || _Cards.Count == 0)
					return new CardCollection();
				if (_Visibility == Visibility.Top && _VisibilityTo == VisibilityTo.All)
					return _Cards.GetRange(0, 1).FindAll(predicate);
				return _Cards.FindAll(predicate);
			}
		}

		public int Count
		{
			get { return _Cards.Count; }
		}

		public void Sort()
		{
			if (this.Comparer != null)
				this._Cards.Sort(this.Comparer);
		}

		public virtual void Reset()
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

		public IEnumerator<Card> GetEnumerator()
		{
			if (_Visibility != Visibility.All)
				throw new AccessViolationException("Cannot iterate over a hidden pile");
			for (int i = 0; i < _Cards.Count; i++)
			{
				yield return _Cards[i];
			}
		}

		internal int VictoryPoints
		{
			get
			{
				if (_Visibility == Visibility.None)
					return 0;
				else if (_Visibility == Visibility.Top)
				{
					if (_Cards.Count == 0)
						return 0;
					return (new CardCollection() { _Cards[0] }).VictoryPoints;
				}
				else
					return _Cards.VictoryPoints;
			}
		}

		public override string ToString()
		{
			switch (_Visibility)
			{
				case Visibility.None:
					return String.Format("{0} Cards", Count);
				case Visibility.All:
					String rtn;
					lock(_Cards)
						rtn = String.Format("{0} Cards: {1}", _Cards.Count, _Cards.ToString(_Collate));
					return rtn;
				case Visibility.Top:
					if (Count == 0)
						return String.Format("{0} Cards", Count);
					return String.Format("{0}; {1} total Cards", _Cards[0], Count);
			}
			return "<<Unknown>>";
		}

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			if (_Visibility != Visibility.All)
				throw new AccessViolationException("Cannot iterate over a hidden pile");
			for (int i = 0; i < _Cards.Count; i++)
			{
				yield return _Cards[i];
			}
		}

		#endregion

		public virtual void TestFireAllEvents()
		{
			if (PileChanged != null)
				PileChanged(this, new PileChangedEventArgs(PileChangedEventArgs.Operation.Reset));
		}
	}
}
