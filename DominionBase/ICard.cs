using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DominionBase
{
	public interface ICard
	{
		String Name { get; }
		String Text { get; }
		Cards.Category Category { get; }
		Cards.Source Source { get; }
		Cards.Location Location { get; }
		Cards.Cost BaseCost { get; }
		Cards.CardBack CardBack { get; }
		Cards.CardBenefit Benefit { get; }
		Type CardType { get; }
	}
}
