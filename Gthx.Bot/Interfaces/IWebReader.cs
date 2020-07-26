using System.IO;
using System.Threading.Tasks;

namespace Gthx.Bot.Interfaces
{
    public interface IWebReader
    {
        /// <summary>
        /// Returns a stream with the data from a URL 
        /// </summary>
        /// <param name="url">URL of the data to get</param>
        /// <returns>A Stream with the data returned from the host</returns>
        public Task<Stream> GetStreamFromUrlAsync(string url);
    }
}
