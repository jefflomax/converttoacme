using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static System.Console;

namespace acmeconvert
{
	// TODO:
	// Consider auto-comment of masking/self modify !byte $2C
	// Consider auto-comment close of conditional
	// Verify PUBLIC are present
	// Verify EXTERN are referenced
	// Consider converting all non public symbols to local

	// VAR
	// Equates a label to a value.
	// The assignment with VAR can be changed as often as desired through-out the program.

	// https://operation8bit.wordpress.com/author/operation8bit/
	// http://www.6502.org/documents/datasheets/

	// RegEx hints
	// * zero or more, + One or more, ? Zero or 1
	// Regex Special: \ ^ $ . | ? * + ( ) [ {

	public class Program
	{
		static void Main( string[] args )
		{
			var settingsFile = args[1];

			var settings = JsonConvert.DeserializeObject<Settings>
			(
				new System.IO
					.StreamReader(settingsFile)
					.ReadToEnd()
			);

			Formats format = settings.Format;

			var factory = new ConverterFactory();

			var convert = factory.CreateConverter
			(
				format,
				args[0],
				settings
			);

			convert.Process();
		}
	}
}
