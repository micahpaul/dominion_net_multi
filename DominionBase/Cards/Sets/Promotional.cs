using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DominionBase.Currencies;
using DominionBase.Piles;
using DominionBase.Players;

namespace DominionBase.Cards.Promotional
{
	public static class TypeClass
	{
		//public static Type BlackMarket = typeof(BlackMarket);
		public static Type Envoy = typeof(Envoy);
		public static Type Governor = typeof(Governor);
		public static Type Prince = typeof(Prince);
		public static Type Stash = typeof(Stash);
		public static Type WalledVillage = typeof(WalledVillage);

		public static Type BlackMarketSupply = typeof(BlackMarketSupply);
		public static Type PrinceSetAside = typeof(PrinceSetAside);
	}

	public class BlackMarket : Card
	{
		public BlackMarket()
			: base("Black Market", Category.Action, Source.Promotional, Location.Kingdom, Group.PlusCoin | Group.PlusBuy | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Currency.Coin.Value = 2;
			this.Text = "Reveal the top 3 cards of the Black Market deck.  You may buy one of them immediately.<nl/>Put the unbought cards on the bottom of the Black Market deck in any order.<br/><i>(Before the game, make a Black Market deck out of one copy of each Kingdom card not in the supply.)</i>";
		}

		public override void Play(Player player)
		{
			base.Play(player);
			player.Draw(5, DeckLocation.Revealed);
		}

		public override void Setup(Game game, Supply supply)
		{
			base.Setup(game, supply);

			Supply blackMarketSupply = new Supply(game, game.Players, TypeClass.BlackMarketSupply, Visibility.All);
			blackMarketSupply.FullSetup();
			game.Table.SpecialPiles.Add(TypeClass.BlackMarketSupply, blackMarketSupply);
		}

		public override void CheckSetup(Preset preset, Table table)
		{
			preset.CardCards[this] = new CardCollection();
			// Grab all of the Black Market Supply cards and stick them into the CardCards for Black Market
			foreach (Type cardType in table.SpecialPiles[TypeClass.BlackMarketSupply].CardTypes)
				preset.CardCards[this].Add(Card.CreateInstance(cardType));
		}

		public override List<Type> GetSerializingTypes()
		{
			return new List<Type>() { typeof(BlackMarket_NumberOfCards), typeof(BlackMarket_UseGameConstraints), 
				typeof(BlackMarket_Constraints) }; //, typeof(BlackMarket_ErrorOnNotEnoughCards) };
		}

		public override CardSettingCollection GenerateSettings()
		{
			CardSettingCollection csc = new CardSettingCollection();
			csc.Add(new BlackMarket_NumberOfCards { Value = 25 });
			csc.Add(new BlackMarket_UseGameConstraints { Value = false });
			csc.Add(new BlackMarket_Constraints { Value = new ConstraintCollection() });
			//csc.Add(new BlackMarket_ErrorOnNotEnoughCards { Value = true });

			return csc;
		}

		public override void FinalizeSettings(CardSettingCollection settings)
		{
			(settings[typeof(BlackMarket_Constraints)].Value as ConstraintCollection).MaxCount = 100;
		}

		[Serializable]
		public class BlackMarket_NumberOfCards : CardSetting
		{
			public override String Name { get { return "NumberOfCards"; } }
			public override String Text { get { return "Number of cards to use"; } }
			public override String Hint { get { return "Number of cards to use in the Black Market supply pile"; } }
			public override Type Type { get { return typeof(int); } }
			public override int DisplayOrder { get { return 0; } }
			public override Object LowerBounds { get { return 1; } }
			public override Boolean UseLowerBounds { get { return true; } }
			public override Boolean IsLowerBoundsInclusive { get { return true; } }
		}

		[Serializable]
		public class BlackMarket_UseGameConstraints : CardSetting
		{
			public override String Name { get { return "UseGameConstraints"; } }
			public override String Text { get { return "Use Game constraints instead of the ones listed below"; } }
			public override String Hint { get { return "Use the defined Game constraints instead of the ones defined here"; } }
			public override Type Type { get { return typeof(Boolean); } }
			public override int DisplayOrder { get { return 2; } }
		}

