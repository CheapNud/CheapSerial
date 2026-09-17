namespace CheapSerial.Core.Models
{
    /// <summary>
    /// A serial port as reported by the operating system.
    /// </summary>
    /// <param name="PortName">Name to open, e.g. "COM3" or "/dev/ttyUSB0".</param>
    /// <param name="Description">
    /// Human readable device name, e.g. "Silicon Labs CP210x USB to UART Bridge".
    /// Empty when the platform gives none. Windows localises this per UI language.
    /// </param>
    /// <param name="DeviceId">Plug and play instance path on Windows, the /dev/serial/by-id name on Linux, empty elsewhere.</param>
    public sealed record SerialPortInfo(string PortName, string Description, string DeviceId);
}
