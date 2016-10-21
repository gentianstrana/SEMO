using System;
using Oraret.Domeni;
using Oraret.Domeni.Sherbimet;

namespace Oraret
{
    public class OrariMbeshtjelles
    {
        private static readonly IOrarLexues OrarLexuesi = new OrarLexues();

        private readonly Orari orari;

        public OrariMbeshtjelles(Orari orari)
        {
            this.orari = orari;
        }

        public bool Shfaq
        {
            get { return orari.Dukshem; }
            set { orari.Dukshem = value; }
        }

        public string Profesori
        {
            get
            {
                if (orari.Angazhimi != null && orari.Angazhimi.Profesori != null)
                {
                    return orari.Angazhimi.Profesori.Emri;
                }

                return string.Empty;
            }
        }

        public LlojiLigjerates Lloji
        {
            get { return orari.Lloji; }
        }

        public string Grupi
        {
            get { return orari.Grupi; }
            set { orari.Grupi = value != null ? value.Trim() : null; }
        }

        public string Dita
        {
            get { return orari.Dita; }
            set { orari.Dita = value != null ? value.Trim() : null; }
        }

        public string Salla
        {
            get { return orari.Salla; }
            set { orari.Salla = value != null ? value.Trim() : null; }
        }

public string Orari
{
    get
    {
        return orari.KohaCaktuar
            ? OrarLexuesi.NeString(orari.KohaFillimit, orari.KohaMbarimit)
            : string.Empty;
    }
    set
    {
        try
        {
            var rez = OrarLexuesi.NgaStringu(value);
            orari.KohaFillimit = rez.Item1;
            orari.KohaMbarimit = rez.Item2;
            orari.KohaCaktuar = true;
        }
        catch
        {
            orari.KohaCaktuar = false;
        }
    }
}
    }
}
