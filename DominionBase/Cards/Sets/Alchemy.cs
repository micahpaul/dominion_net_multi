using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DominionBase.Currencies;
using DominionBase.Piles;
using DominionBase.Players;

namespace DominionBase.Cards.Alchemy
{
	public static class TypeClass
	{
		public static Type Alchemist = typeof(Alchemist);
		public static Type Apothecary = typeof(Apothecary);
		public static Type Apprentice = typeof(Apprentice);
		public static Type Familiar = typeof(Familiar);
		public static Type Golem = typeof(Golem);
		public static Type Herbalist = typeof(Herbalist);
		public static Type PhilosophersStone = typeof(PhilosophersStone);
		public static Type Potion = typeof(Potion);
		public static Type ScryingPool = typeof(ScryingPool);
		public static Type Transmute = typeof(Transmute);
		public static Type University = typeof(University);
		public static Type Vineyard = typeof(Vineyard);
	}

	public class Alchemist : Card
	{
		private Player.CardsDiscardingEventHandler _CardsDiscardingEventHandler = null;

		public Alchemist()
			: base("Alchemist", Category.Action, Source.Alchemy, Location.Kingdom, Group.CardOrdering | Group.PlusCard | Group.PlusAction)
		{
			this.BaseCost = new Cost(3, 1);
			this.Benefit.Cards = 2;
			this.Benefit.Actions = 1;
			this.Text = "<br/>When you discard this from play, you may put this on top of your deck if you have a Potion in play.";

			this.OwnerChanged += new OwnerChangedEventHandler(Alchemist_OwnerChanged);
		}

		internal override void TearDown()
		{
			Alchemist_OwnerChanged(this, new OwnerChangedEventArgs(this.Owner, null));

			base.TearDown();

			this.OwnerChanged -= new OwnerChangedEventHandler(Alchemist_OwnerChanged);
		}

		void Alchemist_OwnerChanged(object sender, OwnerChangedEventArgs e)
		{
			if (_CardsDiscardingEventHandler != null && e.OldOwner != null)
			{
				e.OldOwner.CardsDiscarding -= _CardsDiscardingEventHandler;
				_CardsDiscardingEventHandler = null;
			}

			if (e.NewOwner != null)
			{
				_CardsDiscardingEventHandler = new Player.CardsDiscardingEventHandler(player_CardsDiscarding);
				e.NewOwner.CardsDiscarding += _CardsDiscardingEventHandler;
			}
		}

		void player_CardsDiscarding(object sender, CardsDiscardEventArgs e)
		{
			if (!e.Cards.Contains(this.PhysicalCard) || e.GetAction(TypeClass.Alchemist) != null ||
				(e.FromLocation != DeckLocation.InPlay && e.FromLocation != DeckLocation.SetAside && e.FromLocation != DeckLocation.InPlayAndSetAside))
				return;

			if (e.Cards.Any(c => c.CardType == TypeClass.Potion))
				e.AddAction(TypeClass.Alchemist, new CardsDiscardAction(sender as Player, this, String.Format("Put {0} on your deck", this.PhysicalCard), player_Action, false));
		}