		[Serializable]
		public class BlackMarket_Constraints : CardSetting
		{
			public override String Name { get { return "Constraints"; } }
			public override String Hint { get { return "Constraints to use for selecting cards to use in the Black Market supply"; } }
			public override Type Type { get { return typeof(ConstraintCollection); } }
			public override int DisplayOrder { get { return 3; } }
		}

		//[Serializable]
		//public class BlackMarket_ErrorOnNotEnoughCards : CardSetting
		//{
		//    public override String Name { get { return "ErrorOnNotEnoughCards"; } }
		//    public override String Text { get { return "Error when not enough matching, allowed cards are found"; } }
		//    public override String Hint { get { return "Error when the game can't find enough cards to use for the Black Market supply pile"; } }
		//    public override Type Type { get { return typeof(Boolean); } }
		//    public override int DisplayOrder { get { return 1; } }
		//}
	}
	public class BlackMarketSupply : Card
	{
		public BlackMarketSupply()
			: base("Black Market Supply", Category.Unknown, Source.Promotional, Location.Invisible, Group.None)
		{
		}

		public override void Setup(Game game, Supply supply)
		{
			base.Setup(game, supply);

			IList<Card> availableCards = null; 
			try
			{
				if (game.Settings.Preset != null)
				{
					availableCards = game.Settings.Preset.CardCards[game.Settings.Preset.Cards.First(c => c.CardType == typeof(BlackMarket))];
					// Shuffle the preset cards -- these should definitely not be set up in a known order
					Utilities.Shuffler.Shuffle(availableCards);
				}
				else
				{
					int cardsToUse = 25;
					//Boolean errorOnNotEnoughCards = true;
					Boolean shouldUseGameConstraints = true;
					ConstraintCollection bmConstraints = new ConstraintCollection();
					if (game.Settings.CardSettings.ContainsKey("Black Market"))
					{
						CardsSettings bmSettings = game.Settings.CardSettings["Black Market"];
						cardsToUse = (int)bmSettings.CardSettingCollection[typeof(BlackMarket.BlackMarket_NumberOfCards)].Value;
						//errorOnNotEnoughCards = (Boolean)bmSettings.CardSettingCollection[typeof(BlackMarket.BlackMarket_ErrorOnNotEnoughCards)].Value;
						shouldUseGameConstraints = (Boolean)bmSettings.CardSettingCollection[typeof(BlackMarket.BlackMarket_UseGameConstraints)].Value;
						bmConstraints = (ConstraintCollection)bmSettings.CardSettingCollection[typeof(BlackMarket.BlackMarket_Constraints)].Value;
					}

					// need to set up a supply pile for Black Market; randomly pick an unused supply card and add it to the pile until we have the requisite number of cards
					availableCards = game.CardsAvailable;
					if (shouldUseGameConstraints)
					{
						// Skip all "Must Use" constraints
						ConstraintCollection constraints = new ConstraintCollection(game.Settings.Constraints.Where(c => c.ConstraintType != ConstraintType.CardMustUse));
						availableCards = constraints.SelectCards(availableCards, cardsToUse);
					}
					else
						availableCards = bmConstraints.SelectCards(availableCards, cardsToUse);
				}
			}
			catch (DominionBase.Cards.ConstraintException ce)
			{
				throw new BlackMarketConstraintException(String.Format("Problem setting up Black Market constraints: {0}", ce.Message));
			}

			foreach (Card cardToUse in availableCards)
			{
				game.CardsAvailable.Remove(cardToUse);
				supply.AddTo(cardToUse);
			}
		}
	}

	public class BlackMarketConstraintException : ConstraintException
	{
		public BlackMarketConstraintException() { }
		public BlackMarketConstraintException(string message) : base(message) { }
		public BlackMarketConstraintException(string message, Exception innerException) : base(message, innerException) { }
		internal BlackMarketConstraintException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}

	public class Envoy : Card
	{
		public Envoy()
			: base("Envoy", Category.Action, Source.Promotional, Location.Kingdom, Group.PlusCard | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.Text = "Reveal the top 5 cards from your deck.  The player to your left chooses one for you to discard.  Draw the rest.";
		}

