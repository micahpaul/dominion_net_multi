using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DominionBase.Cards;
using DominionBase.Piles;

namespace DominionBase.Players
{
	public class BenefitsChangedEventArgs : EventArgs
	{
		public Player Player;
		public int Actions;
		public int Buys;
		public Currency Currency;
		public BenefitsChangedEventArgs(Player player)
		{
			this.Player = player;
			this.Actions = player.Actions;
			this.Buys = player.Buys;
			// Make a copy of this so it can't be changed
			this.Currency = new Currency(player.Currency);
		}
	}
	public class PhaseChangingEventArgs : EventArgs
	{
		public Player CurrentPlayer;
		public PhaseEnum CurrentPhase;
		public PhaseEnum NewPhase;
		public Boolean Cancelled = false;
		public PhaseChangingEventArgs(Player player, PhaseEnum newPhase)
		{
			CurrentPlayer = player;
			if (player != null)
				CurrentPhase = player.Phase;
			NewPhase = newPhase;
		}
	}
	public class PhaseChangedEventArgs : EventArgs
	{
		public Player CurrentPlayer;
		public PhaseEnum OldPhase;
		public PhaseEnum NewPhase;
		public PhaseChangedEventArgs(Player player, PhaseEnum oldPhase)
		{
			CurrentPlayer = player;
			OldPhase = oldPhase;
			if (player != null)
				NewPhase = player.Phase;
		}
	}
	public class PlayerModeChangedEventArgs : EventArgs
	{
		public Player CurrentPlayer;
		public PlayerMode OldPlayerMode;
		public PlayerMode NewPlayerMode;
		public PlayerModeChangedEventArgs(Player player, PlayerMode oldPlayerMode)
		{
			CurrentPlayer = player;
			OldPlayerMode = oldPlayerMode;
			if (player != null)
				NewPlayerMode = player.PlayerMode;
		}
	}
	public class AttackedEventArgs : EventArgs
	{
		public Player Attacker;
		public Card AttackCard;
		public Boolean Cancelled = false;
		public List<Type> HandledBy = new List<Type>();
		public Dictionary<Type, AttackReaction> Revealable = new Dictionary<Type, AttackReaction>();
		public AttackedEventArgs(Player attacker, Card attackCard)
		{
			Attacker = attacker;
			AttackCard = attackCard;
		}
	}
	public delegate void AttackReveal(Player player, ref AttackedEventArgs attackedEventArgs);
	public class AttackReaction
	{
		private Card _Card = null;
		private String _Text = String.Empty;
		private AttackReveal _Method = null;
		public Card Card { get { return _Card; } }
		public String Text { get { return _Text; } }
		public AttackReveal Method { get { return _Method; } }
		public AttackReaction(Card card, String text, AttackReveal method)
		{
			_Card = card; _Text = text; _Method = method;
		}
	}
	public class CardsDrawnEventArgs : EventArgs
	{
		public CardCollection Cards = new CardCollection();
		public DeckPosition FromDeckPosition;
		public int NumberToDraw;
		public CardsDrawnEventArgs(IEnumerable<Card> cards, DeckPosition fromDeckPosition, int numberToDraw)
		{
			this.Cards.AddRange(cards);
			this.FromDeckPosition = fromDeckPosition;
			this.NumberToDraw = numberToDraw;
		}
	}
	public class CardsAddedToDeckEventArgs : EventArgs
	{
		public CardCollection Cards;
		public DeckPosition DeckPosition;
		public CardsAddedToDeckEventArgs(CardCollection cards, DeckPosition deckPosition)
		{
			this.Cards = cards;
			this.DeckPosition = deckPosition;
		}
		public CardsAddedToDeckEventArgs(IEnumerable<Card> cards, DeckPosition deckPosition)
		{
			this.Cards = new CardCollection(cards);
			this.DeckPosition = deckPosition;
		}
		public CardsAddedToDeckEventArgs(Card card, DeckPosition deckPosition)
		{
			this.Cards = new CardCollection() { card };
			this.DeckPosition = deckPosition;
		}
	}
	public class CardsAddedToHandEventArgs : EventArgs
	{
		public CardCollection Cards;
		public CardsAddedToHandEventArgs(CardCollection cards)
		{
			this.Cards = cards;
		}
		public CardsAddedToHandEventArgs(IEnumerable<Card> cards)
		{
			this.Cards = new CardCollection(cards);
		}
		public CardsAddedToHandEventArgs(Card card)
		{
			this.Cards = new CardCollection() { card };
		}
	}
	public class CardsDiscardEventArgs : EventArgs
	{
		public CardCollection Cards;
		public DeckLocation FromLocation;
		public Boolean Cancelled = false;
		public Dictionary<Tuple<Type, Type>, CardsDiscardAction> Actions = new Dictionary<Tuple<Type,Type>,CardsDiscardAction>();
		public List<Object> HandledBy = new List<Object>();
		public Object Data { get; set; }
		public CardsDiscardEventArgs(DeckLocation fromLocation, CardCollection cards)
		{
			this.FromLocation = fromLocation;
			this.Cards = cards;
		}
		public CardsDiscardEventArgs(DeckLocation fromLocation, IEnumerable<Card> cards)
			: this(fromLocation, new CardCollection(cards))
		{
		}
		public CardsDiscardEventArgs(DeckLocation fromLocation, Card card)
			: this(fromLocation, new CardCollection() { card })
		{
		}
		public Boolean AddAction(Type card, CardsDiscardAction action)
		{
			return AddAction(card, card, action);
		}
		public Boolean AddAction(Type sourceCard, Type card, CardsDiscardAction action)
		{
			Tuple<Type, Type> key = new Tuple<Type,Type>(sourceCard, card);
			if (this.Actions.ContainsKey(key))
				return false;
			this.Actions[key] = action;
			return true;
		}
		public CardsDiscardAction GetAction(Type card)
		{
			return GetAction(card, card);
		}
		public CardsDiscardAction GetAction(Type sourceCard, Type card)
		{
			Tuple<Type, Type> key = new Tuple<Type,Type>(sourceCard, card);
			if (!this.Actions.ContainsKey(key))
				return null;
			return this.Actions[key];
		}
	}
	public delegate void CardsDiscardMethod(Player player, ref CardsDiscardEventArgs cardDiscardEventArgs);
	public class CardsDiscardAction
	{
		private Player _Player = null;
		private Card _Card = null;
		private String _Text = String.Empty;
		private CardsDiscardMethod _Method = null;
		private Boolean _IsRequired = false;
		public Player Player { get { return _Player; } }
		public Card Card { get { return _Card; } }
		public String Text { get { return _Text; } }
		public CardsDiscardMethod Method { get { return _Method; } }
		public Boolean IsRequired { get { return _IsRequired; } }
		public Object Data { get; set; }
		public CardsDiscardAction(Player player, Card card, String text, CardsDiscardMethod method, Boolean isMandatory)
		{
			_Player = player; _Card = card; _Text = text; _Method = method; _IsRequired = isMandatory;
		}
	}
	public class CardGainEventArgs : EventArgs
	{
		public Card Card;
		public DeckLocation Location;
		public DeckPosition Position;
		public Boolean Bought = false;
		public Boolean Cancelled = false;
		public Boolean IsLostTrackOf = false;
		public Game Game;
		public Dictionary<Type, CardGainAction> Actions = new Dictionary<Type, CardGainAction>();
		public List<Object> HandledBy = new List<Object>();
		public CardGainEventArgs(Game game, Card card, DeckLocation location, DeckPosition position, Boolean bought)
		{
			this.Game = game;
			this.Card = card;
			this.Location = location;
			this.Position = position;
			this.Bought = bought;
		}
	}
	public delegate void CardGainMethod(Player player, ref CardGainEventArgs cardGainEventArgs);
	public class CardGainAction
	{
		private Player _Player = null;
		private Card _Card = null;
		private String _Text = String.Empty;
		private CardGainMethod _Method = null;
		private Boolean _IsRequired = false;
		public Player Player { get { return _Player; } }
		public Card Card { get { return _Card; } }
		public String Text { get { return _Text; } }
		public CardGainMethod Method { get { return _Method; } }
		public Boolean IsRequired { get { return _IsRequired; } }
		public CardGainAction(Player player, Card card, String text, CardGainMethod method, Boolean isRequired)
		{
			_Player = player; _Card = card; _Text = text; _Method = method; _IsRequired = isRequired;
		}
	}

