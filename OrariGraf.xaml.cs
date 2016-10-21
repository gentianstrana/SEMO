using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Oraret
{
    /// <summary>
    /// Interaction logic for OrariGraf.xaml
    /// </summary>
    public partial class OrariGraf : UserControl
    {
        private List<Tuple<double, double>> perqindjet;

        private TranslateTransform transform;

        public OrariGraf()
        {
            this.InitializeComponent();
            transform = ((TranslateTransform)KohaPopup.RenderTransform);
        }

        public void MbushGrafin(IEnumerable<Interval> intervalet)
        {
            Grafi.Children.Clear();
            perqindjet = new List<Tuple<double, double>>();
            foreach (var interval in intervalet)
            {
                var koha1 = interval.KohaFillimit.Ora * 60 + interval.KohaFillimit.Minuti;
                var koha2 = interval.KohaPerfundimit.Ora * 60 + interval.KohaPerfundimit.Minuti;
                var diferenca = koha2 - koha1;
                if (diferenca <= 0)
                {
                    continue;
                }

                if (koha2 <= 420 || koha1 >= 1200)
                {
                    continue;
                }

                if (koha1 < 420)
                {
                    koha1 = 420;
                }

                if (koha2 > 1200)
                {
                    koha2 = 1200;
                }

                diferenca = koha2 - koha1;
                var perqindja = new Tuple<double, double>((koha1 - 420) / 780d, diferenca / 780d);
                perqindjet.Add(perqindja);
                var rect = new Rectangle
                {
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Fill = Brushes.Firebrick,
                    Margin = new Thickness(Grafi.ActualWidth * perqindja.Item1, 0d, 0d, 0d),
                    Width = Grafi.ActualWidth * perqindja.Item2,
                    Height = Grafi.ActualHeight,
                    Tag = new Tuple<string, string>(interval.Tag, interval.AltTag),
                    StrokeThickness = 0d
                };

                rect.MouseEnter += (s, e) => rect.Fill = Brushes.Red;
                rect.MouseLeave += (s, e) => rect.Fill = Brushes.Firebrick;
                rect.MouseMove += Orari_MouseMove;
                Grafi.Children.Add(rect);
            }
        }

        public void Zbraz()
        {
            Grafi.Children.Clear();
            perqindjet = null;
        }

        public void Refresh()
        {
            if (KohaPopup.Visibility != Visibility.Visible)
            {
                return;
            }

            var tag = KohaPopupText.Tag as Tuple<string, string>;

            if (tag == null)
            {
                return;
            }

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                KohaPopupText.Text = tag.Item2;
            }
            else
            {
                KohaPopupText.Text = tag.Item1;
            }

            Dispatcher.Invoke(new Action(() => { }), DispatcherPriority.ContextIdle, null);
            var position = Mouse.GetPosition(Grafi);
            if (position.X + KohaPopup.ActualWidth > Grafi.ActualWidth + 40d)
            {
                transform.X = position.X - KohaPopup.ActualWidth;
            }
            else
            {
                transform.X = position.X;
            }
        }

        private void VendosDrejtkendeshit()
        {
            if (perqindjet == null) return;
            for (int i = 0; i < perqindjet.Count; i++)
            {
                var perqindja = perqindjet[i];
                var rect = (Rectangle)Grafi.Children[i];
                rect.Margin = new Thickness(Grafi.ActualWidth * perqindja.Item1, 0d, 0d, 0d);
                rect.Width = Grafi.ActualWidth * perqindja.Item2;
                rect.Height = Grafi.ActualHeight;
            }
        }

        private void Grafi_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            VendosDrejtkendeshit();
        }

        private void Grafi_MouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(Grafi);
            KohaPopupText.Text = PerqindjeNeOre(position.X / Grafi.ActualWidth);
            KohaPopupText.Tag = null;
            transform.X = position.X;
            transform.Y = position.Y - KohaPopup.ActualHeight;
        }

        private void Orari_MouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(Grafi);
            var tag = ((FrameworkElement)sender).Tag as Tuple<string, string>;
            if (tag != null)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    KohaPopupText.Text = tag.Item2;
                }
                else
                {
                    KohaPopupText.Text = tag.Item1;
                }

                KohaPopupText.Tag = tag;

                if (position.X + KohaPopup.ActualWidth > Grafi.ActualWidth + 40d)
                {
                    transform.X = position.X - KohaPopup.ActualWidth;
                }
                else
                {
                    transform.X = position.X;
                }

                transform.Y = position.Y - KohaPopup.ActualHeight;
                e.Handled = true;
            }
        }

        private string PerqindjeNeOre(double perqindja)
        {
            if (perqindja >= 1d)
            {
                return "20:00";
            }
            else if (perqindja <= 0d)
            {
                return "07:00";
            }

            var totalMinuta = (int)Math.Round(perqindja * 780d);
            var ora = 7 + totalMinuta / 60;
            var minuti = totalMinuta % 60;
            return FixInt(ora) + ":" + FixInt(minuti);
        }

        private string FixInt(int vlera)
        {
            if (vlera < 10)
            {
                return "0" + vlera;
            }
            else return vlera.ToString();
        }
    }
}