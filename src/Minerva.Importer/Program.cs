using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Serilog;
using Minerva.Importer.Extensions;

namespace Minerva.Importer
{
    class Program
    {
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            if(!ElasticReIndexHelper.Initialize(Configuration))
                return;
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();
            IEnumerable<string> errors;
            if (!new IndexDanishCompaniesCommand("danish_cvr", Configuration).TryExecute(out errors))
                errors.ForEach(Log.Logger.Error);
        }
        public static IConfigurationRoot Configuration { get; private set; }
    }
}
