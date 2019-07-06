using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace acmeconvert
{
	public class ADALine : Line
	{
		public ADALine()
		{
			var opcodes = StandardOpcodes + "|XOR"; // XOR allowed in 2500AD but not ACME

			var directives1 = @"BIT7|BYTE|DB|DS|DW|ELSE|ENDIF|END|EXTERN|EQUAL|EQU|";
			var directives2 = @"IFTRUE|IFFALSE|IF|INCLUDE|LIST|PAGE|PUBLIC|PW|ORG|TITLE|VAR";
			var directives = directives1 + directives2;

			// Symbol must begin in column 1 or end in :
			// Local symbols begin with '?'
			// Directives can begin with an optional '.'
			//   Because of BCC ?EQUALS and CMP #$DB, a negative lookback
			//   assurs that directives don't have '#' '$' or '?' as their word boundary
			// '.* captures comments so their contents don't become opcodes or directives

			var pattern = $@"(^(?<sym>[?]?\w+\:?|\s+\w+\:))?(\b((?<![#$?])([.]?(?<dr>{directives}))|(?<op>{opcodes}))\b)?('.*)?";

			CompiledRegex = new Regex
			(
				pattern,
				RegexOptions.Compiled |
					RegexOptions.IgnoreCase |
					RegexOptions.ExplicitCapture |
					RegexOptions.IgnorePatternWhitespace
			);
		}
	}
}
