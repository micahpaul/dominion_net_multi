using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DominionBase.Cards;

namespace DominionBase.Piles
{
	public interface IPile
	{
		CardCollection this[Category type] { get; }

		CardCollection this[Type type]  { get; }

		CardCollection this[String name]  { get; }

		CardCollection this[Predicate<Card> predicate] { get; }

		int Count { get; }
	}
}
