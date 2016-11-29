using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using Minerva.Importer.Extensions;
using Nest;
using Serilog;

namespace Minerva.Importer
{
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
}