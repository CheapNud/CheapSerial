namespace CheapSerial
{
    /// <summary>
    /// Voltage level constants for documentation and clarity
    /// </summary>
    public static class SerialVoltages
    {
        public const double DTR_HIGH_VOLTAGE = 12.0;    // ~12V when DTR is enabled
        public const double DTR_LOW_VOLTAGE = 0.0;      // 0V when DTR is disabled
        public const double RTS_HIGH_VOLTAGE = 12.0;    // ~12V when RTS is enabled  
        public const double RTS_LOW_VOLTAGE = 0.0;      // 0V when RTS is disabled
        public const double TTL_HIGH_VOLTAGE = 3.3;     // 3.3V logic high
        public const double TTL_LOW_VOLTAGE = 0.0;      // 0V logic low
        public const double RS232_HIGH_VOLTAGE = -12.0; // RS232 logic high (negative voltage)
        public const double RS232_LOW_VOLTAGE = 12.0;   // RS232 logic low (positive voltage)
    }
}