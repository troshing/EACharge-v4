using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace EACharge_Out
{
    public class EAChargeMonitor : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        public String _textAlarm;
        public String _textMode;
        
        private System.Windows.Media.Brush _frgrAlarm;
        private System.Windows.Media.Brush _frgrMode;

        public System.Windows.Media.Brush ForegroundAlarm
        {
            get
            {
                return _frgrAlarm;
            }

            set
            {
                SetField(ref _frgrAlarm, value, "ForegroundAlarm");
            }
        }

        public System.Windows.Media.Brush ForegroundMode
        {
            get
            {
                return _frgrMode;
            }

            set
            {
                SetField(ref _frgrMode, value, "ForegroundMode");
            }
        }

        public String TextAlarm
        {
            get => _textAlarm;
            set
            {
                SetField(ref _textAlarm, value, "TextAlarm");
            }
        }
        public String TextMode
        {
            get => _textMode;
            set
            {
                SetField(ref _textMode, value, "TextMode");
            }
        }       

        public ushort valPreAlarm { get; set; }
        public ushort valAlarm { get; set; }

        private ushort valHyster {  get; set; }

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

        public ObservableCollection<RegisterBase> Registers { get; set; }

        public int NumberOfMeasuringRegisters { get; set; } = 7;
        public int NumberOfSettingRegisters { get; set; } = 9;


        public EAChargeMonitor()
        {
            Registers = new ObservableCollection<RegisterBase>()
            {
                new RegisterFloat(1000, 4, "Resistance"),
                new RegisterFloat(1008, 4, "Voltage"),
                new RegisterFloat(1012, 4,"Capacitance"),
                new RegisterFloat(1016, 4,"Voltage plus"),
                new RegisterFloat(1020, 4, "Voltage minus"),
                new RegisterFloat(1036, 4, "Resistance plus"),
                new RegisterFloat(1040, 4, "Resistance minus"),
                //
                new RegisterUshort(3005, "PreAlarm", 1),
                new RegisterUshort(3007, "Alarm", 1),
                new RegisterUshort(3015, "Device Address", 1),
                new RegisterUshort(3016, "BaudRate", 1),
                new RegisterUshort(3017, "Parity", 1),
                new RegisterUshort(3018, "Delay", 1),
                //new RegisterUshort(8003, "Factory Reset", 1),
                new RegisterString(9800, "Device Name", 1),
                new RegisterUshort(9820, "ID", 1),
                new RegisterUshort(9821, "Firmware Version", 1),
                new RegisterUshort(9822, "Operation Mode", 1),
            };
        }

        public ushort GetBaseAddress()
        {
            ushort tmpAddr = 0;

            return tmpAddr;

        }
        public void ParseResponse(RegisterBase register, ushort[] data)
        {
            switch (register)
            {
                case IValue<float> r:
                    float floatToWrite;
                    switch (register.Name)
                    {
                        case "Resistance":
                            float mFloat = Converter.ConvertTwoUInt16ToFloat(data) / 1000; // перевод в кОм из Ом
                            floatToWrite = mFloat;
                            ushort value = (ushort)mFloat;
                            
                            if ((value < valPreAlarm) && (value > valAlarm))
                            {                                
                                ForegroundAlarm = System.Windows.Media.Brushes.Orange;
                                TextAlarm = String.Format("ТРЕВОГА", ForegroundAlarm);                                                                                          
                            }
                            else if ((value <= valAlarm))
                            {
                                ForegroundAlarm = System.Windows.Media.Brushes.Red;
                                TextAlarm = String.Format("АВАРИЯ", ForegroundAlarm);
                            }                              
                            
                            else if (value > valPreAlarm)
                            {                                                                
                                ForegroundAlarm = System.Windows.Media.Brushes.Green;
                                TextAlarm = String.Format("НОРМА", ForegroundAlarm);                                
                            }
                                                          
                            break;
                        case "Resistance plus":
                        case "Resistance minus":
                            float myFloat = Converter.ConvertTwoUInt16ToFloat(data) / 1000; // перевод в кОм из Ом
                            floatToWrite = myFloat;
                            break;
                        case "Capacitance":
                            myFloat = Converter.ConvertTwoUInt16ToFloat(data) * 1000000; // перевод в мкФ из Ф
                            floatToWrite = myFloat;
                            break;
                        default:
                            myFloat = Converter.ConvertTwoUInt16ToFloat(data);
                            floatToWrite = myFloat;
                            break;
                    }
                    r.Value = floatToWrite;
                    break;
                case IValue<string> r:
                    byte[] byteString = Converter.ConvertUshortArrayToByteArray(data);
                    r.Value = Encoding.Default.GetString(byteString);
                    break;
                case IValue<ushort> r:
                    r.Value = data[0];
                    switch(register.Name)
                    {
                        case "PreAlarm":
                            valPreAlarm = r.Value;
                            break;
                        case "Alarm":
                            valAlarm = r.Value;
                            break;
                        case "Operation Mode":
                            if(r.Value == 1)
                            {
                                ForegroundMode = System.Windows.Media.Brushes.Red;
                                TextMode = String.Format("ТЕСТ", ForegroundMode);                                 
                            }
                            else if(r.Value == 0)
                            {
                                ForegroundMode = System.Windows.Media.Brushes.GreenYellow;
                                TextMode = String.Format("РАБОТА", ForegroundMode);
                            }
                            break;
                    }
                    break;
            }
        }
    }
}

