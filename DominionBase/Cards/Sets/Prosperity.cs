using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DominionBase.Currencies;
using DominionBase.Piles;
using DominionBase.Players;

namespace DominionBase.Cards.Prosperity
{
	public static class TypeClass
	{
		public static Type Bank = typeof(Bank);
		public static Type Bishop = typeof(Bishop);
		public static Type City = typeof(City);
		public static Type Colony = typeof(Colony);
		public static Type Contraband = typeof(Contraband);
		public static Type CountingHouse = typeof(CountingHouse);
		public static Type Expand = typeof(Expand);
		public static Type Forge = typeof(Forge);
		public static Type Goons = typeof(Goons);
		public static Type GrandMarket = typeof(GrandMarket);
		public static Type Hoard = typeof(Hoard);
		public static Type KingsCourt = typeof(KingsCourt);
		public static Type Loan = typeof(Loan);
		public static Type Mint = typeof(Mint);
		public static Type Monument = typeof(Monument);
		public static Type Mountebank = typeof(Mountebank);
		public static Type Peddler = typeof(Peddler);
		public static Type Platinum = typeof(Platinum);
		public static Type Quarry = typeof(Quarry);
		public static Type Rabble = typeof(Rabble);
		public static Type RoyalSeal = typeof(RoyalSeal);
		public static Type Talisman = typeof(Talisman);
		public static Type TradeRoute = typeof(TradeRoute);
		public static Type Vault = typeof(Vault);
		public static Type Venture = typeof(Venture);
		public static Type Watchtower = typeof(Watchtower);
		public static Type WorkersVillage = typeof(WorkersVillage);

		public static Type ContrabandToken = typeof(ContrabandToken);
		public static Type TradeRouteToken = typeof(TradeRouteToken);
		public static Type VictoryToken = typeof(VictoryToken);
	}

	public class Bank : Card
	{
		public Bank()
			: base("Bank", Category.Treasure, Source.Prosperity, Location.Kingdom, Group.PlusCoin | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(7);
			this.Text = "When you play this, it's worth <coin>1</coin> per Treasure card you have in play (counting this).";
			this.Benefit.Currency.Coin.IsVariable = true;
		}

		protected override Boolean AllowUndo { get { return true; } }

		public override void Play(Player player)
		{
			base.Play(player);

			CardBenefit benefit = new CardBenefit();
			benefit.Currency += new Coin(player.InPlay[Category.Treasure].Count);
			player.ReceiveBenefit(this, benefit);
		}
	}
	public class Bishop : Card
	{
		public Bishop()
			: base("Bishop", Category.Action, Source.Prosperity, Location.Kingdom, Group.Component | Group.DeckReduction | Group.PlusCoin | Group.Trash | Group.RemoveCurses | Group.AffectOthers | Group.Terminal | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Currency.Coin.Value = 1;
			this.Benefit.VictoryPoints = 1;
			this.Text = "Trash a card from your hand.<nl/>+<vp/> equal to half its cost in coins, rounded down.<nl/>Each other player may trash a card from his hand.";
		}

