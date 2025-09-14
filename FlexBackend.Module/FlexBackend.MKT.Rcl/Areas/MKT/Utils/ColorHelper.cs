using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexBackend.MKT.Rcl.Areas.MKT.Utils
{
    internal class ColorHelper
    {
        private static readonly Random rancolor = new Random();

        public static string RandomColor()
        {
            int r = rancolor.Next(0, 256);
            int g = rancolor.Next(0, 256);
            int b = rancolor.Next(0, 256);

            return $"#{r:X2}{g:X2}{b:X2}";
        }
    }
}
