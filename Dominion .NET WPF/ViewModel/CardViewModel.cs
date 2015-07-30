using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dominion.NET_WPF.ViewModel
{
	class CardViewModel
	{
		public DominionBase.ICard ICard { get; set; }
		public String CardName { get; set; }
		public DominionBase.Piles.Visibility Visibility { get; set; }
		public int OriginalIndex { get; set; }
	}
}
