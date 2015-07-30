using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Xml.Serialization;

namespace DominionBase.Cards
{
	[Serializable]
	public enum ConstraintType
	{
		[DescriptionAttribute("Select constraint")]
		[ToolTipAttribute("Blank constraint that doesn't do anything")]
		Unknown,

		[DescriptionAttribute("Must use Card")]
		[ToolTipAttribute("The card listed must be used")]
		CardMustUse,
		[DescriptionAttribute("Cannot use Card")]
		[ToolTipAttribute("The card listed cannot be used")]
		CardDontUse,

		[DescriptionAttribute("Card is in Set")]
		[ToolTipAttribute("The card was released in the Set listed")]
		SetIs,

		[DescriptionAttribute("Card Type is")]
		[ToolTipAttribute("The card's Type is only the listed Type (no multi-Type)")]
		CategoryIs,
		[DescriptionAttribute("Card Type has")]
		[ToolTipAttribute("The card's Type has listed Type in its Types")]
		CategoryContains,

		[DescriptionAttribute("Card costs")]
		[ToolTipAttribute("The card costs exactly the listed amount")]
		CardCosts,
		[DescriptionAttribute("Card cost contains Potion")]
		[ToolTipAttribute("The card cost has Potion in it")]
		CardCostContainsPotion,

		[DescriptionAttribute("Card is in Group")]
		[ToolTipAttribute("The card is a member of the Group listed")]
		MemberOfGroup,

		[DescriptionAttribute("Sets to use cards from")]
		[ToolTipAttribute("Use cards from the number of sets listed")]
		NumberOfSets,

		//[DescriptionAttribute("Cards per set")]
		//[ToolTipAttribute("Use this many cards per set")]
		//CardsPerSet,
	}

	public class ToolTipAttribute : Attribute
	{
		private String _ToolTip;
		/// Summary:
		///     Specifies the default value for the DominionBase.TooltipAttribute,
		///     which is an empty string (""). This static field is read-only.
		public static readonly ToolTipAttribute Default;

		/// Summary:
		///     Initializes a new instance of the DominionBase.TooltipAttribute
		///     class with no parameters.
		public ToolTipAttribute() : this(String.Empty) { }

		/// Summary:
		///     Initializes a new instance of the DominionBase.TooltipAttribute
		///     class with a tooltip.
		///
		/// Parameters:
		///   tooltip:
		///     The tooltip text.
		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		public ToolTipAttribute(string tooltip)
		{
			this._ToolTip = tooltip;
		}

		/// Summary:
		///     Gets the tooltip stored in this attribute.
		///
		/// Returns:
		///     The tooltip stored in this attribute.
		public virtual string ToolTip { get { return _ToolTip; } }

		/// Summary:
		///     Gets or sets the string stored as the tooltip.
		///
		/// Returns:
		///     The string stored as the tooltip. The default value is an empty string
		///     ("").
		protected string ToolTipValue { get { return _ToolTip; } set { _ToolTip = value; } }

		/// Summary:
		///     Returns whether the value of the given object is equal to the current DominionBase.TooltipAttribute.
		///
		/// Parameters:
		///   obj:
		///     The object to test the value equality of.
		///
		/// Returns:
		///     true if the value of the given object is equal to that of the current; otherwise,
		///     false.
		public override bool Equals(object obj)
		{
			if (obj == this)
			{
				return true;
			}
			ToolTipAttribute tooltipAttribute = obj as ToolTipAttribute;
			return tooltipAttribute != null && tooltipAttribute.ToolTip == this.ToolTip;
		}
		public override int GetHashCode() { return this.ToolTip.GetHashCode(); }

		/// Summary:
		///     Returns a value indicating whether this is the default DominionBase.TooltipAttribute
		///     instance.
		///
		/// Returns:
		///     true, if this is the default DominionBase.TooltipAttribute instance;
		///     otherwise, false.
		public override bool IsDefaultAttribute() { return false; }
	}

