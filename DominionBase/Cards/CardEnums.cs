using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace DominionBase.Cards
{
	[Flags]
	public enum Category
	{
		Unknown = 0,
		Action = 1 << 0,
		Treasure = 1 << 1,
		Victory = 1 << 2,
		Attack = 1 << 3,
		Curse = 1 << 4,
		Reaction = 1 << 5,
		Duration = 1 << 6,
		Prize = 1 << 7,
		Shelter = 1 << 8,
		Ruins = 1 << 9,
		Looter = 1 << 10,
		Knight = 1 << 11
	}

	public enum Source
	{
		All,
		Base,
		Intrigue,
		Seaside,
		Alchemy,
		Prosperity,
		Cornucopia,
		Hinterlands,
		DarkAges,
		Guilds,
		Promotional,
		Custom
	}

	public enum Location
	{
		General,
		Kingdom,
		Special,
		Invisible
	}

	[Flags]
	public enum Group
	{
		None = 0,
		Basic = 1 << 0,
		[DescriptionAttribute("Cards that react to Attacks")]
		ReactToAttack = 1 << 1,
		[DescriptionAttribute("Cards that react to Gaining")]
		ReactToGain = 1 << 2,
		[DescriptionAttribute("Cards that react to Discarding")]
		ReactToDiscard = 1 << 17,
		[DescriptionAttribute("Cards that react to being trashed")]
		ReactToTrashing = 1 << 23,
		[DescriptionAttribute("Cards that provide defense")]
		Defense = 1 << 3,
		[DescriptionAttribute("Cards that require components")]
		Component = 1 << 4,
		[DescriptionAttribute("Cards with multiple Types")]
		MultiType = 1 << 5,
		[DescriptionAttribute("Cards that reduce overall deck size")]
		DeckReduction = 1 << 6,
		[DescriptionAttribute("Cards that change deck ordering")]
		CardOrdering = 1 << 7,
		[DescriptionAttribute("Cards that give Curses")]
		PlusCurses = 1 << 8,
		[DescriptionAttribute("Cards that let you draw")]
		PlusCard = 1 << 9,
		[DescriptionAttribute("Cards that add Actions")]
		PlusAction = 1 << 10,
		[DescriptionAttribute("Cards that add 2+ Actions")]
		PlusMultipleActions = 1 << 11,
		[DescriptionAttribute("Cards that add Currency")]
		PlusCoin = 1 << 12,
		[DescriptionAttribute("Cards that add Buys")]
		PlusBuy = 1 << 13,
		[DescriptionAttribute("Cards that provide conditional benefits")]
		ConditionalBenefit = 1 << 24,
		[DescriptionAttribute("Cards that modify Costs")]
		ModifyCost = 1 << 14,
		[DescriptionAttribute("Cards that discard")]
		Discard = 1 << 18,
		[DescriptionAttribute("Cards that gain")]
		Gain = 1 << 15,
		[DescriptionAttribute("Cards that trash")]
		Trash = 1 << 16,
		[DescriptionAttribute("Victory cards with variable worth")]
		VariableVPs = 1 << 19,
		[DescriptionAttribute("Cards that can get rid of Curses")]
		RemoveCurses = 1 << 20,
		[DescriptionAttribute("Cards that affect other players")]
		AffectOthers = 1 << 21,
		[DescriptionAttribute("Action cards that cannot provide the ability to play more Action cards")]
		Terminal = 1 << 22,
		[DescriptionAttribute("Cards that give you Coin tokens")]
		PlusCoinToken = 1 << 25,
		[DescriptionAttribute("Cards whose cost is variable")]
		VariableCost = 1 << 26,
		[DescriptionAttribute("Cards you can overpay for a benefit")]
		Overpay = 1 << 27,
	}
}
