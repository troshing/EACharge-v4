using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Threading;

using System.Windows;
using System.Collections;
using System.IO;
using NModbus;
using NModbus.Data;
using NModbus.Device;
using NModbus.Utility;
using NModbus.IO;
using NModbus.Serial;

namespace EACharge_Out
{
    public class Bootloader
    {
        public bool IsPollBoot { get; private set; }

        private const int ValueReload = 0xA6A6;
        Task pollTaskBoot;
        public SerialPort _SerialPort { get; set; }
        public int SizeBuffer { get => sizeBuffer; set => sizeBuffer = value; }
        public int Iteration { get => iteration; set => iteration = value; }

        public List<char> arrayBuffer = new List<char>();
        public byte[] arrayCopy;
        public byte[] arraySend = new byte[0x200];
        private int iteration;
        public int indexCopy = 0;

        private string serialPortName;
        private int sizeBuffer;

        public string GetSerialPortName()
        {
            return serialPortName;
        }

        public void SetSerialPortName(string value)
        {
            serialPortName = value;
        }

        private SerialPortAdapter serialPortAdapter;
        public IModbusMaster master;
        public ModbusFactory factory;
        public byte SlaveAddress = 0x00; // по умолчанию
      
        public Bootloader()
        {           
        }

        public void CreatePortBootloader()
        {
            _SerialPort = new SerialPort(GetSerialPortName(), 115200, Parity.None, 8, StopBits.One);
            factory = new ModbusFactory();
            serialPortAdapter = new SerialPortAdapter(_SerialPort);

            master = factory.CreateRtuMaster(serialPortAdapter);
           
            master.Transport.ReadTimeout = 50;
            master.Transport.WriteTimeout = 10;
        }

        public void StartPollBoot()
        {
            IsPollBoot = true;
            pollTaskBoot = new Task(() => PollBootloader());
            pollTaskBoot.Start();
        }

        public void StopPollBoot()
        {
            IsPollBoot = false;
        }

        private void PollBootloader()
        {
            ushort startRegAdr = 9823;

            while (IsPollBoot)
            {
                master.ReadHoldingRegisters(SlaveAddress, startRegAdr,1);
                Thread.Sleep(500);
            }
        }

        public void ResetDevice()
        {
            ushort startRegAdr = 9823;              // REG_RELOAD [ 0x265F ]
            _SerialPort.Open();           
            master.WriteSingleRegister(SlaveAddress, startRegAdr, ValueReload);
            _SerialPort.Close();
        }
        public void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }

        public void SetDataBootloader(byte[] buffer)
        {
            ushort startRegAdr = 9827;                      // REG_PAGES_COUNT [0x2663]
            int sizeBuffer = buffer.Length;
            int fraction = 0;
            SizeBuffer = buffer.Length;
            fraction = SizeBuffer % 0x200;
            indexCopy = 0;
            if (fraction == 0)
            {
                Iteration = (SizeBuffer / 0x1000);
            }
            else
            {
                Iteration = (SizeBuffer / 0x200) + 1;
                SizeBuffer += 0x200;
                arrayCopy = new byte[SizeBuffer];
                Array.Copy(buffer, arrayCopy, buffer.Length);
            }
                        
            _SerialPort.Open();
            master.WriteSingleRegister(SlaveAddress, startRegAdr, (ushort)Iteration);
            Thread.Sleep(10);
            _SerialPort.Close();
        }

        public bool SendBlocks()
        {
            ushort startRegAdr = 9825;          // REG_LOAD_FLASH [ 0x2661 ]
            bool procOperation = false;
                                    
            _SerialPort.Open();
                           
                try
                {
                    Array.Clear(arraySend,0, 0x200);
                    Array.Copy(arrayCopy, indexCopy, arraySend,0, 0x200);
                    master.WriteFileRecord(SlaveAddress,(ushort)Iteration, startRegAdr, arraySend);
                    indexCopy += 0x200;
                    Thread.Sleep(10);
                }
                catch(Exception exc)
                {
                    _SerialPort.Close();
                    procOperation = false;
                    return procOperation;
                }
                
            procOperation = true;
            _SerialPort.Close();

            return procOperation;
        }
        public string GetPortName()
        {
            string[] portNames = SerialPort.GetPortNames();
            string portName = " ";
            if (portNames.Length != 0)
                portName = portNames[0];

            return portName;
        }

        public void CopyTo(ref byte[] massiv,int sizeNum)
        {
            for(int i=0;i< sizeNum;i++)
            {
                massiv[i] = Convert.ToByte(arrayBuffer[i]);
            }
        }
    }
}