		internal void player_Action(Player player, ref CardsDiscardEventArgs e)
		{
			e.Cards.Remove(this.PhysicalCard);
			Card thisCard = null;
			if (player.InPlay.Contains(this.PhysicalCard))
				thisCard = player.RetrieveCardFrom(DeckLocation.InPlay, this.PhysicalCard);
			else
				thisCard = player.RetrieveCardFrom(DeckLocation.SetAside, this.PhysicalCard);
			player.AddCardToDeck(thisCard, DeckPosition.Top);
		}
	}
	public class Apothecary : Card
	{
		public Apothecary()
			: base("Apothecary", Category.Action, Source.Alchemy, Location.Kingdom, Group.CardOrdering | Group.PlusCard | Group.PlusAction)
		{
			this.BaseCost = new Cost(2, 1);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 1;
			this.Text = "Reveal the top 4 cards of your deck.  Put the revealed Coppers and Potions into your hand.  Put the other cards on top of your deck in any order.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			CardCollection newCards = player.Draw(4, DeckLocation.Revealed);

			player.AddCardsToHand(player.RetrieveCardsFrom(DeckLocation.Revealed,
				c => c.CardType == Universal.TypeClass.Copper || c.CardType == Alchemy.TypeClass.Potion));

			Choice replaceChoice = new Choice("Choose order of cards to put back on your deck", this, player.Revealed, player, true, player.Revealed.Count, player.Revealed.Count);
			ChoiceResult replaceResult = player.MakeChoice(replaceChoice);
			player.RetrieveCardsFrom(DeckLocation.Revealed, replaceResult.Cards);
			player.AddCardsToDeck(replaceResult.Cards, DeckPosition.Top);
		}
	}
	public class Apprentice : Card
	{
		public Apprentice()
			: base("Apprentice", Category.Action, Source.Alchemy, Location.Kingdom, Group.DeckReduction | Group.PlusCard | Group.PlusAction | Group.Trash | Group.RemoveCurses | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Actions = 1;
			this.Text = "Trash a card from your hand.<nl/>+1<nbsp/>Card per <coin/> it costs.<nl/>+2<nbsp/>Cards if it has <potion/> in its cost.";
		}

