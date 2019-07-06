using System;
using System.Collections.Generic;
using System.Text;

namespace acmeconvert
{
	public class Module
	{
		public string File { get; set; }
		public string Abr { get; set; }
		public List<string> Renames { get; set; }
	}
}
