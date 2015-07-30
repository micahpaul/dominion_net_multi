using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using DominionBase.Currencies;
using DominionBase.Piles;
using DominionBase.Players;

namespace DominionBase.Cards.Cornucopia
{
	public static class TypeClass
	{
		public static Type BagOfGold = typeof(BagOfGold);
		public static Type Diadem = typeof(Diadem);
		public static Type Fairgrounds = typeof(Fairgrounds);
		public static Type FarmingVillage = typeof(FarmingVillage);
		public static Type Followers = typeof(Followers);
		public static Type FortuneTeller = typeof(FortuneTeller);
		public static Type Hamlet = typeof(Hamlet);
		public static Type Harvest = typeof(Harvest);
		public static Type HornOfPlenty = typeof(HornOfPlenty);
		public static Type HorseTraders = typeof(HorseTraders);
		public static Type HuntingParty = typeof(HuntingParty);
		public static Type Jester = typeof(Jester);
		public static Type Menagerie = typeof(Menagerie);
		public static Type Princess = typeof(Princess);
		public static Type PrizeSupply = typeof(PrizeSupply);
		public static Type Remake = typeof(Remake);
		public static Type Tournament = typeof(Tournament);
		public static Type TrustySteed = typeof(TrustySteed);
		public static Type YoungWitch = typeof(YoungWitch);

		public static Type BaneToken = typeof(BaneToken);
	}

	public class BagOfGold : Card
	{
		public BagOfGold()
			: base("Bag of Gold", Category.Action | Category.Prize, Source.Cornucopia, Location.Special, Group.CardOrdering | Group.PlusAction | Group.Gain)
		{
			this.BaseCost = new Cost(0, true, false);
			this.Benefit.Actions = 1;
			this.Text = "Gain a Gold, putting it on top of your deck.<nl/><nl/><i>(This is not in the Supply.)</i>";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			player.Gain(player._Game.Table.Gold, DeckLocation.Deck, DeckPosition.Top);
		}
	}
	public class BaneToken : Token
	{
		public BaneToken()
			: base("B", "Bane")
		{
		}

		public override String Title { get { return "This supply pile is a Bane pile and cards from here may be revealed to block gaining a Curse card from the Young Witch"; } }
		public override Boolean ActDefined { get { return true; } }

