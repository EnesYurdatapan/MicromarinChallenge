using DataAccess.Abstract;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities;

namespace DataAccess.Concrete
{
    public class WriteRepository<T> : IWriteRepository<T> where T : BaseEntity
    {
        private readonly Context _context;

        public WriteRepository(Context context)
        {
            _context = context;
        }
        public DbSet<T> Table => _context.Set<T>();

        public async Task<bool> AddAsync(T entity)
        {
            EntityEntry<T> entityEntry = await Table.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entityEntry.State == EntityState.Added;
        }

        public async Task<bool> AddRangeAsync(List<T> entities)
        {
            await Table.AddRangeAsync(entities);
            return true;
        }

        //public async Task<bool> DeleteAsync(int id)
        //{
        //    T model = await Table.FirstOrDefaultAsync(data => data.Id == id);
        //    return Delete(model);
        //}
        public bool Delete(int id)
        {
            var entity = Table.FirstOrDefault(x => x.Id == id);
            EntityEntry<T> entityEntry = Table.Remove(entity);
            _context.SaveChanges();
            return entityEntry.State == EntityState.Deleted;
        }

        public async Task<int> SaveAsync()
             => await _context.SaveChangesAsync();

        public bool UpdateAsync(T entity)
        {
            EntityEntry<T> entityEntry = Table.Update(entity);
             _context.SaveChanges();
            return entityEntry.State == EntityState.Modified;
        }
    }
}
