using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using NModbus;
using NModbus.Device;
using NModbus.Serial;
using NModbus.SerialPortStream;
using NModbus.Logging;
using NModbus.Extensions.Enron;
using EACharge;
using System.Collections;

namespace EACharge
{
    public class EAMaster : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        protected bool SetField<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        Task pollTask;
        public bool IsPoll { get; private set; }

        public EAChargeMonitor eaChargeMonitor; //{ get; set; }
        public EAMotoCounter EAMotoCounter { get; set; } 

        private EATestUnit testUnit {  get; set; }

        private SerialPortAdapter serialPortAdapter;
        private IModbusLogger modbusLogger;
        private IModbusMaster ModbusMaster { get; set; }
               
        private ModbusFactory factory;

        public SerialPort _SerialPort { get; set; }

        public byte SlaveAddress { get; set; }

        private string _info;
        public string Info
        {
            get => _info;
            set
            {
                SetField(ref _info, value, "Info");
            }
        }

        public EAMaster()
        {
            _SerialPort = new SerialPort(GetPortName(), 115200, Parity.None, 8, StopBits.One);
            _SerialPort.ReadTimeout = 100;
            _SerialPort.WriteTimeout = 100;
            SlaveAddress = 0x0A;                // по умолчанию "10" 
            serialPortAdapter = new SerialPortAdapter(_SerialPort);

            modbusLogger = new ConsoleModbusLogger(LoggingLevel.Debug);
            factory = new ModbusFactory(null,true,modbusLogger);
            
            ModbusMaster = factory.CreateRtuMaster(serialPortAdapter);
            ModbusMaster.Transport.WriteTimeout = 100;
            ModbusMaster.Transport.ReadTimeout = 3000;
            
            modbusLogger = factory.Logger;
            modbusLogger.ShouldLog(LoggingLevel.Debug);
            
            ModbusMaster.Transport.Retries = 1;
            ModbusMaster.Transport.WaitToRetryMilliseconds = 300;

            eaChargeMonitor = new EAChargeMonitor();
            EAMotoCounter = new EAMotoCounter();
            EAMotoCounter.SetTestData();
            testUnit = new EATestUnit();                // Создание класса для Тестов
            UpdateInfo();
        }

        public void GetChargeMonitor(ref EAChargeMonitor monitor)
        {
            monitor = eaChargeMonitor;
        }
        public string GetPortName()
        {
            string[] portNames = SerialPort.GetPortNames();
            string portName = " ";
            if (portNames.Length != 0)
                portName = portNames[0];

            return portName;
        }

        public bool ArePortsExist()
        {
            string[] portNames = SerialPort.GetPortNames();
            if (portNames.Length != 0) return true;
            else return false;
        }

        public void UpdateInfo()
        {
            Info = $" Адрес устройства : {SlaveAddress}       Имя: {_SerialPort.PortName}    Скорость: {_SerialPort.BaudRate}       Данные: {_SerialPort.DataBits}       " +
                $"Четность: {_SerialPort.Parity}        Стопбиты: {_SerialPort.StopBits}";
        }

        public void WriteRegisters(ushort startAddress, ushort[] data)
        {
            try
            {
                _SerialPort.Open();
                ModbusMaster.WriteMultipleRegisters(SlaveAddress, startAddress, data);
                _SerialPort.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show("Исключение: " + e.ToString());
                _SerialPort.Close();
                throw;
            }
        }