	public class ConstraintException : Exception
	{
		public ConstraintException() { }
		public ConstraintException(string message) : base(message) { }
		public ConstraintException(string message, Exception innerException) : base(message, innerException) { }
		internal ConstraintException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}

	[Serializable]
	public class Constraint
	{
		private ConstraintType _ConstraintType = ConstraintType.Unknown;
		private Object _ConstraintValue = null;
		private int _Minimum = 0;
		private int _Maximum = 10;
		private static CardCollection ccAllCards = null;

		public int Minimum { get { return _Minimum; } 
			set 
			{ 
				this._Minimum = value;
				if (this._Minimum < this.RangeMin)
					this._Minimum = this.RangeMin;
				if (this.Maximum < this._Minimum)
					this.Maximum = this._Minimum;
			} 
		}
		public int Maximum
		{
			get { return _Maximum; }
			set
			{
				this._Maximum = value;
				if (this._Maximum > this.RangeMax)
					this._Maximum = this.RangeMax;
				if (this.Minimum > this._Maximum)
					this.Minimum = this._Maximum;
			}
		}
		public ConstraintType ConstraintType 
		{
			get { return _ConstraintType; }
			set
			{
				if (_ConstraintType == value)
					return;

				Boolean resetValue = false;
				if ((_ConstraintType == Cards.ConstraintType.CardMustUse && value != Cards.ConstraintType.CardDontUse) ||
					(_ConstraintType == Cards.ConstraintType.CardDontUse && value != Cards.ConstraintType.CardMustUse) ||
					(_ConstraintType == Cards.ConstraintType.CategoryIs && value != Cards.ConstraintType.CategoryContains) ||
					(_ConstraintType == Cards.ConstraintType.CategoryContains && value != Cards.ConstraintType.CategoryIs))
					resetValue = true;

				_ConstraintType = value;

				if (resetValue)
					this.ConstraintValue = null;

				this.RangeMin = 0;
				this.RangeMax = 10;
				switch (value)
				{
					case Cards.ConstraintType.CardDontUse:
						this.Minimum = this.Maximum = 0;
						break;

					case Cards.ConstraintType.CardMustUse:
						this.Minimum = this.Maximum = 1;
						break;

					case Cards.ConstraintType.NumberOfSets:
						if (ccAllCards == null)
							ccAllCards = DominionBase.Cards.CardCollection.GetAllCards(c => c.Location == DominionBase.Cards.Location.Kingdom);
						this.RangeMin = 1;
						IEnumerable<IGrouping<DominionBase.Cards.Source, Card>> groups = ccAllCards.GroupBy(c => c.Source);
						this.RangeMax = ccAllCards.GroupBy(c => c.Source).Count();
						break;
				}
			}
		}
		public Object ConstraintValue 
		{
			get { return _ConstraintValue; }
			set
			{
				switch (this.ConstraintType)
				{
					case Cards.ConstraintType.CategoryIs:
					case Cards.ConstraintType.CategoryContains:
						if (value is KeyValuePair<Category, int>)
							_ConstraintValue = ((KeyValuePair<Category, int>)value).Key;
						else if (value != null)
							_ConstraintValue = (Category)value;
						else
							_ConstraintValue = Category.Action;
						break;

					case Cards.ConstraintType.SetIs:
						if (value is KeyValuePair<Source, int>)
							_ConstraintValue = ((KeyValuePair<Source, int>)value).Key;
						else if (value != null)
							_ConstraintValue = (Source)value;
						else
							_ConstraintValue = Source.Base;
						break;

					case Cards.ConstraintType.CardCosts:
						if (value is KeyValuePair<Cost, int>)
							_ConstraintValue = ((KeyValuePair<Cost, int>)value).Key;
						else
							_ConstraintValue = value;
						break;

					case Cards.ConstraintType.MemberOfGroup:
						if (value is KeyValuePair<Group, int>)
							_ConstraintValue = ((KeyValuePair<Group, int>)value).Key;
						else
							_ConstraintValue = value;
						break;

					default:
						_ConstraintValue = value;
						break;
				}
			}
		}