	public class CardBuyEventArgs : EventArgs
	{
		public Card Card;
		public Boolean Cancelled = false;
		public Game Game;
		public Dictionary<Type, CardBuyAction> Actions = new Dictionary<Type, CardBuyAction>();
		public List<Object> HandledBy = new List<Object>();
		public CardBuyEventArgs(Game game, Card card)
		{
			Game = game;
			Card = card;
		}
	}
	public delegate void CardBuyMethod(Player player, ref CardBuyEventArgs cardBuyEventArgs);
	public class CardBuyAction
	{
		private Player _Player = null;
		private Card _Card = null;
		private String _Text = String.Empty;
		private CardBuyMethod _Method = null;
		private Boolean _IsRequired = false;
		public Player Player { get { return _Player; } }
		public Card Card { get { return _Card; } }
		public String Text { get { return _Text; } }
		public CardBuyMethod Method { get { return _Method; } }
		public Boolean IsRequired { get { return _IsRequired; } }
		public CardBuyAction(Player player, Card card, String text, CardBuyMethod method, Boolean isRequired)
		{
			_Player = player; _Card = card; _Text = text; _Method = method; _IsRequired = isRequired;
		}
	}

	public class CardReceivedEventArgs : EventArgs
	{
		public Player FromPlayer;
		public Card Card;
		public DeckLocation Location;
		public DeckPosition Position;
		public CardReceivedEventArgs(Player fromPlayer, Card card, DeckLocation location, DeckPosition position)
		{
			this.FromPlayer = fromPlayer;
			this.Card = card;
			this.Location = location;
			this.Position = position;
		}
	}
	public class CardsLostEventArgs : EventArgs
	{
		public CardCollection Cards;
		public CardsLostEventArgs(CardCollection cards)
		{
			this.Cards = cards;
		}
	}
	public class CardPlayingEventArgs : EventArgs
	{
		public CardCollection Cards;
		public Player Player;
		public String Modifier;
		public CardPlayingEventArgs(Player player, Card card, String modifier)
			: this(player, new CardCollection() { card }, modifier)
		{ }
		public CardPlayingEventArgs(Player player, CardCollection cards, String modifier)
		{
			Player = player;
			Cards = cards;
			this.Modifier = modifier;
		}
	}
	public class CardPutIntoPlayEventArgs : EventArgs
	{
		public Card Card;
		public Player Player;
		public CardPutIntoPlayEventArgs(Player player, Card card)
		{
			Player = player;
			Card = card;
		}
	}
	public class CardPlayedEventArgs : EventArgs
	{
		public CardCollection Cards;
		public Player Player;
		public CardPlayedEventArgs(Player player, Card card)
			: this(player, new CardCollection() { card })
		{ }
		public CardPlayedEventArgs(Player player, CardCollection cards)
		{
			Player = player;
			Cards = cards;
		}
	}
	public class CardUndoPlayingEventArgs : EventArgs
	{
		public CardCollection Cards;
		public Player Player;
		public String Modifier;
		public CardUndoPlayingEventArgs(Player player, Card card, String modifier)
			: this(player, new CardCollection() { card }, modifier)
		{ }
		public CardUndoPlayingEventArgs(Player player, CardCollection cards, String modifier)
		{
			Player = player;
			Cards = cards;
			this.Modifier = modifier;
		}
	}
	public class CardUndoPlayedEventArgs : EventArgs
	{
		public CardCollection Cards;
		public Player Player;
		public CardUndoPlayedEventArgs(Player player, Card card)
			: this(player, new CardCollection() { card })
		{ }
		public CardUndoPlayedEventArgs(Player player, CardCollection cards)
		{
			Player = player;
			Cards = cards;
		}
	}
	public class TokenPlayingEventArgs : EventArgs
	{
		public TokenCollection Tokens;
		public Player Player;
		public TokenPlayingEventArgs(Player player, Token token)
			: this(player, new TokenCollection() { token })
		{ }
		public TokenPlayingEventArgs(Player player, TokenCollection tokens)
		{
			Player = player;
			Tokens = tokens;
		}
	}
	public class TokenPlayedEventArgs : EventArgs
	{
		public TokenCollection Tokens;
		public Player Player;
		public TokenPlayedEventArgs(Player player, Token token)
			: this(player, new TokenCollection() { token })
		{ }
		public TokenPlayedEventArgs(Player player, TokenCollection tokens)
		{
			Player = player;
			Tokens = tokens;
		}
	}
	public class TrashEventArgs : EventArgs
	{
		public Player CurrentPlayer;
		public CardCollection TrashedCards;
		public Dictionary<Type, TrashAction> Actions = new Dictionary<Type, TrashAction>();
		public List<Object> HandledBy = new List<Object>();
		public TrashEventArgs(Player player, CardCollection trashedCards)
		{
			CurrentPlayer = player;
			if (trashedCards == null)
				TrashedCards = null;
			else
				TrashedCards = new CardCollection(trashedCards);
		}
	}
	public delegate void TrashMethod(Player player, ref TrashEventArgs trashedEventArgs);
	public class TrashAction
	{
		private Player _Player = null;
		private Card _Card = null;
		private String _Text = String.Empty;
		private TrashMethod _Method = null;
		private Boolean _IsRequired = false;
		public Player Player { get { return _Player; } }
		public Card Card { get { return _Card; } }
		public String Text { get { return _Text; } }
		public TrashMethod Method { get { return _Method; } }
		public Boolean IsRequired { get { return _IsRequired; } }
		public TrashAction(Player player, Card card, String text, TrashMethod method, Boolean isRequired)
		{
			_Player = player; _Card = card; _Text = text; _Method = method; _IsRequired = isRequired;
		}
	}

