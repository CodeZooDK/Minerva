using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Minerva.Model;
using Minerva.Queries;
using Minerva.Utils;

namespace Minerva.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        // GET api/values
        [HttpGet]
        public PagedResultSet<DanishCompanyIndex> Get(string queryString, ListOptions listOptions)
        {
            if (string.IsNullOrEmpty(queryString))
                return new PagedResultSet<DanishCompanyIndex>();
            var elasticHelper = new ElasticHelper();
            IEnumerable<string> errors;
            int cvr;
            var query = int.TryParse(queryString, out cvr)
                ? (IElasticQuery<DanishCompanyIndex>)new DanishCompanyByCvrQuery(queryString, listOptions)
                : new DanishCompanyByCompanyNameQuery(queryString, listOptions);
            return elasticHelper.TryWrapQuery(query, out errors)
                ? new PagedResultSet<DanishCompanyIndex>(query.Results.Hits.Select(h => h.Source).ToList(), (int)query.Results.Total)
                : new PagedResultSet<DanishCompanyIndex>();
        }

    }
}
