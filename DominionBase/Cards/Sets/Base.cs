using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DominionBase.Currencies;
using DominionBase.Piles;
using DominionBase.Players;

namespace DominionBase.Cards.Base
{
	public static class TypeClass
	{
		public static Type Adventurer = typeof(Adventurer);
		public static Type Bureaucrat = typeof(Bureaucrat);
		public static Type Cellar = typeof(Cellar);
		public static Type Chancellor = typeof(Chancellor);
		public static Type Chapel = typeof(Chapel);
		public static Type CouncilRoom = typeof(CouncilRoom);
		public static Type Feast = typeof(Feast);
		public static Type Festival = typeof(Festival);
		public static Type Gardens = typeof(Gardens);
		public static Type Laboratory = typeof(Laboratory);
		public static Type Library = typeof(Library);
		public static Type Market = typeof(Market);
		public static Type Militia = typeof(Militia);
		public static Type Mine = typeof(Mine);
		public static Type Moat = typeof(Moat);
		public static Type Moneylender = typeof(Moneylender);
		public static Type Remodel = typeof(Remodel);
		public static Type Smithy = typeof(Smithy);
		public static Type Spy = typeof(Spy);
		public static Type Thief = typeof(Thief);
		public static Type ThroneRoom = typeof(ThroneRoom);
		public static Type Village = typeof(Village);
		public static Type Witch = typeof(Witch);
		public static Type Woodcutter = typeof(Woodcutter);
		public static Type Workshop = typeof(Workshop);
	}

	public class Adventurer : Card
	{
		public Adventurer()
			: base("Adventurer", Category.Action, Source.Base, Location.Kingdom, Group.PlusCard | Group.Discard | Group.Terminal)
		{
			this.BaseCost = new Cost(6);
			this.Text = "Reveal cards from your deck until you reveal 2 Treasure cards.<nl/>Put those Treasure cards into your hand and discard the other revealed cards.";
		}

		public override void Play(Player player)
		{
			base.Play(player);
			player.BeginDrawing();
			while (player.Revealed[Category.Treasure].Count < 2 && player.CanDraw)
				player.Draw(DeckLocation.Revealed);

			player.EndDrawing();

			player.AddCardsToHand(player.RetrieveCardsFrom(DeckLocation.Revealed, c => (c.Category & Category.Treasure) == Category.Treasure));

			player.DiscardRevealed();
		}
	}
	public class Bureaucrat : Card
	{
		public Bureaucrat()
			: base("Bureaucrat", Category.Action | Category.Attack, Source.Base, Location.Kingdom, Group.CardOrdering | Group.Gain | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.Text = "Gain a Silver card;<nl/>put it on top of your deck.  Each other player reveals a Victory card from his hand and puts it on his deck (or reveals a hand with no Victory cards).";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			player.Gain(player._Game.Table.Silver, DeckLocation.Deck, DeckPosition.Top);

			// Perform attack on every player (including you)
			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			enumerator.MoveNext();
			while (enumerator.MoveNext())
			{
				Player attackee = enumerator.Current;
				// Skip if the attack is blocked (Moat, Lighthouse, etc.)
				if (this.IsAttackBlocked[attackee])
					continue;

				if (attackee.Hand[Category.Victory].Count == 0)
				{
					attackee.ReturnHand(attackee.RevealHand());
				}
				else
				{
					Choice replaceChoice = new Choice("Choose a card to put back on your deck", this, attackee.Hand[Category.Victory], attackee);
					ChoiceResult replaceResult = attackee.MakeChoice(replaceChoice);
					if (replaceResult.Cards.Count > 0)
					{
						Card returnCard = attackee.RetrieveCardFrom(DeckLocation.Hand, replaceResult.Cards[0]);
						attackee.AddCardInto(DeckLocation.Revealed, returnCard);
						attackee.AddCardToDeck(attackee.RetrieveCardFrom(DeckLocation.Revealed, returnCard), DeckPosition.Top);
					}
				}
			}
		}
	}
	public class Cellar : Card
	{
		public Cellar()
			: base("Cellar", Category.Action, Source.Base, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.Discard | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(2);
			this.Benefit.Actions = 1;
			this.Text = "Discard any number of cards.<nl/>+1<nbsp/>Card per card discarded.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choiceDiscard = new Choice("Choose cards to discard", this, player.Hand, player, false, 0, player.Hand.Count);
			ChoiceResult resultDiscard = player.MakeChoice(choiceDiscard);
			player.Discard(DeckLocation.Hand, resultDiscard.Cards);

			player.ReceiveBenefit(this, new CardBenefit() { Cards = resultDiscard.Cards.Count });
		}
	}
	public class Chancellor : Card
	{
		public Chancellor()
			: base("Chancellor", Category.Action, Source.Base, Location.Kingdom, Group.CardOrdering | Group.PlusCoin | Group.Terminal)
		{
			this.BaseCost = new Cost(3);
			this.Benefit.Currency.Coin.Value = 2;
			this.Text = "You may immediately put your deck into your discard pile.";
		}

