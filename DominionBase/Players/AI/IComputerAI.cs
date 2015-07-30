using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DominionBase.Players.AI
{
	internal interface IComputerAI
	{
		AIState State { get; }
	}
}
