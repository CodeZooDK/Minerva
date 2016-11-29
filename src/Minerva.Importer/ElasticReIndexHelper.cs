using System;
using System.Collections.Generic;
using System.Linq;
using Elasticsearch.Net;
using Microsoft.Extensions.Configuration;
using Nest;
using Serilog;

namespace Minerva.Importer
{
    public class ElasticReIndexHelper
    {
        private static ElasticClient _client;

        public static bool Initialize(IConfigurationRoot configRoot)
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
                var config = new ConnectionSettings(connectionPool);
                var client = new ElasticClient(config);
                var ping = client.Ping();
                Log.Logger.Debug("Ping resulted in: {0} (HTTP)", ping.IsValid);
                if (ping.IsValid)
                {
                    _client = client;
                    return true;
                }
                if (ping.ServerError != null)
                {
                    Log.Logger.Error("Ping failed with the following errors:");
                    Log.Logger.Error("Servererror - Error: {0}", ping.ServerError.Error);
                    Log.Logger.Error("Servererror - Status: {0}", ping.ServerError.Status);
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.Logger.Error("Unable to ping ElasticSearch", ex);
                return false;
            }
        }

        public bool TryWrap(Predicate<ElasticClient> action, out IEnumerable<string> errors)
        {
            if (_client == null)
            {
                errors = new[] { "Client was not initialized - maybe you forgot to call ElasticHelper.Initialize in the startup" };
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
                errors = new[] { string.Format("Elasticsearch failed - {0}", ex.Message) };
                return false;
            }
        }

        public static void SetMap<TIndex>(string newIndexName, Action<PropertiesDescriptor<TIndex>> properties)
            where TIndex : class
        {
            _client.Map<TIndex>(d => d
                .Properties(p =>
                {
                    properties(p);
                    return p;
                }));
        }


        public static void Dispose()
        {
            _client = null;
        }
    }
}