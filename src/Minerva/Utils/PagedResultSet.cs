using System.Collections.Generic;

namespace Minerva.Utils
{
  
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
}