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
		/// <summary>
		/// If line changed, but no skipped, emit the original line with a comment
		/// </summary>
		public bool EmitOriginal { get; set; }
		/// <summary>
		/// For formats without a TITLE directive, set the ACME
		/// Zone to the filename before the first code line
		/// </summary>
		public bool AutoSetZone { get; set; } = true;
	}
}
