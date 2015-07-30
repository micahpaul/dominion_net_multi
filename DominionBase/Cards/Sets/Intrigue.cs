using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DominionBase.Currencies;
using DominionBase.Piles;
using DominionBase.Players;

namespace DominionBase.Cards.Intrigue
{
	public static class TypeClass
	{
		public static Type Baron = typeof(Baron);
		public static Type Bridge = typeof(Bridge);
		public static Type Conspirator = typeof(Conspirator);
		public static Type Coppersmith = typeof(Coppersmith);
		public static Type Courtyard = typeof(Courtyard);
		public static Type Duke = typeof(Duke);
		public static Type GreatHall = typeof(GreatHall);
		public static Type Harem = typeof(Harem);
		public static Type Ironworks = typeof(Ironworks);
		public static Type Masquerade = typeof(Masquerade);
		public static Type MiningVillage = typeof(MiningVillage);
		public static Type Minion = typeof(Minion);
		public static Type Nobles = typeof(Nobles);
		public static Type Pawn = typeof(Pawn);
		public static Type Saboteur = typeof(Saboteur);
		public static Type Scout = typeof(Scout);
		public static Type SecretChamber = typeof(SecretChamber);
		public static Type ShantyTown = typeof(ShantyTown);
		public static Type Steward = typeof(Steward);
		public static Type Swindler = typeof(Swindler);
		public static Type Torturer = typeof(Torturer);
		public static Type TradingPost = typeof(TradingPost);
		public static Type Tribute = typeof(Tribute);
		public static Type Upgrade = typeof(Upgrade);
		public static Type WishingWell = typeof(WishingWell);
	}

	public class Baron : Card
	{
		public Baron()
			: base("Baron", Category.Action, Source.Intrigue, Location.Kingdom, Group.PlusCoin | Group.PlusBuy | Group.Gain | Group.Discard | Group.Terminal | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Buys = 1;
			this.Text = "You may discard an Estate card.<nl/>If you do, +<coin>4</coin>.<nl/>Otherwise, gain an Estate card.";
		}

		public override void Play(Player player)
		{
			base.Play(player);
			if (player.Hand[Cards.Universal.TypeClass.Estate].Count > 0)
			{
				Choice choice = Choice.CreateYesNoChoice("You may discard an Estate card for +<coin>4</coin>.  Do you want to discard?", this, player);
				ChoiceResult result = player.MakeChoice(choice);
				if (result.Options.Contains("Yes"))
				{
					player.Discard(DeckLocation.Hand, Cards.Universal.TypeClass.Estate, 1);

					CardBenefit benefit = new CardBenefit();
					benefit.Currency.Coin += 4;
					player.ReceiveBenefit(this, benefit);

					return;
				}
			}

			player.Gain(player._Game.Table.Estate);
		}
	}
	public class Bridge : Card
	{
		private Player _TurnEndedPlayer = null;
		private Player.TurnEndedEventHandler _TurnEndedEventHandler = null;
		private List<Game.CostComputeEventHandler> _CostComputeEventHandlers = new List<Game.CostComputeEventHandler>();

		public Bridge()
			: base("Bridge", Category.Action, Source.Intrigue, Location.Kingdom, Group.PlusCoin | Group.PlusBuy | Group.ModifyCost | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Currency.Coin.Value = 1;
			this.Benefit.Buys = 1;
			this.Text = "All cards (including cards in players' hands) cost <coin>1</coin> less this turn, but not less than <coin>0</coin>";
		}

		internal override void TearDown()
		{
			base.TearDown();

			if (_TurnEndedEventHandler != null && _TurnEndedPlayer != null)
				player_TurnEnded(_TurnEndedPlayer, new TurnEndedEventArgs(_TurnEndedPlayer));
			_TurnEndedEventHandler = null;
		}

		protected override Boolean AllowUndo { get { return true; } }

		public override void Play(Player player)
		{
			base.Play(player);

			if (_TurnEndedEventHandler == null)
			{
				_TurnEndedPlayer = player;
				_TurnEndedEventHandler = new Player.TurnEndedEventHandler(player_TurnEnded);
				player.TurnEnded += _TurnEndedEventHandler;
			}

			_CostComputeEventHandlers.Add(new Game.CostComputeEventHandler(player_BridgePlayed));
			player._Game.CostCompute += _CostComputeEventHandlers[_CostComputeEventHandlers.Count - 1];
			player._Game.SendMessage(player, this, 1);
		}

