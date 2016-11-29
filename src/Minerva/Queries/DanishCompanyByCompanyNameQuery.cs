using System.Collections.Generic;
using Minerva.Model;
using Minerva.Utils;
using Nest;

namespace Minerva.Queries
{
    public class DanishCompanyByCompanyNameQuery : ElasticQuery<DanishCompanyIndex>
    {
        private readonly string _companyName;
        private readonly ListOptions _listOptions;

        public DanishCompanyByCompanyNameQuery(string companyName, ListOptions listOptions)
            : base(listOptions)
        {
            _companyName = companyName;
            _listOptions = listOptions;
        }

        public override bool TryExecute(ElasticClient client, out IEnumerable<string> errors)
        {
            Results = client.Search<DanishCompanyIndex>(sd =>
            {
                sd.Query(q => q.Prefix(dci => dci.CompanyName, _companyName));
                if (_listOptions.Page > 0)
                    sd.Skip(_listOptions.Page * _listOptions.PageSize);
                sd.Take(_listOptions.PageSize);
                return sd;
            });
            return SuccessResult(out errors);
        }
    }
}