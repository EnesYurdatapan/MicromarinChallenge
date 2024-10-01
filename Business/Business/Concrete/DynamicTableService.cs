using Business.Abstract;
using DataAccess;
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
        public async Task InsertDataAsync(string objectType, Dictionary<string, object> fields)
        {
            // SQL sorgusunu oluşturmak için StringBuilder kullanıyoruz
            var sb = new StringBuilder();

            // Tabloya insert işlemi için SQL cümlesi oluşturuluyor
            sb.Append($"INSERT INTO \"{objectType}\" (");

            // Sütun adlarını ekliyoruz
            foreach (var field in fields)
            {
                sb.Append($"\"{field.Key}\", ");
            }

            // Son virgülü kaldırıyoruz ve VALUES kısmını ekliyoruz
            sb.Length -= 2;
            sb.Append(") VALUES (");

            // Değerler placeholder olarak ekleniyor (örneğin, @p0, @p1, vb.)
            var parameters = new List<object>();
            int paramIndex = 0;
            foreach (var field in fields)
            {
                sb.Append($"@p{paramIndex}, ");
                parameters.Add(field.Value);
                paramIndex++;
            }

            // Son virgülü kaldırıyoruz ve sorguyu kapatıyoruz
            sb.Length -= 2;
            sb.Append(");");

            // SQL sorgusunu parametrelerle birlikte çalıştırıyoruz
            await _context.Database.ExecuteSqlRawAsync(sb.ToString(), parameters.ToArray());
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

        public async Task CreateTableFromSchemaAsync(string objectType, Dictionary<string, object> fields)
        {
            // SQL sorgusunu oluşturmak için StringBuilder kullanıyoruz
            var sb = new StringBuilder();

            sb.Append($"CREATE TABLE IF NOT EXISTS \"{objectType}\" (");

            // İlk sütunu (id) primary key olarak tanımlıyoruz
            sb.Append("Id SERIAL PRIMARY KEY, ");

            // Gelen field'lar için kolonları ekliyoruz
            foreach (var field in fields)
            {
                string columnName = field.Key;
                string columnType = GetSqlTypeFromValue(field.Value);

                // Her bir field'ı SQL cümlesine ekliyoruz
                sb.Append($"\"{columnName}\" {columnType}, ");
            }

            // Son virgülü kaldırıyoruz ve parantezi kapatıyoruz
            sb.Length -= 2; // Son iki karakteri (virgül ve boşluk) siliyoruz
            sb.Append(");");

            // SQL sorgusunu çalıştırıyoruz
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

    }

}
