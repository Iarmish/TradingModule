using ShortPeakRobot.Data;
using ShortPeakRobot.Market.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.ViewModel
{
    internal class SymbolVM
    {
        public static ObservableCollection<Symbol> symbols
        {
            get; set;
        } = new ObservableCollection<Symbol>();


        public SymbolVM()
        {

        }
    }
}