		private int _RangeMin = 0;
		[XmlIgnore]
		public int RangeMin
		{
			get { return _RangeMin; }
			set
			{
				_RangeMin = value;
				if (this._RangeMin > this._Minimum)
					this.Minimum = this._RangeMin;
			}
		}

		private int _RangeMax = 10;
		[XmlIgnore]
		public int RangeMax 
		{ 
			get { return _RangeMax; } 
			set { 
				_RangeMax = value;
				if (this._RangeMax < this._Maximum)
					this.Maximum = this._RangeMax;
			} 
		}

		public Constraint() : this(DominionBase.Cards.ConstraintType.Unknown, null, 0, 10) { }
		public Constraint(ConstraintType constraintType, String cardName)
		{
			this.ConstraintType = constraintType;
			switch (constraintType)
			{
				case Cards.ConstraintType.CardDontUse:
					this.ConstraintValue = cardName;
					break;

				case Cards.ConstraintType.CardMustUse:
					this.ConstraintValue = cardName;
					break;

				default:
					throw new ArgumentException("Specified ConstraintType not allowed for this constructor!");
			}
		}
		public Constraint(ConstraintType constraintType, int minimum, int maximum)
		{
			switch (constraintType)
			{
				case Cards.ConstraintType.NumberOfSets:
					break;

				default:
					throw new ArgumentException("Specified ConstraintType not allowed for this constructor!");
			}
			this.ConstraintType = constraintType;
			this.Minimum = 1;
			if (ccAllCards == null)
				ccAllCards = DominionBase.Cards.CardCollection.GetAllCards(c => c.Location == DominionBase.Cards.Location.Kingdom);
			this.Maximum = ccAllCards.GroupBy(c => c.Source).Count();
		}
		public Constraint(ConstraintType constraintType, object constraintValue, int minimum, int maximum)
		{
			this.ConstraintType = constraintType;
			this.ConstraintValue = constraintValue;
			this.Minimum = minimum;
			this.Maximum = maximum;
		}

		internal Boolean Matches(Card card)
		{
			return this.PredicateFunction(card);
		}

		internal Boolean MinimumMet(IEnumerable<Card> chosenCards)
		{
			switch (this.ConstraintType)
			{
				case Cards.ConstraintType.NumberOfSets:
					return chosenCards.GroupBy(this.GroupingFunction).Count() >= this.Minimum;

				//case Cards.ConstraintType.CardsPerSet:
				//    return chosenCards.GroupBy(this.GroupingFunction).All(g => g.Count() >= this.Minimum);

				default:
					return chosenCards.Count(this.PredicateFunction) >= this.Minimum;
			}
		}

		internal Boolean IsChoosable(IEnumerable<Card> chosenCards, Card card)
		{
			switch (this.ConstraintType)
			{
				case Cards.ConstraintType.NumberOfSets:
					IEnumerable<IGrouping<string, Card>> groupsNOS = chosenCards.GroupBy(this.GroupingFunction);
					return (groupsNOS.Count() < this.Maximum || (groupsNOS.Count() == this.Maximum && groupsNOS.Count(g => g.Key == card.Source.ToString()) == 1));

				//case Cards.ConstraintType.CardsPerSet:
				//    IEnumerable<IGrouping<string, Card>> groupsCPS = chosenCards.GroupBy(this.GroupingFunction);
				//    return !groupsCPS.Any(g => g.Key == card.Source.ToString()) || groupsCPS.FirstOrDefault(g => g.Key == card.Source.ToString()).Count() < this.Maximum;

				default:
					return (chosenCards.Count(this.PredicateFunction) < this.Maximum);
			}
		}

		public IEnumerable<Card> GetMatchingCards(IEnumerable<Card> _CardsAvailable)
		{
			return _CardsAvailable.Where(this.PredicateFunction);
		}

