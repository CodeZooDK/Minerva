using System.Collections.Generic;
using Nest;

namespace Minerva.Importer
{
    public interface IElasticCommand
    {
        bool TryExecute(ElasticClient client, out IEnumerable<string> errors);
    }
}