using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DominionBase.Currencies;
using DominionBase.Piles;
using DominionBase.Players;

namespace DominionBase.Cards.Guilds
{
	public static class TypeClass
	{
		public static Type Advisor = typeof(Advisor);
		public static Type Baker = typeof(Baker);
		public static Type Butcher = typeof(Butcher);
		public static Type CandlestickMaker = typeof(CandlestickMaker);
		public static Type Doctor = typeof(Doctor);
		public static Type Herald = typeof(Herald);
		public static Type Journeyman = typeof(Journeyman);
		public static Type Masterpiece = typeof(Masterpiece);
		public static Type MerchantGuild = typeof(MerchantGuild);
		public static Type Plaza = typeof(Plaza);
		public static Type Soothsayer = typeof(Soothsayer);
		public static Type Stonemason = typeof(Stonemason);
		public static Type Taxman = typeof(Taxman);

		public static Type CoinToken = typeof(CoinToken);
	}

	public class Advisor : Card
	{
		public Advisor()
			: base("Advisor", Category.Action, Source.Guilds, Location.Kingdom, Group.Discard | Group.PlusAction | Group.PlusCard)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Actions = 1;
			this.Text = "Reveal the top 3 cards of your deck. The player to your left chooses one of them. Discard that card. Put the other cards into your hand.";
		}

		public override void Play(Player player)
		{
			base.Play(player);
			player.Draw(3, DeckLocation.Revealed);

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
	public class Baker : Card
	{
		public Baker()
			: base("Baker", Category.Action, Source.Guilds, Location.Kingdom, Group.Component | Group.PlusCard | Group.PlusAction | Group.PlusCoinToken)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 1;
			this.Text = "Take a Coin token.<br/>Setup: Each player takes a Coin token.";
		}

		public override void Finalize(Game game, Supply supply)
		{
			base.Finalize(game, supply);
			foreach (Player player in game.Players)
			{
				if (!player.TokenPiles.ContainsKey(TypeClass.CoinToken))
					player.TokenPiles[TypeClass.CoinToken] = new TokenCollection();
				player.AddToken(new CoinToken());
			}
		}

		public override void Play(Player player)
		{
			base.Play(player);
			player.AddToken(new CoinToken());
		}
	}
	public class Butcher : Card
	{
		public Butcher()
			: base("Butcher", Category.Action, Source.Guilds, Location.Kingdom, Group.Component | Group.Gain | Group.PlusCoinToken | Group.RemoveCurses | Group.Terminal | Group.Trash)
		{
			this.BaseCost = new Cost(5);
			this.Text = "Take 2 Coin tokens.  You may trash a card from your hand and then pay any number of Coin tokens.  If you did trash a card, gain a card with a cost of up to the cost of the trashed card plus the number of Coin tokens you paid.";
		}

		internal override void ReceivedBy(Player player)
		{
			base.ReceivedBy(player);
			if (!player.TokenPiles.ContainsKey(TypeClass.CoinToken))
				player.TokenPiles[TypeClass.CoinToken] = new TokenCollection();
		}