		public IEnumerable<Card> GetCardsToDiscard(IEnumerable<Card> selectedCards, IEnumerable<Card> _CardsAvailable)
		{
			switch (this.ConstraintType)
			{
				case Cards.ConstraintType.NumberOfSets:
					return _CardsAvailable.Where(card => selectedCards.Any(c => card.Source == c.Source));

				//case Cards.ConstraintType.CardsPerSet:
				//    return _CardsAvailable.Where(card => selectedCards.Count(c => card.Source == c.Source) >= this.Maximum);

				default:
					return selectedCards;
			}
		}

		private Func<Card, bool> PredicateFunction
		{
			get
			{
				switch (this.ConstraintType)
				{
					case Cards.ConstraintType.CardMustUse:
					case Cards.ConstraintType.CardDontUse:
						if (this.ConstraintValue.GetType() == typeof(Card))
							return card => ((Card)this.ConstraintValue).CardType == card.CardType;
						else if (this.ConstraintValue.GetType() == typeof(String))
							return card => (String)this.ConstraintValue == card.Name;
						return card => true;

					case Cards.ConstraintType.SetIs:
						return card => card.Source == (Source)this.ConstraintValue;

					case Cards.ConstraintType.CategoryIs:
						return card => card.Category == (Category)this.ConstraintValue;

					case Cards.ConstraintType.CategoryContains:
						return card => (card.Category & ((Category)this.ConstraintValue)) == (Category)this.ConstraintValue;

					case Cards.ConstraintType.CardCosts:
						return card => card.BaseCost == (Cost)this.ConstraintValue;

					case Cards.ConstraintType.CardCostContainsPotion:
						return card => card.BaseCost.Potion > 0;

					case Cards.ConstraintType.MemberOfGroup:
						return card => (card.GroupMembership & (Group)this.ConstraintValue) == (Group)this.ConstraintValue;

					default:
						return card => true;
				}
			}
		}

		private Func<Card, String> GroupingFunction
		{
			get
			{
				switch (this.ConstraintType)
				{
					case Cards.ConstraintType.NumberOfSets:
					//case Cards.ConstraintType.CardsPerSet:
						return card => card.Source.ToString();

					default:
						return card => card.Name;
				}
			}
		}
	}

	[Serializable]
	public class ConstraintCollection : List<Constraint>, INotifyCollectionChanged
	{
		[field:NonSerialized]
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		private int _MaxCount = 10;
		public int MaxCount 
		{ 
			get { return _MaxCount; } 
			set 
			{
				_MaxCount = value;
				foreach (Constraint constraint in this)
					constraint.RangeMax = value;
			} 
		}

		public ConstraintCollection() { }
		public ConstraintCollection(IEnumerable<Constraint> collection) : base(collection) { }

		public Boolean IsChoosable(IEnumerable<Card> cardCollection, Card card)
		{
			foreach (Constraint constraint in this)
			{
				if (constraint.Matches(card) && !constraint.IsChoosable(cardCollection, card))
					return false;
			}

			return true;
		}

		public new void Add(Constraint item)
		{
			//item.RangeMax = this.MaxCount;
			base.Add(item);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
		}

