using System;
using System.Collections.Generic;
using System.Linq;
using Elasticsearch.Net;
using Microsoft.Extensions.Configuration;
using Minerva.Model;
using Minerva.Queries;
using Nest;
using Serilog;

namespace Minerva.Utils
{
    public class ElasticHelper
    {
        private static ElasticClient _client;
        public static void Initialize(IConfigurationRoot configRoot)
        {
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
                    d.Add(typeof(DanishCompanyIndex), "danish_cvr");
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
}