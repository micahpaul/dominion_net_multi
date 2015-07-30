using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DominionBase.Cards;
using DominionBase.Piles;
using DominionBase.Players;

namespace DominionBase
{
	public enum ChoiceType
	{
		Unknown,
		Cards,
		Options,
		Supplies,
		SuppliesAndCards
	}

	public delegate void Choose(Player player, Choice choice);

	public class Choice
	{
		private Guid _UniqueId = Guid.NewGuid();
		private String _Text = String.Empty;
		private Card _CardSource = null;
		private CardCollection _CardTriggers = new CardCollection();
		private ChoiceType _ChoiceType = ChoiceType.Unknown;
		private Player _PlayerSource = null;
		private IEnumerable<Card> _Cards = null;
		private Visibility _Visibility = Visibility.All;
		private OptionCollection _Options = null;
		private SupplyCollection _Supplies = null;
		private Boolean _IsOrdered = false;
		private Boolean _IsSpecific = false;
		private int _Minimum = 0;
		private int _Maximum = 0;
		private EventArgs _EventArgs = null;

		private Choice(String text, Card cardSource, CardCollection cardTriggers, ChoiceType choiceType, Player playerSource, EventArgs eventArgs, Boolean isOrdered, Boolean isSpecific, int minimum, int maximum)
		{
			_Text = text;
			_CardSource = cardSource;
			_CardTriggers = cardTriggers;
			_ChoiceType = choiceType;
			_PlayerSource = playerSource;
			_EventArgs = eventArgs;
			_IsOrdered = isOrdered;
			_IsSpecific = isSpecific;
			_Minimum = minimum < 0 ? 0 : minimum;
			_Maximum = maximum < _Minimum ? _Minimum : maximum;
		}

		#region Cards
		public Choice(String text, Card cardSource, IEnumerable<Card> cards, Player playerSource)
			: this(text, cardSource, cards, playerSource, false, 1, 1) { }

		public Choice(String text, Card cardSource, Card cardTrigger, IEnumerable<Card> cards, Player playerSource, Boolean optional, EventArgs eventArgs)
			: this(text, cardSource, new CardCollection() { cardTrigger }, ChoiceType.Cards, playerSource, eventArgs, false, false, optional ? 0 : 1, 1)
		{
			_Cards = cards;
		}

		public Choice(String text, Card cardSource, CardCollection cardTriggers, IEnumerable<Card> cards, Player playerSource, Boolean isOrdered, int minimum, int maximum)
			: this(text, cardSource, cardTriggers, ChoiceType.Cards, playerSource, null, isOrdered, false, minimum, maximum)
		{
			_Cards = cards;
		}

		public Choice(String text, Card cardSource, IEnumerable<Card> cards, Player playerSource, Boolean isOrdered, int minimum, int maximum)
			: this(text, cardSource, cards, Visibility.All, playerSource, isOrdered, minimum, maximum)
		{
		}

		public Choice(String text, Card cardSource, IEnumerable<Card> cards, Visibility visibility, Player playerSource, Boolean isOrdered, int minimum, int maximum)
			: this(text, cardSource, new CardCollection() { cardSource }, ChoiceType.Cards, playerSource, null, isOrdered, false, minimum, maximum)
		{
			_Cards = cards;
			_Visibility = visibility;
		}

		public Choice(String text, Card cardSource, IEnumerable<Card> cards, Player playerSource, Boolean isOrdered, Boolean isSpecific, int minimum, int maximum)
			: this(text, cardSource, new CardCollection() { cardSource }, ChoiceType.Cards, playerSource, null, isOrdered, isSpecific, minimum, maximum)
		{
			_Cards = cards;
		}

		public Choice(String text, Card cardSource, Supply supply, Player playerSource, int minimum, int maximum)
			: this(text, cardSource, new CardCollection() { cardSource }, ChoiceType.Cards, playerSource, null, false, false, minimum, maximum)
		{
			_Cards = new CardCollection();
			foreach (Type cardType in supply.CardTypes)
			{
				if (!supply.CanGain(cardType))
					continue;
				((CardCollection)_Cards).Add(Card.CreateInstance(cardType));
			}
		}
		#endregion

		#region String Options
		public Choice(String text, Card cardSource, CardCollection cardTriggers, List<String> options, Player playerSource)
			: this(text, cardSource, cardTriggers, options, playerSource, null, false, 1, 1) { }

		public Choice(String text, Card cardSource, OptionCollection options, Player playerSource)
			: this(text, cardSource, new CardCollection(), ChoiceType.Options, playerSource, null, false, false, options.IsAnyRequired ? 1 : 0, 1)
		{
			_Options = options;
		}

