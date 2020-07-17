using Gthx.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Gthx.Test.Mocks
{
    public class MockWebReader : IWebReader
    {
        public async Task<Stream> GetStreamFromUrlAsync(string url)
        {
            var stream = await Task<Stream>.Run(() =>
            {
                var memStream = new MemoryStream(1024);
                var utf8Encoding = new UTF8Encoding();

                // Create the data to write to the stream.
                byte[] dataString;
                if (url.EndsWith("/title"))
                {
                    dataString = utf8Encoding.GetBytes(@"<head>
<title>Dummy Title - YouTube</title>
</head>");
                }
                else if (url.EndsWith("/meta"))
                {
                    dataString = utf8Encoding.GetBytes("<head>\r<meta name=\"title\" content=\"Meta Title\">\r</head>");
                }
                else
                {
                    dataString = utf8Encoding.GetBytes(@"<head>
</head>
<body>Sorry no title!</body>");
                }

                memStream.Write(dataString, 0, dataString.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                return memStream;
            });

            return stream;
        }
    }
}