		public override void Play(Player player)
		{
			base.Play(player);
			player.Draw(5, DeckLocation.Revealed);

			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			enumerator.MoveNext(); // Gets us... which we don't care about here.
			enumerator.MoveNext(); // Get the player to our left

			Player leftPlayer = enumerator.Current;
			Choice choice = new Choice(String.Format("Choose a card of {0}'s to discard", player), this, player.Revealed, player);
			ChoiceResult result = leftPlayer.MakeChoice(choice);
			// Discard the chosen card
			if (result.Cards.Count > 0)
				player.Discard(DeckLocation.Revealed, result.Cards[0]);
			player.AddCardsToHand(DeckLocation.Revealed);
		}
	}
	public class Governor : Card
	{
		public Governor()
			: base("Governor", Category.Action, Source.Promotional, Location.Kingdom, Group.PlusAction | Group.PlusCard | Group.AffectOthers | Group.DeckReduction | Group.Gain | Group.RemoveCurses | Group.Trash | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Actions = 1;
			this.Text = "Choose one; you get the version in parenthesis:  Each player gets +1 (+3) Cards; or each player gains a Silver (Gold); or each player may trash a card from his hand and gain a card costing exactly <coin>1</coin> (<coin>2</coin>) more.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choice = new Choice("Choose 1. You get the version in parenthesis; everyone else gets the other:", this, new CardCollection() { this }, new List<string>() { "+1 (+3) Cards", "Gain a Silver (Gold)", "You may trash a card from your hand and gain a card costing exactly <coin>1</coin> (<coin>2</coin>) more" }, player);
			ChoiceResult result = player.MakeChoice(choice);
			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			while (enumerator.MoveNext())
			{
				Player actee = enumerator.Current;
				if (result.Options.Contains("+1 (+3) Cards"))
				{
					// 3 or 1 cards, depending on who it is
					actee.ReceiveBenefit(this, new CardBenefit() { Cards = (actee == player ? 3 : 1) });
				}
				else if (result.Options.Contains("Gain a Silver (Gold)"))
				{
					if (actee == player)
						actee.Gain(player._Game.Table.Gold);
					else
						actee.Gain(player._Game.Table.Silver);
				}
				else
				{
					Choice choiceTrash = new Choice("You may choose a card to trash", this, actee.Hand, actee, false, 0, 1);
					ChoiceResult resultTrash = actee.MakeChoice(choiceTrash);
					actee.Trash(actee.RetrieveCardsFrom(DeckLocation.Hand, resultTrash.Cards));

					if (resultTrash.Cards.Count > 0)
					{
						Cost trashedCardCost = actee._Game.ComputeCost(resultTrash.Cards[0]);
						SupplyCollection gainableSupplies = actee._Game.Table.Supplies.FindAll(supply => supply.CanGain() && supply.CurrentCost == (trashedCardCost + new Coin(actee == player ? 2 : 1)));
						Choice choiceGain = new Choice("Gain a card", this, gainableSupplies, actee, false);
						ChoiceResult resultGain = actee.MakeChoice(choiceGain);
						if (resultGain.Supply != null)
							actee.Gain(resultGain.Supply);
					}
				}
			}
		}
	}
	public class Prince : Card
	{
		private Card _SetAsideCard = null;
		private Card _SetAsideCardInPlay = null;
		private Player.TurnStartedEventHandler _TurnStartedEventHandler = null;
		private Player _TurnStartedPlayer = null;
		private Player.CardsDiscardingEventHandler _CardsDiscardingEventHandler = null;

		public Prince()
			: base("Prince", Category.Action, Source.Promotional, Location.Kingdom, Group.Terminal | Group.DeckReduction)
		{
			this.BaseCost = new Cost(8);
			this.Text = "You may set this aside. If you do, set aside an Action card from your hand costing up to <coin>4</coin>. At the start of each of your turns, play that Action, setting it aside again when you discard it from play. (Stop playing it if you fail to set it aside on a turn you play it).";
		}

		internal override void ReceivedBy(Player player)
		{
			base.ReceivedBy(player);
			if (!player.PlayerMats.ContainsKey(TypeClass.PrinceSetAside))
				player.PlayerMats[TypeClass.PrinceSetAside] = new PrinceSetAside();
		}

