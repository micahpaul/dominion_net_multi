using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;

using DominionBase.Cards;
using DominionBase.Piles;

namespace DominionBase.Players
{
	public abstract class PlayerMessage
	{
		public String Message { get; set; }
		public AutoResetEvent ReturnEvent { get; set; }

		public PlayerMessage() { }
		public PlayerMessage(String message) : this(null, message) { }
		public PlayerMessage(AutoResetEvent returnEvent) { ReturnEvent = returnEvent; }
		public PlayerMessage(AutoResetEvent returnEvent, String message) : this(returnEvent) { Message = message; }
	}
	public class PlayerResponseMessage : PlayerMessage { }
	public class PlayerChoiceMessage : PlayerMessage
	{
		public Player Player { get; set; }
		public ChoiceResult ChoiceResult { get; set; }
		public PlayerChoiceMessage() { }
		public PlayerChoiceMessage(Player player, ChoiceResult choiceResult)
			: this(null, player, choiceResult)
		{
		}
		public PlayerChoiceMessage(AutoResetEvent returnEvent, Player player, ChoiceResult choiceResult)
			: base(returnEvent)
		{
			Player = player;
			ChoiceResult = choiceResult;
		}
	}

	public enum DeckLocation
	{
		Hand,
		Revealed,
		Discard,
		Deck,
		InPlay,
		SetAside,
		PlayerMat,
		Private,
		InPlayAndSetAside
	}

	public enum PlayerType
	{
		Human,
		Computer
	}

	public delegate void TakeTurn(Game game, Player player);

	public abstract class Player : IDisposable
	{
		#region Delegates & Events
		public delegate void AttackedEventHandler(object sender, AttackedEventArgs e);
		public virtual event AttackedEventHandler Attacked = null;
		public delegate void CardsDrawnEventHandler(object sender, CardsDrawnEventArgs e);
		public virtual event CardsDrawnEventHandler CardsDrawn = null;
		public delegate void CardsAddedToDeckEventHandler(object sender, CardsAddedToDeckEventArgs e);
		public virtual event CardsAddedToDeckEventHandler CardsAddedToDeck = null;
		public delegate void CardsAddedToHandEventHandler(object sender, CardsAddedToHandEventArgs e);
		public virtual event CardsAddedToHandEventHandler CardsAddedToHand = null;
		public delegate void CardsDiscardingEventHandler(object sender, CardsDiscardEventArgs e);
		public virtual event CardsDiscardingEventHandler CardsDiscarding = null;
		public delegate void CardsDiscardEventHandler(object sender, CardsDiscardEventArgs e);
		public virtual event CardsDiscardEventHandler CardsDiscard = null;
		public delegate void CardsDiscardedEventHandler(object sender, CardsDiscardEventArgs e);
		public virtual event CardsDiscardedEventHandler CardsDiscarded = null;

		public delegate void CardBuyingEventHandler(object sender, CardBuyEventArgs e);
		public virtual event CardBuyingEventHandler CardBuying = null;
		public delegate void CardBoughtEventHandler(object sender, CardBuyEventArgs e);
		public virtual event CardBoughtEventHandler CardBought = null;
		public delegate void CardBuyFinishedEventHandler(object sender, CardBuyEventArgs e);
		public virtual event CardBuyFinishedEventHandler CardBuyFinished = null;

		public delegate void CardGainingEventHandler(object sender, CardGainEventArgs e);
		public virtual event CardGainingEventHandler CardGaining = null;
		public delegate void CardGainedEventHandler(object sender, CardGainEventArgs e);
		public virtual event CardGainedEventHandler CardGained = null;
		public delegate void CardGainedIntoEventHandler(object sender, CardGainEventArgs e);
		public virtual event CardGainedIntoEventHandler CardGainedInto = null;
		public delegate void CardGainFinishedEventHandler(object sender, CardGainEventArgs e);
		public virtual event CardGainFinishedEventHandler CardGainFinished = null;

		public delegate void CardReceivedEventHandler(object sender, CardReceivedEventArgs e);
		public virtual event CardReceivedEventHandler CardReceived = null;
		public delegate void CardsLostEventHandler(object sender, CardsLostEventArgs e);
		public virtual event CardsLostEventHandler CardsLost = null;
		public delegate void CardPlayingEventHandler(object sender, CardPlayingEventArgs e);
		public virtual event CardPlayingEventHandler CardPlaying = null;
		public delegate void CardPuttingIntoPlayEventHandler(object sender, CardPutIntoPlayEventArgs e);
		public virtual event CardPuttingIntoPlayEventHandler CardPuttingIntoPlay = null;
		public delegate void CardPutIntoPlayEventHandler(object sender, CardPutIntoPlayEventArgs e);
		public virtual event CardPutIntoPlayEventHandler CardPutIntoPlay = null;
		public delegate void CardPlayedEventHandler(object sender, CardPlayedEventArgs e);
		public virtual event CardPlayedEventHandler CardPlayed = null;
		public delegate void CardUndoPlayingEventHandler(object sender, CardUndoPlayingEventArgs e);
		public virtual event CardUndoPlayingEventHandler CardUndoPlaying = null;
		public delegate void CardUndoPlayedEventHandler(object sender, CardUndoPlayedEventArgs e);
		public virtual event CardUndoPlayedEventHandler CardUndoPlayed = null;
		public delegate void PhaseChangingEventHandler(object sender, PhaseChangingEventArgs e);
		public virtual event PhaseChangingEventHandler PhaseChanging = null;
		public delegate void PhaseChangedEventHandler(object sender, PhaseChangedEventArgs e);
		public virtual event PhaseChangedEventHandler PhaseChanged = null;
		public delegate void PlayerModeChangedEventHandler(object sender, PlayerModeChangedEventArgs e);
		public virtual event PlayerModeChangedEventHandler PlayerModeChanged = null;

		public delegate void TokenPlayingEventHandler(object sender, TokenPlayingEventArgs e);
		public virtual event TokenPlayingEventHandler TokenPlaying = null;
		public delegate void TokenPlayedEventHandler(object sender, TokenPlayedEventArgs e);
		public virtual event TokenPlayedEventHandler TokenPlayed = null;

		public delegate void TrashingEventHandler(object sender, TrashEventArgs e);
		public virtual event TrashingEventHandler Trashing = null;
		public delegate void TrashedEventHandler(object sender, TrashEventArgs e);
		public virtual event TrashedEventHandler Trashed = null;
		public delegate void TrashedFinishedEventHandler(object sender, TrashEventArgs e);
		public virtual event TrashedFinishedEventHandler TrashedFinished = null;

		public delegate void TurnStartingEventHandler(object sender, TurnStartingEventArgs e);
		public virtual event TurnStartingEventHandler TurnStarting = null;
		public delegate void TurnStartedEventHandler(object sender, TurnStartedEventArgs e);
		public virtual event TurnStartedEventHandler TurnStarted = null;
		public delegate void ShufflingEventHandler(object sender, ShuffleEventArgs e);
		public virtual event ShufflingEventHandler Shuffling = null;
		public delegate void ShuffledEventHandler(object sender, ShuffleEventArgs e);
		public virtual event ShuffledEventHandler Shuffled = null;
		public delegate void CleaningUpEventHandler(object sender, CleaningUpEventArgs e);
		public virtual event CleaningUpEventHandler CleaningUp = null;
		public delegate void CleanedUpEventHandler(object sender, CleanedUpEventArgs e);
		public virtual event CleanedUpEventHandler CleanedUp = null;
		public delegate void TurnEndedEventHandler(object sender, TurnEndedEventArgs e);
		public virtual event TurnEndedEventHandler TurnEnded = null;
		public delegate void BenefitReceivingEventHandler(object sender, BenefitReceivingEventArgs e);
		public virtual event BenefitReceivingEventHandler BenefitReceiving = null;
		public delegate void BenefitsChangedEventHandler(object sender, BenefitsChangedEventArgs e);
		public virtual event BenefitsChangedEventHandler BenefitsChanged = null;
		public virtual event Token.TokenActionEventHandler TokenActedOn = null;
		#endregion

		protected PlayerType _PlayerType;
		private String _Name = String.Empty;

		internal Game _Game = null;
		private Deck _Hand = new Deck(DeckLocation.Hand, Visibility.All, VisibilityTo.All, new DominionBase.Cards.Sorting.ByTypeName(DominionBase.Cards.Sorting.SortDirection.Descending), true);
		private Deck _DrawPile = new Deck(DeckLocation.Deck, Visibility.None, VisibilityTo.All);
		private Deck _DiscardPile = new Deck(DeckLocation.Discard, Visibility.Top, VisibilityTo.All);
		private Deck _InPlay = new Deck(DeckLocation.InPlay, Visibility.All, VisibilityTo.All, null, true);
		private Deck _SetAside = new Deck(DeckLocation.SetAside, Visibility.All, VisibilityTo.All, null, true);
		private Deck _Revealed = new Deck(DeckLocation.Revealed, Visibility.All, VisibilityTo.All, null, true);
		private Deck _Private = new Deck(DeckLocation.Private, Visibility.All, VisibilityTo.Owner, null, true);

		private Guid _UniqueId = Guid.NewGuid();

		private PhaseEnum _PhaseEnum = PhaseEnum.Waiting;
		private PlayerMode _PlayerMode = PlayerMode.Waiting;

		private int _Actions = 1;
		private int _Buys = 1;
		private Currency _Currency = new Currency();

		private int _ActionsPlayed = 0;

		private CardMats _PlayerMats = new CardMats();
		private TokenCollections _TokenPiles = new TokenCollections();

		public Choose Choose = null;
		public TakeTurn TakeTurn = null;

		private Turn _CurrentTurn = null;

		protected Boolean _AsynchronousDrawing = false;
		protected CardsDrawnEventArgs _AsynchronousCardsDrawnEventArgs = null;

		private Queue<PlayerMessage> _MessageRequestQueue = new Queue<PlayerMessage>();
		private Queue<PlayerMessage> _MessageResponseQueue = new Queue<PlayerMessage>();
		public Queue<PlayerMessage> MessageRequestQueue
		{
			get { return _MessageRequestQueue; }
		}
		public Queue<PlayerMessage> MessageResponseQueue
		{
			get { return _MessageResponseQueue; }
		}
		public AutoResetEvent WaitEvent = new AutoResetEvent(false);

		private Dictionary<Card, Boolean> _LostTrackStack = new Dictionary<Card, Boolean>();

		public Player(Game game, String name)
		{
			_Game = game;
			_Name = name;
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
					this._Currency = null;
					this._CurrentTurn = null;
					this._DiscardPile = null;
					this._DrawPile = null;
					this._Game = null;
					this._Hand = null;
					this._MessageRequestQueue = null;
					this._MessageResponseQueue = null;
					this._PlayerMats = null;
					this._SetAside = null;
					this._Private = null;
					this._Revealed = null;
					this._InPlay = null;
					this._TokenPiles = null;
				}

				// Call the appropriate methods to clean up
				// unmanaged resources here.
				// If disposing is false,
				// only the following code is executed.

