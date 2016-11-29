using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Minerva.Importer.Extensions;

namespace Minerva.Importer
{
    public class DanishCompanyCreator
    {
        private readonly Dictionary<string, string> _commonPhrases = new Dictionary<string, string>();
        private readonly string[] _cvrData;
        public DanishCompanyCreator(string[] cvrData)
        {
            _cvrData = cvrData;
            SetupCommonPhrases();
        }

        private void SetupCommonPhrases()
        {
            _commonPhrases.Add("aps", "ApS");
            _commonPhrases.Add("af", "af");
            _commonPhrases.Add("og", "og");
            _commonPhrases.Add("en", "en");
        }

        public DanishCompanyIndex CreateNew()
        {
            return new DanishCompanyIndex
            {
                CVRNumber = _cvrData[1],//"cvrnr"
                CompanyName = CleanString(_cvrData[7]),//"navn_tekst"
                Phone = _cvrData[58],//"telefonnummer_kontaktoplysning"
                Email = _cvrData[62],//"email_kontaktoplysning"
                Street = CreateStreet(),
                City = CleanString(_cvrData[18]),//"beliggenhedsadresse_postdistrikt"
                PlaceName = CleanString(_cvrData[19]),//"beliggenhedsadresse_bynavn"
                Zip = _cvrData[17],//"beliggenhedsadresse_postnr"
                CoName = CleanString(_cvrData[23])//"beliggenhedsadresse_coNavn"
            };
        }

        private string CreateStreet()
        {
            return string.Format("{0} {1}", CleanString(_cvrData[9])/*"beliggenhedsadresse_vejnavn"*/, GetRoadNumber());
        }

        private object GetRoadNumber()
        {
            var husNummerFra = _cvrData[11]; //"beliggenhedsadresse_husnummerFra"
            var husummerTil = _cvrData[12];//"beliggenhedsadresse_husnummerTil"
            var bogstavFra = _cvrData[13];//"beliggenhedsadresse_bogstavFra"
            var bogstavTil= _cvrData[14];//"beliggenhedsadresse_bogstavTil"

            var builder = new StringBuilder(string.Format("{0}{1}", husNummerFra, bogstavFra));
            if (!string.IsNullOrEmpty(husummerTil) || !string.IsNullOrEmpty(bogstavFra))
                builder.AppendFormat("-{0}{1}", husummerTil, bogstavTil);
            return CleanString(builder.ToString());
        }

        private string CleanString(string convert)
        {
            return string.Join(" ", convert.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries).Select(s => GetProperCase(s.ToLower())));
        }

        private string GetProperCase(string convert)
        {
            return _commonPhrases.ContainsKey(convert) ? _commonPhrases[convert] : convert.ToTitleCase();
        }
    }
}