		void player_BridgePlayed(object sender, CostComputeEventArgs e)
		{
			e.Cost.Coin -= 1;
		}

		void player_TurnEnded(object sender, TurnEndedEventArgs e)
		{
			Player player = sender as Player;

			if (_TurnEndedEventHandler != null && _TurnEndedPlayer != null)
				_TurnEndedPlayer.TurnEnded -= _TurnEndedEventHandler;
			_TurnEndedPlayer = null;
			_TurnEndedEventHandler = null;

			foreach (Game.CostComputeEventHandler cceh in _CostComputeEventHandlers)
				player._Game.CostCompute -= cceh;
			_CostComputeEventHandlers.Clear();
		}
	}
	public class Conspirator : Card
	{
		public Conspirator()
			: base("Conspirator", Category.Action, Source.Intrigue, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.PlusCoin | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Currency.Coin.Value = 2;
			this.Text = "If you've played 3 or more Actions this turn (counting this): +1<nbsp/>Card, +1<nbsp/>Action";
		}

		public override void Play(Player player)
		{
			base.Play(player);
			if (player.ActionsPlayed >= 3)
			{
				player.ReceiveBenefit(this, new CardBenefit() { Actions = 1, Cards = 1 });
			}
		}
	}
	public class Coppersmith : Card
	{
		private Player _TurnEndedPlayer = null;
		private Player.TurnEndedEventHandler _TurnEndedEventHandler = null;
		private List<Player.CardPlayedEventHandler> _CardPlayedEventHandlers = new List<Player.CardPlayedEventHandler>();

		public Coppersmith()
			: base("Coppersmith", Category.Action, Source.Intrigue, Location.Kingdom, Group.Basic | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.Text = "Copper produces an extra <coin>1</coin> this turn.";
		}

		protected override Boolean AllowUndo { get { return true; } }

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

			_CardPlayedEventHandlers.Add(new Player.CardPlayedEventHandler(ActivePlayer_CardPlayed));
			player.CardPlayed += _CardPlayedEventHandlers[_CardPlayedEventHandlers.Count - 1];
		}

		void ActivePlayer_CardPlayed(object sender, CardPlayedEventArgs e)
		{
			CardBenefit benefit = new CardBenefit();
			foreach (Card card in e.Cards)
			{
				if (card.CardType == Universal.TypeClass.Copper)
					benefit.Currency += new Coin(1);
			}
			e.Player.ReceiveBenefit(this, benefit);
		}