		public override void Play(Player player)
		{
			base.Play(player);
			Choice choice = Choice.CreateYesNoChoice("You may immediately put your deck into your discard pile.", this, this, player, null);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Options[0] == "Yes")
			{
				player._Game.SendMessage(player, this);
				CardCollection cc = player.RetrieveCardsFrom(DeckLocation.Deck);
				player.AddCardsInto(DeckLocation.Discard, cc);
			}
		}
	}
	public class Chapel : Card
	{
		public Chapel()
			: base("Chapel", Category.Action, Source.Base, Location.Kingdom, Group.DeckReduction | Group.Trash | Group.RemoveCurses | Group.Terminal)
		{
			this.BaseCost = new Cost(2);
			this.Text = "Trash up to 4 cards from your hand.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choiceTrash = new Choice("Choose up to 4 cards to trash", this, player.Hand, player, false, 0, 4);
			ChoiceResult resultTrash = player.MakeChoice(choiceTrash);
			player.Trash(player.RetrieveCardsFrom(DeckLocation.Hand, resultTrash.Cards));
		}
	}
	public class CouncilRoom : Card
	{
		public CouncilRoom()
			: base("Council Room", Category.Action, Source.Base, Location.Kingdom, Group.PlusCard | Group.PlusBuy | Group.AffectOthers | Group.Terminal)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Cards = 4;
			this.Benefit.Buys = 1;
			this.Text = "Each other player draws a card.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			CardBenefit benefit = new CardBenefit() { Cards = 1 };

			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			enumerator.MoveNext();
			while (enumerator.MoveNext())
			{
				enumerator.Current.ReceiveBenefit(this, benefit);
			}
		}
	}
	public class Feast : Card
	{
		public Feast()
			: base("Feast", Category.Action, Source.Base, Location.Kingdom, Group.Gain | Group.Trash | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.Text = "Trash this card.<nl/>Gain a card costing up to <coin>5</coin>.";
		}

		public override void Play(Player player)
		{
			base.Play(player);
			if (player.InPlay.Contains(this.PhysicalCard))
			{
				player.Trash(player.RetrieveCardFrom(DeckLocation.InPlay, this.PhysicalCard));
			}
			SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply => supply.CanGain() && supply.CurrentCost <= new Coin(5));
			Choice choice = new Choice("Gain a card costing up to <coin>5</coin>", this, gainableSupplies, player, false);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Supply != null)
				player.Gain(result.Supply);
		}
	}
	public class Festival : Card
	{
		public Festival()
			: base("Festival", Category.Action, Source.Base, Location.Kingdom, Group.PlusAction | Group.PlusMultipleActions | Group.PlusCoin | Group.PlusBuy)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Actions = 2;
			this.Benefit.Buys = 1;
			this.Benefit.Currency.Coin.Value = 2;
		}

		protected override Boolean AllowUndo { get { return true; } }
	}
	public class Gardens : Card
	{
		public Gardens()
			: base("Gardens", Category.Victory, Source.Base, Location.Kingdom, Group.VariableVPs)
		{
			this.BaseCost = new Cost(4);
			this.Text = "Worth <vp>1</vp><nl/>for every 10 cards<nl/>in your deck (rounded down).";
		}

		public override int GetVictoryPoints(IEnumerable<Card> cards)
		{
			return base.GetVictoryPoints(cards) + cards.Count() / 10;
		}
	}
	public class Laboratory : Card
	{
		public Laboratory()
			: base("Laboratory", Category.Action, Source.Base, Location.Kingdom, Group.PlusCard | Group.PlusAction)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Actions = 1;
			this.Benefit.Cards = 2;
		}
	}
	public class Library : Card
	{
		public Library()
			: base("Library", Category.Action, Source.Base, Location.Kingdom, Group.PlusCard | Group.Discard | Group.Terminal | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(5);
			this.Text = "Draw until you have 7 cards in hand.  You may set aside any Action cards drawn this way, as you draw them; discard the set aside cards after you finish drawing.";
		}

		public override void Play(Player player)
		{
			base.Play(player);
			while (player.Hand.Count < 7 && player.CanDraw)
			{
				Card card = player.Draw(DeckLocation.Private);
				if ((card.Category & Category.Action) == Category.Action)
				{
					Choice choice = Choice.CreateYesNoChoice(String.Format("Would you like to set aside {0}?", card.Name), this, card, player, null);
					ChoiceResult result = player.MakeChoice(choice);
					if (result.Options[0] == "Yes")
					{
						player.AddCardInto(DeckLocation.Revealed, player.RetrieveCardFrom(DeckLocation.Private, card));
					}
					else if (result.Options[0] == "No")
					{
						player.AddCardToHand(player.RetrieveCardFrom(DeckLocation.Private, card));
					}
				}
				else
				{
					player.AddCardToHand(player.RetrieveCardFrom(DeckLocation.Private, card));
				}
			}
			player.DiscardRevealed();
		}
	}
	public class Market : Card
	{
		public Market()
			: base("Market", Category.Action, Source.Base, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.PlusCoin | Group.PlusBuy)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Actions = 1;
			this.Benefit.Cards = 1;
			this.Benefit.Buys = 1;
			this.Benefit.Currency.Coin.Value = 1;
		}
	}
	public class Militia : Card
	{
		public Militia()
			: base("Militia", Category.Action | Category.Attack, Source.Base, Location.Kingdom, Group.PlusCoin | Group.Discard | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Currency.Coin.Value = 2;
			this.Text = "Each other player discards down to 3 cards in his hand.";
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
	}
	public class Mine : Card
	{
		public Mine()
			: base("Mine", Category.Action, Source.Base, Location.Kingdom, Group.Gain | Group.Trash | Group.Terminal)
		{
			this.BaseCost = new Cost(5);
			this.Text = "Trash a Treasure card from your hand.<nl/>Gain a Treasure card costing up to <coin>3</coin> more; put it into your hand.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choiceTrash = new Choice("Choose a card to trash", this, player.Hand[Cards.Category.Treasure], player);
			ChoiceResult resultTrash = player.MakeChoice(choiceTrash);
			player.Trash(player.RetrieveCardsFrom(DeckLocation.Hand, resultTrash.Cards));

			if (resultTrash.Cards.Count > 0)
			{
				Cost trashedCardCost = player._Game.ComputeCost(resultTrash.Cards[0]);
				SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(
					supply => supply.CanGain() && 
						((supply.Category & Cards.Category.Treasure) == Cards.Category.Treasure) &&
						supply.CurrentCost <= (trashedCardCost + new Coin(3)));
				Choice choice = new Choice("Gain a card", this, gainableSupplies, player, false);
				ChoiceResult result = player.MakeChoice(choice);
				if (result.Supply != null)
					player.Gain(result.Supply, DeckLocation.Hand, DeckPosition.Automatic);
			}
		}
	}
	public class Moat : Card
	{
		private Player.AttackedEventHandler _AttackHandler = null;

		public Moat()
			: base("Moat", Category.Action | Category.Reaction, Source.Base, Location.Kingdom, Group.ReactToAttack | Group.Defense | Group.PlusCard | Group.Terminal)
		{
			this.BaseCost = new Cost(2);
			this.Benefit.Cards = 2;
			this.Text = "<br/>When another player plays an Attack card, you may reveal this from your hand.  If you do, you are unaffected by that Attack.";
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

			// Moat only protects against other attackers
			if (player == e.Attacker)
				return;

			// Already been cancelled -- don't need to process this one
			if (player.Hand.Contains(this.PhysicalCard) && !e.Cancelled && !e.Revealable.ContainsKey(TypeClass.Moat))
				e.Revealable[TypeClass.Moat] = new AttackReaction(this, String.Format("Reveal {0}", this.PhysicalCard), player_RevealMoat);
		}

		internal void player_RevealMoat(Player player, ref AttackedEventArgs e)
		{
			player.AddCardInto(DeckLocation.Revealed, player.RetrieveCardFrom(DeckLocation.Hand, this.PhysicalCard));
			e.Cancelled = true;
			player.AddCardInto(DeckLocation.Hand, player.RetrieveCardFrom(DeckLocation.Revealed, this.PhysicalCard));
			e.HandledBy.Add(TypeClass.Moat);
		}

		public override void RemovedFrom(DeckLocation location, Player player)
		{
			base.RemovedFrom(location, player);
			if (_AttackHandler != null)
				player.Attacked -= _AttackHandler;
			_AttackHandler = null;
		}
	}
	public class Moneylender : Card
	{
		public Moneylender()
			: base("Moneylender", Category.Action, Source.Base, Location.Kingdom, Group.DeckReduction | Group.PlusCoin | Group.Trash | Group.Terminal | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(4);
			this.Text = "Trash a Copper card from your hand.<nl/>If you do, +<coin>3</coin>.";
		}

		public override void Play(Player player)
		{
			base.Play(player);
			CardCollection singleCopper = player.RetrieveCardsFrom(DeckLocation.Hand, Cards.Universal.TypeClass.Copper, 1);
			if (singleCopper.Count > 0)
			{
				player.Trash(singleCopper[0]);
				CardBenefit benefit = new CardBenefit();
				benefit.Currency += new Coin(3);
				player.ReceiveBenefit(this, benefit);
			}
		}
	}
	public class Remodel : Card
	{
		public Remodel()
			: base("Remodel", Category.Action, Source.Base, Location.Kingdom, Group.Gain | Group.Trash | Group.RemoveCurses | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.Text = "Trash a card from your hand.<nl/>Gain a card costing up to <coin>2</coin> more than the trashed card.";
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
				SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply => supply.CanGain() && supply.CurrentCost <= (trashedCardCost + new Coin(2)));
				Choice choice = new Choice("Gain a card", this, gainableSupplies, player, false);
				ChoiceResult result = player.MakeChoice(choice);
				if (result.Supply != null)
					player.Gain(result.Supply);
			}
		}
	}
	public class Smithy : Card
	{
		public Smithy()
			: base("Smithy", Category.Action, Source.Base, Location.Kingdom, Group.PlusCard | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Cards = 3;
		}
	}
	public class Spy : Card
	{
		public Spy()
			: base("Spy", Category.Action | Category.Attack, Source.Base, Location.Kingdom, Group.CardOrdering | Group.PlusCard | Group.PlusAction | Group.Discard)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 1;
			this.Text = "Each player (including you) reveals the top card of his deck and either discards it or puts it back, your choice.";
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
		}
	}
	public class Thief : Card
	{
		public Thief()
			: base("Thief", Category.Action | Category.Attack, Source.Base, Location.Kingdom, Group.Gain | Group.Trash | Group.Discard | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.Text = "Each other player reveals the top 2 cards of his deck.<nl/>If they revealed any Treasure cards, they trash one of them that you choose.<nl/>You may gain any or all of these trashed cards.  They discard the other revealed cards.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			// Perform attack on every player
			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			enumerator.MoveNext();
			CardCollection trashed = new CardCollection();
			while (enumerator.MoveNext())
			{
				Player attackee = enumerator.Current;
				// Skip if the attack is blocked (Moat, Lighthouse, etc.)
				if (this.IsAttackBlocked[attackee])
					continue;

				attackee.Draw(2, DeckLocation.Revealed);

				CardCollection treasures = attackee.Revealed[Category.Treasure];

				Choice choice = new Choice(String.Format("Choose a Treasure card of {0} to trash", attackee), this, treasures, attackee);
				ChoiceResult result = player.MakeChoice(choice);
				if (result.Cards.Count > 0)
				{
					Card trashCard = attackee.RetrieveCardFrom(DeckLocation.Revealed, result.Cards[0]);
					attackee.Trash(trashCard);
					trashed.Add(trashCard);
				}

				attackee.DiscardRevealed();
			}

			Choice keepCards = new Choice("Choose which cards you'd like to gain from being trashed.", this, trashed, player, true, 0, trashed.Count);
			ChoiceResult keptCards = player.MakeChoice(keepCards);
			foreach (Card card in keptCards.Cards)
				player.Gain(player._Game.Table.Trash, card);
		}
	}
	public class ThroneRoom : Card
	{
		private Boolean _CanCleanUp = true;

		public ThroneRoom()
			: base("Throne Room", Category.Action, Source.Base, Location.Kingdom)
		{
			this.BaseCost = new Cost(4);
			this.Text = "Choose an Action card in your hand.<nl/>Play it twice";
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

			Choice choice = new Choice(String.Format("Choose an Action card to play twice", player), this, player.Hand[Cards.Category.Action], player);
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

				this._CanCleanUp = logicalCard.CanCleanUp;
			}
			else
				player.PlayNothing();
		}

		protected override void ModifyDuration(Player player, Card card)
		{
			base.ModifyDuration(player, card);
			base.ModifyDuration(player, card);
		}
	}
	public class Village : Card
	{
		public Village()
			: base("Village", Category.Action, Source.Base, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.PlusMultipleActions)
		{
			this.BaseCost = new Cost(3);
			this.Benefit.Actions = 2;
			this.Benefit.Cards = 1;
		}
	}
	public class Witch : Card
	{
		public Witch()
			: base("Witch", Category.Action | Category.Attack, Source.Base, Location.Kingdom, Group.PlusCurses | Group.PlusCard | Group.Terminal)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Cards = 2;
			this.Text = "Each other player gains a Curse card.";
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
	public class Woodcutter : Card
	{
		public Woodcutter()
			: base("Woodcutter", Category.Action, Source.Base, Location.Kingdom, Group.PlusCoin | Group.PlusBuy | Group.Terminal)
		{
			this.BaseCost = new Cost(3);
			this.Benefit.Buys = 1;
			this.Benefit.Currency.Coin.Value = 2;
		}

		protected override Boolean AllowUndo { get { return true; } }
	}
	public class Workshop : Card
	{
		public Workshop()
			: base("Workshop", Category.Action, Source.Base, Location.Kingdom, Group.Gain | Group.Terminal)
		{
			this.BaseCost = new Cost(3);
			this.Text = "Gain a card costing up to <coin>4</coin>.";
		}

		public override void Play(Player player)
		{
			base.Play(player);
			SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply => supply.CanGain() && supply.CurrentCost <= new Coin(4));
			Choice choice = new Choice("Gain a card costing up to <coin>4</coin>", this, gainableSupplies, player, false);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Supply != null)
				player.Gain(result.Supply);
		}
	}
}
