using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing.Charts;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using Oraret.Domeni;
using Oraret.Domeni.Sherbimet;

namespace Oraret
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private readonly Dictionary<string, OrariGraf> grafetSalla;
        private readonly Dictionary<string, OrariGraf> grafetProfesora;

        private string connectionString;

        private Databaza databaza;

        private readonly ConnectionStringFactory conn = new ConnectionStringFactory();

        public MainWindow()
        {
            InitializeComponent();
            grafetSalla = new Dictionary<string, OrariGraf>
            {
                {"Hënë", OrariGrafHene},
                {"Martë", OrariGrafMarte},
                {"Mërkurë", OrariGrafMerkure},
                {"Enjte", OrariGrafEnjte},
                {"Premte", OrariGrafPremte},
                {"Shtunë", OrariGrafShtune},
                {"Diel", OrariGrafDiel}
            };
            grafetProfesora = new Dictionary<string, OrariGraf>
            {
                {"Hënë", ProfesorOrariGrafHene},
                {"Martë", ProfesorOrariGrafMarte},
                {"Mërkurë", ProfesorOrariGrafMerkure},
                {"Enjte", ProfesorOrariGrafEnjte},
                {"Premte", ProfesorOrariGrafPremte},
                {"Shtunë", ProfesorOrariGrafShtune},
                {"Diel", ProfesorOrariGrafDiel}
            };
        }

        private void MenuExcel_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog { Filter = "Excel Fajlla|*.xlsx" };
            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            var file = openFileDialog.FileName;
            IEnumerable<Lenda> lendet;
            try
            {
                var lexuesi = new AngazhimetLexues();
                var excelLexuesi = new ClosedXmlLexues(file);
                lendet = lexuesi.MerrLendet(excelLexuesi);
            }
            catch
            {
                MessageBox.Show("Pati nje gabim gjate leximit te dhenave.");
                return;
            }

            MessageBox.Show("U lexua me sukses. Zgjedheni path ku do te ruhet databaza.");
            var saveFileDialog = new SaveFileDialog { Filter = "Databazë|*.db" };
            if (saveFileDialog.ShowDialog() == true)
            {
                var path = saveFileDialog.FileName;
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                using (var databaza = new Databaza(connectionString = conn.Merr(path)))
                {
                    databaza.Database.Create();
                    databaza.Lendet.AddRange(lendet);
                    databaza.SaveChanges();
                    MbushDataGrid(databaza);
                }
            }
        }

        private void MenuDatabaze_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog { Filter = "Databazë|*.db" };
            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            var path = openFileDialog.FileName;
            using (var databaza = new Databaza(connectionString = conn.Merr(path)))
            {
                MbushDataGrid(databaza);
            }
        }

        private void MenuWord_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return;
            }

            var saveFileDialog = new SaveFileDialog { Filter = "Word|*.doc" };
            if (saveFileDialog.ShowDialog() == true)
            {
                var wordRuajtesi = new WordRuajtes();
                var path = saveFileDialog.FileName;
                using (var databaza = new Databaza(connectionString))
                {
                    wordRuajtesi.RuajNeWord(databaza, path, ComboBoxSemestrat.SelectedIndex);
                }
            }
        }

        private void MenuOrar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return;
            }

            using (var databaza = new Databaza(connectionString))
            {
                var lendetPaOrare = Queryable.Where(
                    databaza.Lendet, l => l.Semestri % 2 == ComboBoxSemestrat.SelectedIndex
                        && l.Angazhimet.Any(a => a.Oraret.Any(o => o.Dukshem && (!o.KohaCaktuar || o.Dita == null || o.Dita == "" || o.Salla == null || o.Salla == "")))).Select(l => new { l.Emri })
                        .ToArray();

                DataGridLendetPaOrar.ItemsSource = lendetPaOrare;
            }

            LendetPaOrarFlyout.IsOpen = true;
        }

        private void MenuSallat_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return;
            }

            using (var databaza = new Databaza(connectionString))
            {
                var sallat = databaza.Oraret
                    .Where(o => o.Salla != null && o.Salla != "")
                    .Select(o => o.Salla)
                    .Distinct()
                    .ToArray();

                ComboBoxSallat.ItemsSource = sallat;
            }

            ComboBoxSallat_SelectionChanged(null, null);
            OraretSallaveFlyout.IsOpen = true;
        }

        private void ComboBoxSallat_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            foreach (var kvp in grafetSalla)
            {
                kvp.Value.Zbraz();
            }

            if (ComboBoxSallat.SelectedIndex == -1)
            {
                return;
            }

            var salla = (string)ComboBoxSallat.SelectedItem;
            using (var databaza = new Databaza(connectionString))
            {
                var oraretSalles = Queryable
                    .Where(databaza.Oraret, o => o.Salla == salla && o.Angazhimi.Lenda.Semestri % 2 == ComboBoxSemestrat.SelectedIndex)
                    .GroupBy(o => o.Dita)
                    .Where(g => g.Key != null);

                foreach (var dita in oraretSalles)
                {
                    var intervalet = dita
                        .Where(o => o.EshtePercaktuar())
                        .Select(o => new Interval(o.KohaFillimit, o.KohaMbarimit,
                            o.Angazhimi.Lenda.Emri + " (" + o.KohaString + ")",
                            o.Angazhimi.Profesori.Emri + " (" + o.KohaString + ")"));

                    if (grafetSalla.ContainsKey(dita.Key))
                    {
                        grafetSalla[dita.Key].MbushGrafin(intervalet);
                    }
                }
            }
        }

        private void MenuProfesoret_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return;
            }

            using (var databaza = new Databaza(connectionString))
            {
                var profesoret = databaza.Profesoret.Select(p => new { p.Id, p.Emri }).ToArray();
                ComboBoxProfesoret.SelectedValuePath = "Id";
                ComboBoxProfesoret.DisplayMemberPath = "Emri";
                ComboBoxProfesoret.ItemsSource = profesoret;
            }

            ComboBoxProfesoret_SelectionChanged(null, null);
            OraretProfesoreveFlyout.IsOpen = true;
        }

        private void ComboBoxProfesoret_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            foreach (var kvp in grafetProfesora)
            {
                kvp.Value.Zbraz();
            }

            if (ComboBoxProfesoret.SelectedIndex == -1)
            {
                return;
            }

            var profesoriId = (int)ComboBoxProfesoret.SelectedValue;
            using (var databaza = new Databaza(connectionString))
            {
                var profesori = databaza.Profesoret.Find(profesoriId);
                var oraretProfesorit = profesori.Angazhimet
                    .Where(a => a.Lenda.Semestri % 2 == ComboBoxSemestrat.SelectedIndex)
                    .SelectMany(a => a.Oraret)
                    .Where(o => o.EshtePercaktuar())
                    .GroupBy(o => o.Dita)
                    .Where(g => g.Key != null);

                foreach (var dita in oraretProfesorit)
                {
                    var intervalet = dita
                        .Select(o => new Interval(o.KohaFillimit, o.KohaMbarimit,
                            o.Angazhimi.Lenda.Emri + " (" + o.KohaString + ")",
                            o.Salla + " (" + o.KohaString + ")"));

                    if (grafetProfesora.ContainsKey(dita.Key))
                    {
                        grafetProfesora[dita.Key].MbushGrafin(intervalet);
                    }
                }
            }
        }

        private void MenuKonfliktet_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return;
            }

            using (var databaza = new Databaza(connectionString))
            {
                // Konfliktet e profesoreve
                var profesorKonfliktet = KrijoList(new
                {
                    Profesori = "",
                    Dita = "",
                    Lenda1 = "",
                    Salla1 = "",
                    Orari1 = "",
                    Lenda2 = "",
                    Salla2 = "",
                    Orari2 = ""
                });

                var profesoret = databaza.Profesoret;
                foreach (var profesori in profesoret)
                {
                    var ditet = profesori.Angazhimet
                        .Where(a => a.Lenda.Semestri % 2 == ComboBoxSemestrat.SelectedIndex)
                        .SelectMany(a => a.Oraret)
                        .Where(o => o.EshtePercaktuar())
                        .GroupBy(o => o.Dita);

                    foreach (var dita in ditet)
                    {
                        var oraretRenditur = dita.OrderBy(o => o.KohaFillimit).ToList();
                        for (int i = 1; i < oraretRenditur.Count; i++)
                        {
                            var orariPare = oraretRenditur[i - 1];
                            var orariDyte = oraretRenditur[i];
                            bool konflikt = orariPare.KohaMbarimit > orariDyte.KohaFillimit;
                            if (konflikt)
                            {
                                profesorKonfliktet.Add(new
                                {
                                    Profesori = profesori.Emri,
                                    Dita = dita.Key,
                                    Lenda1 = orariPare.Angazhimi.Lenda.Emri,
                                    Salla1 = orariPare.Salla,
                                    Orari1 = orariPare.KohaString,
                                    Lenda2 = orariDyte.Angazhimi.Lenda.Emri,
                                    Salla2 = orariDyte.Salla,
                                    Orari2 = orariDyte.KohaString
                                });
                            }
                        }
                    }
                }

                var sallatKonfliktet = KrijoList(new
                {
                    Salla = "",
                    Dita = "",
                    Lenda1 = "",
                    Profesori1 = "",
                    Orari1 = "",
                    Lenda2 = "",
                    Profesori2 = "",
                    Orari2 = ""
                });

                var sallat = Queryable
                    .Where(databaza.Oraret, o => o.Angazhimi.Lenda.Semestri % 2 == ComboBoxSemestrat.SelectedIndex)
                    .GroupBy(o => o.Salla);

                foreach (var salla in sallat)
                {
                    var ditet = salla
                        .Where(o => o.EshtePercaktuar())
                        .GroupBy(o => o.Dita);

                    foreach (var dita in ditet)
                    {
                        var oraretRenditur = dita.OrderBy(o => o.KohaFillimit).ToList();
                        for (int i = 1; i < oraretRenditur.Count; i++)
                        {
                            var orariPare = oraretRenditur[i - 1];
                            var orariDyte = oraretRenditur[i];
                            bool konflikt = orariPare.KohaMbarimit > orariDyte.KohaFillimit;
                            if (konflikt)
                            {
                                sallatKonfliktet.Add(new
                                {
                                    Salla = salla.Key,
                                    Dita = dita.Key,
                                    Lenda1 = orariPare.Angazhimi.Lenda.Emri,
                                    Profesori1 = orariPare.Angazhimi.Profesori.Emri,
                                    Orari1 = orariPare.KohaString,
                                    Lenda2 = orariDyte.Angazhimi.Lenda.Emri,
                                    Profesori2 = orariDyte.Angazhimi.Profesori.Emri,
                                    Orari2 = orariDyte.KohaString
                                });
                            }
                        }
                    }
                }

                DataGridKonfliktetSalla.ItemsSource = sallatKonfliktet;
                DataGridKonfliktetProfesora.ItemsSource = profesorKonfliktet;
                KonfliktetFlyout.IsOpen = true;
            }
        }

        private List<T> KrijoList<T>(T shembull)
        {
            return new List<T>();
        }

        private void MenaxhoOraret_Click(object sender, RoutedEventArgs e)
        {
            Lenda lenda = (Lenda)((FrameworkElement)sender).DataContext;
            ShfaqOraret(lenda.Id);
        }

        private void ShfaqOraret(int lendaId)
        {
            databaza = new Databaza(connectionString);
            var lenda = databaza.Lendet.Find(lendaId);
            var oraret = lenda
                .Angazhimet
                .SelectMany(a => a.Oraret)
                .Select(o => new OrariMbeshtjelles(o))
                .ToList();

            DataGridOraretLigjerata.ItemsSource = oraret
                .Where(o => o.Lloji == LlojiLigjerates.Ligjerata)
                .ToList();
            DataGridOraretUshtrime.ItemsSource = oraret
                .Where(o => o.Lloji == LlojiLigjerates.Ushtrime)
                .ToList();
            MenaxhoOraretFlyout.Header = lenda.Emri + " (" + lenda.FondiOreve + ")";
            MenaxhoOraretFlyout.IsOpen = true;
        }

        private void AnuloMenaxhoOraret_Click(object sender, RoutedEventArgs e)
        {
            if (!MenaxhoOraretFlyout.IsOpen)
            {
                return;
            }

            DataGridOraretLigjerata.ItemsSource = null;
            DataGridOraretUshtrime.ItemsSource = null;
            databaza.Dispose();
            MenaxhoOraretFlyout.IsOpen = false;
        }

        private void RuajMenaxhoOraret_Click(object sender, RoutedEventArgs e)
        {
            if (!MenaxhoOraretFlyout.IsOpen)
            {
                return;
            }

            databaza.SaveChanges();
            databaza.Dispose();
            MenaxhoOraretFlyout.IsOpen = false;
        }

        private void MbushDataGrid(Databaza databaza)
        {
            DataGridLendet.ItemsSource = Queryable.Where(databaza.Lendet, l => l.Semestri % 2 == ComboBoxSemestrat.SelectedIndex).Include(l => l.Angazhimet).AsNoTracking().ToList();
        }

        private void MetroWindow_KeyChange(object sender, KeyEventArgs e)
        {
            if (OraretSallaveFlyout.IsOpen)
            {
                foreach (var kvp in grafetSalla)
                {
                    kvp.Value.Refresh();
                }
            }

            if (OraretProfesoreveFlyout.IsOpen)
            {
                foreach (var kvp in grafetProfesora)
                {
                    kvp.Value.Refresh();
                }
            }
        }

        private void ComboBoxSemestrat_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (connectionString == null) return;
            using (var databaza = new Databaza(connectionString))
            {
                MbushDataGrid(databaza);
            }
        }
    }
}