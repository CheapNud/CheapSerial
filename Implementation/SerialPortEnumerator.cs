using CheapSerial.Core.Models;
using Microsoft.Win32;
using System.IO.Ports;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace CheapSerial.Implementation
{
    /// <summary>
    /// Lists serial ports with their device descriptions, so callers can pick a port by
    /// what is plugged in instead of by a COM number that changes between machines.
    /// Windows reads the registry (same data WMI serves, no extra dependency), Linux reads
    /// /dev/serial/by-id, everything else falls back to bare port names.
    /// </summary>
    public static partial class SerialPortEnumerator
    {
        /// <summary>
        /// Description fragments of ports that are never a real device: chipset management
        /// engines, Bluetooth virtual ports and hypervisor ports. Matched case-insensitively.
        /// </summary>
        public static readonly string[] DefaultBlacklist =
        [
            "Intel(R) Active Management",
            "Intel(R) Management Engine",
            "Bluetooth",
            "VMware",
            "Virtual",
        ];

        /// <summary>
        /// Ports present right now. Blacklisted ports are dropped unless <paramref name="includeBlacklisted"/>
        /// is set, which is the diagnostics view.
        /// </summary>
        public static SerialPortInfo[] GetPorts(bool includeBlacklisted = false)
        {
            var ports = OperatingSystem.IsWindows() ? GetWindowsPorts()
                : OperatingSystem.IsLinux() ? GetLinuxPorts()
                : SerialPort.GetPortNames().Select(name => new SerialPortInfo(name, "", "")).ToArray();

            return includeBlacklisted ? ports : Filter(ports, DefaultBlacklist);
        }

        /// <summary>
        /// The single port whose description contains any of the fragments. Pass every
        /// localised spelling of a device name, Windows translates friendly names per UI language.
        /// Returns null when nothing matches and throws when more than one port matches,
        /// because two identical adapters cannot be told apart by description.
        /// </summary>
        public static SerialPortInfo? FindPort(params string[] descriptionFragments)
            => Find(GetPorts(), descriptionFragments);

        public static SerialPortInfo[] Filter(IEnumerable<SerialPortInfo> ports, IEnumerable<string> blacklist)
            => ports.Where(port => !blacklist.Any(fragment => port.Description.Contains(fragment, StringComparison.OrdinalIgnoreCase))).ToArray();

        public static SerialPortInfo? Find(IEnumerable<SerialPortInfo> ports, params string[] descriptionFragments)
        {
            var matches = ports
                .Where(port => descriptionFragments.Any(fragment => port.Description.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            return matches.Length switch
            {
                0 => null,
                1 => matches[0],
                _ => throw new InvalidOperationException(
                    $"{matches.Length} ports match [{string.Join(", ", descriptionFragments)}]: " +
                    $"{string.Join(", ", matches.Select(port => $"{port.PortName} ({port.Description})"))}. Refusing to guess."),
            };
        }

        [SupportedOSPlatform("windows")]
        private static SerialPortInfo[] GetWindowsPorts()
        {
            // SERIALCOMM lists the ports that exist right now. The Enum tree also keeps every
            // adapter ever plugged in, so it is only used to look up descriptions for present ports.
            using var serialComm = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM");
            var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var valueName in serialComm?.GetValueNames() ?? [])
            {
                if (serialComm!.GetValue(valueName) is string portName)
                {
                    present.Add(portName);
                }
            }

            var found = new Dictionary<string, SerialPortInfo>(StringComparer.OrdinalIgnoreCase);
            using var enumKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum");
            foreach (var busName in enumKey?.GetSubKeyNames() ?? [])
            {
                using var busKey = enumKey!.OpenSubKey(busName);
                foreach (var deviceName in busKey?.GetSubKeyNames() ?? [])
                {
                    using var deviceKey = busKey!.OpenSubKey(deviceName);
                    foreach (var instanceName in deviceKey?.GetSubKeyNames() ?? [])
                    {
                        using var instanceKey = deviceKey!.OpenSubKey(instanceName);
                        using var parameters = instanceKey?.OpenSubKey("Device Parameters");
                        if (parameters?.GetValue("PortName") is not string portName || !present.Contains(portName))
                        {
                            continue;
                        }

                        var description = instanceKey!.GetValue("FriendlyName") as string
                            ?? instanceKey.GetValue("DeviceDesc") as string
                            ?? "";
                        found.TryAdd(portName, new SerialPortInfo(portName, CleanWindowsDescription(description), $@"{busName}\{deviceName}\{instanceName}"));
                    }
                }
            }

            foreach (var portName in present)
            {
                found.TryAdd(portName, new SerialPortInfo(portName, "", ""));
            }

            return found.Values.OrderBy(port => port.PortName, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        /// <summary>
        /// DeviceDesc can be an indirect string ("@oem12.inf,%desc%;Real Name") and FriendlyName
        /// carries a trailing " (COM3)" that duplicates PortName. Both are stripped.
        /// </summary>
        internal static string CleanWindowsDescription(string description)
        {
            if (description.StartsWith('@'))
            {
                description = description[(description.LastIndexOf(';') + 1)..];
            }

            return TrailingPortSuffix().Replace(description, "").Trim();
        }

        private static SerialPortInfo[] GetLinuxPorts()
        {
            // udev keeps one symlink per USB serial device, named after the device, pointing at /dev/ttyUSBn.
            const string byIdDirectory = "/dev/serial/by-id";
            var descriptions = new Dictionary<string, string>();
            if (Directory.Exists(byIdDirectory))
            {
                foreach (var link in Directory.EnumerateFileSystemEntries(byIdDirectory))
                {
                    var target = new FileInfo(link).ResolveLinkTarget(returnFinalTarget: true)?.FullName;
                    if (target is not null)
                    {
                        descriptions[target] = Path.GetFileName(link);
                    }
                }
            }

            return SerialPort.GetPortNames()
                .Select(name => descriptions.TryGetValue(name, out var id)
                    ? new SerialPortInfo(name, id.Replace('_', ' '), id)
                    : new SerialPortInfo(name, "", ""))
                .ToArray();
        }

        [GeneratedRegex(@"\s*\(COM\d+\)\s*$", RegexOptions.IgnoreCase)]
        private static partial Regex TrailingPortSuffix();
    }
}
