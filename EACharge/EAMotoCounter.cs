using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EACharge
{
    public class EAMotoCounter : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public long timeticks { get; set; }
        private int Days{ set; get; }
        private int Hours { set; get; }
        private int Minutes { set; get; }
        private int Seconds { set; get; }

        public String _motorCounter;
        public String TotalMotoCount
        {
            get => _motorCounter;
            set
            {
                SetField(ref _motorCounter, value, "TotalMotoCount");
            }
        }
        
        private String format = "В работе: {1}: дней,{2}: часов, {3}:минут, {4}: секунд";
    
        private TimeSpan elapsedSpan;

        public DateTime originDT = new DateTime(2025, 1, 1, 12, 0, 1, DateTimeKind.Utc);
        public DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        private long originTicks;
        private long _testTicks;
        private DateTime _testTime;
        private TimeSpan _testElapsed;
        private String _testMCounter;
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

        public EAMotoCounter()
        {            
            originTicks = originDT.Ticks;
            timeticks = 0;
            elapsedSpan = new TimeSpan();
            _motorCounter = "";
        }

        public void SetTestData()
        {
            _testTicks = TimeSpan.FromSeconds(1735732809).Ticks + epoch.Ticks;
            _testTime = new DateTime(_testTicks);
            _testElapsed = new TimeSpan(_testTicks - originTicks);
        }
        public void TestElapsed()
        {
            _testMCounter = String.Format(format, 0, _testElapsed.Days, _testElapsed.Hours, _testElapsed.Minutes, _testElapsed.Seconds);
            TotalMotoCount = String.Format(format, 0,_testElapsed.Days, _testElapsed.Hours, _testElapsed.Minutes, _testElapsed.Seconds);                                                
        }

        public void SetOriginDateTime(DateTime newDate)
        {
            originDT = newDate;
            originTicks = originDT.Ticks;
        }
        
        // Устанавливает тики соответствующие дате отсчета
        public void SetOriginTicks(ref long ticks)
        {
            ticks = originTicks;
        }

        public void SetTimeTicks(long ticks)
        {
            elapsedSpan = TimeSpan.FromTicks(ticks - originTicks);
        }

        public TimeSpan GetElapsedTime()
        {
            return elapsedSpan;
        }

        public void GetStrMotorCounter()
        {
            TotalMotoCount = String.Format(format, 0, elapsedSpan.Days, elapsedSpan.Hours, elapsedSpan.Minutes, elapsedSpan.Seconds);
        }

    }
}
