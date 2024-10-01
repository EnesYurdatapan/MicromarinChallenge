using Business.Abstract;
using DataAccess;
using DataAccess.Abstract.EntityFramework.ObjectDataRepositories;
using DataAccess.Abstract.EntityFramework.ObjectSchemaRepositories;
using Entities;
using Entities.DTOs;
using Entities.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Concrete
{
    public class ObjectSchemaService : IObjectSchemaService
    {
        private readonly Context _context;

        public ObjectSchemaService(Context context)
        {
            _context = context;
        }

        public async Task<ObjectSchema> CreateObjectSchemaAsync(string objectType, List<Field> fields)
        {
            var schema = new ObjectSchema
            {
                ObjectType = objectType,
                Fields = fields
            };

            _context.ObjectSchemas.Add(schema);
            await _context.SaveChangesAsync();

            return schema;
        }

        public async Task<ObjectSchema> GetObjectSchemaAsync(int id)
        {
            return await _context.ObjectSchemas
                .Include(os => os.Fields)
                .FirstOrDefaultAsync(os => os.Id == id);
        }

        public async Task<List<ObjectSchema>> GetAllObjectSchemasAsync()
        {
            return await _context.ObjectSchemas
                .Include(os => os.Fields)
                .ToListAsync();
        }

        public async Task UpdateObjectSchemaAsync(ObjectSchema objectSchema)
        {
            _context.ObjectSchemas.Update(objectSchema);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteObjectSchemaAsync(int id)
        {
            var schema = await _context.ObjectSchemas.FindAsync(id);
            if (schema != null)
            {
                _context.ObjectSchemas.Remove(schema);
                await _context.SaveChangesAsync();
            }
        }
    }
}