using System.Collections.Generic;

namespace CheapSerial.Configuration
{
    /// <summary>
    /// Configuration for multiple serial ports
    /// </summary>
    public class SerialPortConfiguration
    {
        public const string SectionName = "SerialPorts";

        public Dictionary<string, SerialPortOptions> Ports { get; set; } = new();
    }
}