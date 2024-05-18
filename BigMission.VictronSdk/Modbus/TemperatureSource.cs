using Microsoft.Extensions.Logging;
using NModbus;
using System.Net.Sockets;

namespace BigMission.VictronSdk.Modbus;

// https://www.victronenergy.com/live/ccgx:modbustcp_faq
/// <summary>
/// Connects to Victron Cerbo with Modbus to get temperature.
/// </summary>
public class TemperatureSource
{
    // Product ID	3300	uint16	1	0 to 65535	/ProductId	no	
    // Temperature scale factor	3301	uint16	100	0 to 655.35	/Scale no
    // Temperature offset	3302	int16	100	-327.68 to 327.67	/Offset no
    // Temperature type	3303	uint16	1	0 to 65535	/TemperatureType no	0=Battery;1=Fridge;2=Generic
    // Temperature	3304	int16	100	-327.68 to 327.67	/Temperature no  Degrees Celsius
    // Temperature status	3305	uint16	1	0 to 65535	/Status no	0=OK;1=Disconnected;2=Short circuited;3=Reverse Polarity;4=Unknown
    // Not Working: Humidity	3306	uint16	10	0 to 6553.3	/Humidity no	%
    // Not Working: Sensor battery voltage	3307	uint16	100	0 to 655.35	/BatteryVoltage no  V
    // Not Working: Atmospheric pressure	3308	uint16	1	0 to 65535	/Pressure no  hPa
    
    /// <summary>
    /// Requests temperature from Cerbo.
    /// </summary>
    /// <param name="cerboIp"></param>
    /// <param name="port"></param>
    /// <param name="sensorVrmInstance">ID under the VRM Sensor | Device | VRM Instance</param>
    /// <param name="logger"></param>
    /// <returns></returns>
    public static async Task<double?> GetTemperatureF(string cerboIp, int port, int sensorVrmInstance, ILogger? logger = null)
    {
        using TcpClient client = new(cerboIp, port);
        return await GetTemperatureF(client, sensorVrmInstance, logger);
    }

    public static async Task<double?> GetTemperatureF(TcpClient client, int sensorVrmInstance, ILogger? logger = null)
    {
        var factory = new ModbusFactory();
        IModbusMaster master = factory.CreateMaster(client);

        ushort startAddress = 3300;
        ushort numInputs = 6;
        logger?.LogDebug($"Requesting registers from Cerbo.");
        var registers = await master.ReadInputRegistersAsync((byte)sensorVrmInstance, startAddress, numInputs);

        var status = (SensorStatus)registers[5];
        if (status == SensorStatus.Ok)
        {
            double tempC = (short)registers[4] / 100.0;
            double tempF = (tempC * 1.8) + 32;
            logger?.LogInformation($"Received temperature C={tempC:0.#} F={tempF:0.#} for VRM Instance {sensorVrmInstance}");
            return tempF;
        }
        else
        {
            logger?.LogError($"Failed to access Cerbo temperature. Device status is not 'OK': {status}.");
        }
        return null;
    }
}
