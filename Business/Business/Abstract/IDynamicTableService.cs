using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Abstract
{
    public interface IDynamicTableService
    {
        Task InsertDataAsync(string objectType, Dictionary<string, object> fields);
        Task<JObject> GetDataById(string objectType, int id);
        Task UpdateData(string objectType, int id, Dictionary<string, object> fields);
        Task DeleteData(string objectType, int id);
        Task CreateTableFromSchemaAsync(string objectType, Dictionary<string, object> fields);
        Task<bool> TableExistsAsync(string tableName);
        Task<List<Dictionary<string, object>>> GetObjectsByTypeAndFiltersAsync(string objectType, int? id, Dictionary<string, string> filters);

    }
}
