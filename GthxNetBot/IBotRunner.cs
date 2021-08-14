namespace GthxNetBot
{
    public interface IBotRunner
    {
        /// <summary>
        /// Start and run the bot
        /// </summary>
        public void Run();

        /// <summary>
        /// Stop the bot and exit
        /// </summary>
        public void Exit();
    }
}