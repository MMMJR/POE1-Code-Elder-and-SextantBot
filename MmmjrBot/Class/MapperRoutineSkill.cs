using System;
using JetBrains.Annotations;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MmmjrBot.Class
{
    public class MapperRoutineSkill : INotifyPropertyChanged
    {
        private Stopwatch delaySw = Stopwatch.StartNew();
        private string _slotName { get; set; }
        private string _slotIndex { get; set; }
        private bool _enabled { get; set; }
        private int _sktype { get; set; }
        private double _delay { get; set; }

        public MapperRoutineSkill()
        {
            
        }

        public MapperRoutineSkill(string slotName, string slotIndex, bool enabled, int sktype, double delay)
        {
            Enabled = enabled;
            SlotName = slotName;
            SlotIndex = slotIndex;
            Delay = delay;
            SkType = sktype;
        }
        public string SlotName
        {
            get { return _slotName; }
            set
            {
                _slotName = value;
                NotifyPropertyChanged(nameof(SlotName));
            }
        }
        public string SlotIndex
        {
            get { return _slotIndex; }
            set
            {
                _slotIndex = value;
                NotifyPropertyChanged(nameof(SlotIndex));
            }
        }
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                NotifyPropertyChanged(nameof(Enabled));
            }
        }
        public int SkType
        {
            get { return _sktype; }
            set
            {
                _sktype = value;
                NotifyPropertyChanged(nameof(SkType));
            }
        }
        public double Delay
        {
            get { return _delay; }
            set
            {
                _delay = Math.Round(value, 1, MidpointRounding.AwayFromZero);
                NotifyPropertyChanged(nameof(Delay));
            }
        }

        public bool IsReadyToCast
        {
            get
            {
                return  delaySw.ElapsedMilliseconds > Delay * 1000;
            }
        }
        public void Casted()
        {
            delaySw.Restart();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
