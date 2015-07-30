using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using DominionBase.Piles;
using DominionBase.Players;

namespace DominionBase.Cards
{
	public class OwnerChangedEventArgs : EventArgs
	{
		public Player OldOwner;
		public Player NewOwner;
		public OwnerChangedEventArgs(Player oldOwner, Player newOwner)
		{
			this.OldOwner = oldOwner;
			this.NewOwner = newOwner;
		}
	}

	public enum CardBack
	{ 
		Standard,
		Red
	}

	/// <summary>
	/// This is the standard abstract class for defining a card and declaring what it is, what happens when it is played, and what benefits it may provide.
	/// See the standard constructor for more detailed information about basic setup.
	/// </summary>
	public abstract class Card : IComparable<Card>, ICard, IDisposable
	{
		public delegate void OwnerChangedEventHandler(object sender, OwnerChangedEventArgs e);
		public event OwnerChangedEventHandler OwnerChanged = null;

		private String _Name = String.Empty;
		private String _ActionText = "<benefit/>";
		private String _ExtraText = String.Empty;
		private Category _Category = Category.Unknown;
		private Source _Source = Source.All;
		private Location _Location = Location.General;
		private Group _GroupMembership = Group.Basic;
		private Cost _BaseCost = new Cost(0);
		private CardBack _CardBack = CardBack.Standard;

		private CardBenefit _Benefit;
		private CardBenefit _DurationBenefit;
		private int _VictoryPoints = 0;
		private Card _ModifiedBy = null;
		private Dictionary<Player, Boolean> _IsAttackBlocked = new Dictionary<Player, Boolean>();

		private Guid _UniqueId = Guid.NewGuid();

		private Player _Owner = null;
		private Token.TokenActionEventHandler _TokenEventHandler = null;

		private Card _PhysicalCard = null;

		internal Card(String name, Category category, Source source, Location location)
		{
			_Name = name;
			_Category = category;
			_Source = source;
			_Location = location;
			Boolean isTreasure = (_Category & Category.Treasure) == Category.Treasure;
			_Benefit = new CardBenefit(isTreasure);
			_DurationBenefit = new CardBenefit(isTreasure);

			if ((category & Cards.Category.Attack) == Cards.Category.Attack)
				_GroupMembership |= Group.AffectOthers;

			Boolean isAction = (category & Cards.Category.Action) == Cards.Category.Action;
			Boolean isVictory = (category & Cards.Category.Victory) == Cards.Category.Victory;
			Boolean isReaction = (category & Cards.Category.Reaction) == Cards.Category.Reaction;
			Boolean isShelter = (category & Cards.Category.Shelter) == Cards.Category.Shelter;
			if (((isAction && isTreasure) || (isAction && isVictory) || (isAction && isShelter)) ||
				((isTreasure && isVictory) || (isTreasure && isReaction) || (isTreasure && isShelter)) ||
				((isVictory && isReaction) || (isVictory && isShelter)) ||
				(isReaction && isShelter))
				_GroupMembership |= Group.MultiType;
		}

		internal Card(String name, Category category, Source source, Location location, Group group)
			: this(name, category, source, location)
		{
			if (group == Group.None)
				_GroupMembership = group;
			else
				_GroupMembership |= group;
		}

		internal Card(String name, Category category, Source source, Location location, CardBack cardBack)
			: this(name, category, source, location)
		{
			_CardBack = cardBack;
		}

		internal Card(String name, Category category, Source source, Location location, Group group, CardBack cardBack)
			: this(name, category, source, location, group)
		{
			_CardBack = cardBack;
		}

		public static Card CreateInstance(Type type)
		{
			return (Card)type.GetConstructor(Type.EmptyTypes).Invoke(null);
		}

		public virtual List<Type> GetSerializingTypes()
		{
			return new List<Type>();
		}

		public virtual void CheckSetup(Preset preset, Table table)
		{
			return;
		}

