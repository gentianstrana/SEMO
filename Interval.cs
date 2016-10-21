using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Oraret.Domeni;

namespace Oraret
{
    public class Interval
    {
        public Interval() { }

        public Interval(Koha kohaFillimit, Koha kohaPerfundimit, string tag, string altTag)
        {
            KohaFillimit = kohaFillimit;
            KohaPerfundimit = kohaPerfundimit;
            Tag = tag;
            AltTag = altTag;
        }

        public Koha KohaFillimit { get; set; }

        public Koha KohaPerfundimit { get; set; }

        public string Tag { get; set; }

        public string AltTag { get; set; }
    }
}