		internal override void ReceivedBy(Player player)
		{
			base.ReceivedBy(player);
			if (!player.TokenPiles.ContainsKey(TypeClass.VictoryToken))
				player.TokenPiles[TypeClass.VictoryToken] = new TokenCollection();
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choice = new Choice("Trash a card. +<vp/> equal to half its cost in coins, rounded down.", this, player.Hand, player);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Cards.Count > 0)
			{
				Card trash = player.RetrieveCardFrom(DeckLocation.Hand, result.Cards[0]);
				Cost trashedCardCost = player._Game.ComputeCost(trash);
				player.Trash(trash);

				player.ReceiveBenefit(this, new CardBenefit() { VictoryPoints = trashedCardCost.Coin.Value / 2 });
			}

			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			enumerator.MoveNext(); // skip active player
			while (enumerator.MoveNext())
			{
				Player otherPlayer = enumerator.Current;
				Choice choicePlayer = new Choice("Trash a card if you wish", this, otherPlayer.Hand, otherPlayer, false, 0, 1);
				ChoiceResult resultPlayer = otherPlayer.MakeChoice(choicePlayer);
				if (resultPlayer.Cards.Count > 0)
				{
					otherPlayer.Trash(otherPlayer.RetrieveCardFrom(DeckLocation.Hand, resultPlayer.Cards[0]));
				}
			}
		}
	}
	public class City : Card
	{
		public City()
			: base("City", 
			Category.Action, 
			Source.Prosperity, 
			Location.Kingdom,
			Group.PlusCard | Group.PlusAction | Group.PlusMultipleActions | Group.PlusCoin | Group.PlusBuy | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Actions = 2;
			this.Benefit.Cards = 1;
			this.Text = "If there are one or more empty Supply piles, +1<nbsp/>Card.  If there are two or more, +<coin>1</coin> and +1<nbsp/>Buy.";
		}

		public override void Play(Player player)
		{
			int emptyPiles = player._Game.Table.Supplies.EmptySupplyPiles;
			if (emptyPiles > 0)
			{
				this.Benefit.Cards = 2;
				if (emptyPiles > 1)
				{
					this.Benefit.Currency.Coin.Value = 1;
					this.Benefit.Buys = 1;
				}
			}
			base.Play(player);
		}
	}
	public class Colony : Card
	{
		public Colony()
			: base("Colony", Category.Victory, Source.Prosperity, Location.General)
		{
			this.BaseCost = new Cost(11);
			this.VictoryPoints = 10;
		}

		public override Boolean IsEndgameTriggered(Supply supply)
		{
			return (supply.Count == 0);
		}
	}
	public class Contraband : Card
	{
		private Player _TurnEndedPlayer = null;
		private Player.TurnEndedEventHandler _TurnEndedEventHandler = null;
		private Dictionary<Supply, Supply.BuyCheckEventHandler> _BuyCheckEventHandlers = new Dictionary<Supply, Supply.BuyCheckEventHandler>();

		public Contraband()
			: base("Contraband", Category.Treasure, Source.Prosperity, Location.Kingdom, Group.PlusBuy | Group.PlusCoin)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Buys = 1;
			this.Benefit.Currency.Coin.Value = 3;
			this.Text = "When you play this, the player to your left names a card.<nl/>You can't buy that card this turn.";
		}

		internal override void TearDown()
		{
			base.TearDown();

			foreach (Supply supply in _BuyCheckEventHandlers.Keys)
				supply.BuyCheck -= _BuyCheckEventHandlers[supply];
			_BuyCheckEventHandlers.Clear();

			if (_TurnEndedEventHandler != null && _TurnEndedPlayer != null)
				player_TurnEnded(_TurnEndedPlayer, new TurnEndedEventArgs(_TurnEndedPlayer));
			_TurnEndedEventHandler = null;
		}

		public override void Play(Player player)
		{
			base.Play(player);

			if (_TurnEndedEventHandler == null)
			{
				_TurnEndedPlayer = player;
				_TurnEndedEventHandler = new Player.TurnEndedEventHandler(player_TurnEnded);
				player.TurnEnded += _TurnEndedEventHandler;
			}

			// Get the player to my left
			Player playerToLeft = player._Game.GetPlayerFromIndex(player, 1);

			Choice choice = new Choice(String.Format("Name a card {0} can't buy", player), this, player._Game.Table.Supplies, player, false);
			ChoiceResult result = playerToLeft.MakeChoice(choice);
			player._Game.SendMessage(playerToLeft, this, result.Supply);

			if (!_BuyCheckEventHandlers.ContainsKey(result.Supply))
			{
				result.Supply.AddToken(new ContrabandToken());
				_BuyCheckEventHandlers[result.Supply] = new Supply.BuyCheckEventHandler(Supply_BuyCheck);
				result.Supply.BuyCheck += _BuyCheckEventHandlers[result.Supply];
			}
		}

		private void Supply_BuyCheck(object sender, BuyCheckEventArgs e)
		{
			// Already been cancelled -- don't need to process this one
			if (e.Cancelled)
				return;

			e.Cancelled = true;
			return;
		}

		void player_TurnEnded(object sender, TurnEndedEventArgs e)
		{
			Player player = sender as Player;

			if (_TurnEndedEventHandler != null && _TurnEndedPlayer != null)
				_TurnEndedPlayer.TurnEnded -= _TurnEndedEventHandler;
			_TurnEndedPlayer = null;
			_TurnEndedEventHandler = null;

			foreach (Supply supply in _BuyCheckEventHandlers.Keys)
				supply.BuyCheck -= _BuyCheckEventHandlers[supply];
			_BuyCheckEventHandlers.Clear();
		}
	}
	public class ContrabandToken : Token
	{
		public ContrabandToken()
			: base("X", "Unbuyable")
		{
		}

		public override string Title { get { return "This card cannot be bought this turn"; } }
		public override Boolean IsTemporary { get { return true; } }
	}
	public class CountingHouse : Card
	{
		public CountingHouse()
			: base("Counting House", Category.Action, Source.Prosperity, Location.Kingdom, Group.PlusCard | Group.Terminal)
		{
			this.BaseCost = new Cost(5);
			this.Text = "Look through your discard pile, reveal any number of Copper cards from it, and put them into your hand.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			int copperCount = player.DiscardPile.LookThrough(c => c.CardType == Cards.Universal.TypeClass.Copper).Count;
			List<String> options = new List<string>();
			for (int i = 0; i <= copperCount; i++)
				options.Add(i.ToString());
			Choice choice = new Choice("How many Copper cards would you like to reveal and put into your hand?", this, new CardCollection() { this }, options, player);
			ChoiceResult result = player.MakeChoice(choice);
			int number = int.Parse(result.Options[0]);
			player.AddCardsInto(DeckLocation.Revealed, player.RetrieveCardsFrom(DeckLocation.Discard, Cards.Universal.TypeClass.Copper, number));
			player.AddCardsToHand(DeckLocation.Revealed);
		}
	}
	public class Expand : Card
	{
		public Expand()
			: base("Expand", Category.Action, Source.Prosperity, Location.Kingdom, Group.Gain | Group.Trash | Group.RemoveCurses | Group.Terminal)
		{
			this.BaseCost = new Cost(7);
			this.Text = "Trash a card from your hand.<nl/>Gain a card costing up to <coin>3</coin> more than the trashed card.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choiceTrash = new Choice("Choose a card to trash", this, player.Hand, player);
			ChoiceResult resultTrash = player.MakeChoice(choiceTrash);
			player.Trash(player.RetrieveCardsFrom(DeckLocation.Hand, resultTrash.Cards));

			if (resultTrash.Cards.Count > 0)
			{
				Cost trashedCardCost = player._Game.ComputeCost(resultTrash.Cards[0]);
				SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply => supply.CanGain() && supply.CurrentCost <= (trashedCardCost + new Coin(3)));
				Choice choice = new Choice("Gain a card", this, gainableSupplies, player, false);
				ChoiceResult result = player.MakeChoice(choice);
				if (result.Supply != null) 
					player.Gain(result.Supply);
			}
		}
	}
	public class Forge : Card
	{
		public Forge()
			: base("Forge", Category.Action, Source.Prosperity, Location.Kingdom, Group.DeckReduction | Group.Gain | Group.Trash | Group.RemoveCurses | Group.Terminal)
		{
			this.BaseCost = new Cost(7);
			this.Text = "Trash any number of cards from your hand.  Gain a card with cost exactly equal to the total cost in coins of the trashed cards.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choiceTrash = new Choice("Choose any number of cards to trash", this, player.Hand, player, false, 0, player.Hand.Count);
			ChoiceResult resultTrash = player.MakeChoice(choiceTrash);
			player.Trash(player.RetrieveCardsFrom(DeckLocation.Hand, resultTrash.Cards));

			Coin totalCoinCost = new Coin();
			foreach (Card card in resultTrash.Cards)
				totalCoinCost += player._Game.ComputeCost(card).Coin;

			SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply => supply.CanGain() && supply.CurrentCost == new Cost(totalCoinCost));
			Choice choice = new Choice("Gain a card", this, gainableSupplies, player, false);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Supply != null)
				player.Gain(result.Supply);
		}
	}
	public class Goons : Card
	{
		private Player.CardBoughtEventHandler _CardBoughtHandler = null;

		public Goons()
			: base("Goons", Category.Action | Category.Attack, Source.Prosperity, Location.Kingdom, Group.Component | Group.PlusCoin | Group.PlusBuy | Group.Discard | Group.Terminal)
		{
			this.BaseCost = new Cost(6);
			this.Benefit.Buys = 1;
			this.Benefit.Currency.Coin.Value = 2;
			this.Text = "Each other player discards down to 3 cards in his hand.<br/>While this is in play, when you buy a card, +<vp>1</vp>.";
		}

		internal override void ReceivedBy(Player player)
		{
			base.ReceivedBy(player);
			if (!player.TokenPiles.ContainsKey(TypeClass.VictoryToken))
				player.TokenPiles[TypeClass.VictoryToken] = new TokenCollection();
		}

		public override void Play(Player player)
		{
			base.Play(player);
			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			enumerator.MoveNext();
			while (enumerator.MoveNext())
			{
				Player attackee = enumerator.Current;
				// Skip if the attack is blocked (Moat, Lighthouse, etc.)
				if (this.IsAttackBlocked[attackee])
					continue;

				Choice choice = new Choice("Choose cards to discard.  You must discard down to 3 cards in hand", this, attackee.Hand, attackee, false, attackee.Hand.Count - 3, attackee.Hand.Count - 3);
				ChoiceResult result = attackee.MakeChoice(choice);
				attackee.Discard(DeckLocation.Hand, result.Cards);
			}
		}

		public override void AddedTo(DeckLocation location, Player player)
		{
			base.AddedTo(location, player);

			if (location == DeckLocation.InPlay)
			{
				if (_CardBoughtHandler != null)
					player.CardBought -= _CardBoughtHandler;

				_CardBoughtHandler = new Player.CardBoughtEventHandler(player_CardBought);
				player.CardBought += _CardBoughtHandler;
			}
		}

		void player_CardBought(object sender, CardBuyEventArgs e)
		{
			// Already been cancelled or processed -- don't need to process this one
			if (e.Cancelled || e.HandledBy.Contains(this))
				return;

			e.HandledBy.Add(this);

			(sender as Player).ReceiveBenefit(this, new CardBenefit() { VictoryPoints = 1 });
		}

		public override void RemovedFrom(DeckLocation location, Player player)
		{
			base.RemovedFrom(location, player);
			if (_CardBoughtHandler != null)
				player.CardBought -= _CardBoughtHandler;
			_CardBoughtHandler = null;
		}
	}
	public class GrandMarket : Card
	{
		public GrandMarket()
			: base("Grand Market", Category.Action, Source.Prosperity, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.PlusCoin | Group.PlusBuy)
		{
			this.BaseCost = new Cost(6);
			this.Benefit.Actions = 1;
			this.Benefit.Cards = 1;
			this.Benefit.Buys = 1;
			this.Benefit.Currency.Coin.Value = 2;
			this.Text = "<br/>You can't buy this if you have any Copper in play.";
		}

		internal override bool CanBuy(Player player)
		{
			return (player.InPlay[Cards.Universal.TypeClass.Copper].Count == 0);
		}
	}
	public class Hoard : Card
	{
		private Player.CardBoughtEventHandler _CardBoughtHandler = null;

		public Hoard()
			: base("Hoard", Category.Treasure, Source.Prosperity, Location.Kingdom, Group.Gain | Group.PlusCoin)
		{
			this.BaseCost = new Cost(6);
			this.Benefit.Currency.Coin.Value = 2;
			this.Text = "<br/>While this is in play, when you buy a Victory card, gain a Gold.";
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
				if (_CardBoughtHandler != null)
					player.CardBought -= _CardBoughtHandler;

				_CardBoughtHandler = new Player.CardBoughtEventHandler(player_CardBought);
				player.CardBought += _CardBoughtHandler;
			}
		}

		void player_CardBought(object sender, CardBuyEventArgs e)
		{
			if (e.HandledBy.Contains(this) || e.Actions.ContainsKey(TypeClass.Hoard) || 
				(e.Card.Category & Category.Victory) != Category.Victory || !e.Game.Table.Gold.CanGain())
				return;

			Player player = sender as Player;
			e.Actions[TypeClass.Hoard] = new Players.CardBuyAction(this.Owner, this, "gain a Gold", player_BuyWithHoard, true);
		}

		internal void player_BuyWithHoard(Player player, ref Players.CardBuyEventArgs e)
		{
			player.Gain(e.Game.Table.Gold);

			e.HandledBy.Add(this);
		}

		public override void RemovedFrom(DeckLocation location, Player player)
		{
			base.RemovedFrom(location, player);
			if (_CardBoughtHandler != null)
				player.CardBought -= _CardBoughtHandler;
			_CardBoughtHandler = null;
		}
	}
	public class KingsCourt : Card
	{
		private Boolean _CanCleanUp = true;

		public KingsCourt()
			: base("King's Court", Category.Action, Source.Prosperity, Location.Kingdom)
		{
			this.BaseCost = new Cost(7);
			this.Text = "You may choose an Action card in your hand.  Play it three times.";
		}

		public override Boolean CanCleanUp { get { return this._CanCleanUp; } }

		public override void AddedTo(DeckLocation location, Player player)
		{
			base.AddedTo(location, player);
			this._CanCleanUp = true;
		}

		public override void Play(Player player)
		{
			this._CanCleanUp = true;

			base.Play(player);

			Choice choice = new Choice(String.Format("You may choose an Action card to play three times", player), this, player.Hand[Cards.Category.Action], player, false, 0, 1);
			ChoiceResult result = player.MakeChoice(choice);

			if (result.Cards.Count > 0)
			{
				result.Cards[0].ModifiedBy = this;

				player.Actions++;
				PlayerMode previousPlayerMode = player.PutCardIntoPlay(result.Cards[0], String.Empty);
				Card logicalCard = result.Cards[0].LogicalCard;
				player.PlayCard(result.Cards[0].LogicalCard, previousPlayerMode);
				player.Actions++;
				previousPlayerMode = player.PutCardIntoPlay(result.Cards[0], "again");
				player.PlayCard(logicalCard, previousPlayerMode);
				player.Actions++;
				previousPlayerMode = player.PutCardIntoPlay(result.Cards[0], "again");
				player.PlayCard(logicalCard, previousPlayerMode);

				this._CanCleanUp = logicalCard.CanCleanUp;
			}
			else
				player.PlayNothing();
		}

		protected override void ModifyDuration(Player player, Card card)
		{
			base.ModifyDuration(player, card);
			base.ModifyDuration(player, card);
			base.ModifyDuration(player, card);
		}
	}
	public class Loan : Card
	{
		public Loan()
			: base("Loan", Category.Treasure, Source.Prosperity, Location.Kingdom, Group.DeckReduction | Group.Trash | Group.PlusCoin | Group.Discard)
		{
			this.BaseCost = new Cost(3);
			this.Benefit.Currency.Coin.Value = 1;
			this.Text = "When you play this, reveal cards from your deck until you reveal a Treasure.<nl/>Discard it or trash it.<nl/>Discard the other cards.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			player.BeginDrawing();
			while (player.Revealed[Category.Treasure].Count < 1 && player.CanDraw)
				player.Draw(DeckLocation.Revealed);

			player.EndDrawing();

			CardCollection treasureCards = player.Revealed[c => (c.Category & Category.Treasure) == Category.Treasure];
			if (treasureCards.Count > 0)
			{
				Card card = treasureCards[0];
				Choice choice = new Choice(String.Format("You revealed a {0}.", card.Name), this, new CardCollection() { card }, new List<string>() { "Discard it", "Trash it" }, player);
				ChoiceResult result = player.MakeChoice(choice);
				if (result.Options[0] == "Discard it")
				{
					player.Discard(DeckLocation.Revealed, card);
				}
				else if (result.Options[0] == "Trash it")
				{
					player.Trash(player.RetrieveCardFrom(DeckLocation.Revealed, card));
				}
			}
			player.DiscardRevealed();
		}
	}
	public class Mint : Card
	{
		private Dictionary<Player, Player.CardBoughtEventHandler> _CardBoughtHandlers = new Dictionary<Player, Player.CardBoughtEventHandler>();

		public Mint()
			: base("Mint", Category.Action, Source.Prosperity, Location.Kingdom, Group.DeckReduction | Group.Gain | Group.Trash | Group.Terminal)
		{
			this.BaseCost = new Cost(5);
			this.Text = "You may reveal a Treasure card from your hand.  Gain a copy of it.<br/>When you buy this, trash all Treasures you have in play.";
		}

		internal override void TearDown()
		{
			base.TearDown();

			foreach (Player playerLoop in _CardBoughtHandlers.Keys)
				playerLoop.CardBought -= _CardBoughtHandlers[playerLoop];
			_CardBoughtHandlers.Clear();
		}

		public override void Play(Player player)
		{
			base.Play(player);
			if (player.Hand[Category.Treasure].Count > 0)
			{
				Choice choice = new Choice("You may reveal a Treasure card to gain a copy of it.", this, player.Hand[Category.Treasure], player, false, 0, 1);
				ChoiceResult result = player.MakeChoice(choice);
				if (result.Cards.Count > 0)
				{
					player.AddCardInto(DeckLocation.Revealed, player.RetrieveCardFrom(DeckLocation.Hand, result.Cards[0]));
					player.AddCardInto(DeckLocation.Hand, player.RetrieveCardFrom(DeckLocation.Revealed, result.Cards[0]));
					Supply supply = player._Game.Table.FindSupplyPileByCard(result.Cards[0]);
					if (supply != null && supply.TopCard != null && supply.TopCard.Name == result.Cards[0].Name)
						player.Gain(supply);
				}
			}
		}

		public override void AddedToSupply(Game game, Supply supply)
		{
			base.AddedToSupply(game, supply);

			ResetTriggers(game);
		}

		private void ResetTriggers(Game game)
		{
			IEnumerator<Player> enumPlayers = game.GetPlayersStartingWithActiveEnumerator();
			while (enumPlayers.MoveNext())
			{
				_CardBoughtHandlers[enumPlayers.Current] = new Player.CardBoughtEventHandler(player_CardBought);
				enumPlayers.Current.CardBought += _CardBoughtHandlers[enumPlayers.Current];
			}
		}

		void player_CardBought(object sender, Players.CardBuyEventArgs e)
		{
			// This is not the card you are looking for
			if (e.Card != this || e.Actions.ContainsKey(TypeClass.Mint))
				return;

			Player player = sender as Player;
			e.Actions[TypeClass.Mint] = new Players.CardBuyAction(this.Owner, this, "trash all Treasures in play", player_BuyMint, true);
		}

		internal void player_BuyMint(Player player, ref Players.CardBuyEventArgs e)
		{
			player.Trash(player.RetrieveCardsFrom(DeckLocation.InPlay, Category.Treasure));
			player.Trash(player.RetrieveCardsFrom(DeckLocation.SetAside, Category.Treasure));

			e.HandledBy.Add(TypeClass.Mint);

			// Clear out the Event Triggers -- this only happens when its Gained, so we don't care any more
			foreach (Player playerLoop in _CardBoughtHandlers.Keys)
				playerLoop.CardBought -= _CardBoughtHandlers[playerLoop];
			_CardBoughtHandlers.Clear();
		}
	}
	public class Monument : Card
	{
		public Monument()
			: base("Monument", Category.Action, Source.Prosperity, Location.Kingdom, Group.Component | Group.PlusCoin | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Currency.Coin.Value = 2;
			this.Benefit.VictoryPoints = 1;
		}

		protected override Boolean AllowUndo { get { return true; } }

		internal override void ReceivedBy(Player player)
		{
			base.ReceivedBy(player);
			if (!player.TokenPiles.ContainsKey(TypeClass.VictoryToken))
				player.TokenPiles[TypeClass.VictoryToken] = new TokenCollection();
		}
	}
	public class Mountebank : Card
	{
		public Mountebank()
			: base("Mountebank", Category.Action | Category.Attack, Source.Prosperity, Location.Kingdom, Group.PlusCurses | Group.PlusCoin | Group.Discard | Group.Terminal)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Currency.Coin.Value = 2;
			this.Text = "Each other player may discard a Curse.  If he doesn't, he gains a Curse and a Copper.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			// Perform attack on every other player
			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			enumerator.MoveNext();
			while (enumerator.MoveNext())
			{
				Player attackee = enumerator.Current;
				// Skip if the attack is blocked (Moat, Lighthouse, etc.)
				if (this.IsAttackBlocked[attackee])
					continue;

				Boolean discardedCurse = false;
				if (attackee.Hand[Category.Curse].Count > 0)
				{
					Choice choice = new Choice("Do you want to discard a Curse or gain a Curse and a Copper?", this, new CardCollection() { this }, new List<string>() { "Discard Curse", "Gain Curse & Copper" }, attackee);
					ChoiceResult result = attackee.MakeChoice(choice);
					if (result.Options[0] == "Discard Curse")
					{
						discardedCurse = true;
						attackee.Discard(DeckLocation.Hand, Cards.Universal.TypeClass.Curse, 1);
					}
				}
				if (!discardedCurse)
				{
					attackee.Gain(player._Game.Table.Curse);
					attackee.Gain(player._Game.Table.Copper);
				}
			}
		}
	}
	public class Peddler : Card
	{
		private Dictionary<Player, Player.CardGainedEventHandler> _CardGainedHandlers = new Dictionary<Player, Player.CardGainedEventHandler>();
		private Game.CostComputeEventHandler _CostComputeEventHandler = null;

		public Peddler()
			: base("Peddler", Category.Action, Source.Prosperity, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.PlusCoin | Group.ModifyCost | Group.VariableCost)
		{
			this.BaseCost = new Cost(8, true, false);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 1;
			this.Benefit.Currency.Coin.Value = 1;
			this.Text = "<br/>During your Buy phase, this costs <coin>2</coin> less per Action card you have in play, but not less than <coin>0</coin>.";
		}

		internal override void TearDown()
		{
			base.TearDown();

			foreach (Player playerLoop in _CardGainedHandlers.Keys)
				playerLoop.CardGained -= _CardGainedHandlers[playerLoop];
			_CardGainedHandlers.Clear();
		}

		public override void AddedToSupply(Game game, Supply supply)
		{
			base.AddedToSupply(game, supply);

			ResetTriggers(game);
		}

		private void ResetTriggers(Game game)
		{
			IEnumerator<Player> enumPlayers = game.GetPlayersStartingWithActiveEnumerator();
			while (enumPlayers.MoveNext())
			{
				_CardGainedHandlers[enumPlayers.Current] = new Player.CardGainedEventHandler(player_CardGained);
				enumPlayers.Current.CardGained += _CardGainedHandlers[enumPlayers.Current];
			}

			if (_CostComputeEventHandler == null)
			{
				_CostComputeEventHandler = new Game.CostComputeEventHandler(peddler_CostCompute);
				game.CostCompute += _CostComputeEventHandler;
			}
		}

		void player_CardGained(object sender, Players.CardGainEventArgs e)
		{
			// This is not the card you are looking for
			if (e.Card != this)
				return;

			// Clear out the Event Triggers -- the cost computation only matters when it's in the Supply
			foreach (Player playerLoop in _CardGainedHandlers.Keys)
				playerLoop.CardGained -= _CardGainedHandlers[playerLoop];
			_CardGainedHandlers.Clear();

			if (_CostComputeEventHandler != null)
				_CostComputeEventHandler = null;
		}

		void peddler_CostCompute(object sender, CostComputeEventArgs e)
		{
			if (e.Card != this)
				return;

			Game game = sender as Game;
			if (game.ActivePlayer != null && 
				(game.ActivePlayer.Phase == PhaseEnum.Buy || game.ActivePlayer.Phase == PhaseEnum.BuyTreasure))
			{
				int actionCardsInPlay = game.ActivePlayer.InPlay[Cards.Category.Action].Count + game.ActivePlayer.SetAside[Cards.Category.Action].Count;
				e.Cost.Coin -= 2 * actionCardsInPlay;
			}
		}
	}
	public class Platinum : Card
	{
		public Platinum()
			: base("Platinum", Category.Treasure, Source.Prosperity, Location.General)
		{
			this.BaseCost = new Cost(9);
			this.Benefit.Currency.Coin.Value = 5;
		}

		protected override Boolean AllowUndo { get { return true; } }
	}
	public class Quarry : Card
	{
		private Game.CostComputeEventHandler _CostComputeEventHandler = null;

		public Quarry()
			: base("Quarry", Category.Treasure, Source.Prosperity, Location.Kingdom, Group.ModifyCost | Group.PlusCoin)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Currency.Coin.Value = 1;
			this.Text = "<br/>While this is in play, Action cards cost <coin>2</coin> less, but not less than <coin>0</coin>";
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

				_CostComputeEventHandler = new Game.CostComputeEventHandler(player_QuarryInPlayArea);
				player._Game.CostCompute += _CostComputeEventHandler;
				player._Game.SendMessage(player, this, 2);
			}
		}

		void player_QuarryInPlayArea(object sender, CostComputeEventArgs e)
		{
			if ((e.Card.Category & Cards.Category.Action) == Cards.Category.Action)
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
	public class Rabble : Card
	{
		public Rabble()
			: base("Rabble", Category.Action | Category.Attack, Source.Prosperity, Location.Kingdom, Group.CardOrdering | Group.PlusCard | Group.Discard | Group.Terminal)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Cards = 3;
			this.Text = "Each other player reveals the top 3 cards of his deck, discards the revealed Actions and Treasures, and puts the rest back on top in any order he chooses.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			// Perform attack on every other player
			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			enumerator.MoveNext();
			while (enumerator.MoveNext())
			{
				Player attackee = enumerator.Current;
				// Skip if the attack is blocked (Moat, Lighthouse, etc.)
				if (this.IsAttackBlocked[attackee])
					continue;

				attackee.Draw(3, DeckLocation.Revealed);
				Predicate<Card> actionsTreasures = (c => 
					(c.Category & Category.Action) == Category.Action || 
					(c.Category & Category.Treasure) == Category.Treasure);
				attackee.Discard(DeckLocation.Revealed, actionsTreasures);

				Choice replaceChoice = new Choice("Choose order of cards to put back on your deck", this, attackee.Revealed, player, true, attackee.Revealed.Count, attackee.Revealed.Count);
				ChoiceResult replaceResult = attackee.MakeChoice(replaceChoice);
				attackee.AddCardsToDeck(attackee.RetrieveCardsFrom(DeckLocation.Revealed, replaceResult.Cards), DeckPosition.Top);
			}
		}
	}
	public class RoyalSeal : Card
	{
		private Player.CardGainedEventHandler _CardGainedHandler = null;

		public RoyalSeal()
			: base("Royal Seal", Category.Treasure, Source.Prosperity, Location.Kingdom, Group.CardOrdering | Group.PlusCoin)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Currency.Coin.Value = 2;
			this.Text = "<br/>While this is in play, when you gain a card, you may put that card on top of your deck.";
		}

		protected override Boolean AllowUndo { get { return true; } }

		public override void AddedTo(DeckLocation location, Player player)
		{
			base.AddedTo(location, player);

			if (location == DeckLocation.InPlay)
			{
				if (_CardGainedHandler != null)
					player.CardGained -= _CardGainedHandler;

				_CardGainedHandler = new Player.CardGainedEventHandler(player_CardGained);
				player.CardGained += _CardGainedHandler;
			}
		}

		void player_CardGained(object sender, Players.CardGainEventArgs e)
		{
			Player player = sender as Player;

			// Already been cancelled -- don't need to process this one
			if (e.Cancelled || e.Actions.ContainsKey(TypeClass.RoyalSeal))
				return;

			e.Actions[TypeClass.RoyalSeal] = new Players.CardGainAction(this.Owner, this, "Put it on top your deck", player_Action, false);
		}

		internal void player_Action(Player player, ref Players.CardGainEventArgs e)
		{
			e.Cancelled = true;
			e.Location = DeckLocation.Deck;
			e.Position = DeckPosition.Top;

			e.HandledBy.Add(TypeClass.RoyalSeal);
		}

		public override void RemovedFrom(DeckLocation location, Player player)
		{
			base.RemovedFrom(location, player);
			if (_CardGainedHandler != null)
				player.CardGained -= _CardGainedHandler;
			_CardGainedHandler = null;
		}
	}
	public class Talisman : Card
	{
		private Player.CardBoughtEventHandler _CardBoughtHandler = null;

		public Talisman()
			: base("Talisman", Category.Treasure, Source.Prosperity, Location.Kingdom, Group.Gain | Group.PlusCoin)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Currency.Coin.Value = 1;
			this.Text = "<br/>While this is in play, when you buy a card costing <coin>4</coin> or less that is not a Victory card, gain a copy of it.";
		}

		protected override Boolean AllowUndo { get { return true; } }

		public override void AddedTo(DeckLocation location, Player player)
		{
			base.AddedTo(location, player);

			if (location == DeckLocation.InPlay)
			{
				if (_CardBoughtHandler != null)
					player.CardBought -= _CardBoughtHandler;

				_CardBoughtHandler = new Player.CardBoughtEventHandler(player_CardBought);
				player.CardBought += _CardBoughtHandler;
			}
		}

		void player_CardBought(object sender, CardBuyEventArgs e)
		{
			// Already been cancelled -- don't need to process this one
			if (e.Cancelled)
				return;

			if (e.HandledBy.Contains(this) || e.Actions.ContainsKey(TypeClass.Talisman))
				return;

			if ((e.Card.Category & Category.Victory) != Category.Victory && e.Game.Table[e.Card].CurrentCost <= new Coin(4))
			{
				Player player = sender as Player;
				e.Actions[TypeClass.Talisman] = new Players.CardBuyAction(this.Owner, this, String.Format("Gain a copy of {0}", e.Card), player_GainFromTalisman, true);
			}
		}

		internal void player_GainFromTalisman(Player player, ref Players.CardBuyEventArgs e)
		{
			Supply supply = e.Game.Table[e.Card];
			if (supply != null && supply.CanGain() && supply.TopCard.Name == e.Card.Name)
				player.Gain(e.Game.Table[e.Card]);

			e.HandledBy.Add(this);
		}

		public override void RemovedFrom(DeckLocation location, Player player)
		{
			base.RemovedFrom(location, player);
			if (_CardBoughtHandler != null)
				player.CardBought -= _CardBoughtHandler;
			_CardBoughtHandler = null;
		}
	}
	public class TradeRoute : Card
	{
		public TradeRoute()
			: base("Trade Route", Category.Action, Source.Prosperity, Location.Kingdom, Group.Component | Group.DeckReduction | Group.PlusCoin | Group.PlusBuy | Group.Trash | Group.RemoveCurses | Group.Terminal | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(3);
			this.Benefit.Buys = 1;
			this.Text = "+<coin>1</coin> per token on the Trade Route mat.<nl/>Trash a card from your hand.<br/>Setup: Put a token on each Victory card Supply pile.  When a card is gained from that pile, move the token to the Trade Route mat.";
		}

		public override void Finalize(Game game, Supply supply)
		{
			base.Finalize(game, supply);
			foreach (Supply sVictory in game.Table.Supplies.Values)
			{
				if ((sVictory.Randomizer.Category & Category.Victory) == Category.Victory)
					sVictory.AddToken(new TradeRouteToken());
			}
			game.Table.TokenPiles[TypeClass.TradeRouteToken] = new TokenCollection();
		}

		public override void Play(Player player)
		{
			base.Play(player);

			CardBenefit benefit = new CardBenefit();
			benefit.Currency += new Coin(player._Game.Table.TokenPiles[TypeClass.TradeRouteToken].Count);
			player.ReceiveBenefit(this, benefit);

			Choice choice = new Choice("Trash a card.", this, player.Hand, player);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Cards.Count > 0)
				player.Trash(player.RetrieveCardFrom(DeckLocation.Hand, result.Cards[0]));
		}
	}
	public class TradeRouteToken : Token
	{
		public Boolean Placed = true;
		public Boolean Used = false;
		public TradeRouteToken()
			: base("T", "Trade Route token")
		{
		}
		public override Boolean Gaining()
		{
			if (!Used)
			{
				Used = true;
				return true;
			}
			return false;
		}
		public override string Title { get { return "Once a card is gained from this supply pile, this token will get added to the Trade Route Mat"; } }
	}
	public class Vault : Card
	{
		public Vault()
			: base("Vault", Category.Action, Source.Prosperity, Location.Kingdom, Group.PlusCard | Group.PlusCoin | Group.Discard | Group.AffectOthers | Group.Terminal | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Cards = 2;
			this.Text = "Discard any number of cards.  +<coin>1</coin> per card discarded.<nl/>Each other player may discard 2 cards.  If he does, he draws a card.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choice = new Choice("Discard any number of cards.  +<coin>1</coin> per card discarded.", this, player.Hand, player, false, 0, player.Hand.Count);
			ChoiceResult result = player.MakeChoice(choice);

			player.Discard(DeckLocation.Hand, result.Cards);

			CardBenefit benefit = new CardBenefit();
			benefit.Currency += new Coin(result.Cards.Count);
			player.ReceiveBenefit(this, benefit);

			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			enumerator.MoveNext(); // skip active player
			while (enumerator.MoveNext())
			{
				Player otherPlayer = enumerator.Current;
				if (otherPlayer.Hand.Count >= 2)
				{
					Choice choicePlayer = Choice.CreateYesNoChoice("Do you want to discard 2 cards to draw 1 card?", this, otherPlayer);
					ChoiceResult resultPlayer = otherPlayer.MakeChoice(choicePlayer);
					if (resultPlayer.Options[0] == "Yes")
					{
						Choice choiceDiscard = new Choice("Choose 2 cards to discard", this, otherPlayer.Hand, otherPlayer, false, 2, 2);
						ChoiceResult discards = otherPlayer.MakeChoice(choiceDiscard);
						otherPlayer.Discard(DeckLocation.Hand, discards.Cards);

						if (otherPlayer.CanDraw)
							otherPlayer.Draw(DeckLocation.Hand);
					}
				}
			}

			this.Benefit.Currency.Coin.Value = 0;
		}
	}
	public class Venture : Card
	{
		public Venture()
			: base("Venture", Category.Treasure, Source.Prosperity, Location.Kingdom, Group.PlusCard | Group.PlusCoin | Group.Discard)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Currency.Coin.Value = 1;
			this.Text = "When you play this, reveal cards from your deck until you reveal a Treasure.  Discard the other cards.<nl/>Play that Treasure.";
		}

		public override void Play(Player player)
		{
			base.Play(player);
			player.BeginDrawing();
			while (player.Revealed[Category.Treasure].Count < 1 && player.CanDraw)
				player.Draw(DeckLocation.Revealed);

			player.EndDrawing();

			CardCollection cards = player.Revealed[Cards.Category.Treasure];
			player.DiscardRevealed(c => !cards.Contains(c));

			if (cards.Count > 0)
				player.PlayCardInternal(cards[0]);
		}
	}
	public class VictoryToken : Token
	{
		public VictoryToken()
			: base("<vp/>", "Victory Point chit")
		{
		}
		public override string Title { get { return "Worth 1<vp/> at the end of the game"; } }
	}
	public class Watchtower : Card
	{
		private Player.CardGainedEventHandler _CardGainedHandler = null;

		public Watchtower()
			: base("Watchtower", 
			Category.Action | Category.Reaction, 
			Source.Prosperity, 
			Location.Kingdom,
			Group.ReactToGain | Group.Defense | Group.CardOrdering | Group.PlusCard | Group.Trash | Group.Terminal)
		{
			this.BaseCost = new Cost(3);
			this.Text = "Draw until you have 6 cards in hand.<br/>When you gain a card, you may reveal this from your hand.  If you do, either trash that card, or put it on top of your deck.";
		}

		public override void Play(Player player)
		{
			base.Play(player);
			player.Draw(6 - player.Hand.Count, DeckLocation.Hand);
		}

		public override void AddedTo(DeckLocation location, Player player)
		{
			base.AddedTo(location, player);

			if (location == DeckLocation.Hand)
			{
				if (_CardGainedHandler != null)
					player.CardGained -= _CardGainedHandler;

				_CardGainedHandler = new Player.CardGainedEventHandler(player_CardGained);
				player.CardGained += _CardGainedHandler;
			}
		}

		void player_CardGained(object sender, Players.CardGainEventArgs e)
		{
			Player player = sender as Player;

			// Already been cancelled -- don't need to process this one
			// If the card has been "lost track of", then we can skip revealing it
			// We also need to make sure we're in the player's hand and we can be revealed
			if (e.Cancelled || e.IsLostTrackOf || !player.Hand.Contains(this.PhysicalCard) || e.Actions.ContainsKey(TypeClass.Watchtower))
				return;

			e.Actions[TypeClass.Watchtower] = new Players.CardGainAction(this.Owner, this, String.Format("Reveal {0}", this.PhysicalCard), player_RevealWatchtower, false);
		}

		internal void player_RevealWatchtower(Player player, ref Players.CardGainEventArgs e)
		{
			player.AddCardInto(DeckLocation.Revealed, player.RetrieveCardFrom(DeckLocation.Hand, this.PhysicalCard));
			player.AddCardInto(DeckLocation.Hand, player.RetrieveCardFrom(DeckLocation.Revealed, this.PhysicalCard));

			Choice trashChoice = new Choice(String.Format("Trash {0} or put it on top of your deck?", e.Card), this, new CardCollection() { e.Card }, new List<String>() { "Trash", "Put on Deck" }, player);
			ChoiceResult trashResult = player.MakeChoice(trashChoice);
			if (trashResult.Options[0] == "Trash")
			{
				e.Cancelled = true;
				Card card = player.RetrieveCardFrom(e.Location, e.Card);
				if (card != null)
					player.Trash(e.Card);
				e.IsLostTrackOf = true;
			}
			else
			{
				e.Cancelled = true;
				if (e.Location != DeckLocation.Deck && e.Position != DeckPosition.Top)
				{
					Card c = player.RetrieveCardFrom(e.Location, e.Card);
					e.Location = DeckLocation.Deck;
					e.Position = DeckPosition.Top;
				}
			}

			e.HandledBy.Add(TypeClass.Watchtower);
		}

		public override void RemovedFrom(DeckLocation location, Player player)
		{
			base.RemovedFrom(location, player);
			if (_CardGainedHandler != null)
				player.CardGained -= _CardGainedHandler;
			_CardGainedHandler = null;
		}
	}
	public class WorkersVillage : Card
	{
		public WorkersVillage()
			: base("Worker's Village", Category.Action, Source.Prosperity, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.PlusMultipleActions | Group.PlusBuy)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Actions = 2;
			this.Benefit.Cards = 1;
			this.Benefit.Buys = 1;
		}
	}
}
