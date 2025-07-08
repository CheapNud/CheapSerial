using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheapSerial.Core.Models
{
    /// <summary>
    /// Event arguments for connection status changes
    /// </summary>
    public class ConnectionStatusChangedEventArgs(string portName, bool isConnected) : EventArgs
    {
        /// <summary>
        /// Async event handler for connection status changes
        /// </summary>
        public delegate Task ConnectionStatusChangedHandler(object sender, ConnectionStatusChangedEventArgs e);

        public string PortName { get; } = portName;
        public bool IsConnected { get; } = isConnected;
    }
}
