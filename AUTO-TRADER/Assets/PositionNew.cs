using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AUTO_TRADER
{
    internal class PositionNew
    {
        public string epic = null;
        public string expiry = "-";
        public string direction = null;
        public string size = "1.0";
        public string orderType = "MARKET";
        public string timeInForce = "FILL_OR_KILL";
        public string level = null;
        public string guaranteedStop = "true";
        public string stopLevel = null;
        public string stopDistance = null;
        public string trailingStop = null;
        public string trailingStopIncrement = null;
        public string forceOpen = "true";
        public string limitLevel = null;
        public string limitDistance = null;
        public string quoteId = null;
        public string currencyCode = null;

        public PositionNew(string epic, string direction, string stopDistance, string limitDistance, string currencyCode)
        {
            this.epic = epic;
            this.direction = direction;
            this.stopDistance = stopDistance;
            this.limitDistance = limitDistance;
            this.currencyCode = currencyCode;
        }
    }
}
