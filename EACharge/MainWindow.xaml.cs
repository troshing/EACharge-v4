using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Drawing;
using NModbus;
using NModbus.Message;

namespace EACharge
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool IsUIBlocked { get; set; }

        EAMaster master;

        object selectedRWControl;

        Dictionary<object, RegisterBase> tableOfRegisters;

        List<object> rwControls; // элементы управления через которые записываются / считываются данные

        public MainWindow()
        {
            InitializeComponent();
            master = new EAMaster();

            this.DataContext = master.eaChargeMonitor;
            txtblckStatus.DataContext = master;
            txtAlarm.DataContext = master.eaChargeMonitor;
            txtMode.DataContext = master.eaChargeMonitor;
            txtbxMotoCounter.DataContext = master.EAMotoCounter;
            
            GetAllRWControls();
            GetTableOfRegisters();

            if (!master.ArePortsExist())
            {
                BlockUI();                
                master.UpdateInfo();
                MessageBox.Show("В системе не обнаружены COM порты", "Ошибка");
            }
        }

        public void BlockUI()
        {
            foreach (var child in Measure.Children)
            {
                if (!(child is TextBlock)) (child as UIElement).IsEnabled = false;
            }

            foreach (var child in Options.Children)
            {
                if (!(child is TextBlock)) (child as UIElement).IsEnabled = false;
            }
            IsUIBlocked = true;
        }

        public void UnblockUI()
        {
            foreach (var child in Measure.Children)
            {
                if (!(child is TextBlock)) (child as UIElement).IsEnabled = true;
            }

            foreach (var child in Options.Children)
            {
                if (!(child is TextBlock)) (child as UIElement).IsEnabled = true;
            }
            IsUIBlocked = false;
        }

        public void GetAllRWControls()
        {
            rwControls = new List<object>();

            foreach (var child in Measure.Children)
            {
                if (child is TextBox) rwControls.Add(child as TextBox);
            }

            foreach (var child in Options.Children)
            {
                if (child is TextBox) rwControls.Add(child as TextBox);
                else if (child is ComboBox) rwControls.Add(child as ComboBox);
            }

        }
        public void GetTableOfRegisters()
        {
            tableOfRegisters = new Dictionary<object, RegisterBase>
            {
                {txtbxResistance, master.eaChargeMonitor.Registers[0] },
                {txtbxVoltage, master.eaChargeMonitor.Registers[1] },
                {txtbxCapacitance, master.eaChargeMonitor.Registers[2] },
                {txtbxVoltagePlus, master.eaChargeMonitor.Registers[3] },
                {txtbxVoltageMinus, master.eaChargeMonitor.Registers[4] },
                {txtbxResistancePlus, master.eaChargeMonitor.Registers[5] },
                {txtbxResistanceMinus, master.eaChargeMonitor.Registers[6] },
                {txtbxPreAlarm, master.eaChargeMonitor.Registers[7] },
                {txtbxAlarm, master.eaChargeMonitor.Registers[8] },
                {txtbxAddress, master.eaChargeMonitor.Registers[9] },
                {cmbbxBaudRate, master.eaChargeMonitor.Registers[10] },
                {cmbbxParity, master.eaChargeMonitor.Registers[11] },
                {txtbxDelay, master.eaChargeMonitor.Registers[12] },
                {txtbxDeviceName, master.eaChargeMonitor.Registers[13] },
                {txtbxID, master.eaChargeMonitor.Registers[14] },
                {txtbxFirmwareVersion, master.eaChargeMonitor.Registers[15] }
            };
        }

        private void LastFocusedTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            selectedRWControl = sender;
        }

        private void BtnReadAllSettingRegisters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                master.ReadAllSettingRegisters();
            }
            catch (TimeoutException ex)
            {
                ex.ToString();
                MessageBox.Show("Время ожидания истекло");
            }
            catch (SlaveException ex)
            {
                ex.ToString();
                MessageBox.Show("Ошибка чтения");
            }
            catch (UnauthorizedAccessException ex)
            {
                ex.ToString();
                MessageBox.Show("Выбранный порт занят", "Ошибка");
            }
            catch (System.IO.IOException ex)
            {
                ex.ToString();
                MessageBox.Show("Порт с указанным именем не существует", "Ошибка");
                if (!master.ArePortsExist())
                {
                    MessageBox.Show("В системе нет доступных COM портов", "Ошибка");
                    BlockUI();
                }
            }
        }

        private void BtnReadOneSettingRegister_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (selectedRWControl is TextBox)
                {
                    master.ReadRegisters(tableOfRegisters[selectedRWControl as TextBox]);
                }
                else if (selectedRWControl is ComboBox)
                {
                    master.ReadRegisters(tableOfRegisters[selectedRWControl as ComboBox]);
                }
            }
            catch (TimeoutException ex)
            {
                ex.ToString();
                MessageBox.Show("Время ожидания истекло");
            }
            catch (SlaveException ex)
            {
                ex.ToString();
                MessageBox.Show("Ошибка чтения");
            }
            catch (UnauthorizedAccessException ex)
            {
                ex.ToString();
                MessageBox.Show("Выбранный порт занят", "Ошибка");
            }
            catch (System.IO.IOException ex)
            {
                ex.ToString();
                MessageBox.Show("Порт с указанным именем не существует", "Ошибка");
                if (!master.ArePortsExist())
                {
                    MessageBox.Show("В системе нет доступных COM портов", "Ошибка");
                    BlockUI();
                }
            }
        }

        private void BtnWriteOneSettingRegister_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (selectedRWControl is TextBox)
                    master.WriteRegisters(tableOfRegisters[selectedRWControl as TextBox]);
                else if (selectedRWControl is ComboBox)
                    master.WriteRegisters(tableOfRegisters[selectedRWControl as ComboBox]);
            }
            catch (TimeoutException ex)
            {
                ex.ToString();
                MessageBox.Show("Время ожидания истекло.");
            }
            catch (SlaveException ex)
            {
                ex.ToString();
                MessageBox.Show("Ошибка записи", "Ошибка");
            }
            catch (UnauthorizedAccessException ex)
            {
                ex.ToString();
                MessageBox.Show("Выбранный порт занят", "Ошибка");
            }
            catch (System.IO.IOException ex)
            {
                ex.ToString();
                MessageBox.Show("Порт с указанным именем не существует", "Ошибка");
                if (!master.ArePortsExist())
                {
                    MessageBox.Show("В системе нет доступных COM портов", "Ошибка");
                    BlockUI();
                }
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            master.Reset();
        }

        private void BtnReadAllMeasuringRegisters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                master.ReadAllMeasuringRegisters();
            }
            catch (TimeoutException ex)
            {
                ex.ToString();
                MessageBox.Show("Время ожидания истекло", "Таймаут");
            }
            catch ( SlaveException ex)
            {
                ex.ToString();
                MessageBox.Show("Ошибка чтения", "Ошибка");
            }
            catch (UnauthorizedAccessException ex)
            {
                ex.ToString();
                MessageBox.Show("Выбранный порт занят", "Ошибка");
            }
            catch (System.IO.IOException ex)
            {
                ex.ToString();
                MessageBox.Show("Порт с указанным именем не существует", "Ошибка");
                if (!master.ArePortsExist())
                {
                    MessageBox.Show("В системе нет доступных COM портов", "Ошибка");
                    BlockUI();
                }
            }
        }

        private void BtnReadOneMeasuringRegister_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (tableOfRegisters.ContainsKey(selectedRWControl as TextBox))
                {
                    master.ReadRegisters(tableOfRegisters[selectedRWControl as TextBox]);
                }
            }
            catch (TimeoutException ex)
            {
                ex.ToString();
                MessageBox.Show("Время ожидания истекло");
            }
            catch (SlaveException ex)
            {
                ex.ToString();
                MessageBox.Show("Ошибка чтения");
            }
            catch (UnauthorizedAccessException ex)
            {
                ex.ToString();
                MessageBox.Show("Выбранный порт занят", "Ошибка");
            }
            catch (System.IO.IOException ex)
            {
                ex.ToString();
                MessageBox.Show("Порт с указанным именем не существует" + ex.Message, "Ошибка");
                if (!master.ArePortsExist())
                {
                    MessageBox.Show("В системе нет доступных COM портов", "Ошибка");
                    BlockUI();
                }
            }
        }

        private void CmbbxBaudRate_LostFocus(object sender, RoutedEventArgs e)
        {
            selectedRWControl = sender;
        }

        private void CmbbxParity_LostFocus(object sender, RoutedEventArgs e)
        {
            selectedRWControl = sender;
        }

        private void MenuItemSettings_Click(object sender, RoutedEventArgs e)
        {
            ConnectionSettingsWindow connectionSettingsWindow = new ConnectionSettingsWindow(master);
            connectionSettingsWindow.ShowDialog();
            if (IsUIBlocked && master.ArePortsExist() && master._SerialPort.PortName != " ")
            {
                UnblockUI();
            }
        }

        private async void BtnSearchOfDevice_Click(object sender, RoutedEventArgs e)
        {
            WaitingWindow ww = new WaitingWindow();
            ww.Show();
            if (await Task.Run(() => master.FindDevice()))
            {
                ww.Close();
                MessageBox.Show("Устройство найдено. Параметры устройства выведены на информационную панель.", "Поиск устройства");
            }
            else
            {
                ww.Close();
                MessageBox.Show("Устройство не найдено", "Поиск устройства");
            }

        }
        private void BtnFactorySetting_Click(object sender, RoutedEventArgs e)
        {
            master.FactorySetting();
        }

        private void BtnStartStopPoll_Click(object sender, RoutedEventArgs e)
        {
            if (master.IsPoll)
            {
                master.StopPoll();
                btnReadAllMeasuringRegisters.IsEnabled = true;
                btnReadOneMeasuringRegister.IsEnabled = true;
                numUpDownInterval.IsEnabled = true;
                btnStartStopPoll.Content = "Начать опрос";
            }
            else
            {
                try
                {
                    master.ReadAllSettingRegisters();
                }
                catch (TimeoutException ex)
                {
                    MessageBox.Show("Время ожидания истекло" + ex.Message);
                }
                master.StartPoll(Convert.ToInt32(numUpDownInterval.Text));
                btnReadAllMeasuringRegisters.IsEnabled = false;
                btnReadOneMeasuringRegister.IsEnabled = false;
                numUpDownInterval.IsEnabled = false;

                btnStartStopPoll.Content = "Остановить опрос";

            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);

            // TODO проверка больше 255
        }

        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            if (!master.IsPoll) { master.StopPoll(); }
            this.Close();
        }

        private void btnTestMotoCounter_Click(object sender, RoutedEventArgs e)
        {
            // Запуск Теста МотоЧасов       
            master.RunTestMotoCounter();
        }

        private void btnReadMotoCounter_Click(object sender, RoutedEventArgs e)
        {
            // Считывание Мото часов
            master.ReadMotocounter();
        }

        private void btnSaveMotoCounter_Click(object sender, RoutedEventArgs e)
        {
            // Запись Мото часов(начальное время) во вн флэш память 
            master.SaveMotocounter();
        }

        private void BtnGetProtectLevel_Click(object sender, RoutedEventArgs e)
        {
            // Получить уровень защиты 
        }

        private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            // Открыть файл прошивки
        }

        private void BtnReloadBoot_Click(object sender, RoutedEventArgs e)
        {
            // Перезагрузка контроллера
        }

        private void BtnSendProgramm_Click(object sender, RoutedEventArgs e)
        {
            // Начать программирование контроллера
        }
    }
}