		public virtual void CheckSetup(Preset preset, Card card)
		{
			return;
		}

		public virtual CardSettingCollection GenerateSettings()
		{
			return new CardSettingCollection();
		}

		public virtual void FinalizeSettings(CardSettingCollection settings)
		{
			return;
		}

		public void TestFireAllEvents()
		{
			if (OwnerChanged != null)
				OwnerChanged(this, new OwnerChangedEventArgs(null, null));
		}

		internal virtual void TearDown()
		{
			this.Owner = null;
		}

		#region IDisposable variables, properties, & methods
		// Track whether Dispose has been called.
		private bool disposed = false;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			// Check to see if Dispose has already been called.
			if (!this.disposed)
			{
				// If disposing equals true, dispose all managed
				// and unmanaged resources.
				if (disposing)
				{
					// Dispose managed resources.
					_BaseCost = null;
					_Benefit = null;
					_DurationBenefit = null;
					_ModifiedBy = null;
					_Owner = null;
				}

				// Call the appropriate methods to clean up
				// unmanaged resources here.
				// If disposing is false,
				// only the following code is executed.

				// Note disposing has been done.
				disposed = true;
			}
		}

		~Card()
		{
			Dispose(false);
		}
		#endregion

		public Guid UniqueId { get { return _UniqueId; } }
		public Card PhysicalCard 
		{ 
			get 
			{
				if (_PhysicalCard != null)
					return _PhysicalCard;
				return this;
			}
			set
			{
				_PhysicalCard = value;
			}
		}
		public Category PhysicalCategory { get { return this._Category; } }
		public virtual Card LogicalCard { get { return this; } }
		public Type CardType { get { return this.GetType(); } }
		public virtual Type BaseType { get { return this.CardType; } }
		public virtual Boolean IsStackable { get { return true; } }
		public virtual String SpecialPresetKey { get { return null; } }
		protected virtual Boolean AllowUndo { get { return false; } }
		public Boolean CanUndo
		{
			get
			{
				return this.AllowUndo && this.ModifiedBy == null;
			}
		}

		public String Name
		{
			get { return _Name; }
			protected set { _Name = value; }
		}

		public String Text
		{
			get 
			{
				StringBuilder sb = new StringBuilder();
				StringBuilder sbBenefitText = new StringBuilder();
				if (_Benefit.Any && _Benefit.Equals(_DurationBenefit))
				{
					sbBenefitText.AppendLine("Now and at the start of your next turn:");
					sbBenefitText.Append(_Benefit.Text);
				}
				else
				{
					if (_Benefit.Any || (_Category & Category.Treasure) == Category.Treasure)
					{
						sbBenefitText.Append(_Benefit.Text);
					}
					if (_DurationBenefit.Any)
					{
						if (sbBenefitText.Length > 0)
						{
							sbBenefitText.AppendLine();
							sbBenefitText.AppendLine();
						}
						sbBenefitText.AppendLine("At the start of your next turn:");
						sbBenefitText.Append(_DurationBenefit.Text);
					}
				}
				if (!String.IsNullOrEmpty(_ActionText))
				{
					if (sb.Length > 0 && !_ActionText.StartsWith("<br/>"))
					{
						sb.AppendLine();
						sb.AppendLine();
					}
					sb.Append(_ActionText.Replace("<benefit/>", sbBenefitText.ToString()));
				}
				if (_VictoryPoints != 0 || ((this.Category & Cards.Category.Victory) == Cards.Category.Victory && (this.GroupMembership & Group.VariableVPs) != Group.VariableVPs))
				{
					if (sb.Length > 0)
					{
						if ((this.Category & Cards.Category.Treasure) != Cards.Category.Treasure)
							sb.Append("<br/>");
						else
						{
							sb.AppendLine();
							sb.AppendLine();
						}
					}

					sb.Append(String.Format("<vplg>{0}</vplg>", _VictoryPoints));
				}
				if (!String.IsNullOrEmpty(_ExtraText))
				{
					sb.Append("<br/>");
					sb.AppendLine();
					sb.Append(_ExtraText);
				}
				return sb.ToString();
			}
			protected set 
			{
				string[] strings = value.Split(new string[] { "<br/>" }, StringSplitOptions.None);
				if (strings.Length > 0)
				{
					_ActionText = strings[0].Replace("<nl/>", System.Environment.NewLine);
					if (strings.Length > 1)
						_ExtraText = strings[1].Replace("<nl/>", System.Environment.NewLine);
				}
				if (!_ActionText.Contains("<benefit/>"))
					_ActionText = String.Format("<benefit/>{1}{0}", _ActionText, (String.IsNullOrEmpty(_ActionText) ? "" : System.Environment.NewLine));
			}
		}

