using System.Net;
using NetworkManager.DBus;
using Tmds.DBus;

namespace ConsoleApp1
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Network Manager tester");

            // Note that I'm running this in a docker container, which is why the custom bus is specified
            var dbusSystemConnection = new Connection("unix:path=/host/run/dbus/system_bus_socket");
            var dbusNetworkManager = dbusSystemConnection.CreateProxy<INetworkManager>("org.freedesktop.NetworkManager", "/org/freedesktop/NetworkManager");
            //var dbusNetworkManagerI = dbusSystemConnection.CreateProxy<NetworkManager.DBus.INetworkManager>("org.freedesktop.NetworkManager.Settings.Connection", "/org/freedesktop/NetworkManager");
            //var dbusNetworkManagerS = dbusSystemConnection.CreateProxy<NetworkManager.DBus.ISettings>("org.freedesktop.NetworkManager.Settings", "/org/freedesktop/NetworkManager/Settings");

            // Connect
            await dbusSystemConnection.ConnectAsync();
            try
            {

                Console.WriteLine("List devices");

                foreach (var device in await dbusNetworkManager.GetDevicesAsync())
                {
                    var interfaceName = await device.GetInterfaceAsync();

                    Console.WriteLine($"Interface: {interfaceName}");

                    // For testing
                    //if (interfaceName != "eth0")
                    //    continue;

                    var ipv4Config = await device.GetIp4ConfigAsync();

                    Console.WriteLine($"Gateway: {await ipv4Config.GetGatewayAsync()}");

                    foreach (var addressDetails in await ipv4Config.GetAddressDataAsync())
                    {

                        if (addressDetails.TryGetValue("address", out var addressValue))
                            Console.WriteLine($"Address: {(string)addressValue}");

                        if (addressDetails.TryGetValue("prefix", out var prefixValue))
                            Console.WriteLine($"NetmaskLength: {(int)(uint)prefixValue}");
                    }

                    var properties2 = await ipv4Config.GetAllAsync();
                    ListProps(properties2);
                    Console.WriteLine();
                }

                var properties = await dbusNetworkManager.GetAllAsync();
                ListProps(properties);

                Console.WriteLine();
                Console.WriteLine("Active connection:");

                var activeConnections = await dbusNetworkManager.GetActiveConnectionsAsync();
                foreach (var connection in activeConnections)
                {
                    Console.WriteLine($"Connection: {connection}");
 
                    var proxy1 = dbusSystemConnection.CreateProxy<NetworkManager.DBus.IConnection>("org.freedesktop.NetworkManager", connection);

                    // Throws exception here: Unhandled exception. Tmds.DBus.DBusException: org.freedesktop.DBus.Error.UnknownMethod: No such interface “org.freedesktop.NetworkManager.Settings.Connection” on object at path /org/freedesktop/NetworkManager/ActiveConnection/2
                    var settings = await proxy1.GetSettingsAsync();
                    foreach (var step1 in settings)
                    {
                        Console.WriteLine($"Key: {step1.Key}");
                        ListPropsDict(step1.Value);
                    }
                }

                //var primaryConnection = await dbusNetworkManager.GetPrimaryConnectionAsync();
                //var settings = await primaryConnection.GetSettingsAsync();
                //ListProps(settings);
            }
            finally
            {
                dbusSystemConnection.Dispose();
            }
        }

        private static void ListProps(object obj)
        {
            foreach (var prop in obj.GetType().GetProperties())
            {
                var value = prop.GetValue(obj);
                Console.WriteLine($"{prop.Name}={value}");
            }
        }

        private static void ListPropsDict(IDictionary<string, object> input)
        {
            foreach (var prop in input)
            {
                Console.WriteLine($"{prop.Key}={prop.Value}");
            }
        }
    }
}