		public override void Play(Player player)
		{
			base.Play(player);
			Choice choice = new Choice("Choose a card to trash", this, player.Hand, player, false, 1, 1);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Cards.Count > 0)
			{
				player.Trash(player.RetrieveCardFrom(DeckLocation.Hand, result.Cards[0]));

				Cost cardCost = player._Game.ComputeCost(result.Cards[0]);
				int toDraw = cardCost.Coin.Value;
				if (cardCost.Potion.Value > 0)
					toDraw += 2;
				player.ReceiveBenefit(this, new CardBenefit() { Cards = toDraw });
			}
		}
	}
	public class Familiar : Card
	{
		public Familiar()
			: base("Familiar", Category.Action | Category.Attack, Source.Alchemy, Location.Kingdom, Group.PlusCurses | Group.PlusCard | Group.PlusAction)
		{
			this.BaseCost = new Cost(3, 1);
			this.Benefit.Actions = 1;
			this.Benefit.Cards = 1;
			this.Text = "Each other player gains a Curse.";
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

				attackee.Gain(player._Game.Table.Curse);
			}
		}
	}
	public class Golem : Card
	{
		public Golem()
			: base("Golem", Category.Action, Source.Alchemy, Location.Kingdom)
		{
			this.BaseCost = new Cost(4, 1);
			this.Text = "Reveal cards from your deck until you reveal 2 Action cards other than Golem cards.<nl/>Discard the other cards, then play the Action cards in either order.";
		}

		public override void Play(Player player)
		{
			base.Play(player);
			// We're looking for 2 non-Golem Action cards
			player.BeginDrawing();
			while (player.Revealed[Category.Action].Count(c => c.CardType != Cards.Alchemy.TypeClass.Golem) < 2 && player.CanDraw)
				player.Draw(DeckLocation.Revealed);

			player.EndDrawing();

			player.Revealed.BeginChanges();
			CardCollection actions = player.Revealed[c => (c.Category & Category.Action) == Category.Action && c.CardType != Cards.Alchemy.TypeClass.Golem];
			player.DiscardRevealed(c => !actions.Contains(c));
			player.Revealed.EndChanges();

			CardCollection cardsToPlay = player.RetrieveCardsFrom(DeckLocation.Revealed);
			Choice choice = new Choice("Which card would you like to play first?", this, actions, player);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Cards.Count > 0)
			{
				actions.Remove(result.Cards[0]);

				// Play the first (selected) one
				player.AddCardInto(DeckLocation.Private, result.Cards[0]);
				player.Actions++;
				player.PlayCardInternal(result.Cards[0], "first");
			}
			else
				player.PlayNothing("first");

			if (actions.Count > 0)
			{
				// Play the other one
				player.AddCardInto(DeckLocation.Private, actions[0]);
				player.Actions++;
				player.PlayCardInternal(actions[0], "second");
			}
			else
				player.PlayNothing("second");
		}
	}
	public class Herbalist : Card
	{
		private Player.CardsDiscardingEventHandler _CardsDiscardingEventHandler = null;

		public Herbalist()
			: base("Herbalist", Category.Action, Source.Alchemy, Location.Kingdom, Group.CardOrdering | Group.PlusCoin | Group.PlusBuy | Group.Terminal)
		{
			this.BaseCost = new Cost(2);
			this.Benefit.Buys = 1;
			this.Benefit.Currency.Coin.Value = 1;
			this.Text = "When you discard this from play, you may put one of your Treasures from play on top of your deck.";

			this.OwnerChanged += new OwnerChangedEventHandler(Herbalist_OwnerChanged);
		}

		protected override Boolean AllowUndo { get { return true; } }

		internal override void TearDown()
		{
			Herbalist_OwnerChanged(this, new OwnerChangedEventArgs(this.Owner, null));

			base.TearDown();

			this.OwnerChanged -= new OwnerChangedEventHandler(Herbalist_OwnerChanged);
		}

		void Herbalist_OwnerChanged(object sender, OwnerChangedEventArgs e)
		{
			if (_CardsDiscardingEventHandler != null && e.OldOwner != null)
			{
				e.OldOwner.CardsDiscarding -= _CardsDiscardingEventHandler;
				_CardsDiscardingEventHandler = null;
			}

			if (e.NewOwner != null)
			{
				_CardsDiscardingEventHandler = new Player.CardsDiscardingEventHandler(player_CardsDiscarding);
				e.NewOwner.CardsDiscarding += _CardsDiscardingEventHandler;
			}
		}

		void player_CardsDiscarding(object sender, CardsDiscardEventArgs e)
		{
			if (!e.Cards.Contains(this.PhysicalCard) || e.GetAction(TypeClass.Herbalist) != null || e.HandledBy.Contains(this) ||
				(e.FromLocation != DeckLocation.InPlay && e.FromLocation != DeckLocation.SetAside && e.FromLocation != DeckLocation.InPlayAndSetAside))
				return;

			if (e.Cards.Any(c => (c.Category & Cards.Category.Treasure) == Cards.Category.Treasure))
				e.AddAction(TypeClass.Herbalist, new CardsDiscardAction(sender as Player, this, "Put a Treasure on your deck", player_Action, false));
		}

		internal void player_Action(Player player, ref CardsDiscardEventArgs e)
		{
			Choice choice = new Choice("Select a treasure to place on your deck", this, e.Cards.Where(c => (c.Category & Category.Treasure) == Cards.Category.Treasure), player);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Cards.Count > 0)
			{
				e.Cards.Remove(result.Cards[0]);
				if (player.InPlay.Contains(result.Cards[0]))
					player.RetrieveCardFrom(DeckLocation.InPlay, result.Cards[0]);
				else
					player.RetrieveCardFrom(DeckLocation.SetAside, result.Cards[0]);
				player.AddCardToDeck(result.Cards[0], DeckPosition.Top);
			}

			e.HandledBy.Add(this);
		}
	}
	public class PhilosophersStone : Card
	{
		public PhilosophersStone()
			: base("Philosopher's Stone", Category.Treasure, Source.Alchemy, Location.Kingdom, Group.PlusCoin | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(3, 1);
			this.Text = "When you play this, count your deck and discard pile.<nl/>Worth <coin>1</coin> per 5 cards total between them (rounded down).";
			this.Benefit.Currency.Coin.IsVariable = true;
		}

		public override void Play(Player player)
		{
			base.Play(player);

			CardBenefit benefit = new CardBenefit();
			benefit.Currency += new Coin((player.DiscardPile.Count + player.DrawPile.Count) / 5);
			benefit.FlavorText = String.Format(" (Deck: {0}, Discard: {1})", Utilities.StringUtility.Plural("card", player.DrawPile.Count), Utilities.StringUtility.Plural("card", player.DiscardPile.Count));
			player.ReceiveBenefit(this, benefit);
		}
	}
	public class Potion : Card
	{
		public Potion()
			: base("Potion", Category.Treasure, Source.Alchemy, Location.General)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Currency.Potion.Value = 1;
		}

		protected override Boolean AllowUndo { get { return true; } }
	}
	public class ScryingPool : Card
	{
		public ScryingPool()
			: base("Scrying Pool", Category.Action | Category.Attack, Source.Alchemy, Location.Kingdom, Group.CardOrdering | Group.PlusCard | Group.PlusAction | Group.Discard)
		{
			this.BaseCost = new Cost(2, 1);
			this.Benefit.Actions = 1;
			this.Text = "Each player (including you) reveals the top card of his deck and either discards it or puts it back, your choice. Then reveal cards from the top of your deck until you reveal one that is not an Action.<nl/>Put all of your revealed cards into your hand.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

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
					Card card = attackee.Draw(DeckLocation.Revealed);
					Choice choice = new Choice(String.Format("Do you want to discard {0}'s {1} or put it back on top?", attackee.Name, card.Name), this, new CardCollection() { card }, new List<string>() { "Discard", "Put it back" }, attackee);
					ChoiceResult result = player.MakeChoice(choice);
					if (result.Options[0] == "Discard")
						attackee.DiscardRevealed();
					else
						attackee.AddCardsToDeck(attackee.RetrieveCardsFrom(DeckLocation.Revealed), DeckPosition.Top);
				}
			}

			player.BeginDrawing();
			// Keep flipping cards until we hit a non-Action card, thus making the counts different
			while (player.CanDraw && player.Revealed[Category.Action].Count == player.Revealed.Count)
				player.Draw(DeckLocation.Revealed);

			player.EndDrawing();
			player.AddCardsToHand(DeckLocation.Revealed);
		}
	}
	public class Transmute : Card
	{
		public Transmute()
			: base("Transmute", Category.Action, Source.Alchemy, Location.Kingdom, Group.Gain | Group.Trash | Group.RemoveCurses | Group.Terminal)
		{
			this.BaseCost = new Cost(0, 1);
			this.Text = "Trash a card from your hand.<nl/>If it's an...<nl/>Action card, gain a Duchy<nl/>Treasure card, gain a Transmute<nl/>Victory card, gain a Gold";
		}

		public override void Play(Player player)
		{
			Choice choice = new Choice("Trash a card.", this, player.Hand, player);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Cards.Count > 0)
			{
				Card trash = player.RetrieveCardFrom(DeckLocation.Hand, result.Cards[0]);

				player.Trash(trash);
				if ((trash.Category & Category.Action) == Category.Action)
					player.Gain(player._Game.Table.Duchy);
				if ((trash.Category & Category.Treasure) == Category.Treasure)
					player.Gain(player._Game.Table[Cards.Alchemy.TypeClass.Transmute]);
				if ((trash.Category & Category.Victory) == Category.Victory)
					player.Gain(player._Game.Table.Gold);
			}

			base.Play(player);
		}
	}
	public class University : Card
	{
		public University()
			: base("University", Category.Action, Source.Alchemy, Location.Kingdom, Group.PlusAction | Group.PlusMultipleActions)
		{
			this.BaseCost = new Cost(2, 1);
			this.Benefit.Actions = 2;
			this.Text = "You may gain an Action card costing up to <coin>5</coin>.";
		}

		public override void Play(Player player)
		{
			base.Play(player);
			SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(
				supply => supply.CanGain() && 
					((supply.Category & Cards.Category.Action) == Cards.Category.Action) && 
					supply.CurrentCost <= new Coin(5));
			Choice choice = new Choice("You may gain an Action card costing up to <coin>5</coin>", this, gainableSupplies, player, true);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Supply != null)
				player.Gain(result.Supply);
		}
	}
	public class Vineyard : Card
	{
		public Vineyard()
			: base("Vineyard", Category.Victory, Source.Alchemy, Location.Kingdom, Group.VariableVPs)
		{
			this.BaseCost = new Cost(0, 1);
			this.Text = "Worth <vp>1</vp> for every 3 Action cards in your deck (rounded down).";
		}

		public override int GetVictoryPoints(IEnumerable<Card> cards)
		{
			return base.GetVictoryPoints(cards) +
				cards.Count(c => (c.Category & Category.Action) == Category.Action) / 3;
		}
	}
}
