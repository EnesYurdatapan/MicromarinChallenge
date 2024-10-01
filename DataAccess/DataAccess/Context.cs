using Entities;
using Entities.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess
{
    public class Context : DbContext
    {
        public Context(DbContextOptions options) : base(options)
        {
        }
        public DbSet<ObjectSchema> ObjectSchemas { get; set; }
        public DbSet<Field> Fields { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ObjectSchema - Field ilişkisi
            modelBuilder.Entity<ObjectSchema>()
                .HasMany(os => os.Fields)
                .WithOne(f => f.ObjectSchema)
                .HasForeignKey(f => f.ObjectSchemaId);
        }

        public DbSet<dynamic> GetDbSet(string tableName)
        {
            return (DbSet<dynamic>)this.GetType().GetProperty(tableName).GetValue(this);
        }
    }
}
