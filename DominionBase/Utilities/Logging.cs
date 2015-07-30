using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace DominionBase.Utilities
{
	public static class Logging
	{
		public static void LogError(Exception ex)
		{
			try
			{
				String errorLog = System.IO.Path.Combine(Utilities.Application.ApplicationPath, "error.log");
				if (!Directory.Exists(Path.GetDirectoryName(errorLog)))
				{
					Directory.CreateDirectory(Path.GetDirectoryName(errorLog));
				}
				using (StreamWriter sw = new StreamWriter(errorLog, true))
				{
					sw.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
					sw.WriteLine(String.Format("{0} : Exception thrown: {1}", DateTime.Now, ex.Message));
					sw.WriteLine(String.Format("{0} : Stack Trace: {1}", DateTime.Now, ex.StackTrace));
				}
			}
			catch (IOException ioeLog) { }
			catch (Exception eLog) { }
		}
	}
}
