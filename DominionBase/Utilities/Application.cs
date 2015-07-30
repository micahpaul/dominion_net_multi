using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DominionBase.Utilities
{
	public static class Application
	{
		public static String ApplicationPath
		{
			get
			{
				return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dominion .NET");
			}
		}
	}
}
