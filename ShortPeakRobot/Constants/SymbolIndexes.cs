using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binance.Infrastructure.Constants
{
    public static class SymbolIndexes
    {
        public static Dictionary<string, int> price = new Dictionary<string, int>() {
        { "ETHUSDT",2},
        { "BTCUSDT",1},
        { "BNBUSDT",2},
        { "SOLUSDT",3},
        { "XRPUSDT",4},
        { "ADAUSDT",4},
        { "DOGEUSDT",5},
        { "LTCUSDT",2},
        { "CHZUSDT",5},
        { "LINKUSDT",3},
        { "EOSUSDT",3},
       { "TRXUSDT",5},
       { "MATICUSDT",4},
       { "AVAXUSDT",3},
       { "DOTUSDT",3},

        };

        public static Dictionary<string, int> lot = new Dictionary<string, int>() {
        { "ETHUSDT",3},
        { "BTCUSDT",3},
        { "BNBUSDT",2},
        { "SOLUSDT",0},
        { "XRPUSDT",1},
        { "ADAUSDT",0},
        { "DOGEUSDT",0},
        { "LTCUSDT",2},
        { "CHZUSDT",0},
        { "LINKUSDT",2},
        { "EOSUSDT",1},
        { "TRXUSDT",0},
        { "MATICUSDT",0},
        { "AVAXUSDT",0},
        { "DOTUSDT",1},
        };

        //public const int Lot = 1000;
    }
}
