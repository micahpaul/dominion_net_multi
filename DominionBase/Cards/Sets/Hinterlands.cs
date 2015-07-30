using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using DominionBase.Currencies;
using DominionBase.Piles;
using DominionBase.Players;

namespace DominionBase.Cards.Hinterlands
{
	public static class TypeClass
	{
		public static Type BorderVillage = typeof(BorderVillage);
		public static Type Cache = typeof(Cache);
		public static Type Cartographer = typeof(Cartographer);
		public static Type Crossroads = typeof(Crossroads);
		public static Type Develop = typeof(Develop);
		public static Type Duchess = typeof(Duchess);
		public static Type Embassy = typeof(Embassy);
		public static Type Farmland = typeof(Farmland);
		public static Type FoolsGold = typeof(FoolsGold);
		public static Type Haggler = typeof(Haggler);
		public static Type Highway = typeof(Highway);
		public static Type IllGottenGains = typeof(IllGottenGains);
		public static Type Inn = typeof(Inn);
		public static Type JackOfAllTrades = typeof(JackOfAllTrades);
		public static Type Mandarin = typeof(Mandarin);
		public static Type Margrave = typeof(Margrave);
		public static Type NobleBrigand = typeof(NobleBrigand);
		public static Type NomadCamp = typeof(NomadCamp);
		public static Type Oasis = typeof(Oasis);
		public static Type Oracle = typeof(Oracle);
		public static Type Scheme = typeof(Scheme);
		public static Type SilkRoad = typeof(SilkRoad);
		public static Type SpiceMerchant = typeof(SpiceMerchant);
		public static Type Stables = typeof(Stables);
		public static Type Trader = typeof(Trader);
		public static Type Tunnel = typeof(Tunnel);
	}

	public class BorderVillage : Card
	{
		private Dictionary<Player, Player.CardGainedEventHandler> _CardGainedHandlers = new Dictionary<Player, Player.CardGainedEventHandler>();

		public BorderVillage()
			: base("Border Village", Category.Action, Source.Hinterlands, Location.Kingdom, Group.PlusAction | Group.PlusMultipleActions | Group.Gain)
		{
			this.BaseCost = new Cost(6);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 2;
			this.Text = "<br/>When you gain this, gain a card costing less than this.";
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

		internal override void TrashedBy(Player player)
		{
			base.TrashedBy(player);

			// Need to reset any Gain triggers when we're trashed -- we can technically be gained from the Trash
			ResetTriggers(player._Game);
		}

		private void ResetTriggers(Game game)
		{
			IEnumerator<Player> enumPlayers = game.GetPlayersStartingWithActiveEnumerator();
			while (enumPlayers.MoveNext())
			{
				_CardGainedHandlers[enumPlayers.Current] = new Player.CardGainedEventHandler(player_CardGained);
				enumPlayers.Current.CardGained += _CardGainedHandlers[enumPlayers.Current];
			}
		}

		void player_CardGained(object sender, Players.CardGainEventArgs e)
		{
			// This is not the card you are looking for
			if (e.Card != this || e.Actions.ContainsKey(TypeClass.BorderVillage))
				return;

			Player player = sender as Player;
			e.Actions[TypeClass.BorderVillage] = new Players.CardGainAction(this.Owner, this, "Gain a card costing less", player_GainBorderVillage, true);
		}

		internal void player_GainBorderVillage(Player player, ref Players.CardGainEventArgs e)
		{
			Cost myCost = e.Game.ComputeCost(this);
			SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply => supply.CanGain() && supply.CurrentCost < myCost);
			Choice choice = new Choice(String.Format("Gain a card costing less than {0}", this.Name), this, gainableSupplies, player, false);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Supply != null)
				player.Gain(result.Supply);

			e.HandledBy.Add(TypeClass.BorderVillage);

