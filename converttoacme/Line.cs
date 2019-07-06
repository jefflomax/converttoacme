using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace acmeconvert
{
	[DebuggerDisplay("Symbol:{Symbol} Directive:{Directive} OpCode:{OpCode} {Operand}")]
	public class Line
	{
		// TODO: Can we get the operand extracted by RegEx

		public bool HasComment { get; private set; }
		public bool IsCommentOrEmpty { get; private set; }
		public bool HasCode { get; private set; }
		public bool HasSymbolDirectiveOrOpCode
		{
			get
			{
				return HasSymbol ||
					Directive.Length > 0 ||
					OpCode.Length > 0;
			}
		}
		public bool HasSymbol { get { return Symbol.Length > 0; } }
		public string Symbol { get; set; }
		public string Directive { get; private set; }
		public string OpCode { get; private set; }
		public string Operand { get; set; }
		public string Code { get; set; }
		public string Comment { get; private set; }

		public string OriginalLine {get; set;}
		protected CommentInfo CommentInformation {get; set;}
		protected Regex CompiledRegex {
			get; set;
		}

		// Just for readability
		protected const char COMMENT = ';';
		protected const char TICK = '\'';
		protected const char QUOTE = '"';

		protected string StandardOpcodes{ get; }

		public Line()
		{
			var opcodesa = @"ADC|AND|ASL|";
			var opcodesb = @"BCC|BCS|BEQ|BIT|BMI|BNE|BPL|BRK|BVC|BVS|";
			var opcodesc = @"CLC|CLD|CLI|CLV|CMP|CPX|CPY|";
			var opcodesd = @"DEC|DEX|DEY|";
			var opcodese = @"EOR|";
			var opcodesi = @"INC|INX|INY|";
			var opcodesj = @"JMP|JSR|";
			var opcodesl = @"LDA|LDX|LDY|LSR|";
			var opcodesn = @"NOP|";
			var opcodeso = @"ORA|";
			var opcodesp = @"PHA|PHP|PLA|PLP|";
			var opcodesr = @"ROL|ROR|RTI|RTS|";
			var opcodess = @"SBC|SEC|SED|SEI|STA|STX|STY|";
			var opcodest = @"TAX|TAY|TSX|TXA|TXS|TYA";

			StandardOpcodes = opcodesa + opcodesb + opcodesc + opcodesd +
				opcodese + opcodesi + opcodesj + opcodesl + opcodesn +
				opcodeso + opcodesp + opcodesr + opcodess + opcodest;
		}

		public virtual void Parse(string line)
		{
			OriginalLine = line;
			line = PreSyntaxFixes(line);

			CommentInformation = SplitAtComment(line);
			HasComment = CommentInformation.HasComment;
			Code = CommentInformation.Code;
			Comment = CommentInformation.Comment;

			HasCode = Code.Length > 0 && !Code.All(ch => char.IsWhiteSpace(ch));
			IsCommentOrEmpty = HasComment && !HasCode;

			if (!HasCode)
			{
				Symbol = string.Empty;
				Directive = string.Empty;
				OpCode = string.Empty;
				Operand = string.Empty;
				return;
			}

			if (Match())
			{
				PostSyntaxFixes();
				return;
			}

			Symbol = string.Empty;
			Directive = string.Empty;
			OpCode = string.Empty;
			Operand = string.Empty;
		}

		private bool Match()
		{
			// Named Group Match will not set groups not found
			Group sym = null;
			int symIndex = 0;
			int symLength = 0;
			Group opc = null;
			int opcIndex = 0;
			int opcLength = 0;
			Group drc = null;
			int drcIndex = 0;
			int drcLength = 0;

			Symbol = string.Empty;
			OpCode = string.Empty;
			Directive = string.Empty;

			//evaluate results of Regex and for each match
			var matches = CompiledRegex.Matches(Code);
			foreach (Match m in matches)
			{
				//loop through all the groups in current match
				for (int x = 1; x < m.Groups.Count; x++)
				{
					//print the names wherever there is a succesful match
					if (m.Groups[x].Success)
					{
						var group = m.Groups[x];
						var groupName = CompiledRegex.GroupNameFromNumber(x);
						switch (groupName)
						{
							case "sym":
								sym = group;
								symIndex = group.Index;
								symLength = group.Length;
								Symbol = group.Value;
								break;
							case "op":
								opc = group;
								opcIndex = group.Index;
								opcLength = group.Length;
								OpCode = group.Value;
								break;
							case "dr":
								drc = group;
								drcIndex = group.Index;
								drcLength = group.Length;
								Directive = group.Value;
								break;
						}
					}
				}
			}

			if (sym != null || drc != null || opc != null)
			{
				// Find beginning of operand
				var startOfOperand = opcIndex + opcLength;
				var otherEnd = drcIndex + drcLength;
				if (otherEnd > startOfOperand)
				{
					startOfOperand = otherEnd;
				}
				otherEnd = symIndex + symLength;
				if (otherEnd > startOfOperand)
				{
					startOfOperand = otherEnd;
				}

				Operand = Code.Substring(startOfOperand);

				return true;
			}

			return false;
		}

		protected virtual string PreSyntaxFixes(string line)
		{
			return line;
		}

		protected virtual void PostSyntaxFixes()
		{
			// ASM syntax variations fixes needed before processing
		}


		/// <summary>
		/// MADS doesn't require a string to be terminated, ACME does
		/// </summary>
		protected void FixUnterminatedString()
		{
			var firstSpaceInCode = CommentInformation.FirstSpaceInCode;
			if (Operand.IndexOf(TICK) != -1)
			{
				(var _, var terminated, var firstSpaceInOperand) =
					FirstCommentNotInString(Operand);

				if (terminated)
				{
					return;
				}

				// Probably doesn't handle unterminated spaces
				Code = Code.Insert
				(
					firstSpaceInCode == -1 ? Code.Length : firstSpaceInCode,
					"'"
				);
				Operand = Operand.Insert
				(
					firstSpaceInOperand == -1 ? Operand.Length: firstSpaceInOperand,
					"'"
				);
			}
		}

		public override string ToString()
		{
			return $"Symbol:{Symbol} Directive:{Directive} OpCode:{OpCode} Operand:{Operand} Comment:{Comment}";
		}

		protected class CommentInfo
		{
			public bool HasComment { get; }
			public string Code  { get; }
			public string Comment { get; }
			public int FirstSpaceInCode { get; }
			public CommentInfo
			(
				bool hasComment,
				string code,
				string comment,
				int firstSpaceInString = -1
			)
			{
				HasComment = hasComment;
				Code = code;
				Comment = comment;
				FirstSpaceInCode = firstSpaceInString;
			}
		}

		private CommentInfo SplitAtComment(string line)
		{
			bool _;
			var firstCommentChar = line.IndexOf(COMMENT);
			if (firstCommentChar < 0)
			{
				return new CommentInfo(false, line, string.Empty);
			}

			// There is a comment char, is there any string before it?
			var codeSpan = line.AsSpan().Slice(0, firstCommentChar);
			var quoteChar = codeSpan.IndexOf(TICK);
			if (quoteChar < 0)
			{
				return new CommentInfo
				(
					true,
					codeSpan.ToString(),
					line.Substring(firstCommentChar).TrimEnd()
				);
			}

			// Find the actual first ;
			// Comment in UNTERMINATED string blows up.
			var firstSpaceInComment = -1;
			(firstCommentChar, _, firstSpaceInComment) = FirstCommentNotInString(line);

			if (firstCommentChar < 0)
			{
				// There is no comment, the only ; is within a string
				return new CommentInfo(false, line, string.Empty, firstSpaceInComment);
			}

			return new CommentInfo
			(
				true,
				line.Substring(0, firstCommentChar),
				line.Substring(firstCommentChar).TrimEnd(),
				firstSpaceInComment
			);
		}

		private
		(
			int commentOffset,
			bool terminated,
			int firstSpaceInComment
		) FirstCommentNotInString(string line)
		{
			var inString = false;
			var firstSpaceInComment = -1;

			// MADS unterminated strings mean that we see
			// the comment character inside a string.  Look for
			// last comment and use it as a backstop
			var lastCommentChar = line.LastIndexOf(COMMENT);

			// assuming single quote in string is '' and not \'
			for (var index = 0; index < line.Length; index++)
			{
				var ch = line[index];

				if (inString)
				{
					if (ch == ' ' && firstSpaceInComment == -1)
					{
						firstSpaceInComment = index;
					}
					else if (ch != TICK)
					{
						continue;
					}
					else
					{
						inString = false;
						continue;
					}
				}

				if (ch == TICK)
				{
					inString = true;
				}
				else if (ch == COMMENT)
				{
					return (index, !inString, firstSpaceInComment);
				}
			}

			if (inString && lastCommentChar != -1)
			{
				// Unterminated string, return backstop COMMENT char
				return (lastCommentChar, !inString, firstSpaceInComment);
			}

			return (-1, !inString,  firstSpaceInComment);
		}
	}
}