		internal override void Act(Card card, TokenActionEventArgs e)
		{
			base.Act(card, e);

			Player player = e.Actee as Player;

			// Already been cancelled -- don't need to process this one
			if (e.Cancelled || !e.Actee.Hand.Contains(card) || e.HandledBy.Contains(card.CardType))
				return;

			// Bane token/card only protects against other attackers
			if (player == e.Actor)
				return;
			Choice choice = Choice.CreateYesNoChoice(String.Format("Reveal Bane card {0} to block gaining a Curse card?", card.Name), card, e.ActingCard, player, e);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Options[0] == "Yes")
			{
				player.AddCardInto(DeckLocation.Revealed, e.Actee.RetrieveCardFrom(DeckLocation.Hand, card));
				e.Cancelled = true;
				player.AddCardInto(DeckLocation.Hand, e.Actee.RetrieveCardFrom(DeckLocation.Revealed, card));
			}
			e.HandledBy.Add(card.CardType);
		}
	}
	public class Diadem : Card
	{
		public Diadem()
			: base("Diadem", Category.Treasure | Category.Prize, Source.Cornucopia, Location.Special, Group.PlusCoin | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(0, true, false);
			this.Benefit.Currency.Coin.Value = 2;
			this.Text = "When you play this, +<coin>1</coin> per unused Action you have (Action, not Action card).<nl/><nl/><i>(This is not in the Supply.)</i>";
		}

		protected override Boolean AllowUndo { get { return true; } }

		public override void Play(Player player)
		{
			base.Play(player);

			CardBenefit benefit = new CardBenefit();
			benefit.Currency += new Coin(player.Actions);
			player.ReceiveBenefit(this, benefit);
		}
	}
	public class Fairgrounds : Card
	{
		public Fairgrounds()
			: base("Fairgrounds", Category.Victory, Source.Cornucopia, Location.Kingdom, Group.VariableVPs)
		{
			this.BaseCost = new Cost(6);
			this.Text = "Worth <vp>2</vp> for every 5 differently named cards in your deck (rounded down).";
		}

		public override int GetVictoryPoints(IEnumerable<Card> cards)
		{
			return base.GetVictoryPoints(cards) + 2 * (cards.GroupBy(card => card.CardType).Count() / 5);
		}
	}
	public class FarmingVillage : Card
	{
		public FarmingVillage()
			: base("Farming Village", Category.Action, Source.Cornucopia, Location.Kingdom, Group.PlusAction | Group.PlusMultipleActions | Group.Discard)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Actions = 2;
			this.Text = "Reveal cards from the top of your deck until you reveal an Action or Treasure card.  Put that card into your hand and discard the other cards.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			player.BeginDrawing();
			while (player.CanDraw)
			{
				player.Draw(DeckLocation.Revealed);
				Card lastRevealed = player.Revealed.Last();
				if ((lastRevealed.Category & Cards.Category.Action) == Cards.Category.Action ||
					(lastRevealed.Category & Cards.Category.Treasure) == Cards.Category.Treasure)
					break;
			}
			player.EndDrawing();

			if (player.Revealed.Count > 0)
				player.AddCardToHand(player.RetrieveCardFrom(DeckLocation.Revealed, player.Revealed.Last()));

			player.DiscardRevealed();
		}
	}
	public class Followers : Card
	{
		public Followers()
			: base("Followers", Category.Action | Category.Attack | Category.Prize, Source.Cornucopia, Location.Special, Group.PlusCurses | Group.PlusCard | Group.Gain | Group.Discard | Group.Terminal)
		{
			this.BaseCost = new Cost(0, true, false);
			this.Benefit.Cards = 2;
			this.Text = "Gain an Estate. Each other player gains a Curse and discards down to 3 cards in hand.<nl/><nl/><i>(This is not in the Supply.)</i>";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			player.Gain(player._Game.Table.Estate);

			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			enumerator.MoveNext();
			while (enumerator.MoveNext())
			{
				Player attackee = enumerator.Current;
				// Skip if the attack is blocked (Moat, Lighthouse, etc.)
				if (this.IsAttackBlocked[attackee])
					continue;

				attackee.Gain(player._Game.Table.Curse);

				Choice choice = new Choice("Choose cards to discard.  You must discard down to 3 cards in hand", this, attackee.Hand, attackee, false, attackee.Hand.Count - 3, attackee.Hand.Count - 3);
				ChoiceResult result = attackee.MakeChoice(choice);
				attackee.Discard(DeckLocation.Hand, result.Cards);
			}
		}
	}
	public class FortuneTeller : Card
	{
		public FortuneTeller()
			: base("Fortune Teller", Category.Action | Category.Attack, Source.Cornucopia, Location.Kingdom, Group.PlusCoin | Group.Discard | Group.Terminal)
		{
			this.BaseCost = new Cost(3);
			this.Benefit.Currency.Coin.Value = 2;
			this.Text = "Each other player reveals cards from the top of his deck until he reveals a Victory or Curse card.  He puts it on top and discards the other revealed cards.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			// Perform attack on every player
			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			enumerator.MoveNext(); // skip active player
			while (enumerator.MoveNext())
			{
				Player attackee = enumerator.Current;
				// Skip if the attack is blocked (Moat, Lighthouse, etc.)
				if (this.IsAttackBlocked[attackee])
					continue;

				attackee.BeginDrawing();
				while (attackee.CanDraw)
				{
					attackee.Draw(DeckLocation.Revealed);
					if ((attackee.Revealed.Last().Category & Cards.Category.Victory) == Cards.Category.Victory ||
						(attackee.Revealed.Last().Category & Cards.Category.Curse) == Cards.Category.Curse)
						break;
				}
				attackee.EndDrawing();

				if (attackee.Revealed.Count > 0)
				{
					Card lastCard = attackee.Revealed.Last();
					if ((lastCard.Category & Cards.Category.Victory) == Cards.Category.Victory ||
						(lastCard.Category & Cards.Category.Curse) == Cards.Category.Curse)
						attackee.AddCardToDeck(attackee.RetrieveCardFrom(DeckLocation.Revealed, lastCard), DeckPosition.Top);
				}

				attackee.DiscardRevealed();
			}
		}
	}
	public class Hamlet : Card
	{
		public Hamlet()
			: base("Hamlet", Category.Action, Source.Cornucopia, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.PlusMultipleActions | Group.PlusBuy | Group.Discard | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(2);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 1;
			this.Text = "You may discard a card; if you do, +1 Action.<nl/><nl/>You may discard a card; if you do, +1 Buy.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choiceAction = new Choice("You may discard a card for +1 Action.", this, player.Hand, player, false, 0, 1);
			ChoiceResult resultAction = player.MakeChoice(choiceAction);
			if (resultAction.Cards.Count > 0)
			{
				player.Discard(DeckLocation.Hand, resultAction.Cards);
				player.ReceiveBenefit(this, new CardBenefit() { Actions = 1 });
			}

			Choice choiceBuy = new Choice("You may discard a card for +1 Buy.", this, player.Hand, player, false, 0, 1);
			ChoiceResult resultBuy = player.MakeChoice(choiceBuy);
			if (resultBuy.Cards.Count > 0)
			{
				player.Discard(DeckLocation.Hand, resultBuy.Cards);
				player.ReceiveBenefit(this, new CardBenefit() { Buys = 1 });
			}
		}
	}
	public class Harvest : Card
	{
		public Harvest()
			: base("Harvest", Category.Action, Source.Cornucopia, Location.Kingdom, Group.PlusCoin | Group.Discard | Group.Terminal | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(5);
			this.Text = "Reveal the top 4 cards of your deck, then discard them. +<coin>1</coin> per differently named card revealed.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			CardCollection newCards = player.Draw(4, DeckLocation.Revealed);

			player.DiscardRevealed();

			CardBenefit benefit = new CardBenefit();
			benefit.Currency += new Coin(newCards.GroupBy(card => card.CardType).Count());
			player.ReceiveBenefit(this, benefit);
		}
	}
	public class HornOfPlenty : Card
	{
		public HornOfPlenty()
			: base("Horn of Plenty", Category.Treasure, Source.Cornucopia, Location.Kingdom, Group.Gain | Group.Trash)
		{
			this.BaseCost = new Cost(5);
			this.Text = "When you play this, gain a card costing up to <coin>1</coin> per differently named card you have in play, counting this.  If it's a Victory card, trash this.";
			this.Benefit.Currency.Coin.Value = 0;
		}

		public override void Play(Player player)
		{
			base.Play(player);

			List<Type> cardTypes = new List<Type>();
			foreach (Card card in player.InPlay)
			{
				Type t = card.CardType;
				if (!cardTypes.Contains(t))
					cardTypes.Add(t);
			}
			foreach (Card card in player.SetAside)
			{
				Type t = card.CardType;
				if (!cardTypes.Contains(t))
					cardTypes.Add(t);
			}

			Currencies.Coin uniqueCardsInPlay = new Currencies.Coin(cardTypes.Count);
			SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply => supply.CanGain() && supply.CurrentCost <= uniqueCardsInPlay);
			Choice choice = new Choice("Gain a card.", this, gainableSupplies, player, false);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Supply != null)
			{
				player.Gain(result.Supply);
				if ((result.Supply.Category & Cards.Category.Victory) == Cards.Category.Victory)
				{
					player.RetrieveCardFrom(DeckLocation.InPlay, this);
					player.Trash(this);
				}
			}
		}
	}
	public class HorseTraders : Card
	{
		private Player _TurnStartedEventPlayer = null;
		private Player.TurnStartedEventHandler _TurnStartedEventHandler = null;
		private Player.AttackedEventHandler _AttackHandler = null;

		public HorseTraders()
			: base("Horse Traders", 
			Category.Action | Category.Reaction, 
			Source.Cornucopia, 
			Location.Kingdom,
			Group.ReactToAttack | Group.Defense | Group.PlusCoin | Group.PlusBuy | Group.Discard | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Buys = 1;
			this.Benefit.Currency.Coin.Value = 3;
			this.Benefit.Cards = -2;
			this.Text = "<br/>When another player plays an Attack card, you may set this aside from your hand.  If you do, then at the start of your next turn, +1 Card and return this to your hand.";
		}

		internal override void TearDown()
		{
			base.TearDown();

			if (_TurnStartedEventHandler != null && _TurnStartedEventPlayer != null)
				_TurnStartedEventPlayer.TurnStarted -= _TurnStartedEventHandler;
			_TurnStartedEventPlayer = null;
			_TurnStartedEventHandler = null;
		}

		public override void AddedTo(DeckLocation location, Player player)
		{
			base.AddedTo(location, player);

			switch (location)
			{
				case DeckLocation.Hand:
					if (_AttackHandler != null)
						player.Attacked -= _AttackHandler;

					_AttackHandler = new Player.AttackedEventHandler(player_Attacked);
					player.Attacked += _AttackHandler;
					break;

				case DeckLocation.SetAside:
					if (_TurnStartedEventHandler != null)
						player.TurnStarted -= _TurnStartedEventHandler;
					_TurnStartedEventPlayer = player;
					_TurnStartedEventHandler = new Player.TurnStartedEventHandler(player_TurnStarted);
					_TurnStartedEventPlayer.TurnStarted += _TurnStartedEventHandler;
					break;
			}
		}

		internal override void player_Attacked(object sender, AttackedEventArgs e)
		{
			Player player = sender as Player;

			// Horse Traders only protects against other attackers
			if (player == e.Attacker)
				return;

			// Make sure it exists already
			if (player.Hand.Contains(this.PhysicalCard) && !e.Revealable.ContainsKey(TypeClass.HorseTraders))
				e.Revealable[TypeClass.HorseTraders] = new AttackReaction(this, String.Format("Reveal {0}", this.PhysicalCard), player_RevealHorseTraders);
		}

		internal void player_RevealHorseTraders(Player player, ref AttackedEventArgs e)
		{
			player.AddCardInto(DeckLocation.Revealed, player.RetrieveCardFrom(DeckLocation.Hand, this.PhysicalCard));
			player.AddCardInto(DeckLocation.SetAside, player.RetrieveCardFrom(DeckLocation.Revealed, this.PhysicalCard));
			// Attack isn't cancelled... it's just mitigated
			e.HandledBy.Add(TypeClass.HorseTraders);
		}

		void player_TurnStarted(object sender, TurnStartedEventArgs e)
		{
			this.PlayDuration(e.Player);
			if (_TurnStartedEventHandler != null)
				e.Player.TurnStarted -= _TurnStartedEventHandler;
			_TurnStartedEventHandler = null;
		}

		public override void PlayDuration(Player player)
		{
			base.PlayDuration(player);

			player.ReceiveBenefit(this, new CardBenefit() { Cards = 1 });
			player.AddCardToHand(player.RetrieveCardFrom(DeckLocation.SetAside, this));
		}

		public override void RemovedFrom(DeckLocation location, Player player)
		{
			base.RemovedFrom(location, player);
			if (_AttackHandler != null)
				player.Attacked -= _AttackHandler;
			_AttackHandler = null;
		}
	}
	public class HuntingParty : Card
	{
		public HuntingParty()
			: base("Hunting Party", Category.Action, Source.Cornucopia, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.Discard)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 1;
			this.Text = "Reveal your hand.  Reveal cards from your deck until you reveal a card that isn't a duplicate of one in your hand.  Put it into your hand and discard the rest.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			player.ReturnHand(player.RevealHand());

			Boolean foundUniqueCard = false;
			player.BeginDrawing();
			while (player.CanDraw)
			{
				player.Draw(DeckLocation.Revealed);
				if (player.Hand[player.Revealed.Last().CardType].Count == 0)
				{
					foundUniqueCard = true;
					break;
				}
			}
			player.EndDrawing();

			if (foundUniqueCard && player.Revealed.Count > 0)
				player.AddCardToHand(player.RetrieveCardFrom(DeckLocation.Revealed, player.Revealed.Last()));

			player.DiscardRevealed();
		}
	}
	public class Jester : Card
	{
		public Jester()
			: base("Jester", Category.Action | Category.Attack, Source.Cornucopia, Location.Kingdom, Group.PlusCurses | Group.PlusCoin | Group.Gain | Group.Discard | Group.Terminal)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Currency.Coin.Value = 2;
			this.Text = "Each other player discards the top card of his deck.  If it's a Victory card he gains a Curse.  Otherwise either he gains a copy of the discarded card or you do, your choice.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			// Perform attack on every player
			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			enumerator.MoveNext(); // skip active player
			while (enumerator.MoveNext())
			{
				Player attackee = enumerator.Current;
				// Skip if the attack is blocked (Moat, Lighthouse, etc.)
				if (this.IsAttackBlocked[attackee])
					continue;

				if (attackee.CanDraw)
				{
					Card card = attackee.Draw(DeckLocation.Revealed);
					attackee.DiscardRevealed();

					if ((card.Category & Cards.Category.Victory) == Cards.Category.Victory)
					{
						attackee.Gain(player._Game.Table.Curse);
					}
					else
					{
						Supply supply = null;
						if (player._Game.Table.Supplies.ContainsKey(card))
							supply = player._Game.Table[card];
						if (supply != null && supply.CanGain() && supply.TopCard.Name == card.Name)
						{
							Choice choice = new Choice(String.Format("Who should receive the copy of {0}?", card), this, new CardCollection() { card },
								new List<string>() { player.ToString(), attackee.ToString() }, player);
							ChoiceResult result = player.MakeChoice(choice);
							if (result.Options[0] == player.ToString())
								player.Gain(supply);
							else
								attackee.Gain(supply);
						}
					}
				}
			}
		}
	}
	public class Menagerie : Card
	{
		public Menagerie()
			: base("Menagerie", Category.Action, Source.Cornucopia, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(3);
			this.Benefit.Actions = 1;
			this.Text = "Reveal your hand.<nl/>If there are no duplicate cards in it, +3 Cards.<nl/>Otherwise, +1 Card";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			player.ReturnHand(player.RevealHand());

			List<Type> cardTypes = new List<Type>();
			foreach (Card card in player.Hand)
			{
				Type t = card.CardType;
				if (!cardTypes.Contains(t))
					cardTypes.Add(t);
			}

			CardBenefit benefit = new CardBenefit();
			if (player.Hand.Count == cardTypes.Count)
				benefit.Cards = 3;
			else
				benefit.Cards = 1;
			player.ReceiveBenefit(this, benefit);
		}
	}
	public class Princess : Card
	{
		private Game.CostComputeEventHandler _CostComputeEventHandler = null;

		public Princess()
			: base("Princess", Category.Action | Category.Prize, Source.Cornucopia, Location.Special, Group.PlusBuy | Group.ModifyCost | Group.Terminal)
		{
			this.BaseCost = new Cost(0, true, false);
			this.Benefit.Buys = 1;
			this.Text = "While this is in play, cards cost <coin>2</coin> less, but not less than <coin>0</coin>.<nl/><nl/><i>(This is not in the Supply.)</i>";
		}

		protected override Boolean AllowUndo { get { return true; } }

		public override void Play(Player player)
		{
			base.Play(player);
		}

		public override void AddedTo(DeckLocation location, Player player)
		{
			base.AddedTo(location, player);

			if (location == DeckLocation.InPlay)
			{
				if (_CostComputeEventHandler != null)
					player._Game.CostCompute -= _CostComputeEventHandler;

				_CostComputeEventHandler = new Game.CostComputeEventHandler(player_PrincessInPlayArea);
				player._Game.CostCompute += _CostComputeEventHandler;
				player._Game.SendMessage(player, this, 2);
			}
		}

		void player_PrincessInPlayArea(object sender, CostComputeEventArgs e)
		{
			e.Cost.Coin -= 2;
		}

		public override void RemovedFrom(DeckLocation location, Player player)
		{
			base.RemovedFrom(location, player);
			if (_CostComputeEventHandler != null)
				player._Game.CostCompute -= _CostComputeEventHandler;
			_CostComputeEventHandler = null;
		}
	}
	public class PrizeSupply : Card
	{
		public PrizeSupply()
			: base("Prize Supply", Category.Prize, Source.Cornucopia, Location.Invisible, Group.None)
		{
		}

		public override void Setup(Game game, Supply supply)
		{
			base.Setup(game, supply);

			supply.AddTo(new BagOfGold());
			supply.AddTo(new Diadem());
			supply.AddTo(new Followers());
			supply.AddTo(new Princess());
			supply.AddTo(new TrustySteed());
		}
	}
	public class Remake : Card
	{
		public Remake()
			: base("Remake", Category.Action, Source.Cornucopia, Location.Kingdom, Group.DeckReduction | Group.Gain | Group.Trash | Group.RemoveCurses | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.Text = "Do this twice.  Trash a card from your hand, then gain a card costing exactly <coin>1</coin> more than the trashed card.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			for (int count = 0; count < 2; count++)
			{
				Choice choiceTrash = new Choice("Choose a card to trash", this, player.Hand, player);
				ChoiceResult resultTrash = player.MakeChoice(choiceTrash);
				player.Trash(player.RetrieveCardsFrom(DeckLocation.Hand, resultTrash.Cards));

				if (resultTrash.Cards.Count > 0)
				{
					Cost trashedCardCost = player._Game.ComputeCost(resultTrash.Cards[0]);
					SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply => supply.CanGain() && supply.CurrentCost == (trashedCardCost + new Coin(1)));
					Choice choice = new Choice("Gain a card", this, gainableSupplies, player, false);
					ChoiceResult result = player.MakeChoice(choice);
					if (result.Supply != null)
						player.Gain(result.Supply);
				}
			}
		}
	}
	public class Tournament : Card
	{
		public Tournament()
			: base("Tournament", Category.Action, Source.Cornucopia, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.PlusCoin | Group.Gain | Group.Discard | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Actions = 1;
			this.Text = "Each player may reveal a Province from his hand.<nl/>If you do, discard it and gain a Prize (from the Prize pile) or a Duchy, putting it on top of your deck.<nl/>If no-one else does, +1 Card +<coin>1</coin>.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Boolean playerRevealedProvince = false;
			Boolean anyoneElseRevealedProvince = false;

			// Perform on every player (including you)
			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			while (enumerator.MoveNext())
			{
				Player actor = enumerator.Current;

				if (actor.Hand[Universal.TypeClass.Province].Count == 0)
					continue;

				Choice showChoice = Choice.CreateYesNoChoice("Do you want to reveal a Province from your hand?", this, actor);
				ChoiceResult showResult = actor.MakeChoice(showChoice);
				if (showResult.Options[0] == "Yes")
				{
					Card shownProvince = actor.RetrieveCardsFrom(DeckLocation.Hand, Universal.TypeClass.Province, 1)[0];
					actor.AddCardInto(DeckLocation.Revealed, shownProvince);

					if (actor == player)
					{
						playerRevealedProvince = true;
					}
					else
					{
						actor.AddCardInto(DeckLocation.Hand, actor.RetrieveCardFrom(DeckLocation.Revealed, shownProvince));
						anyoneElseRevealedProvince = true;
					}
				}
			}

			if (playerRevealedProvince)
			{
				player.Discard(DeckLocation.Revealed);

				Boolean isOptional = (player._Game.Table[Cards.Universal.TypeClass.Duchy].Count + player._Game.Table.SpecialPiles[TypeClass.PrizeSupply].Count) == 0;
				Choice prizeChoice = new Choice("Select a Prize or a Duchy", this, player._Game.Table.SpecialPiles[TypeClass.PrizeSupply], player, isOptional ? 0 : 1, 1);
				((CardCollection)prizeChoice.Cards).Add(new Universal.Duchy());

				ChoiceResult prizeResult = player.MakeChoice(prizeChoice);
				if (prizeResult.Cards.Count > 0)
				{
					if (prizeResult.Cards[0].CardType == Universal.TypeClass.Duchy)
						player.Gain(player._Game.Table.Duchy, DeckLocation.Deck, DeckPosition.Top);
					else
						player.Gain(player._Game.Table.SpecialPiles[TypeClass.PrizeSupply], prizeResult.Cards[0].CardType, DeckLocation.Deck, DeckPosition.Top);
				}
			}

			if (!anyoneElseRevealedProvince)
			{
				CardBenefit benefit = new CardBenefit() { Cards = 1 };
				benefit.Currency += new Coin(1);
				player.ReceiveBenefit(this, benefit);
			}
		}

		public override void Setup(Game game, Supply supply)
		{
			base.Setup(game, supply);
			Supply prizeSupply = new Supply(game, game.Players, TypeClass.PrizeSupply, Visibility.All);
			prizeSupply.FullSetup();
			game.Table.SpecialPiles.Add(TypeClass.PrizeSupply, prizeSupply);
		}
	}
	public class TrustySteed : Card
	{
		public TrustySteed()
			: base("Trusty Steed", 
			Category.Action | Category.Prize, 
			Source.Cornucopia, 
			Location.Special,
			Group.PlusCard | Group.PlusAction | Group.PlusMultipleActions | Group.PlusCoin | Group.CardOrdering | Group.Gain | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(0, true, false);
			this.Text = "Choose two: +2<nbsp/>Cards; +2<nbsp/>Actions;<nl/>+<coin>2</coin>; or gain 4 Silvers and put your deck into your discard pile.<nl/>(The choices must be different.)<nl/><nl/><i>(This is not in the Supply.)</i>";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choice = new Choice("Choose 2:", this, new CardCollection() { this }, new List<string>() { "+2<nbsp/>Cards", "+2<nbsp/>Actions", "+<coin>2</coin>", "Gain 4 Silvers & discard deck" }, player, null, false, 2, 2);
			ChoiceResult result = player.MakeChoice(choice);

			foreach (String option in result.Options)
			{
				CardBenefit benefit = new CardBenefit();
				if (option == "+2<nbsp/>Cards")
					benefit.Cards = 2;
				if (option == "+2<nbsp/>Actions")
					benefit.Actions += 2;
				if (option == "+<coin>2</coin>")
					benefit.Currency += new Coin(2);
				if (option == "Gain 4 Silvers & discard deck")
				{
					player.Gain(player._Game.Table.Silver, 4);

					player._Game.SendMessage(player, this);
					CardCollection cc = player.RetrieveCardsFrom(DeckLocation.Deck);
					player.AddCardsInto(DeckLocation.Discard, cc);
				}
				player.ReceiveBenefit(this, benefit);
			}
		}
	}
	public class YoungWitch : Card
	{
		public YoungWitch()
			: base("Young Witch", Category.Action | Category.Attack, Source.Cornucopia, Location.Kingdom, Group.PlusCurses | Group.PlusCard | Group.Discard | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Cards = 2;
			this.Text = "Discard 2 cards. Each other player may reveal a Bane card from his hand. If he doesn't, he gains a Curse.<br/>Setup: Add an extra Kingdom card pile costing <coin>2</coin> or <coin>3</coin> to the Supply.  Cards from that pile are Bane cards.";
		}

		public override String SpecialPresetKey { get { return "Bane"; } }

		public override void Play(Player player)
		{
			base.Play(player);

			// discard 2 cards
			Choice choiceDiscard = new Choice("Choose two cards to discard", this, player.Hand, player, false, 2, 2);
			ChoiceResult resultDiscard = player.MakeChoice(choiceDiscard);
			player.Discard(DeckLocation.Hand, resultDiscard.Cards);

			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			enumerator.MoveNext();
			while (enumerator.MoveNext())
			{
				Player attackee = enumerator.Current;
				if (this.IsAttackBlocked[attackee])
					continue;

				if (!attackee.TokenActOn(player, this))
					continue;

				attackee.Gain(player._Game.Table.Curse);
			}
		}

		public override void Setup(Game game, Supply supply)
		{
			base.Setup(game, supply);

			Card baneCard = null;
			try
			{
				if (game.Settings.Preset != null)
				{
					baneCard = game.Settings.Preset.CardCards[game.Settings.Preset.Cards.First(c => c.CardType == this.CardType)].ElementAt(0);
				}
				else
				{
					IList<Card> availableBaneCards = null;
					Boolean shouldUseGameConstraints = true;
					ConstraintCollection ywConstraints = new ConstraintCollection();
					if (game.Settings.CardSettings.ContainsKey(this.Name))
					{
						CardsSettings ywSettings = game.Settings.CardSettings[this.Name];
						shouldUseGameConstraints = (Boolean)ywSettings.CardSettingCollection[typeof(YoungWitch_UseGameConstraints)].Value;
						ywConstraints = (ConstraintCollection)ywSettings.CardSettingCollection[typeof(YoungWitch_Constraints)].Value;
					}

					// need to setup a bane supply pile here; randomly pick an unused supply card type of cost $2 or $3 from
					// the Kingdom cards, create a new supply pile of it, and mark it with a Bane token
					availableBaneCards = game.CardsAvailable.Where(c => c.BaseCost == new Cost(2) || c.BaseCost == new Cost(3)).ToList();
					if (shouldUseGameConstraints)
					{
						// Skip all "Must Use" constraints
						ConstraintCollection constraints = new ConstraintCollection(game.Settings.Constraints.Where(c => c.ConstraintType != ConstraintType.CardMustUse));
						availableBaneCards = constraints.SelectCards(availableBaneCards, 1);
					}
					else
						availableBaneCards = ywConstraints.SelectCards(availableBaneCards, 1);
					baneCard = availableBaneCards[0];
				}
			}
			catch (DominionBase.Cards.ConstraintException ce)
			{
				throw new YoungWitchConstraintException(String.Format("Problem setting up Young Witch constraints: {0}", ce.Message));
			}

			game.CardsAvailable.Remove(baneCard);
			game.Table.AddKingdomSupply(game.Players, baneCard.CardType);
			game.Table.Supplies[baneCard].Setup();
			game.Table.Supplies[baneCard].SnapshotSetup();
			game.Table.Supplies[baneCard].AddToken(new BaneToken());
		}

		public override void CheckSetup(Preset preset, Table table)
		{
			// We need to find out what the Bane card is, remove it from the Preset cards, and add it to our own CardCards in the Preset
			foreach (Supply supply in table.Supplies.Values)
			{
				if (supply.Tokens.Any(t => t.GetType() == TypeClass.BaneToken))
				{
					// This is our supply!
					preset.Cards.Remove(preset.Cards.Find(c => c.CardType == supply.CardType));
					this.CheckSetup(preset, Card.CreateInstance(supply.CardType));
				}
			}
		}

		public override void CheckSetup(Preset preset, Card card)
		{
			if (card == null)
				return;
			preset.CardCards[this] = new CardCollection { card };
		}

		public override List<Type> GetSerializingTypes()
		{
			return new List<Type>() { typeof(YoungWitch_UseGameConstraints), typeof(YoungWitch_Constraints) };
		}

		public override CardSettingCollection GenerateSettings()
		{
			CardSettingCollection csc = new CardSettingCollection();
			csc.Add(new YoungWitch_UseGameConstraints { Value = false });
			csc.Add(new YoungWitch_Constraints { Value = new ConstraintCollection() });

			return csc;
		}

		public override void FinalizeSettings(CardSettingCollection settings)
		{
			(settings[typeof(YoungWitch_Constraints)].Value as ConstraintCollection).MaxCount = 1;
		}

		[Serializable]
		public class YoungWitch_UseGameConstraints : CardSetting
		{
			public override String Name { get { return "UseGameConstraints"; } }
			public override String Text { get { return "Use Game constraints instead of the ones listed below"; } }
			public override String Hint { get { return "Use the defined Game constraints instead of the ones defined here"; } }
			public override Type Type { get { return typeof(Boolean); } }
		}

		[Serializable]
		public class YoungWitch_Constraints : CardSetting
		{
			public override String Name { get { return "Constraints"; } }
			public override String Hint { get { return "Constraints to use for selecting a Bane card to use"; } }
			public override Type Type { get { return typeof(ConstraintCollection); } }
		}
	}

	public class YoungWitchConstraintException : ConstraintException
	{
		public YoungWitchConstraintException() { }
		public YoungWitchConstraintException(string message) : base(message) { }
		public YoungWitchConstraintException(string message, Exception innerException) : base(message, innerException) { }
		internal YoungWitchConstraintException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