        public void WriteRegisters(RegisterBase registerBase)
        {
            try
            {
                _SerialPort.Open();
                switch (registerBase)
                {
                    case IValue<float> r:                        
                        ModbusMaster.WriteMultipleRegisters(SlaveAddress, registerBase.Address, Converter.ConvertFloatToTwoUint16(r.Value));
                        break;
                    case IValue<string> r:
                        string defaultString = new string(' ', 20);
                        byte[] stringBytes = Encoding.Default.GetBytes(r.Value);
                        stringBytes.CopyTo(stringBytes, 0);
                        ModbusMaster.WriteMultipleRegisters(SlaveAddress, registerBase.Address, Converter.ConvertByteArrayToUshortArray(stringBytes));
                        break;
                    case IValue<ushort> r:
                        if (registerBase.Address == 3005)
                        {
                            eaChargeMonitor.valPreAlarm = r.Value;
                        }
                        else if(registerBase.Address == 3007)
                        {
                            eaChargeMonitor.valAlarm = r.Value;
                        }
                        ModbusMaster.WriteMultipleRegisters(SlaveAddress, registerBase.Address, new ushort[] { r.Value });
                        break;
                }

                if (registerBase.Name == "BaudRate")
                {
                    _SerialPort.BaudRate = Converter.ToBaudRate((registerBase as IValue<ushort>).Value);
                    UpdateInfo();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Исключение: " + e.ToString());
                throw;
            }
            finally
            {
                _SerialPort.Close();
            }

        }

        public void ReadRegisters(RegisterBase registerBase)
        {
            try
            {
                _SerialPort.Open();
                ushort[] response = new ushort[registerBase.Length];
                
                if (registerBase.Address == 9800)        // EAChargeMonitor.Registers[index:13])
                {
                    uint[] txtresponse = new uint[5];
                    // testUnit.SetTextArray();                    
                    txtresponse = ModbusMaster.ReadHoldingRegisters32(SlaveAddress, registerBase.Address, registerBase.Length);
                    byte[] byteString = Converter.ConvertUintArrayToByteArray(txtresponse);
                    eaChargeMonitor.DeviceName = Encoding.Default.GetString(byteString);

                }
                else
                {
                    response = ModbusMaster.ReadHoldingRegisters(SlaveAddress, registerBase.Address, registerBase.Length);                    
                }    
              
              eaChargeMonitor.ParseResponse(registerBase,response);
            }
            catch (Exception e)
            {
                MessageBox.Show("Исключение: " + e.ToString());               
            }
            finally
            {
                _SerialPort.Close();
            }
        }

        public void ReadOneRegister32(RegisterBase registerBase, ref long value)
        {
            value = 0;
            try
            {
                _SerialPort.Open();
                uint[] response = { 0 };
                uint[] time = new uint[2];
                response = ModbusMaster.ReadHoldingRegisters32(SlaveAddress, registerBase.Address, registerBase.Length);
                                 
                time[0] = (uint)(response[0] & 0x0000FFFF) << 16;
                time[1] = (uint)(response[0] & 0xFFFF0000) >> 16;
                value = time[0] | time[1];
             }
            catch (Exception e)
            {
                MessageBox.Show("Исключение: " + e.ToString());
            }
            finally
            {
                _SerialPort.Close();
            }
        }
        public void WriteOneRegister32(RegisterBase registerBase, long value)
        {
            try
            {
                _SerialPort.Open();
               
                uint[] time = new uint[2];
                long temp = 0;
                time[0] = (uint)(value & 0xFFFF0000);
                temp = value & 0x0000FFFF;
                time[0]|= (uint)temp;

                ModbusMaster.WriteMultipleRegisters32(SlaveAddress, registerBase.Address, time);

            }
            catch (Exception e)
            {
                MessageBox.Show("Исключение: " + e.ToString());
            }
            finally
            {
                _SerialPort.Close();
            }
        }


        public void ReadAllMeasuringRegisters()
        {
            try
            {
                for (int i = 0; i < eaChargeMonitor.NumberOfMeasuringRegisters; i++)
                {
                    ReadRegisters(eaChargeMonitor.Registers[i]);
                    Thread.Sleep(50);
                }
            }
            catch (TimeoutException e)
            {
                MessageBox.Show("Исключение: " + e.ToString());
            }
            catch (SlaveException e)
            {
                MessageBox.Show("Исключение: " + e.ToString());
            }
        }

        public void ReadAllSettingRegisters()
        {
            try
            {
                for (int i = 7; i < eaChargeMonitor.Registers.Count; i++)
                {
                    ReadRegisters(eaChargeMonitor.Registers[i]);
                    Thread.Sleep(50);
                }
            }
            catch (TimeoutException e)
            {
                MessageBox.Show("Таймаут: " + e.ToString());
            }
            catch (SlaveException e)
            {
                MessageBox.Show("Исключение: " + e.ToString());
            }
        }

        public void Reset()
        {
            WriteRegisters(8003, new ushort[] { 0x7273 });
        }

        public void FactorySetting()
        {
            WriteRegisters(8003, new ushort[] { 0x6661 });
        }

        public bool FindDevice()
        {
            int baudRatesCount = 8;
            int paritiesCount = 4;
            bool isFound = false;
            SlaveAddress = 0x00;

            for (int i = baudRatesCount; i >= 0; i--)
            {
                try
                {
                    _SerialPort.BaudRate = Converter.ToBaudRate((ushort)(i + 1));
                    
                    for (int j = 0; j < paritiesCount; j++)
                    {
                        try
                        {
                            switch (j)
                            {
                                case 0:
                                    _SerialPort.Parity = Parity.None;
                                    _SerialPort.StopBits = StopBits.One;
                                    break;
                                case 1:
                                    _SerialPort.Parity = Parity.Odd;
                                    _SerialPort.StopBits = StopBits.One;
                                    break;
                                case 2:
                                    _SerialPort.Parity = Parity.Even;
                                    _SerialPort.StopBits = StopBits.One;
                                    break;
                                case 3:
                                    _SerialPort.Parity = Parity.None;
                                    _SerialPort.StopBits = StopBits.Two;
                                    break;
                            }

                            ReadRegisters(eaChargeMonitor.Registers[9]); // регистр адреса устройства

                            isFound = true;

                            break;
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("Исключение: " + e.ToString());

                        }
                    }

                    if (isFound) break;
                    else
                    {
                        _SerialPort.BaudRate = 115200;
                        _SerialPort.Parity = Parity.None;
                        _SerialPort.StopBits = StopBits.One;
                    }
                        
                }
                catch (Exception e)
                {
                    MessageBox.Show("Исключение: " + e.ToString());
                }
            }

            SlaveAddress = (byte)(eaChargeMonitor.Registers[9] as IValue<ushort>).Value;
            UpdateInfo();
            return isFound;
        }

        public void RunTestMotoCounter()
        {
            EAMotoCounter.TestElapsed();
        }

        public void ReadMotocounter()
        {
            ushort register;
            byte regIdx;
            long valTicks = 0;
            regIdx = eaChargeMonitor.GetBaseRegisterIndex("Motocounter");
            register = eaChargeMonitor.GetBaseAddress("Motocounter");
            ReadOneRegister32(eaChargeMonitor.Registers[regIdx],ref valTicks);
            DateTimeOffset dtOffset = DateTimeOffset.FromUnixTimeSeconds(valTicks);
            valTicks = dtOffset.Ticks;
            eaChargeMonitor.valMotocounter = valTicks;
            
            EAMotoCounter.SetTimeTicks(eaChargeMonitor.valMotocounter);
            EAMotoCounter.GetStrMotorCounter();
        }

        public void SaveMotocounter()
        {
            ushort register;
            byte regIdx;
            long valTicks = 0;
            regIdx = eaChargeMonitor.GetBaseRegisterIndex("Motocounter");
            register = eaChargeMonitor.GetBaseAddress("Motocounter");

            valTicks = ((DateTimeOffset)EAMotoCounter.originDT).ToUnixTimeSeconds();
            eaChargeMonitor.valMotocounter = valTicks;
            WriteOneRegister32(eaChargeMonitor.Registers[regIdx], eaChargeMonitor.valMotocounter);
        }

        public void StartPoll(int interval)
        {
            IsPoll = true;
            pollTask = new Task(() => Poll(interval));
            pollTask.Start();
        }

        public void StopPoll()
        {
            IsPoll = false;
        }

        private void Poll(int interval)
        {
            while (IsPoll)
            {
                for (int i = 0; i < eaChargeMonitor.NumberOfMeasuringRegisters && IsPoll; i++)
                {
                    ReadRegisters(eaChargeMonitor.Registers[i]);
                    ReadRegisters(eaChargeMonitor.Registers[16]);                    
                    Thread.Sleep(interval);
                }

            }

        }
    }
}
