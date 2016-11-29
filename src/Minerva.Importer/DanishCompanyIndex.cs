using System;
using Elasticsearch.Net;
using Nest;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using Minerva.Importer.Extensions;
using Serilog;
namespace Minerva.Importer
{
    public class DanishCompanyIndex
    {
        public string CompanyName { get; set; }
        public string CoName { get; set; }
        public string Street { get; set; }
        public string Zip { get; set; }
        public string City { get; set; }
        public string PlaceName { get; set; }
        public string CVRNumber { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Id => CVRNumber;
    }
    public class IndexDanishCompaniesCommand
    {
        private readonly string _index;
        private readonly IConfigurationRoot _config;

        public IndexDanishCompaniesCommand(string index, IConfigurationRoot config)
        {
            _index = index;
            _config = config;
        }

        public bool TryExecute(out IEnumerable<string> errors)
        {
            var cvrDirectoryPath = _config["CVRDirectory"];
            if (!Directory.Exists(cvrDirectoryPath))
            {
                errors = new[] { string.Format("CVR directory not found - make sure it exists. (app.config: {0})", cvrDirectoryPath) };
                return false;
            }
            Encoding encoding;
            var setting = _config["Encoding"];
            try
            {
                encoding = Encoding.GetEncoding(setting);
            }
            catch (Exception ex)
            {
                var error = string.Format("Failed to parse encoding from app.config: {0}", setting);
                Log.Logger.Error(error, ex);
                errors = new[] { ex.Message, error };
                return false;
            }

            Log.Logger.Warning("Creating Danish Company (CVR) index.");
            return new ElasticReIndexHelper().TryWrap(c =>
            {
                var totalCount = 0;
                new DirectoryInfo(cvrDirectoryPath).GetFiles().ForEach(file =>
                {
                    using (var stream = File.OpenRead(file.FullName))
                    using (var csvReader = new StreamReader(stream, encoding))
                    {
                        if (!csvReader.EndOfStream)
                            csvReader.ReadLine(); //Skip headers
                        bool moreRecords = !csvReader.EndOfStream;
                        while (moreRecords)
                        {
                            var companyList = new List<DanishCompanyIndex>(1000);
                            for (int i = 0; i < 1000; i++)
                            {
                                var line = csvReader.ReadLine().Split(',');
                                companyList.Add(new DanishCompanyCreator(line).CreateNew());
                                if (csvReader.EndOfStream)
                                {
                                    moreRecords = false;
                                    break;
                                }
                            }
                            if (companyList.Any())
                            {
                                var descriptor = new BulkDescriptor();
                                companyList.ForEach(search =>
                                {
                                    descriptor.Index<DanishCompanyIndex>(op => op.Document(search).Id(search.Id).Index(_index));
                                });
                                var result = c.Bulk(d => descriptor);
                                totalCount += result.Items.Count();
                                Log.Logger.Warning(string.Format("Imported {0} {3} - errors: {1}, took {2} MS", totalCount, string.Join(", ", result.Errors), result.TookAsLong, typeof(DanishCompanyIndex).Name));
                            }
                        }
                    }

                });
                return true;
            }, out errors);
        }
    }
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

        public bool TryWrapCommand(IElasticCommand command, out IEnumerable<string> errors)
        {
            if (_client == null)
            {
                errors = new[] { "Client was not initialized - maybe you forgot to call ElasticHelper.Initialize in the startup" };
                return false;
            }
            try
            {
                return command.TryExecute(_client, out errors);
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
    public interface IElasticCommand
    {
        bool TryExecute(ElasticClient client, out IEnumerable<string> errors);
    }

}