	public class TurnStartingEventArgs : EventArgs
	{
		public Player Player;
		public Card GrantedBy = null;
		public Boolean Cancelled = false;
		public TurnStartingEventArgs(Player player)
		{
			Player = player;
		}
	}
	public class TurnStartedEventArgs : EventArgs
	{
		public Player Player;
		public Dictionary<String, TurnStartedAction> Actions = new Dictionary<String, TurnStartedAction>();
		public List<Object> HandledBy = new List<Object>();
		public TurnStartedEventArgs(Player player)
		{
			Player = player;
		}
	}
	public delegate void TurnStartedMethod(Player player, ref TurnStartedEventArgs turnStartedEventArgs);
	public class TurnStartedAction
	{
		private Player _Player = null;
		private Card _Card = null;
		private String _Text = String.Empty;
		private TurnStartedMethod _Method = null;
		private Boolean _IsRequired = false;
		public Player Player { get { return _Player; } }
		public Card Card { get { return _Card; } }
		public String Text { get { return _Text; } }
		public TurnStartedMethod Method { get { return _Method; } }
		public Boolean IsRequired { get { return _IsRequired; } }
		public TurnStartedAction(Player player, Card card, String text, TurnStartedMethod method, Boolean isRequired)
		{
			_Player = player; _Card = card; _Text = text; _Method = method; _IsRequired = isRequired;
		}
	}

