using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus write coil functions/requests.
    /// </summary>
    public class WriteSingleCoilFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteSingleCoilFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public WriteSingleCoilFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusWriteCommandParameters));
        }

        /// <inheritdoc />
        public override byte[] PackRequest()
        {
            ModbusWriteCommandParameters cp = this.CommandParameters as ModbusWriteCommandParameters;
            byte[] request = new byte[12];

            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)cp.TransactionId)), 0, request, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)cp.ProtocolId)), 0, request, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)cp.Length)), 0, request, 4, 2);

            request[6] = cp.UnitId;
            request[7] = cp.FunctionCode;

            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)cp.OutputAddress)), 0, request, 8, 2);

            ushort valueToSend = (cp.Value == (ushort)DState.ON) ? (ushort)0xFF00 : (ushort)0x0000;
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)valueToSend)), 0, request, 10, 2);

            return request;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            Dictionary<Tuple<PointType, ushort>, ushort> result = new Dictionary<Tuple<PointType, ushort>, ushort>();

            ushort address = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(response, 8));
            ushort rawValue = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(response, 10));

            ushort value = (rawValue == 0xFF00) ? (ushort)DState.ON : (ushort)DState.OFF;

            result.Add(new Tuple<PointType, ushort>(PointType.DIGITAL_OUTPUT, address), value);

            return result;
        }
    }
}