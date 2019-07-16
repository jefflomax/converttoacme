using NUnit;
using NUnit.Framework;
using Should;
using acmeconvert;
using System.Collections.Generic;
using System;
using System.Text;
using System.IO;

namespace NUnitTests
{
	public class AD2500ConversionTests
	{
		private Settings _settings;
		private acmeconvert.Convert _convert;

		[SetUp]
		public void Setup()
		{
			_settings = new Settings();
			_settings.EmitOriginal = false;
			_settings.AutoSetZone = false;
			_settings.Format = Formats.AD2500;
			_settings.Modules = new List<acmeconvert.Module>();
			_settings.Macros = new List<string>();
		}

		[TestCase(" DS 120", "!fill 120, $FF","")]
		[TestCase(" DS 120-LENG", "!fill 120-LENG, $FF","")]
		[TestCase(".ORG $+SECTION_LENGTH-OVERLAY_SIZE","!initmem $FF", "* = *+SECTION_LENGTH-OVERLAY_SIZE")]
		public void TestConvert( string line, string newLine, string nextLine )
		{
			var stream = MakeSteamReader(line);
			_convert = new ADAConvert("TEST.ASM", stream, _settings);

			// Converter results are .WriteLine, so capture
			// the console output
			using (var sw = new StringWriter())
			{
				Console.SetOut(sw);

				_convert.Process();

				var stringReader = new StringReader(sw.ToString());

				var result = stringReader.ReadLine().Trim();

				result.ShouldEqual(newLine);

				if (nextLine.Length == 0)
				{
					return;
				}

				result = stringReader.ReadLine().Trim();
				result.ShouldEqual(nextLine);

			}
		}

		[TestCase(" DB 'HELLO'", "!byte $C8,$C5,$CC,$CC,$CF")]
		public void TestBit7(string line, string newLine)
		{
			var stream = MakeSteamReader(line);
			_convert = new ADAConvert("TEST.ASM", stream, _settings);
			_convert.ManageBit7("ON", "", "");

			// Converter results are .WriteLine, so capture
			// the console output
			using (var sw = new StringWriter())
			{
				Console.SetOut(sw);

				_convert.Process();

				var result = sw.ToString().Trim();

				result.ShouldEqual(newLine);
			}
		}

		private StreamReader MakeSteamReader( string s )
		{
			byte[] byteArray = Encoding.ASCII.GetBytes(s);
			var stream = new MemoryStream(byteArray);
			var reader = new StreamReader(stream);
			return reader;
		}
	}
}
