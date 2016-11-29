using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Serilog;
using Minerva.Importer.Extensions;

namespace Minerva.Importer
{
    class Program
    {
        static void Main(string[] args)
        {
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();
            IEnumerable<string> errors;
            if (!new IndexDanishCompaniesCommand("hest", Configuration).TryExecute(out errors))
                errors.ForEach(Log.Logger.Error);
        }
        public static IConfigurationRoot Configuration { get; private set; }
    }
}
