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

        private string GetSqlTypeFromValue(object value)
        {
            // Gelen değer tipine göre SQL veri tipini döndürüyoruz
            return value switch
            {
                int => "INT",
                long => "BIGINT",
                double => "DOUBLE PRECISION",
                decimal => "DECIMAL",
                bool => "BOOLEAN",
                DateTime => "TIMESTAMP",
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
