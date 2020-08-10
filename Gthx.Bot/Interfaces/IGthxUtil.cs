using System;
using System.Threading.Tasks;

namespace Gthx.Bot.Interfaces
{
    public interface IGthxUtil
    {
        /// <summary>
        /// User readable "gthx standard" time between two times
        /// </summary>
        /// <param name="firstTime">UTC DateTime of the first time to compare against <paramref name="secondTime"/></param>
        /// <param name="secondTime">UTC DateTime of the first time to compare against <paramref name="firstTime"/></param>
        /// <returns>A string in user readable format between the times passed in</returns>
        string TimeBetweenString(DateTime? firstTime, DateTime? secondTime = null);

        /// <summary>
        /// Gets the title from a given URL from either the title element
        /// or the 'meta title' element.
        /// </summary>
        /// <param name="url">URL to load</param>
        /// <returns></returns>
        Task<string> GetTitle(string url);
    }
}