				// Note disposing has been done.
				disposed = true;
			}
		}

		~Player()
		{
			Dispose(false);
		}
		#endregion

		public void InitializeDeck()
		{
			this.Gain(_Game.Table.Copper, 7);
			this.Gain(_Game.Table.Estate, 3);
		}

		public void FinalizeDeck()
		{
			DrawHand(5);
		}

		public Guid UniqueId { get { return _UniqueId; } }

		/// <summary>
		/// Fires off the Attack event, so any listeners can pick it up.
		/// </summary>
		/// <param name="player">The attacking player</param>
		/// <param name="attackingCard">The card that triggered the Attack event</param>
		/// <returns>"true" if the Attack can proceed (wasn't blocked).  "false" if the Attack was blocked.</returns>
		/// <summary>
		public Boolean AttackedBy(Player attacker, Card attackingCard)
		{
			if (Attacked != null)
			{
				AttackedEventArgs aea = null;
				Boolean cancelled = false;
				do
				{
					aea = new AttackedEventArgs(attacker, attackingCard);
					aea.Cancelled |= cancelled;
					Attacked(this, aea);

					CardCollection cards = new CardCollection(aea.Revealable.Values.Select(s => s.Card));
					if (cards.Count > 0)
					{
						cards.Sort();
						Choice choice = new Choice("Reveal a card?", null, attackingCard, cards, this, true, aea);
						ChoiceResult result = this.MakeChoice(choice);

						if (result.Cards.Count > 0)
							aea.Revealable[result.Cards[0].CardType].Method(this, ref aea);
						else
							break;
					}

					cancelled |= aea.Cancelled;

				} while (Attacked != null && aea.HandledBy.Count > 0);

				if (aea != null)
					cancelled |= aea.Cancelled;

				return !cancelled;
			}
			return true;
		}

		/// <summary>
		/// Fires off the TokenAction event, so any listeners can pick it up.
		/// </summary>
		/// <param name="actor">The acting player (the one playing the card)</param>
		/// <param name="actingCard">The card that triggered the TokenAction event</param>
		/// <returns>"true" if the TokenAction can proceed (wasn't blocked).  "false" if the TokenAction was blocked.</returns>
		public Boolean TokenActOn(Player actor, Card actingCard)
		{
			if (TokenActedOn != null)
			{
				TokenActionEventArgs taea = new TokenActionEventArgs(actor, this, actingCard);
				TokenActedOn(this, taea);
				return !taea.Cancelled;
			}
			return true;
		}

		public PlayerType PlayerType { get { return _PlayerType; } }
		public String Name { get { return _Name; } }

		public Deck Hand { get { return _Hand; } }
		public Deck DrawPile { get { return _DrawPile; } }
		public Deck DiscardPile { get { return _DiscardPile; } }
		public Deck InPlay { get { return _InPlay; } }
		public Deck SetAside { get { return _SetAside; } }
		public Deck Revealed { get { return _Revealed; } }
		public Deck Private { get { return _Private; } }
		public DeckCollection InPlayAndSetAside { get { return new DeckCollection(_InPlay, _SetAside); } }
		public CardMats PlayerMats { get { return _PlayerMats; } }
		public TokenCollections TokenPiles { get { return _TokenPiles; } }

		public Turn CurrentTurn { get { return _CurrentTurn; } }

		public virtual void StartAsync() { }

		public ChoiceResult MakeChoice(Choice choice)
		{
			PlayerMode previous = this.PlayerMode;
			PlayerMode = Players.PlayerMode.Choosing;

			CardCollection cards = null;
			ChoiceResult result = null;
			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					if (!choice.IsOrdered && choice.Minimum >= choice.Options.Count)
						result = new ChoiceResult(choice.Options);
					break;

				case ChoiceType.Cards:
					//if (choice.IsSpecific && choice.Minimum == 1 && choice.Cards.Count() == 1)
					//    result = new ChoiceResult(new CardCollection(choice.Cards));
					//else 
					IEnumerable<Card> nonDummyCards = choice.Cards.Where(c => c.CardType != Cards.Universal.TypeClass.Dummy);
					if (nonDummyCards.Count() == 0)
						cards = new CardCollection();
					else if (!choice.IsOrdered && choice.Minimum >= nonDummyCards.Count())
						result = new ChoiceResult(new CardCollection(nonDummyCards));
					else if (choice.Maximum == 0)
						cards = new CardCollection();
					else if (choice.Maximum == choice.Minimum &&
							(!choice.IsOrdered || nonDummyCards.Count(card => card.CardType == nonDummyCards.ElementAt(0).CardType) == nonDummyCards.Count()))
						cards = new CardCollection(nonDummyCards);
					break;

				case ChoiceType.Supplies:
					if (choice.Supplies.Count == 1 && choice.Minimum > 0)
						result = new ChoiceResult(choice.Supplies.Values.First());
					else if (choice.Supplies.Count == 0)
						result = new ChoiceResult();
					break;

				case ChoiceType.SuppliesAndCards:
					if (choice.Supplies.Count == 1 && choice.Cards.Count() == 0)
						result = new ChoiceResult(choice.Supplies.Values.First());
					else if (choice.Supplies.Count == 0 && choice.Cards.Count() == 1)
						result = new ChoiceResult(new CardCollection { choice.Cards.First() });
					else if (choice.Supplies.Count == 0)
						result = new ChoiceResult();
					break;

				default:
					throw new Exception("Unable to do anything with this Choice Type!");
			}
			if (result == null && cards != null)
			{
				if (cards.All(delegate(Card c) { return c.CardType == cards[0].CardType; }))
					result = new ChoiceResult(new CardCollection(cards.Take(choice.Minimum)));
			}
			if (result == null)
			{
				Thread choiceThread = new Thread(delegate()
				{
					Choose.Invoke(this, choice);
				});
				choiceThread.Start();
				WaitEvent.WaitOne();
				choiceThread = null;

				if (_MessageRequestQueue.Count > 0)
				{
					lock (_MessageRequestQueue)
					{
						PlayerMessage message = _MessageRequestQueue.Dequeue();
						//System.Diagnostics.Trace.WriteLine(String.Format("Message: {0}", message.Message));
						PlayerMessage response = new PlayerResponseMessage();
						response.Message = "ACK";

						if (message is PlayerChoiceMessage)
							result = ((PlayerChoiceMessage)message).ChoiceResult;

						lock (_MessageResponseQueue)
						{
							_MessageResponseQueue.Enqueue(response);
						}
						if (message.ReturnEvent != null)
							message.ReturnEvent.Set();
					}
				}
			}

			this.PlayerMode = previous;
			return result;
		}

		public PhaseEnum Phase
		{
			get { return _PhaseEnum; }
			private set
			{
				try
				{
					Boolean phaseChanged = false;

					PhaseEnum newValue = value;
					PhaseEnum oldValue = _PhaseEnum;
					// If we're going into the Action phase and there are no Action cards,
					// then we can immediately go to the Buy phase.
					if (newValue == PhaseEnum.Action &&
						(this.Actions == 0 || Hand[Category.Action].Count == 0))
					{
						newValue = PhaseEnum.BuyTreasure;
					}
					// Similarly, if we're going into the Treasure-playing phase and there are no Treasure
					// cards or any playable Tokens, then we can immediately go to the Buy phase.
					if (newValue == PhaseEnum.BuyTreasure &&
						Hand[Category.Treasure].Count == 0 && 
						!this.TokenPiles.IsAnyPlayable)
					{
						newValue = PhaseEnum.Buy;
					}

					if (newValue == PhaseEnum.Endgame)
					{
						this.Hand.Collate = true;
						this.Hand.Comparer = new DominionBase.Cards.Sorting.ForEndgame();
						this.Hand.Sort();
					}


					if (_PhaseEnum != newValue)
					{
						phaseChanged = true;
						if (PhaseChanging != null)
						{
							PhaseChangingEventArgs pcea = new PhaseChangingEventArgs(this, newValue);
							PhaseChanging(this, pcea);
							if (pcea.Cancelled)
								return;
						}

						_PhaseEnum = newValue;

						if (phaseChanged && PhaseChanged != null)
						{
							PhaseChangedEventArgs pcea = new PhaseChangedEventArgs(this, oldValue);
							PhaseChanged(this, pcea);
						}

						if (newValue == PhaseEnum.Waiting || newValue == PhaseEnum.Starting || newValue == PhaseEnum.Endgame)
							this.PlayerMode = Players.PlayerMode.Waiting;
						else
							this.PlayerMode = Players.PlayerMode.Normal;
					}
					else
					{
					}

				}
				catch (Exception ex)
				{
					Utilities.Logging.LogError(ex);
					throw;
				}

			}
		}

		public PlayerMode PlayerMode
		{
			get { return _PlayerMode; }
			set
			{
				//if (_PlayerMode != value)
				//{
					PlayerMode oldValue = _PlayerMode;

					_PlayerMode = value;
					if (PlayerModeChanged != null)
					{
						PlayerModeChangedEventArgs pcea = new PlayerModeChangedEventArgs(this, oldValue);
						PlayerModeChanged(this, pcea);
					}
				//}
			}
		}

		public int Actions 
		{ 
			get 
			{ 
				return _Actions;

			} 
			internal set {
				_Actions = value; 
				if (BenefitsChanged != null)
				{
					BenefitsChangedEventArgs bcea = new BenefitsChangedEventArgs(this);
					BenefitsChanged(this, bcea);
				}
			}
		}
		public int Buys
		{ 
			get 
			{
				return _Buys;
			} 
			protected set 
			{ 
				_Buys = value;
				if (BenefitsChanged != null)
				{
					BenefitsChangedEventArgs bcea = new BenefitsChangedEventArgs(this);
					BenefitsChanged(this, bcea);
				}
				// Done buying cards -- no more left
			} 
		}
		public Currency Currency
		{
			get
			{
				if (_PhaseEnum == PhaseEnum.Starting || _PhaseEnum == PhaseEnum.Action || _PhaseEnum == PhaseEnum.ActionTreasure ||
					_PhaseEnum == PhaseEnum.BuyTreasure || _PhaseEnum == PhaseEnum.Buy || _PlayerMode == PlayerMode.Playing ||
					_PlayerMode == PlayerMode.Choosing)
					return _Currency;
				return new Currency();
			}
			protected set { 
				_Currency = value; 
				if (BenefitsChanged != null)
				{
					BenefitsChangedEventArgs bcea = new BenefitsChangedEventArgs(this);
					BenefitsChanged(this, bcea);
				}
			}
		}

		public int VictoryChits 
		{ 
			get 
			{
				if (this.TokenPiles.ContainsKey(Cards.Prosperity.TypeClass.VictoryToken))
					return this.TokenPiles[Cards.Prosperity.TypeClass.VictoryToken].Count;
				return 0;
			} 
		}
		public int ActionsPlayed { get { return _ActionsPlayed; } }
		internal void ActionPlayed()
		{
			_ActionsPlayed++;
			this.Actions--;
		}
		internal void UndoActionPlayed()
		{
			_ActionsPlayed--;
			this.Actions++;
		}
		internal void UndoPhaseChange(PhaseEnum newPhase)
		{
			if (this.Phase == PhaseEnum.Buy && newPhase == PhaseEnum.BuyTreasure && this.CurrentTurn.CardsBought.Count == 0)
			{
				this.Phase = PhaseEnum.BuyTreasure;
				return;
			}
			//if (this.Phase == PhaseEnum.Treasure && newPhase == PhaseEnum.Action && this.CurrentTurn.CardsPlayed.Count(c => (c.Category & Category.Treasure) == Category.Treasure) == 0)
			//{
			//    this.Phase = PhaseEnum.Action;
			//    this.Actions = 0;
			//    return;
			//}
		}

		public int VictoryPoints
		{
			get 
			{
				int vp = this.VictoryChits;
				if (this.Phase == PhaseEnum.Endgame)
					lock (_Hand)
						vp += _Hand.VictoryPoints;
				lock (_InPlay)
					vp += _InPlay.VictoryPoints;
				lock (_Revealed)
					vp += _Revealed.VictoryPoints;
				lock (_SetAside)
					vp += _SetAside.VictoryPoints;
				lock (_Private)
					vp += _Private.VictoryPoints;
				foreach (Deck deck in this._PlayerMats.Values)
					lock (deck)
						vp += deck.VictoryPoints;
				return vp;
			}
		}

		internal virtual void Setup(Game game)
		{
		}

		internal virtual void Clear()
		{
			this._Game = null;

			this._Hand.Clear();
			this._DrawPile.Clear();
			this._DiscardPile.Clear();
			this._InPlay.Clear();
			this._SetAside.Clear();
			this._Revealed.Clear();
			this._Private.Clear();

			this._PlayerMats.Clear();
			this._TokenPiles.Clear();
			if (this._CurrentTurn != null)
				this._CurrentTurn.Clear();

			this._MessageRequestQueue.Clear();
			this._MessageResponseQueue.Clear();
		}

		internal virtual void TearDown()
		{
			this._Hand.TearDown();
			this._DrawPile.TearDown();
			this._DiscardPile.TearDown();
			this._InPlay.TearDown();
			this._SetAside.TearDown();
			this._Revealed.TearDown();
			this._Private.TearDown();

			this._PlayerMats.TearDown();
			this._TokenPiles.TearDown();
		}

		public void TestFireAllEvents()
		{
			if (Attacked != null)
				Attacked(this, new AttackedEventArgs(null, null));
			if (BenefitReceiving != null)
				BenefitReceiving(this, new BenefitReceivingEventArgs(null, null));
			if (BenefitsChanged != null)
				BenefitsChanged(this, new BenefitsChangedEventArgs(null));
			CardBuyEventArgs cbea = new CardBuyEventArgs(null, null);
			if (CardBought != null)
				CardBought(this, cbea);
			if (CardBuying != null)
				CardBuying(this, cbea);
			if (CardBuyFinished != null)
				CardBuyFinished(this, cbea);
			CardGainEventArgs cgea = new CardGainEventArgs(null, null, DeckLocation.Discard, DeckPosition.Automatic, false);
			if (CardGained != null)
				CardGained(this, cgea);
			if (CardGainedInto != null)
				CardGainedInto(this, cgea);
			if (CardGainFinished != null)
				CardGainFinished(this, cgea);
			if (CardGaining != null)
				CardGaining(this, cgea);
			if (CardPlayed != null)
				CardPlayed(this, new CardPlayedEventArgs(null, new Cards.Universal.Copper()));
			if (CardPlaying != null)
				CardPlaying(this, new CardPlayingEventArgs(null, new Cards.Universal.Copper(), null));
			if (CardReceived != null)
				CardReceived(this, new CardReceivedEventArgs(null, null, DeckLocation.Discard, DeckPosition.Automatic));
			if (CardsAddedToDeck != null)
				CardsAddedToDeck(this, new CardsAddedToDeckEventArgs(new Cards.Universal.Copper(), DeckPosition.Automatic));
			if (CardsAddedToHand != null)
				CardsAddedToHand(this, new CardsAddedToHandEventArgs(new Cards.Universal.Copper()));
			if (CardsDiscarded != null)
				CardsDiscarded(this, new CardsDiscardEventArgs(DeckLocation.Discard, new Cards.Universal.Copper()));
			if (CardsDiscarding != null)
				CardsDiscarding(this, new CardsDiscardEventArgs(DeckLocation.Discard, new Cards.Universal.Copper()));
			if (CardsDrawn != null)
				CardsDrawn(this, new CardsDrawnEventArgs(null, DeckPosition.Automatic, 0));
			if (CardsLost != null)
				CardsLost(this, new CardsLostEventArgs(null));
			if (CleanedUp != null)
				CleanedUp(this, new CleanedUpEventArgs(null, 0));
			if (CleaningUp != null)
			{
				CardMovementCollection cmc = new CardMovementCollection();
				CleaningUp(this, new CleaningUpEventArgs(null, 0, ref cmc));
			}
			if (PhaseChanged != null)
				PhaseChanged(this, new PhaseChangedEventArgs(null, PhaseEnum.Endgame));
			if (PhaseChanging != null)
				PhaseChanging(this, new PhaseChangingEventArgs(null, PhaseEnum.Endgame));
			if (Shuffling != null)
				Shuffling(this, new ShuffleEventArgs(null));
			if (Shuffled != null)
				Shuffled(this, new ShuffleEventArgs(null));
			if (Trashing != null)
				Trashing(this, new TrashEventArgs(null, null));
			if (Trashed != null)
				Trashed(this, new TrashEventArgs(null, null));
			if (TrashedFinished != null)
				TrashedFinished(this, new TrashEventArgs(null, null));
			if (TurnEnded != null)
				TurnEnded(this, new TurnEndedEventArgs(null));
			if (TurnStarted != null)
				TurnStarted(this, new TurnStartedEventArgs(null));
			if (TurnStarting != null)
				TurnStarting(this, new TurnStartingEventArgs(null));
		}

		public IPile ResolveDeck(DeckLocation location)
		{
			switch (location)
			{
				case DeckLocation.Hand:
					return this.Hand;

				case DeckLocation.Revealed:
					return this.Revealed;

				case DeckLocation.Discard:
					return this.DiscardPile;

				case DeckLocation.Deck:
					return this.DrawPile;

				case DeckLocation.InPlay:
					return this.InPlay;

				case DeckLocation.SetAside:
					return this.SetAside;

				case DeckLocation.Private:
					return this.Private;

				case DeckLocation.InPlayAndSetAside:
					return this.InPlayAndSetAside;
			}
			return null;
		}

		public DeckPosition ResolveDeckPosition(DeckLocation location, DeckPosition position)
		{
			switch (location)
			{
				case DeckLocation.Hand:
					return position == DeckPosition.Automatic ? DeckPosition.Bottom : position;

				case DeckLocation.Revealed:
					return position == DeckPosition.Automatic ? DeckPosition.Bottom : position;

				case DeckLocation.Discard:
					return position == DeckPosition.Automatic ? DeckPosition.Top : position;

				case DeckLocation.Deck:
					return position == DeckPosition.Automatic ? DeckPosition.Top : position;

				case DeckLocation.InPlay:
					return position == DeckPosition.Automatic ? DeckPosition.Bottom : position;

				case DeckLocation.SetAside:
					return position == DeckPosition.Automatic ? DeckPosition.Bottom : position;

				case DeckLocation.PlayerMat:
					return position == DeckPosition.Automatic ? DeckPosition.Bottom : position;

				case DeckLocation.Private:
					return position == DeckPosition.Automatic ? DeckPosition.Bottom : position;

				case DeckLocation.InPlayAndSetAside:
					return position == DeckPosition.Automatic ? DeckPosition.Bottom : position;
			}
			return position;
		}
		public virtual void BeginDrawing()
		{
			this.Revealed.BeginChanges();
			_AsynchronousDrawing = true;
			_AsynchronousCardsDrawnEventArgs = null;
		}

		public virtual void EndDrawing()
		{
			_AsynchronousDrawing = false;
			if (_AsynchronousCardsDrawnEventArgs != null && CardsDrawn != null)
			{
				CardsDrawn(this, _AsynchronousCardsDrawnEventArgs);
			}
			_AsynchronousCardsDrawnEventArgs = null;
			this.Revealed.EndChanges();
		}

		public Boolean CanDraw
		{
			get { return _DrawPile.Count + _DiscardPile.Count > 0; }
		}
		public CardCollection DrawHand(int number)
		{
			CardCollection cc = Draw(number, DeckLocation.Hand);
			PlayerMode = Players.PlayerMode.Waiting;
			return cc;
		}
		public Card Draw(DeckLocation destination) { return Draw(1, destination).FirstOrDefault(); }
		public CardCollection Draw(int number, DeckLocation destination)
		{
			CardCollection cardsDrawn = DrawFrom(DeckPosition.Top, number, destination);
			return cardsDrawn;
		}
		public Card Draw(Type deckType) { return Draw(1, deckType).FirstOrDefault(); }
		public CardCollection Draw(int number, Type deckType)
		{
			CardCollection cardsDrawn = DrawFrom(DeckPosition.Top, number, deckType);
			return cardsDrawn;
		}
		public CardCollection DrawFrom(DeckPosition deckPosition, int number, Object destination)
		{
			CardCollection cards = new CardCollection();
			if (number <= 0)
				return cards;

			CardCollection cardsFirst = _DrawPile.Retrieve(this, deckPosition, c => true, number);
			cards.AddRange(cardsFirst);
			cards.RemovedFrom(DeckLocation.Deck, this);

			if (_AsynchronousDrawing)
			{
				if (_AsynchronousCardsDrawnEventArgs == null)
					_AsynchronousCardsDrawnEventArgs = new CardsDrawnEventArgs(cardsFirst, deckPosition, number);
				else
					_AsynchronousCardsDrawnEventArgs.Cards.AddRange(cardsFirst);
			}
			else if (CardsDrawn != null)
			{
				CardsDrawnEventArgs cdea = new CardsDrawnEventArgs(cardsFirst, deckPosition, number);
				CardsDrawn(this, cdea);
			}

			if (destination is Type)
				this.AddCardsInto((Type)destination, cardsFirst);
			else if (destination is DeckLocation)
				this.AddCardsInto((DeckLocation)destination, cardsFirst);
			else
				throw new Exception(String.Format("Destination of {0} ({1}) is not supported", destination, destination.GetType()));
			
			if (cardsFirst.Count < number && _DrawPile.Count == 0 && _DiscardPile.Count > 0)
			{
				this.ShuffleForDrawing();

				CardCollection cardsSecond = _DrawPile.Retrieve(this, deckPosition, c => true, number < 0 ? number : number - cards.Count);
				cards.AddRange(cardsSecond);
				cardsSecond.RemovedFrom(DeckLocation.Deck, this);

				if (_AsynchronousDrawing)
				{
					if (_AsynchronousCardsDrawnEventArgs == null)
						_AsynchronousCardsDrawnEventArgs = new CardsDrawnEventArgs(cardsSecond, deckPosition, number);
					else
						_AsynchronousCardsDrawnEventArgs.Cards.AddRange(cardsSecond);
				}
				else if (CardsDrawn != null)
				{
					CardsDrawnEventArgs cdea = new CardsDrawnEventArgs(cardsSecond, deckPosition, number);
					CardsDrawn(this, cdea);
				}

				if (destination is Type)
					this.AddCardsInto((Type)destination, cardsSecond);
				else if (destination is DeckLocation)
					this.AddCardsInto((DeckLocation)destination, cardsSecond);
				else
					throw new Exception(String.Format("Destination of {0} ({1}) is not supported", destination, destination.GetType()));
			}

			return cards;
		}
		protected void ShuffleForDrawing()
		{
			this.AddCardsInto(DeckLocation.Deck, this.RetrieveCardsFrom(DeckLocation.Discard), DeckPosition.Bottom);
			ShuffleDrawPile();
		}
		internal void ShuffleDrawPile()
		{
			if (Shuffling != null)
			{
				ShuffleEventArgs sea = new ShuffleEventArgs(this);
				Shuffling(this, sea);
			}
			_DrawPile.Shuffle();
			if (Shuffled != null)
			{
				ShuffleEventArgs sea = new ShuffleEventArgs(this);
				Shuffled(this, sea);
			}
		}
		public void AddCardToDeck(Card card, DeckPosition deckPosition)
		{
			AddCardInto(DeckLocation.Deck, card, deckPosition);
			if (CardsAddedToDeck != null)
			{
				CardsAddedToDeckEventArgs catdea = new CardsAddedToDeckEventArgs(card, deckPosition);
				CardsAddedToDeck(this, catdea);
			}
			
		}
		public void AddCardsToDeck(IEnumerable<Card> cards, DeckPosition deckPosition)
		{
			AddCardsInto(DeckLocation.Deck, cards, deckPosition);
			if (CardsAddedToDeck != null)
			{
				CardsAddedToDeckEventArgs catdea = new CardsAddedToDeckEventArgs(cards, deckPosition);
				CardsAddedToDeck(this, catdea);
			}
		}
		public void AddCardToHand(Card card)
		{
			AddCardInto(DeckLocation.Hand, card, DeckPosition.Bottom);
			if (CardsAddedToHand != null)
			{
				CardsAddedToHandEventArgs cathea = new CardsAddedToHandEventArgs(card);
				CardsAddedToHand(this, cathea);
			}
		}
		public void AddCardsToHand(IEnumerable<Card> cards)
		{
			AddCardsInto(DeckLocation.Hand, cards, DeckPosition.Bottom);
			if (CardsAddedToHand != null)
			{
				CardsAddedToHandEventArgs cathea = new CardsAddedToHandEventArgs(cards);
				CardsAddedToHand(this, cathea);
			}
		}
		public void AddCardsToHand(DeckLocation location)
		{
			this.AddCardsToHand(this.RetrieveCardsFrom(location));
		}
		public void Discard(DeckLocation fromLocation)
		{
			Discard(fromLocation, -1);
		}
		public void Discard(DeckLocation fromLocation, int count)
		{
			Discard(fromLocation, c => true, count, null);
		}
		public void Discard(DeckLocation fromLocation, Card card)
		{
			Discard(fromLocation, c => c == card);
		}
		public void Discard(DeckLocation fromLocation, IEnumerable<Card> cards)
		{
			Discard(fromLocation, cards, null);
		}
		public void Discard(DeckLocation fromLocation, IEnumerable<Card> cards, CardsDiscardAction discardAction)
		{
			Discard(fromLocation, c => cards.Contains(c), discardAction);
		}
		public void Discard(DeckLocation fromLocation, Type cardType, int count)
		{
			Discard(fromLocation, c => c.CardType == cardType, count, null);
		}
		public void Discard(DeckLocation fromLocation, Predicate<Card> match)
		{
			Discard(fromLocation, match, null);
		}
		public void Discard(DeckLocation fromLocation, Predicate<Card> match, CardsDiscardAction discardAction)
		{
			Discard(fromLocation, match, -1, discardAction);
		}
		public void Discard(DeckLocation fromLocation, Predicate<Card> match, int count, CardsDiscardAction discardAction)
		{
			CardCollection matchingCards = this.ResolveDeck(fromLocation)[match];
			if (count >= 0 && count < matchingCards.Count)
			{
				matchingCards.RemoveRange(count, matchingCards.Count - count);
				if (matchingCards.Count != count)
					throw new Exception("Incorrect number of cards found!");
			}
			if (matchingCards.Count == 0)
				return;

			if (CardsDiscarding != null)
			{
				CardsDiscardEventArgs cdea = null;
				List<Object> handledBy = new List<Object>();
				Boolean actionPerformed = false;
				Boolean cancelled = false;
				do
				{
					actionPerformed = false;

					cdea = new CardsDiscardEventArgs(fromLocation, matchingCards);
					cdea.Cancelled = cancelled;
					cdea.HandledBy.AddRange(handledBy);
					CardsDiscarding(this, cdea);

					handledBy = cdea.HandledBy;
					matchingCards = cdea.Cards;
					cancelled |= cdea.Cancelled;

					OptionCollection options = new OptionCollection();
					IEnumerable<Tuple<Type, Type>> cardTypes = cdea.Actions.Keys;
					foreach (Tuple<Type, Type> key in cardTypes)
						options.Add(new Option(cdea.Actions[key].Text, cdea.Actions[key].IsRequired));

					if (options.Count > 0)
					{
						if (discardAction != null && !cdea.HandledBy.Contains(this))
						{
							cdea.AddAction(this.GetType(), discardAction);
							options.Add(new Option(discardAction.Text, true));
						}

						options.Sort();
						Choice choice = new Choice(String.Format("You are discarding {0}", Utilities.StringUtility.Plural("card", matchingCards.Count)), options, this, cdea);
						ChoiceResult result = this.MakeChoice(choice);

						if (result.Options.Count > 0)
						{
							CardsDiscardAction action = cdea.Actions.First(kvp => kvp.Value.Text == result.Options[0]).Value;
							cdea.Data = action.Data;
							action.Method(this, ref cdea);
							actionPerformed = true;
							handledBy = cdea.HandledBy;
							matchingCards = cdea.Cards;
							cancelled |= cdea.Cancelled;
						}
					}

				} while (CardsDiscarding != null && actionPerformed);

				if (cancelled)
					return;
			}

			this.RetrieveCardsFrom(fromLocation, matchingCards);
			if (CardsDiscard != null)
			{
				CardsDiscardEventArgs cdea = null;
				List<Object> handledBy = new List<Object>();

				cdea = new CardsDiscardEventArgs(fromLocation, matchingCards);
				cdea.HandledBy.AddRange(handledBy);
				CardsDiscard(this, cdea);

				handledBy = cdea.HandledBy;
				matchingCards = cdea.Cards;
			}
			this.AddCardsInto(DeckLocation.Discard, matchingCards);

			if (CardsDiscarded != null)
			{
				CardsDiscardEventArgs cdea = null;
				List<Object> handledBy = new List<Object>();
				Boolean actionPerformed = false;
				do
				{
					actionPerformed = false;

					cdea = new CardsDiscardEventArgs(fromLocation, matchingCards);
					cdea.HandledBy.AddRange(handledBy);
					CardsDiscarded(this, cdea);
					handledBy = cdea.HandledBy;

					OptionCollection options = new OptionCollection();
					IEnumerable<Tuple<Type, Type>> cardTypes = cdea.Actions.Keys;
					foreach (Tuple<Type, Type> key in cardTypes)
						options.Add(new Option(cdea.Actions[key].Text, false));

					if (options.Count > 0)
					{
						options.Sort();
						Choice choice = new Choice(String.Format("You discarded {0}", Utilities.StringUtility.Plural("card", matchingCards.Count)), options, this, cdea);
						ChoiceResult result = this.MakeChoice(choice);

						if (result.Options.Count > 0)
						{
							cdea.Actions.First(kvp => kvp.Value.Text == result.Options[0]).Value.Method(this, ref cdea);
							actionPerformed = true;
						}
					}

				} while (CardsDiscarded != null && actionPerformed);
			}
		}
		public void AddCardInto(DeckLocation location, Card card)
		{
			AddCardInto(location, card, DeckPosition.Automatic);
		}
		public void AddCardInto(DeckLocation location, Card card, DeckPosition position)
		{
			AddCardsInto(location, new CardCollection() { card }, position);
		}
		public void AddCardsInto(DeckLocation location, IEnumerable<Card> cards)
		{
			AddCardsInto(location, cards, DeckPosition.Automatic);
		}
		public void AddCardsInto(DeckLocation location, IEnumerable<Card> cards, DeckPosition position)
		{
			if (this.Phase != PhaseEnum.Endgame)
			{
				foreach (Card card in cards)
					card.AddedTo(location, this);
			}

			DeckPosition realPosition = ResolveDeckPosition(location, position);
			switch (location)
			{
				case DeckLocation.Hand:
					foreach (Card card in cards)
						card.ModifiedBy = null;
					_Hand.AddRange(this, cards, realPosition);
					break;

				case DeckLocation.Revealed:
					foreach (Card card in cards)
						card.ModifiedBy = null;
					_Revealed.AddRange(this, cards, realPosition);
					break;

				case DeckLocation.Discard:
					foreach (Card card in cards)
						card.ModifiedBy = null;
					_DiscardPile.AddRange(this, cards, realPosition);
					break;

				case DeckLocation.Deck:
					foreach (Card card in cards)
						card.ModifiedBy = null;
					_DrawPile.AddRange(this, cards, realPosition);
					break;

				case DeckLocation.InPlay:
					_InPlay.AddRange(this, cards, realPosition);
					break;

				case DeckLocation.SetAside:
					_SetAside.AddRange(this, cards, realPosition);
					break;

				case DeckLocation.Private:
					foreach (Card card in cards)
						card.ModifiedBy = null;
					_Private.AddRange(this, cards, realPosition);
					break;
			}
		}
		public void AddCardInto(Type deckType, Card card)
		{
			AddCardInto(deckType, card, DeckPosition.Automatic);
		}
		public void AddCardInto(Type deckType, Card card, DeckPosition position)
		{
			AddCardsInto(deckType, new CardCollection() { card }, position);
		}
		public void AddCardsInto(Type deckType, IEnumerable<Card> cards)
		{
			AddCardsInto(deckType, cards, DeckPosition.Automatic);
		}
		public void AddCardsInto(Type deckType, IEnumerable<Card> cards, DeckPosition position)
		{
			if (cards.Count() == 0)
				return;
			foreach (Card card in cards)
			{
				card.ModifiedBy = null;
				card.AddedTo(deckType, this);
			}
			PlayerMats.Add(this, deckType, cards);
		}

		internal Card RetrieveCardFrom(DeckLocation location, Card card)
		{
			CardCollection cc = RetrieveCardsFrom(location, DeckPosition.Automatic, c => c == card, -1);
			if (cc.Count == 1)
				return cc[0];
			throw new Exception("Found incorrect number of cards.  Should return exactly 1");
		}
		internal CardCollection RetrieveCardsFrom(DeckLocation location)
		{
			return RetrieveCardsFrom(location, DeckPosition.Automatic, c => true, -1);
		}
		internal CardCollection RetrieveCardsFrom(DeckLocation location, CardCollection cards)
		{
			//return new CardCollection(RetrieveCardsFrom(location, DeckPosition.Automatic, c => cards.Any(card => card.CardType == c.CardType), cards.Count).OrderBy(c => cards.IndexOf(c)));
			return new CardCollection(RetrieveCardsFrom(location, DeckPosition.Automatic, c => cards.Contains(c), -1).OrderBy(c => cards.IndexOf(c)));
		}
		internal CardCollection RetrieveCardsFrom(DeckLocation location, Category cardType)
		{
			return RetrieveCardsFrom(location, cardType, -1);
		}
		internal CardCollection RetrieveCardsFrom(DeckLocation location, Category cardType, int count)
		{
			return RetrieveCardsFrom(location, DeckPosition.Automatic, c => (c.Category & cardType) == cardType, count);
		}
		internal CardCollection RetrieveCardsFrom(DeckLocation location, Type type, int count)
		{
			return RetrieveCardsFrom(location, DeckPosition.Automatic, c => c.CardType == type, count);
		}
		internal CardCollection RetrieveCardsFrom(DeckLocation location, Predicate<Card> match)
		{
			return RetrieveCardsFrom(location, DeckPosition.Automatic, match, -1);
		}
		internal CardCollection RetrieveCardsFrom(DeckLocation location, DeckPosition position, Predicate<Card> match, int count)
		{
			CardCollection cards;
			switch (location)
			{
				case DeckLocation.Hand:
					cards = _Hand.Retrieve(this, position, match, count);
					break;

				case DeckLocation.Revealed:
					cards = _Revealed.Retrieve(this, position, match, count);
					break;

				case DeckLocation.Discard:
					cards = _DiscardPile.Retrieve(this, position, match, count);
					break;

				case DeckLocation.Deck:
					cards = _DrawPile.Retrieve(this, position, match, count);
					if (cards.Count < count && _DrawPile.Count == 0 && _DiscardPile.Count > 0)
						this.ShuffleForDrawing();
					cards.AddRange(_DrawPile.Retrieve(this, position, match, count < 0 ? count : count - cards.Count));
					break;

				case DeckLocation.InPlay:
					cards = _InPlay.Retrieve(this, position, match, count);
					break;

				case DeckLocation.SetAside:
					cards = _SetAside.Retrieve(this, position, match, count);
					break;

				case DeckLocation.Private:
					cards = _Private.Retrieve(this, position, match, count);
					break;

				case DeckLocation.InPlayAndSetAside:
					cards = _InPlay.Retrieve(this, position, match, count);
					cards.AddRange(_SetAside.Retrieve(this, position, match, count < 0 ? count : count - cards.Count));
					break;

				default:
					cards = new CardCollection();
					break;
			}
			cards.RemovedFrom(location, this);
			return cards;
		}

		internal CardCollection RetrieveCardsFrom(Type deckType)
		{
			return RetrieveCardsFrom(deckType, c => true, -1);
		}

		internal CardCollection RetrieveCardsFrom(Type deckType, Predicate<Card> match)
		{
			return RetrieveCardsFrom(deckType, match, -1);
		}

		internal CardCollection RetrieveCardsFrom(Type deckType, Predicate<Card> match, int count)
		{
			CardCollection cards = PlayerMats.Retrieve(this, deckType, match, count);
			cards.RemovedFrom(deckType, this);
			return cards;
		}

		private void MoveInPlayToSetAside(Predicate<Card> match)
		{
			CardCollection cardsToMove = this._InPlay.Retrieve(this, match);
			// Can't do this because it will trigger Band of Misfit's action
			// Need to do this to trigger removing of event from cards like Lighthouse
			// Need to resolve this issue somehow.
			//cardsToMove.RemovedFrom(DeckLocation.InPlay, this);
			this.AddCardsInto(DeckLocation.SetAside, cardsToMove);
		}

		private CardCollection MoveToTrashStart(DeckLocation location, CardCollection cards)
		{
			CardCollection cardsMoved = null;
			switch (location)
			{
				case DeckLocation.Hand:
					cardsMoved = _Hand.Retrieve(this, c => cards.Contains(c));
					break;

				case DeckLocation.Revealed:
					cardsMoved = _Revealed.Retrieve(this, c => cards.Contains(c));
					break;

				case DeckLocation.Discard:
					cardsMoved = _DiscardPile.Retrieve(this, c => cards.Contains(c));
					break;

				case DeckLocation.InPlay:
					cardsMoved = _InPlay.Retrieve(this, c => cards.Contains(c));
					break;

				case DeckLocation.SetAside:
					cardsMoved = _SetAside.Retrieve(this, c => cards.Contains(c));
					break;

				case DeckLocation.Private:
					cardsMoved = _Private.Retrieve(this, c => cards.Contains(c));
					break;

				default:
					throw new Exception("Cannot move card to trash from this location");
			}
			_Game.Table.Trash.AddRange(cardsMoved);
			return cardsMoved;
		}

		/// <summary>
		/// Short-hand for discarding all cards in Hand
		/// </summary>
		public void DiscardHand(Boolean visible)
		{
			if (visible)
				this.Discard(DeckLocation.Hand);
			else
				this.AddCardsInto(DeckLocation.Discard, this.RetrieveCardsFrom(DeckLocation.Hand));
		}
		/// <summary>
		/// Short-hand for discarding all Revealed cards
		/// </summary>
		public void DiscardRevealed()
		{
			this.Discard(DeckLocation.Revealed);
		}
		/// <summary>
		/// Short-hand for discarding all Revealed cards matching the Predicate
		/// </summary>
		public void DiscardRevealed(Predicate<Card> match)
		{
			this.Discard(DeckLocation.Revealed, match);
		}
		/// <summary>
		/// Short-hand for revealing Hand
		/// </summary>
		public CardCollection RevealHand()
		{
			CardCollection hand = this.RetrieveCardsFrom(DeckLocation.Hand);
			this.AddCardsInto(DeckLocation.Revealed, hand);
			return hand;
		}
		/// <summary>
		/// Short-hand for returning Revealed to Hand.  This version also doesn't trigger the CardsAddedToHandEventArgs event
		/// </summary>
		public void ReturnHand(CardCollection hand)
		{
			this.AddCardsInto(DeckLocation.Hand, this.RetrieveCardsFrom(DeckLocation.Revealed, hand), DeckPosition.Bottom);
		}

		private CardGainEventArgs CardGainCheckAllowed(Card card, DeckLocation location, DeckPosition position, Boolean isBought)
		{
			Boolean cancelled = false;
			CardGainEventArgs cgea = new CardGainEventArgs(_Game, card, location, position, isBought);

			if (CardGaining != null)
			{
				do
				{
					cgea = new CardGainEventArgs(_Game, card, location, position, isBought);
					cgea.Cancelled = cancelled;
					CardGaining(this, cgea);

					Boolean isAnyRequired = false;
					List<String> options = new List<String>();
					IEnumerable<Type> cardTypes = cgea.Actions.Keys;
					foreach (Type key in cardTypes)
					{
						options.Add(cgea.Actions[key].Text);
						isAnyRequired |= cgea.Actions[key].IsRequired;
					}

					if (options.Count > 0)
					{
						options.Sort();
						Choice choice = new Choice(String.Format("You are gaining {0}", card), null, new CardCollection() { card }, options, this, cgea, false, isAnyRequired ? 1 : 0, 1);
						ChoiceResult result = this.MakeChoice(choice);

						if (result.Options.Count > 0)
							cgea.Actions.First(kvp => kvp.Value.Text == result.Options[0]).Value.Method(this, ref cgea);
					}

					cancelled = cgea.Cancelled;
					location = cgea.Location;
					position = cgea.Position;
				} while (CardGaining != null && cgea.HandledBy.Count > 0);
			}
			return cgea;
		}
		/// <summary>
		/// Tries to gain the specified card from the trash specified
		/// </summary>
		/// <param name="trash">The trash pile to look through</param>
		/// <param name="card">The card to gain from the trash</param>
		/// <returns>True if the card was actually gained, False otherwise</returns>
		public Boolean Gain(Trash trash, Card card)
		{
			return Gain(trash, card, DeckLocation.Discard, DeckPosition.Automatic);
		}
		/// <summary>
		/// Tries to gain the specified card from the trash specified into the location and position of the deck specified
		/// </summary>
		/// <param name="trash">The trash pile to look through</param>
		/// <param name="card">The card to gain from the trash</param>
		/// <param name="location">The deck the card should go into</param>
		/// <param name="position">The position into the deck the card should go</param>
		/// <returns>True if the card was actually gained, False otherwise</returns>
		public Boolean Gain(Trash trash, Card card, DeckLocation location, DeckPosition position)
		{
			if (trash.Contains(card))
			{
				CardGainEventArgs cgea = CardGainCheckAllowed(card, location, position, false);
				if (!cgea.Cancelled)
					return this.Gain(trash.Retrieve(this, card), cgea.Location, cgea.Position, cgea.Bought);
				else
				{
					CardGainFinish(card, cgea.Location, cgea.Position, cgea.Bought, cgea.Cancelled, cgea.IsLostTrackOf);
					return true;
				}
			}
			return false;
		}
		/// <summary>
		/// Tries to gain from the specified supply
		/// </summary>
		/// <param name="supply">The supply pile to gain from</param>
		/// <returns>True if the card was actually gained, False otherwise</returns>
		public Boolean Gain(Supply supply)
		{
			return this.Gain(supply, supply.TopCard == null ? null : supply.TopCard.CardType, DeckLocation.Discard, DeckPosition.Automatic, false);
		}
		/// <summary>
		/// Tries to gain the number of cards specified from the specified supply
		/// </summary>
		/// <param name="supply">The supply pile to gain from</param>
		/// <param name="count">How many cards to gain</param>
		/// <returns>True if the card was actually gained, False otherwise</returns>
		public Boolean Gain(Supply supply, int count)
		{
			Boolean success = true;
			for (int i = 0; i < count; i++)
				success &= this.Gain(supply, supply.TopCard == null ? null : supply.TopCard.CardType, DeckLocation.Discard, DeckPosition.Automatic, false);
			return success;
		}
		/// <summary>
		/// Tries to gain from the specified supply
		/// </summary>
		/// <param name="supply">The supply pile to gain from</param>
		/// <param name="isBought">Indicating whether or not the card was bought</param>
		/// <returns>True if the card was actually gained, False otherwise</returns>
		public Boolean Gain(Supply supply, Boolean isBought)
		{
			return this.Gain(supply, supply.TopCard == null ? null : supply.TopCard.CardType, DeckLocation.Discard, DeckPosition.Automatic, isBought);
		}
		/// <summary>
		/// Tries to gain from the specified supply into the location and position of the deck specified
		/// </summary>
		/// <param name="supply">The supply pile to gain from</param>
		/// <param name="location">The deck the card should go into</param>
		/// <param name="position">The position into the deck the card should go</param>
		/// <returns>True if the card was actually gained, False otherwise</returns>
		public Boolean Gain(Supply supply, DeckLocation location, DeckPosition position)
		{
			return Gain(supply, supply.TopCard == null ? null : supply.TopCard.CardType, location, position, false);
		}
		/// <summary>
		/// Tries to gain the number of cards specified from the specified supply into the location and position of the deck specified
		/// </summary>
		/// <param name="supply">The supply pile to gain from</param>
		/// <param name="location">The deck the card should go into</param>
		/// <param name="position">The position into the deck the card should go</param>
		/// <param name="count">How many cards to gain</param>
		/// <returns>True if the card was actually gained, False otherwise</returns>
		public Boolean Gain(Supply supply, DeckLocation location, DeckPosition position, int count)
		{
			Boolean success = true;
			for (int i = 0; i < count; i++)
				success &= this.Gain(supply, supply.TopCard == null ? null : supply.TopCard.CardType, location, position, false);
			return success;
		}
		/// <summary>
		/// Tries to gain the specified cardType from the specified supply into the location and position of the deck specified
		/// </summary>
		/// <param name="supply">The supply pile to gain from</param>
		/// <param name="cardType">The card type we're trying to gain</param>
		/// <param name="location">The deck the card should go into</param>
		/// <param name="position">The position into the deck the card should go</param>
		/// <returns>True if the card was actually gained, False otherwise</returns>
		public Boolean Gain(Supply supply, Type cardType, DeckLocation location, DeckPosition position)
		{
			return Gain(supply, cardType, location, position, false);
		}
		/// <summary>
		/// Tries to gain the specified cardType from the specified supply into the location and position of the deck specified
		/// </summary>
		/// <param name="supply">The supply pile to gain from</param>
		/// <param name="cardType">The card type we're trying to gain</param>
		/// <param name="location">The deck the card should go into</param>
		/// <param name="position">The position into the deck the card should go</param>
		/// <param name="isBought">Indicating whether or not the card was bought</param>
		/// <returns>True if the card was actually gained, False otherwise</returns>
		public Boolean Gain(Supply supply, Type cardType, DeckLocation location, DeckPosition position, Boolean isBought)
		{
			if (supply.CanGain(cardType))
			{
				Card supplyCard = supply[cardType].First();
				CardGainEventArgs cgea = CardGainCheckAllowed(supplyCard, location, position, isBought);
				if (!cgea.Cancelled)
					return this.Gain(supply.Take(cardType), cgea.Location, cgea.Position, cgea.Bought);
				else
				{
					CardGainFinish(supplyCard, cgea.Location, cgea.Position, cgea.Bought, cgea.Cancelled, cgea.IsLostTrackOf);
					return false;
				}
			}
			return false;
		}
		/// <summary>
		/// Tries to gain the specified card into the location and position of the deck specified
		/// </summary>
		/// <param name="card">The card to gain from the supply</param>
		/// <param name="location">The deck the card should go into</param>
		/// <param name="position">The position into the deck the card should go</param>
		/// <param name="isBought">Indicating whether or not the card was bought</param>
		/// <returns>True if the card was actually gained, False otherwise</returns>
		private Boolean Gain(Card card, DeckLocation location, DeckPosition position, Boolean isBought)
		{
			if (card == null)
				return false;

			Boolean cancelled = false;
			Boolean lostTrackOf = false;
			if (CurrentTurn != null)
				CurrentTurn.Gained(card);

			CardGainInto(card, location, position, isBought, false, false);

			if (CardGained != null)
			{
				// This is a little bit wacky, but we're going to set up an event listener INSIDE this method that listens to both
				// the Discard Pile and the Draw Pile for changes.  We need to do this in order to capture any "Lost Track" updates
				// that might happen from one card covering up another card (e.g. this card being gained) and causing the game state
				// to "Lose Track" of the card being gained in this method.
				_LostTrackStack[card] = false;
				Pile.PileChangedEventHandler pceh = new Pile.PileChangedEventHandler(DiscardPile_PileChanged_CaptureLostTrack);
				this.DiscardPile.PileChanged += pceh;

				List<Object> handledBy = new List<Object>();
				CardGainEventArgs cgea = null;
				Boolean actionPerformed = false;
				do
				{
					actionPerformed = false;

					cgea = new CardGainEventArgs(_Game, card, location, position, isBought);
					cgea.HandledBy.AddRange(handledBy);
					cgea.Cancelled = cancelled;
					cgea.IsLostTrackOf = lostTrackOf;
					CardGained(this, cgea);
					handledBy = cgea.HandledBy;
					cancelled |= cgea.Cancelled;
					lostTrackOf |= cgea.IsLostTrackOf || _LostTrackStack[card];
					location = cgea.Location;
					position = cgea.Position;

					IEnumerator<Player> enumerator = this._Game.GetPlayersStartingWithEnumerator(this);
					while (enumerator.MoveNext())
					{
						Boolean isAnyRequired = false;
						List<String> options = new List<String>();
						IEnumerable<Type> cardTypes = cgea.Actions.Keys;
						foreach (Type key in cardTypes)
						{
							if (enumerator.Current == cgea.Actions[key].Player)
							{
								options.Add(cgea.Actions[key].Text);
								isAnyRequired |= cgea.Actions[key].IsRequired;
							}
						}
						if (options.Count > 0)
						{
							Choice choice = new Choice(String.Format("{0} gained {1}", this == enumerator.Current ? "You" : this.ToString(), card), null, new CardCollection() { card }, options, this, cgea, false, isAnyRequired ? 1 : 0, 1);
							ChoiceResult result = enumerator.Current.MakeChoice(choice);

							if (result.Options.Count > 0)
							{
								options.Sort();
								cgea.Actions.First(kvp => kvp.Value.Text == result.Options[0]).Value.Method(enumerator.Current, ref cgea);
								actionPerformed = true;
							}

							if (enumerator.Current == this && (cgea.Location != location || cgea.Position != position))
								CardGainInto(card, cgea.Location, cgea.Position, cgea.Bought, cgea.Cancelled, cgea.IsLostTrackOf);

							cancelled |= cgea.Cancelled;
							lostTrackOf |= cgea.IsLostTrackOf || _LostTrackStack[card];
							location = cgea.Location;
							position = cgea.Position;
						}
					}
				} while (CardGained != null && actionPerformed);

				if (pceh != null)
					this.DiscardPile.PileChanged -= pceh;
				_LostTrackStack.Remove(card);
			}

			CardGainFinish(card, location, position, isBought, cancelled, lostTrackOf);

			return true;
		}

		void DiscardPile_PileChanged_CaptureLostTrack(object sender, PileChangedEventArgs e)
		{
			if (e.OperationPerformed == PileChangedEventArgs.Operation.Reset)
				return;
			List<Card> cards = new List<Card>(_LostTrackStack.Keys);
			foreach (Card card in cards)
			{
				// Check to see if the card we're tracking is available (we can only look at the top card)
				// If it isn't the top card, then we've lost track of it.
				if (!_LostTrackStack[card] && this.DiscardPile[c => c == card].Count == 0)
					_LostTrackStack[card] = true;
			}
		}

		private void CardGainInto(Card card, DeckLocation location, DeckPosition position, Boolean isBought, Boolean isCancelled, Boolean isLostTrackOf)
		{
			if (this.DiscardPile.Contains(card))
				this.RetrieveCardFrom(DeckLocation.Discard, card);
			card.Gaining(this, ref location, ref position);
			this.AddCardInto(location, card, position);
			card.Gained(this);

			if (CardGainedInto != null)
			{
				CardGainEventArgs cgea = new CardGainEventArgs(_Game, card, location, position, isBought);
				cgea.Cancelled = isCancelled;
				cgea.IsLostTrackOf = isLostTrackOf;
				CardGainedInto(this, cgea);
			}
		}
		private void CardGainFinish(Card card, DeckLocation location, DeckPosition position, Boolean isBought, Boolean isCancelled, Boolean isLostTrackOf)
		{
			if (CardGainFinished != null)
			{
				CardGainEventArgs cgea = new CardGainEventArgs(_Game, card, location, position, isBought);
				cgea.Cancelled = isCancelled;
				cgea.IsLostTrackOf = isLostTrackOf;
				CardGainFinished(this, cgea);
			}
		}

		public Boolean Buy(Supply supply)
		{
			Card supplyCard = supply.TopCard;

			PlayerMode previousMode = this.PlayerMode;
			this.PlayerMode = Players.PlayerMode.Buying;
			Boolean cancelled = false;
			if (CardBuying != null)
			{
				CardBuyEventArgs cbea = new CardBuyEventArgs(_Game, supplyCard);
				CardBuying(this, cbea);
				cancelled = cbea.Cancelled;
			}
			if (!cancelled)
			{
				CurrentTurn.Bought(supplyCard);
				supplyCard.Bought(this);
				supply.Bought(this);

				if (CardBought != null)
				{
					CardBuyEventArgs cbea = null;
					List<Object> handledBy = new List<Object>();
					Boolean actionPerformed = false;
					do
					{
						actionPerformed = false;
						cbea = new CardBuyEventArgs(_Game, supplyCard);
						cbea.HandledBy.AddRange(handledBy);
						CardBought(this, cbea);
						handledBy = cbea.HandledBy;

						Boolean isAnyRequired = false;
						List<String> options = new List<String>();
						IEnumerable<Type> cardTypes = cbea.Actions.Keys;
						foreach (Type key in cardTypes)
						{
							options.Add(cbea.Actions[key].Text);
							isAnyRequired |= cbea.Actions[key].IsRequired;
						}

						if (options.Count > 0)
						{
							options.Sort();
							Choice choice = new Choice(String.Format("You bought {0}", supplyCard), null, new CardCollection() { supplyCard }, options, this, cbea, false, isAnyRequired ? 1 : 0, 1);
							ChoiceResult result = this.MakeChoice(choice);

							if (result.Options.Count > 0)
							{
								cbea.Actions.First(kvp => kvp.Value.Text == result.Options[0]).Value.Method(this, ref cbea);
								actionPerformed = true;
							}
						}

					} while (CardBought != null && actionPerformed);
				}
			}

			if (CardBuyFinished != null)
			{
				CardBuyEventArgs cbea = new CardBuyEventArgs(_Game, supplyCard);
				cbea.Cancelled = cancelled;
				CardBuyFinished(this, cbea);
			}

			if (!cancelled)
			{
				this.Gain(supply, true);

				this.SpendCurrency(new Currency(_Game.ComputeCost(supplyCard)));
				this.Buys--;
			}

			this.PlayerMode = previousMode;
			return cancelled;
		}

		internal void SpendCurrency(Currency currency)
		{
			this.Currency -= currency;
		}

		public void Receive(Player fromPlayer, Card card, DeckLocation location, DeckPosition position)
		{
			if (CurrentTurn != null)
				CurrentTurn.Received(card);
			this.AddCardInto(location, card, position);
			card.ReceivedBy(this);

			if (CardReceived != null)
			{
				CardReceivedEventArgs crea = new CardReceivedEventArgs(fromPlayer, card, location, position);
				CardReceived(this, crea);
			}
		}

		public void Lose(CardCollection cards)
		{
			cards.LostBy(this);
			if (CardsLost != null)
			{
				CardsLostEventArgs clea = new CardsLostEventArgs(cards);
				CardsLost(this, clea);
			}
		}

		public void Lose(Card card)
		{
			Lose(new CardCollection() { card });
		}

		public virtual void Cleanup()
		{
			this.Phase = PhaseEnum.Cleanup;
			PerformCleanup();
			this.Phase = PhaseEnum.Waiting;
		}

		private void PerformCleanup()
		{
			// Sets up card movements to indicate where each card should go.
			CardMovementCollection cardsToMove = new CardMovementCollection(this.SetAside, c => c.CanCleanUp, DeckLocation.SetAside, DeckLocation.Discard);
			cardsToMove.AddRange(this.Hand, DeckLocation.Hand, DeckLocation.Discard);
			cardsToMove.AddRange(this.InPlay, DeckLocation.InPlay, DeckLocation.Discard);

			foreach (Card durationCard in this.InPlay.Where(c => !c.CanCleanUp))
				cardsToMove[durationCard].Destination = DeckLocation.SetAside;
			IEnumerable<CardMovement> inPlayCards = cardsToMove.Where(cm => cm.CurrentLocation == DeckLocation.InPlay);

			ParallelQuery<CardMovement> pqCardsToMove = cardsToMove.AsParallel().Where(cm =>
				cm.CurrentLocation == DeckLocation.InPlay &&
				cm.Destination == DeckLocation.SetAside &&
				cm.Card.ModifiedBy != null &&
				cardsToMove.Contains(cm.Card.ModifiedBy.PhysicalCard) &&
				cardsToMove[cm.Card.ModifiedBy.PhysicalCard].Destination == DeckLocation.Discard);

			pqCardsToMove.ForAll(cm => cardsToMove[cm.Card.ModifiedBy.PhysicalCard].Destination = DeckLocation.SetAside);

			int drawSize = 5;
			if (CleaningUp != null)
			{
				Boolean cancelled = false;
				// Possibly changing events that can happen in the game
				CleaningUpEventArgs cuea = null;

				do
				{
					cuea = new CleaningUpEventArgs(this, 5, ref cardsToMove);
					cuea.Cancelled |= cancelled;
					CleaningUp(this, cuea);

					OptionCollection options = new OptionCollection();
					IEnumerable<Type> cardTypes = cuea.Actions.Keys;
					foreach (Type key in cardTypes)
						options.Add(new Option(cuea.Actions[key].Text, false));
					if (options.Count > 0)
					{
						options.Sort();
						Choice choice = new Choice("Performing Clean-up", options, this, cuea);
						ChoiceResult result = this.MakeChoice(choice);

						if (result.Options.Count > 0)
							cuea.Actions.First(kvp => kvp.Value.Text == result.Options[0]).Value.Method(this, ref cuea);
						else
							break;
					}
					else
						break;

					cancelled |= cuea.Cancelled;
				} while (CleaningUp != null);

				if (cuea != null)
					cancelled |= cuea.Cancelled;

				if (cancelled)
					return;

				if (cuea.NextPlayer != null)
					_CurrentTurn.NextPlayer = cuea.NextPlayer;
				_CurrentTurn.NextGrantedBy = cuea.NextGrantedBy;
				drawSize = cuea.DrawSize;
			}

			// Discard any Revealed cards (should be none?)
			this.DiscardRevealed();

			CardsDiscardAction cdaHand = null;
			if (cardsToMove.Count(c => c.CurrentLocation == DeckLocation.Hand) > 0)
				cdaHand = new CardsDiscardAction(this, null, "Discard hand", player_DiscardHand, true) { Data = cardsToMove };

			// Discard non-Duration (or Duration-modifying) cards in In Play & Set Aside at the same time
			this.Discard(DeckLocation.InPlayAndSetAside, cardsToMove.Where(cm =>
				(cm.CurrentLocation == DeckLocation.InPlay || cm.CurrentLocation == DeckLocation.SetAside) &&
				cm.Destination == DeckLocation.Discard).Select<CardMovement, Card>(cm => cm.Card), cdaHand);

			// Discard Hand
			this.AddCardsInto(DeckLocation.Discard,
				this.RetrieveCardsFrom(DeckLocation.Hand, c =>
					cardsToMove[c].CurrentLocation == DeckLocation.Hand && cardsToMove[c].Destination == DeckLocation.Discard));

			// Move Duration (and Duration-modifying) cards from In Play into Set Aside
			this.MoveInPlayToSetAside(c => cardsToMove.Contains(c) && cardsToMove[c].CurrentLocation == DeckLocation.InPlay && cardsToMove[c].Destination == DeckLocation.SetAside);

			// Move any cards that have had their Destination changed to their appropriate locations
			IEnumerable<Card> replaceCards = cardsToMove.Where(cm => cm.Destination == DeckLocation.Deck).Select(cm => cm.Card);
			if (replaceCards.Count() > 0)
			{
				Choice replaceChoice = new Choice("Choose order of cards to put back on your deck", null, replaceCards, this, true, replaceCards.Count(), replaceCards.Count());
				ChoiceResult replaceResult = this.MakeChoice(replaceChoice);
				this.RetrieveCardsFrom(DeckLocation.InPlay, c => cardsToMove[c].CurrentLocation == DeckLocation.InPlay && replaceResult.Cards.Contains(c));
				this.RetrieveCardsFrom(DeckLocation.SetAside, c => cardsToMove[c].CurrentLocation == DeckLocation.SetAside && replaceResult.Cards.Contains(c));
				this.AddCardsToDeck(replaceResult.Cards, DeckPosition.Top);
			}

#if DEBUG
			if (this.InPlay.Count > 0)
				throw new Exception("Something happened -- there are cards left in the player's In Play area!");
#endif

			if (CurrentTurn != null)
				CurrentTurn.Finished();

			_Actions = _Buys = 0;
			_Currency.Coin.Value = 0;
			_Currency.Potion.Value = 0;
			_ActionsPlayed = 0;

#if DEBUG
			// Check to see that there are no duplicate cards anywhere
			CardCollection allCards = new CardCollection();
			allCards.AddRange(this.Hand);
			allCards.AddRange(this.Revealed);
			allCards.AddRange(this.Private);
			allCards.AddRange(this.InPlay);
			allCards.AddRange(this.SetAside);
			allCards.AddRange(this.DrawPile.LookThrough(c => true));
			allCards.AddRange(this.DiscardPile.LookThrough(c => true));
			foreach (CardMat mat in this.PlayerMats.Values)
				allCards.AddRange(mat);

			ParallelQuery<Card> duplicateCards = allCards.AsParallel().Where(c => allCards.Count(ct => ct == c) > 1);

			//IEnumerable<Card> duplicateCards = allCards.FindAll(c => allCards.Count(ct => ct == c) > 1);
			if (duplicateCards.Count() > 0)
			{
				// Ruh Roh
				throw new Exception("Duplicate cards found!  Something went wrong!");
			}
#endif

			DrawHand(drawSize);
			if (CleanedUp != null)
			{
				CleanedUp(this, new CleanedUpEventArgs(this, drawSize));
			}
			if (TurnEnded != null)
			{
				TurnEnded(this, new TurnEndedEventArgs(this));
			}
		}

		internal void player_DiscardHand(Player player, ref CardsDiscardEventArgs e)
		{
			CardMovementCollection cardsToMove = (CardMovementCollection)e.Data;

			// Discard Hand
			this.AddCardsInto(DeckLocation.Discard,
				this.RetrieveCardsFrom(DeckLocation.Hand, c =>
					cardsToMove[c].CurrentLocation == DeckLocation.Hand && cardsToMove[c].Destination == DeckLocation.Discard));

			e.HandledBy.Add(this);
		}

		public void PlayTreasures(Game game)
		{
			if (Phase == PhaseEnum.Buy || Phase == PhaseEnum.Cleanup || Phase == PhaseEnum.Endgame || PlayerMode == PlayerMode.Waiting || PlayerMode == PlayerMode.Choosing || PlayerMode == PlayerMode.Playing)
				throw new Exception("Can't play treasures right now!");
			if (Phase == PhaseEnum.Action)
				Phase = PhaseEnum.BuyTreasure;

			// Play all Treasure cards that have no special Play method defined
			PlayCards(this.Hand[c => 
				(c.Category & Category.Treasure) == Category.Treasure && 
				(c.Location == Location.General || c.CardType.GetMethod("Play", new Type[] { typeof(Player) }).DeclaringType == typeof(Card))]);
		}

		public void PlayTokens(Game game, Type token, int count)
		{
			if (!this.TokenPiles.ContainsKey(token) || count <= 0)
				return;
			if (Phase == PhaseEnum.Buy || Phase == PhaseEnum.Cleanup || Phase == PhaseEnum.Endgame || PlayerMode == PlayerMode.Waiting || PlayerMode == PlayerMode.Choosing || PlayerMode == PlayerMode.Playing)
				throw new Exception("Can't play tokens right now!");
			if (Phase == PhaseEnum.Action)
				Phase = PhaseEnum.BuyTreasure;

			int finalCount = count > this.TokenPiles[token].Count ? this.TokenPiles[token].Count : count;
			TokenCollection tokens = new TokenCollection(this.TokenPiles[token].Take(finalCount));
			if (TokenPlaying != null)
			{
				TokenPlayingEventArgs tpgea = new TokenPlayingEventArgs(this, tokens);
				TokenPlaying(this, tpgea);
			}
			this.TokenPiles[token].First().Play(this, finalCount);
			this.RemoveTokens(tokens);
			if (TokenPlayed != null)
			{
				TokenPlayedEventArgs tpgea = new TokenPlayedEventArgs(this, tokens);
				TokenPlayed(this, tpgea);
			}

			if (this.Phase == PhaseEnum.BuyTreasure && this.Hand[Category.Treasure].Count == 0 && !this.TokenPiles.IsAnyPlayable)
				this.Phase = PhaseEnum.Buy;
		}

		public void GoToTreasurePhase()
		{
			if (Phase == PhaseEnum.Action || Phase == PhaseEnum.ActionTreasure)
				Phase = PhaseEnum.BuyTreasure;
		}

		public void GoToBuyPhase()
		{
			if (Phase == PhaseEnum.Action || Phase == PhaseEnum.ActionTreasure || Phase == PhaseEnum.BuyTreasure)
				Phase = PhaseEnum.Buy;
		}

		public void PlayNothing() { PlayNothing(String.Empty); }

		public void PlayNothing(String modifier)
		{
			if (CardPlaying != null)
			{
				CardPlayingEventArgs cpgea = new CardPlayingEventArgs(this, (CardCollection)null, modifier);
				CardPlaying(this, cpgea);
			}
			if (CardPlayed != null)
			{
				CardPlayedEventArgs cpdea = new CardPlayedEventArgs(this, (CardCollection)null);
				CardPlayed(this, cpdea);
			}
		}

		public void PlayCard(Card card)
		{
			PlayCards(new CardCollection() { card });
		}

		public void PlayCards(CardCollection cards)
		{
			if (this.Actions == 0 && cards.Any(c => (c.Category & Category.Action) == Category.Action))
				throw new Exception("You cannot play any Action cards right now!");

			PlayCardsInternal(cards);

			// Check Phase after playing -- we may need to switch phases
			if (this.Phase == PhaseEnum.Action && (this.Actions == 0 || this.Hand[Category.Action].Count == 0))
				this.Phase = PhaseEnum.BuyTreasure;
			else if (this.Phase == PhaseEnum.BuyTreasure && this.Hand[Category.Treasure].Count == 0 && !this.TokenPiles.IsAnyPlayable)
				this.Phase = PhaseEnum.Buy;
		}

		internal void PlayCardInternal(Card card)
		{
			PlayCardInternal(card, String.Empty);
		}
		internal void PlayCardInternal(Card card, String modifier)
		{
			PlayCardsInternal(new CardCollection() { card }, modifier);
		}

		internal void PlayCardsInternal(CardCollection cards)
		{
			PlayCardsInternal(cards, String.Empty);
		}
		internal void PlayCardsInternal(CardCollection cards, String modifier)
		{
			// Don't even bother; just return straight away
			if (cards.Count == 0)
				return;

			// So the AI doesn't blow things up, just return immediately if the Phase is Endgame
			if (this.Phase == PhaseEnum.Endgame)
				return;

			foreach (Card card in cards)
			{
				if (Phase == PhaseEnum.Action && (card.Category & Category.Action) != Category.Action)
					this.Phase = PhaseEnum.BuyTreasure;
			}

			if (this.Phase != PhaseEnum.Action && this.Phase != PhaseEnum.Starting && this.PlayerMode != PlayerMode.Playing && cards.Any(c => (c.Category & Category.Action) == Category.Action))
				throw new Exception("You cannot play any Action cards right now!");
			if (this.Phase != PhaseEnum.ActionTreasure && this.Phase != PhaseEnum.BuyTreasure && this.PlayerMode != PlayerMode.Playing && 
				cards.Any(c => (c.Category & Category.Treasure) == Category.Treasure))
				throw new Exception("You cannot play any Treasure cards right now!");

			PlayerMode currentPlayerMode = this.PlayerMode;
			this.PlayerMode = PlayerMode.Playing;

			if (CardPlaying != null)
			{
				CardPlayingEventArgs cpgea = new CardPlayingEventArgs(this, cards, modifier);
				CardPlaying(this, cpgea);
			}

			// Retrieve the actual card instead of the one we're passed.  It might not exist
			// Also, we need to remove them from the Hand, Revealed, or Private (these are the 3 places cards can be played from)
			CardCollection actualCards = this.RetrieveCardsFrom(DeckLocation.Hand, cards);
			actualCards.AddRange(this.RetrieveCardsFrom(DeckLocation.Revealed, cards));
			actualCards.AddRange(this.RetrieveCardsFrom(DeckLocation.Private, cards));

			// Add them to In Play, add them to the Played list for the turn, and Play them (individually)
			foreach (Card card in cards)
			{
				if (actualCards.Contains(card))
				{
					if (CardPuttingIntoPlay != null)
					{
						CardPutIntoPlayEventArgs cpipea = new CardPutIntoPlayEventArgs(this, card);
						CardPuttingIntoPlay(this, cpipea);
					}

					this.AddCardInto(DeckLocation.InPlay, card);

					if (CardPutIntoPlay != null)
					{
						CardPutIntoPlayEventArgs cpipea = new CardPutIntoPlayEventArgs(this, card);
						CardPutIntoPlay(this, cpipea);
					}
				}

				this.CurrentTurn.Played(card);
				card.Play(this);
				card.PlayFinished(this);
			}

			this.InPlay.Refresh(this);

			if (CardPlayed != null)
			{
				CardPlayedEventArgs cpdea = new CardPlayedEventArgs(this, cards);
				CardPlayed(this, cpdea);
			}
			this.PlayerMode = currentPlayerMode;
		}

		public PlayerMode PutCardIntoPlay(Card card, String modifier)
		{
			PlayerMode currentPlayerMode = this.PlayerMode;
			this.PlayerMode = PlayerMode.Playing;

			if (CardPlaying != null)
			{
				CardPlayingEventArgs cpgea = new CardPlayingEventArgs(this, card, modifier);
				CardPlaying(this, cpgea);
			}

			// Retrieve the actual card instead of the one we're passed.  It might not exist
			// Also, we need to remove it from the Hand, Revealed, or Private (these are the 3 places cards can be played from)
			Card actualCard = null;
			if (this.Hand.Contains(card))
				actualCard = this.RetrieveCardFrom(DeckLocation.Hand, card);
			if (actualCard == null && this.Revealed.Contains(card))
				actualCard = this.RetrieveCardFrom(DeckLocation.Revealed, card);
			if (actualCard == null && this.Private.Contains(card))
				actualCard = this.RetrieveCardFrom(DeckLocation.Private, card);

			// Add it to In Play, add it to the Played list for the turn, and Play it
			if (actualCard == card)
			{
				if (CardPuttingIntoPlay != null)
				{
					CardPutIntoPlayEventArgs cpipea = new CardPutIntoPlayEventArgs(this, card);
					CardPuttingIntoPlay(this, cpipea);
				}

				this.AddCardInto(DeckLocation.InPlay, card);

				if (CardPutIntoPlay != null)
				{
					CardPutIntoPlayEventArgs cpipea = new CardPutIntoPlayEventArgs(this, card);
					CardPutIntoPlay(this, cpipea);
				}
			}

			return currentPlayerMode;
		}

		public void PlayCard(Card card, PlayerMode previousPlayerModeToRestore)
		{
			this.CurrentTurn.Played(card);
			card.Play(this);
			card.PlayFinished(this);

			this.InPlay.Refresh(this);

			if (CardPlayed != null)
			{
				CardPlayedEventArgs cpdea = new CardPlayedEventArgs(this, card);
				CardPlayed(this, cpdea);
			}
			this.PlayerMode = previousPlayerModeToRestore;
		}

		public void UndoPlayCard(Card card)
		{
			UndoPlayCards(new CardCollection() { card });
		}

		public void UndoPlayCards(CardCollection cards)
		{
			if (this.Actions == 0 && this.Phase != PhaseEnum.BuyTreasure && cards.Any(c => (c.Category & Category.Action) == Category.Action))
				throw new Exception("You cannot play any Action cards right now!");

			UndoPlayCardsInternal(cards, String.Empty);

			if (cards.Any(c => (c.Category & Category.Action) == Category.Action))
				this.Phase = PhaseEnum.Action;
			else
				this.Phase = PhaseEnum.BuyTreasure;
		}

		internal void UndoPlayCardsInternal(CardCollection cards, String modifier)
		{
			// Don't even bother; just return straight away
			if (cards.Count == 0)
				return;

			// So the AI doesn't blow things up, just return immediately if the Phase is Endgame
			if (this.Phase == PhaseEnum.Endgame)
				return;

			PlayerMode currentPlayerMode = this.PlayerMode;
			this.PlayerMode = PlayerMode.Playing;

			if (CardUndoPlaying != null)
			{
				CardUndoPlayingEventArgs cupea = new CardUndoPlayingEventArgs(this, cards, modifier);
				CardUndoPlaying(this, cupea);
			}

			// Retrieve the actual card instead of the one we're passed.  It might not exist
			CardCollection actualCards = this.RetrieveCardsFrom(DeckLocation.InPlay, cards);

			// Add them to the Hand, remove them from the Played list for the turn, and un-Play them (individually)
			foreach (Card card in cards)
			{
				//if (actualCards.Any(c => c.CardType == card.CardType))
				//{
				//    this.AddCardInto(DeckLocation.Hand, card);
				//    actualCards.Remove(actualCards.First(c => c.CardType == card.CardType));
				//}
				if (actualCards.Contains(card))
					this.AddCardInto(DeckLocation.Hand, card);
				this.CurrentTurn.UndoPlayed(card);
				card.UndoPlay(this);
				card.PlayFinished(this);
			}
			this.InPlay.Refresh(this);

			if (CardUndoPlayed != null)
			{
				CardUndoPlayedEventArgs cupea = new CardUndoPlayedEventArgs(this, cards);
				CardUndoPlayed(this, cupea);
			}
			this.PlayerMode = currentPlayerMode;
		}

		public Boolean AnyActions
		{
			get
			{
				if (_Hand[Category.Action].Count == 0)
					return false;
				return true;
			}
		}

		internal void Reset()
		{
			_Hand.Reset();
			_DrawPile.Reset();
			_DiscardPile.Reset();
			_Revealed.Reset();
			_SetAside.Reset();
			_InPlay.Reset();
			_Private.Reset();
			foreach (Deck deck in this.PlayerMats.Values)
				deck.Reset();
		}

		public override string ToString()
		{
			return _Name;
		}

		internal void End()
		{
			this.Phase = PhaseEnum.Endgame;
			_Hand.BeginChanges();
			lock (_Hand)
			{
				this.AddCardsInto(DeckLocation.Hand, this.RetrieveCardsFrom(DeckLocation.InPlay));
				this.AddCardsInto(DeckLocation.Hand, this.RetrieveCardsFrom(DeckLocation.Revealed));
				this.AddCardsInto(DeckLocation.Hand, this.RetrieveCardsFrom(DeckLocation.SetAside));
				this.AddCardsInto(DeckLocation.Hand, this.RetrieveCardsFrom(DeckLocation.Discard));
				this.AddCardsInto(DeckLocation.Hand, this.RetrieveCardsFrom(DeckLocation.Deck));
				this.AddCardsInto(DeckLocation.Hand, this.RetrieveCardsFrom(DeckLocation.Private));
				foreach (Type deckType in this.PlayerMats.Keys)
					this.AddCardsInto(DeckLocation.Hand, this.RetrieveCardsFrom(deckType));
				_Hand.End(this);
			}
			_Hand.EndChanges();
		}

		internal void Trash(Card card)
		{
			this.Trash(new CardCollection() { card });
		}

		internal void Trash(DeckLocation location, Card card)
		{
			this.Trash(location, new CardCollection() { card });
		}

		internal void Trash(CardCollection cards)
		{
			if (cards.Count == 0)
				return;

			TrashEventArgs tea = null;

			if (Trashing != null)
			{
				do
				{
					tea = new TrashEventArgs(this, cards);
					Trashing(this, tea);

					Boolean isAnyRequired = false;
					List<String> options = new List<String>();
					IEnumerable<Type> cardTypes = tea.Actions.Keys;
					foreach (Type key in cardTypes)
					{
						options.Add(tea.Actions[key].Text);
						isAnyRequired |= tea.Actions[key].IsRequired;
					}

					if (options.Count > 0)
					{
						options.Sort();
						Choice choice = new Choice(String.Format("You are trashing {0} cards", cards.Count), null, cards, options, this, tea, false, isAnyRequired ? 1 : 0, 1);
						ChoiceResult result = this.MakeChoice(choice);

						if (result.Options.Count > 0)
							tea.Actions.First(kvp => kvp.Value.Text == result.Options[0]).Value.Method(this, ref tea);
					}
				} while (Trashing != null && tea.HandledBy.Count > 0);
			}

			_Game.Table.Trash.AddRange(cards);
			if (CurrentTurn != null)
				CurrentTurn.Trashed(cards);

			if (Trashed != null)
			{
				List<Object> handledBy = new List<Object>();
				Boolean actionPerformed = false;
				do
				{
					actionPerformed = false;

					tea = new TrashEventArgs(this, cards);
					tea.HandledBy.AddRange(handledBy);
					Trashed(this, tea);
					handledBy = tea.HandledBy;

					IEnumerator<Player> enumerator = this._Game.GetPlayersStartingWithEnumerator(this);
					while (enumerator.MoveNext())
					{
						Boolean isAnyRequired = false;
						List<String> options = new List<String>();
						IEnumerable<Type> cardTypes = tea.Actions.Keys;
						foreach (Type key in cardTypes)
						{
							if (enumerator.Current == tea.Actions[key].Player)
							{
								options.Add(tea.Actions[key].Text);
								isAnyRequired |= tea.Actions[key].IsRequired;
							}
						}
						if (options.Count > 0)
						{
							options.Sort();
							Choice choice = new Choice(String.Format("{0} trashed {1}", this == enumerator.Current ? "You" : this.ToString(), Utilities.StringUtility.Plural("card", cards.Count)), null, cards, options, this, tea, false, isAnyRequired ? 1 : 0, 1);
							ChoiceResult result = enumerator.Current.MakeChoice(choice);

							if (result.Options.Count > 0)
							{
								tea.Actions.First(kvp => kvp.Value.Text == result.Options[0]).Value.Method(enumerator.Current, ref tea);
								actionPerformed = true;
							}
						}
					}
				} while (Trashed != null && actionPerformed);
			}

			cards.TrashedBy(this);
			Lose(cards);

			if (TrashedFinished != null)
			{
				tea = new TrashEventArgs(this, cards);
				TrashedFinished(this, tea);
			}
		}

		internal void Trash(DeckLocation location, CardCollection cards)
		{
			if (cards.Count == 0)
				return;

			TrashEventArgs tea = null;

			if (Trashing != null)
			{
				do
				{
					tea = new TrashEventArgs(this, cards);
					Trashing(this, tea);

					Boolean isAnyRequired = false;
					List<String> options = new List<String>();
					IEnumerable<Type> cardTypes = tea.Actions.Keys;
					foreach (Type key in cardTypes)
					{
						options.Add(tea.Actions[key].Text);
						isAnyRequired |= tea.Actions[key].IsRequired;
					}

					if (options.Count > 0)
					{
						options.Sort();
						Choice choice = new Choice(String.Format("You are trashing {0} cards", cards.Count), null, cards, options, this, tea, false, isAnyRequired ? 1 : 0, 1);
						ChoiceResult result = this.MakeChoice(choice);

						if (result.Options.Count > 0)
							tea.Actions.First(kvp => kvp.Value.Text == result.Options[0]).Value.Method(this, ref tea);
					}
				} while (Trashing != null && tea.HandledBy.Count > 0);
			}

			this.MoveToTrashStart(location, cards);
			if (CurrentTurn != null)
				CurrentTurn.Trashed(cards);

			if (Trashed != null)
			{
				List<Object> handledBy = new List<Object>();
				Boolean actionPerformed = false;
				do
				{
					actionPerformed = false;

					tea = new TrashEventArgs(this, cards);
					tea.HandledBy.AddRange(handledBy);
					Trashed(this, tea);
					handledBy = tea.HandledBy;

					IEnumerator<Player> enumerator = this._Game.GetPlayersStartingWithEnumerator(this);
					while (enumerator.MoveNext())
					{
						Boolean isAnyRequired = false;
						List<String> options = new List<String>();
						IEnumerable<Type> cardTypes = tea.Actions.Keys;
						foreach (Type key in cardTypes)
						{
							if (enumerator.Current == tea.Actions[key].Player)
							{
								options.Add(tea.Actions[key].Text);
								isAnyRequired |= tea.Actions[key].IsRequired;
							}
						}
						if (options.Count > 0)
						{
							options.Sort();
							Choice choice = new Choice(String.Format("{0} trashed {1}", this == enumerator.Current ? "You" : this.ToString(), Utilities.StringUtility.Plural("card", cards.Count)), null, cards, options, this, tea, false, isAnyRequired ? 1 : 0, 1);
							ChoiceResult result = enumerator.Current.MakeChoice(choice);

							if (result.Options.Count > 0)
							{
								tea.Actions.First(kvp => kvp.Value.Text == result.Options[0]).Value.Method(enumerator.Current, ref tea);
								actionPerformed = true;
							}
						}
					}
				} while (Trashed != null && actionPerformed);
			}

			cards.TrashedBy(this);
			Lose(cards);

			if (TrashedFinished != null)
			{
				tea = new TrashEventArgs(this, cards);
				TrashedFinished(this, tea);
			}

			cards.RemovedFrom(location, this);
		}
		internal void Start(Turn turn)
		{
			if (TurnStarting != null)
			{
				TurnStartingEventArgs tsea = new TurnStartingEventArgs(this);
				tsea.GrantedBy = turn.GrantedBy;
				TurnStarting(this, tsea);
				if (tsea.Cancelled)
					return;
			}
			_CurrentTurn = turn;
			_Actions = _Buys = 1;
			Phase = PhaseEnum.Starting;
			if (BenefitsChanged != null)
			{
				BenefitsChangedEventArgs bcea = new BenefitsChangedEventArgs(this);
				BenefitsChanged(this, bcea);
			}
			if (TurnStarted != null)
			{
				TurnStartedEventArgs tsea = null;

				List<Object> handledBy = new List<Object>();
				Boolean actionPerformed = false;
				do 
				{
					actionPerformed = false;

					tsea = new TurnStartedEventArgs(this);

					tsea.HandledBy.AddRange(handledBy);
					TurnStarted(this, tsea);
					handledBy = tsea.HandledBy;

					IEnumerator<Player> enumerator = this._Game.GetPlayersStartingWithEnumerator(this);
					while (enumerator.MoveNext())
					{
						OptionCollection options = new OptionCollection();
						IEnumerable<String> cardTypes = tsea.Actions.Keys;
						foreach (String key in cardTypes)
						{
							if (enumerator.Current == tsea.Actions[key].Player)
								options.Add(tsea.Actions[key].Text, tsea.Actions[key].IsRequired);
						}
						if (options.Count > 0)
						{
							options.Sort();
							Choice choice = new Choice(String.Format("{0} turn has started", this == enumerator.Current ? "Your" : String.Format("{0}'s", this)), options, this, tsea);
							ChoiceResult result = enumerator.Current.MakeChoice(choice);

							if (result.Options.Count > 0)
							{
								tsea.Actions.First(kvp => kvp.Value.Text == result.Options[0]).Value.Method(enumerator.Current, ref tsea);
								actionPerformed = true;
							}
						}
					}

				} while (TurnStarted != null && actionPerformed);
			}
			_SetAside.Refresh(this);
			Phase = PhaseEnum.Action;
			//TakeTurn(this._Game, this);
		}

		internal void SetCurrentTurn(Turn turn)
		{
			this._CurrentTurn = turn;
		}

		internal void ReceiveBenefit(Card sourceOfBenefit, CardBenefit benefit)
		{
			ReceiveBenefit(sourceOfBenefit, benefit, false);
		}

		internal void ReceiveBenefit(Card sourceOfBenefit, CardBenefit benefit, Boolean isInternal)
		{
			if (benefit.Any && BenefitReceiving != null && 
				((sourceOfBenefit.Category & Category.Action) == Category.Action ||
				((sourceOfBenefit.Category & Category.Treasure) == Category.Treasure && !isInternal)))
			{
				BenefitReceivingEventArgs brea = new BenefitReceivingEventArgs(this, benefit);
				BenefitReceiving(sourceOfBenefit, brea);
			}

			if (benefit.Cards > 0)
				this.Draw(benefit.Cards, DeckLocation.Hand);

			this.Actions += benefit.Actions;
			this.Buys += benefit.Buys;
			this.Currency += benefit.Currency;
			for (int count = 0; count < benefit.VictoryPoints; count++)
				this.TokenPiles.Add(new Cards.Prosperity.VictoryToken(), this);

			if (benefit.Cards < 0)
			{
				Choice choice = new Choice(String.Format("Discard {0}.", Utilities.StringUtility.Plural("card", -benefit.Cards)), sourceOfBenefit, this.Hand, this, false, -benefit.Cards, -benefit.Cards);
				ChoiceResult result = this.MakeChoice(choice);
				this.Discard(DeckLocation.Hand, result.Cards);
			}
		}

		internal void ReceiveBenefit(Token sourceOfBenefit, CardBenefit benefit)
		{
			if (benefit.Any && BenefitReceiving != null)
			{
				BenefitReceivingEventArgs brea = new BenefitReceivingEventArgs(this, benefit);
				BenefitReceiving(sourceOfBenefit, brea);
			}

			if (benefit.Cards > 0)
				this.Draw(benefit.Cards, DeckLocation.Hand);

			this.Actions += benefit.Actions;
			this.Buys += benefit.Buys;
			this.Currency += benefit.Currency;
			for (int count = 0; count < benefit.VictoryPoints; count++)
				this.TokenPiles.Add(new Cards.Prosperity.VictoryToken(), this);
		}

		internal void RemoveBenefit(Card sourceOfBenefit, CardBenefit benefit, Boolean isInternal)
		{
			ReceiveBenefit(sourceOfBenefit, new CardBenefit()
			{
				Actions = -benefit.Actions,
				Buys = -benefit.Buys,
				Cards = -benefit.Cards,
				Currency = -benefit.Currency,
				FlavorText = benefit.FlavorText,
				VictoryPoints = -benefit.VictoryPoints
			}, isInternal);
		}

		
		internal int CountAll()
		{
			return CountAll(this, c => true, true, false);
		}


		internal int CountAll(Player fromPlayer, Predicate<Card> predicate)
		{
			return CountAll(fromPlayer, predicate, true, false);
		}

		internal int CountAll(Player fromPlayer, Predicate<Card> predicate, Boolean onlyObtainable, Boolean onlyCurrentlyDrawable)
		{
			int count = 0;
			count += this.DiscardPile.LookThrough(predicate).Count;
			count += this.DrawPile.LookThrough(predicate).Count;

			if (!onlyCurrentlyDrawable)
			{
				count += this.Hand[predicate].Count;
				count += this.Private[predicate].Count;
				count += this.Revealed[predicate].Count;
				count += this.InPlay[predicate].Count;
				count += this.SetAside[predicate].Count;

				foreach (Type deckType in this.PlayerMats.Keys)
				{
					// Don't count cards on mats that you can't obtain cards from
					if (onlyObtainable && !this.PlayerMats[deckType].IsObtainable)
						continue;
					count += this.PlayerMats[deckType].LookThrough(predicate).Count;
				}
			}
			return count;
		}

		internal int SumAll(Player fromPlayer, Predicate<Card> filterPredicate, Func<Card, int> sumSelector)
		{
			return SumAll(fromPlayer, filterPredicate, sumSelector, true, false);
		}

		internal int SumAll(Player fromPlayer, Predicate<Card> filterPredicate, Func<Card, int> sumSelector, Boolean onlyObtainable, Boolean onlyCurrentlyDrawable)
		{
			int sum = 0;
			sum += this.DiscardPile.LookThrough(filterPredicate).Sum(sumSelector);
			sum += this.DrawPile.LookThrough(filterPredicate).Sum(sumSelector);

			if (!onlyCurrentlyDrawable)
			{
				sum += this.Hand[filterPredicate].Sum(sumSelector);
				sum += this.Private[filterPredicate].Sum(sumSelector);
				sum += this.Revealed[filterPredicate].Sum(sumSelector);
				sum += this.InPlay[filterPredicate].Sum(sumSelector);
				sum += this.SetAside[filterPredicate].Sum(sumSelector);

				foreach (Type deckType in this.PlayerMats.Keys)
				{
					// Don't count cards on mats that you can't obtain cards from
					if (onlyObtainable && !this.PlayerMats[deckType].IsObtainable)
						continue;
					sum += this.PlayerMats[deckType].LookThrough(filterPredicate).Sum(sumSelector);
				}
			}
			return sum;
		}

		internal double SumAll(Player fromPlayer, Predicate<Card> filterPredicate, Func<Card, double> sumSelector)
		{
			return SumAll(fromPlayer, filterPredicate, sumSelector, true, false);
		}

		internal double SumAll(Player fromPlayer, Predicate<Card> filterPredicate, Func<Card, double> sumSelector, Boolean onlyObtainable, Boolean onlyCurrentlyDrawable)
		{
			double sum = 0;
			sum += this.DiscardPile.LookThrough(filterPredicate).Sum(sumSelector);
			sum += this.DrawPile.LookThrough(filterPredicate).Sum(sumSelector);

			if (!onlyCurrentlyDrawable)
			{
				sum += this.Hand[filterPredicate].Sum(sumSelector);
				sum += this.Private[filterPredicate].Sum(sumSelector);
				sum += this.Revealed[filterPredicate].Sum(sumSelector);
				sum += this.InPlay[filterPredicate].Sum(sumSelector);
				sum += this.SetAside[filterPredicate].Sum(sumSelector);

				foreach (Type deckType in this.PlayerMats.Keys)
				{
					// Don't count cards on mats that you can't obtain cards from
					if (onlyObtainable && !this.PlayerMats[deckType].IsObtainable)
						continue;
					sum += this.PlayerMats[deckType].LookThrough(filterPredicate).Sum(sumSelector);
				}
			}
			return sum;
		}

		internal void AddToken(Token token)
		{
			this.TokenPiles.Add(token, this);
		}

		internal void RemoveToken(Token token)
		{
			this.TokenPiles.Remove(token);
		}

		internal void RemoveTokens(IEnumerable<Token> tokens)
		{
			this.TokenPiles.Remove(tokens);
		}

		internal void RemoveTokens(Type tokenType, int count)
		{
			if (count <= 0)
				return;
			int finalCount = count > this.TokenPiles[tokenType].Count ? this.TokenPiles[tokenType].Count : count;
			this.TokenPiles.Remove(this.TokenPiles[tokenType].Take(finalCount));
		}

		internal void SetupDeckAs(Player player)
		{
			CardCollection cards = new CardCollection(this.RetrieveCardsFrom(DeckLocation.Hand).Union(this.RetrieveCardsFrom(DeckLocation.Deck)));
			foreach (Card sourceCard in player.Hand)
			{
				Card myFoundCard = cards.First(c => c.CardType == sourceCard.CardType);
				cards.Remove(myFoundCard);
				this.AddCardInto(DeckLocation.Hand, myFoundCard);
			}
			this.AddCardsInto(DeckLocation.Deck, cards);
		}

		internal virtual XmlNode GenerateXml(XmlDocument doc)
		{
			XmlElement xePlayer = doc.CreateElement("player");

			XmlElement xe = doc.CreateElement("name");
			xe.InnerText = this.Name;
			xePlayer.AppendChild(xe);

			xe = doc.CreateElement("uniqueid");
			xe.InnerText = this.UniqueId.ToString();
			xePlayer.AppendChild(xe);

			xe = doc.CreateElement("playertype");
			xe.InnerText = this.PlayerType.ToString();
			xePlayer.AppendChild(xe);

			xe = doc.CreateElement("type");
			xe.InnerText = this.GetType().ToString();
			xePlayer.AppendChild(xe);

			xe = doc.CreateElement("phase");
			PhaseEnum phase = this.Phase;
			if (phase == PhaseEnum.Action)
				phase = PhaseEnum.Starting;
			xe.InnerText = phase.ToString();
			xePlayer.AppendChild(xe);

			xe = doc.CreateElement("mode");
			xe.InnerText = this.PlayerMode.ToString();
			xePlayer.AppendChild(xe);

			xe = doc.CreateElement("actions");
			xe.InnerText = this.Actions.ToString();
			xePlayer.AppendChild(xe);

			xe = doc.CreateElement("buys");
			xe.InnerText = this.Buys.ToString();
			xePlayer.AppendChild(xe);

			xe = doc.CreateElement("currency");
			xe.InnerText = this.Currency.ToString();
			xePlayer.AppendChild(xe);

			xePlayer.AppendChild(this.Hand.LookThrough(c => true).GenerateXml(doc, "hand"));
			xePlayer.AppendChild(this.DrawPile.LookThrough(c => true).GenerateXml(doc, "deck"));
			xePlayer.AppendChild(this.DiscardPile.LookThrough(c => true).GenerateXml(doc, "discard"));
			xePlayer.AppendChild(this.InPlay.LookThrough(c => true).GenerateXml(doc, "inplay"));
			xePlayer.AppendChild(this.SetAside.LookThrough(c => true).GenerateXml(doc, "setaside"));
			xePlayer.AppendChild(this.Revealed.LookThrough(c => true).GenerateXml(doc, "revealed"));
			xePlayer.AppendChild(this.Private.LookThrough(c => true).GenerateXml(doc, "private"));

			xePlayer.AppendChild(this.PlayerMats.GenerateXml(doc, "cardmats"));
			xePlayer.AppendChild(this.TokenPiles.GenerateXml(doc, "tokenpiles"));

			return xePlayer;
		}

		internal static Player Load(Game game, XmlNode xnPlayer)
		{
			XmlNode xnName = xnPlayer.SelectSingleNode("name");
			XmlNode xnPlayerType = xnPlayer.SelectSingleNode("playertype");
			XmlNode xnType = xnPlayer.SelectSingleNode("type");
			XmlNode xnPhase = xnPlayer.SelectSingleNode("phase");
			XmlNode xnMode = xnPlayer.SelectSingleNode("mode");
			XmlNode xnActions = xnPlayer.SelectSingleNode("actions");
			XmlNode xnBuys = xnPlayer.SelectSingleNode("buys");
			XmlNode xnCurrency = xnPlayer.SelectSingleNode("currency");

			if (xnName == null || xnPlayerType == null || xnType == null)
				return null;

			String name = xnName.InnerText;
			PlayerType playerType = (PlayerType)Enum.Parse(typeof(PlayerType), xnPlayerType.InnerText, true);
			Type type = Type.GetType(xnType.InnerText);
			PhaseEnum phase = (PhaseEnum)Enum.Parse(typeof(PhaseEnum), xnPhase.InnerText, true);
			PlayerMode playerMode = (PlayerMode)Enum.Parse(typeof(PlayerMode), xnMode.InnerText, true);
			int actions = int.Parse(xnActions.InnerText);
			int buys = int.Parse(xnBuys.InnerText);
			Currency currency = new Currency(xnCurrency.InnerText);

			Player player = null;
			switch (playerType)
			{
				case PlayerType.Human:
					player = new Players.Human(game, xnName.InnerText);
					break;

				case PlayerType.Computer:
					player = (Player)type.GetConstructor(new Type[] { typeof(Game), typeof(String) }).Invoke(new Object[] { game, name });
					break;

				default:
					break;
			}

			if (player == null)
				return player;

			player.Load(xnPlayer);
			player.Phase = phase;
			player.PlayerMode = playerMode;
			player.Actions = actions;
			player.Buys = buys;
			player.Currency = currency;

			return player;
		}

		internal virtual void Load(XmlNode xnPlayer)
		{
			XmlNode xnUniqueId = xnPlayer.SelectSingleNode("uniqueid");

			if (xnUniqueId != null)
				this._UniqueId = new Guid(xnUniqueId.InnerText);

			foreach (DeckLocation location in Enum.GetValues(typeof(DeckLocation)))
			{
				switch (location)
				{
					case DeckLocation.InPlayAndSetAside:
						continue;

					default:
						XmlNode xnLocation = xnPlayer.SelectSingleNode(location.ToString().ToLower());
						if (xnLocation == null)
							continue;
						CardCollection cards = CardCollection.Load(xnLocation);
						this.AddCardsInto(location, cards, DeckPosition.Bottom);
						cards.ObtainedBy(this);
						break;
				}
			}

			// Temporary solution for upgrade (changing Tableau -> InPlay and PreviousTableau -> SetAside
			foreach (String locationString in new String[] { "tableau", "previoustableau" })
			{
				DeckLocation location = DeckLocation.Discard;
				switch (locationString)
				{
					case "tableau":
						location = DeckLocation.InPlay;
						break;

					case "previoustableau":
						location = DeckLocation.SetAside;
						break;
				}

				XmlNode xnLocation = xnPlayer.SelectSingleNode(location.ToString().ToLower());
				if (xnLocation == null)
					continue;
				CardCollection cards = CardCollection.Load(xnLocation);
				this.AddCardsInto(location, cards, DeckPosition.Bottom);
				cards.ObtainedBy(this);
			}

			this.PlayerMats.Load(xnPlayer.SelectSingleNode("cardmats"));
			this.TokenPiles.Load(xnPlayer.SelectSingleNode("tokenpiles"));
		}
	}
}