		public virtual Category Category
		{
			get { return _Category; }
		}

		public virtual Source Source
		{
			get { return _Source; }
		}

		public virtual Location Location
		{
			get { return _Location; }
		}

		public virtual Group GroupMembership
		{
			get { return _GroupMembership; }
		}

		public virtual Cost BaseCost
		{
			get { return _BaseCost; }
			protected set {
				if (value.CanOverpay)
					_GroupMembership |= Group.VariableCost;
				_BaseCost = value; 
			}
		}

		public CardBack CardBack 
		{
			get { return _CardBack; }
		}

		public virtual CardBenefit Benefit
		{
			get { return _Benefit; }
		}
		public virtual CardBenefit DurationBenefit
		{
			get { return _DurationBenefit; }
		}

		public virtual int VictoryPoints
		{
			get { return _VictoryPoints; }
			protected set { _VictoryPoints = value; }
		}

		public virtual Card ModifiedBy
		{
			get { return _ModifiedBy; }
			internal set { _ModifiedBy = value; }
		}

		protected Dictionary<Player, Boolean> IsAttackBlocked
		{
			get { return _IsAttackBlocked; }
		}

		internal Player Owner
		{
			get { return _Owner; }
			set 
			{
				Player oldOwner = _Owner;
				_Owner = value;
				if (oldOwner != _Owner && OwnerChanged != null)
				{
					OwnerChangedEventArgs ocea = new OwnerChangedEventArgs(oldOwner, _Owner);
					OwnerChanged(this, ocea);
				}
			}
		}

		public virtual Boolean IsEndgameTriggered(Supply supply)
		{
			return false;
		}

		public virtual int GetVictoryPoints(IEnumerable<Card> cards)
		{
			return _VictoryPoints;
		}

		public virtual void ObtainedBy(Player player)
		{
			this.Owner = player;
		}

		public virtual void LostBy(Player player)
		{
			this.Owner = null;
		}

		public virtual void AddedTo(DeckLocation location, Player player)
		{
			if (location == DeckLocation.Hand)
			{
				if (player._Game.Table.Supplies.ContainsKey(this))
				{
					if (player._Game.Table.Supplies[this].Tokens.Any(token => token.ActDefined))
					{
						_TokenEventHandler = new Token.TokenActionEventHandler(token_TokenAction);
						player.TokenActedOn += _TokenEventHandler;
					}
				}
			}
		}

		private void token_TokenAction(object sender, TokenActionEventArgs e)
		{
			e.Actor._Game.Table.Supplies[this].Tokens.ForEach(delegate(Token t) { if (t.ActDefined) t.Act(this, e); });
		}

		public virtual void AddedTo(Type deckType, Player player)
		{
		}

		public virtual void RemovedFrom(DeckLocation location, Player player)
		{
			if (_TokenEventHandler != null)
				player.TokenActedOn -= _TokenEventHandler;
			_TokenEventHandler = null;
		}

		public virtual void RemovedFrom(Type deckType, Player player)
		{
		}