//private ushort _preAlarm;
///// <summary>
///// Уставка - предварительная тревога (кОм)
///// </summary>
//public ushort PreAlarm
//{
//    get => _preAlarm;
//    set
//    {
//        _preAlarm = value;
//        OnPropertyChanged();
//    }
//}

//private ushort _alarm;
///// <summary>
///// Уставка - тревога (кОм)
///// </summary>
//public ushort Alarm
//{
//    get => _alarm;
//    set
//    {
//        _alarm = value;
//        OnPropertyChanged();
//    }
//}

//private ushort _deviceAddress;
///// <summary>
///// Modbus адрес устройства (10-101) default - 10
///// </summary>
//public ushort DeviceAddress
//{
//    get => _deviceAddress;
//    set
//    {
//        _deviceAddress = value;
//        OnPropertyChanged();
//    }
//}

//private ushort _baudRate;
///// <summary>
///// Скорость обмена 
///// </summary>
//public ushort BaudRate
//{
//    get => _baudRate;
//    set
//    {
//        _baudRate = value;
//        OnPropertyChanged();
//    }
//}

//private ushort _parity;
///// <summary>
///// Четность
///// </summary>
//public ushort Parity
//{
//    get => _parity;
//    set
//    {
//        _parity = value;
//        OnPropertyChanged();
//    }
//}

//private ushort _delay;
///// <summary>
///// Задержка
///// </summary>
//public ushort Delay
//{
//    get => _delay;
//    set
//    {
//        _delay = value;
//        OnPropertyChanged();
//    }
//}

//private ushort _factoryReset;
///// <summary>
///// Сброс установок в "фабричные параметры" (0х6661)
///// </summary>
//public ushort FactoryReset
//{
//    get => _factoryReset;
//    set
//    {
//        _factoryReset = value;
//        OnPropertyChanged();
//    }
//}

//private string _deviceName;
///// <summary>
///// Имя устройства
///// </summary>
//public string DeviceName
//{
//    get => _deviceName;
//    set
//    {
//        _deviceName = value;
//        OnPropertyChanged();
//    }
//}

//private ushort _identityNumber;
///// <summary>
///// Индетификационный номер
///// </summary>
//public ushort IdentityNumber
//{
//    get => _identityNumber;
//    set
//    {
//        _identityNumber = value;
//        OnPropertyChanged();
//    }
//}

//private ushort _firmwareVersion;
///// <summary>
///// Версия прошивки
///// </summary>
//public ushort FirmwareVersion
//{
//    get => _firmwareVersion;
//    set
//    {
//        _firmwareVersion = value;
//        OnPropertyChanged();
//    }
//}
