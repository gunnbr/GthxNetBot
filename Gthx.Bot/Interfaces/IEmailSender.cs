using System.Threading.Tasks;

namespace Gthx.Bot.Interfaces
{
    /// <summary>
    /// Sends email to the bot's admin
    /// </summary>
    public interface IEmailSender
    {
        /// <summary>
        /// False when email isn't set up, so there's no point preparing any
        /// </summary>
        public bool IsEnabled { get; }

        public Task SendAsync(string subject, string body);
    }

    /// <summary>
    /// Used when email isn't set up
    /// </summary>
    public class NoEmailSender : IEmailSender
    {
        public bool IsEnabled => false;

        public Task SendAsync(string subject, string body) => Task.CompletedTask;
    }
}
