using System.Collections.Generic;
using Nest;

namespace Minerva.Queries
{
    public interface IElasticQuery
    {
        bool TryExecute(ElasticClient client, out IEnumerable<string> errors);
    }
    public interface IElasticQuery<T> : IElasticQuery
      where T : class
    {
        ISearchResponse<T> Results { get; }
    }
}