		// Stub for Attacks, so they can get called during an existing attack
		internal virtual void player_Attacked(object sender, AttackedEventArgs e)
		{
		}

		public virtual void Setup(Game game, Supply supply)
		{
		}

		public virtual void Finalize(Game game, Supply supply)
		{
		}

		public virtual void AddedToSupply(Game game, Supply supply)
		{
		}

		/// <summary>
		/// Very basic card playing -- anything special needs to happen in the override for the specific card class 
		/// </summary>
		/// <param name="player">Game this card is associated with</param>
		public virtual void Play(Player player)
		{
			PlaySetup(player);
			PlayRest(player);
		}

		protected virtual void PlaySetup(Player player)
		{
			if (player.Phase == PhaseEnum.Action &&
				(this.Category & Category.Action) != Category.Action)
				throw new Exception("Cannot play this card right now!");

			if ((player.Phase == PhaseEnum.ActionTreasure || player.Phase == PhaseEnum.BuyTreasure) &&
				(this.Category & Category.Treasure) != Category.Treasure)
				throw new Exception("Cannot play this card right now!");

			if (player.Phase == PhaseEnum.Buy ||
				player.Phase == PhaseEnum.Cleanup ||
				player.Phase == PhaseEnum.Endgame ||
				//player.Phase == PhaseEnum.Starting ||
				player.PlayerMode == PlayerMode.Waiting ||
				player.PlayerMode == PlayerMode.Choosing)
				throw new Exception("Cannot play cards right now!");

			// This is an Attack card, so React-to-Attack cards must trigger before anything else
			if ((this.Category & Cards.Category.Attack) == Cards.Category.Attack)
			{
				// Check Attack play reactions
				IEnumerator<Player> enumerator = player._Game.GetPlayersStartingWithEnumerator(player);
				enumerator.MoveNext();
				this.IsAttackBlocked[enumerator.Current] = false;
				while (enumerator.MoveNext())
					this.IsAttackBlocked[enumerator.Current] = !enumerator.Current.AttackedBy(player, this);
			}
		}

		protected virtual void PlayRest(Player player)
		{
			player.ReceiveBenefit(this, this.Benefit, true);
			if ((this.Category & Category.Action) == Category.Action)
				player.ActionPlayed();
		}

		public virtual void PlayFinished(Player player)
		{
			this.IsAttackBlocked.Clear();
		}

		public virtual void UndoPlay(Player player)
		{
			if (!this.CanUndo)
				throw new Exception("Cannot undo this card!");

			player.RemoveBenefit(this, this.Benefit, true);
			if ((this.Category & Category.Action) == Category.Action)
				player.UndoActionPlayed();
		}

		public virtual Boolean CanCleanUp { get { return true; } }

		public virtual void PlayDuration(Player player)
		{
			if (this.ModifiedBy != null)
				this.ModifiedBy.ModifyDuration(player, this);
			else
				GainDurationBenefits(player);
		}

		protected virtual void GainDurationBenefits(Player player)
		{
			player.ReceiveBenefit(this, this.DurationBenefit, true);
		}

		protected virtual void ModifyDuration(Player player, Card card)
		{
			card.GainDurationBenefits(player);
		}

		public override string ToString()
		{
			return Name;
		}

		/// <summary>
		/// Returns true if the card can be bought
		/// </summary>
		/// <param name="game"></param>
		/// <returns></returns>
		internal virtual Boolean CanBuy(Player player)
		{
			return CanGain();
		}

		internal virtual Boolean CanGain()
		{
			return true;
		}

		internal virtual CardCollection Gaining(Game game, Supply supplyPile)
		{
			return new CardCollection();
		}

		internal virtual CardCollection Buying(Game game, Supply supplyPile)
		{
			return new CardCollection();
		}

		internal virtual void PhaseChanged(object sender, PhaseChangedEventArgs e)
		{
		}

