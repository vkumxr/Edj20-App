using System;
using System.Threading.Tasks;
using Edj20Tester.Models;

namespace Edj20Tester
{
    public class ModbusPacket
    {
        public byte[] RawBytes { get; set; }
        public byte SlaveAddress { get; set; }
        public byte FunctionCode { get; set; }
        public ushort StartAddress { get; set; }
        public ushort Quantity { get; set; }
        public byte[] DataBytes { get; set; }
        public ushort Crc { get; set; }
        public bool IsResponse { get; set; }
        public byte ByteCount { get; set; }
        public ModbusFunction Function { get; set; }
    }

    public class DeviceResponse
    {
        public string Raw { get; }
        public ModbusPacket Request { get; set; }
        public ModbusPacket Response { get; set; }
        public bool IsError => Raw == "ERROR";
        public DeviceResponse(string raw) => Raw = raw;
    }

    public class DeviceClient
    {
        private const byte SlaveId = 0x01;

        private static ushort ComputeCrc(byte[] data)
        {
            ushort crc = 0xFFFF;
            foreach (byte b in data)
            {
                crc ^= b;
                for (int i = 0; i < 8; i++)
                    crc = (crc & 1) != 0
                        ? (ushort)((crc >> 1) ^ 0xA001)
                        : (ushort)(crc >> 1);
            }
            return crc;
        }

        public async Task<DeviceResponse> SendAsync(ModbusFunction function)
        {
            return await Task.Run(() =>
            {
                try
                {
                    byte fc = (byte)function;
                    byte addrHi = 0x00;
                    byte addrLo = 0x00;
                    byte qtyHi = 0x00;
                    byte qtyLo = 0x02;

                    // ── BUILD REQUEST ──────────────────────────────────────
                    byte[] reqCore = { SlaveId, fc, addrHi, addrLo, qtyHi, qtyLo };
                    ushort reqCrc = ComputeCrc(reqCore);
                    byte crcLo = (byte)(reqCrc & 0xFF);
                    byte crcHi = (byte)(reqCrc >> 8);
                    byte[] reqFull = { SlaveId, fc, addrHi, addrLo, qtyHi, qtyLo, crcLo, crcHi };

                    var req = new ModbusPacket
                    {
                        RawBytes = reqFull,
                        SlaveAddress = SlaveId,
                        FunctionCode = fc,
                        Function = function,
                        StartAddress = (ushort)((addrHi << 8) | addrLo),
                        Quantity = (ushort)((qtyHi << 8) | qtyLo),
                        Crc = reqCrc,
                        IsResponse = false
                    };

                    // ── BUILD SIMULATED RESPONSE ───────────────────────────
                    // FC01 / FC02 → bit-packed coil/discrete response (1 byte = 8 coils)
                    // FC03 / FC04 → register response (2 bytes per register)
                    byte[] resFull;
                    ModbusPacket res;

                    if (function == ModbusFunction.FC01_ReadCoils ||
                        function == ModbusFunction.FC02_ReadDiscreteInputs)
                    {
                        // 2 coils packed into 1 byte: coil1=ON(1), coil2=OFF(0) → 0x01
                        byte byteCount = 0x01;
                        byte coilData = 0x01;   // bit0=coil1 ON, bit1=coil2 OFF

                        byte[] resCore = { SlaveId, fc, byteCount, coilData };
                        ushort resCrc = ComputeCrc(resCore);
                        byte rCrcLo = (byte)(resCrc & 0xFF);
                        byte rCrcHi = (byte)(resCrc >> 8);
                        resFull = new byte[] { SlaveId, fc, byteCount, coilData, rCrcLo, rCrcHi };

                        res = new ModbusPacket
                        {
                            RawBytes = resFull,
                            SlaveAddress = SlaveId,
                            FunctionCode = fc,
                            Function = function,
                            ByteCount = byteCount,
                            DataBytes = new byte[] { coilData },
                            Crc = resCrc,
                            IsResponse = true
                        };
                    }
                    else
                    {
                        // FC03 / FC04 — two 16-bit registers
                        byte byteCount = 0x04;
                        byte d1Hi = 0x00; byte d1Lo = 0x06;   // Register 1 = 6
                        byte d2Hi = 0x00; byte d2Lo = 0x05;   // Register 2 = 5

                        byte[] resCore = { SlaveId, fc, byteCount, d1Hi, d1Lo, d2Hi, d2Lo };
                        ushort resCrc = ComputeCrc(resCore);
                        byte rCrcLo = (byte)(resCrc & 0xFF);
                        byte rCrcHi = (byte)(resCrc >> 8);
                        resFull = new byte[]
                            { SlaveId, fc, byteCount, d1Hi, d1Lo, d2Hi, d2Lo, rCrcLo, rCrcHi };

                        res = new ModbusPacket
                        {
                            RawBytes = resFull,
                            SlaveAddress = SlaveId,
                            FunctionCode = fc,
                            Function = function,
                            ByteCount = byteCount,
                            DataBytes = new byte[] { d1Hi, d1Lo, d2Hi, d2Lo },
                            Crc = resCrc,
                            IsResponse = true
                        };
                    }

                    return new DeviceResponse("OK") { Request = req, Response = res };
                }
                catch (Exception ex)
                {
                    return new DeviceResponse($"ERROR: {ex.Message}");
                }
            });
        }
    }
}
