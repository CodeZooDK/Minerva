using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Minerva.Importer;
using Nest;
using Serilog;

namespace Minerva.Controllers
{
    public class ListOptions
    {
        public ListOptions()
        {
            Page = 0;
            PageSize = 10;
        }

        public bool ShowDeactivated { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool ForceNoPaging { get; set; }
    }
    public class PagedResultSet<TEntity>
    {
        public PagedResultSet(IList<TEntity> page, int count)
        {
            Entities = page;
            Count = count;
        }

        public PagedResultSet()
        {
            Entities = new List<TEntity>();
            Count = 0;
        }
        public int Count { get; set; }
        public IList<TEntity> Entities { get; private set; }
    }
    public interface IElasticQuery
    {
        bool TryExecute(ElasticClient client, out IEnumerable<string> errors);
    }

    public interface IElasticQuery<T> : IElasticQuery
        where T : class
    {
        ISearchResponse<T> Results { get; }
    }
    public class ElasticHelper
    {
        private static ElasticClient _client;
        private static string _defaultIndex;
        public static void Initialize(IConfigurationRoot configRoot)
        {
            _defaultIndex = "xena";
            try
            {
                var urls = new[]
                {
                        configRoot["elastic_node_1"],
                        configRoot["elastic_node_2"],
                        configRoot["elastic_node_3"]
                }.Where(u => !string.IsNullOrEmpty(u))
                .Select(u => new Uri(u))
                .ToArray();
                IConnectionPool connectionPool = new SniffingConnectionPool(urls);
                var config = new ConnectionSettings(connectionPool).MapDefaultTypeIndices(d =>
                {
                    d.Add(typeof(DanishCompanyIndex), $"{_defaultIndex}_danish_cvr");
                });
                var client = new ElasticClient(config);
                var ping = client.Ping();
                Log.Logger.Warning($"Ping resulted in: {ping.IsValid} (Validity)");
                if (ping.IsValid)
                    _client = client;
            }
            catch (Exception ex)
            {
                Log.Logger.Error("Unable to ping ElasticSearch", ex);
            }
        }

        public bool TryWrap(Predicate<ElasticClient> action, out IEnumerable<string> errors)
        {
            if (_client == null)
            {
                errors = new[] { "Client was not initialized - maybe you forgot to call ElasticHelper.Initialize in the startup" };
                Log.Logger.Warning(string.Join(Environment.NewLine, errors));
                return false;
            }
            try
            {
                var success = action(_client);
                errors = new string[0];
                return success;
            }
            catch (Exception ex)
            {
                Log.Logger.Error("ElasticSearch helper encountered an exception.", ex);
                errors = new[] { $"Elasticsearch failed - {ex.Message}" };
                return false;
            }
        }

        public bool TryWrapQuery(IElasticQuery query, out IEnumerable<string> errors)
        {
            if (_client == null)
            {
                errors = new[] { "Client was not initialized - maybe you forgot to call ElasticHelper.Initialize in the startup" };
                return false;
            }
            try
            {
                var success = query.TryExecute(_client, out errors);
                if (errors.Any())
                    Log.Logger.Warning(string.Join(Environment.NewLine, errors));
                return success;
            }
            catch (Exception ex)
            {
                Log.Logger.Error("ElasticSearch helper encountered an exception.", ex);
                errors = new[] { $"Elasticsearch failed - {ex.Message}" };
                return false;
            }
        }
    }
    public abstract class ElasticQuery<T> : IElasticQuery<T>
       where T : class
    {
        protected readonly ListOptions _listOptions;

        protected ElasticQuery(ListOptions listOptions)
        {
            _listOptions = listOptions;
        }

        public ISearchResponse<T> Results { get; protected set; }

        public abstract bool TryExecute(ElasticClient client, out IEnumerable<string> errors);
      
        protected bool SuccessResult(out IEnumerable<string> errors)
        {
            errors = new string[0];
            return true;
        }

        protected virtual void LimitQuery(SearchDescriptor<T> query)
        {
            query.Take((_listOptions.Page + 1) * _listOptions.PageSize + 10);
        }

        protected bool ErrorResult(out IEnumerable<string> errors, params string[] error)
        {
            errors = error;
            return false;
        }

    }
    public class DanishCompanyByCVRQuery : ElasticQuery<DanishCompanyIndex>
    {
        private readonly string _cvrNumber;
        private readonly ListOptions _listOptions;

        public DanishCompanyByCVRQuery(string cvrNumber, ListOptions listOptions)
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
                ? (IElasticQuery<DanishCompanyIndex>)new DanishCompanyByCVRQuery(queryString, listOptions)
                : new DanishCompanyByCompanyNameQuery(queryString, listOptions);
            return elasticHelper.TryWrapQuery(query, out errors)
                ? new PagedResultSet<DanishCompanyIndex>(query.Results.Hits.Select(h => h.Source).ToList(), (int)query.Results.Total)
                : new PagedResultSet<DanishCompanyIndex>();
        }

    }
}
