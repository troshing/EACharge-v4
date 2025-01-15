﻿using System;
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

namespace EACharge_Out
{
    public class Master : INotifyPropertyChanged
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

        public EAChargeMonitor EAChargeMonitor { get; set; }
        private SerialPortAdapter serialPortAdapter;

        IModbusMaster ModbusMaster { get; set; }

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

        public Master()
        {
            _SerialPort = new SerialPort(GetPortName(), 115200, Parity.None, 8, StopBits.One);
            _SerialPort.ReadTimeout = 2000;
            _SerialPort.WriteTimeout = 1000;
            SlaveAddress = 0x0A;                // по умолчанию "10" 
            serialPortAdapter = new SerialPortAdapter(_SerialPort);
            factory = new ModbusFactory();
            
            ModbusMaster = factory.CreateRtuMaster(serialPortAdapter);
            ModbusMaster.Transport.WriteTimeout = 2000;
            ModbusMaster.Transport.ReadTimeout = 2000;
            ModbusMaster.Transport.Retries = 1;
            ModbusMaster.Transport.WaitToRetryMilliseconds = 300;
            EAChargeMonitor = new EAChargeMonitor();
            UpdateInfo();
        }

        public void GetChargeMonitor(ref EAChargeMonitor monitor)
        {
            monitor = EAChargeMonitor;
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
                            EAChargeMonitor.valPreAlarm = r.Value;
                        }
                        else if(registerBase.Address == 3007)
                        {
                            EAChargeMonitor.valAlarm = r.Value;
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
                ushort[] response = { 0};
                if (registerBase.Address == 9800)        // EAChargeMonitor.Registers[index:13])
                {
                    byte[] txtresponse = new byte[20];
                    // вызвать ModbusMaster.ReadFileRecord
                    ModbusMaster.ReadFileRecord(SlaveAddress, 0x01,0x01, txtresponse);
                }
                else
                {
                    response = ModbusMaster.ReadHoldingRegisters(SlaveAddress, registerBase.Address, registerBase.Length);
                }    
                
             EAChargeMonitor.ParseResponse(registerBase, response);
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
                for (int i = 0; i < EAChargeMonitor.NumberOfMeasuringRegisters; i++)
                {
                    ReadRegisters(EAChargeMonitor.Registers[i]);
                    Thread.Sleep(50);
                }
            }
            catch (TimeoutException e)
            {
                throw;
            }
            catch (SlaveException e)
            {
                throw;
            }
        }

        public void ReadAllSettingRegisters()
        {
            try
            {
                for (int i = 7; i < EAChargeMonitor.Registers.Count; i++)
                {
                    ReadRegisters(EAChargeMonitor.Registers[i]);
                    Thread.Sleep(50);
                }
            }
            catch (TimeoutException e)
            {
                //throw;
            }
            catch (SlaveException e)
            {
                throw;
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

                            ReadRegisters(EAChargeMonitor.Registers[9]); // регистр адреса устройства

                            isFound = true;

                            break;
                        }
                        catch (Exception e)
                        {

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

                }
            }

            SlaveAddress = (byte)(EAChargeMonitor.Registers[9] as IValue<ushort>).Value;
            UpdateInfo();
            return isFound;
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
                for (int i = 0; i < EAChargeMonitor.NumberOfMeasuringRegisters && IsPoll; i++)
                {
                    ReadRegisters(EAChargeMonitor.Registers[i]);
                    ReadRegisters(EAChargeMonitor.Registers[16]);                    
                    Thread.Sleep(interval);
                }

            }

        }
    }
}