		internal virtual void PlayerModeChanged(object sender, PlayerModeChangedEventArgs e)
		{
		}

		public int CompareTo(Card card)
		{
			if (ReferenceEquals(this, card))
				return 0;
			else if (card.CardType == Cards.Prosperity.TypeClass.Colony)
				return 1;
			else if (this.CardType == Cards.Prosperity.TypeClass.Colony)
				return -1;
			else if (card.CardType == Cards.Universal.TypeClass.Province)
				return 1;
			else if (this.CardType == Cards.Universal.TypeClass.Province)
				return -1;
			else if (card.CardType == Cards.Universal.TypeClass.Duchy)
				return 1;
			else if (this.CardType == Cards.Universal.TypeClass.Duchy)
				return -1;
			else if (card.CardType == Cards.Universal.TypeClass.Estate)
				return 1;
			else if (this.CardType == Cards.Universal.TypeClass.Estate)
				return -1;
			else if (card.CardType == Cards.Universal.TypeClass.Curse)
				return 1;
			else if (this.CardType == Cards.Universal.TypeClass.Curse)
				return -1;
			else if (card.CardType == Cards.Prosperity.TypeClass.Platinum)
				return 1;
			else if (this.CardType == Cards.Prosperity.TypeClass.Platinum)
				return -1;
			else if (card.CardType == Cards.Universal.TypeClass.Gold)
				return 1;
			else if (this.CardType == Cards.Universal.TypeClass.Gold)
				return -1;
			else if (card.CardType == Cards.Alchemy.TypeClass.Potion)
				return 1;
			else if (this.CardType == Cards.Alchemy.TypeClass.Potion)
				return -1;
			else if (card.CardType == Cards.Universal.TypeClass.Silver)
				return 1;
			else if (this.CardType == Cards.Universal.TypeClass.Silver)
				return -1;
			else if (card.CardType == Cards.Universal.TypeClass.Copper)
				return 1;
			else if (this.CardType == Cards.Universal.TypeClass.Copper)
				return -1;
			else
			{
				int nc = - this.BaseCost.CompareTo(card.BaseCost);
				if (nc == 0)
					nc = this.Name.CompareTo(card.Name);
				if (nc == 0)
					nc = this.UniqueId.CompareTo(card.UniqueId);
				return nc;
			}
		}

		internal virtual void End(Player player, Deck deck)
		{
		}

		internal virtual void Bought(Player player)
		{
		}

		internal virtual void Gaining(Player player, ref DeckLocation location, ref DeckPosition position)
		{
			Receiving(player, ref location, ref position);
		}

		internal virtual void Gained(Player player)
		{
			ReceivedBy(player);
		}

		internal virtual void Receiving(Player player, ref DeckLocation location, ref DeckPosition position)
		{
		}

		internal virtual void ReceivedBy(Player player)
		{
			this.ObtainedBy(player);
		}

		internal virtual void TrashedBy(Player player)
		{
			this.LostBy(player);
		}

		public virtual CardCollection CardStack()
		{
			return new CardCollection() { this };
		}

		internal virtual XmlNode GenerateXml(XmlDocument doc, String nodeName)
		{
			XmlElement xe = doc.CreateElement(nodeName);
			xe.SetAttribute("type", this.CardType.ToString());
			if (this.ModifiedBy != null)
				xe.AppendChild(this.ModifiedBy.GenerateXml(doc, "modified_by"));
			return xe;
		}

		internal static Card Load(XmlNode xnCard)
		{
			if (xnCard == null)
				return null;
			Type cardType = Type.GetType(xnCard.Attributes["type"].Value);
			Card card = Card.CreateInstance(cardType);
			card.LoadInstance(xnCard);
			return card;
		}

		internal virtual void LoadInstance(XmlNode xnCard)
		{
			this.ModifiedBy = Card.Load(xnCard.SelectSingleNode("modified_by"));
		}
	}
}
