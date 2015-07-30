using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using DominionBase.Currencies;
using DominionBase.Piles;
using DominionBase.Players;

namespace DominionBase.Cards.DarkAges
{
	public static class TypeClass
	{
		public static Type AbandonedMine = typeof(AbandonedMine);
		public static Type Altar = typeof(Altar);
		public static Type Armory = typeof(Armory);
		public static Type BandOfMisfits = typeof(BandOfMisfits);
		public static Type BanditCamp = typeof(BanditCamp);
		public static Type Beggar = typeof(Beggar);
		public static Type Catacombs = typeof(Catacombs);
		public static Type Count = typeof(Count);
		public static Type Counterfeit = typeof(Counterfeit);
		public static Type Cultist = typeof(Cultist);
		public static Type DameAnna = typeof(DameAnna);
		public static Type DameJosephine = typeof(DameJosephine);
		public static Type DameMolly = typeof(DameMolly);
		public static Type DameNatalie = typeof(DameNatalie);
		public static Type DameSylvia = typeof(DameSylvia);
		public static Type DeathCart = typeof(DeathCart);
		public static Type Feodum = typeof(Feodum);
		public static Type Forager = typeof(Forager);
		public static Type Fortress = typeof(Fortress);
		public static Type Graverobber = typeof(Graverobber);
		public static Type Hermit = typeof(Hermit);
		public static Type Hovel = typeof(Hovel);
		public static Type HuntingGrounds = typeof(HuntingGrounds);
		public static Type Ironmonger = typeof(Ironmonger);
		public static Type JunkDealer = typeof(JunkDealer);
		public static Type Knights = typeof(Knights);
		public static Type Madman = typeof(Madman);
		public static Type Marauder = typeof(Marauder);
		public static Type MarketSquare = typeof(MarketSquare);
		public static Type Mercenary = typeof(Mercenary);
		public static Type Mystic = typeof(Mystic);
		public static Type Necropolis = typeof(Necropolis);
		public static Type OvergrownEstate = typeof(OvergrownEstate);
		public static Type Pillage = typeof(Pillage);
		public static Type PoorHouse = typeof(PoorHouse);
		public static Type Procession = typeof(Procession);
		public static Type Rats = typeof(Rats);
		public static Type Rebuild = typeof(Rebuild);
		public static Type Rogue = typeof(Rogue);
		public static Type RuinedLibrary = typeof(RuinedLibrary);
		public static Type RuinedMarket = typeof(RuinedMarket);
		public static Type RuinedVillage = typeof(RuinedVillage);
		public static Type RuinsSupply = typeof(RuinsSupply);
		public static Type Sage = typeof(Sage);
		public static Type Scavenger = typeof(Scavenger);
		public static Type Shelters = typeof(Shelters);
		public static Type SirBailey = typeof(SirBailey);
		public static Type SirDestry = typeof(SirDestry);
		public static Type SirMartin = typeof(SirMartin);
		public static Type SirMichael = typeof(SirMichael);
		public static Type SirVander = typeof(SirVander);
		public static Type Spoils = typeof(Spoils);
		public static Type Squire = typeof(Squire);
		public static Type Storeroom = typeof(Storeroom);
		public static Type Survivors = typeof(Survivors);
		public static Type Urchin = typeof(Urchin);
		public static Type Vagrant = typeof(Vagrant);
		public static Type WanderingMinstrel = typeof(WanderingMinstrel);
	}

	public class AbandonedMine : Card
	{
		public AbandonedMine()
			: base("Abandoned Mine", Category.Action | Category.Ruins, Source.DarkAges, Location.General, Group.PlusCoin | Group.Terminal)
		{
			this.BaseCost = new Cost(0);
			this.Benefit.Currency.Coin.Value = 1;
		}

		protected override Boolean AllowUndo { get { return true; } }

