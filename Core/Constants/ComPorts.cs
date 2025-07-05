namespace CheapSerial
{
    /// <summary>
    /// Common COM port identifiers
    /// </summary>
    public static class ComPorts
    {
        public const string COM1 = "COM1";
        public const string COM2 = "COM2";
        public const string COM3 = "COM3";
        public const string COM4 = "COM4";
        public const string COM5 = "COM5";
        public const string COM6 = "COM6";
        public const string COM7 = "COM7";
        public const string COM8 = "COM8";
        public const string COM9 = "COM9";
        public const string COM10 = "COM10";
        public const string COM11 = "COM11";
        public const string COM12 = "COM12";
        public const string COM13 = "COM13";
        public const string COM14 = "COM14";
        public const string COM15 = "COM15";
        public const string COM16 = "COM16";
        public const string COM17 = "COM17";
        public const string COM18 = "COM18";
        public const string COM19 = "COM19";
        public const string COM20 = "COM20";

        // Linux/Unix style ports
        public const string TTY_USB0 = "/dev/ttyUSB0";
        public const string TTY_USB1 = "/dev/ttyUSB1";
        public const string TTY_USB2 = "/dev/ttyUSB2";
        public const string TTY_USB3 = "/dev/ttyUSB3";
        public const string TTY_ACM0 = "/dev/ttyACM0";
        public const string TTY_ACM1 = "/dev/ttyACM1";
        public const string TTY_S0 = "/dev/ttyS0";
        public const string TTY_S1 = "/dev/ttyS1";

        /// <summary>
        /// Gets a COM port name by number (1-based)
        /// </summary>
        public static string GetComPort(int portNumber) => $"COM{portNumber}";

        /// <summary>
        /// Gets a USB TTY port by number (0-based)
        /// </summary>
        public static string GetTtyUsb(int portNumber) => $"/dev/ttyUSB{portNumber}";

        /// <summary>
        /// Gets an ACM TTY port by number (0-based)  
        /// </summary>
        public static string GetTtyAcm(int portNumber) => $"/dev/ttyACM{portNumber}";
    }
}