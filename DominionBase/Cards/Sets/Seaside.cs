using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using DominionBase.Currencies;
using DominionBase.Piles;
using DominionBase.Players;

namespace DominionBase.Cards.Seaside
{
	public static class TypeClass
	{
		public static Type Ambassador = typeof(Ambassador);
		public static Type Bazaar = typeof(Bazaar);
		public static Type Caravan = typeof(Caravan);
		public static Type Cutpurse = typeof(Cutpurse);
		public static Type Embargo = typeof(Embargo);
		public static Type Explorer = typeof(Explorer);
		public static Type FishingVillage = typeof(FishingVillage);
		public static Type GhostShip = typeof(GhostShip);
		public static Type Haven = typeof(Haven);
		public static Type Island = typeof(Island);
		public static Type Lighthouse = typeof(Lighthouse);
		public static Type Lookout = typeof(Lookout);
		public static Type MerchantShip = typeof(MerchantShip);
		public static Type NativeVillage = typeof(NativeVillage);
		public static Type Navigator = typeof(Navigator);
		public static Type Outpost = typeof(Outpost);
		public static Type PearlDiver = typeof(PearlDiver);
		public static Type PirateShip = typeof(PirateShip);
		public static Type Salvager = typeof(Salvager);
		public static Type SeaHag = typeof(SeaHag);
		public static Type Smugglers = typeof(Smugglers);
		public static Type Tactician = typeof(Tactician);
		public static Type TreasureMap = typeof(TreasureMap);
		public static Type Treasury = typeof(Treasury);
		public static Type Warehouse = typeof(Warehouse);
		public static Type Wharf = typeof(Wharf);

		public static Type EmbargoToken = typeof(EmbargoToken);
		public static Type PirateShipToken = typeof(PirateShipToken);
		public static Type IslandMat = typeof(IslandMat);
		public static Type NativeVillageMat = typeof(NativeVillageMat);
	}