		internal override void TearDown()
		{
			base.TearDown();

			_SetAsideCard = null;
			_SetAsideCardInPlay = null;

			if (_TurnStartedEventHandler != null && _TurnStartedPlayer != null)
				_TurnStartedPlayer.TurnStarted -= _TurnStartedEventHandler;
			_TurnStartedPlayer = null;
			_TurnStartedEventHandler = null;
		}

		public override bool IsStackable 
		{ 
			get 
			{
				if (this._TurnStartedPlayer == null)
					return true;
				if (this._TurnStartedPlayer.Phase == PhaseEnum.Endgame)
					return true;
				return false; 
			} 
		}
		public override CardCollection CardStack()
		{
			CardCollection cc = new CardCollection();
			if (_SetAsideCard != null)
				cc.Add(_SetAsideCard);
			cc.Add(this);
			return cc;
		}

		public Card SetAsideCard { get { return this._SetAsideCard != null ? this._SetAsideCard : this._SetAsideCardInPlay; } }

		public override void Play(Player player)
		{
			base.Play(player);
			if (player.InPlay.Contains(this.PhysicalCard))
			{
				Choice choice = Choice.CreateYesNoChoice("Do you want to set this card aside?", this, player);
				ChoiceResult result = player.MakeChoice(choice);
				if (result.Options[0] == "Yes")
				{
					player.AddCardInto(TypeClass.PrinceSetAside, player.RetrieveCardFrom(DeckLocation.InPlay, this.PhysicalCard));

					if (_TurnStartedEventHandler != null)
						player.TurnStarted -= _TurnStartedEventHandler;
					_TurnStartedPlayer = player;
					_TurnStartedEventHandler = new Player.TurnStartedEventHandler(player_TurnStarted);
					_TurnStartedPlayer.TurnStarted += _TurnStartedEventHandler;

					Choice setAsideChoice = new Choice("Choose a card to set aside", this, player.Hand[c => (c.Category & Cards.Category.Action) == Cards.Category.Action && player._Game.ComputeCost(c) <= new Coin(4)], player, false, 1, 1);
					ChoiceResult setAsideResult = player.MakeChoice(setAsideChoice);
					if (setAsideResult.Cards.Count > 0)
					{
						_SetAsideCard = player.RetrieveCardFrom(DeckLocation.Hand, setAsideResult.Cards[0]);
						player.PlayerMats[TypeClass.PrinceSetAside].Refresh(player);
						player._Game.SendMessage(player, this, this._SetAsideCard);
					}

				}
			}
		}

		internal override void End(Player player, Deck deck)
		{
			// Add back any set aside cards that are still on this
			if (_SetAsideCard != null)
				deck.AddRange(player, new CardCollection() { _SetAsideCard });
			if (_SetAsideCardInPlay != null)
				deck.AddRange(player, new CardCollection() { _SetAsideCardInPlay });
			_SetAsideCard = null;
			_SetAsideCardInPlay = null;

			if (_TurnStartedEventHandler != null && _TurnStartedPlayer != null)
				_TurnStartedPlayer.TurnStarted -= _TurnStartedEventHandler;
			_TurnStartedPlayer = null;
			_TurnStartedEventHandler = null;
		}

		void player_TurnStarted(object sender, TurnStartedEventArgs e)
		{
			if (e.HandledBy.Contains(this))
				return;

			_SetAsideCardInPlay = null;
			if (this._SetAsideCard != null)
			{
				String key = String.Format("{0}:{1}", this, this._SetAsideCard);
				if (e.Actions.ContainsKey(key))
					return;

				e.Actions[key] = new TurnStartedAction(e.Player, this, String.Format("Play {0} from {1}", this._SetAsideCard, this.PhysicalCard), player_Action, true);
			}
		}

		internal void player_Action(Player player, ref TurnStartedEventArgs e)
		{
			e.Player.Actions++;
			e.Player.AddCardInto(DeckLocation.InPlay, this._SetAsideCard);
			this._SetAsideCardInPlay = this._SetAsideCard;
			this._SetAsideCard = null;

			e.Player.PlayerMats[TypeClass.PrinceSetAside].Refresh(e.Player);

			e.Player.PlayCardInternal(this._SetAsideCardInPlay.LogicalCard, String.Format(" from {0}", this.Name));

			_CardsDiscardingEventHandler = new Player.CardsDiscardingEventHandler(player_CardsDiscarding);
			e.Player.CardsDiscarding += _CardsDiscardingEventHandler;

			e.HandledBy.Add(this);
		}