		public new void AddRange(IEnumerable<Constraint> collection)
		{
			//foreach (Constraint item in collection)
			//    item.RangeMax = this.MaxCount;
			base.AddRange(collection);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, collection));
		}

		public new void Insert(int index, Constraint item)
		{
			//item.RangeMax = this.MaxCount;
			base.Insert(index, item);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
		}

		public new void Remove(Constraint item)
		{
			base.Remove(item);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
		}

		public new void Sort()
		{
			this.Sort(delegate(Constraint c1, Constraint c2) { return -c1.Minimum.CompareTo(c2.Minimum); });
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		internal bool MinimumMet(List<Card> cardCollection)
		{
			foreach (Constraint constraint in this)
			{
				if (!constraint.MinimumMet(cardCollection))
					return false;
			}
			return true;
		}

		protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (CollectionChanged != null)
				CollectionChanged(this, e);
		}

		public IList<DominionBase.Cards.Card> SelectCards(IList<Cards.Card> availableCards, int numberCardsToSelect)
		{
			List<Cards.Card> _CardsChosen = new List<Cards.Card>();

			// Remove all "CardDontUse" constraint cards first
			IList<Cards.Card> usableCards = new List<Cards.Card>(availableCards);
			foreach (Cards.Constraint constraint in this.Where(c => c.ConstraintType == Cards.ConstraintType.CardDontUse))
				foreach (Cards.Card card in constraint.GetMatchingCards(availableCards))
					usableCards.Remove(card);

			IEnumerable<Constraint> usableConstraints = this.Where(c => c.ConstraintType != Cards.ConstraintType.CardDontUse);

			Dictionary<Cards.Constraint, IList<Cards.Card>> constraintCards = new Dictionary<Cards.Constraint, IList<Cards.Card>>();
			foreach (Cards.Constraint constraint in usableConstraints)
				constraintCards[constraint] = new Cards.CardCollection(constraint.GetMatchingCards(availableCards));

			int attempts = 0;
			// Satisfy Minimum constraints first
			do
			{
				attempts++;
				_CardsChosen.Clear();

				// Add in required cards first
				foreach (Cards.Constraint constraint in usableConstraints.Where(c => c.ConstraintType == Cards.ConstraintType.CardMustUse))
					_CardsChosen.AddRange(constraintCards[constraint]);

				if (_CardsChosen.Count > numberCardsToSelect)
					throw new ConstraintException(String.Format("Too many required cards specified in constraints!  Please double-check your setup and loosen the requirements.  {0} needed & found {1} required constraints.", numberCardsToSelect, _CardsChosen.Count));

				foreach (Cards.Constraint constraint in usableConstraints.OrderByDescending(c => c.Minimum))
				{
					if (constraint.MinimumMet(_CardsChosen))
						continue;

					CardCollection discardCards = new CardCollection();
					Utilities.Shuffler.Shuffle(constraintCards[constraint]);
					foreach (Cards.Card card in constraintCards[constraint])
					{
						if (discardCards.Contains(card))
							continue;

						if (this.IsChoosable(_CardsChosen, card))
						{
							_CardsChosen.Add(card);
							if (constraint.MinimumMet(_CardsChosen))
								break;
							// Certain constraints (like NumberOfSets) immediately disallow other cards when a card is chosen.
							// We need to get the list of disallowed cards after each card is added to the list.
							// This is only needed to ensure the Minimum constraint without taking 10k iterations
							discardCards.AddRange(constraint.GetCardsToDiscard(_CardsChosen, constraintCards[constraint]));
						}
					}
				}

				// Give it 50 attempts at trying to satisfy the Minimum requirements
				if (attempts > 50)
					throw new ConstraintException("Cannot satisfy specified constraints!  Please double-check and make sure it's possible to construct a Kingdom card setup with the constraints specified.");

			} while (!this.MinimumMet(_CardsChosen) || _CardsChosen.Count > numberCardsToSelect);

			// After satisfying the Minimums, Maximums should be pretty easy to handle
			List<Cards.Card> _CardsChosenNeeded = new List<Cards.Card>(_CardsChosen);
			attempts = 0;
			while (_CardsChosen.Count < numberCardsToSelect)
			{
				attempts++;
				// Give it 50 attempts at trying to satisfy the Minimum requirements
				if (attempts > 50)
					throw new ConstraintException("Cannot satisfy specified constraints!  Please double-check and make sure it's possible to construct a Kingdom card setup with the constraints specified.");

				_CardsChosen.Clear();
				_CardsChosen.AddRange(_CardsChosenNeeded);
				Utilities.Shuffler.Shuffle(usableCards);

				foreach (Cards.Card chosenCard in usableCards)
				{
					if (_CardsChosen.Contains(chosenCard))
						continue;

					if (this.IsChoosable(_CardsChosen, chosenCard))
						_CardsChosen.Add(chosenCard);

					if (_CardsChosen.Count == numberCardsToSelect)
						break;
				}
			}

			return _CardsChosen;
		}
	}
}