		public override void Play(Player player)
		{
			base.Play(player);
			player.AddToken(new CoinToken());
			player.AddToken(new CoinToken());

			Choice choiceTrash = new Choice("You may choose a card to trash", this, player.Hand, player, false, 0, 1);
			ChoiceResult resultTrash = player.MakeChoice(choiceTrash);

			if (resultTrash.Cards.Count > 0)
			{
				player.Trash(player.RetrieveCardsFrom(DeckLocation.Hand, resultTrash.Cards));

				List<String> options = new List<string>();
				for (int i = 0; i <= player.TokenPiles[TypeClass.CoinToken].Count; i++)
					options.Add(i.ToString());
				Choice choiceOverpay = new Choice("You may pay any number of Coin tokens", this, new CardCollection() { this }, options, player);
				ChoiceResult resultOverpay = player.MakeChoice(choiceOverpay);

				int overpayAmount = int.Parse(resultOverpay.Options[0]);
				player._Game.SendMessage(player, this, overpayAmount);
				player.RemoveTokens(TypeClass.CoinToken, overpayAmount);

				Cost trashedCardCost = player._Game.ComputeCost(resultTrash.Cards[0]);
				SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply => supply.CanGain() && supply.CurrentCost <= (trashedCardCost + new Coin(overpayAmount)));
				Choice choice = new Choice("Gain a card", this, gainableSupplies, player, false);
				ChoiceResult result = player.MakeChoice(choice);
				if (result.Supply != null)
					player.Gain(result.Supply);
			}
		}
	}
	public class CandlestickMaker : Card
	{
		public CandlestickMaker()
			: base("Candlestick Maker", Category.Action, Source.Guilds, Location.Kingdom, Group.Component | Group.PlusCoinToken | Group.PlusAction | Group.PlusBuy)
		{
			this.BaseCost = new Cost(2);
			this.Benefit.Actions = 1;
			this.Benefit.Buys = 1;
			this.Text = "Take a Coin Token.";
		}

		internal override void ReceivedBy(Player player)
		{
			base.ReceivedBy(player);
			if (!player.TokenPiles.ContainsKey(TypeClass.CoinToken))
				player.TokenPiles[TypeClass.CoinToken] = new TokenCollection();
		}

		public override void Play(Player player)
		{
			base.Play(player);
			player.AddToken(new CoinToken());
		}
	}
	public class CoinToken : Token
	{
		public CoinToken()
			: base("C", "Coin token")
		{
		}
		public override string Title { get { return "Spendable for <coin>1</coin> per token at the beginning of the Buy phase"; } }
		public override Boolean IsPlayable { get { return true; } }

		/// <summary>
		/// Used internally by the base Card class -- Don't use this.
		/// </summary>
		internal override void Play(Player player, int count)
		{
			if (count <= 0)
				throw new ArgumentOutOfRangeException("'count' must be positive!");
			player.ReceiveBenefit(this, new CardBenefit() { Currency = new Currency(count) });
		}
	}
	public class Doctor : Overpayer
	{
		public Doctor()
			: base("Doctor", Category.Action, Group.DeckReduction | Group.CardOrdering | Group.Discard | Group.Trash | Group.RemoveCurses | Group.Terminal, "Look at the top card of your deck; trash it, discard it, or put it back for each <coin>1</coin> you overpay.")
		{
			this.BaseCost = new Cost(3, false, true);
			this.Text = "Name a card.  Reveal the top 3 cards of your deck.  Trash the matches.  Put the rest back on top in any order.<br/>When you buy this, you may overpay for it.  For each <coin>1</coin> you overpaid, look at the top card of your deck; trash it, discard it, or put it back.";
		}

		public override void Overpay(Player player, Currency amount)
		{
			for (int i = 0; i < amount.Coin.Value; i++)
			{
				if (!player.CanDraw)
					break;
				Card card = player.Draw(DeckLocation.Private);
				Choice choice = new Choice(String.Format("Do you want to discard {0}, trash {0}, or put it back on top?", card.Name), this, new CardCollection() { card }, new List<string>() { "Discard", "Trash", "Put it back" }, player);
				ChoiceResult result = player.MakeChoice(choice);
				switch (result.Options[0])
				{
					case "Discard":
						player.Discard(DeckLocation.Private);
						break;
					case "Trash":
						player.Trash(DeckLocation.Private, card);
						break;
					default:
						player.AddCardsToDeck(player.RetrieveCardsFrom(DeckLocation.Private), DeckPosition.Top);
						break;
				}
			}
		}

		public override void Play(Player player)
		{
			base.Play(player);

			SupplyCollection availableSupplies = new SupplyCollection(player._Game.Table.Supplies.Where(kvp => kvp.Value.Randomizer != null && kvp.Value.Randomizer.GroupMembership != Group.None));
			CardCollection cards = new CardCollection();
			Choice choice = new Choice("Name a card", this, availableSupplies, player, false);
			foreach (Supply supply in player._Game.Table.Supplies.Values.Union(player._Game.Table.SpecialPiles.Values))
			{
				foreach (Type type in supply.CardTypes)
				{
					if (!choice.Supplies.Any(kvp => kvp.Value.CardType == type))
						cards.Add(Card.CreateInstance(type));
				}
			}
			cards.Sort();
			choice.AddCards(cards);

			ChoiceResult result = player.MakeChoice(choice);
			ICard namedCard = null;
			if (result.Supply != null)
				namedCard = result.Supply;
			else
				namedCard = result.Cards[0];

			player._Game.SendMessage(player, this, namedCard);
			CardCollection newCards = player.Draw(3, DeckLocation.Revealed);

			player.Trash(player.RetrieveCardsFrom(DeckLocation.Revealed, c => c.Name == namedCard.Name));

			Choice replaceChoice = new Choice("Choose order of cards to put back on your deck", this, player.Revealed, player, true, player.Revealed.Count, player.Revealed.Count);
			ChoiceResult replaceResult = player.MakeChoice(replaceChoice);
			player.RetrieveCardsFrom(DeckLocation.Revealed, replaceResult.Cards);
			player.AddCardsToDeck(replaceResult.Cards, DeckPosition.Top);
		}
	}
	public class Herald : Overpayer
	{
		public Herald()
			: base("Herald", Category.Action, Group.CardOrdering | Group.Component | Group.ConditionalBenefit | Group.PlusAction | Group.PlusCard, "Look through your discard pile and put a card from it on top of your deck per <coin>1</coin> you overpay.")
		{
			this.BaseCost = new Cost(4, false, true);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 1;
			this.Text = "Reveal the top card of your deck.  If it is an Action, play it.<br/>When you buy this, you may overpay for it.  For each <coin>1</coin> you overpaid, look through your discard pile and put a card from it on top of your deck.";
		}

		public override void Overpay(Player player, Currency amount)
		{
			Choice choice = new Choice("Choose cards to put on top of your deck", this, player.DiscardPile.LookThrough(c => true), player, false, amount.Coin.Value, amount.Coin.Value);
			ChoiceResult result = player.MakeChoice(choice);
			player.AddCardsInto(DeckLocation.Revealed, player.DiscardPile.Retrieve(player, c => result.Cards.Contains(c)));
			player.AddCardsToDeck(player.Revealed.Retrieve(player, c => result.Cards.Contains(c)), DeckPosition.Top);
		}

		public override void Play(Player player)
		{
			base.Play(player);

			if (player.CanDraw)
			{
				Card card = player.Draw(DeckLocation.Revealed);
				if ((card.Category & Cards.Category.Action) == Cards.Category.Action)
				{
					player.Actions++;
					PlayerMode previousPlayerMode = player.PutCardIntoPlay(card, String.Empty);
					Card logicalCard = card.LogicalCard;
					player.PlayCard(logicalCard, previousPlayerMode);
				}
				else
				{
					player.AddCardInto(DeckLocation.Deck, player.RetrieveCardFrom(DeckLocation.Revealed, card), DeckPosition.Top);
				}
			}
		}
	}
	public class Journeyman : Card
	{
		public Journeyman()
			: base("Journeyman", Category.Action, Source.Guilds, Location.Kingdom, Group.Discard | Group.Terminal)
		{
			this.BaseCost = new Cost(5);
			this.Text = "Name a card.  Reveal cards from the top of your deck until you reveal 3 cards that are not the named card.  Put those cards into your hand and discard the rest.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			SupplyCollection availableSupplies = new SupplyCollection(player._Game.Table.Supplies.Where(kvp => kvp.Value.Randomizer != null && kvp.Value.Randomizer.GroupMembership != Group.None));
			CardCollection cards = new CardCollection();
			Choice choice = new Choice("Name a card", this, availableSupplies, player, false);
			foreach (Supply supply in player._Game.Table.Supplies.Values.Union(player._Game.Table.SpecialPiles.Values))
			{
				foreach (Type type in supply.CardTypes)
				{
					if (!choice.Supplies.Any(kvp => kvp.Value.CardType == type))
						cards.Add(Card.CreateInstance(type));
				}
			}
			cards.Sort();
			choice.AddCards(cards);

			ChoiceResult result = player.MakeChoice(choice);
			ICard namedCard = null;
			if (result.Supply != null)
				namedCard = result.Supply;
			else
				namedCard = result.Cards[0];

			player._Game.SendMessage(player, this, namedCard);
			while (player.CanDraw && player.Revealed.Count(c => c.CardType != namedCard.CardType) < 3)
			{
				player.Draw(DeckLocation.Revealed);
			}
			player.AddCardsToHand(player.RetrieveCardsFrom(DeckLocation.Revealed,c => c.CardType != namedCard.CardType));
			player.DiscardRevealed();
		}
	}
	public class Masterpiece : Overpayer
	{
		public Masterpiece()
			: base("Masterpiece", Category.Treasure, Group.Gain, "Gain a Silver per <coin>1</coin> you overpay.")
		{
			this.BaseCost = new Cost(3, false, true);
			this.Benefit.Currency = new Currency(1);
			this.Text = "<br/>When you buy this, you may overpay for it. If you do, gain a Silver per <coin>1</coin> you overpaid.";
		}

		public override void Overpay(Player player, Currency amount)
		{
			player.Gain(player._Game.Table.Silver, amount.Coin.Value);
		}
	}
	public class MerchantGuild : Card
	{
		private Player.CardBoughtEventHandler _CardBoughtHandler = null;

		public MerchantGuild()
			: base("Merchant Guild", Category.Action, Source.Guilds, Location.Kingdom, Group.Component | Group.PlusBuy | Group.PlusCoinToken | Group.Terminal)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Buys = 1;
			this.Benefit.Currency = new Currency(1);
			this.Text = "<br/>While this is in play, when you buy a card, take a Coin token.";
		}

		internal override void ReceivedBy(Player player)
		{
			base.ReceivedBy(player);
			if (!player.TokenPiles.ContainsKey(TypeClass.CoinToken))
				player.TokenPiles[TypeClass.CoinToken] = new TokenCollection();
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

			(sender as Player).AddToken(new CoinToken());
		}

		public override void RemovedFrom(DeckLocation location, Player player)
		{
			base.RemovedFrom(location, player);
			if (_CardBoughtHandler != null)
				player.CardBought -= _CardBoughtHandler;
			_CardBoughtHandler = null;
		}
	}
	public abstract class Overpayer : Card
	{
		private Dictionary<Player, Player.CardBoughtEventHandler> _CardBoughtHandlers = new Dictionary<Player, Player.CardBoughtEventHandler>();
		private String _OverpayMessage = String.Empty;
		public Boolean _HandlersHookedUp = false;

		internal Overpayer(String name, Category category, Group group, String overpayMessage)
			: base(name, category, Source.Guilds, Location.Kingdom, group | Group.Overpay)
		{
			_OverpayMessage = String.Format("{0}  How much do you want to overpay?", overpayMessage);
		}

		private String OverpayMessage
		{
			get { return _OverpayMessage; }
		}

		public virtual void Overpay(Player player, Currency amount) { throw new NotImplementedException(); }

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
			_HandlersHookedUp = true;
		}

		void player_CardBought(object sender, Players.CardBuyEventArgs e)
		{
			// This is not the card you are looking for
			if (e.Card != this || e.Actions.ContainsKey(this.GetType()))
				return;

			Player player = sender as Player;
			if (player.Currency > e.Game.ComputeCost(this))
				e.Actions[this.GetType()] = new Players.CardBuyAction(this.Owner, this, String.Format("Overpay for {0}", this.Name), player_BuyOverpayer, false);
		}

		internal void player_BuyOverpayer(Player player, ref Players.CardBuyEventArgs e)
		{
			OptionCollection options = new OptionCollection();
			for (int i = 0; i <= player.Currency.Coin.Value - e.Game.ComputeCost(this).Coin.Value; i++)
				for (int j = 0; j <= player.Currency.Potion.Value; j++)
					if (i != 0 || j != 0)
						options.Add(new Currency(i, j).ToStringInline(), false);

			if (options.Count > 0)
			{
				Choice choiceOverpay = new Choice(this.OverpayMessage, this, options, player);
				ChoiceResult resultOverpay = player.MakeChoice(choiceOverpay);

				if (resultOverpay.Options.Count > 0)
				{
					Currency overpayAmount = new Currency(resultOverpay.Options[0]);
					player._Game.SendMessage(player, this, overpayAmount);

					if (!overpayAmount.IsBlank)
					{
						player.SpendCurrency(overpayAmount);
						Overpay(player, overpayAmount);
					}
				}
			}

			e.HandledBy.Add(this.GetType());

			// Clear out the Event Triggers -- this only happens when its Gained, so we don't care any more
			foreach (Player playerLoop in _CardBoughtHandlers.Keys)
				playerLoop.CardBought -= _CardBoughtHandlers[playerLoop];
			_CardBoughtHandlers.Clear();
		}
	}
	public class Plaza : Card
	{
		public Plaza()
			: base("Plaza", Category.Action, Source.Guilds, Location.Kingdom, Group.Component | Group.PlusCoinToken | Group.Discard | Group.PlusAction | Group.PlusMultipleActions | Group.PlusCard)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 2;
			this.Text = "You may discard a Treasure card.<nl/>If you do, take a Coin token.";
		}

		internal override void ReceivedBy(Player player)
		{
			base.ReceivedBy(player);
			if (!player.TokenPiles.ContainsKey(TypeClass.CoinToken))
				player.TokenPiles[TypeClass.CoinToken] = new TokenCollection();
		}

		public override void Play(Player player)
		{
			base.Play(player);
			Choice choice = new Choice("You may choose a Treasure card to discard", this, player.Hand[Cards.Category.Treasure], player, false, 0, 1);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Cards.Count > 0)
			{
				player.Discard(DeckLocation.Hand, result.Cards[0]);

				player.AddToken(new CoinToken());
			}
		}
	}
	public class Soothsayer : Card
	{
		public Soothsayer()
			: base("Soothsayer", Category.Action | Category.Attack, Source.Guilds, Location.Kingdom, Group.Gain | Group.Terminal)
		{
			this.BaseCost = new Cost(5);
			this.Text = "Gain a Gold. Each other player gains a Curse. Each player who did draws a card.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			player.Gain(player._Game.Table.Gold);

			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			enumerator.MoveNext();
			while (enumerator.MoveNext())
			{
				Player attackee = enumerator.Current;
				// Skip if the attack is blocked (Moat, Lighthouse, etc.)
				if (this.IsAttackBlocked[attackee])
					continue;

				if (attackee.Gain(player._Game.Table.Curse) && attackee.CanDraw)
					attackee.Draw(DeckLocation.Hand);
			}
		}
	}
	public class Stonemason : Overpayer
	{
		public Stonemason()
			: base("Stonemason", Category.Action, Group.Trash | Group.Gain | Group.RemoveCurses | Group.Terminal, "Gain 2 Action cards each costing the amount you overpay.")
		{
			this.BaseCost = new Cost(2, false, true);
			this.Text = "Trash a card from your hand, Gain 2 cards each costing less than it.<br/>When you buy this, you may overpay for it.<nl/>If you do, gain 2 Action cards each costing the amount you overpaid.";
		}

		public override void Overpay(Player player, Currency amount)
		{
			for (int i = 0; i < 2; i++)
			{
				SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply => 
					supply.CanGain() && 
					(supply.Category & Cards.Category.Action) == Cards.Category.Action && 
					supply.CurrentCost == amount);
				Choice choice = new Choice(String.Format("Gain an Action card costing {0}", amount.ToStringInline()), this, gainableSupplies, player, false);
				ChoiceResult result = player.MakeChoice(choice);
				if (result.Supply != null)
					player.Gain(result.Supply);
			}
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
				for (int i = 0; i < 2; i++)
				{
					SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply => supply.CanGain() && supply.CurrentCost < trashedCardCost);
					Choice choice = new Choice(String.Format("Gain a card costing less than {0}", trashedCardCost.ToString()), this, gainableSupplies, player, false);
					ChoiceResult result = player.MakeChoice(choice);
					if (result.Supply != null)
						player.Gain(result.Supply);
				}
			}
		}
	}
	public class Taxman : Card
	{
		public Taxman()
			: base("Taxman", Category.Action | Category.Attack, Source.Guilds, Location.Kingdom, Group.Trash | Group.Gain | Group.CardOrdering | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.Text = "You may trash a Treasure from your hand. Each other player with 5 or more cards in hand discards a copy of it (or reveals a hand without it). Gain a Treasure card costing up to <coin>3</coin> more than the trashed card, putting it on top of your deck.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choiceTrash = new Choice("You may choose a Treasure card to trash", this, player.Hand[Cards.Category.Treasure], player, false, 0, 1);
			ChoiceResult resultTrash = player.MakeChoice(choiceTrash);
			player.Trash(player.RetrieveCardsFrom(DeckLocation.Hand, resultTrash.Cards));

			if (resultTrash.Cards.Count > 0)
			{
				Cost trashedCardCost = player._Game.ComputeCost(resultTrash.Cards[0]);

				IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
				enumerator.MoveNext();
				while (enumerator.MoveNext())
				{
					Player attackee = enumerator.Current;
					// Skip if the attack is blocked (Moat, Lighthouse, etc.)
					if (this.IsAttackBlocked[attackee])
						continue;

					if (attackee.Hand.Count < 5)
						continue;

					// If the player doesn't have any of this card, reveal the player's hand
					if (attackee.Hand[resultTrash.Cards[0].CardType].Count == 0)
						attackee.ReturnHand(attackee.RevealHand());
					// Otherwise, the player automatically discards the card (no real choices to be made here)
					else
						attackee.Discard(DeckLocation.Hand, attackee.Hand[resultTrash.Cards[0].CardType].First());
				}

				SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(
					supply => supply.CanGain() &&
						((supply.Category & Cards.Category.Treasure) == Cards.Category.Treasure) &&
						supply.CurrentCost <= (trashedCardCost + new Coin(3)));
				Choice choice = new Choice("Gain a card", this, gainableSupplies, player, false);
				ChoiceResult result = player.MakeChoice(choice);
				if (result.Supply != null)
					player.Gain(result.Supply, DeckLocation.Deck, DeckPosition.Top);
			}
		}
	}
}