		void player_CardsDiscarding(object sender, CardsDiscardEventArgs e)
		{
			if (e.FromLocation != DeckLocation.InPlay && e.FromLocation != DeckLocation.SetAside && e.FromLocation != DeckLocation.InPlayAndSetAside)
				return;

			if (e.HandledBy.Contains(this))
				return;

			if (e.Cards.Contains(this._SetAsideCardInPlay))
			{
				e.AddAction(TypeClass.Prince, this._SetAsideCardInPlay.CardType, new CardsDiscardAction(sender as Player, this, String.Format("Set aside {0}", this._SetAsideCardInPlay), player_DiscardAction, true) { Data = this._SetAsideCardInPlay });
			}
			else
			{
			}
		}

		internal void player_DiscardAction(Player player, ref CardsDiscardEventArgs e)
		{
			if (_CardsDiscardingEventHandler != null)
				this._TurnStartedPlayer.CardsDiscarding -= _CardsDiscardingEventHandler;

			Card cardToSetAside = e.Data as Card;
			e.Cards.Remove(cardToSetAside);
			if (player.InPlay.Contains(this._SetAsideCardInPlay))
				player.RetrieveCardFrom(DeckLocation.InPlay, cardToSetAside);
			else
				player.RetrieveCardFrom(DeckLocation.SetAside, cardToSetAside);
			this._SetAsideCard = cardToSetAside;
			player._Game.SendMessage(player, this, this._SetAsideCard);
			this._SetAsideCardInPlay = null;

			player.PlayerMats[TypeClass.PrinceSetAside].Refresh(player);

			e.HandledBy.Add(this);
		}
	}
	public class PrinceSetAside : CardMat
	{
		public PrinceSetAside()
			: base(Visibility.All, VisibilityTo.All, new DominionBase.Cards.Sorting.ByTypeName(DominionBase.Cards.Sorting.SortDirection.Descending), false)
		{
			this.IsObtainable = false;
		}
	}
	public class Stash : Card
	{
		private Player.ShuffledEventHandler _ShuffledEventHandler = null;

		public Stash()
			: base("Stash", Category.Treasure, Source.Promotional, Location.Kingdom, Group.PlusCoin, CardBack.Red)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Currency.Coin.Value = 2;
			this.Text = "<br/>When you shuffle, you may put this anywhere in your deck.";

			this.OwnerChanged += new OwnerChangedEventHandler(Stash_OwnerChanged);
		}

		protected override Boolean AllowUndo { get { return true; } }

		internal override void TearDown()
		{
			Stash_OwnerChanged(this, new OwnerChangedEventArgs(this.Owner, null));

			base.TearDown();

			this.OwnerChanged -= new OwnerChangedEventHandler(Stash_OwnerChanged);
		}

		void Stash_OwnerChanged(object sender, OwnerChangedEventArgs e)
		{
			if (_ShuffledEventHandler != null && e.OldOwner != null)
			{
				e.OldOwner.Shuffled -= _ShuffledEventHandler;
				_ShuffledEventHandler = null;
			}

			if (e.NewOwner != null)
			{
				_ShuffledEventHandler = new Player.ShuffledEventHandler(player_Shuffled);
				e.NewOwner.Shuffled += _ShuffledEventHandler;
			}
		}

		public override void AddedTo(DeckLocation location, Player player)
		{
			base.AddedTo(location, player);

			switch (location)
			{
				case DeckLocation.Deck:
					if (_ShuffledEventHandler != null)
						player.Shuffled -= _ShuffledEventHandler;

					_ShuffledEventHandler = new Player.ShuffledEventHandler(player_Shuffled);
					player.Shuffled += _ShuffledEventHandler;
					break;
			}
		}

