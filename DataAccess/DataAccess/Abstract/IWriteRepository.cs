using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
    public interface IWriteRepository<T>:IRepository<T> where T : class
    {
        Task<bool> AddAsync(T entity);
        Task<bool> AddRangeAsync(List<T> entities);
        bool UpdateAsync(T entity);
        //Task<bool> DeleteAsync(int id);
        bool Delete(int id);
        Task<int> SaveAsync();
    }
}
