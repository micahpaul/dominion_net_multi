using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace Dominion.NET_WPF
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private void Application_Startup(object sender, StartupEventArgs e)
		{
			if (e.Args != null)
			{
				String param = String.Empty;
				for (int i = 0; i < e.Args.Count(); i++)
				{
					String arg = e.Args.ElementAt(i);
					switch (arg)
					{
						case "-u":
							param = "Update";
							break;

						case "-U":
							this.Properties["Updated"] = true;
							break;

						default:
							if (!String.IsNullOrWhiteSpace(param))
							{
								this.Properties[param] = arg;
							}
							param = String.Empty;
							break;
					}
				}
			}
		}
	}
}