		void player_Shuffled(object sender, ShuffleEventArgs e)
		{
			// Only do this if we're the first one
			if (e.HandledBy.Contains(this.CardType))
				return;

			CardCollection deck = e.Player.DrawPile.Retrieve(e.Player, c => true);

			Choice choiceShuffle = new Choice("Cards have been shuffled.  You may rearrange them", this, deck, Visibility.None, e.Player, true, deck.Count, deck.Count);
			ChoiceResult resultShuffle = e.Player.MakeChoice(choiceShuffle);

			e.Player.DrawPile.AddRange(e.Player, resultShuffle.Cards);

			e.HandledBy.Add(this.CardType);
		}

		public override void RemovedFrom(DeckLocation location, Player player)
		{
			base.RemovedFrom(location, player);
			if (_ShuffledEventHandler != null)
				player.Shuffled -= _ShuffledEventHandler;
			_ShuffledEventHandler = null;
		}
	}
	public class WalledVillage : Card
	{
		private Player.PhaseChangingEventHandler _PhaseChangingEventHandler = null;
		private Player.CleaningUpEventHandler _CleaningUpEventHandler = null;
		private Boolean _CanPutOnDeck = false;

		public WalledVillage()
			: base("Walled Village", Category.Action, Source.Promotional, Location.Kingdom, Group.CardOrdering | Group.PlusCard | Group.PlusAction | Group.PlusMultipleActions)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 2;
			this.Text = "<br/>At the start of Clean-up, if you have this and no more than one other Action card in play, you may put this on top of your deck.";
		}

		public override void AddedTo(DeckLocation location, Player player)
		{
			base.AddedTo(location, player);

			switch (location)
			{
				case DeckLocation.InPlay:
					if (_PhaseChangingEventHandler != null)
						player.PhaseChanging -= _PhaseChangingEventHandler;

					_PhaseChangingEventHandler = new Player.PhaseChangingEventHandler(player_PhaseChanging);
					player.PhaseChanging += _PhaseChangingEventHandler;

					if (_CleaningUpEventHandler != null)
						player.CleaningUp -= _CleaningUpEventHandler;

					_CleaningUpEventHandler = new Player.CleaningUpEventHandler(player_CleaningUp);
					player.CleaningUp += _CleaningUpEventHandler;

					_CanPutOnDeck = false;
					break;
			}
		}

		void player_PhaseChanging(object sender, PhaseChangingEventArgs e)
		{
			switch (e.NewPhase)
			{
				case PhaseEnum.Cleanup:
					if (e.CurrentPlayer.InPlay.Count(c => (c.Category & Cards.Category.Action) == Cards.Category.Action) +
						e.CurrentPlayer.SetAside.Count(c => (c.Category & Cards.Category.Action) == Cards.Category.Action) <= 2)

						_CanPutOnDeck = true;
					else
						_CanPutOnDeck = false;
					break;
			}
		}

		void player_CleaningUp(object sender, CleaningUpEventArgs e)
		{
			if (!e.CurrentPlayer.InPlay.Contains(this.PhysicalCard) || e.Actions.ContainsKey(TypeClass.WalledVillage))
				return;

			if (_CanPutOnDeck)
				e.Actions[TypeClass.WalledVillage] = new CleaningUpAction(this, String.Format("Put {0} on top your deck", this.PhysicalCard), player_Action);
		}

		internal void player_Action(Player player, ref CleaningUpEventArgs e)
		{
			//e.CardsMovements[this].Destination = DeckLocation.Deck;
			e.CardsMovements.Remove(e.CardsMovements.Find(cm => cm.Card == this.PhysicalCard));
			//e.CardsMovements.MoveToEnd(this);
			e.CurrentPlayer.AddCardToDeck(e.CurrentPlayer.RetrieveCardFrom(DeckLocation.InPlay, this.PhysicalCard), DeckPosition.Top);
		}


		public override void RemovedFrom(DeckLocation location, Player player)
		{
			base.RemovedFrom(location, player);
			if (_PhaseChangingEventHandler != null)
				player.PhaseChanging -= _PhaseChangingEventHandler;
			_PhaseChangingEventHandler = null;
			if (_CleaningUpEventHandler != null)
				player.CleaningUp -= _CleaningUpEventHandler;
			_CleaningUpEventHandler = null;
			_CanPutOnDeck = false;
		}
	}
}
