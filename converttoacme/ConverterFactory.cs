using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace acmeconvert
{
	public class ConverterFactory
	{
		public Convert CreateConverter
		(
			Formats format,
			string fileToConvert,
			Settings settings
		)
		{
			var streamReader = new StreamReader(fileToConvert);
			switch (format)
			{
				case Formats.AD2500:
					return new ADAConvert
					(
						fileToConvert,
						streamReader,
						settings
					);

				case Formats.MADS:
					return new MADSConvert
					(
						fileToConvert,
						streamReader,
						settings
					);

				case Formats.PDS:
					return new PDSConvert
					(
						fileToConvert,
						streamReader,
						settings
					);

				default:
					throw new Exception("Invalid format requested");
			}
		}
	}
}
