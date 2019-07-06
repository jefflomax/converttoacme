using System;
using System.Collections.Generic;
using System.Text;

namespace acmeconvert
{
	public class Settings
	{
		public Formats Format { get; set; }
		public List<Module> Modules { get; set; }
		public List<string> Macros { get; set; }
		public bool EmitOriginal { get; set; }
	}
}