		void player_TurnEnded(object sender, TurnEndedEventArgs e)
		{
			Player player = sender as Player;

			if (_TurnEndedEventHandler != null && _TurnEndedPlayer != null)
				_TurnEndedPlayer.TurnEnded -= _TurnEndedEventHandler;
			_TurnEndedPlayer = null;
			_TurnEndedEventHandler = null;

			foreach (Player.CardPlayedEventHandler cpeh in _CardPlayedEventHandlers)
				player.CardPlayed -= cpeh;
			_CardPlayedEventHandlers.Clear();
		}
	}
	public class Courtyard : Card
	{
		public Courtyard()
			: base("Courtyard", Category.Action, Source.Intrigue, Location.Kingdom, Group.CardOrdering | Group.PlusCard | Group.Terminal)
		{
			this.BaseCost = new Cost(2);
			this.Benefit.Cards = 3;
			this.Text = "Put a card from your hand on top of your deck.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choice = new Choice("Choose a card to put on top of your deck", this, player.Hand, player);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Cards.Count > 0)
				player.AddCardToDeck(player.RetrieveCardFrom(DeckLocation.Hand, result.Cards[0]), DeckPosition.Top);
		}
	}
	public class Duke : Card
	{
		public Duke()
			: base("Duke", Category.Victory, Source.Intrigue, Location.Kingdom, Group.VariableVPs)
		{
			this.BaseCost = new Cost(5);
			this.Text = "Worth <vp>1</vp> per Duchy you have.";
		}

		public override int GetVictoryPoints(IEnumerable<Card> cards)
		{
			return base.GetVictoryPoints(cards) +
				cards.Count(c => c.CardType == Cards.Universal.TypeClass.Duchy);
		}
	}
	public class GreatHall : Card
	{
		public GreatHall()
			: base("Great Hall", Category.Action | Category.Victory, Source.Intrigue, Location.Kingdom, Group.PlusCard | Group.PlusAction)
		{
			this.BaseCost = new Cost(3);
			this.Benefit.Actions = 1;
			this.Benefit.Cards = 1;
			this.VictoryPoints = 1;
		}
	}
	public class Harem : Card
	{
		public Harem()
			: base("Harem", Category.Treasure | Category.Victory, Source.Intrigue, Location.Kingdom, Group.PlusCoin)
		{
			this.BaseCost = new Cost(6);
			this.Benefit.Currency.Coin.Value = 2;
			this.VictoryPoints = 2;
		}

		protected override Boolean AllowUndo { get { return true; } }
	}
	public class Ironworks : Card
	{
		public Ironworks()
			: base("Ironworks", Category.Action, Source.Intrigue, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.PlusCoin | Group.Gain | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(4);
			this.Text = "Gain a card costing up to <coin>4</coin>.<nl/><nl/>If it is an...<nl/>Action card, +1 Action<nl/>Treasure card, +<coin>1</coin><nl/>Victory card, +1 Card";
		}

		public override void Play(Player player)
		{
			base.Play(player);
			SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply => supply.CanGain() && supply.CurrentCost <= new Coin(4));
			Choice choice = new Choice("Gain a card costing up to <coin>4</coin>", this, gainableSupplies, player, false);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Supply != null)
			{
				CardBenefit benefit = new CardBenefit();

				if (player.Gain(result.Supply))
				{
					if ((result.Supply.Category & Cards.Category.Action) == Cards.Category.Action)
						benefit.Actions = 1;
					if ((result.Supply.Category & Cards.Category.Treasure) == Cards.Category.Treasure)
						benefit.Currency += new Coin(1);
					if ((result.Supply.Category & Cards.Category.Victory) == Cards.Category.Victory)
						benefit.Cards = 1;

					player.ReceiveBenefit(this, benefit);
				}
			}
		}
	}
	public class Masquerade : Card
	{
		public Masquerade()
			: base("Masquerade", Category.Action, Source.Intrigue, Location.Kingdom, Group.DeckReduction | Group.PlusCurses | Group.PlusCard | Group.Trash | Group.RemoveCurses | Group.AffectOthers | Group.Terminal)
		{
			this.BaseCost = new Cost(3);
			this.Benefit.Cards = 2;
			this.Text = "Each player passes a card from his hand to the left at once.  Then you may trash a card from your hand.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			List<Card> toPass = new List<Card>(player._Game.Players.Count);
			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			while (enumerator.MoveNext())
			{
				Player choosingPlayer = enumerator.Current;
				Choice choice = new Choice("Choose a card to pass to the left", this, choosingPlayer.Hand, player);
				ChoiceResult result = choosingPlayer.MakeChoice(choice);
				if (result.Cards.Count > 0)
				{
					Card passingCard = choosingPlayer.RetrieveCardFrom(DeckLocation.Hand, result.Cards[0]);
					choosingPlayer.Lose(passingCard);
					toPass.Add(passingCard);
					player._Game.SendMessage(choosingPlayer, player._Game.GetPlayerFromIndex(choosingPlayer, 1), this, result.Cards[0]);
				}
				else
					toPass.Add(null);
			}

			enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			int index = 0;
			while (enumerator.MoveNext())
			{
				Card fromRight = toPass[(index + player._Game.Players.Count - 1) % player._Game.Players.Count];
				if (fromRight != null)
					enumerator.Current.Receive(player._Game.GetPlayerFromIndex(enumerator.Current, -1), fromRight, DeckLocation.Hand, DeckPosition.Automatic);
				index++;
			}

			Choice choiceTrash = new Choice("You may choose a card to trash", this, player.Hand, player, false, 0, 1);
			ChoiceResult resultTrash = player.MakeChoice(choiceTrash);
			if (resultTrash.Cards.Count > 0)
				player.Trash(player.RetrieveCardFrom(DeckLocation.Hand, resultTrash.Cards[0]));
		}
	}
	public class MiningVillage : Card
	{
		public MiningVillage()
			: base("Mining Village", 
			Category.Action, 
			Source.Intrigue, 
			Location.Kingdom,
			Group.PlusCard | Group.PlusAction | Group.PlusMultipleActions | Group.PlusCoin | Group.Trash | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 2;
			this.Text = "You may trash this card immediately.<nl/>If you do, +<coin>2</coin>";
		}

		public override void Play(Player player)
		{
			base.Play(player);
			if (player.InPlay.Contains(this.PhysicalCard))
			{
				Choice choice = Choice.CreateYesNoChoice("Do you want to trash this card for +<coin>2</coin>?", this, player);
				ChoiceResult result = player.MakeChoice(choice);
				if (result.Options[0] == "Yes")
				{
					player.Trash(player.RetrieveCardFrom(DeckLocation.InPlay, this.PhysicalCard));
					CardBenefit benefit = new CardBenefit();
					benefit.Currency += new Coin(2);
					player.ReceiveBenefit(this, benefit);
				}
			}
		}
	}
	public class Minion : Card
	{
		public Minion()
			: base("Minion", Category.Action | Category.Attack, Source.Intrigue, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.PlusCoin | Group.Discard | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Actions = 1;
			this.Text = "Choose one: +<coin>2</coin>;<nl/>or discard your hand, +4<nbsp/>Cards, and each other player with at least 5 cards in hand discards his hand and draws 4 cards.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choice = new Choice("Choose one:", this, new CardCollection() { this }, new List<string>() { "+<coin>2</coin>", "Discard your hand, +4<nbsp/>Cards, and each other player with at least 5 cards in hand discards his hand and draws 4 cards" }, player);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Options.Contains("+<coin>2</coin>"))
			{
				CardBenefit benefit = new CardBenefit();
				benefit.Currency += new Coin(2);
				player.ReceiveBenefit(this, benefit);
			}
			else
			{
				player.DiscardHand(true);
				CardBenefit benefit = new CardBenefit() { Cards = 4 };
				player.ReceiveBenefit(this, benefit);

				// Perform attack on each other player
				IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
				enumerator.MoveNext();
				while (enumerator.MoveNext())
				{
					Player attackee = enumerator.Current;
					if (this.IsAttackBlocked[attackee])
						continue;

					if (attackee.Hand.Count > 4)
					{
						attackee.DiscardHand(true);
						attackee.ReceiveBenefit(this, benefit);
					}
				}
			}
		}
	}
	public class Nobles : Card
	{
		public Nobles()
			: base("Nobles", 
			Category.Action | Category.Victory, 
			Source.Intrigue, 
			Location.Kingdom,
			Group.PlusCard | Group.PlusAction | Group.PlusMultipleActions | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(6);
			this.VictoryPoints = 2;
			this.Text = "Choose one: +3<nbsp/>Cards; or +2<nbsp/>Actions.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choice = new Choice("Choose 1:", this, new CardCollection() { this }, new List<string>() { "+3<nbsp/>Cards", "+2<nbsp/>Actions" }, player);
			ChoiceResult result = player.MakeChoice(choice);

			CardBenefit benefit = new CardBenefit();
			if (result.Options.Contains("+3<nbsp/>Cards"))
				benefit.Cards = 3;
			if (result.Options.Contains("+2<nbsp/>Actions"))
				benefit.Actions = 2;
			player.ReceiveBenefit(this, benefit);
		}
	}
	public class Pawn : Card
	{
		public Pawn()
			: base("Pawn", Category.Action, Source.Intrigue, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.PlusCoin | Group.PlusBuy | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(2);
			this.Text = "Choose two: +1<nbsp/>Card; +1<nbsp/>Action; +1<nbsp/>Buy; +<coin>1</coin>.<nl/>(The choices must be different.)";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choice = new Choice("Choose 2:", this, new CardCollection() { this }, new List<string>() { "+1<nbsp/>Card", "+1<nbsp/>Action", "+1<nbsp/>Buy", "+<coin>1</coin>" }, player, null, true, 2, 2);
			ChoiceResult result = player.MakeChoice(choice);

			foreach (String option in result.Options)
			{
				CardBenefit benefit = new CardBenefit();
				if (option == "+1<nbsp/>Card")
					benefit.Cards = 1;
				if (option == "+1<nbsp/>Action")
					benefit.Actions = 1;
				if (option == "+1<nbsp/>Buy")
					benefit.Buys = 1;
				if (option == "+<coin>1</coin>")
					benefit.Currency += new Coin(1);
				player.ReceiveBenefit(this, benefit);
			}
		}
	}
	public class Saboteur : Card
	{
		public Saboteur()
			: base("Saboteur", Category.Action | Category.Attack, Source.Intrigue, Location.Kingdom, Group.Trash | Group.Discard | Group.Terminal)
		{
			this.BaseCost = new Cost(5);
			this.Text = "Each other player reveals cards from the top of his deck until revealing one costing <coin>3</coin> or more.  He trashes that card and may gain a card costing at most <coin>2</coin> less than it.  He discards the other revealed cards.";
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
				while (attackee.Revealed[card => player._Game.ComputeCost(card).Coin >= 3].Count < 1 && attackee.CanDraw)
					attackee.Draw(DeckLocation.Revealed);

				attackee.EndDrawing();

				CardCollection cards = attackee.Revealed[c => player._Game.ComputeCost(c).Coin >= 3];

				if (cards.Count > 0)
				{
					Card card = cards[0];
					Cost trashedCardCost = player._Game.ComputeCost(card);
					attackee.Trash(attackee.RetrieveCardFrom(DeckLocation.Revealed, card));
					SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply => supply.CanGain() && supply.CurrentCost <= (trashedCardCost.Coin - 2));
					Choice choice = new Choice("You may gain a card", this, gainableSupplies, player, true);
					ChoiceResult result = attackee.MakeChoice(choice);
					if (result.Supply != null)
						attackee.Gain(result.Supply);
				}
				attackee.DiscardRevealed();
			}
		}
	}
	public class Scout : Card
	{
		public Scout()
			: base("Scout", Category.Action, Source.Intrigue, Location.Kingdom, Group.CardOrdering | Group.PlusAction)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Actions = 1;
			this.Text = "Reveal the top 4 cards of your deck.  Put the revealed Victory cards into your hand.  Put the other cards on top of your deck in any order.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			CardCollection newCards = player.Draw(4, DeckLocation.Revealed);

			player.AddCardsToHand(player.RetrieveCardsFrom(DeckLocation.Revealed, Category.Victory));

			Choice replaceChoice = new Choice("Choose order of cards to put back on your deck", this, player.Revealed, player, true, player.Revealed.Count, player.Revealed.Count);
			ChoiceResult replaceResult = player.MakeChoice(replaceChoice);
			player.RetrieveCardsFrom(DeckLocation.Revealed, replaceResult.Cards);
			player.AddCardsToDeck(replaceResult.Cards, DeckPosition.Top);
		}
	}
	public class SecretChamber : Card
	{
		private Player.AttackedEventHandler _AttackHandler = null;

		public SecretChamber()
			: base("Secret Chamber", Category.Action | Category.Reaction, Source.Intrigue, Location.Kingdom, Group.ReactToAttack | Group.Defense | Group.PlusCoin | Group.Discard | Group.Terminal | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(2);
			this.Text = "Discard any number of cards.<nl/>+<coin>1</coin> per card discarded.<br/>When another player plays an Attack card, you may reveal this from your hand.<nl/>If you do, +2<nbsp/>Cards, then put 2 cards from your hand on top of your deck.";
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
		}

		public override void AddedTo(DeckLocation location, Player player)
		{
			base.AddedTo(location, player);

			if (location == DeckLocation.Hand)
			{
				if (_AttackHandler != null)
					player.Attacked -= _AttackHandler;

				_AttackHandler = new Player.AttackedEventHandler(player_Attacked);
				player.Attacked += _AttackHandler;
			}
		}

		internal override void player_Attacked(object sender, AttackedEventArgs e)
		{
			Player player = sender as Player;

			// Secret Chamber only protects against other attackers
			if (player == e.Attacker)
				return;

			// Only allow a single handling by a given card type
			if (player.Hand.Contains(this.PhysicalCard) && !e.HandledBy.Contains(TypeClass.SecretChamber) && !e.Revealable.ContainsKey(TypeClass.SecretChamber))
				e.Revealable[TypeClass.SecretChamber] = new AttackReaction(this, String.Format("Reveal {0}", this.PhysicalCard), player_RevealSecretChamber);
		}

		internal void player_RevealSecretChamber(Player player, ref AttackedEventArgs e)
		{
			player.AddCardInto(DeckLocation.Revealed, player.RetrieveCardFrom(DeckLocation.Hand, this.PhysicalCard));
			player.AddCardToHand(player.RetrieveCardFrom(DeckLocation.Revealed, this.PhysicalCard));

			player.Draw(2, DeckLocation.Hand);

			Choice replaceChoice = new Choice("Choose order of cards to put back on your deck", this, new CardCollection() { e.AttackCard }, player.Hand, player, true, 2, 2);
			ChoiceResult replaceResult = player.MakeChoice(replaceChoice);
			player.RetrieveCardsFrom(DeckLocation.Hand, replaceResult.Cards);
			player.AddCardsToDeck(replaceResult.Cards, DeckPosition.Top);

			e.HandledBy.Add(TypeClass.SecretChamber);

			// Attack isn't cancelled... it's just mitigated
		}

		public override void RemovedFrom(DeckLocation location, Player player)
		{
			base.RemovedFrom(location, player);
			if (_AttackHandler != null)
				player.Attacked -= _AttackHandler;
			_AttackHandler = null;
		}
	}
	public class ShantyTown : Card
	{
		public ShantyTown()
			: base("Shanty Town", Category.Action, Source.Intrigue, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.PlusMultipleActions | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(3);
			this.Benefit.Actions = 2;
			this.Text = "Reveal your hand.<nl/>If you have no Action cards in hand, +2<nbsp/>Cards.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			player.ReturnHand(player.RevealHand());

			if (player.Hand[Category.Action].Count == 0)
				player.ReceiveBenefit(this, new CardBenefit() { Cards = 2 });
		}
	}
	public class Steward : Card
	{
		public Steward()
			: base("Steward", Category.Action, Source.Intrigue, Location.Kingdom, Group.DeckReduction | Group.PlusCard | Group.PlusCoin | Group.Trash | Group.RemoveCurses | Group.Terminal | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(3);
			this.Text = "Choose one: +2<nbsp/>Cards; or +<coin>2</coin>; or trash 2 cards from your hand.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			CardBenefit benefit = new CardBenefit();

			Choice choice = new Choice("Choose one:", this, new CardCollection() { this }, new List<string>() { "+2<nbsp/>Cards", "+<coin>2</coin>", "Trash 2 cards from your hand" }, player);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Options.Contains("+2<nbsp/>Cards"))
				benefit.Cards = 2;
			else if (result.Options.Contains("+<coin>2</coin>"))
				benefit.Currency += new Coin(2);
			else
			{
				Choice choiceTrash = new Choice("Choose 2 cards to trash", this, player.Hand, player, false, 2, 2);
				ChoiceResult resultTrash = player.MakeChoice(choiceTrash);
				player.Trash(player.RetrieveCardsFrom(DeckLocation.Hand, resultTrash.Cards));
			}

			player.ReceiveBenefit(this, benefit);
		}
	}
	public class Swindler : Card
	{
		public Swindler()
			: base("Swindler", Category.Action | Category.Attack, Source.Intrigue, Location.Kingdom, Group.PlusCurses | Group.PlusCoin | Group.Trash | Group.Terminal)
		{
			this.BaseCost = new Cost(3);
			this.Benefit.Currency.Coin.Value = 2;
			this.Text = "Each other player trashes the top card of his deck and gains a card with the same cost that you choose.";
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
					Cost trashedCardCost = player._Game.ComputeCost(card);
					attackee.Trash(attackee.RetrieveCardFrom(DeckLocation.Revealed, card));
					SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply => supply.CanGain() && supply.CurrentCost == trashedCardCost);
					Choice choice = new Choice(String.Format("Choose a card for {0} to gain", attackee), this, gainableSupplies, attackee, false);
					ChoiceResult result = player.MakeChoice(choice);
					if (result.Supply != null)
						attackee.Gain(result.Supply);
				}
			}
		}
	}
	public class Torturer : Card
	{
		public Torturer()
			: base("Torturer", Category.Action | Category.Attack, Source.Intrigue, Location.Kingdom, Group.PlusCurses | Group.PlusCard | Group.Discard | Group.Terminal)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Cards = 3;
			this.Text = "Each other player chooses one: he discards 2 cards; or he gains a Curse card, putting it in his hand.";
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

				Choice choice = new Choice("Do you want to discard 2 cards or gain a Curse into your hand?", this, new CardCollection() { this }, new List<string>() { "Discard 2 cards", "Gain a Curse in hand" }, attackee);
				ChoiceResult result = attackee.MakeChoice(choice);
				if (result.Options[0] == "Discard 2 cards")
				{
					Choice choiceDiscard = new Choice("Choose 2 cards to discard", this, attackee.Hand, attackee, false, 2, 2);
					ChoiceResult discards = attackee.MakeChoice(choiceDiscard);
					attackee.Discard(DeckLocation.Hand, discards.Cards);
				}
				else
				{
					attackee.Gain(player._Game.Table.Curse, DeckLocation.Hand, DeckPosition.Bottom);
				}
			}
		}
	}
	public class TradingPost : Card
	{
		public TradingPost()
			: base("Trading Post", Category.Action, Source.Intrigue, Location.Kingdom, Group.DeckReduction | Group.Gain | Group.Trash | Group.RemoveCurses | Group.Terminal)
		{
			this.BaseCost = new Cost(5);
			this.Text = "Trash 2 cards from your hand.<nl/>If you do, gain a Silver card; put it into your hand.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choiceTrash = new Choice("Choose 2 cards to trash", this, player.Hand, player, false, 2, 2);
			ChoiceResult resultTrash = player.MakeChoice(choiceTrash);

			player.Trash(player.RetrieveCardsFrom(DeckLocation.Hand, resultTrash.Cards));

			if (resultTrash.Cards.Count == 2)
				player.Gain(player._Game.Table.Silver, DeckLocation.Hand, DeckPosition.Bottom);
		}
	}
	public class Tribute : Card
	{
		public Tribute()
			: base("Tribute", Category.Action, Source.Intrigue, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.PlusMultipleActions | Group.PlusCoin | Group.Discard | Group.AffectOthers | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(5);
			this.Text = "The player to your left reveals then discards the top 2 cards of his deck.<nl/>For each differently named card revealed, if it is an...<nl/>Action Card, +2<nbsp/>Actions<nl/>Treasure Card, +<coin>2</coin><nl/>Victory Card, +2<nbsp/>Cards";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			// Get the player to my left
			Player playerToLeft = player._Game.GetPlayerFromIndex(player, 1);
			playerToLeft.Draw(2, DeckLocation.Revealed);
			String previousCardName = String.Empty;

			CardBenefit benefit = new CardBenefit();
			foreach (Card card in playerToLeft.Revealed)
			{
				if (card.Name != previousCardName)
				{
					if ((card.Category & Category.Action) == Category.Action)
						benefit.Actions += 2;
					if ((card.Category & Category.Treasure) == Category.Treasure)
						benefit.Currency += new Coin(2);
					if ((card.Category & Category.Victory) == Category.Victory)
						benefit.Cards += 2;
				}
				previousCardName = card.Name;
			}
			playerToLeft.DiscardRevealed();
			player.ReceiveBenefit(this, benefit);
		}
	}
	public class Upgrade : Card
	{
		public Upgrade()
			: base("Upgrade", Category.Action, Source.Intrigue, Location.Kingdom, Group.DeckReduction | Group.PlusCard | Group.PlusAction | Group.Gain | Group.Trash | Group.RemoveCurses)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 1;
			this.Text = "Trash a card from your hand.<nl/>Gain a card costing exactly <coin>1</coin> more than it.";
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
				SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply => supply.CanGain() && supply.CurrentCost == (trashedCardCost + new Coin(1)));
				Choice choice = new Choice("Gain a card", this, gainableSupplies, player, false);
				ChoiceResult result = player.MakeChoice(choice);
				if (result.Supply != null)
					player.Gain(result.Supply);
			}
		}
	}
	public class WishingWell : Card
	{
		public WishingWell()
			: base("Wishing Well", Category.Action, Source.Intrigue, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(3);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 1;
			this.Text = "Name a card.  Reveal the top card of your deck.  If it's the named card, put it into your hand.";
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
			ICard wishedCard = null;
			if (result.Supply != null)
				wishedCard = result.Supply;
			else
				wishedCard = result.Cards[0];

			player._Game.SendMessage(player, this, wishedCard);
			if (player.CanDraw)
			{
				player.Draw(DeckLocation.Revealed);
				if (player.Revealed[wishedCard.CardType].Count > 0)
				{
					player.AddCardsToHand(DeckLocation.Revealed);
				}
				else
				{
					player.AddCardsToDeck(player.RetrieveCardsFrom(DeckLocation.Revealed), DeckPosition.Top);
				}
			}
		}
	}
}
