using Entities;
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
        public DbSet<ObjectData> ObjectDatas { get; set; }
        public DbSet<ObjectSchema> ObjectSchemas { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ObjectData>()
                .Property(e => e.Data)
                .HasColumnType("jsonb");

            modelBuilder.Entity<ObjectSchema>()
              .Property(e => e.Schema)
              .HasColumnType("jsonb");
        }
    }
}