	public class Ambassador : Card
	{
		public Ambassador()
			: base("Ambassador", Category.Action | Category.Attack, Source.Seaside, Location.Kingdom, Group.DeckReduction | Group.PlusCurses | Group.RemoveCurses | Group.Terminal)
		{
			this.BaseCost = new Cost(3);
			this.Text = "Reveal a card from your hand.<nl/>Return up to 2 copies of it from your hand to the Supply.  Then each other player gains a copy of it.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choiceCard = new Choice("Reveal a card from your hand to return up to 2 to the Supply.", this, player.Hand, player);
			ChoiceResult resultCard = player.MakeChoice(choiceCard);

			if (resultCard.Cards.Count > 0)
			{
				Card revealedCard = resultCard.Cards[0];
				player.AddCardInto(DeckLocation.Revealed, player.RetrieveCardFrom(DeckLocation.Hand, revealedCard));
				player.AddCardInto(DeckLocation.Hand, player.RetrieveCardFrom(DeckLocation.Revealed, revealedCard));

				Supply supply = player._Game.Table.FindSupplyPileByCard(revealedCard);
				if (supply != null)
				{
					List<String> options = new List<string>() { "0", "1" };
					if (player.Hand[revealedCard.CardType].Count > 1)
						options.Add("2");
					Choice choice = new Choice("How many would you like to return to the Supply?", this, new CardCollection() { revealedCard }, options, player);
					ChoiceResult result = player.MakeChoice(choice);

					int numberToReturn = int.Parse(result.Options[0]);
					if (numberToReturn > 0)
					{
						CardCollection cardsToReturn = player.RetrieveCardsFrom(DeckLocation.Hand, revealedCard.CardType, numberToReturn);
						player.Lose(cardsToReturn);
						supply.AddTo(cardsToReturn);
					}

					player._Game.SendMessage(player, this, supply, numberToReturn);
				}

				IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
				enumerator.MoveNext();
				while (enumerator.MoveNext())
				{
					Player attackee = enumerator.Current;
					// Skip if the attack is blocked (Moat, Lighthouse, etc.)
					if (this.IsAttackBlocked[attackee])
						continue;

					if (supply != null && supply.CanGain() && supply.TopCard.Name == revealedCard.Name)
						attackee.Gain(supply);
				}
			}
		}
	}
	public class Bazaar : Card
	{
		public Bazaar()
			: base("Bazaar", Category.Action, Source.Seaside, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.PlusMultipleActions | Group.PlusCoin)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Actions = 2;
			this.Benefit.Cards = 1;
			this.Benefit.Currency.Coin.Value = 1;
		}
	}
	public class Caravan : Card
	{
		private Boolean _CanCleanUp = true;
		private Player.TurnStartedEventHandler _TurnStartedEventHandler = null;
		private Player _TurnStartedPlayer = null;

		public Caravan()
			: base("Caravan", Category.Action | Category.Duration, Source.Seaside, Location.Kingdom, Group.PlusCard | Group.PlusAction)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Actions = 1;
			this.Benefit.Cards = 1;
			this.DurationBenefit.Cards = 1;
		}

		internal override void TearDown()
		{
			base.TearDown();

			if (_TurnStartedEventHandler != null && _TurnStartedPlayer != null)
				_TurnStartedPlayer.TurnStarted -= _TurnStartedEventHandler;
			_TurnStartedPlayer = null;
			_TurnStartedEventHandler = null;
		}

		public override Boolean CanCleanUp { get { return this._CanCleanUp; } }

		public override void AddedTo(DeckLocation location, Player player)
		{
			base.AddedTo(location, player);
			switch (location)
			{
				case DeckLocation.InPlay:
					this._CanCleanUp = false;
					break;

				case DeckLocation.SetAside:
					if (_TurnStartedEventHandler != null)
						player.TurnStarted -= _TurnStartedEventHandler;
					_TurnStartedPlayer = player;
					_TurnStartedEventHandler = new Player.TurnStartedEventHandler(player_TurnStarted);
					_TurnStartedPlayer.TurnStarted += _TurnStartedEventHandler;
					this._CanCleanUp = true;
					break;

				default:
					this._CanCleanUp = true;
					break;
			}
		}

		void player_TurnStarted(object sender, TurnStartedEventArgs e)
		{
			String key = this.ToString();
			if (!e.Actions.ContainsKey(key))
				e.Actions[key] = new TurnStartedAction(e.Player, this, String.Format("Play {0}", this.PhysicalCard), player_Action, true);
		}

		internal void player_Action(Player player, ref TurnStartedEventArgs e)
		{
			this.PlayDuration(e.Player);
			if (_TurnStartedEventHandler != null)
				e.Player.TurnStarted -= _TurnStartedEventHandler;
			_TurnStartedPlayer = null;
			_TurnStartedEventHandler = null;
		}
	}
	public class Cutpurse : Card
	{
		public Cutpurse()
			: base("Cutpurse", Category.Action | Category.Attack, Source.Seaside, Location.Kingdom, Group.PlusCoin | Group.Discard | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Currency.Coin.Value = 2;
			this.Text = "Each other player discards a Copper card (or reveals a hand with no Copper).";
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

				if (attackee.Hand.LookThrough(c => c.CardType == Cards.Universal.TypeClass.Copper).Count > 0)
				{
					attackee.Discard(DeckLocation.Hand, Cards.Universal.TypeClass.Copper, 1);
				}
				else
				{
					attackee.ReturnHand(attackee.RevealHand());
				}
			}
		}
	}
	public class Embargo : Card
	{
		public Embargo()
			: base("Embargo", Category.Action, Source.Seaside, Location.Kingdom, Group.Component | Group.PlusCurses | Group.PlusCoin | Group.Gain | Group.Trash | Group.Terminal)
		{
			this.BaseCost = new Cost(2);
			this.Benefit.Currency.Coin.Value = 2;
			this.Text = "Trash this card. Put an Embargo token on top of a Supply pile.<br/>When a player buys a card, he gains a Curse card per Embargo token on that pile.";
		}

		public override void Play(Player player)
		{
			base.Play(player);
			if (player.InPlay.Contains(this.PhysicalCard))
				player.Trash(player.RetrieveCardFrom(DeckLocation.InPlay, this.PhysicalCard));

			Choice choice = new Choice("Choose a supply pile to put an Embargo token on", this, player._Game.Table.Supplies, player, false);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Supply != null)
			{
				player._Game.SendMessage(player, this, result.Supply);
				result.Supply.AddToken(new EmbargoToken(player._Game, result.Supply));
			}
		}
	}
	public class EmbargoToken : Token
	{
		private Dictionary<Player, Player.CardBoughtEventHandler> _CardBoughtHandlers = new Dictionary<Player, Player.CardBoughtEventHandler>();
		private Supply _SupplyPile = null;

		public EmbargoToken(Game game, Supply supply)
			: base("<vp/>", "Embargo token")
		{
			_SupplyPile = supply;

			IEnumerator<Player> enumPlayers = game.GetPlayersStartingWithActiveEnumerator();
			while (enumPlayers.MoveNext())
			{
				_CardBoughtHandlers[enumPlayers.Current] = new Player.CardBoughtEventHandler(player_CardBought);
				enumPlayers.Current.CardBought += _CardBoughtHandlers[enumPlayers.Current];
			}
		}

		internal override void TearDown()
		{
			base.TearDown();

			_SupplyPile = null;
			foreach (Player playerLoop in _CardBoughtHandlers.Keys)
				playerLoop.CardBought -= _CardBoughtHandlers[playerLoop];
			_CardBoughtHandlers.Clear();
		}

		void player_CardBought(object sender, Players.CardBuyEventArgs e)
		{
			// This is not the card you are looking for
			if (e.Card.CardType != _SupplyPile.CardType)
				return;

			if (e.Actions.ContainsKey(TypeClass.EmbargoToken) || e.HandledBy.Contains(this) || !e.Game.Table.Curse.CanGain())
				return;

			Player player = sender as Player;
			e.Actions[TypeClass.EmbargoToken] = new Players.CardBuyAction(player, e.Card, "gain a Curse", player_BuyCursePile, true);
		}

		internal void player_BuyCursePile(Player player, ref Players.CardBuyEventArgs e)
		{
			player.Gain(e.Game.Table.Curse);

			e.HandledBy.Add(this);
		}

		public override string Title { get { return "A player that buys a card off this supply pile will gain 1 Curse card for each of these tokens on it"; } }
	}
	public class Explorer : Card
	{
		public Explorer()
			: base("Explorer", Category.Action, Source.Seaside, Location.Kingdom, Group.Gain | Group.Terminal)
		{
			this.BaseCost = new Cost(5);
			this.Text = "You may reveal a Province card from your hand.  If you do, gain a Gold card, putting it into your hand.  Otherwise, gain a Silver card, putting it into your hand.";
		}

		public override void Play(Player player)
		{
			base.Play(player);
			Boolean provinceRevealed = false;
			if (player.Hand[Cards.Universal.TypeClass.Province].Count > 0)
			{
				Choice choice = Choice.CreateYesNoChoice("You may reveal a Province card to gain a Gold in your hand.  Otherwise, gain a Silver in your hand.  Do you want to reveal?", this, player);
				ChoiceResult result = player.MakeChoice(choice);
				if (result.Options.Contains("Yes"))
				{
					CardCollection singleProvince = player.RetrieveCardsFrom(DeckLocation.Hand, Cards.Universal.TypeClass.Province, 1);
					player.AddCardInto(DeckLocation.Revealed, singleProvince[0]);
					provinceRevealed = true;
					player.Gain(player._Game.Table.Gold, DeckLocation.Hand, DeckPosition.Bottom);
					player.AddCardsToHand(DeckLocation.Revealed);
				}
			}
			if (!provinceRevealed)
				player.Gain(player._Game.Table.Silver, DeckLocation.Hand, DeckPosition.Bottom);
		}
	}
	public class FishingVillage : Card
	{
		private Boolean _CanCleanUp = true;
		private Player.TurnStartedEventHandler _TurnStartedEventHandler = null;
		private Player _TurnStartedPlayer = null;

		public FishingVillage()
			: base("Fishing Village", Category.Action | Category.Duration, Source.Seaside, Location.Kingdom, Group.PlusAction | Group.PlusMultipleActions | Group.PlusCoin)
		{
			this.BaseCost = new Cost(3);
			this.Benefit.Actions = 2;
			this.Benefit.Currency.Coin.Value = 1;
			this.DurationBenefit.Actions = 1;
			this.DurationBenefit.Currency.Coin.Value = 1;
		}

		internal override void TearDown()
		{
			base.TearDown();

			if (_TurnStartedEventHandler != null && _TurnStartedPlayer != null)
				_TurnStartedPlayer.TurnStarted -= _TurnStartedEventHandler;
			_TurnStartedPlayer = null;
			_TurnStartedEventHandler = null;
		}

		protected override Boolean AllowUndo { get { return true; } }

		public override Boolean CanCleanUp { get { return this._CanCleanUp; } }

		public override void AddedTo(DeckLocation location, Player player)
		{
			base.AddedTo(location, player);
			switch (location)
			{
				case DeckLocation.InPlay:
					this._CanCleanUp = false;
					break;

				case DeckLocation.SetAside:
					if (_TurnStartedEventHandler != null)
						player.TurnStarted -= _TurnStartedEventHandler;
					_TurnStartedPlayer = player;
					_TurnStartedEventHandler = new Player.TurnStartedEventHandler(player_TurnStarted);
					_TurnStartedPlayer.TurnStarted += _TurnStartedEventHandler;
					this._CanCleanUp = true;
					break;

				default:
					this._CanCleanUp = true;
					break;
			}
		}

		void player_TurnStarted(object sender, TurnStartedEventArgs e)
		{
			String key = this.ToString();
			if (!e.Actions.ContainsKey(key))
				e.Actions[key] = new TurnStartedAction(e.Player, this, String.Format("Play {0}", this.PhysicalCard), player_Action, true);
		}

		internal void player_Action(Player player, ref TurnStartedEventArgs e)
		{
			this.PlayDuration(e.Player);
			if (_TurnStartedEventHandler != null)
				e.Player.TurnStarted -= _TurnStartedEventHandler;
			_TurnStartedPlayer = null;
			_TurnStartedEventHandler = null;
		}
	}
	public class GhostShip : Card
	{
		public GhostShip()
			: base("Ghost Ship", Category.Action | Category.Attack, Source.Seaside, Location.Kingdom, Group.PlusCard | Group.Terminal)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Cards = 2;
			this.Text = "Each other player with 4 or more cards in hand puts cards from his hand on top of his deck until he has 3 cards in his hand.";
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

				Choice choice = new Choice("Choose cards to put on top of your deck, until you have 3 cards in hand.", this, attackee.Hand, player, true, attackee.Hand.Count - 3, attackee.Hand.Count - 3);
				ChoiceResult result = attackee.MakeChoice(choice);
				attackee.RetrieveCardsFrom(DeckLocation.Hand, result.Cards);
				attackee.AddCardsToDeck(result.Cards, DeckPosition.Top);
			}
		}
	}
	public class Haven : Card
	{
		private CardCollection _HavenedCards = new CardCollection();
		private Boolean _CanCleanUp = true;
		private Player.TurnStartedEventHandler _TurnStartedEventHandler = null;
		private Player _TurnStartedPlayer = null;

		public Haven()
			: base("Haven", Category.Action | Category.Duration, Source.Seaside, Location.Kingdom, Group.PlusCard | Group.PlusAction)
		{
			this.BaseCost = new Cost(2);
			this.Benefit.Actions = 1;
			this.Benefit.Cards = 1;
			this.Text = "Set aside a card from your hand face down.  At the start of your next turn, put it into your hand.";
		}

		internal override void TearDown()
		{
			base.TearDown();

			_HavenedCards.Clear();

			if (_TurnStartedEventHandler != null && _TurnStartedPlayer != null)
				_TurnStartedPlayer.TurnStarted -= _TurnStartedEventHandler;
			_TurnStartedPlayer = null;
			_TurnStartedEventHandler = null;
		}

		public override bool IsStackable { get { return _HavenedCards.Count == 0; } }
		public override CardCollection CardStack()
		{
			CardCollection cc = new CardCollection();
			_HavenedCards.ForEach(c => cc.Add(new Cards.Universal.Dummy()));
			cc.Add(this);
			return cc;
		}

		public override Boolean CanCleanUp { get { return this._CanCleanUp; } }

		public override void AddedTo(DeckLocation location, Player player)
		{
			base.AddedTo(location, player);
			switch (location)
			{
				case DeckLocation.InPlay:
					this._CanCleanUp = false;
					break;

				case DeckLocation.SetAside:
					if (_TurnStartedEventHandler != null)
						player.TurnStarted -= _TurnStartedEventHandler;
					_TurnStartedPlayer = player;
					_TurnStartedEventHandler = new Player.TurnStartedEventHandler(player_TurnStarted);
					_TurnStartedPlayer.TurnStarted += _TurnStartedEventHandler;
					this._CanCleanUp = true;
					break;

				default:
					this._CanCleanUp = true;
					break;
			}
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choice = new Choice("Which card would you like to set aside?", this, player.Hand, player);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Cards.Count > 0)
			{
				_HavenedCards.Add(player.RetrieveCardFrom(DeckLocation.Hand, result.Cards[0]));
				player._Game.SendMessage(player, this, result.Cards[0]);
			}
		}

		public override void PlayDuration(Player player)
		{
			base.PlayDuration(player);
			if (_HavenedCards.Count > 0)
			{
				player.AddCardsToHand(_HavenedCards);
				_HavenedCards.Clear();
			}
		}

		internal override void End(Player player, Deck deck)
		{
			// Add back any Haven'ed cards that are still on this
			deck.AddRange(player, _HavenedCards);
			_HavenedCards.Clear();
		}

		void player_TurnStarted(object sender, TurnStartedEventArgs e)
		{
			this.PlayDuration(e.Player);
			if (_TurnStartedEventHandler != null)
				e.Player.TurnStarted -= _TurnStartedEventHandler;
			_TurnStartedPlayer = null;
			_TurnStartedEventHandler = null;
		}
	}
	public class Island : Card
	{
		public Island()
			: base("Island", Category.Action | Category.Victory, Source.Seaside, Location.Kingdom, Group.Component | Group.DeckReduction | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.VictoryPoints = 2;
			this.Text = "Set aside this and another card from your hand.  Return them to your deck at the end of the game.";
		}

		internal override void ReceivedBy(Player player)
		{
			base.ReceivedBy(player);
			if (!player.PlayerMats.ContainsKey(TypeClass.IslandMat))
				player.PlayerMats[TypeClass.IslandMat] = new IslandMat();
		}

		public override void Play(Player player)
		{
			base.Play(player);

			if (player.InPlay.Contains(this.PhysicalCard))
				player.AddCardInto(TypeClass.IslandMat, player.RetrieveCardFrom(DeckLocation.InPlay, this.PhysicalCard));

			Choice choice = new Choice("Which card would you like to put on your island?", this, player.Hand, player);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Cards.Count > 0)
			{
				player.AddCardInto(TypeClass.IslandMat, player.RetrieveCardFrom(DeckLocation.Hand, result.Cards[0]));
			}
		}
	}
	public class IslandMat : CardMat
	{
		public IslandMat()
			: base(Visibility.All, VisibilityTo.All, new DominionBase.Cards.Sorting.ByTypeName(DominionBase.Cards.Sorting.SortDirection.Descending), true)
		{
			this.IsObtainable = false;
		}
	}
	public class Lighthouse : Card
	{
		private Player.AttackedEventHandler _AttackHandler = null;
		private Boolean _CanCleanUp = true;
		private Player.TurnStartedEventHandler _TurnStartedEventHandler = null;
		private Player _TurnStartedPlayer = null;

		public Lighthouse()
			: base("Lighthouse", Category.Action | Category.Duration, Source.Seaside, Location.Kingdom, Group.Defense | Group.PlusAction | Group.PlusCoin) 
		{
			this.BaseCost = new Cost(2);
			this.Benefit.Actions = 1;
			this.Benefit.Currency.Coin.Value = 1;
			this.DurationBenefit.Currency.Coin.Value = 1;
			this.Text = "<br/>While this is in play, when another player plays an Attack card, it does not affect you.";
		}

		internal override void TearDown()
		{
			base.TearDown();

			if (_TurnStartedEventHandler != null && _TurnStartedPlayer != null)
				_TurnStartedPlayer.TurnStarted -= _TurnStartedEventHandler;
			_TurnStartedPlayer = null;
			_TurnStartedEventHandler = null;
		}

		protected override Boolean AllowUndo { get { return true; } }

		public override Boolean CanCleanUp { get { return this._CanCleanUp; } }

		public override void AddedTo(DeckLocation location, Player player)
		{
			base.AddedTo(location, player);
			if (location == DeckLocation.InPlay || location == DeckLocation.SetAside)
			{
				if (_AttackHandler != null)
					player.Attacked -= _AttackHandler;

				_AttackHandler = new Player.AttackedEventHandler(player_Attacked_InPlay);
				player.Attacked += _AttackHandler;
			}

			switch (location)
			{
				case DeckLocation.InPlay:
					this._CanCleanUp = false;
					break;

				case DeckLocation.SetAside:
					if (_TurnStartedEventHandler != null)
						player.TurnStarted -= _TurnStartedEventHandler;
					_TurnStartedPlayer = player;
					_TurnStartedEventHandler = new Player.TurnStartedEventHandler(player_TurnStarted);
					_TurnStartedPlayer.TurnStarted += _TurnStartedEventHandler;
					this._CanCleanUp = true;
					break;

				default:
					this._CanCleanUp = true;
					break;
			}
		}

		private void player_Attacked_InPlay(object sender, AttackedEventArgs e)
		{
			// Already been cancelled -- don't need to process this one
			if (e.Cancelled)
				return;
			Player player = sender as Player;

			// Lighthouse only protects against other attackers
			if (player == e.Attacker)
				return;

			// Passively cancels any attack
			e.Cancelled = true;
			player._Game.SendMessage(player, this);
		}

		public override void RemovedFrom(DeckLocation location, Player player)
		{
			base.RemovedFrom(location, player);
			if (_AttackHandler != null)
				player.Attacked -= _AttackHandler;
			_AttackHandler = null;
		}

		void player_TurnStarted(object sender, TurnStartedEventArgs e)
		{
			String key = this.ToString();
			if (!e.Actions.ContainsKey(key))
				e.Actions[key] = new TurnStartedAction(e.Player, this, String.Format("Play {0}", this.PhysicalCard), player_Action, true);
		}

		internal void player_Action(Player player, ref TurnStartedEventArgs e)
		{
			this.PlayDuration(e.Player);
			if (_TurnStartedEventHandler != null)
				e.Player.TurnStarted -= _TurnStartedEventHandler;
			_TurnStartedPlayer = null;
			_TurnStartedEventHandler = null;
		}
	}
	public class Lookout : Card
	{
		public Lookout()
			: base("Lookout", Category.Action, Source.Seaside, Location.Kingdom, Group.DeckReduction | Group.PlusAction | Group.Trash | Group.RemoveCurses | Group.Discard)
		{
			this.BaseCost = new Cost(3);
			this.Benefit.Actions = 1;
			this.Text = "Look at the top 3 cards of your deck.  Trash one of them.  Discard one of them.  Put the other one on top of your deck.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			CardCollection newCards = player.Draw(3, DeckLocation.Private);

			Choice trashChoice = new Choice("Choose a card to trash", this, newCards, player);
			ChoiceResult trashResult = player.MakeChoice(trashChoice);
			if (trashResult.Cards.Count > 0)
			{
				newCards.Remove(trashResult.Cards[0]);
				player.Trash(player.RetrieveCardFrom(DeckLocation.Private, trashResult.Cards[0]));
			}

			Choice discardChoice = new Choice("Choose a card to discard", this, newCards, player);
			ChoiceResult discardResult = player.MakeChoice(discardChoice);
			if (discardResult.Cards.Count > 0)
			{
				newCards.Remove(discardResult.Cards[0]);
				player.Discard(DeckLocation.Private, discardResult.Cards[0]);
			}

			player.AddCardsToDeck(player.RetrieveCardsFrom(DeckLocation.Private), DeckPosition.Top);
		}
	}
	public class MerchantShip : Card
	{
		private Boolean _CanCleanUp = true;
		private Player.TurnStartedEventHandler _TurnStartedEventHandler = null;
		private Player _TurnStartedPlayer = null;

		public MerchantShip()
			: base("Merchant Ship", Category.Action | Category.Duration, Source.Seaside, Location.Kingdom, Group.PlusCoin | Group.Terminal)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Currency.Coin.Value = 2;
			this.DurationBenefit.Currency.Coin.Value = 2;
		}

		internal override void TearDown()
		{
			base.TearDown();

			if (_TurnStartedEventHandler != null && _TurnStartedPlayer != null)
				_TurnStartedPlayer.TurnStarted -= _TurnStartedEventHandler;
			_TurnStartedPlayer = null;
			_TurnStartedEventHandler = null;
		}

		protected override Boolean AllowUndo { get { return true; } }

		public override Boolean CanCleanUp { get { return this._CanCleanUp; } }

		public override void AddedTo(DeckLocation location, Player player)
		{
			base.AddedTo(location, player);
			switch (location)
			{
				case DeckLocation.InPlay:
					this._CanCleanUp = false;
					break;

				case DeckLocation.SetAside:
					if (_TurnStartedEventHandler != null)
						player.TurnStarted -= _TurnStartedEventHandler;
					_TurnStartedPlayer = player;
					_TurnStartedEventHandler = new Player.TurnStartedEventHandler(player_TurnStarted);
					_TurnStartedPlayer.TurnStarted += _TurnStartedEventHandler;
					this._CanCleanUp = true;
					break;

				default:
					this._CanCleanUp = true;
					break;
			}
		}

		void player_TurnStarted(object sender, TurnStartedEventArgs e)
		{
			String key = this.ToString();
			if (!e.Actions.ContainsKey(key))
				e.Actions[key] = new TurnStartedAction(e.Player, this, String.Format("Play {0}", this.PhysicalCard), player_Action, true);
		}

		internal void player_Action(Player player, ref TurnStartedEventArgs e)
		{
			this.PlayDuration(e.Player);
			if (_TurnStartedEventHandler != null)
				e.Player.TurnStarted -= _TurnStartedEventHandler;
			_TurnStartedPlayer = null;
			_TurnStartedEventHandler = null;
		}
	}
	public class NativeVillage : Card
	{
		public NativeVillage()
			: base("Native Village", Category.Action, Source.Seaside, Location.Kingdom, Group.Component | Group.PlusCard | Group.PlusAction | Group.PlusMultipleActions | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(2);
			this.Benefit.Actions = 2;
			this.Text = "Choose one: Set aside the top card of your deck face down on your Native Village mat; or put all the cards from your mat into your hand.<nl/><nl/>You may look at the cards on your mat at any time; return them to your deck at the end of the game.";
		}

		internal override void ReceivedBy(Player player)
		{
			base.ReceivedBy(player);
			if (!player.PlayerMats.ContainsKey(TypeClass.NativeVillageMat))
				player.PlayerMats[TypeClass.NativeVillageMat] = new NativeVillageMat();
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choice = new Choice("Choose one:", this, new CardCollection() { this }, new List<string>() { "Set aside the top card of your deck face down on your Native Village mat", "Put all the cards from your mat into your hand" }, player);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Options.Contains("Set aside the top card of your deck face down on your Native Village mat"))
			{
				if (player.CanDraw)
					player.Draw(TypeClass.NativeVillageMat);
			}
			else
			{
				player.AddCardsToHand(player.RetrieveCardsFrom(TypeClass.NativeVillageMat));
			}
		}
	}
	public class NativeVillageMat : CardMat
	{
		public NativeVillageMat()
			: base(Visibility.All, VisibilityTo.Owner, new DominionBase.Cards.Sorting.ByTypeName(DominionBase.Cards.Sorting.SortDirection.Descending), false)
		{
			this.IsObtainable = true;
		}
	}
	public class Navigator : Card
	{
		public Navigator()
			: base("Navigator", Category.Action, Source.Seaside, Location.Kingdom, Group.CardOrdering | Group.PlusCoin | Group.Discard | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Currency.Coin.Value = 2;
			this.Text = "Look at the top 5 cards of your deck.  Either discard all of them, or put them back on top of your deck in any order.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			player.Draw(5, DeckLocation.Private);

			Choice keepDiscard = new Choice(String.Format("You drew: {1}{0}Do you want to discard them all or put them back on your deck?", System.Environment.NewLine, player.Private), this, new CardCollection(player.Private), new List<String>() { "Discard them all", "Put them back" }, player);
			ChoiceResult keepDiscardResult = player.MakeChoice(keepDiscard);
			if (keepDiscardResult.Options[0] == "Discard them all")
			{
				player.Discard(DeckLocation.Private, player.Private);
			}
			else
			{
				Choice replaceChoice = new Choice("Choose order of cards to put back on your deck", this, player.Private, player, true, player.Private.Count, player.Private.Count);
				ChoiceResult replaceResult = player.MakeChoice(replaceChoice);
				player.AddCardsToDeck(player.RetrieveCardsFrom(DeckLocation.Private, replaceResult.Cards), DeckPosition.Top);
			}
		}
	}
	public class Outpost : Card
	{
		private Player _TurnEndedPlayer = null;
		private Player.TurnEndedEventHandler _TurnEndedEventHandler = null;
		private Player _TurnStartedPlayer = null;
		private Player.TurnStartedEventHandler _TurnStartedEventHandler = null;
		private Player.CleaningUpEventHandler _CleaningUpEventHandler = null;
		private Boolean _CanCleanUp = true;

		public Outpost()
			: base("Outpost", Category.Action | Category.Duration, Source.Seaside, Location.Kingdom, Group.Basic | Group.Terminal)
		{
			this.BaseCost = new Cost(5);
			this.Text = "You only draw 3 cards (instead of 5) in this turn's Clean-up phase.<nl/>Take an extra turn after this one.<nl/>This can't cause you to take more than two consecutive turns.";
		}

		protected override Boolean AllowUndo { get { return true; } }

		public override Boolean CanCleanUp { get { return this._CanCleanUp; } }

		internal override void TearDown()
		{
			base.TearDown();

			if (_TurnEndedEventHandler != null && _TurnEndedPlayer != null)
				player_TurnEnded(_TurnEndedPlayer, new TurnEndedEventArgs(_TurnEndedPlayer));
			_TurnEndedEventHandler = null;

			if (_TurnStartedEventHandler != null && _TurnStartedPlayer != null)
				_TurnStartedPlayer.TurnStarted -= _TurnStartedEventHandler;
			_TurnEndedEventHandler = null;
		}

		public override void AddedTo(DeckLocation location, Player player)
		{
			base.AddedTo(location, player);
			if (location == DeckLocation.InPlay)
				this._CanCleanUp = false;
			else
				this._CanCleanUp = true;
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

			if (_CleaningUpEventHandler == null)
			{
				_CleaningUpEventHandler = new Player.CleaningUpEventHandler(player_CleaningUp);
				player.CleaningUp += _CleaningUpEventHandler;
			}
		}

		void player_CleaningUp(object sender, CleaningUpEventArgs e)
		{
			if (_TurnStartedEventHandler != null)
				return;

			e.DrawSize = 3;
			if (e.CurrentPlayer._Game.TurnsTaken.Count > 1 && e.CurrentPlayer._Game.TurnsTaken[e.CurrentPlayer._Game.TurnsTaken.Count - 2].Player != e.CurrentPlayer)
			{
				e.NextPlayer = e.CurrentPlayer;
				e.NextGrantedBy = this;
			}
		}

		void player_TurnEnded(object sender, TurnEndedEventArgs e)
		{
			Player player = sender as Player;

			if (_TurnEndedEventHandler != null && _TurnEndedPlayer != null)
				_TurnEndedPlayer.TurnEnded -= _TurnEndedEventHandler;
			_TurnEndedPlayer = null;
			_TurnEndedEventHandler = null;

			if (_CleaningUpEventHandler != null)
				player.CleaningUp -= _CleaningUpEventHandler;
			_CleaningUpEventHandler = null;
		}

		void player_TurnStarted(object sender, TurnStartedEventArgs e)
		{
			Player player = sender as Player;

			if (_TurnStartedEventHandler != null && _TurnStartedPlayer != null)
				_TurnStartedPlayer.TurnStarted -= _TurnStartedEventHandler;
			_TurnStartedPlayer = null;
			_TurnStartedEventHandler = null;
		}
	}
	public class PearlDiver : Card
	{
		public PearlDiver()
			: base("Pearl Diver", Category.Action, Source.Seaside, Location.Kingdom, Group.CardOrdering | Group.PlusCard | Group.PlusAction)
		{
			this.BaseCost = new Cost(2);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 1;
			this.Text = "Look at the bottom card of your deck.  You may put it on top.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			if (player.CanDraw)
			{
				Card card = player.DrawFrom(DeckPosition.Bottom, 1, DeckLocation.Private)[0];

				Choice choice = Choice.CreateYesNoChoice(String.Format("Do you want to put {0} on top of your deck?", card.Name), this, card, player, null);
				ChoiceResult result = player.MakeChoice(choice);
				card = player.RetrieveCardFrom(DeckLocation.Private, card);
				if (result.Options[0] == "Yes")
					player.AddCardToDeck(card, DeckPosition.Top);
				else if (result.Options[0] == "No")
					player.AddCardToDeck(card, DeckPosition.Bottom);
			}
		}
	}
	public class PirateShip : Card
	{
		public PirateShip()
			: base("Pirate Ship", Category.Action | Category.Attack, Source.Seaside, Location.Kingdom, Group.Component | Group.PlusCoin | Group.Trash | Group.Discard | Group.Terminal | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(4);
			this.Text = "Choose one: Each other player reveals the top 2 cards of his deck, trashes a revealed Treasure card that you choose, discards the rest, and if anyone trashed a Treasure you take a Coin token; or, +<coin>1</coin> per Coin token you've taken with Pirate Ships this game.";
		}

		internal override void ReceivedBy(Player player)
		{
			base.ReceivedBy(player);
			if (!player.TokenPiles.ContainsKey(TypeClass.PirateShipToken))
				player.TokenPiles[TypeClass.PirateShipToken] = new TokenCollection();
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choice = new Choice("Choose 1:", this, new CardCollection() { this }, new List<string>() { "Perform attack", String.Format("+<coin>{0}</coin> (from Pirate Ship tokens)", player.TokenPiles[TypeClass.PirateShipToken].Count) }, player);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Options.Contains("Perform attack"))
			{
				// Perform attack on every player (including you)
				IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
				enumerator.MoveNext();
				Boolean anyTrashed = false;
				while (enumerator.MoveNext())
				{
					Player attackee = enumerator.Current;
					if (this.IsAttackBlocked[attackee])
						continue;

					attackee.Draw(2, DeckLocation.Revealed);

					CardCollection treasures = attackee.Revealed[Category.Treasure];

					Choice choiceTrash = new Choice(String.Format("Choose a Treasure card of {0} to trash", attackee), this, treasures, attackee);
					ChoiceResult resultTrash = player.MakeChoice(choiceTrash);
					if (resultTrash.Cards.Count > 0)
					{
						attackee.Trash(attackee.RetrieveCardFrom(DeckLocation.Revealed, resultTrash.Cards[0]));
						anyTrashed = true;
					}

					attackee.DiscardRevealed();
				}
				if (anyTrashed)
					player.AddToken(new PirateShipToken());
			}
			else
			{
				CardBenefit benefit = new CardBenefit();
				benefit.Currency += new Coin(player.TokenPiles[TypeClass.PirateShipToken].Count);
				player.ReceiveBenefit(this, benefit);
			}
		}
	}
	public class PirateShipToken : Token
	{
		public PirateShipToken()
			: base("P", "Pirate Ship coin")
		{
		}
		public override string Title { get { return "Worth <coin>1</coin> for each token when the 2nd option is chosen for the Pirate Ship"; } }
	}
	public class Salvager : Card
	{
		public Salvager()
			: base("Salvager", Category.Action, Source.Seaside, Location.Kingdom, Group.DeckReduction | Group.PlusCoin | Group.PlusBuy | Group.Trash | Group.RemoveCurses | Group.Terminal | Group.ConditionalBenefit)
		{
			this.BaseCost = new Cost(4);
			this.Benefit.Buys = 1;
			this.Text = "Trash a card from your hand.<nl/>+<coin/> equal to its cost.";
		}

		public override void Play(Player player)
		{
			base.Play(player);
			Choice choice = new Choice("Choose a card to trash", this, player.Hand, player);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Cards.Count > 0)
			{
				Cost trashedCardCost = player._Game.ComputeCost(result.Cards[0]);
				player.Trash(player.RetrieveCardFrom(DeckLocation.Hand, result.Cards[0]));
				CardBenefit benefit = new CardBenefit();
				benefit.Currency += trashedCardCost.Coin;
				player.ReceiveBenefit(this, benefit);
			}
		}
	}
	public class SeaHag : Card
	{
		public SeaHag()
			: base("Sea Hag", Category.Action | Category.Attack, Source.Seaside, Location.Kingdom, Group.PlusCurses | Group.Discard | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.Text = "Each other player discards the top card of his deck, then gains a Curse card, putting it on top of his deck.";
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
				{
					attackee.Draw(DeckLocation.Revealed);
					attackee.DiscardRevealed();
				}
				attackee.Gain(player._Game.Table.Curse, DeckLocation.Deck, DeckPosition.Top);
			}
		}
	}
	public class Smugglers : Card
	{
		public Smugglers()
			: base("Smugglers", Category.Action, Source.Seaside, Location.Kingdom, Group.Gain | Group.Terminal)
		{
			this.BaseCost = new Cost(3);
			this.Text = "Gain a copy of a card costing up to <coin>6</coin> that the player to your right gained on his last turn.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			// Get the player to my right
			Player playerToRight = player._Game.GetPlayerFromIndex(player, -1);
			Turn mostRecentTurn = player._Game.TurnsTaken.Last(turn => turn.Player == playerToRight);

			Cost cost6 = new Cards.Cost(6);
			IEnumerable<Card> cardsAvailableToGain = mostRecentTurn.CardsGained.Where(card => player._Game.ComputeCost(card) <= cost6).GroupBy(card => card.Name).Select(igCard => igCard.ElementAt(0));
			Choice choice = new Choice("Choose a card to gain", this, cardsAvailableToGain, player);
			ChoiceResult result = player.MakeChoice(choice);
			if (result.Cards.Count > 0)
			{
				Supply supply = player._Game.Table.FindSupplyPileByCard(result.Cards[0]);
				if (supply != null && supply.TopCard != null && supply.TopCard.Name == result.Cards[0].Name)
					player.Gain(supply);
			}
		}
	}
	public class Tactician : Card
	{
		private Boolean _CanCleanUp = true;
		private int _DurationBenefits = 0;
		private Player.TurnStartedEventHandler _TurnStartedEventHandler = null;
		private Player _TurnStartedPlayer = null;

		public Tactician()
			: base("Tactician", Category.Action | Category.Duration, Source.Seaside, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.PlusBuy)
		{
			this.BaseCost = new Cost(5);
			this.Text = "Discard your hand.<nl/>If you discarded any cards this way, then at the start of your next turn, +5<nbsp/>Cards, +1<nbsp/>Buy, and +1<nbsp/>Action.";
		}

		internal override void TearDown()
		{
			base.TearDown();

			if (_TurnStartedEventHandler != null && _TurnStartedPlayer != null)
				_TurnStartedPlayer.TurnStarted -= _TurnStartedEventHandler;
			_TurnStartedPlayer = null;
			_TurnStartedEventHandler = null;
		}

		public override Boolean CanCleanUp { get { return this._CanCleanUp; } }

		public override void AddedTo(DeckLocation location, Player player)
		{
			base.AddedTo(location, player);

			switch (location)
			{
				case DeckLocation.InPlay:
					if (player.Hand.Count > 0)
						this._CanCleanUp = false;
					_DurationBenefits = 1;
					break;

				case DeckLocation.SetAside:
					if (_TurnStartedEventHandler != null)
						player.TurnStarted -= _TurnStartedEventHandler;
					_TurnStartedPlayer = player;
					_TurnStartedEventHandler = new Player.TurnStartedEventHandler(player_TurnStarted);
					_TurnStartedPlayer.TurnStarted += _TurnStartedEventHandler;
					this._CanCleanUp = true;
					break;

				default:
					this._CanCleanUp = true;
					break;
			}
		}

		public override void Play(Player player)
		{
			base.Play(player);
			player.Discard(DeckLocation.Hand);
		}

		protected override void GainDurationBenefits(Player player)
		{
			if (_DurationBenefits > 0)
			{
				player.ReceiveBenefit(this, new CardBenefit() { Cards = 5, Actions = 1, Buys = 1 });
				_DurationBenefits = 0;
			}
		}

		void player_TurnStarted(object sender, TurnStartedEventArgs e)
		{
			String key = this.ToString();
			if (!e.Actions.ContainsKey(key))
				e.Actions[key] = new TurnStartedAction(e.Player, this, String.Format("Play {0}", this.PhysicalCard), player_Action, true);
		}

		internal void player_Action(Player player, ref TurnStartedEventArgs e)
		{
			this.PlayDuration(e.Player);
			if (_TurnStartedEventHandler != null)
				e.Player.TurnStarted -= _TurnStartedEventHandler;
			_TurnStartedPlayer = null;
			_TurnStartedEventHandler = null;
			_DurationBenefits = 0;
		}

		internal override XmlNode GenerateXml(XmlDocument doc, string nodeName)
		{
			XmlNode xn = base.GenerateXml(doc, nodeName);
			XmlNode xnDurationBenefits = doc.CreateElement("duration_benefits");
			xnDurationBenefits.InnerText = this._DurationBenefits.ToString();
			xn.AppendChild(xnDurationBenefits);
			return xn;
		}

		internal override void LoadInstance(XmlNode xnCard)
		{
			base.LoadInstance(xnCard);
			XmlNode xn = xnCard.SelectSingleNode("duration_benefits");
			if (xn != null)
			{
				this._DurationBenefits = int.Parse(xn.InnerText);
			}
		}
	}
	public class TreasureMap : Card
	{
		public TreasureMap()
			: base("Treasure Map", Category.Action, Source.Seaside, Location.Kingdom, Group.Gain | Group.Trash | Group.Terminal)
		{
			this.BaseCost = new Cost(4);
			this.Text = "Trash this and another copy of Treasure Map from your hand.<nl/>If you do trash two Treasure Maps, gain 4 Gold cards, putting them on top of your deck.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			int trashed = 0;
			if (player.InPlay.Contains(this.PhysicalCard))
			{
				player.Trash(player.RetrieveCardFrom(DeckLocation.InPlay, this.PhysicalCard));
				trashed++;
			}

			CardCollection anotherTreasure = player.RetrieveCardsFrom(DeckLocation.Hand, Cards.Seaside.TypeClass.TreasureMap, 1);
			if (anotherTreasure.Count == 1)
			{
				player.Trash(anotherTreasure);
				trashed++;
				if (trashed == 2)
				{
					for (int i = 0; i < 4; i++)
						player.Gain(player._Game.Table.Gold, DeckLocation.Deck, DeckPosition.Top);
				}
			}
		}
	}
	public class Treasury : Card
	{
		private Player.CardsDiscardingEventHandler _CardsDiscardingEventHandler = null;

		public Treasury()
			: base("Treasury", Category.Action, Source.Seaside, Location.Kingdom, Group.CardOrdering | Group.PlusCard | Group.PlusAction | Group.PlusCoin)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Cards = 1;
			this.Benefit.Actions = 1;
			this.Benefit.Currency.Coin.Value = 1;
			this.Text = "<br/>When you discard this from play, if you didn't buy a Victory card this turn, you may put it on top of your deck.";

			this.OwnerChanged += new OwnerChangedEventHandler(Treasury_OwnerChanged);
		}

		internal override void TearDown()
		{
			Treasury_OwnerChanged(this, new OwnerChangedEventArgs(this.Owner, null));

			base.TearDown();

			this.OwnerChanged -= new OwnerChangedEventHandler(Treasury_OwnerChanged);
		}

		void Treasury_OwnerChanged(object sender, OwnerChangedEventArgs e)
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
			if (!e.Cards.Contains(this.PhysicalCard) || e.GetAction(TypeClass.Treasury) != null || e.HandledBy.Contains(this) ||
				(e.FromLocation != DeckLocation.InPlay && e.FromLocation != DeckLocation.SetAside && e.FromLocation != DeckLocation.InPlayAndSetAside))
				return;

			// Only allow this if no Victory cards were bought this turn
			if (!((sender as Player).CurrentTurn.CardsBought.Any(c => (c.Category & Cards.Category.Victory) == Cards.Category.Victory)))
				e.AddAction(TypeClass.Treasury, new CardsDiscardAction(sender as Player, this, String.Format("Put {0} on your deck", this.PhysicalCard), player_Action, false));
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

			e.HandledBy.Add(this);
		}
	}
	public class Warehouse : Card
	{
		public Warehouse()
			: base("Warehouse", Category.Action, Source.Seaside, Location.Kingdom, Group.PlusCard | Group.PlusAction | Group.Discard)
		{
			this.BaseCost = new Cost(3);
			this.Benefit.Cards = 3;
			this.Benefit.Actions = 1;
			this.Text = "Discard 3 cards.";
		}

		public override void Play(Player player)
		{
			base.Play(player);

			Choice choice = new Choice("Discard 3 cards.", this, player.Hand, player, false, 3, 3);
			ChoiceResult result = player.MakeChoice(choice);
			player.Discard(DeckLocation.Hand, result.Cards);
		}
	}
	public class Wharf : Card
	{
		private Boolean _CanCleanUp = true;
		private Player.TurnStartedEventHandler _TurnStartedEventHandler = null;
		private Player _TurnStartedPlayer = null;

		public Wharf()
			: base("Wharf", Category.Action | Category.Duration, Source.Seaside, Location.Kingdom, Group.PlusCard | Group.PlusBuy | Group.Terminal)
		{
			this.BaseCost = new Cost(5);
			this.Benefit.Cards = 2;
			this.Benefit.Buys = 1;
			this.DurationBenefit.Cards = 2;
			this.DurationBenefit.Buys = 1;
		}

		internal override void TearDown()
		{
			base.TearDown();

			if (_TurnStartedEventHandler != null && _TurnStartedPlayer != null)
				_TurnStartedPlayer.TurnStarted -= _TurnStartedEventHandler;
			_TurnStartedPlayer = null;
			_TurnStartedEventHandler = null;
		}

		public override Boolean CanCleanUp { get { return this._CanCleanUp; } }

		public override void AddedTo(DeckLocation location, Player player)
		{
			base.AddedTo(location, player);
			switch (location)
			{
				case DeckLocation.InPlay:
					this._CanCleanUp = false;
					break;

				case DeckLocation.SetAside:
					if (_TurnStartedEventHandler != null)
						player.TurnStarted -= _TurnStartedEventHandler;
					_TurnStartedPlayer = player;
					_TurnStartedEventHandler = new Player.TurnStartedEventHandler(player_TurnStarted);
					_TurnStartedPlayer.TurnStarted += _TurnStartedEventHandler;
					this._CanCleanUp = true;
					break;

				default:
					this._CanCleanUp = true;
					break;
			}
		}

		void player_TurnStarted(object sender, TurnStartedEventArgs e)
		{
			String key = this.ToString();
			if (!e.Actions.ContainsKey(key))
				e.Actions[key] = new TurnStartedAction(e.Player, this, String.Format("Play {0}", this.PhysicalCard), player_Action, true);
		}

		internal void player_Action(Player player, ref TurnStartedEventArgs e)
		{
			this.PlayDuration(e.Player);
			if (_TurnStartedEventHandler != null)
				e.Player.TurnStarted -= _TurnStartedEventHandler;
			_TurnStartedPlayer = null;
			_TurnStartedEventHandler = null;
		}
	}
}