			// Clear out the Event Triggers -- this only happens when its Gained, so we don't care any more
			foreach (Player playerLoop in _CardGainedHandlers.Keys)
				playerLoop.CardGained -= _CardGainedHandlers[playerLoop];
			_CardGainedHandlers.Clear();
		}
	}
	public class Cache : Card
	{
		private Dictionary<Player, Player.CardGainedEventHandler> _CardGainedHandlers = new Dictionary<Player, Player.CardGainedEventHandler>();

		public Cache()
			: base("Cache", Category.Treasure, Source.Hinterlands, Location.Kingdom, Group.Gain | Group.PlusCoin)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Currency.Coin.Value = 3;
			this.Text = "<br/>When you gain this, gain two Coppers.";
		}

		protected override Boolean AllowUndo { get { return true; } }

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

		internal override void TrashedBy(Player player)
		{
			base.TrashedBy(player);

			// Need to reset any Gain triggers when we're trashed -- we can technically be gained from the Trash
			ResetTriggers(player._Game);
		}

		private void ResetTriggers(Game game)
		{
			IEnumerator<Player> enumPlayers = game.GetPlayersStartingWithActiveEnumerator();
			while (enumPlayers.MoveNext())
			{
				_CardGainedHandlers[enumPlayers.Current] = new Player.CardGainedEventHandler(player_CardGained);
				enumPlayers.Current.CardGained += _CardGainedHandlers[enumPlayers.Current];
			}
		}

		void player_CardGained(object sender, Players.CardGainEventArgs e)
		{
			// This is not the card you are looking for
			if (e.Card != this || e.Actions.ContainsKey(TypeClass.Cache) || !e.Game.Table.Copper.CanGain())
				return;

			Player player = sender as Player;
			e.Actions[TypeClass.Cache] = new Players.CardGainAction(this.Owner, this, "Gain 2 Coppers", player_GainCache, true);
		}

		internal void player_GainCache(Player player, ref Players.CardGainEventArgs e)
		{
			player.Gain(player._Game.Table.Copper, 2);

			e.HandledBy.Add(TypeClass.Cache);

			// Clear out the Event Triggers -- this only happens when its Gained, so we don't care any more
			foreach (Player playerLoop in _CardGainedHandlers.Keys)
				playerLoop.CardGained -= _CardGainedHandlers[playerLoop];
			_CardGainedHandlers.Clear();
		}
	}
	public class Cartographer : Card
	{
		public Cartographer()
			: base("Cartographer", Category.Action, Source.Hinterlands, Location.Kingdom, Group.CardOrdering | Group.PlusCard | Group.PlusAction | Group.Discard)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 1;
			this.Text = "Look at the top 4 cards of your deck.  Discard any number of them.  Put the rest back on top in any order.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			player.Draw(4, DeckLocation.Private);

			Choice choiceDiscard = new Choice("Choose cards to discard", this, player.Private, player, false, 0, player.Private.Count);
			ChoiceResult resultDiscard = player.MakeChoice(choiceDiscard);
			player.Discard(DeckLocation.Private, resultDiscard.Cards);

			Choice replaceChoice = new Choice("Choose order of cards to put back on your deck", this, player.Private, player, true, player.Private.Count, player.Private.Count);
			ChoiceResult replaceResult = player.MakeChoice(replaceChoice);
			player.AddCardsToDeck(player.RetrieveCardsFrom(DeckLocation.Private, replaceResult.Cards), DeckPosition.Top);
		}
	}
	public class Crossroads : Card
	{
		public Crossroads()
			: base("Crossroads", Category.Action, Source.Hinterlands, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.PlusMultipleActions | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(2);
			this.Text = "Reveal your hand.  +1 Card per Victory card revealed.  If this is the first time you played a Crossroads this turn, +3 Actions.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			player.ReturnHand(player.RevealHand());

			int plusActions = 0;
			if (player.CurrentTurn.CardsResolved.Count(c => c.LogicalCard.CardType == TypeClass.Crossroads) == 1)
				plusActions = 3;
			player.ReceiveBenefit(this, new CardBenefit() { Cards = player.Hand[Cards.Category.Victory].Count, Actions = plusActions });
		}
	}
	public class Develop : Card
	{
		public Develop()
			: base("Develop", Category.Action, Source.Hinterlands, Location.Kingdom, Group.DeckReduction | Group.Gain | Group.Trash | Group.CardOrdering | Group.RemoveCurses | Group.Terminal)
		{
			this.BaseCost = new Cost(3);
			this.Text = "Trash a card from your hand.  Gain a card costing exactly <coin>1</coin> more than it and a card costing exactly <coin>1</coin> less than it, in either order, putting them on top of your deck.";
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

				SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply =>
					supply.CanGain() &&
					(supply.CurrentCost == (trashedCardCost + new Coin(1)) ||
						trashedCardCost.Coin > 0 && supply.CurrentCost == (trashedCardCost - new Coin(1))));

				Choice choice = new Choice("Gain a card to put on your deck", this, gainableSupplies, player, false);
				ChoiceResult result = player.MakeChoice(choice);
				if (result.Supply != null)
				{
					player.Gain(result.Supply, DeckLocation.Deck, DeckPosition.Top);

					if (!(result.Supply.CurrentCost <= trashedCardCost))
						gainableSupplies = player._Game.Table.Supplies.FindAll(supply => supply.CanGain() && trashedCardCost.Coin > 0 &&
							supply.CurrentCost == (trashedCardCost - new Coin(1)));
					else
						gainableSupplies = player._Game.Table.Supplies.FindAll(supply => supply.CanGain() && supply.CurrentCost == (trashedCardCost + new Coin(1)));

					choice = new Choice("Gain a card to put on your deck", this, gainableSupplies, player, false);
					result = player.MakeChoice(choice);
					if (result.Supply != null)
						player.Gain(result.Supply, DeckLocation.Deck, DeckPosition.Top);
				}
			}
		}
	}
	public class Duchess : Card
	{
		private Dictionary<Player, Player.CardGainedEventHandler> _CardGainedHandlers = new Dictionary<Player, Player.CardGainedEventHandler>();

		public Duchess()
			: base("Duchess", Category.Action, Source.Hinterlands, Location.Kingdom, Group.CardOrdering | Group.PlusCoin | Group.Gain | Group.AffectOthers | Group.Terminal)
		{
			this.BaseCost = new Cost(2);
			this.Benefit.Currency.Coin.Value = 2;
			this.Text = "Each player (including you) looks at the top card of his deck, and discards it or puts it back.<br/>In games using this, when you gain a Duchy, you may gain a Duchess.";
		}

		internal override void TearDown()
		{
			base.TearDown();

			foreach (Player playerLoop in _CardGainedHandlers.Keys)
				playerLoop.CardGained -= _CardGainedHandlers[playerLoop];
			_CardGainedHandlers.Clear();
		}

		public override void Play(Player player)
		{
			base.Play(player);

			// Perform action on every player
			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			while (enumerator.MoveNext())
			{
				Player actor = enumerator.Current;

				if (actor.CanDraw)
				{
					Card card = actor.Draw(DeckLocation.Private);
					Choice choice = new Choice(String.Format("Do you want to discard {0} or put it back on top?", card.Name), this, new CardCollection() { card }, new List<string>() { "Discard", "Put it back" }, actor);
					ChoiceResult result = actor.MakeChoice(choice);
					if (result.Options[0] == "Discard")
						actor.Discard(DeckLocation.Private);
					else
						actor.AddCardsToDeck(actor.RetrieveCardsFrom(DeckLocation.Private), DeckPosition.Top);
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
				_CardGainedHandlers[enumPlayers.Current] = new Player.CardGainedEventHandler(player_CardGained);
				enumPlayers.Current.CardGained += _CardGainedHandlers[enumPlayers.Current];
			}
		}

		void player_CardGained(object sender, Players.CardGainEventArgs e)
		{
			// Clear out the Event Triggers -- this only happens when a Duchy is Gained, so we don't care any more when the Duchess isn't on the Supply pile
			if (e.Card == this)
			{
				foreach (Player playerLoop in _CardGainedHandlers.Keys)
					playerLoop.CardGained -= _CardGainedHandlers[playerLoop];
				_CardGainedHandlers.Clear();
			}

			// This is not the card you are looking for
			if (e.Card.CardType != Cards.Universal.TypeClass.Duchy || e.Game.Table[TypeClass.Duchess].TopCard != this ||
				e.HandledBy.Contains(TypeClass.Duchess) || !e.Game.Table[TypeClass.Duchess].CanGain())
				return;

			Player player = sender as Player;
			e.Actions[TypeClass.Duchess] = new Players.CardGainAction(player, this, "Gain Duchess", player_GainDuchy, false);
		}

		internal void player_GainDuchy(Player player, ref Players.CardGainEventArgs e)
		{
			player.Gain(player._Game.Table.Supplies[TypeClass.Duchess]);

			e.HandledBy.Add(TypeClass.Duchess);
		}
	}
	public class Embassy : Card
	{
		private Dictionary<Player, Player.CardGainedEventHandler> _CardGainedHandlers = new Dictionary<Player, Player.CardGainedEventHandler>();

		public Embassy()
			: base("Embassy", Category.Action, Source.Hinterlands, Location.Kingdom, Group.PlusCard | Group.Gain | Group.Discard | Group.AffectOthers | Group.Terminal)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Cards = 5;
			this.Text = "Discard 3 cards.<br/>When you gain this, each other player gains a Silver.";
		}

		internal override void TearDown()
		{
			base.TearDown();

			foreach (Player playerLoop in _CardGainedHandlers.Keys)
				playerLoop.CardGained -= _CardGainedHandlers[playerLoop];
			_CardGainedHandlers.Clear();
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choice = new Choice("Discard 3 cards.", this, player.Hand, player, false, 3, 3);
			ChoiceResult result = player.MakeChoice(choice);
			player.Discard(DeckLocation.Hand, result.Cards);
		}

		public override void AddedToSupply(Game game, Supply supply)
		{
			base.AddedToSupply(game, supply);

			ResetTriggers(game);
		}

		internal override void TrashedBy(Player player)
		{
			base.TrashedBy(player);

			// Need to reset any Gain triggers when we're trashed -- we can technically be gained from the Trash
			ResetTriggers(player._Game);
		}

		private void ResetTriggers(Game game)
		{
			IEnumerator<Player> enumPlayers = game.GetPlayersStartingWithActiveEnumerator();
			while (enumPlayers.MoveNext())
			{
				_CardGainedHandlers[enumPlayers.Current] = new Player.CardGainedEventHandler(player_CardGained);
				enumPlayers.Current.CardGained += _CardGainedHandlers[enumPlayers.Current];
			}
		}

		void player_CardGained(object sender, Players.CardGainEventArgs e)
		{
			// This is not the card you are looking for
			if (e.Card != this || e.Actions.ContainsKey(TypeClass.Embassy) || !e.Game.Table.Silver.CanGain())
				return;

			Player player = sender as Player;
			e.Actions[TypeClass.Embassy] = new Players.CardGainAction(this.Owner, this, "other players gain Silvers", player_GainEmbassy, true);
		}

		internal void player_GainEmbassy(Player player, ref Players.CardGainEventArgs e)
		{
			// Skip current player (they don't get the Silver)
			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			enumerator.MoveNext();
			while (enumerator.MoveNext())
			{
				enumerator.Current.Gain(player._Game.Table.Silver);
			}

			e.HandledBy.Add(TypeClass.Embassy);

			// Clear out the Event Triggers -- this only happens when its Gained, so we don't care any more
			foreach (Player playerLoop in _CardGainedHandlers.Keys)
				playerLoop.CardGained -= _CardGainedHandlers[playerLoop];
			_CardGainedHandlers.Clear();
		}
	}
	public class Farmland : Card
	{
		private Dictionary<Player, Player.CardBoughtEventHandler> _CardBoughtHandlers = new Dictionary<Player, Player.CardBoughtEventHandler>();

		public Farmland()
			: base("Farmland", Category.Victory, Source.Hinterlands, Location.Kingdom, Group.Gain | Group.Trash | Group.DeckReduction | Group.RemoveCurses)
		{
			this.BaseCost = new Cost(6);
			this.VictoryPoints = 2;
			this.Text = "<br/>When you buy this, trash a card from your hand.<nl/>Gain a card costing exactly <coin>2</coin> more than the trashed card.";
		}

		internal override void TearDown()
		{
			base.TearDown();

			foreach (Player playerLoop in _CardBoughtHandlers.Keys)
				playerLoop.CardBought -= _CardBoughtHandlers[playerLoop];
			_CardBoughtHandlers.Clear();
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
			if (e.Card != this || e.Actions.ContainsKey(TypeClass.Farmland))
				return;

			Player player = sender as Player;
			if (player.Hand.Count > 0)
				e.Actions[TypeClass.Farmland] = new Players.CardBuyAction(this.Owner, this, "trash a card from your hand", player_BuyFarmland, true);
		}

		internal void player_BuyFarmland(Player player, ref Players.CardBuyEventArgs e)
		{
			Choice choiceTrash = new Choice("Choose a card to trash", this, player.Hand, player);
			ChoiceResult resultTrash = player.MakeChoice(choiceTrash);
			player.Trash(player.RetrieveCardsFrom(DeckLocation.Hand, resultTrash.Cards));

			if (resultTrash.Cards.Count > 0)
			{
				Cost trashedCardCost = player._Game.ComputeCost(resultTrash.Cards[0]);
				SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply => supply.CanGain() && supply.CurrentCost == (trashedCardCost + new Coin(2)));
				Choice choice = new Choice("Gain a card", this, gainableSupplies, player, false);
				ChoiceResult result = player.MakeChoice(choice);
				if (result.Supply != null)
					player.Gain(result.Supply);
			}

			e.HandledBy.Add(TypeClass.Farmland);

			// Clear out the Event Triggers -- this only happens when its Gained, so we don't care any more
			foreach (Player playerLoop in _CardBoughtHandlers.Keys)
				playerLoop.CardBought -= _CardBoughtHandlers[playerLoop];
			_CardBoughtHandlers.Clear();
		}
	}
	public class FoolsGold : Card
	{
		private Dictionary<Player, Player.CardGainedEventHandler> _CardGainedHandlers = new Dictionary<Player, Player.CardGainedEventHandler>();

		public FoolsGold()
			: base("Fool's Gold", Category.Treasure | Category.Reaction, Source.Hinterlands, Location.Kingdom,
				Group.PlusCoin | Group.ReactToGain | Group.Trash | Group.DeckReduction | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(2);
			this.Text = "If this is the first time you played a Fool's Gold this turn, this is worth <coin>1</coin>, otherwise it's worth <coin>4</coin>.<br/>When another player gains a Province, you may trash this from your hand.  If you do, gain a Gold, putting it on your deck.";
			this.Benefit.Currency.Coin.IsVariable = true;
		}

		internal override void TearDown()
		{
			base.TearDown();

			foreach (Player playerLoop in _CardGainedHandlers.Keys)
				playerLoop.CardGained -= _CardGainedHandlers[playerLoop];
			_CardGainedHandlers.Clear();
		}

		public override void Play(Player player)
		{
			base.Play(player);

			CardBenefit benefit = new CardBenefit();
			benefit.Currency += new Coin(1);
			if (player.CurrentTurn.CardsResolved.Count(c => c.CardType == TypeClass.FoolsGold) > 1)
				benefit.Currency += new Coin(3);

			player.ReceiveBenefit(this, benefit);
		}

		public override void AddedTo(DeckLocation location, Player player)
		{
			base.AddedTo(location, player);

			if (location == DeckLocation.Hand)
			{
				IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
				enumerator.MoveNext();
				while (enumerator.MoveNext())
				{
					if (_CardGainedHandlers.ContainsKey(enumerator.Current) && _CardGainedHandlers[enumerator.Current] != null)
						enumerator.Current.CardGained -= _CardGainedHandlers[enumerator.Current];

					_CardGainedHandlers[enumerator.Current] = new Player.CardGainedEventHandler(player_CardGained);
					enumerator.Current.CardGained += _CardGainedHandlers[enumerator.Current];
				}
			}
		}

		void player_CardGained(object sender, Players.CardGainEventArgs e)
		{
			Player player = sender as Player;

			// Already been cancelled -- don't need to process this one
			if (e.Cancelled || e.Actions.ContainsKey(TypeClass.FoolsGold) || player == this.Owner || e.Card.CardType != Cards.Universal.TypeClass.Province)
				return;

			e.Actions[TypeClass.FoolsGold] = new Players.CardGainAction(this.Owner, this, String.Format("Trash {0}", this.PhysicalCard), player_FoolsGold, false);
		}

		internal void player_FoolsGold(Player player, ref Players.CardGainEventArgs e)
		{
			if (player.Hand.Contains(this.PhysicalCard))
			{
				player.AddCardInto(DeckLocation.Revealed, player.RetrieveCardFrom(DeckLocation.Hand, this.PhysicalCard));
				player.Trash(player.RetrieveCardFrom(DeckLocation.Revealed, this.PhysicalCard));
				player.Gain(player._Game.Table.Gold, DeckLocation.Deck, DeckPosition.Top);
				e.HandledBy.Add(TypeClass.FoolsGold);
			}
		}

		public override void RemovedFrom(DeckLocation location, Player player)
		{
			base.RemovedFrom(location, player);

			if (_CardGainedHandlers.Count > 0)
			{
				foreach (Player otherPlayer in _CardGainedHandlers.Keys)
					otherPlayer.CardGained -= _CardGainedHandlers[otherPlayer];
			}

			_CardGainedHandlers.Clear();
		}
	}
	public class Haggler : Card
	{
		private Player.CardBoughtEventHandler _CardBoughtHandler = null;

		public Haggler()
			: base("Haggler", Category.Action, Source.Hinterlands, Location.Kingdom, Group.PlusCoin | Group.Gain | Group.Terminal)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Currency.Coin.Value = 2;
			this.Text = "<br/>While this is in play, when you buy a card, gain a card costing less than it that is not a Victory card.";
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

		void player_CardBought(object sender, Players.CardBuyEventArgs e)
		{
			// This is not the card you are looking for
			if (e.HandledBy.Contains(this) || e.Actions.ContainsKey(TypeClass.Haggler) || e.Card.BaseCost == new Cost())
				return;

			Player player = sender as Player;
			e.Actions[TypeClass.Haggler] = new Players.CardBuyAction(this.Owner, this, "gain a non-Victory card costing less", player_BuyWithHaggler, true);
		}

		internal void player_BuyWithHaggler(Player player, ref Players.CardBuyEventArgs e)
		{
			Cost costCard = e.Game.ComputeCost(e.Card);
			SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply =>
				supply.CanGain() &&
				((supply.Category & Cards.Category.Victory) != Cards.Category.Victory) &&
				supply.CurrentCost < costCard);
			Choice choice = new Choice(String.Format("Gain a non-Victory card costing less than {0}", e.Card.Name), this, gainableSupplies, player, false);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Supply != null)
				player.Gain(result.Supply);

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
	public class Highway : Card
	{
		private Game.CostComputeEventHandler _CostComputeEventHandler = null;

		public Highway()
			: base("Highway", Category.Action, Source.Hinterlands, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.ModifyCost)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 1;
			this.Text = "<br/>While this is in play, cards cost <coin>1</coin> less, but not less than <coin>0</coin>.";
		}

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

				_CostComputeEventHandler = new Game.CostComputeEventHandler(player_HighwayInPlayArea);
				player._Game.CostCompute += _CostComputeEventHandler;
				player._Game.SendMessage(player, this, 1);
			}
		}

		void player_HighwayInPlayArea(object sender, CostComputeEventArgs e)
		{
			e.Cost.Coin -= 1;
		}

		public override void RemovedFrom(DeckLocation location, Player player)
		{
			base.RemovedFrom(location, player);
			if (_CostComputeEventHandler != null)
				player._Game.CostCompute -= _CostComputeEventHandler;
			_CostComputeEventHandler = null;
		}
	}
	public class IllGottenGains : Card
	{
		private Dictionary<Player, Player.CardGainedEventHandler> _CardGainedHandlers = new Dictionary<Player, Player.CardGainedEventHandler>();

		public IllGottenGains()
			: base("Ill-Gotten Gains", Category.Treasure, Source.Hinterlands, Location.Kingdom, Group.Gain | Group.PlusCurses | Group.PlusCoin | Group.AffectOthers)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Currency.Coin.Value = 1;
			this.Text = "When you play this, you may gain a Copper, putting it into your hand.<br/>When you gain this, each other player gains a Curse.";
		}

		internal override void TearDown()
		{
			base.TearDown();

			foreach (Player playerLoop in _CardGainedHandlers.Keys)
				playerLoop.CardGained -= _CardGainedHandlers[playerLoop];
			_CardGainedHandlers.Clear();
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choicePlayer = Choice.CreateYesNoChoice("Do you want to gain a Copper into your hand?", this, player);
			ChoiceResult resultPlayer = player.MakeChoice(choicePlayer);
			if (resultPlayer.Options[0] == "Yes")
			{
				player.Gain(player._Game.Table.Copper, DeckLocation.Hand, DeckPosition.Bottom);
			}
		}

		public override void AddedToSupply(Game game, Supply supply)
		{
			base.AddedToSupply(game, supply);

			ResetTriggers(game);
		}

		internal override void TrashedBy(Player player)
		{
			base.TrashedBy(player);

			// Need to reset any Gain triggers when we're trashed -- we can technically be gained from the Trash
			ResetTriggers(player._Game);
		}

		private void ResetTriggers(Game game)
		{
			IEnumerator<Player> enumPlayers = game.GetPlayersStartingWithActiveEnumerator();
			while (enumPlayers.MoveNext())
			{
				_CardGainedHandlers[enumPlayers.Current] = new Player.CardGainedEventHandler(player_CardGained);
				enumPlayers.Current.CardGained += _CardGainedHandlers[enumPlayers.Current];
			}
		}

		void player_CardGained(object sender, Players.CardGainEventArgs e)
		{
			Player player = sender as Player;

			// This is not the card you are looking for
			if (e.Card != this || e.Actions.ContainsKey(TypeClass.IllGottenGains) || !e.Game.Table.Curse.CanGain())
				return;

			e.Actions[TypeClass.IllGottenGains] = new Players.CardGainAction(this.Owner, this, "other players gain Curses", player_GainIllGottenGains, true);
		}

		internal void player_GainIllGottenGains(Player player, ref Players.CardGainEventArgs e)
		{
			// Skip current player (they don't get the Curse)
			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			enumerator.MoveNext();
			while (enumerator.MoveNext())
			{
				enumerator.Current.Gain(player._Game.Table.Curse);
			}

			e.HandledBy.Add(TypeClass.IllGottenGains);

			// Clear out the Event Triggers -- this only happens when its Gained, so we don't care any more
			foreach (Player playerLoop in _CardGainedHandlers.Keys)
				playerLoop.CardGained -= _CardGainedHandlers[playerLoop];
			_CardGainedHandlers.Clear();
		}
	}
	public class Inn : Card
	{
		private Dictionary<Player, Player.CardGainedEventHandler> _CardGainedHandlers = new Dictionary<Player, Player.CardGainedEventHandler>();

		public Inn()
			: base("Inn", Category.Action, Source.Hinterlands, Location.Kingdom, Group.PlusAction | Group.PlusMultipleActions | Group.PlusCard | Group.CardOrdering | Group.Discard)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Cards = 2;
			this.Benefit.Actions = 2;
			this.Text = "Discard 2 cards.<br/>When you gain this, look through your discard pile (including this), reveal any number of Action cards from it, and shuffle them into your deck.";
		}

		internal override void TearDown()
		{
			base.TearDown();

			foreach (Player playerLoop in _CardGainedHandlers.Keys)
				playerLoop.CardGained -= _CardGainedHandlers[playerLoop];
			_CardGainedHandlers.Clear();
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choice = new Choice("Discard 2 cards.", this, player.Hand, player, false, 2, 2);
			ChoiceResult result = player.MakeChoice(choice);
			player.Discard(DeckLocation.Hand, result.Cards);
		}

		public override void AddedToSupply(Game game, Supply supply)
		{
			base.AddedToSupply(game, supply);

			ResetTriggers(game);
		}

		internal override void TrashedBy(Player player)
		{
			base.TrashedBy(player);

			// Need to reset any Gain triggers when we're trashed -- we can technically be gained from the Trash
			ResetTriggers(player._Game);
		}

		private void ResetTriggers(Game game)
		{
			IEnumerator<Player> enumPlayers = game.GetPlayersStartingWithActiveEnumerator();
			while (enumPlayers.MoveNext())
			{
				_CardGainedHandlers[enumPlayers.Current] = new Player.CardGainedEventHandler(player_CardGained);
				enumPlayers.Current.CardGained += _CardGainedHandlers[enumPlayers.Current];
			}
		}

		void player_CardGained(object sender, Players.CardGainEventArgs e)
		{
			Player player = sender as Player;

			// This is not the card you are looking for
			if (e.Card != this || e.Actions.ContainsKey(TypeClass.Inn))
				return;

			e.Actions[TypeClass.Inn] = new Players.CardGainAction(this.Owner, this, "Resolve Inn", player_GainInn, true);
		}

		internal void player_GainInn(Player player, ref Players.CardGainEventArgs e)
		{
			CardCollection actionCards = player.DiscardPile.LookThrough(c => (c.Category & Cards.Category.Action) == Cards.Category.Action);
			Choice choice = new Choice("Choose cards to reveal and shuffle into your deck", this, actionCards, player, false, 0, actionCards.Count);
			ChoiceResult result = player.MakeChoice(choice);

			// We lose track of Inn if we put it on top of the deck and shuffle
			if (result.Cards.Contains(this))
				e.IsLostTrackOf = true;

			player.AddCardsInto(DeckLocation.Revealed, player.DiscardPile.Retrieve(player, c => result.Cards.Contains(c)));
			player.AddCardsToDeck(player.Revealed.Retrieve(player, c => result.Cards.Contains(c)), DeckPosition.Top);
			player.ShuffleDrawPile();

			e.HandledBy.Add(TypeClass.Inn);

			// Clear out the Event Triggers -- this only happens when its Gained, so we don't care any more
			foreach (Player playerLoop in _CardGainedHandlers.Keys)
				playerLoop.CardGained -= _CardGainedHandlers[playerLoop];
			_CardGainedHandlers.Clear();
		}
	}
	public class JackOfAllTrades : Card
	{
		public JackOfAllTrades()
			: base("Jack of all Trades", Category.Action, Source.Hinterlands, Location.Kingdom, Group.PlusCard | Group.CardOrdering | Group.Discard | Group.Gain | Group.Trash | Group.RemoveCurses | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.Text = "Gain a Silver.<nl/><nl/>Look at the top card of your deck; discard it or put it back.<nl/><nl/>Draw until you have 5 cards in hand.<nl/><nl/>You may trash a card from your hand that is not a Treasure.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			// Step 1
			player.Gain(player._Game.Table.Silver);

			// Step 2
			if (player.CanDraw)
			{
				Card card = player.Draw(DeckLocation.Private);
				Choice choice = new Choice(String.Format("Do you want to discard {0} or put it back on top?", card.Name), this, new CardCollection() { card }, new List<string>() { "Discard", "Put it back" }, player);
				ChoiceResult result = player.MakeChoice(choice);
				if (result.Options[0] == "Discard")
					player.Discard(DeckLocation.Private);
				else
					player.AddCardsToDeck(player.RetrieveCardsFrom(DeckLocation.Private), DeckPosition.Top);
			}

			// Step 3
			player.Draw(5 - player.Hand.Count, DeckLocation.Hand);

			// Step 4
			Choice choiceTrash = new Choice("You may trash a card", this, player.Hand[c => (c.Category & Category.Treasure) != Category.Treasure], player, false, 0, 1);
			ChoiceResult resultTrash = player.MakeChoice(choiceTrash);
			player.Trash(player.RetrieveCardsFrom(DeckLocation.Hand, resultTrash.Cards));

		}

	}
	public class Mandarin : Card
	{
		private Dictionary<Player, Player.CardGainedEventHandler> _CardGainedHandlers = new Dictionary<Player, Player.CardGainedEventHandler>();

		public Mandarin()
			: base("Mandarin", Category.Action, Source.Hinterlands, Location.Kingdom, Group.PlusCoin | Group.CardOrdering | Group.Terminal)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Currency.Coin.Value = 3;
			this.Text = "Put a card from your hand on top of your deck.<br/>When you gain this, put all Treasures you have in play on top of your deck in any order.";
		}

		internal override void TearDown()
		{
			base.TearDown();

			foreach (Player playerLoop in _CardGainedHandlers.Keys)
				playerLoop.CardGained -= _CardGainedHandlers[playerLoop];
			_CardGainedHandlers.Clear();
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice replaceChoice = new Choice("Choose a card to put back on your deck", this, player.Hand, player, false, 1, 1);
			ChoiceResult replaceResult = player.MakeChoice(replaceChoice);
			player.RetrieveCardsFrom(DeckLocation.Hand, replaceResult.Cards);
			player.AddCardsToDeck(replaceResult.Cards, DeckPosition.Top);
		}

		public override void AddedToSupply(Game game, Supply supply)
		{
			base.AddedToSupply(game, supply);

			ResetTriggers(game);
		}

		internal override void TrashedBy(Player player)
		{
			base.TrashedBy(player);

			// Need to reset any Gain triggers when we're trashed -- we can technically be gained from the Trash
			ResetTriggers(player._Game);
		}

		private void ResetTriggers(Game game)
		{
			IEnumerator<Player> enumPlayers = game.GetPlayersStartingWithActiveEnumerator();
			while (enumPlayers.MoveNext())
			{
				_CardGainedHandlers[enumPlayers.Current] = new Player.CardGainedEventHandler(player_CardGained);
				enumPlayers.Current.CardGained += _CardGainedHandlers[enumPlayers.Current];
			}
		}

		void player_CardGained(object sender, Players.CardGainEventArgs e)
		{
			Player player = sender as Player;

			// This is not the card you are looking for
			if (e.Card != this || e.Actions.ContainsKey(TypeClass.Mandarin) || player.InPlay[Cards.Category.Treasure].Count == 0)
				return;

			e.Actions[TypeClass.Mandarin] = new Players.CardGainAction(this.Owner, this, "put all Treasures on your deck", player_GainMandarin, true);
		}

		internal void player_GainMandarin(Player player, ref Players.CardGainEventArgs e)
		{
			CardCollection cardsInPlay = new CardCollection(player.InPlay[Cards.Category.Treasure]);
			cardsInPlay.AddRange(player.SetAside[Cards.Category.Treasure]);
			Choice replaceChoice = new Choice("Choose order of Treasure cards to put back on your deck", this, cardsInPlay, player, true, cardsInPlay.Count, cardsInPlay.Count);
			ChoiceResult replaceResult = player.MakeChoice(replaceChoice);

			player.RetrieveCardsFrom(DeckLocation.InPlay, c => replaceResult.Cards.Contains(c));
			player.RetrieveCardsFrom(DeckLocation.SetAside, c => replaceResult.Cards.Contains(c));

			player.AddCardsToDeck(replaceResult.Cards, DeckPosition.Top);

			e.HandledBy.Add(TypeClass.Mandarin);

			// Clear out the Event Triggers -- this only happens when its Gained, so we don't care any more
			foreach (Player playerLoop in _CardGainedHandlers.Keys)
				playerLoop.CardGained -= _CardGainedHandlers[playerLoop];
			_CardGainedHandlers.Clear();
		}
	}
	public class Margrave : Card
	{
		public Margrave()
			: base("Margrave", Category.Action | Category.Attack, Source.Hinterlands, Location.Kingdom, Group.PlusCard | Group.PlusBuy | Group.Discard | Group.Terminal)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Cards = 3;
			this.Benefit.Buys = 1;
			this.Text = "Each other player draws a card, then discards down to 3 cards in hand.";
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

				if (attackee.CanDraw)
					attackee.Draw(DeckLocation.Hand);
				Choice choice = new Choice("Choose cards to discard.  You must discard down to 3 cards in hand", this, attackee.Hand, attackee, false, attackee.Hand.Count - 3, attackee.Hand.Count - 3);
				ChoiceResult result = attackee.MakeChoice(choice);
				attackee.Discard(DeckLocation.Hand, result.Cards);
			}
		}
	}
	public class NobleBrigand : Card
	{
		private Dictionary<Player, Player.CardBoughtEventHandler> _CardBoughtHandlers = new Dictionary<Player, Player.CardBoughtEventHandler>();

		public NobleBrigand()
			: base("Noble Brigand", Category.Action | Category.Attack, Source.Hinterlands, Location.Kingdom, Group.PlusCoin | Group.Gain | Group.Trash | Group.Discard | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Currency.Coin.Value = 1;
			this.Text = "When you buy this or play it, each other player reveals the top 2 cards of his deck, trashes a revealed Silver or Gold you choose, and discards the rest.  If he didn't reveal a Treasure, he gains a Copper.  You gain the trashed cards.";
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

			CardEffects(player);
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
			if (e.Card != this || e.Actions.ContainsKey(TypeClass.NobleBrigand))
				return;

			Player player = sender as Player;
			e.Actions[TypeClass.NobleBrigand] = new Players.CardBuyAction(this.Owner, this, String.Format("perform {0} attack", this.PhysicalCard.Name), player_BuyNobleBrigand, true);
		}

		internal void player_BuyNobleBrigand(Player player, ref Players.CardBuyEventArgs e)
		{
			CardEffects(player);

			e.HandledBy.Add(TypeClass.NobleBrigand);

			// Clear out the Event Triggers -- this only happens when its Gained, so we don't care any more
			foreach (Player playerLoop in _CardBoughtHandlers.Keys)
				playerLoop.CardBought -= _CardBoughtHandlers[playerLoop];
			_CardBoughtHandlers.Clear();
		}

		private void CardEffects(Player player)
		{
			// Perform attack on every player
			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			enumerator.MoveNext();
			CardCollection trashed = new CardCollection();
			while (enumerator.MoveNext())
			{
				Player attackee = enumerator.Current;
				// Skip if the attack is blocked (Moat, Lighthouse, etc.)
				// The on-buy Attack can't be blocked, so make sure the player is in the dictionary first
				if (this.IsAttackBlocked.ContainsKey(attackee) && this.IsAttackBlocked[attackee])
					continue;

				attackee.Draw(2, DeckLocation.Revealed);

				Boolean gainCopper = false;
				if (attackee.Revealed[Cards.Category.Treasure].Count == 0)
					gainCopper = true;

				CardCollection treasuresSilverGold = attackee.Revealed[c => c.CardType == Universal.TypeClass.Silver || c.CardType == Universal.TypeClass.Gold];

				Choice choice = new Choice(String.Format("Choose a Treasure card of {0} to trash", attackee), this, treasuresSilverGold, attackee);
				ChoiceResult result = player.MakeChoice(choice);
				if (result.Cards.Count > 0)
				{
					Card trashCard = attackee.RetrieveCardFrom(DeckLocation.Revealed, result.Cards[0]);
					attackee.Trash(trashCard);
					trashed.Add(trashCard);
				}

				attackee.DiscardRevealed();

				if (gainCopper)
					attackee.Gain(player._Game.Table.Copper);
			}

			// Gain all trashed Silver & Golds
			foreach (Card card in trashed)
				player.Gain(player._Game.Table.Trash, card);
		}
	}
	public class NomadCamp : Card
	{
		private Dictionary<Player, Player.CardGainedEventHandler> _CardGainedHandlers = new Dictionary<Player, Player.CardGainedEventHandler>();

		public NomadCamp()
			: base("Nomad Camp", Category.Action, Source.Hinterlands, Location.Kingdom, Group.PlusBuy | Group.PlusCoin | Group.CardOrdering | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Buys = 1;
			this.Benefit.Currency.Coin.Value = 2;
			this.Text = "<br/>When you gain this, put it on top of your deck.";
		}

		protected override Boolean AllowUndo { get { return true; } }

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

		internal override void TrashedBy(Player player)
		{
			base.TrashedBy(player);

			// Need to reset any Gain triggers when we're trashed -- we can technically be gained from the Trash
			ResetTriggers(player._Game);
		}

		private void ResetTriggers(Game game)
		{
			IEnumerator<Player> enumPlayers = game.GetPlayersStartingWithActiveEnumerator();
			while (enumPlayers.MoveNext())
			{
				_CardGainedHandlers[enumPlayers.Current] = new Player.CardGainedEventHandler(player_CardGained);
				enumPlayers.Current.CardGained += _CardGainedHandlers[enumPlayers.Current];
			}
		}

		void player_CardGained(object sender, Players.CardGainEventArgs e)
		{
			Player player = sender as Player;

			// This is not the card you are looking for
			if (e.Card != this || e.Actions.ContainsKey(TypeClass.NomadCamp))
				return;

			e.Actions[TypeClass.NomadCamp] = new Players.CardGainAction(this.Owner, this, String.Format("put {0} on your deck", this.PhysicalCard), player_GainNomadCamp, true);
		}

		internal void player_GainNomadCamp(Player player, ref Players.CardGainEventArgs e)
		{
			e.Cancelled = true;
			e.Location = DeckLocation.Deck;
			e.Position = DeckPosition.Top;

			e.HandledBy.Add(TypeClass.NomadCamp);

			// Clear out the Event Triggers -- this only happens when its Gained, so we don't care any more
			foreach (Player playerLoop in _CardGainedHandlers.Keys)
				playerLoop.CardGained -= _CardGainedHandlers[playerLoop];
			_CardGainedHandlers.Clear();
		}
	}
	public class Oasis : Card
	{
		public Oasis()
			: base("Oasis", Category.Action, Source.Hinterlands, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.PlusCoin | Group.Discard)
		{
			this.BaseCost = new Cost(3);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 1;
			this.Benefit.Currency.Coin.Value = 1;
			this.Text = "Discard a card.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choice = new Choice("Discard a card.", this, player.Hand, player, false, 1, 1);
			ChoiceResult result = player.MakeChoice(choice);
			player.Discard(DeckLocation.Hand, result.Cards);
		}

	}
	public class Oracle : Card
	{
		public Oracle()
			: base("Oracle", Category.Action | Category.Attack, Source.Hinterlands, Location.Kingdom, Group.CardOrdering | Group.PlusCard | Group.Discard | Group.Terminal)
		{
			this.BaseCost = new Cost(3);
			this.Benefit.Cards = 2;
			this.Text = "Each player (including you) reveals the top 2 cards of his deck, and you choose one: he discards them, or he puts them back on top in an order he chooses.<nl/><nl/><benefit/>";
		}

		public override void Play(Player player)
		{
			base.PlaySetup(player);

			// Perform attack on every player (including you)
			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			while (enumerator.MoveNext())
			{
				Player attackee = enumerator.Current;
				// Skip if the attack is blocked (Moat, Lighthouse, etc.)
				if (this.IsAttackBlocked[attackee])
					continue;

				if (attackee.CanDraw)
				{
					CardCollection cards = attackee.Draw(2, DeckLocation.Revealed);
					String name = (attackee == player ? "your" : String.Format("{0}'s", attackee.Name));
					Choice choice = new Choice(
						String.Format("Do you want to discard {0} {1} or put them back on top?", name, String.Join(" and ", cards.Select(c => c.Name))), 
						this,
						cards, 
						new List<string>() { "Discard", "Put them back" }, 
						attackee);
					ChoiceResult result = player.MakeChoice(choice);
					if (result.Options[0] == "Discard")
						attackee.DiscardRevealed();
					else
					{
						Choice replaceChoice = new Choice("Choose order of cards to put back on your deck", this, attackee.Revealed, attackee, true, 2, 2);
						ChoiceResult replaceResult = attackee.MakeChoice(replaceChoice);
						attackee.RetrieveCardsFrom(DeckLocation.Revealed);
						attackee.AddCardsToDeck(replaceResult.Cards, DeckPosition.Top);
					}
				}
			}

			base.PlayRest(player);
		}
	}
	public class Scheme : Card
	{
		private Player _TurnEndedPlayer = null;
		private Player.TurnEndedEventHandler _TurnEndedEventHandler = null;

		private List<Player.CleaningUpEventHandler> _CleaningUpEventHandlers = new List<Player.CleaningUpEventHandler>();
		private static Player.CardsDiscardingEventHandler _CardsDiscardingEventHandler = null;
		private static CardCollection _CardsToTopDeck = new CardCollection();
		private static Player.CleanedUpEventHandler _CleanedUpEventHandler = null;

		public Scheme()
			: base("Scheme", Category.Action, Source.Hinterlands, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.CardOrdering)
		{
			this.BaseCost = new Cost(3);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 1;
			this.Text = "At the start of Clean-up this turn, you may choose an Action card you have in play.  If you discard it from play this turn, put it on your deck.";
		}

		internal override void TearDown()
		{
			base.TearDown();

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

			_CleaningUpEventHandlers.Add(new Player.CleaningUpEventHandler(player_CleaningUp));
			player.CleaningUp += _CleaningUpEventHandlers.Last();
		}

		void player_CleaningUp(object sender, CleaningUpEventArgs e)
		{
			if (e.Actions.ContainsKey(TypeClass.Scheme))
			{
				e.Actions[TypeClass.Scheme].Data = ((int)e.Actions[TypeClass.Scheme].Data) + 1;
				return;
			}

			// Check to see if Scheme has been resolved yet
			if (_CardsToTopDeck.Count == 0)
				e.Actions[TypeClass.Scheme] = new CleaningUpAction(this, String.Format("Resolve {0}", this), player_Action) { Data = 1 };
		}

		internal void player_Action(Player player, ref CleaningUpEventArgs e)
		{
			if (_CleaningUpEventHandlers.Count > 0)
			{
				player.CleaningUp -= _CleaningUpEventHandlers[0];
				_CleaningUpEventHandlers.RemoveAt(0);
			}

			CardCollection allInPlayCards = this.GetSelectableCards(e);

			if (allInPlayCards.Count > 0)
			{
				int schemeChoices = (int)e.Actions[TypeClass.Scheme].Data;
				Choice choice = new Choice(
					String.Format("Select up to {0} Action {1} to place on top of your deck", schemeChoices, Utilities.StringUtility.Plural("card", schemeChoices, false)),
					this,
					allInPlayCards,
					player,
					false,
					true,
					0,
					schemeChoices);
				ChoiceResult result = player.MakeChoice(choice);
				_CardsToTopDeck.Clear();
				foreach (Card cardToMove in result.Cards)
					_CardsToTopDeck.Add(cardToMove);

				if (_CardsToTopDeck.Count > 0)
				{
					_CardsDiscardingEventHandler = new Player.CardsDiscardingEventHandler(player_CardsDiscarding);
					e.CurrentPlayer.CardsDiscarding += _CardsDiscardingEventHandler;
					_CleanedUpEventHandler = new Player.CleanedUpEventHandler(player_CleanedUp);
					e.CurrentPlayer.CleanedUp += _CleanedUpEventHandler;
				}
			}
		}

		void player_CardsDiscarding(object sender, CardsDiscardEventArgs e)
		{
			Player player = (Player)sender;
			foreach (Card card in _CardsToTopDeck)
			{
				if (e.GetAction(TypeClass.Scheme, card.CardType) != null || player.ResolveDeck(e.FromLocation)[c => c == card].Count == 0)
					continue;

				e.AddAction(TypeClass.Scheme, card.CardType, new CardsDiscardAction(player, this, String.Format("Put {0} on your deck", card), player_DiscardAction, true) { Data = card });
			}
		}

		internal void player_DiscardAction(Player player, ref CardsDiscardEventArgs e)
		{
			Card cardToTopDeck = e.Data as Card;
			e.Cards.Remove(cardToTopDeck);
			if (player.InPlay.Contains(cardToTopDeck))
				player.RetrieveCardFrom(DeckLocation.InPlay, cardToTopDeck);
			else
				player.RetrieveCardFrom(DeckLocation.SetAside, cardToTopDeck);
			player.AddCardToDeck(cardToTopDeck, DeckPosition.Top);

			e.HandledBy.Add(cardToTopDeck);
		}

		void player_CleanedUp(object sender, CleanedUpEventArgs e)
		{
			if (_CardsDiscardingEventHandler != null)
				((Player)sender).CardsDiscarding -= _CardsDiscardingEventHandler;
			if (_CleanedUpEventHandler != null)
				((Player)sender).CleanedUp -= _CleanedUpEventHandler;
			_CardsToTopDeck.Clear();
			_CardsDiscardingEventHandler = null;
			_CleanedUpEventHandler = null;
		}

		private CardCollection GetSelectableCards(CleaningUpEventArgs e)
		{
			CardCollection allInPlayCards = new CardCollection();
			allInPlayCards.AddRange(e.CardsMovements.Where(cm =>
				((cm.Card.Category & Cards.Category.Action) == Cards.Category.Action) &&
				cm.CurrentLocation == DeckLocation.InPlay &&
				(cm.Destination == DeckLocation.SetAside || cm.Destination == DeckLocation.Discard)).Select(cm => cm.Card));

			// This is used to separate the In Play from the Set Aside for the Choice.MakeChoice call
			allInPlayCards.Add(new Universal.Dummy());

			allInPlayCards.AddRange(e.CardsMovements.Where(cm =>
				((cm.Card.Category & Cards.Category.Action) == Cards.Category.Action) &&
				cm.CurrentLocation == DeckLocation.SetAside &&
				cm.Destination == DeckLocation.Discard).Select(cm => cm.Card));

			if (allInPlayCards.FirstOrDefault() is Universal.Dummy)
				allInPlayCards.RemoveAt(0);
			if (allInPlayCards.LastOrDefault() is Universal.Dummy)
				allInPlayCards.RemoveAt(allInPlayCards.Count - 1);

			return allInPlayCards;
		}

		void player_TurnEnded(object sender, TurnEndedEventArgs e)
		{
			Player player = sender as Player;

			if (_TurnEndedEventHandler != null && _TurnEndedPlayer != null)
				_TurnEndedPlayer.TurnEnded -= _TurnEndedEventHandler;
			_TurnEndedPlayer = null;
			_TurnEndedEventHandler = null;

			foreach (Player.CleaningUpEventHandler cueh in _CleaningUpEventHandlers)
				player.CleaningUp -= cueh;
			_CleaningUpEventHandlers.Clear();
		}
	}
	public class SilkRoad : Card
	{
		public SilkRoad()
			: base("Silk Road", Category.Victory, Source.Hinterlands, Location.Kingdom, Group.VariableVPs)
		{
			this.BaseCost = new Cost(4);
			this.Text = "Worth <vp>1</vp> for every 4 Victory cards in your deck (rounded down).";
		}

		public override int GetVictoryPoints(IEnumerable<Card> cards)
		{
			int vps = base.GetVictoryPoints(cards) +
				cards.Count(c => (c.Category & Category.Victory) == Category.Victory) / 4;
			return vps;
		}
	}
	public class SpiceMerchant : Card
	{
		public SpiceMerchant()
			: base("Spice Merchant", Category.Action, Source.Hinterlands, Location.Kingdom, Group.DeckReduction | Group.PlusCard | Group.PlusAction | Group.Trash | Group.PlusCoin | Group.PlusBuy | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(4);
			this.Text = "You may trash a Treasure card from your hand.  If you do, choose one:<nl/>+2 Cards and +1 Action;<nl/>or +<coin>2</coin> and +1 Buy.";
		}

		public override void Play(Player player)
		{
			base.Play(player);
			Choice choice = new Choice("You may choose a Treasure card to trash", this, player.Hand[Cards.Category.Treasure], player, false, 0, 1);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Cards.Count > 0)
			{
				player.Trash(player.RetrieveCardFrom(DeckLocation.Hand, result.Cards[0]));

				CardBenefit benefit = new CardBenefit();
				Choice choiceBenefit = new Choice("Choose either +2 Cards and +1 Action; or +<coin>2</coin> and +1 Buy", this, new CardCollection() { this }, new List<string>() { "+2<nbsp/>Cards and +1<nbsp/>Action", "+<coin>2</coin> and +1<nbsp/>Buy" }, player);
				ChoiceResult resultBenefit = player.MakeChoice(choiceBenefit);
				if (resultBenefit.Options[0] == "+2<nbsp/>Cards and +1<nbsp/>Action")
				{
					benefit.Cards = 2;
					benefit.Actions = 1;
				}
				else
				{
					benefit.Currency.Coin.Value = 2;
					benefit.Buys = 1;
				}

				player.ReceiveBenefit(this, benefit);
			}
		}
	}
	public class Stables : Card
	{
		public Stables()
			: base("Stables", Category.Action, Source.Hinterlands, Location.Kingdom, Group.Discard | Group.PlusCard | Group.PlusAction | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(5);
			this.Text = "You may discard a Treasure.<nl/>If you do, +3 Cards and +1 Action.";
		}

		public override void Play(Player player)
		{
			base.Play(player);
			Choice choice = new Choice("You may choose a Treasure card to discard", this, player.Hand[Cards.Category.Treasure], player, false, 0, 1);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Cards.Count > 0)
			{
				player.Discard(DeckLocation.Hand, result.Cards[0]);

				CardBenefit benefit = new CardBenefit() { Cards = 3, Actions = 1 };
				player.ReceiveBenefit(this, benefit);
			}
		}
	}
	public class Trader : Card
	{
		private Player.CardGainingEventHandler _CardGainingHandler = null;

		public Trader()
			: base("Trader", Category.Action | Category.Reaction, Source.Hinterlands, Location.Kingdom,
			Group.DeckReduction | Group.Gain | Group.ReactToGain | Group.Trash | Group.RemoveCurses | Group.Defense | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.Text = "Trash a card from your hand.<nl/>Gain a number of Silvers equal to its cost in coins.<br/>When you would gain a card, you may reveal this from your hand. If you do, instead, gain a Silver.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choiceTrash = new Choice("Choose a card to trash", this, player.Hand, player);
			ChoiceResult resultTrash = player.MakeChoice(choiceTrash);
			player.Trash(player.RetrieveCardsFrom(DeckLocation.Hand, resultTrash.Cards));

			if (resultTrash.Cards.Count > 0)
			{
				player.Gain(player._Game.Table.Silver, player._Game.ComputeCost(resultTrash.Cards[0]).Coin.Value);
			}
		}

		public override void AddedTo(DeckLocation location, Player player)
		{
			base.AddedTo(location, player);

			if (location == DeckLocation.Hand)
			{
				if (_CardGainingHandler != null)
					player.CardGaining -= _CardGainingHandler;

				_CardGainingHandler = new Player.CardGainingEventHandler(player_CardGaining);
				player.CardGaining += _CardGainingHandler;
			}
		}

		void player_CardGaining(object sender, Players.CardGainEventArgs e)
		{
			Player player = sender as Player;

			// Already been cancelled -- don't need to process this one; also skip revealing for Silver
			if (e.Cancelled || !player.Hand.Contains(this) || e.Actions.ContainsKey(TypeClass.Trader) || e.Card.CardType == Cards.Universal.TypeClass.Silver)
				return;

			e.Actions[TypeClass.Trader] = new Players.CardGainAction(this.Owner, this, String.Format("Reveal {0}", this.PhysicalCard), player_RevealTrader, false);
		}

		internal void player_RevealTrader(Player player, ref Players.CardGainEventArgs e)
		{
			player.AddCardInto(DeckLocation.Revealed, player.RetrieveCardFrom(DeckLocation.Hand, this));
			player.AddCardInto(DeckLocation.Hand, player.RetrieveCardFrom(DeckLocation.Revealed, this));

			// Cancel the gain, add the card back to the Supply pile, and then Gain a Silver instead.
			e.Cancelled = true;
			e.IsLostTrackOf = true;
			//player._Game.Table.Supplies[e.Card].AddTo(e.Card);
			player._Game.SendMessage(player, this, e.Card, player._Game.Table.Silver);
			player.Gain(player._Game.Table.Silver);
		}

		public override void RemovedFrom(DeckLocation location, Player player)
		{
			base.RemovedFrom(location, player);
			if (_CardGainingHandler != null)
				player.CardGaining -= _CardGainingHandler;
			_CardGainingHandler = null;
		}

	}
	public class Tunnel : Card
	{
		private Player.CardsDiscardedEventHandler _CardsDiscardedEventHandler = null;

		public Tunnel()
			: base("Tunnel", Category.Victory | Category.Reaction, Source.Hinterlands, Location.Kingdom, Group.Gain | Group.ReactToDiscard)
		{
			this.BaseCost = new Cost(3);
			this.VictoryPoints = 2;
			this.Text = "<br/>When you discard this other than during a Clean-up phase, you may reveal it.  If you do, gain a Gold.";

			this.OwnerChanged += new OwnerChangedEventHandler(Tunnel_OwnerChanged);
		}

		internal override void TearDown()
		{
			Tunnel_OwnerChanged(this, new OwnerChangedEventArgs(this.Owner, null));

			base.TearDown();

			this.OwnerChanged -= new OwnerChangedEventHandler(Tunnel_OwnerChanged);
		}

		void Tunnel_OwnerChanged(object sender, OwnerChangedEventArgs e)
		{
			if (_CardsDiscardedEventHandler != null && e.OldOwner != null)
			{
				e.OldOwner.CardsDiscarded -= _CardsDiscardedEventHandler;
				_CardsDiscardedEventHandler = null;
			}

			if (e.NewOwner != null)
			{
				_CardsDiscardedEventHandler = new Player.CardsDiscardedEventHandler(player_CardsDiscarded);
				e.NewOwner.CardsDiscarded += _CardsDiscardedEventHandler;
			}
		}

		void player_CardsDiscarded(object sender, CardsDiscardEventArgs e)
		{
			Player player = sender as Player;

			// Already being processed or been handled -- don't need to process this one
			if (e.GetAction(TypeClass.Tunnel) != null || e.HandledBy.Contains(this))
				return;

			if (e.Cards.Contains(this.PhysicalCard) && player.Phase != PhaseEnum.Cleanup)
				e.AddAction(TypeClass.Tunnel, new CardsDiscardAction(this.Owner, this, String.Format("Reveal {0}", this.PhysicalCard), player_DiscardTunnel, false));
		}

		internal void player_DiscardTunnel(Player player, ref CardsDiscardEventArgs e)
		{
			player.AddCardInto(DeckLocation.Revealed, this);
			player.RetrieveCardFrom(DeckLocation.Revealed, this);
			player.Gain(player._Game.Table.Gold);
			e.HandledBy.Add(this);
		}
	}
}