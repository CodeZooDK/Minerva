using System.Collections.Generic;
using Minerva.Utils;
using Nest;

namespace Minerva.Queries
{
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
}