	public class ShuffleEventArgs : EventArgs
	{
		public Player Player;
		public List<Type> HandledBy = new List<Type>();
		public ShuffleEventArgs(Player player)
		{
			this.Player = player;
		}
	}
	public class CleaningUpEventArgs : EventArgs
	{
		public Player CurrentPlayer;
		public int DrawSize;
		public Boolean Cancelled = false;
		public Player NextPlayer = null;
		public Card NextGrantedBy = null;
		public Dictionary<Type, CleaningUpAction> Actions = new Dictionary<Type, CleaningUpAction>();
		public CardMovementCollection CardsMovements = new CardMovementCollection();
		public CleaningUpEventArgs(Player player, int drawSize, ref CardMovementCollection cardsMovements)
		{
			this.CurrentPlayer = player;
			this.DrawSize = drawSize;
			this.CardsMovements = cardsMovements;
		}
	}
	public delegate void CleaningUpMethod(Player player, ref CleaningUpEventArgs retrievingEventArgs);
	public class CleaningUpAction
	{
		private Card _Card = null;
		private String _Text = String.Empty;
		private CleaningUpMethod _Method = null;
		public Card Card { get { return _Card; } }
		public String Text { get { return _Text; } }
		public CleaningUpMethod Method { get { return _Method; } }
		public Object Data { get; set; }
		public CleaningUpAction(Card card, String text, CleaningUpMethod method)
		{
			_Card = card; _Text = text; _Method = method;
		}
	}

	public class CleanedUpEventArgs : EventArgs
	{
		public Player CurrentPlayer;
		public int DrawSize;
		public CleanedUpEventArgs(Player player, int drawSize)
		{
			CurrentPlayer = player;
			DrawSize = drawSize;
		}
	}
	public class TurnEndedEventArgs : EventArgs
	{
		public Player Player;
		public TurnEndedEventArgs(Player player)
		{
			Player = player;
		}
	}

	public class BenefitReceivingEventArgs : EventArgs
	{
		private PhaseEnum _Phase;
		public CardBenefit Benefit;
		public Visual.VisualPlayer Player;
		public PhaseEnum Phase { get { return _Phase; } private set { _Phase = value; } }
		public BenefitReceivingEventArgs(Player player, CardBenefit cardBenefit)
		{
			this.Player = new Visual.VisualPlayer(player);
			this.Benefit = cardBenefit;
			this.Phase = player.Phase;
		}
	}
}
