using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheapSerial.Core.Models
{
    /// <summary>
    /// Event arguments for received data
    /// </summary>
    public class SerialDataReceivedEventArgs(string portName, byte[] data) : EventArgs
    {
        /// <summary>
        /// Async event handler for data received events
        /// </summary>
        public delegate Task SerialDataReceivedHandler(object sender, SerialDataReceivedEventArgs e);
        public string PortName { get; } = portName;
        public byte[] Data { get; } = data;
    }
}
