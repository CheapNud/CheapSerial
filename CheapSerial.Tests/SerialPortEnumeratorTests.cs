using CheapSerial.Core.Models;
using CheapSerial.Implementation;
using Xunit;

namespace CheapSerial.Tests;

public class SerialPortEnumeratorTests
{
    private static readonly SerialPortInfo Cp210x = new("COM3", "Silicon Labs CP210x USB to UART Bridge", @"USB\VID_10C4&PID_EA60\0001");
    private static readonly SerialPortInfo Cp210xTwin = new("COM7", "Silicon Labs CP210x USB to UART Bridge", @"USB\VID_10C4&PID_EA60\0002");
    private static readonly SerialPortInfo Amt = new("COM4", "Intel(R) Active Management Technology - SOL", @"PCI\VEN_8086&DEV_A13E\3&11583659&0&B3");
    private static readonly SerialPortInfo BluetoothLink = new("COM5", "Standard Serial over Bluetooth link", @"BTHENUM\{00001101-0000-1000-8000-00805F9B34FB}_LOCALMFG&0000\8&1");
    private static readonly SerialPortInfo Dutch = new("COM6", "Serieel USB-apparaat", @"USB\VID_2341&PID_0043\85736323");

    [Fact]
    public void Filter_drops_blacklisted_descriptions()
    {
        var kept = SerialPortEnumerator.Filter([Cp210x, Amt, BluetoothLink], SerialPortEnumerator.DefaultBlacklist);

        Assert.Equal([Cp210x], kept);
    }

    [Fact]
    public void Find_returns_null_when_nothing_matches()
    {
        Assert.Null(SerialPortEnumerator.Find([Cp210x, Amt], "FTDI"));
    }

    [Fact]
    public void Find_matches_any_localised_fragment_case_insensitively()
    {
        var port = SerialPortEnumerator.Find([Cp210x, Dutch], "USB Serial Device", "serieel usb-apparaat");

        Assert.Same(Dutch, port);
    }

    [Fact]
    public void Find_refuses_to_guess_between_identical_adapters()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => SerialPortEnumerator.Find([Cp210x, Cp210xTwin], "CP210x"));

        Assert.Contains("COM3", ex.Message);
        Assert.Contains("COM7", ex.Message);
    }

    [Theory]
    [InlineData("Silicon Labs CP210x USB to UART Bridge (COM3)", "Silicon Labs CP210x USB to UART Bridge")]
    [InlineData("@oem12.inf,%cp210x.desc%;Silicon Labs CP210x USB to UART Bridge", "Silicon Labs CP210x USB to UART Bridge")]
    [InlineData("Communications Port (COM1)", "Communications Port")]
    public void CleanWindowsDescription_strips_indirect_prefix_and_port_suffix(string raw, string expected)
    {
        Assert.Equal(expected, SerialPortEnumerator.CleanWindowsDescription(raw));
    }

    [Fact]
    public void GetPorts_runs_on_this_platform()
    {
        // Smoke check only: hardware varies per machine, but enumeration must never throw.
        var ports = SerialPortEnumerator.GetPorts(includeBlacklisted: true);

        Assert.All(ports, port => Assert.False(string.IsNullOrEmpty(port.PortName)));
    }
}
