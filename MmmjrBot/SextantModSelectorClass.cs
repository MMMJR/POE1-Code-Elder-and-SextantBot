using DreamPoeBot.Loki.Game.GameData;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MmmjrBot
{
    public class StatGGG
    {
        private StatTypeGGG _stat;
        private int _value;

        public StatGGG() {
            _stat = StatTypeGGG.None;
            _value = 0;
        }

        public StatTypeGGG Stat { get { return _stat; } set { _stat = value; } }

        public int Value { get { return _value; } set { _value = value; } }
    }

    public class SextantModSelectorClass : INotifyPropertyChanged
    {
        private string _sextantName = "";
        private bool _enabled = false;
        private string _modDescA = "";
        private string _modDescB = "";
        private string _modDescC = "";
        private string _modDescD = "";

        private StatTypeGGG _stat1;
        private int _value1;
        private StatTypeGGG _stat2;
        private int _value2;
        private StatTypeGGG _stat3;
        private int _value3;
        private StatTypeGGG _stat4;
        private int _value4;
        private StatTypeGGG _stat5;
        private int _value5;

        public SextantModSelectorClass()
        {

        }

        public SextantModSelectorClass(string sextantName, bool enabled, string modDescA, string modDescB, string modDescC, string modDescD, StatTypeGGG modStat1 = StatTypeGGG.None, int value1 = 0, StatTypeGGG modStat2 = StatTypeGGG.None, int value2 = 0, StatTypeGGG modStat3 = StatTypeGGG.None, int value3 = 0, StatTypeGGG modStat4 = StatTypeGGG.None, int value4 = 0, StatTypeGGG modStat5 = StatTypeGGG.None, int value5 = 0)
        {
            SextantName = sextantName;
            Enabled = enabled;
            ModDescA = modDescA;
            ModDescB = modDescB;
            ModDescC = modDescC;
            ModDescD = modDescD;
            ModStatGGG1 = modStat1;
            Value1 = value1;
            ModStatGGG2 = modStat2;
            Value2 = value2;
            ModStatGGG3 = modStat3;
            Value3 = value3;
            ModStatGGG4 = modStat4;
            Value4 = value4;
            ModStatGGG5 = modStat5;
            Value5 = value5;
        }
        public string SextantName
        {
            get { return _sextantName; }
            set
            {
                _sextantName = value;
                NotifyPropertyChanged(nameof(SextantName));
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
        public string ModDescA
        {
            get { return _modDescA; }
            set
            {
                _modDescA = value;
                NotifyPropertyChanged(nameof(ModDescA));
            }
        }
        public string ModDescB
        {
            get { return _modDescB; }
            set
            {
                _modDescB = value;
                NotifyPropertyChanged(nameof(ModDescB));
            }
        }
        public string ModDescC
        {
            get { return _modDescC; }
            set
            {
                _modDescC = value;
                NotifyPropertyChanged(nameof(ModDescC));
            }
        }
        public string ModDescD
        {
            get { return _modDescD; }
            set
            {
                _modDescD = value;
                NotifyPropertyChanged(nameof(ModDescD));
            }
        }

        public StatTypeGGG ModStatGGG1
        {
            get { return _stat1; }
            set
            {
                _stat1 = value;
            }
        }

        public int Value1
        {
            get { return _value1; }
            set
            {
                _value1 = value;
            }
        }
        public StatTypeGGG ModStatGGG2
        {
            get { return _stat2; }
            set
            {
                _stat2 = value;
            }
        }

        public int Value2
        {
            get { return _value2; }
            set
            {
                _value2 = value;
            }
        }
        public StatTypeGGG ModStatGGG3
        {
            get { return _stat3; }
            set
            {
                _stat3 = value;
            }
        }

        public int Value3
        {
            get { return _value3; }
            set
            {
                _value3 = value;
            }
        }
        public StatTypeGGG ModStatGGG4
        {
            get { return _stat4; }
            set
            {
                _stat4 = value;
            }
        }

        public int Value4
        {
            get { return _value4; }
            set
            {
                _value4 = value;
            }
        }
        public StatTypeGGG ModStatGGG5
        {
            get { return _stat5; }
            set
            {
                _stat5 = value;
            }
        }

        public int Value5
        {
            get { return _value5; }
            set
            {
                _value5 = value;
            }
        }

        public List<StatGGG> GetAllModsBySextantVoidstoneRow()
        {
            List<StatGGG> result = new List<StatGGG>();

            if(ModStatGGG1 != StatTypeGGG.None)
            {
                StatGGG data = new StatGGG();
                data.Stat = ModStatGGG1;
                data.Value = Value1;
                result.Add(data);
            }
            if (ModStatGGG2 != StatTypeGGG.None)
            {
                StatGGG data = new StatGGG();
                data.Stat = ModStatGGG2;
                data.Value = Value2;
                result.Add(data);
            }
            if (ModStatGGG3 != StatTypeGGG.None)
            {
                StatGGG data = new StatGGG();
                data.Stat = ModStatGGG3;
                data.Value = Value3;
                result.Add(data);
            }
            if (ModStatGGG4 != StatTypeGGG.None)
            {
                StatGGG data = new StatGGG();
                data.Stat = ModStatGGG4;
                data.Value = Value4;
                result.Add(data);
            }
            if (ModStatGGG5 != StatTypeGGG.None)
            {
                StatGGG data = new StatGGG();
                data.Stat = ModStatGGG5;
                data.Value = Value5;
                result.Add(data);
            }

            return result;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
