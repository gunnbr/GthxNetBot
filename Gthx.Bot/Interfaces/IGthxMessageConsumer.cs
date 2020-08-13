using System;
using System.Collections.Generic;
using System.Text;

namespace Gthx.Bot.Interfaces
{
    public interface IGthxMessageConsumer
    {
        public EventHandler<GthxMessageProducedEventArgs> MessageProducedHandler { get; set; }
    }
}
