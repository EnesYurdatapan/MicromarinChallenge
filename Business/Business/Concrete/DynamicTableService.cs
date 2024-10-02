using Business.Abstract;
using DataAccess;
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
    public class DynamicTableService: IDynamicTableService
    {
        private readonly Context _context;

        public DynamicTableService(Context context)
        {
            _context = context;
        }

        // Insert işlemi
        public async Task InsertDataAsync(string objectType, Dictionary<string, object> fields, int? parentId = null, string parentFieldName = null)
        {
            var sb = new StringBuilder();
            var parameters = new List<object>();
            int paramIndex = 0;

            // 1. Get the schema for the objectType (this should be retrieved from your ObjectSchema table)
            var schema = await _context.ObjectSchemas
                                       .Include(s => s.Fields)
                                       .FirstOrDefaultAsync(s => s.ObjectType == objectType);

            if (schema == null)
            {
                throw new Exception($"Schema for object type {objectType} not found.");
            }

            // 2. Build the SQL insert statement for the non-relational fields
            sb.Append($"INSERT INTO \"{objectType}\" (");

            foreach (var field in fields)
            {
                // Check if this field is a relationship (array of child objects)
                if (field.Value is JArray childItems)
                {
                    // For each item in the array, call InsertDataAsync recursively to handle child records
                    foreach (var childItem in childItems)
                    {
                        if (childItem is JObject childObject)
                        {
                            var childFields = childObject.ToObject<Dictionary<string, object>>();
                            var childSchema = schema.Fields.FirstOrDefault(f => f.FieldName == field.Key);

                            if (childSchema != null && childSchema.ForeignKeyTable != null)
                            {
                                // Recursively insert child data into the related table
                                await InsertDataAsync(childSchema.ForeignKeyTable, childFields, parentId, schema.ObjectType + "Id");
                            }
                        }
                    }
                }
                else
                {
                    // Add the non-relational field to the SQL statement
                    sb.Append($"\"{field.Key}\", ");
                    parameters.Add(field.Value);
                    paramIndex++;
                }
            }

            // 3. Add the foreign key column if this is a child record
            if (parentId.HasValue && !string.IsNullOrEmpty(parentFieldName))
            {
                sb.Append($"\"{parentFieldName}\", ");
                parameters.Add(parentId.Value);
                paramIndex++;
            }

            // Remove the last comma and space, then close the field section
            sb.Length -= 2;
            sb.Append(") VALUES (");

            // Add the parameter placeholders
            for (int i = 0; i < paramIndex; i++)
            {
                sb.Append($"@p{i}, ");
            }

            // Remove the last comma and space, then close the VALUES section
            sb.Length -= 2;
            sb.Append(");");

            // 4. Execute the SQL insert statement
            await _context.Database.ExecuteSqlRawAsync(sb.ToString(), parameters.ToArray());

            // 5. After the insert, you might need to return the ID of the inserted record to link with child data
        }





        private async Task ExecuteInsertAsync(string query, List<object> parameters)
        {
            // SQL sorgusunu parametrelerle birlikte çalıştırıyoruz
            Console.WriteLine(query);
            await _context.Database.ExecuteSqlRawAsync(query, parameters.ToArray());
        }




        // Read işlemi (ID'ye göre)
        public async Task<JObject> GetDataById(string objectType, int id)
        {
            var dbSet = GetDynamicDbSet(objectType);
            var entity = await dbSet.FindAsync(id);

            if (entity != null)
            {
                return JObject.FromObject(entity);
            }
            throw new Exception("Record not found.");
        }

        // Update işlemi
        public async Task UpdateData(string objectType, int id, Dictionary<string, object> fields)
        {
            var dbSet = GetDynamicDbSet(objectType);
            var entity = await dbSet.FindAsync(id);

            if (entity != null)
            {
                foreach (var field in fields)
                {
                    entity.GetType().GetProperty(field.Key).SetValue(entity, field.Value);
                }

                _context.Update(entity);
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new Exception("Record not found.");
            }
        }

        // Delete işlemi
        public async Task DeleteData(string objectType, int id)
        {
            var dbSet = GetDynamicDbSet(objectType);
            var entity = await dbSet.FindAsync(id);

            if (entity != null)
            {
                dbSet.Remove(entity);
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new Exception("Record not found.");
            }
        }

        // Dinamik DbSet yaratma
        private DbSet<dynamic> GetDynamicDbSet(string objectType)
        {
            return _context.GetDbSet(objectType);
        }

        // Dinamik entity oluşturma
        private object CreateDynamicEntity(string objectType, Dictionary<string, object> fields)
        {
            var entity = Activator.CreateInstance(Type.GetType(objectType));
            foreach (var field in fields)
            {
                entity.GetType().GetProperty(field.Key)?.SetValue(entity, field.Value);
            }
            return entity;
        }

        public async Task CreateTableFromSchemaAsync(string objectType, Dictionary<string, string> fields)
        {
            var sb = new StringBuilder();

            sb.Append($"CREATE TABLE IF NOT EXISTS \"{objectType}\" (");

            sb.Append("Id SERIAL PRIMARY KEY, ");

            foreach (var field in fields)
            {
                string columnName = field.Key;
                string columnType = GetSqlTypeFromValue(field.Value);

                sb.Append($"\"{columnName}\" {columnType}, ");
            }

            sb.Length -= 2; // Son virgülü kaldırıyoruz
            sb.Append(");");

            await _context.Database.ExecuteSqlRawAsync(sb.ToString());
        }



        public async Task<List<Dictionary<string, object>>> GetObjectsByTypeAndFiltersAsync(string objectType, int? id, Dictionary<string, string> filters)
        {
            // SQL sorgusunu oluşturmak için StringBuilder kullanıyoruz
            var sb = new StringBuilder();

            // Gelen objectType'a göre tablodan veri çekiyoruz
            sb.Append($"SELECT * FROM \"{objectType}\"");

            // Eğer id parametresi gelmişse ona göre sorgu ekliyoruz
            if (id.HasValue)
            {
                sb.Append($" WHERE Id = {id.Value}");
            }
            else if (filters != null && filters.Any())
            {
                sb.Append(" WHERE ");

                // Filtreleri dinamik olarak ekliyoruz
                foreach (var filter in filters)
                {
                    string columnName = filter.Key;
                    object columnValue = filter.Value;

                    // String ya da tarih gibi tiplerde veriyi tırnak içinde yazıyoruz
                    if (columnValue is string || columnValue is DateTime)
                    {
                        sb.Append($"\"{columnName}\" = '{columnValue}' AND ");
                    }
                    else
                    {
                        sb.Append($"\"{columnName}\" = {columnValue} AND ");
                    }
                }

                // Son eklenen AND ifadesini kaldırıyoruz
                sb.Length -= 5; // " AND " ifadesini kaldırmak için
            }

            sb.Append(";"); // Sorguyu bitiriyoruz

            var results = new List<Dictionary<string, object>>();

            // SQL sorgusunu manuel çalıştırmak için ADO.NET kullanıyoruz
            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sb.ToString();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();
                            for (var i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.GetValue(i);
                            }
                            results.Add(row);
                        }
                    }
                }
            }

            return results;
        }



        private string GetSqlTypeFromValue(object value)
        {
            // Gelen fieldType string olduğunda uygun SQL veri tipini döndürüyoruz
            return value.ToString().ToLower() switch
            {
                "int" => "INT",
                "bigint" => "BIGINT",
                "double" => "DOUBLE PRECISION",
                "decimal" => "DECIMAL",
                "bool" => "BOOLEAN",
                "datetime" => "TIMESTAMP",
                "string" => "TEXT", // VARCHAR kullanmak isterseniz VARCHAR olarak da değiştirebilirsiniz
                _ => "TEXT" // Varsayılan olarak TEXT kullanıyoruz
            };
        }

        public async Task<bool> TableExistsAsync(string tableName)
        {
            var result = await _context.Database.ExecuteSqlRawAsync($"SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = '{tableName.ToLower()}');");
            return result == 1;
        }

        public async Task CreateForeignKeyAsync(string parentTable, string childTable, string foreignKeyField)
        {
            // Foreign key adını oluşturuyoruz
            var foreignKeyName = $"FK_{parentTable}_{childTable}";

            // SQL sorgusu ile foreign key eklenmesi
            var sql = $@"
        ALTER TABLE ""{parentTable}""
        ADD CONSTRAINT ""{foreignKeyName}""
        FOREIGN KEY (""{foreignKeyField}"")
        REFERENCES ""{childTable}"" (""Id"")
        ON DELETE CASCADE;
    ";

            // Foreign key ekleme işlemi
            await _context.Database.ExecuteSqlRawAsync(sql);
        }


    }

}
