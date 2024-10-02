using Business.Abstract;
using DataAccess;
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
        private readonly IDynamicTableService _dynamicTableService;

        public ObjectSchemaService(Context context, IDynamicTableService dynamicTableService)
        {
            _context = context;
            _dynamicTableService = dynamicTableService;
        }

        public async Task<ObjectSchema> CreateObjectSchemaAsync(string objectType, IList<Field> fields)
        {
            // Şemanın zaten var olup olmadığını kontrol ediyoruz
            var existingSchema = await _context.ObjectSchemas
                .Include(s => s.Fields) // Fields ile birlikte alıyoruz
                .FirstOrDefaultAsync(s => s.ObjectType == objectType);

            ObjectSchema schema;

            if (existingSchema != null)
            {
                schema = existingSchema;
            }
            else
            {
                // Yeni şema oluştur
                schema = new ObjectSchema
                {
                    ObjectType = objectType,
                    Fields = fields
                };

                _context.ObjectSchemas.Add(schema);
                await _context.SaveChangesAsync();
            }

            // Ana tabloyu oluştur
            await _dynamicTableService.CreateTableFromSchemaAsync(objectType, fields.ToDictionary(f => f.FieldName, f => f.FieldType));

            // Alt şema varsa alt tabloları da oluşturuyoruz
            foreach (var field in fields)
            {
                if (field.ChildSchema != null)
                {
                    // İlgili child schema ve tablosunu da oluştur
                    await CreateObjectSchemaAsync(field.ChildSchema.ObjectType, field.ChildSchema.Fields.ToList());

                    // Foreign Key ekliyoruz
                    var foreignKeyField = new Field
                    {
                        ObjectSchemaId = schema.Id,
                        FieldName = field.FieldName + "Id",
                        FieldType = "int",
                        IsRequired = true,
                        ChildSchema = field.ChildSchema
                    };

                    _context.Fields.Add(foreignKeyField);
                    await _context.SaveChangesAsync();
                }
            }

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