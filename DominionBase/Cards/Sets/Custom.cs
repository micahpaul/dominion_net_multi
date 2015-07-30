using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DominionBase.Currencies;
using DominionBase.Piles;
using DominionBase.Players;

/// Use this class file to define any custom cards you want to create
/// Cards are fairly straight-forward to create; you can look to any of the existing cards as reference material
/// 
/// Each card MUST have the following defined:
/// 
///    1. A static Type object in the TypeClass class that is defined as:
///			public static Type CardName = typeof(CardName);
///			
///	   2. A non-static class that is subclassed from the Card class.
///	   
/// See the Card class for more information about what is needed for proper definitions and declarations

namespace DominionBase.Cards.Custom
{
	public static class TypeClass
	{
	}
}
