using System.ComponentModel;

namespace Squash.DataAccess
{
    public interface IDBSet<TEntity> : IQueryable<TEntity>, IListSource
    {
    }
}
