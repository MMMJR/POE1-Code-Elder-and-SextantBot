using System;
using JetBrains.Annotations;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MmmjrBot
{
    public class OTestClass : INotifyPropertyChanged
    {
        private Stopwatch delaySw = Stopwatch.StartNew();
        private string _sextantName { get; set; }
        private bool _enabled { get; set; }
        private string _modDescA { get; set; }
        private string _modDescB { get; set; }
        private string _modDescC { get; set; }
        private string _modDescD { get; set; }

        public OTestClass()
        {

        }

        public OTestClass(string sextantName, bool enabled, string modDescA, string modDescB, string modDescC, string modDescD)
        {
            SextantName = sextantName;
            Enabled = enabled;
            ModDescA = modDescA;
            ModDescB = modDescB;
            ModDescC = modDescC;
            ModDescD = modDescD;
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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
