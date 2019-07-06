using NUnit;
using NUnit.Framework;
using System.Linq;
using Should;
using acmeconvert;
using System.Reflection;
using System.Collections.Generic;
using System;

namespace NUnitTests
{
	public class MadsUnitTest
	{
		private Line _line;

		[SetUp]
		public void Setup()
		{
			_line = new MADSLine();
		}

		[TestCase("TEMP0 =$00","TEMP0","","=")]
		[TestCase("INITIL LDA #'1","INITIL","LDA","")]
		[TestCase(" ASL A","","ASL","")]
		[TestCase(".BYTE $29,18,$A,0,$9E", "", "", "BYTE")]
		[TestCase("    SETSCN LDA #$10 ", "SETSCN", "LDA", "")]
		[TestCase(" ASL A", "", "ASL", "")]
		[TestCase(" LDA #$00", "", "LDA", "")]
		[TestCase("FLAPQUACK ASL A", "FLAPQUACK", "ASL", "")]
		[TestCase("FLAPQUACK BYTE $0", "FLAPQUACK", "", "BYTE")]
		[TestCase("FLAPQUACK .DBBYTE $0", "FLAPQUACK", "", "DBBYTE")]
		[TestCase(" BYTE 0", "", "", "BYTE")]
		[TestCase(".BYTE 0", "", "", "BYTE")]
		[TestCase("TEMP0 =2", "TEMP0", "", "=")]
		[TestCase("*=$E000", "*", "", "=")]
		[TestCase("HEGET LDA $21E,X", "HEGET", "LDA", "")]
		[TestCase("   TOLETT STY TEMP0", "TOLETT", "STY", "")]
		[TestCase(" CMP #'Y", "", "CMP", "")]
		[TestCase("  SLAP: .WOR $ffff", "SLAP:", "", "WOR")]
		[TestCase("  SLAP: WOR $ffff", "SLAP:", "", "WOR")]
		[TestCase("MNLOOP JSR BEPEND","MNLOOP","JSR","")]
		[TestCase(".BYTE 'SEARCH FOR?>REPLACE WITH?>'","","","BYTE")]
		[TestCase(".BYTE 'WORD RET EOT(W/R/',$C5,')>'","","","BYTE")]
		[TestCase("REPCOL .BYT 4 ", "REPCOL","","BYT")]
		[TestCase("ENDPRG .BYT $93,13,$FF,4,' PRESS Y TO '","ENDPRG","","BYT")]
		public void MadsTest( string line, string symbol, string op, string drctv )
		{
			_line.Parse(line);
			_line.Symbol.ShouldEqual(symbol);
			_line.OpCode.ShouldEqual(op);
			_line.Directive.ShouldEqual(drctv);
		}

		[TestCase(" CMP #'0         ;OK?","","CMP","", " #'0'         ")]
		public void MadsOperandTest
		(
			string line,
			string symbol,
			string op,
			string directive,
			string operand
		)
		{
			_line.Parse(line);
			_line.Symbol.ShouldEqual(symbol);
			_line.OpCode.ShouldEqual(op);
			_line.Directive.ShouldEqual(directive);
			_line.Operand.ShouldEqual(operand);
		}
	}
}
