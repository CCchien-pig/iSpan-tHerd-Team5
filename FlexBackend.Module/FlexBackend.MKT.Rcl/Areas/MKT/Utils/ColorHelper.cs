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
            int r = rancolor.Next(0, 255);
            int g = rancolor.Next(0, 255);
            int b = rancolor.Next(0, 255);

            return $"#{r:X2}{g:X2}{b:X2}";
        }
    }
}