		public Choice(String text, OptionCollection options, Player playerSource, EventArgs eventArgs)
			: this(text, null, new CardCollection(), ChoiceType.Options, playerSource, eventArgs, false, false, options.IsAnyRequired ? 1 : 0, 1)
		{
			_Options = options;
		}

		public Choice(String text, Card cardSource, CardCollection cardTriggers, List<String> options, Player playerSource, EventArgs eventArgs, Boolean isOrdered, int minimum, int maximum)
			: this(text, cardSource, cardTriggers, ChoiceType.Options, playerSource, eventArgs, isOrdered, false, minimum, maximum)
		{
			_Options = new OptionCollection(options);
		}
		#endregion

		#region Supplies
		public Choice(String text, Card cardSource, SupplyCollection supplies, Player playerSource, Boolean optional)
			: this(text, cardSource, new CardCollection() { cardSource }, ChoiceType.Supplies, playerSource, null, false, false, optional ? 0 : 1, 1)
		{
			_Supplies = supplies;
		}
		#endregion

		public void AddCard(Card card)
		{
			switch (this.ChoiceType)
			{
				case DominionBase.ChoiceType.Cards:
				case DominionBase.ChoiceType.SuppliesAndCards:
					_Cards = _Cards.Concat(new CardCollection { card });
					break;

				case DominionBase.ChoiceType.Supplies:
					_Cards = _Cards.Concat(new CardCollection { card });
					_ChoiceType = DominionBase.ChoiceType.SuppliesAndCards;
					break;

				default:
					throw new UnauthorizedAccessException("Cannot add cards to Choice Type!");
			}
		}

		public void AddCards(IEnumerable<Card> cards)
		{
			switch (this.ChoiceType)
			{ 
				case DominionBase.ChoiceType.Cards:
				case DominionBase.ChoiceType.SuppliesAndCards:
					_Cards = _Cards.Concat(cards);
					break;

				case DominionBase.ChoiceType.Supplies:
					_Cards = cards;
					_ChoiceType = DominionBase.ChoiceType.SuppliesAndCards;
					break;

				default:
					throw new UnauthorizedAccessException("Cannot add cards to Choice Type!");
			}
		}

		public Guid UniqueId { get { return _UniqueId; } }
		public String Text { get { return _Text; } }
		public Card CardSource { get { return _CardSource; } }
		public CardCollection CardTriggers { get { return _CardTriggers; } }
		public Player PlayerSource { get { return _PlayerSource; } }
		public IEnumerable<Card> Cards { get { return _Cards; } }
		public Visibility Visibility { get { return _Visibility; } }
		public OptionCollection Options { get { return _Options; } }
		public SupplyCollection Supplies { get { return _Supplies; } }
		public Boolean IsOrdered { get { return _IsOrdered; } }
		public Boolean IsSpecific { get { return _IsSpecific; } }
		public EventArgs EventArgs { get { return _EventArgs; } }
		public int Minimum { get { return _Minimum; } }
		public int Maximum { get { return _Maximum; } }
		public ChoiceType ChoiceType { get { return _ChoiceType; } }

		public static Choice CreateYesNoChoice(String text, Card cardSource, Player playerSource)
		{
			return new Choice(text, cardSource, new CardCollection() { cardSource }, new List<String>() { "Yes", "No" }, playerSource);
		}
		public static Choice CreateYesNoChoice(String text, Card cardSource, Card cardTrigger, Player playerSource, EventArgs eventArgs)
		{
			return new Choice(text, cardSource, new CardCollection() { cardTrigger }, new List<String>() { "Yes", "No" }, playerSource, eventArgs, false, 1, 1);
		}
	}

	public class ChoiceResult
	{
		private ChoiceType _ChoiceType = ChoiceType.Unknown;
		private CardCollection _Cards = null;
		private List<String> _Options = null;
		private Supply _Supply = null;

		private ChoiceResult(ChoiceType choiceType)
		{
			_ChoiceType = choiceType;
		}

		public ChoiceResult(CardCollection cards)
			: this(ChoiceType.Cards)
		{
			_Cards = cards;
		}

		public ChoiceResult(List<String> options)
			: this(ChoiceType.Options)
		{
			_Options = options;
		}

		public ChoiceResult(OptionCollection options)
			: this(ChoiceType.Options)
		{
			_Options = new List<String>(options.Select(o => o.Text));
		}

		public ChoiceResult(Supply supply)
			: this(ChoiceType.Supplies)
		{
			_Supply = supply;
		}

		public ChoiceResult()
			: this(ChoiceType.Unknown)
		{
		}

		public ChoiceType ChoiceType { get { return _ChoiceType; } }
		public CardCollection Cards { get { return _Cards; } }
		public List<String> Options { get { return _Options; } }
		public Supply Supply { get { return _Supply; } }
	}
}