using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReencGUI
{
    public class Utils
    {
        public static ulong LengthToMS(int hours, int minutes, int seconds, int ms)
        {
            return (ulong)(ms + seconds * 1000 + minutes * 60 * 1000 + hours * 60 * 60 * 1000);
        }
    }
}