		public override Type BaseType { get { return TypeClass.RuinsSupply; } }
	}
	public class Altar : Card
	{
		public Altar()
			: base("Altar", Category.Action, Source.DarkAges, Location.Kingdom, Group.Gain | Group.Trash | Group.Terminal)
		{
			this.BaseCost = new Cost(6);
			this.Text = "Trash a card from your hand.<nl/>Gain a card costing up to <coin>5</coin>.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choiceTrash = new Choice("Choose a card to trash", this, player.Hand, player);
			ChoiceResult resultTrash = player.MakeChoice(choiceTrash);
			player.Trash(player.RetrieveCardsFrom(DeckLocation.Hand, resultTrash.Cards));

			SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply => supply.CanGain() && supply.CurrentCost <= new Coin(5));
			Choice choice = new Choice("Gain a card costing up to <coin>5</coin>", this, gainableSupplies, player, false);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Supply != null)
				player.Gain(result.Supply);
		}
	}
	public class Armory : Card
	{
		public Armory()
			: base("Armory", Category.Action, Source.DarkAges, Location.Kingdom, Group.Gain | Group.CardOrdering | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.Text = "Gain a card costing up to <coin>4</coin>, putting it on top of your deck.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply => supply.CanGain() && supply.CurrentCost <= new Coin(4));
			Choice choice = new Choice("Gain a card costing up to <coin>4</coin>", this, gainableSupplies, player, false);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Supply != null)
				player.Gain(result.Supply, DeckLocation.Deck, DeckPosition.Top);
		}
	}
	public class BandOfMisfits : Card
	{
		private Player.CardPuttingIntoPlayEventHandler _CardPuttingIntoPlayEventHandler = null;

		private Card _ClonedCard = null;
		public Card ClonedCard { get { return _ClonedCard; } private set { _ClonedCard = value; } }

		public BandOfMisfits()
			: base("Band of Misfits", Category.Action, Source.DarkAges, Location.Kingdom)
		{
			this.BaseCost = new Cost(5);
			this.Text = "Play this as if it were an Action card in the Supply costing less than it that you choose.<nl/>This is that card until it leaves play.";

			this.OwnerChanged += new OwnerChangedEventHandler(BandOfMisfits_OwnerChanged);
		}

		internal override void TearDown()
		{
			BandOfMisfits_OwnerChanged(this, new OwnerChangedEventArgs(this.Owner, null));

			base.TearDown();

			this.OwnerChanged -= new OwnerChangedEventHandler(BandOfMisfits_OwnerChanged);
		}

		void BandOfMisfits_OwnerChanged(object sender, OwnerChangedEventArgs e)
		{
			if (_CardPuttingIntoPlayEventHandler != null && e.OldOwner != null)
			{
				e.OldOwner.CardPuttingIntoPlay -= _CardPuttingIntoPlayEventHandler;
				_CardPuttingIntoPlayEventHandler = null;
			}

			if (e.NewOwner != null)
			{
				_CardPuttingIntoPlayEventHandler = new Player.CardPuttingIntoPlayEventHandler(player_CardPuttingIntoPlay);
				e.NewOwner.CardPuttingIntoPlay += _CardPuttingIntoPlayEventHandler;
			}
		}

		void player_CardPuttingIntoPlay(object sender, CardPutIntoPlayEventArgs e)
		{
			if (e.Card != this)
				return;

			if (this.ClonedCard == null)
			{
				Choice choice = new Choice("Name a card to clone this card as", this,
					new SupplyCollection(e.Player._Game.Table.Supplies.Where(kvp =>
						(kvp.Value.Category & Cards.Category.Action) == Cards.Category.Action && kvp.Value.Count > 0 && kvp.Value.CurrentCost < e.Player._Game.ComputeCost(this))),
					e.Player, false);
				ChoiceResult result = e.Player.MakeChoice(choice);
				if (result.Supply != null)
				{
					e.Player._Game.SendMessage(e.Player, this, result.Supply.TopCard);
					this.ClonedCard = Card.CreateInstance(result.Supply.TopCard.CardType);
					this.ClonedCard.PhysicalCard = this;
					// This is needed in order to set up player mats & any other acutrements
					this.ClonedCard.ReceivedBy(e.Player);
				}
				else
					e.Player._Game.SendMessage(e.Player, this, (ICard)null);
			}
		}

		public override void AddedTo(DeckLocation location, Player player)
		{
			base.AddedTo(location, player);
			if (this.ClonedCard == null)
				return;

			switch (location)
			{
				case DeckLocation.InPlay:
				case DeckLocation.SetAside:
					this.ClonedCard.AddedTo(location, player);
					break;
			}
		}

		public override void RemovedFrom(DeckLocation location, Player player)
		{
			base.RemovedFrom(location, player);
			if (location == DeckLocation.InPlay || location == DeckLocation.SetAside || location == DeckLocation.InPlayAndSetAside)
			{
				if (this.ClonedCard != null)
				{
					this.ClonedCard.PhysicalCard = null;
					this.ClonedCard.RemovedFrom(location, player);
					this.ClonedCard.TearDown();
				}
				this.ClonedCard = null;
			}
		}

		public override Card LogicalCard
		{
			get
			{
				if (this.ClonedCard != null)
					return this.ClonedCard.LogicalCard;
				return base.LogicalCard;
			}
		}
		public override Category Category
		{
			get
			{
				if (this.ClonedCard != null)
					return this.ClonedCard.Category;
				return base.Category;
			}
		}
		public override Source Source
		{
			get
			{
				if (this.ClonedCard != null)
					return this.ClonedCard.Source;
				return base.Source;
			}
		}
		public override Location Location
		{
			get
			{
				if (this.ClonedCard != null)
					return this.ClonedCard.Location;
				return base.Location;
			}
		}
		public override Group GroupMembership
		{
			get
			{
				if (this.ClonedCard != null)
					return this.ClonedCard.GroupMembership;
				return base.GroupMembership;
			}
		}
		public override Cost BaseCost
		{
			get
			{
				if (this.ClonedCard != null)
					return this.ClonedCard.BaseCost;
				return base.BaseCost;
			}
		}
		public override CardBenefit Benefit
		{
			get
			{
				if (this.ClonedCard != null)
					return this.ClonedCard.Benefit;
				return base.Benefit;
			}
		}
		public override CardBenefit DurationBenefit
		{
			get
			{
				if (this.ClonedCard != null)
					return this.ClonedCard.DurationBenefit;
				return base.DurationBenefit;
			}
		}
		public override int VictoryPoints
		{
			get
			{
				if (this.ClonedCard != null)
					return this.ClonedCard.VictoryPoints;
				return base.VictoryPoints;
			}
		}
		public override Card ModifiedBy
		{
			get
			{
				if (this.ClonedCard != null)
					return this.ClonedCard.ModifiedBy;
				return base.ModifiedBy;
			}
		}

		public override Boolean CanCleanUp
		{
			get
			{
				if (this.ClonedCard != null)
					return this.ClonedCard.CanCleanUp;
				return base.CanCleanUp;
			}
		}


		public override bool IsStackable { get { return this.ClonedCard == null; } }
		public override CardCollection CardStack()
		{
			CardCollection cc = new CardCollection() { this };
			if (this.ClonedCard != null)
				cc.AddRange(this.ClonedCard.CardStack());
			return cc;
		}

		public override void Play(Player player)
		{
			if (this.ClonedCard != null)
				this.ClonedCard.Play(player);
		}

		public override void PlayDuration(Player player)
		{
			if (this.ClonedCard != null)
				this.ClonedCard.PlayDuration(player);
		}

		internal override XmlNode GenerateXml(XmlDocument doc, string nodeName)
		{
			XmlNode xn = base.GenerateXml(doc, nodeName);
			if (this.ClonedCard != null)
				xn.AppendChild(this.ClonedCard.GenerateXml(doc, "cloned_card"));
			return xn;
		}

		internal override void LoadInstance(XmlNode xnCard)
		{
			base.LoadInstance(xnCard);
			this.ClonedCard = Card.Load(xnCard.SelectSingleNode("cloned_card"));
		}
	}
	public class BanditCamp : Card
	{
		public BanditCamp()
			: base("Bandit Camp", Category.Action, Source.DarkAges, Location.Kingdom, Group.Gain | Group.PlusCard | Group.PlusAction | Group.PlusMultipleActions)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 2;
			this.Text = "<nl/>Gain a Spoils from the Spoils pile.";
		}

		public override void Setup(Game game, Supply supply)
		{
			base.Setup(game, supply);

			if (!game.Table.SpecialPiles.ContainsKey(TypeClass.Spoils))
			{
				Supply spoilsSupply = new Supply(game, game.Players, TypeClass.Spoils, Spoils.BaseCount);
				spoilsSupply.FullSetup();
				game.Table.SpecialPiles.Add(TypeClass.Spoils, spoilsSupply);
			}
		}

		public override void Play(Player player)
		{
			base.Play(player);

			player.Gain(player._Game.Table.SpecialPiles[TypeClass.Spoils]);
		}
	}
	public class Beggar : Card
	{
		private Player.AttackedEventHandler _AttackHandler = null;

		public Beggar()
			: base("Beggar", Category.Action | Category.Reaction, Source.DarkAges, Location.Kingdom, Group.Gain | Group.ReactToAttack | Group.CardOrdering | Group.Terminal)
		{
			this.BaseCost = new Cost(2);
			this.Text = "Gain 3 Coppers, putting them into your hand.<br/>When another player plays an Attack card, you may discard this.  If you do, gain two Silvers, putting one on top of your deck.";
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

			// Horse Traders only protects against other attackers
			if (player == e.Attacker)
				return;

			// Make sure it exists already
			if (player.Hand.Contains(this.PhysicalCard) && !e.Revealable.ContainsKey(TypeClass.Beggar))
				e.Revealable[TypeClass.Beggar] = new AttackReaction(this, String.Format("Discard {0}", this.PhysicalCard), player_RevealBeggar);
		}

		internal void player_RevealBeggar(Player player, ref AttackedEventArgs e)
		{
			player.Discard(DeckLocation.Hand, this.PhysicalCard);
			player.Gain(player._Game.Table.Silver, DeckLocation.Deck, DeckPosition.Top);
			player.Gain(player._Game.Table.Silver);

			e.HandledBy.Add(TypeClass.Beggar);

			// Attack isn't cancelled... it's just mitigated
		}

		public override void RemovedFrom(DeckLocation location, Player player)
		{
			base.RemovedFrom(location, player);
			if (_AttackHandler != null)
				player.Attacked -= _AttackHandler;
			_AttackHandler = null;
		}

		public override void Play(Player player)
		{
			base.Play(player);

			player.Gain(player._Game.Table.Copper, DeckLocation.Hand, DeckPosition.Automatic, 3);
		}
	}
	public class Catacombs : Card
	{
		private Player.TrashedEventHandler _TrashedEventHandler = null;

		public Catacombs()
			: base("Catacombs", Category.Action, Source.DarkAges, Location.Kingdom, Group.CardOrdering | Group.ReactToTrashing | Group.Gain | Group.Discard | Group.Terminal)
		{
			this.BaseCost = new Cost(5);
			this.Text = "Look at the top 3 cards of your deck.  Choose one: Put them into your hand; or discard them and +3 Cards.<br/>When you trash this, gain a cheaper card.";

			this.OwnerChanged += new OwnerChangedEventHandler(Catacombs_OwnerChanged);
		}

		internal override void TearDown()
		{
			Catacombs_OwnerChanged(this, new OwnerChangedEventArgs(this.Owner, null));

			base.TearDown();

			this.OwnerChanged -= new OwnerChangedEventHandler(Catacombs_OwnerChanged);
		}

		void Catacombs_OwnerChanged(object sender, OwnerChangedEventArgs e)
		{
			if (_TrashedEventHandler != null && e.OldOwner != null)
			{
				e.OldOwner.Trashed -= _TrashedEventHandler;
				_TrashedEventHandler = null;
			}

			if (e.NewOwner != null)
			{
				_TrashedEventHandler = new Player.TrashedEventHandler(player_Trashed);
				e.NewOwner.Trashed += _TrashedEventHandler;
			}
		}

		void player_Trashed(object sender, TrashEventArgs e)
		{
			Player player = sender as Player;

			// Already being processed or been handled -- don't need to process this one
			if (e.Actions.ContainsKey(TypeClass.Catacombs) || e.HandledBy.Contains(this))
				return;

			if (e.TrashedCards.Contains(this.PhysicalCard))
				e.Actions[TypeClass.Catacombs] = new TrashAction(this.Owner, this, "Gain a cheaper card", player_PlusCard, true);
		}

		internal void player_PlusCard(Player player, ref TrashEventArgs e)
		{
			SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply => supply.CanGain() && supply.CurrentCost < player._Game.ComputeCost(this));
			Choice choice = new Choice(String.Format("Gain a card costing less than {0}", player._Game.ComputeCost(this).ToString()), this, gainableSupplies, player, false);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Supply != null)
				player.Gain(result.Supply);

			e.HandledBy.Add(this);
		}

		public override void Play(Player player)
		{
			base.Play(player);

			CardCollection newCards = player.Draw(3, DeckLocation.Private);

			Choice choice = new Choice(
				String.Format("Do you want to discard {0} or put them back on top?", String.Join(" and ", newCards.Select(c => c.Name))),
				this,
				newCards,
				new List<string>() { "Discard", "Put them into your hand" },
				player);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Options[0] == "Discard")
			{
				player.Discard(DeckLocation.Private);
				player.DrawHand(3);
			}
			else
			{
				player.AddCardsToHand(DeckLocation.Private);
			}
		}
	}
	public class Count : Card
	{
		public Count()
			: base("Count", Category.Action, Source.DarkAges, Location.Kingdom, Group.CardOrdering | Group.Discard | Group.Gain | Group.PlusCoin | Group.Trash | Group.Terminal | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(5);
			this.Text = "Choose one: Discard 2 cards; or put a card from your hand on top of your deck; or gain a Copper.<nl/><nl/>Choose one: +<coin>3</coin>; or trash your hand; or gain a Duchy.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choiceTheFirst = new Choice("Choose one:", this, new CardCollection() { this }, new List<string>() { "Discard 2 cards", "Put a card on your deck", "Gain a Copper" }, player);
			ChoiceResult resultTheFirst = player.MakeChoice(choiceTheFirst);
			if (resultTheFirst.Options.Contains("Discard 2 cards"))
			{
				Choice choiceDiscard = new Choice("Discard 2 cards.", this, player.Hand, player, false, 2, 2);
				ChoiceResult resultDiscard = player.MakeChoice(choiceDiscard);
				player.Discard(DeckLocation.Hand, resultDiscard.Cards);
			}
			else if (resultTheFirst.Options.Contains("Put a card on your deck"))
			{
				Choice replaceChoice = new Choice("Choose a card to put back on your deck", this, player.Hand, player, false, 1, 1);
				ChoiceResult replaceResult = player.MakeChoice(replaceChoice);
				player.RetrieveCardsFrom(DeckLocation.Hand, replaceResult.Cards);
				player.AddCardsToDeck(replaceResult.Cards, DeckPosition.Top);
			}
			else
			{
				player.Gain(player._Game.Table.Copper);
			}

			Choice choiceTheSecond = new Choice("Choose one:", this, new CardCollection() { this }, new List<string>() { "+<coin>3</coin>", "Trash your hand", "Gain a Duchy" }, player);
			ChoiceResult resultTheSecond = player.MakeChoice(choiceTheSecond);
			if (resultTheSecond.Options.Contains("+<coin>3</coin>"))
			{
				player.ReceiveBenefit(this, new CardBenefit() { Currency = new Currency(3) });
			}
			else if (resultTheSecond.Options.Contains("Trash your hand"))
			{
				player.Trash(player.RetrieveCardsFrom(DeckLocation.Hand));
			}
			else
			{
				player.Gain(player._Game.Table.Duchy);
			}
		}
	}
	public class Counterfeit : Card
	{
		public Counterfeit()
			: base("Counterfeit", Category.Treasure, Source.DarkAges, Location.Kingdom, Group.PlusCoin | Group.PlusBuy | Group.Trash)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Currency.Coin.Value = 1;
			this.Benefit.Buys = 1;
			this.Text = "When you play this, you may play a Treasure from your hand twice.  If you do, trash that Treasure.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choice = new Choice(String.Format("You may play a Treasure card twice", player), this, player.Hand[Cards.Category.Treasure], player, false, 0, 1);
			ChoiceResult result = player.MakeChoice(choice);

			if (result.Cards.Count > 0)
			{
				Card card = result.Cards[0];
				player.PlayCardInternal(card);
				player.PlayCardInternal(card, " again");

				if (player.InPlay.Contains(card))
					player.Trash(player.RetrieveCardFrom(DeckLocation.InPlay, card));
			}
			else
				player.PlayNothing();
		}
	}
	public class Cultist : Looter
	{
		private Player.TrashedEventHandler _TrashedEventHandler = null;

		public Cultist()
			: base("Cultist", Category.Action | Category.Attack, Group.Gain | Group.ReactToTrashing)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Cards = 2;
			this.Text = "Each other player gains a Ruins.  You may play a Cultist from your hand.<br/>When you trash this, +3 Cards.";

			this.OwnerChanged += new OwnerChangedEventHandler(Cultist_OwnerChanged);
		}

		internal override void TearDown()
		{
			Cultist_OwnerChanged(this, new OwnerChangedEventArgs(this.Owner, null));

			base.TearDown();

			this.OwnerChanged -= new OwnerChangedEventHandler(Cultist_OwnerChanged);
		}

		void Cultist_OwnerChanged(object sender, OwnerChangedEventArgs e)
		{
			if (_TrashedEventHandler != null && e.OldOwner != null)
			{
				e.OldOwner.Trashed -= _TrashedEventHandler;
				_TrashedEventHandler = null;
			}

			if (e.NewOwner != null)
			{
				_TrashedEventHandler = new Player.TrashedEventHandler(player_Trashed);
				e.NewOwner.Trashed += _TrashedEventHandler;
			}
		}

		void player_Trashed(object sender, TrashEventArgs e)
		{
			Player player = sender as Player;

			// Already being processed or been handled -- don't need to process this one
			if (e.Actions.ContainsKey(TypeClass.Cultist) || e.HandledBy.Contains(this))
				return;

			if (e.TrashedCards.Contains(this.PhysicalCard))
				e.Actions[TypeClass.Cultist] = new TrashAction(this.Owner, this, "+3 Cards", player_PlusCard, true);
		}

		internal void player_PlusCard(Player player, ref TrashEventArgs e)
		{
			player.ReceiveBenefit(this, new CardBenefit() { Cards = 3 });

			e.HandledBy.Add(this);
		}

		public override void Play(Player player)
		{
			base.Play(player);

			// Perform attack on every player
			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			enumerator.MoveNext();
			while (enumerator.MoveNext())
			{
				Player attackee = enumerator.Current;
				// Skip if the attack is blocked (Moat, Lighthouse, etc.)
				if (this.IsAttackBlocked[attackee])
					continue;

				attackee.Gain(player._Game.Table[TypeClass.RuinsSupply]);
			}

			if (player.Hand[TypeClass.Cultist].Count > 0)
			{
				Choice choicePlayer = Choice.CreateYesNoChoice("Do you want to play a Cultist from your hand?", this, player);
				ChoiceResult resultPlayer = player.MakeChoice(choicePlayer);
				if (resultPlayer.Options[0] == "Yes")
				{
					player.Actions++;
					player.PlayCardInternal(player.Hand[TypeClass.Cultist][0]);
				}
			}
		}
	}
	public class DameAnna : Knight
	{
		public DameAnna()
			: base("Dame Anna", Category.Action | Category.Attack | Category.Knight, Group.Terminal | Group.Trash | Group.Discard)
		{
			this.BaseCost = new Cost(5);
			this.Text = "You may trash up to 2 cards from your hand.<nl/>Each other player reveals the top 2 cards of his deck, trashes one of them costing from <coin>3</coin> to <coin>6</coin>, and discards the rest.  If a Knight is trashed by this, trash this card.";
		}

		public override Type BaseType { get { return TypeClass.Knights; } }

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choiceTrash = new Choice("Choose up to 2 cards to trash", this, player.Hand, player, false, 0, 2);
			ChoiceResult resultTrash = player.MakeChoice(choiceTrash);
			player.Trash(player.RetrieveCardsFrom(DeckLocation.Hand, resultTrash.Cards));

			this.KnightAttack(player);
		}
	}
	public class DameJosephine : Knight
	{
		public DameJosephine()
			: base("Dame Josephine", Category.Action | Category.Attack | Category.Knight | Category.Victory, Group.Terminal | Group.Trash | Group.Discard)
		{
			this.BaseCost = new Cost(5);
			this.VictoryPoints = 2;
			this.Text = "Each other player reveals the top 2 cards of his deck, trashes one of them costing from <coin>3</coin> to <coin>6</coin>, and discards the rest.  If a Knight is trashed by this, trash this card.";
		}

		public override Type BaseType { get { return TypeClass.Knights; } }

		public override void Play(Player player)
		{
			base.Play(player);

			this.KnightAttack(player);
		}
	}
	public class DameMolly : Knight
	{
		public DameMolly()
			: base("Dame Molly", Category.Action | Category.Attack | Category.Knight, Group.PlusAction | Group.PlusMultipleActions | Group.Trash | Group.Discard)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Actions = 2;
			this.Text = "Each other player reveals the top 2 cards of his deck, trashes one of them costing from <coin>3</coin> to <coin>6</coin>, and discards the rest.  If a Knight is trashed by this, trash this card.";
		}

		public override Type BaseType { get { return TypeClass.Knights; } }

		public override void Play(Player player)
		{
			base.Play(player);

			this.KnightAttack(player);
		}
	}
	public class DameNatalie : Knight
	{
		public DameNatalie()
			: base("Dame Natalie", Category.Action | Category.Attack | Category.Knight, Group.Terminal | Group.Trash | Group.Discard | Group.Gain)
		{
			this.BaseCost = new Cost(5);
			this.Text = "You may gain a card costing up to <coin>3</coin>.<nl/>Each other player reveals the top 2 cards of his deck, trashes one of them costing from <coin>3</coin> to <coin>6</coin>, and discards the rest.  If a Knight is trashed by this, trash this card.";
		}

		public override Type BaseType { get { return TypeClass.Knights; } }

		public override void Play(Player player)
		{
			base.Play(player);

			SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply => supply.CanGain() && supply.CurrentCost <= new Coin(3));
			Choice choice = new Choice("You may gain a card costing up to <coin>3</coin>", this, gainableSupplies, player, true);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Supply != null)
				player.Gain(result.Supply);

			this.KnightAttack(player);
		}
	}
	public class DameSylvia : Knight
	{
		public DameSylvia()
			: base("Dame Sylvia", Category.Action | Category.Attack | Category.Knight, Group.PlusCoin | Group.Terminal | Group.Trash | Group.Discard)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Currency.Coin.Value = 2;
			this.Text = "Each other player reveals the top 2 cards of his deck, trashes one of them costing from <coin>3</coin> to <coin>6</coin>, and discards the rest.  If a Knight is trashed by this, trash this card.";
		}

		public override Type BaseType { get { return TypeClass.Knights; } }

		public override void Play(Player player)
		{
			base.Play(player);

			this.KnightAttack(player);
		}
	}
	public class DeathCart : Looter
	{
		private Dictionary<Player, Player.CardGainedEventHandler> _CardGainedHandlers = new Dictionary<Player, Player.CardGainedEventHandler>();

		public DeathCart()
			: base("Death Cart", Category.Action,  Group.PlusCoin | Group.Trash | Group.Gain | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Currency.Coin.Value = 5;
			this.Text = "You may trash an Action card from your hand.  If you don't, trash this.<br/>When you gain this, gain 2 Ruins.";
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
			if (e.Card != this || e.Actions.ContainsKey(TypeClass.DeathCart) || !e.Game.Table.Copper.CanGain())
				return;

			Player player = sender as Player;
			e.Actions[TypeClass.DeathCart] = new Players.CardGainAction(this.Owner, this, "Gain 2 Ruins", player_GainCache, true);
		}

		internal void player_GainCache(Player player, ref Players.CardGainEventArgs e)
		{
			player.Gain(player._Game.Table[TypeClass.RuinsSupply], 2);

			e.HandledBy.Add(TypeClass.DeathCart);

			// Clear out the Event Triggers -- this only happens when its Gained, so we don't care any more
			foreach (Player playerLoop in _CardGainedHandlers.Keys)
				playerLoop.CardGained -= _CardGainedHandlers[playerLoop];
			_CardGainedHandlers.Clear();
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choiceTrash = new Choice("You may trash an Action card", this, player.Hand[Cards.Category.Action], player, false, 0, 1);
			ChoiceResult resultTrash = player.MakeChoice(choiceTrash);
			if (resultTrash.Cards.Count > 0)
			{
				player.Trash(player.RetrieveCardsFrom(DeckLocation.Hand, resultTrash.Cards));
			}
			else if (player.InPlay.Contains(this.PhysicalCard))
			{
				player.Trash(player.RetrieveCardFrom(DeckLocation.InPlay, this.PhysicalCard));
			}
		}
	}
	public class Feodum : Card
	{
		private Player.TrashedEventHandler _TrashedEventHandler = null;

		public Feodum()
			: base("Feodum", Category.Victory, Source.DarkAges, Location.Kingdom, Group.ReactToTrashing | Group.VariableVPs)
		{
			this.BaseCost = new Cost(4);
			this.Text = "Worth <vp>1</vp> for every 3 Silvers in your deck (rounded down).<br/>When you trash this, gain 3 Silvers.";

			this.OwnerChanged += new OwnerChangedEventHandler(Feodum_OwnerChanged);
		}

		public override int GetVictoryPoints(IEnumerable<Card> cards)
		{
			return base.GetVictoryPoints(cards) +
				cards.Count(c => c.CardType == Cards.Universal.TypeClass.Silver) / 3;
		}

		internal override void TearDown()
		{
			Feodum_OwnerChanged(this, new OwnerChangedEventArgs(this.Owner, null));

			base.TearDown();

			this.OwnerChanged -= new OwnerChangedEventHandler(Feodum_OwnerChanged);
		}

		void Feodum_OwnerChanged(object sender, OwnerChangedEventArgs e)
		{
			if (_TrashedEventHandler != null && e.OldOwner != null)
			{
				e.OldOwner.Trashed -= _TrashedEventHandler;
				_TrashedEventHandler = null;
			}

			if (e.NewOwner != null)
			{
				_TrashedEventHandler = new Player.TrashedEventHandler(player_Trashed);
				e.NewOwner.Trashed += _TrashedEventHandler;
			}
		}

		void player_Trashed(object sender, TrashEventArgs e)
		{
			Player player = sender as Player;

			// Already being processed or been handled -- don't need to process this one
			if (e.Actions.ContainsKey(TypeClass.Feodum) || e.HandledBy.Contains(this))
				return;

			if (e.TrashedCards.Contains(this.PhysicalCard))
				e.Actions[TypeClass.Feodum] = new TrashAction(this.Owner, this, "Gain 3 Silvers", player_Gain3Silvers, true);
		}

		internal void player_Gain3Silvers(Player player, ref TrashEventArgs e)
		{
			player.Gain(player._Game.Table.Silver, 3);

			e.HandledBy.Add(this);
		}
	}
	public class Forager : Card
	{
		public Forager()
			: base("Forager", Category.Action, Source.DarkAges, Location.Kingdom, Group.PlusAction | Group.PlusBuy | Group.Trash | Group.PlusCoin | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(3);
			this.Benefit.Actions = 1;
			this.Benefit.Buys = 1;
			this.Text = "<nl/>Trash a card from your hand.<nl/>+<coin>1</coin> per differently named Treasure in the trash.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choiceTrash = new Choice("Choose a card to trash", this, player.Hand, player);
			ChoiceResult resultTrash = player.MakeChoice(choiceTrash);
			player.Trash(player.RetrieveCardsFrom(DeckLocation.Hand, resultTrash.Cards));

			player.ReceiveBenefit(this, new CardBenefit()
			{
				Currency = new Currency(player._Game.Table.Trash.Where(card => 
					(card.Category & Cards.Category.Treasure) == Cards.Category.Treasure
					).GroupBy(card => card.Name).Count())
			});
		}
	}
	public class Fortress : Card
	{
		private Player.TrashedEventHandler _TrashedEventHandler = null;
		private Player.TrashedFinishedEventHandler _TrashedFinishedEventHandler = null;

		public Fortress()
			: base("Fortress", Category.Action, Source.DarkAges, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.PlusMultipleActions | Group.ReactToTrashing)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 2;
			this.Text = "<br/>When you trash this, put it into your hand.";

			this.OwnerChanged += new OwnerChangedEventHandler(Fortress_OwnerChanged);
		}

		internal override void TearDown()
		{
			Fortress_OwnerChanged(this, new OwnerChangedEventArgs(this.Owner, null));

			base.TearDown();

			this.OwnerChanged -= new OwnerChangedEventHandler(Fortress_OwnerChanged);
		}

		void Fortress_OwnerChanged(object sender, OwnerChangedEventArgs e)
		{
			if (_TrashedEventHandler != null && e.OldOwner != null)
			{
				e.OldOwner.Trashed -= _TrashedEventHandler;
				_TrashedEventHandler = null;
			}

			if (e.NewOwner != null)
			{
				_TrashedEventHandler = new Player.TrashedEventHandler(player_Trashed);
				e.NewOwner.Trashed += _TrashedEventHandler;
			}
		}

		void player_Trashed(object sender, TrashEventArgs e)
		{
			Player player = sender as Player;

			// Already being processed or been handled -- don't need to process this one
			if (e.Actions.ContainsKey(TypeClass.Fortress) || e.HandledBy.Contains(this))
				return;

			if (e.TrashedCards.Contains(this.PhysicalCard))
				e.Actions[TypeClass.Fortress] = new TrashAction(this.Owner, this, "Put Fortress into your hand", player_RegainFortress, true);
		}

		void player_TrashedFinished(object sender, TrashEventArgs e)
		{
			this.PhysicalCard.ObtainedBy(e.CurrentPlayer);
			if (_TrashedFinishedEventHandler != null)
				e.CurrentPlayer.TrashedFinished -= _TrashedFinishedEventHandler;
			_TrashedFinishedEventHandler = null;
		}

		internal void player_RegainFortress(Player player, ref TrashEventArgs e)
		{
			if (player._Game.Table.Trash.Contains(this.PhysicalCard))
			{
				player.Gain(player._Game.Table.Trash, this.PhysicalCard, DeckLocation.Hand, DeckPosition.Automatic);
				_TrashedFinishedEventHandler = new Player.TrashedFinishedEventHandler(player_TrashedFinished);
				player.TrashedFinished += _TrashedFinishedEventHandler;
			}

			e.HandledBy.Add(this);
		}
	}
	public class Graverobber : Card
	{
		public Graverobber()
			: base("Graverobber", Category.Action, Source.DarkAges, Location.Kingdom, Group.DeckReduction | Group.Gain | Group.RemoveCurses | Group.Terminal | Group.Trash)
		{
			this.BaseCost = new Cost(5);
			this.Text = "Choose one: Gain a card from the trash costing from <coin>3</coin> to <coin>6</coin>, putting it on top of your deck; or trash an Action card from your hand and gain a card costing up to <coin>3</coin> more than it.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choice = new Choice("Choose one:", this, new CardCollection() { this }, new List<string>() { "Gain a card from the trash", "Trash an Action card from your hand" }, player);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Options.Contains("Gain a card from the trash"))
			{
				List<Cost> availableCosts = new List<Cost>() { new Cost(3), new Cost(4), new Cost(5), new Cost(6) };
				IEnumerable<Card> availableTrashCards = player._Game.Table.Trash.Where(c => availableCosts.Any(cost => player._Game.ComputeCost(c) == cost));

				Choice choiceFromTrash = new Choice("Choose a card to gain from the trash", this, availableTrashCards, player);
				ChoiceResult resultFromTrash = player.MakeChoice(choiceFromTrash);
				if (resultFromTrash.Cards.Count > 0)
				{
					player.Gain(player._Game.Table.Trash, resultFromTrash.Cards[0], DeckLocation.Deck, DeckPosition.Top);
				}
			}
			else
			{
				Choice choiceTrash = new Choice("Choose an Action card to trash", this, player.Hand[Cards.Category.Action], player);
				ChoiceResult resultTrash = player.MakeChoice(choiceTrash);
				player.Trash(player.RetrieveCardsFrom(DeckLocation.Hand, resultTrash.Cards));

				if (resultTrash.Cards.Count > 0)
				{
					Cost trashedCardCost = player._Game.ComputeCost(resultTrash.Cards[0]);
					SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply => supply.CanGain() && supply.CurrentCost <= (trashedCardCost + new Coin(3)));
					Choice choiceGain = new Choice("Gain a card", this, gainableSupplies, player, false);
					ChoiceResult resultGain = player.MakeChoice(choiceGain);
					if (resultGain.Supply != null)
						player.Gain(resultGain.Supply);
				}

			}
		}
	}
	public class Hermit : Card
	{
		private Player.CardsDiscardingEventHandler _CardsDiscardingEventHandler = null;
		private Boolean _ShouldBeTrashed = false;

		public Hermit()
			: base("Hermit", Category.Action, Source.DarkAges, Location.Kingdom, Group.Gain | Group.Terminal | Group.Trash)
		{
			this.BaseCost = new Cost(3);
			this.Text = "Look through your discard pile.  You may trash a card from your discard pile or hand that is not a Treasure.  Gain a card costing up to <coin>3</coin>.<br/>When you discard this from play, if you did not buy any cards this turn, trash this and gain a Madman from the Madman pile.";

			this.OwnerChanged += new OwnerChangedEventHandler(Hermit_OwnerChanged);
		}

		public override void Setup(Game game, Supply supply)
		{
			base.Setup(game, supply);
			Supply madmanSupply = new Supply(game, game.Players, TypeClass.Madman, 10);
			madmanSupply.FullSetup();
			game.Table.SpecialPiles.Add(TypeClass.Madman, madmanSupply);
		}

		internal override void TearDown()
		{
			Hermit_OwnerChanged(this, new OwnerChangedEventArgs(this.Owner, null));

			base.TearDown();

			this.OwnerChanged -= new OwnerChangedEventHandler(Hermit_OwnerChanged);
		}

		void Hermit_OwnerChanged(object sender, OwnerChangedEventArgs e)
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
			Player player = (Player)sender;
			// Only allow this if no cards were bought this turn
			// We set this up right now because this action happens regardless of whether or not the card is "lost track of"
			if (e.Cards.Contains(this.PhysicalCard) && player.CurrentTurn.CardsBought.Count == 0 && 
				(e.FromLocation == DeckLocation.InPlay || e.FromLocation == DeckLocation.SetAside || e.FromLocation == DeckLocation.InPlayAndSetAside))
				_ShouldBeTrashed = true;

			if (e.GetAction(TypeClass.Hermit) != null || !_ShouldBeTrashed)
				return;

			e.AddAction(TypeClass.Hermit, new CardsDiscardAction(sender as Player, this, String.Format("Trash {0}", this.PhysicalCard), player_Action, true));
		}

		internal void player_Action(Player player, ref CardsDiscardEventArgs e)
		{
			e.Cards.Remove(this.PhysicalCard);
			if (player.InPlay.Contains(this.PhysicalCard))
				player.Trash(player.RetrieveCardFrom(DeckLocation.InPlay, this.PhysicalCard));
			else if (player.SetAside.Contains(this.PhysicalCard))
				player.Trash(player.RetrieveCardFrom(DeckLocation.SetAside, this.PhysicalCard));

			player.Gain(player._Game.Table.SpecialPiles[TypeClass.Madman]);

			e.HandledBy.Add(this);
			_ShouldBeTrashed = false;
		}

		public override void Play(Player player)
		{
			_ShouldBeTrashed = false;

			base.Play(player);

			CardCollection nonTreasures = player.DiscardPile.LookThrough(c => (c.Category & Cards.Category.Treasure) != Cards.Category.Treasure);

			nonTreasures.Add(new Universal.Dummy());
			nonTreasures.AddRange(player.Hand[c => (c.Category & Cards.Category.Treasure) != Cards.Category.Treasure]);

			if (nonTreasures.Count > 1)
			{
				Choice choiceTrash = new Choice("You may choose a non-Treasure card to trash", this, nonTreasures, player, false, 0, 1);
				ChoiceResult resultTrash = player.MakeChoice(choiceTrash);

				if (resultTrash.Cards.Count > 0)
				{
					Card cardToTrash = null;
					if (player.Hand.Contains(resultTrash.Cards[0]))
						cardToTrash = player.RetrieveCardFrom(DeckLocation.Hand, resultTrash.Cards[0]);
					else
						cardToTrash = player.RetrieveCardFrom(DeckLocation.Discard, resultTrash.Cards[0]);
					player.Trash(cardToTrash);
				}
			}


			SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply => supply.CanGain() && supply.CurrentCost <= new Coin(3));
			Choice choice = new Choice("Gain a card costing up to <coin>3</coin>", this, gainableSupplies, player, false);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Supply != null)
				player.Gain(result.Supply);
		}
	}
	public class Hovel : Card
	{
		private Player.CardBoughtEventHandler _CardBoughtEventHandler = null;

		public Hovel()
			: base("Hovel", Category.Reaction | Category.Shelter, Source.DarkAges, Location.General, Group.ReactToTrashing)
		{
			this.BaseCost = new Cost(1);
			this.Text = "When you buy a Victory card, you may trash this from your hand.";
		}

		internal override void TearDown()
		{
			if (this.Owner != null && _CardBoughtEventHandler != null)
				this.Owner.CardBought -= _CardBoughtEventHandler;

			base.TearDown();
		}

		public override void AddedTo(DeckLocation location, Player player)
		{
			base.AddedTo(location, player);

			if (location == DeckLocation.Hand)
			{
				if (_CardBoughtEventHandler != null)
					player.CardBought -= _CardBoughtEventHandler;

				_CardBoughtEventHandler = new Player.CardBoughtEventHandler(player_CardBought);
				player.CardBought += _CardBoughtEventHandler;
			}
		}

		void player_CardBought(object sender, Players.CardBuyEventArgs e)
		{
			Player player = sender as Player;

			// Already been cancelled -- don't need to process this one
			if (e.Cancelled || !player.Hand.Contains(this.PhysicalCard) || e.Actions.ContainsKey(TypeClass.Hovel))
				return;

			if ((e.Card.Category & Cards.Category.Victory) == Cards.Category.Victory)
				e.Actions[TypeClass.Hovel] = new Players.CardBuyAction(this.Owner, this, String.Format("Trash {0}", this.PhysicalCard), player_TrashHovel, false);
		}

		internal void player_TrashHovel(Player player, ref Players.CardBuyEventArgs e)
		{
			player.Trash(player.RetrieveCardFrom(DeckLocation.Hand, this.PhysicalCard));

			e.HandledBy.Add(this);
		}

		public override void RemovedFrom(DeckLocation location, Player player)
		{
			base.RemovedFrom(location, player);
			if (_CardBoughtEventHandler != null)
				player.CardBought -= _CardBoughtEventHandler;
			_CardBoughtEventHandler = null;
		}

	}
	public class HuntingGrounds : Card
	{
		private Player.TrashedEventHandler _TrashedEventHandler = null;

		public HuntingGrounds()
			: base("Hunting Grounds", Category.Action, Source.DarkAges, Location.Kingdom, Group.PlusCard | Group.ReactToTrashing | Group.Terminal | Group.Gain)
		{
			this.BaseCost = new Cost(6);
			this.Benefit.Cards = 4;
			this.Text = "<br/>When you trash this, gain a Duchy or 3 Estates.";

			this.OwnerChanged += new OwnerChangedEventHandler(HuntingGrounds_OwnerChanged);
		}

		internal override void TearDown()
		{
			HuntingGrounds_OwnerChanged(this, new OwnerChangedEventArgs(this.Owner, null));

			base.TearDown();

			this.OwnerChanged -= new OwnerChangedEventHandler(HuntingGrounds_OwnerChanged);
		}

		void HuntingGrounds_OwnerChanged(object sender, OwnerChangedEventArgs e)
		{
			if (_TrashedEventHandler != null && e.OldOwner != null)
			{
				e.OldOwner.Trashed -= _TrashedEventHandler;
				_TrashedEventHandler = null;
			}

			if (e.NewOwner != null)
			{
				_TrashedEventHandler = new Player.TrashedEventHandler(player_Trashed);
				e.NewOwner.Trashed += _TrashedEventHandler;
			}
		}

		void player_Trashed(object sender, TrashEventArgs e)
		{
			Player player = sender as Player;

			// Already being processed or been handled -- don't need to process this one
			if (e.Actions.ContainsKey(TypeClass.HuntingGrounds) || e.HandledBy.Contains(this))
				return;

			if (e.TrashedCards.Contains(this.PhysicalCard))
				e.Actions[TypeClass.HuntingGrounds] = new TrashAction(this.Owner, this, "Gain Duchy/Estates", player_PlusCard, true);
		}

		internal void player_PlusCard(Player player, ref TrashEventArgs e)
		{
			Choice choice = new Choice("Choose one:", this, new CardCollection() { this }, new List<string>() { "Gain a Duchy", "Gain 3 Estates" }, player);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Options.Contains("Gain a Duchy"))
			{
				player.Gain(player._Game.Table.Duchy);
			}
			else
			{
				player.Gain(player._Game.Table.Estate, 3);
			}

			e.HandledBy.Add(this);
		}
	}
	public class Ironmonger : Card
	{
		public Ironmonger()
			: base("Ironmonger", Category.Action, Source.DarkAges, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.Discard | Group.CardOrdering | Group.PlusMultipleActions | Group.PlusCoin | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 1;
			this.Text = "Reveal the top card of your deck;<nl/>you may discard it.<nl/>Either way, if it is an...<nl/>Action card, +1 Action<nl/>Treasure card, +<coin>1</coin><nl/>Victory card, +1 Card";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			if (player.CanDraw)
			{
				Card revealedCard = player.Draw(DeckLocation.Revealed);

				if (revealedCard != null)
				{
					Choice choice = new Choice(
						String.Format("Do you want to discard {0} or put it back on your deck?", revealedCard),
						this,
						new CardCollection() { this },
						new List<string>() { "Discard", "Put it back" },
						player);
					ChoiceResult result = player.MakeChoice(choice);
					if (result.Options.Contains("Discard"))
						player.DiscardRevealed();
					else
						player.AddCardsToDeck(player.RetrieveCardsFrom(DeckLocation.Revealed), DeckPosition.Top);

					CardBenefit benefit = new CardBenefit();

					if ((revealedCard.Category & Cards.Category.Action) == Cards.Category.Action)
						benefit.Actions = 1;
					if ((revealedCard.Category & Cards.Category.Treasure) == Cards.Category.Treasure)
						benefit.Currency += new Coin(1);
					if ((revealedCard.Category & Cards.Category.Victory) == Cards.Category.Victory)
						benefit.Cards = 1;

					player.ReceiveBenefit(this, benefit);
				}
			}
		}
	}
	public class JunkDealer : Card
	{
		public JunkDealer()
			: base("Junk Dealer", Category.Action, Source.DarkAges, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.PlusCoin | Group.Trash | Group.DeckReduction)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 1;
			this.Benefit.Currency.Coin.Value = 1;
			this.Text = "<nl/>Trash a card from your hand.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choiceTrash = new Choice("Choose a card to trash", this, player.Hand, player);
			ChoiceResult resultTrash = player.MakeChoice(choiceTrash);
			player.Trash(player.RetrieveCardsFrom(DeckLocation.Hand, resultTrash.Cards));
		}
	}
	public abstract class Knight : Card
	{
		internal Knight(String name, Category category, Group group)
			: base(name, category, Source.DarkAges, Location.Special, group)
		{
		}

		public void KnightAttack(Player player)
		{
			List<Cost> availableCosts = new List<Cost>() { new Cost(3), new Cost(4), new Cost(5), new Cost(6) };

			Boolean anyKnightsTrashed = false;
			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			enumerator.MoveNext();
			while (enumerator.MoveNext())
			{
				Player attackee = enumerator.Current;
				if (this.IsAttackBlocked[attackee])
					continue;

				CardCollection attackeeCards = attackee.Draw(2, DeckLocation.Revealed);
				Choice choiceTrash = new Choice("Choose a card to trash", this, attackee.Revealed[c => availableCosts.Any(cost => player._Game.ComputeCost(c) == cost)], player);
				ChoiceResult resultTrash = attackee.MakeChoice(choiceTrash);
				if (resultTrash.Cards.Count > 0)
				{
					Card cardToTrash = attackee.RetrieveCardFrom(DeckLocation.Revealed, resultTrash.Cards[0]);
					if ((cardToTrash.Category & Cards.Category.Knight) == Cards.Category.Knight)
						anyKnightsTrashed = true;
					attackee.Trash(cardToTrash);
				}
				attackee.DiscardRevealed();
			}

			if (anyKnightsTrashed && player.InPlay.Contains(this.PhysicalCard))
				player.Trash(player.RetrieveCardFrom(DeckLocation.InPlay, this.PhysicalCard));
		}
	}
	public class Knights : Card
	{
		public Knights()
			: base("Knights", Category.Action | Category.Attack | Category.Knight, Source.DarkAges, Location.Kingdom, Group.None)
		{
			this.BaseCost = new Cost(5);
			this.Text = "Shuffle the Knight pile before each game with it.  Keep it face down except for the top card, which is the only one that can be bought or gained.";
		}

		public override void Setup(Game game, Supply supply)
		{
			base.Setup(game, supply);

			supply.Empty();

			CardCollection cards = new CardCollection();
			cards.Add(new DameAnna());
			cards.Add(new DameJosephine());
			cards.Add(new DameMolly());
			cards.Add(new DameNatalie());
			cards.Add(new DameSylvia());
			cards.Add(new SirBailey());
			cards.Add(new SirDestry());
			cards.Add(new SirMartin());
			cards.Add(new SirMichael());
			cards.Add(new SirVander());

			Utilities.Shuffler.Shuffle<Card>(cards);

			supply.AddTo(cards);
		}
	}
	public abstract class Looter : Card
	{
		internal Looter(String name, Category category, Group group)
			: base(name, category | Category.Looter, Source.DarkAges, Location.Kingdom, group)
		{
		}

		public override void Setup(Game game, Supply supply)
		{
			base.Setup(game, supply);
			if (!game.Table.Supplies.ContainsKey(TypeClass.RuinsSupply))
			{
				Supply ruinsSupply = new Supply(game, game.Players, TypeClass.RuinsSupply, Visibility.Top);
				ruinsSupply.FullSetup();
				game.Table.Supplies.Add(TypeClass.RuinsSupply, ruinsSupply);
			}
		}
	}
	public class Madman : Card
	{
		public Madman()
			: base("Madman", Category.Action, Source.DarkAges, Location.Special, Group.DeckReduction | Group.PlusAction | Group.PlusCard | Group.PlusMultipleActions | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(0, true, false);
			this.Benefit.Actions = 2;
			this.Text = "<nl/>Return this to the Madman pile.  If you do, +1 Card per card in your hand.<nl/><i>(This is not in the Supply.)</i>";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			if (player.InPlay.Contains(this.PhysicalCard))
			{
				Supply supply = player._Game.Table.SpecialPiles[TypeClass.Madman];
				Card cardToReturn = player.RetrieveCardFrom(DeckLocation.InPlay, this.PhysicalCard);
				player.Lose(this);
				supply.AddTo(this);
				player._Game.SendMessage(player, this, supply, 1);

				player.ReceiveBenefit(this, new CardBenefit() { Cards = player.Hand.Count });
			}
		}
	}
	public class Marauder : Looter
	{
		public Marauder()
			: base("Marauder", Category.Action | Category.Attack, Group.Gain)
		{
			this.BaseCost = new Cost(4);
			this.Text = "Gain a Spoils from the Spoils pile.<nl/>Each other player gains a Ruins.";
		}

		public override void Setup(Game game, Supply supply)
		{
			base.Setup(game, supply);

			if (!game.Table.SpecialPiles.ContainsKey(TypeClass.Spoils))
			{
				Supply spoilsSupply = new Supply(game, game.Players, TypeClass.Spoils, Spoils.BaseCount);
				spoilsSupply.FullSetup();
				game.Table.SpecialPiles.Add(TypeClass.Spoils, spoilsSupply);
			}
		}

		public override void Play(Player player)
		{
			base.Play(player);

			player.Gain(player._Game.Table.SpecialPiles[TypeClass.Spoils]);

			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			enumerator.MoveNext();
			while (enumerator.MoveNext())
			{
				Player attackee = enumerator.Current;
				// Skip if the attack is blocked (Moat, Lighthouse, etc.)
				if (this.IsAttackBlocked[attackee])
					continue;

				attackee.Gain(player._Game.Table[TypeClass.RuinsSupply]);
			}
		}
	}
	public class MarketSquare : Card
	{
		private Player.TrashedEventHandler _TrashedEventHandler = null;

		public MarketSquare()
			: base("Market Square", Category.Action | Category.Reaction, Source.DarkAges, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.PlusBuy | Group.ReactToTrashing | Group.Discard | Group.Gain)
		{
			this.BaseCost = new Cost(3);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 1;
			this.Benefit.Buys = 1;
			this.Text = "<br/>When one of your cards is trashed, you may discard this from your hand.  If you do, gain a Gold.";
		}

		public override void AddedTo(DeckLocation location, Player player)
		{
			base.AddedTo(location, player);

			if (location == DeckLocation.Hand)
			{
				if (_TrashedEventHandler != null)
					player.Trashed -= _TrashedEventHandler;

				_TrashedEventHandler = new Player.TrashedEventHandler(player_Trashed);
				player.Trashed += _TrashedEventHandler;
			}
		}

		public override void RemovedFrom(DeckLocation location, Player player)
		{
			base.RemovedFrom(location, player);
			if (_TrashedEventHandler != null)
				player.Trashed -= _TrashedEventHandler;
			_TrashedEventHandler = null;
		}

		void player_Trashed(object sender, TrashEventArgs e)
		{
			Player player = sender as Player;

			// Already being processed or been handled -- don't need to process this one
			if (e.Actions.ContainsKey(TypeClass.MarketSquare) || e.HandledBy.Contains(this))
				return;

			e.Actions[TypeClass.MarketSquare] = new TrashAction(this.Owner, this, "Discard Market Square", player_DiscardMarketSquare, false);
		}

		internal void player_DiscardMarketSquare(Player player, ref TrashEventArgs e)
		{
			player.Discard(DeckLocation.Hand, this);
			player.Gain(player._Game.Table.Gold);

			e.HandledBy.Add(this);
		}
	}
	public class Mercenary : Card
	{
		public const int BaseCount = 10;

		public Mercenary()
			: base("Mercenary", Category.Action | Category.Attack, Source.DarkAges, Location.Special, Group.PlusCard | Group.PlusCoin | Group.Trash | Group.DeckReduction | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(0, true, false);
			this.Text = "You may trash 2 cards from your hand.<nl/>If you do, +2 Cards, +<coin>2</coin>, and each other player discards down to 3 cards in hand.<nl/><i>(This is not in the Supply.)</i>";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choice = Choice.CreateYesNoChoice("Do you want to trash 2 cards?", this, player);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Options[0] == "Yes")
			{
				Choice choiceTrash = new Choice("Choose 2 cards to trash", this, player.Hand, player, false, 2, 2);
				ChoiceResult resultTrash = player.MakeChoice(choiceTrash);
				player.Trash(player.RetrieveCardsFrom(DeckLocation.Hand, resultTrash.Cards));

				if (resultTrash.Cards.Count == 2)
				{
					player.ReceiveBenefit(this, new CardBenefit() { Cards = 2, Currency = new Currency(2) });

					IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
					enumerator.MoveNext();
					while (enumerator.MoveNext())
					{
						Player attackee = enumerator.Current;
						// Skip if the attack is blocked (Moat, Lighthouse, etc.)
						if (this.IsAttackBlocked[attackee])
							continue;

						Choice choiceDiscard = new Choice("Choose cards to discard.  You must discard down to 3 cards in hand", this, attackee.Hand, attackee, false, attackee.Hand.Count - 3, attackee.Hand.Count - 3);
						ChoiceResult resultDiscard = attackee.MakeChoice(choiceDiscard);
						attackee.Discard(DeckLocation.Hand, resultDiscard.Cards);
					}

				}
			}
		}
	}
	public class Mystic : Card
	{
		public Mystic()
			: base("Mystic", Category.Action, Source.DarkAges, Location.Kingdom, Group.PlusAction | Group.PlusCoin | Group.PlusCard | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Actions = 1;
			this.Benefit.Currency.Coin.Value = 2;
			this.Text = "<nl/>Name a card.<nl/>Reveal the top card of your deck.<nl/>If it's the named card, put it into your hand.";
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
			if (player.CanDraw)
			{
				player.Draw(DeckLocation.Revealed);
				if (player.Revealed[namedCard.CardType].Count > 0)
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
	public class Necropolis : Card
	{
		public Necropolis()
			: base("Necropolis", Category.Action | Category.Shelter, Source.DarkAges, Location.General, Group.PlusAction | Group.PlusMultipleActions)
		{
			this.BaseCost = new Cost(1);
			this.Benefit.Actions = 2;
		}

		protected override Boolean AllowUndo { get { return true; } }
	}
	public class OvergrownEstate : Card
	{
		private Player.TrashedEventHandler _TrashedEventHandler = null;

		public OvergrownEstate()
			: base("Overgrown Estate", Category.Victory | Category.Shelter, Source.DarkAges, Location.General, Group.ReactToTrashing)
		{
			this.BaseCost = new Cost(1);
			this.Benefit.VictoryPoints = 0;
			this.Text = "<br/>When you trash this, +1 Card";

			this.OwnerChanged += new OwnerChangedEventHandler(OvergrownEstate_OwnerChanged);
		}

		internal override void TearDown()
		{
			OvergrownEstate_OwnerChanged(this, new OwnerChangedEventArgs(this.Owner, null));

			base.TearDown();

			this.OwnerChanged -= new OwnerChangedEventHandler(OvergrownEstate_OwnerChanged);
		}

		void OvergrownEstate_OwnerChanged(object sender, OwnerChangedEventArgs e)
		{
			if (_TrashedEventHandler != null && e.OldOwner != null)
			{
				e.OldOwner.Trashed -= _TrashedEventHandler;
				_TrashedEventHandler = null;
			}

			if (e.NewOwner != null)
			{
				_TrashedEventHandler = new Player.TrashedEventHandler(player_Trashed);
				e.NewOwner.Trashed += _TrashedEventHandler;
			}
		}

		void player_Trashed(object sender, TrashEventArgs e)
		{
			Player player = sender as Player;

			// Already being processed or been handled -- don't need to process this one
			if (e.Actions.ContainsKey(TypeClass.OvergrownEstate) || e.HandledBy.Contains(this))
				return;

			if (e.TrashedCards.Contains(this.PhysicalCard))
				e.Actions[TypeClass.OvergrownEstate] = new TrashAction(this.Owner, this, "+1 Card", player_PlusCard, true);
		}

		internal void player_PlusCard(Player player, ref TrashEventArgs e)
		{
			player.ReceiveBenefit(this, new CardBenefit() { Cards = 1 });

			e.HandledBy.Add(this);
		}
	}
	public class Pillage : Card
	{
		public Pillage()
			: base("Pillage", Category.Action | Category.Attack, Source.DarkAges, Location.Kingdom, Group.Discard | Group.Gain | Group.Terminal | Group.Trash)
		{
			this.BaseCost = new Cost(5);
			this.Text = "Trash this. Each other player with 5 or more cards in hand reveals his hand and discards a card you choose.<nl/>Gain 2 Spoils from the Spoils pile.";
		}

		public override void Setup(Game game, Supply supply)
		{
			base.Setup(game, supply);

			if (!game.Table.SpecialPiles.ContainsKey(TypeClass.Spoils))
			{
				Supply spoilsSupply = new Supply(game, game.Players, TypeClass.Spoils, Spoils.BaseCount);
				spoilsSupply.FullSetup();
				game.Table.SpecialPiles.Add(TypeClass.Spoils, spoilsSupply);
			}
		}

		public override void Play(Player player)
		{
			base.Play(player);

			if (player.InPlay.Contains(this.PhysicalCard))
				player.Trash(player.RetrieveCardFrom(DeckLocation.InPlay, this.PhysicalCard));

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

				attackee.RevealHand();
				Choice choice = new Choice(String.Format("Choose a card for {0} to discard.", attackee), this, attackee.Revealed, attackee, false, 1, 1);
				ChoiceResult result = player.MakeChoice(choice);
				attackee.Discard(DeckLocation.Revealed, result.Cards);
				attackee.ReturnHand(attackee.Revealed[c => true]);
			}

			player.Gain(player._Game.Table.SpecialPiles[TypeClass.Spoils], 2);
		}
	}
	public class PoorHouse : Card
	{
		public PoorHouse()
			: base("Poor House", Category.Action, Source.DarkAges, Location.Kingdom, Group.PlusCoin | Group.Terminal | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(1);
			this.Benefit.Currency = new Currency(4);
			this.Text = "<nl/>Reveal your hand. -<coin>1</coin> per Treasure card in your hand, to a minimum of <coin>0</coin>.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			player.ReturnHand(player.RevealHand());

			Currency negativeCurrency = new Currency(-Math.Min(player.Currency.Coin.Value, player.Hand[Cards.Category.Treasure].Count));
			player.ReceiveBenefit(this, new CardBenefit() { Currency = negativeCurrency });
		}
	}
	public class Procession : Card
	{
		private Boolean _CanCleanUp = true;

		public Procession()
			: base("Procession", Category.Action, Source.DarkAges, Location.Kingdom, Group.Trash | Group.Gain)
		{
			this.BaseCost = new Cost(4);
			this.Text = "You may play an Action card from your hand twice.  Trash it.  Gain an Action card costing exactly <coin>1</coin> more than it.";
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

			Choice choice = new Choice(String.Format("You may play an Action card twice", player), this, player.Hand[Cards.Category.Action], player, false, 0, 1);
			ChoiceResult result = player.MakeChoice(choice);

			if (result.Cards.Count > 0)
			{
				Card card = result.Cards[0];

				card.ModifiedBy = this;
				player.Actions++;
				PlayerMode previousPlayerMode = player.PutCardIntoPlay(card, String.Empty);
				Card logicalCard = card.LogicalCard;
				player.PlayCard(card.LogicalCard, previousPlayerMode);
				player.Actions++;
				previousPlayerMode = player.PutCardIntoPlay(card, "again");
				player.PlayCard(logicalCard, previousPlayerMode);

				this._CanCleanUp = logicalCard.CanCleanUp;

				if (player.InPlay.Contains(card))
					player.Trash(DeckLocation.InPlay, card);

				Cost trashedCardCost = player._Game.ComputeCost(card);
				SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply => supply.CanGain() && (supply.Category & Cards.Category.Action) == Cards.Category.Action && supply.CurrentCost == (trashedCardCost + new Coin(1)));
				Choice choiceGain = new Choice("Gain an Action card", this, gainableSupplies, player, false);
				ChoiceResult resultGain = player.MakeChoice(choiceGain);
				if (resultGain.Supply != null)
					player.Gain(resultGain.Supply);
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
	public class Rats : Card
	{
		private Player.TrashedEventHandler _TrashedEventHandler = null;

		public Rats()
			: base("Rats", Category.Action, Source.DarkAges, Location.Kingdom, Group.DeckReduction | Group.Gain | Group.PlusAction | Group.PlusCard | Group.ReactToTrashing | Group.RemoveCurses | Group.Trash)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Actions = 1;
			this.Benefit.Cards = 1;
			this.Text = "Gain a Rats. Trash a card from your hand other than a Rats (or reveal a hand of all Rats).<br/>When you trash this, +1 Card.";

			this.OwnerChanged += new OwnerChangedEventHandler(Rats_OwnerChanged);
		}

		public override void Setup(Game game, Supply supply)
		{
			base.Setup(game, supply);

			// Add 10 more cards to the Rats pile, bringing it to 20
			supply.AddTo(10);
		}

		internal override void TearDown()
		{
			Rats_OwnerChanged(this, new OwnerChangedEventArgs(this.Owner, null));

			base.TearDown();

			this.OwnerChanged -= new OwnerChangedEventHandler(Rats_OwnerChanged);
		}

		void Rats_OwnerChanged(object sender, OwnerChangedEventArgs e)
		{
			if (_TrashedEventHandler != null && e.OldOwner != null)
			{
				e.OldOwner.Trashed -= _TrashedEventHandler;
				_TrashedEventHandler = null;
			}

			if (e.NewOwner != null)
			{
				_TrashedEventHandler = new Player.TrashedEventHandler(player_Trashed);
				e.NewOwner.Trashed += _TrashedEventHandler;
			}
		}

		void player_Trashed(object sender, TrashEventArgs e)
		{
			Player player = sender as Player;

			// Already being processed or been handled -- don't need to process this one
			if (e.Actions.ContainsKey(TypeClass.Rats) || e.HandledBy.Contains(this))
				return;

			if (e.TrashedCards.Contains(this.PhysicalCard))
				e.Actions[TypeClass.Rats] = new TrashAction(this.Owner, this, "+1 Card", player_PlusCard, true);
		}

		internal void player_PlusCard(Player player, ref TrashEventArgs e)
		{
			player.ReceiveBenefit(this, new CardBenefit() { Cards = 1 });

			e.HandledBy.Add(this);
		}

		public override void Play(Player player)
		{
			base.Play(player);

			player.Gain(player._Game.Table[TypeClass.Rats]);

			CardCollection nonRats = player.Hand[c => c.CardType != TypeClass.Rats];
			if (nonRats.Count > 0)
			{
				Choice choiceTrash = new Choice("Choose a non-Rats card to trash", this, nonRats, player);
				ChoiceResult resultTrash = player.MakeChoice(choiceTrash);
				player.Trash(player.RetrieveCardsFrom(DeckLocation.Hand, resultTrash.Cards));
			}
			else
			{
				player.ReturnHand(player.RevealHand());
			}
		}
	}
	public class Rebuild : Card
	{
		public Rebuild()
			: base("Rebuild", Category.Action, Source.DarkAges, Location.Kingdom, Group.PlusAction | Group.Discard | Group.Trash | Group.Gain)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Actions = 1;
			this.Text = "<nl/>Name a card.  Reveal cards from the top of your deck until you reveal a Victory card that is not the named card.  Discard the other cards.  Trash the Victory card and gain a Victory card costing up to <coin>3</coin> more than it.";
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
			choice.AddCards(cards);

			ChoiceResult result = player.MakeChoice(choice);
			ICard namedCard = null;
			if (result.Supply != null)
				namedCard = result.Supply;
			else
				namedCard = result.Cards[0];

			player._Game.SendMessage(player, this, namedCard);

			Card foundCard = null;
			player.BeginDrawing();
			while (player.CanDraw)
			{
				player.Draw(DeckLocation.Revealed);
				Card lastRevealed = player.Revealed.Last();
				if ((lastRevealed.Category & Cards.Category.Victory) == Cards.Category.Victory &&
					namedCard.Name != lastRevealed.Name)
				{
					foundCard = lastRevealed;
					break;
				}
			}
			player.EndDrawing();

			if (foundCard != null)
				foundCard = player.RetrieveCardFrom(DeckLocation.Revealed, foundCard);

			player.DiscardRevealed();

			if (foundCard != null)
			{
				player.Trash(foundCard);

				Cost trashedCardCost = player._Game.ComputeCost(foundCard);
				SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(supply => supply.CanGain() && (supply.Category & Cards.Category.Victory) == Cards.Category.Victory && supply.CurrentCost <= (trashedCardCost + new Coin(3)));
				Choice choiceGain = new Choice("Gain a Victory card", this, gainableSupplies, player, false);
				ChoiceResult resultGain = player.MakeChoice(choiceGain);
				if (resultGain.Supply != null)
					player.Gain(resultGain.Supply);
			}
		}
	}
	public class Rogue : Card
	{
		public Rogue()
			: base("Rogue", Category.Action | Category.Attack, Source.DarkAges, Location.Kingdom, Group.PlusCoin | Group.Terminal | Group.Trash | Group.Discard | Group.Gain)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Currency.Coin.Value = 2;
			this.Text = "If there are any cards in the trash costing from <coin>3</coin> to <coin>6</coin>, gain one of them.  Otherwise, each other player reveals the top 2 cards of his deck, trashes one of them costing from <coin>3</coin> to <coin>6</coin>, and discards the rest.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			List<Cost> availableCosts = new List<Cost>() { new Cost(3), new Cost(4), new Cost(5), new Cost(6) };
			IEnumerable<Card> availableTrashCards = player._Game.Table.Trash.Where(c => availableCosts.Any(cost => player._Game.ComputeCost(c) == cost));

			if (availableTrashCards.Count() > 0)
			{
				Choice choiceFromTrash = new Choice("Choose a card to gain from the trash", this, availableTrashCards, player);
				ChoiceResult resultFromTrash = player.MakeChoice(choiceFromTrash);
				if (resultFromTrash.Cards.Count > 0)
				{
					player.Gain(player._Game.Table.Trash, resultFromTrash.Cards[0]);
				}
			}
			else
			{
				IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
				enumerator.MoveNext();
				while (enumerator.MoveNext())
				{
					Player attackee = enumerator.Current;
					// Skip if the attack is blocked (Moat, Lighthouse, etc.)
					if (this.IsAttackBlocked[attackee])
						continue;

					CardCollection attackeeCards = attackee.Draw(2, DeckLocation.Revealed);
					Choice choiceTrash = new Choice("Choose a card to trash", this, attackee.Revealed[c => availableCosts.Any(cost => player._Game.ComputeCost(c) == cost)], attackee);
					ChoiceResult resultTrash = attackee.MakeChoice(choiceTrash);
					attackee.Trash(attackee.RetrieveCardsFrom(DeckLocation.Revealed, resultTrash.Cards));
					attackee.DiscardRevealed();
				}
			}
		}
	}
	public class RuinedLibrary : Card
	{
		public RuinedLibrary()
			: base("Ruined Library", Category.Action | Category.Ruins, Source.DarkAges, Location.General, Group.PlusCard | Group.Terminal)
		{
			this.BaseCost = new Cost(0);
			this.Benefit.Cards = 1;
		}

		public override Type BaseType { get { return TypeClass.RuinsSupply; } }
	}
	public class RuinedMarket : Card
	{
		public RuinedMarket()
			: base("Ruined Market", Category.Action | Category.Ruins, Source.DarkAges, Location.General, Group.PlusBuy | Group.Terminal)
		{
			this.BaseCost = new Cost(0);
			this.Benefit.Buys = 1;
		}

		protected override Boolean AllowUndo { get { return true; } }

		public override Type BaseType { get { return TypeClass.RuinsSupply; } }
	}
	public class RuinedVillage : Card
	{
		public RuinedVillage()
			: base("Ruined Village", Category.Action | Category.Ruins, Source.DarkAges, Location.General, Group.PlusAction)
		{
			this.BaseCost = new Cost(0);
			this.Benefit.Actions = 1;
		}

		protected override Boolean AllowUndo { get { return true; } }

		public override Type BaseType { get { return TypeClass.RuinsSupply; } }
	}
	public class RuinsSupply : Card
	{
		public RuinsSupply()
			: base("Ruins", Category.Ruins, Source.DarkAges, Location.General, Group.None)
		{
		}

		public override void Setup(Game game, Supply supply)
		{
			base.Setup(game, supply);

			CardCollection cards = new CardCollection();
			for (int i = 0; i < 10; i++)
			{
				cards.Add(new AbandonedMine());
				cards.Add(new RuinedLibrary());
				cards.Add(new RuinedMarket());
				cards.Add(new RuinedVillage());
				cards.Add(new Survivors());
			}

			Utilities.Shuffler.Shuffle<Card>(cards);

			supply.AddTo(cards.Take(10 * (game.Players.Count - 1)));
		}
	}
	public class Sage : Card
	{
		public Sage()
			: base("Sage", Category.Action, Source.DarkAges, Location.Kingdom, Group.Discard | Group.PlusAction | Group.PlusCard)
		{
			this.BaseCost = new Cost(3);
			this.Benefit.Actions = 1;
			this.Text = "<nl/>Reveal cards from the top of your deck until you reveal one costing <coin>3</coin> or more. Put that card into your hand and discard the rest.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			player.BeginDrawing();
			while (player.CanDraw)
			{
				player.Draw(DeckLocation.Revealed);
				if (player._Game.ComputeCost(player.Revealed.Last()) >= new Cost(3))
					break;
			}
			player.EndDrawing();

			if (player.Revealed.Count > 0)
			{
				Card lastCard = player.Revealed.Last();
				if (player._Game.ComputeCost(player.Revealed.Last()) >= new Cost(3))
					player.AddCardToHand(player.RetrieveCardFrom(DeckLocation.Revealed, lastCard));
			}

			player.DiscardRevealed();
		}
	}
	public class Scavenger : Card
	{
		public Scavenger()
			: base("Scavenger", Category.Action, Source.DarkAges, Location.Kingdom, Group.PlusCoin | Group.Terminal | Group.CardOrdering)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Currency.Coin.Value = 2;
			this.Text = "<nl/>You may put your deck into your discard pile.  Look through your discard pile and put one card from it on top of your deck.";
		}

		public override void Play(Player player)
		{
			base.Play(player);
			Choice choice = Choice.CreateYesNoChoice("You may put your deck into your discard pile.", this, this, player, null);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Options[0] == "Yes")
			{
				player._Game.SendMessage(player, this);
				CardCollection cc = player.RetrieveCardsFrom(DeckLocation.Deck);
				player.AddCardsInto(DeckLocation.Discard, cc);
			}

			CardCollection cards = player.DiscardPile.LookThrough(c => true);
			Choice choiceTop = new Choice("Choose a card to put onto your deck", this, cards, player, false, 0, 1);
			ChoiceResult resultTop = player.MakeChoice(choiceTop);
			if (resultTop.Cards.Count > 0)
				player.AddCardsToDeck(player.DiscardPile.Retrieve(player, c => resultTop.Cards.Contains(c)), DeckPosition.Top);
		}
	}
	public class Shelters : Card
	{
		public Shelters()
			: base("Shelters", Category.Shelter, Source.DarkAges, Location.Invisible, Group.None)
		{
		}

		public override void Setup(Game game, Supply supply)
		{
			for (int i = 0; i < game.Players.Count; i++)
			{
				game.Table.Estate.AddTo(new Hovel());
				game.Table.Estate.AddTo(new Necropolis());
				game.Table.Estate.AddTo(new OvergrownEstate());
			}
		}
	}
	public class SirBailey : Knight
	{
		public SirBailey()
			: base("Sir Bailey", Category.Action | Category.Attack | Category.Knight, Group.PlusCard | Group.PlusAction | Group.Trash | Group.Discard)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 1;
			this.Text = "Each other player reveals the top 2 cards of his deck, trashes one of them costing from <coin>3</coin> to <coin>6</coin>, and discards the rest.  If a Knight is trashed by this, trash this card.";
		}

		public override Type BaseType { get { return TypeClass.Knights; } }

		public override void Play(Player player)
		{
			base.Play(player);

			this.KnightAttack(player);
		}
	}
	public class SirDestry : Knight
	{
		public SirDestry()
			: base("Sir Destry", Category.Action | Category.Attack | Category.Knight, Group.PlusCard | Group.Terminal | Group.Trash | Group.Discard)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Cards = 2;
			this.Text = "Each other player reveals the top 2 cards of his deck, trashes one of them costing from <coin>3</coin> to <coin>6</coin>, and discards the rest.  If a Knight is trashed by this, trash this card.";
		}

		public override Type BaseType { get { return TypeClass.Knights; } }

		public override void Play(Player player)
		{
			base.Play(player);

			this.KnightAttack(player);
		}
	}
	public class SirMartin : Knight
	{
		public SirMartin()
			: base("Sir Martin", Category.Action | Category.Attack | Category.Knight, Group.PlusBuy | Group.Terminal | Group.Trash | Group.Discard)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Buys = 2;
			this.Text = "Each other player reveals the top 2 cards of his deck, trashes one of them costing from <coin>3</coin> to <coin>6</coin>, and discards the rest.  If a Knight is trashed by this, trash this card.";
		}

		public override Type BaseType { get { return TypeClass.Knights; } }

		public override void Play(Player player)
		{
			base.Play(player);

			this.KnightAttack(player);
		}
	}
	public class SirMichael : Knight
	{
		public SirMichael()
			: base("Sir Michael", Category.Action | Category.Attack | Category.Knight, Group.Terminal | Group.Trash | Group.Discard)
		{
			this.BaseCost = new Cost(5);
			this.Text = "Each other player discards down to 3 cards in hand.<nl/>Each other player reveals the top 2 cards of his deck, trashes one of them costing from <coin>3</coin> to <coin>6</coin>, and discards the rest.  If a Knight is trashed by this, trash this card.";
		}

		public override Type BaseType { get { return TypeClass.Knights; } }

		public override void Play(Player player)
		{
			base.Play(player);

			IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
			enumerator.MoveNext();
			while (enumerator.MoveNext())
			{
				Player attackee = enumerator.Current;
				if (this.IsAttackBlocked[attackee])
					continue;

				Choice choice = new Choice("Choose cards to discard.  You must discard down to 3 cards in hand", this, attackee.Hand, attackee, false, attackee.Hand.Count - 3, attackee.Hand.Count - 3);
				ChoiceResult result = attackee.MakeChoice(choice);
				attackee.Discard(DeckLocation.Hand, result.Cards);
			}

			this.KnightAttack(player);
		}
	}
	public class SirVander : Knight
	{
		private Player.TrashedEventHandler _TrashedEventHandler = null;

		public SirVander()
			: base("Sir Vander", Category.Action | Category.Attack | Category.Knight, Group.Terminal | Group.Trash | Group.Discard | Group.ReactToTrashing | Group.Gain)
		{
			this.BaseCost = new Cost(5);
			this.Text = "Each other player reveals the top 2 cards of his deck, trashes one of them costing from <coin>3</coin> to <coin>6</coin>, and discards the rest.  If a Knight is trashed by this, trash this card.<br/>When you trash this, gain a Gold.";

			this.OwnerChanged += new OwnerChangedEventHandler(SirVander_OwnerChanged);
		}

		public override Type BaseType { get { return TypeClass.Knights; } }

		internal override void TearDown()
		{
			SirVander_OwnerChanged(this, new OwnerChangedEventArgs(this.Owner, null));

			base.TearDown();

			this.OwnerChanged -= new OwnerChangedEventHandler(SirVander_OwnerChanged);
		}

		void SirVander_OwnerChanged(object sender, OwnerChangedEventArgs e)
		{
			if (_TrashedEventHandler != null && e.OldOwner != null)
			{
				e.OldOwner.Trashed -= _TrashedEventHandler;
				_TrashedEventHandler = null;
			}

			if (e.NewOwner != null)
			{
				_TrashedEventHandler = new Player.TrashedEventHandler(player_Trashed);
				e.NewOwner.Trashed += _TrashedEventHandler;
			}
		}

		void player_Trashed(object sender, TrashEventArgs e)
		{
			Player player = sender as Player;

			// Already being processed or been handled -- don't need to process this one
			if (e.Actions.ContainsKey(TypeClass.SirVander) || e.HandledBy.Contains(this))
				return;

			if (e.TrashedCards.Contains(this.PhysicalCard))
				e.Actions[TypeClass.SirVander] = new TrashAction(this.Owner, this, "Gain a Gold", player_GainGold, true);
		}

		internal void player_GainGold(Player player, ref TrashEventArgs e)
		{
			player.Gain(player._Game.Table.Gold);

			e.HandledBy.Add(this);
		}

		public override void Play(Player player)
		{
			base.Play(player);

			this.KnightAttack(player);
		}
	}
	public class Spoils : Card
	{
		public const int BaseCount = 15;

		public Spoils()
			: base("Spoils", Category.Treasure, Source.DarkAges, Location.Special, Group.DeckReduction | Group.PlusCoin)
		{
			this.BaseCost = new Cost(0, true, false);
			this.Benefit.Currency = new Currency(3);
			this.Text = "<nl/>When you play this, return it to the Spoils pile.<nl/><i>(This is not in the Supply.)</i>";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			if (player.InPlay.Contains(this.PhysicalCard))
			{
				Card cardToReturn = player.RetrieveCardFrom(DeckLocation.InPlay, this.PhysicalCard);
				Supply supply = player._Game.Table.SpecialPiles[TypeClass.Spoils];
				player.Lose(this);
				supply.AddTo(this);
				player._Game.SendMessage(player, this, supply, 1);
			}
		}
	}
	public class Squire : Card
	{
		private Player.TrashedEventHandler _TrashedEventHandler = null;

		public Squire()
			: base("Squire", Category.Action, Source.DarkAges, Location.Kingdom, Group.Gain | Group.PlusAction | Group.PlusBuy | Group.PlusCoin | Group.PlusMultipleActions | Group.ReactToTrashing | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(2);
			this.Benefit.Currency = new Currency(1);
			this.Text = "Choose one: +2<nbsp/>Actions; or +2<nbsp/>Buys; or gain a Silver.<br/>When you trash this,<nl/>gain an Attack card.";

			this.OwnerChanged += new OwnerChangedEventHandler(Squire_OwnerChanged);
		}

		internal override void TearDown()
		{
			Squire_OwnerChanged(this, new OwnerChangedEventArgs(this.Owner, null));

			base.TearDown();

			this.OwnerChanged -= new OwnerChangedEventHandler(Squire_OwnerChanged);
		}

		void Squire_OwnerChanged(object sender, OwnerChangedEventArgs e)
		{
			if (_TrashedEventHandler != null && e.OldOwner != null)
			{
				e.OldOwner.Trashed -= _TrashedEventHandler;
				_TrashedEventHandler = null;
			}

			if (e.NewOwner != null)
			{
				_TrashedEventHandler = new Player.TrashedEventHandler(player_Trashed);
				e.NewOwner.Trashed += _TrashedEventHandler;
			}
		}

		void player_Trashed(object sender, TrashEventArgs e)
		{
			Player player = sender as Player;

			// Already being processed or been handled -- don't need to process this one
			if (e.Actions.ContainsKey(TypeClass.Squire) || e.HandledBy.Contains(this))
				return;

			if (e.TrashedCards.Contains(this.PhysicalCard))
				e.Actions[TypeClass.Squire] = new TrashAction(this.Owner, this, "Gain an Attack card", player_GainAttack, true);
		}

		internal void player_GainAttack(Player player, ref TrashEventArgs e)
		{
			SupplyCollection gainableSupplies = player._Game.Table.Supplies.FindAll(
				supply => supply.CanGain() &&
					((supply.Category & Cards.Category.Attack) == Cards.Category.Attack));
			Choice choice = new Choice("Gain an Attack card", this, gainableSupplies, player, false);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Supply != null)
				player.Gain(result.Supply);

			e.HandledBy.Add(this);
		}

		public override void Play(Player player)
		{
			base.Play(player);

			CardBenefit benefit = new CardBenefit();

			Choice choice = new Choice("Choose one:", this, new CardCollection() { this }, new List<string>() { "+2<nbsp/>Actions", "+2<nbsp/>Buys", "Gain a Silver" }, player);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Options.Contains("+2<nbsp/>Actions"))
				benefit.Actions = 2;
			else if (result.Options.Contains("+2<nbsp/>Buys"))
				benefit.Buys = 2;
			else
				player.Gain(player._Game.Table.Silver);

			player.ReceiveBenefit(this, benefit);
		}
	}
	public class Storeroom : Card
	{
		public Storeroom()
			: base("Storeroom", Category.Action, Source.DarkAges, Location.Kingdom, Group.PlusBuy | Group.Terminal | Group.Discard | Group.PlusCard | Group.PlusCoin | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(3);
			this.Benefit.Buys = 1;
			this.Text = "<nl/>Discard any number of cards.<nl/>+1 Card per card discarded.<nl/>Discard any number of cards.<nl/>+<coin>1</coin> per card discarded the second time.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choiceDiscardTheFirst = new Choice("Discard any number of cards.  +1 Card per card discarded", this, player.Hand, player, false, 0, player.Hand.Count);
			ChoiceResult resultDiscardTheFirst = player.MakeChoice(choiceDiscardTheFirst);
			player.Discard(DeckLocation.Hand, resultDiscardTheFirst.Cards);

			player.ReceiveBenefit(this, new CardBenefit() { Cards = resultDiscardTheFirst.Cards.Count });


			Choice choiceDiscardTheSecond = new Choice("Discard any number of cards.  +<coin>1</coin> per card discarded.", this, player.Hand, player, false, 0, player.Hand.Count);
			ChoiceResult resultDiscardTheSecond = player.MakeChoice(choiceDiscardTheSecond);
			player.Discard(DeckLocation.Hand, resultDiscardTheSecond.Cards);

			player.ReceiveBenefit(this, new CardBenefit() { Currency = new Currency(resultDiscardTheSecond.Cards.Count) });

		}
	}
	public class Survivors : Card
	{
		public Survivors()
			: base("Survivors", Category.Action | Category.Ruins, Source.DarkAges, Location.General, Group.CardOrdering | Group.Discard | Group.Terminal)
		{
			this.BaseCost = new Cost(0);
			this.Text = "Look at the top 2 cards of your deck.  Discard them or put them back in any order.";
		}

		public override Type BaseType { get { return TypeClass.RuinsSupply; } }

		public override void Play(Player player)
		{
			base.Play(player);

			CardCollection newCards = player.Draw(2, DeckLocation.Private);

			if (newCards.Count > 0)
			{
				Choice choice = new Choice(
					String.Format("Do you want to discard {0} or put {1} back on top?", String.Join(" and ", newCards.Select(c => c.Name)), newCards.Count == 1 ? "it" : "them"),
					this,
					newCards,
					new List<string>() { "Discard", String.Format("Put {0} back", newCards.Count == 1 ? "it" : "them") },
					player);
				ChoiceResult result = player.MakeChoice(choice);
				if (result.Options[0] == "Discard")
					player.Discard(DeckLocation.Private);
				else
				{
					Choice replaceChoice = new Choice("Choose order of cards to put back on your deck", this, player.Private, player, true, 2, 2);
					ChoiceResult replaceResult = player.MakeChoice(replaceChoice);
					player.RetrieveCardsFrom(DeckLocation.Private);
					player.AddCardsToDeck(replaceResult.Cards, DeckPosition.Top);
				}
			}
		}

	}
	public class Urchin : Card
	{
		private Player.CardPutIntoPlayEventHandler _CardPutIntoPlayEventHandler = null;

		public Urchin()
			: base("Urchin", Category.Action | Category.Attack, Source.DarkAges, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.Discard | Group.Trash | Group.Gain)
		{
			this.BaseCost = new Cost(3);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 1;
			this.Text = "Each other player discards down to 4 cards in hand.<br/>When you play another Attack card with this in play, you may trash this.  If you do, gain a Mercenary from the Mercenary pile.";
		}

		public override void Setup(Game game, Supply supply)
		{
			base.Setup(game, supply);
			if (!game.Table.SpecialPiles.ContainsKey(TypeClass.Mercenary))
			{
				Supply mercenarySupply = new Supply(game, game.Players, TypeClass.Mercenary, Mercenary.BaseCount);
				mercenarySupply.FullSetup();
				game.Table.SpecialPiles.Add(TypeClass.Mercenary, mercenarySupply);
			}
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

				Choice choice = new Choice("Choose cards to discard.  You must discard down to 4 cards in hand", this, attackee.Hand, attackee, false, attackee.Hand.Count - 4, attackee.Hand.Count - 4);
				ChoiceResult result = attackee.MakeChoice(choice);
				attackee.Discard(DeckLocation.Hand, result.Cards);
			}

			if (_CardPutIntoPlayEventHandler != null)
				player.CardPutIntoPlay -= _CardPutIntoPlayEventHandler;
			_CardPutIntoPlayEventHandler = new Player.CardPutIntoPlayEventHandler(ActivePlayer_CardPutIntoPlay);
			player.CardPutIntoPlay += _CardPutIntoPlayEventHandler;
		}

		void ActivePlayer_CardPutIntoPlay(object sender, CardPutIntoPlayEventArgs e)
		{
			if (e.Card != this && (e.Card.Category & Cards.Category.Attack) == Cards.Category.Attack)
			{
				Choice choicePlayer = Choice.CreateYesNoChoice(String.Format("Do you want to trash {0}?", this.PhysicalCard), this, e.Player);
				ChoiceResult resultPlayer = e.Player.MakeChoice(choicePlayer);
				if (resultPlayer.Options[0] == "Yes")
				{
					if (e.Player.InPlay.Contains(this.PhysicalCard))
						e.Player.Trash(e.Player.RetrieveCardFrom(DeckLocation.InPlay, this.PhysicalCard));
					else if (e.Player.SetAside.Contains(this.PhysicalCard))
						e.Player.Trash(e.Player.RetrieveCardFrom(DeckLocation.SetAside, this.PhysicalCard));
					else
						return;

					e.Player.Gain(e.Player._Game.Table.SpecialPiles[TypeClass.Mercenary]);
				}
			}
		}

		public override void RemovedFrom(DeckLocation location, Player player)
		{
			base.RemovedFrom(location, player);
			if (_CardPutIntoPlayEventHandler != null)
				player.CardPutIntoPlay -= _CardPutIntoPlayEventHandler;
			_CardPutIntoPlayEventHandler = null;
		}
	}
	public class Vagrant : Card
	{
		public Vagrant()
			: base("Vagrant", Category.Action, Source.DarkAges, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.CardOrdering | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(2);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 1;
			this.Text = "<nl/>Reveal the top card of your deck.  If it's a Curse, Ruins, Shelter, or Victory card, put it into your hand.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			CardCollection newCards = player.Draw(1, DeckLocation.Revealed);

			player.AddCardsToHand(player.RetrieveCardsFrom(DeckLocation.Revealed, 
				c => (c.Category & Cards.Category.Curse) == Cards.Category.Curse ||
					(c.Category & Cards.Category.Ruins) == Cards.Category.Ruins ||
					(c.Category & Cards.Category.Shelter) == Cards.Category.Shelter ||
					(c.Category & Cards.Category.Victory) == Cards.Category.Victory
				));

			player.AddCardsToDeck(player.RetrieveCardsFrom(DeckLocation.Revealed), DeckPosition.Top);
		}
	}
	public class WanderingMinstrel : Card
	{
		public WanderingMinstrel()
			: base("Wandering Minstrel", Category.Action, Source.DarkAges, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.PlusMultipleActions | Group.CardOrdering | Group.Discard)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 2;
			this.Text = "<nl/>Reveal the top 3 cards of your deck.  Put the Actions back on top in any order and discard the rest.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			player.Draw(3, DeckLocation.Revealed);

			CardCollection actionCards = player.Revealed[Cards.Category.Action];
			Choice replaceChoice = new Choice("Choose order of cards to put back on your deck", this, actionCards, player, true, actionCards.Count, actionCards.Count);
			ChoiceResult replaceResult = player.MakeChoice(replaceChoice);
			player.AddCardsToDeck(player.RetrieveCardsFrom(DeckLocation.Revealed, replaceResult.Cards), DeckPosition.Top);

			player.DiscardRevealed();
		}
	}
}
