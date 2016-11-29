using System.Collections.Generic;
using System.Text.RegularExpressions;
using Minerva.Model;
using Minerva.Utils;
using Nest;

namespace Minerva.Queries
{
    public class DanishCompanyByCvrQuery : ElasticQuery<DanishCompanyIndex>
    {
        private readonly string _cvrNumber;
        private readonly ListOptions _listOptions;

        public DanishCompanyByCvrQuery(string cvrNumber, ListOptions listOptions)
            : base(listOptions)
        {
            _listOptions = listOptions;
            var regexObj = new Regex(@"[^\d]");
            _cvrNumber = regexObj.Replace(cvrNumber, "");
        }

        public override bool TryExecute(ElasticClient client, out IEnumerable<string> errors)
        {
            Results = client.Search<DanishCompanyIndex>(sd =>
            {
                sd.Query(q => q.Prefix(dc => dc.CVRNumber, _cvrNumber));
                if (_listOptions.Page > 0)
                    sd.Skip(_listOptions.Page * _listOptions.PageSize);
                sd.Take(_listOptions.PageSize);
                return sd;
            });
            return SuccessResult(out errors);
        }
    }
}