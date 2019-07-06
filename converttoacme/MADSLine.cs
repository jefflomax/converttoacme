using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace acmeconvert
{
	public class MADSLine : Line
	{
		public MADSLine()
		{
			// Directives do require a leading ".", but not =
			var directives = @"BY(T|TE)|DBBYTE|END|FIL|LIB|OPT|PAGE|SKIP|WORD|WOR";

			// Positive Look Behind (? < = expr ) If preceeded by
			// Negative Look Ahead ( ? ! expr ) Not preceeded by
			// Positive Look Ahead ( ? = expr )

			// TODO: {1,6} length restriction on symbol?

			// Various failures
			//Pattern = $@"(^\s*(?!({opcodes}|{directivesNoPrefix})|\.?({directives}))(?<sym>[*]|[A-Za-z]\w*:?))?(?<op>{opcodes})?(\.?((?<dr>{directives}|{directivesNoPrefix})\b))?";
			//Pattern = $@"(^\s*(?!({opcodes}|=)|\.?({directives}))(?<sym>[*]|[A-Za-z]\w*:?))?(\b(?<op>{opcodes})\b)?(((?<dr>([=])|(\b\.?({directives})\b))))?";
			//Pattern = $@"(^\s*(?!({opcodes}|=)|\.?({directives}))(?<sym>[*]|[A-Za-z]\w*:?))?( (\b(?<op>{opcodes})\b)|  (   (    (?<dr>([=]) | (\b\.?({directives})\b)    )   )  ) )?('.*)?";

			// Look for an optional <symBOL> that is either * or Alpha char followed by word chars and optional :
			//      That is not preceeded by any opcode, directive, or =,
			//      where those are followed by a word boundry, to assure we don't exclude a symbol
			//      that contains a directive (e.g. ENDPRG)
			//      MADS symbols are free form and can start anywhere as long as they precede opcodes or directives
			// Then look for an optional <opCODE> that is within a word boundry
			//      or an optional <dIrECTIVE> that is =
			//      or one of the directives within a word boundary optionally preceded by '.'
			// Then, to get RegEx to STOP and not include text that happens to contain a directive
			//      optionally capture any text starting with an apostrophe (')
			// Sadly, I've no idea how to get RegEx to capture the Operand
			var pattern = $@"(^\s*(?!({StandardOpcodes}|=)|\.?({directives})\b)(?<sym>[*]|[A-Za-z]\w*:?))?( (\b(?<op>{StandardOpcodes})\b)|  (   (    (?<dr>([=]) | (\b\.?({directives})\b)    )   )  ) )?('.*)?";

			CompiledRegex = new Regex
			(
				pattern,
				RegexOptions.Compiled |
					RegexOptions.IgnoreCase |
					RegexOptions.ExplicitCapture |
					RegexOptions.IgnorePatternWhitespace
			);
		}

		protected override void PostSyntaxFixes()
		{
			if (OpCode.Length > 0 || Directive.Length > 0)
			{
				FixUnterminatedString();
			}
		}
	}
}
