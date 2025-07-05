using CheapSerial.Configuration;
using CheapSerial.Core.Enums;
using System.IO.Ports;

namespace CheapSerial.Interfaces
{
    /// <summary>
    /// Factory interface for creating serial port managers
    /// </summary>
    public interface ISerialPortFactory
    {
        ISerialPortManager CreateManager(SerialPortOptions options);
        ISerialPortManager CreateManager(string portName, int baudRate = (int)BaudRate.Baud115200,
            StopBits stopBits = StopBits.One, Parity parity = Parity.None, DataBits dataBits = DataBits.Eight);
        ISerialPortManager CreateManager(string portName, BaudRate baudRate,
            StopBits stopBits = StopBits.One, Parity parity = Parity.None, DataBits dataBits = DataBits.Eight);

        // Predefined configuration methods
        ISerialPortManager CreateArduino(string portName);
        ISerialPortManager CreateArduinoHighSpeed(string portName);
        ISerialPortManager CreateESP32(string portName);
        ISerialPortManager CreateRaspberryPi(string portName);
        ISerialPortManager CreateGPS(string portName);
        ISerialPortManager CreateBluetooth(string portName);
        ISerialPortManager CreateRS485(string portName);
        ISerialPortManager CreateIndustrial(string portName);
        ISerialPortManager CreateModem(string portName);
        ISerialPortManager CreateDebug(string portName);
    }
}