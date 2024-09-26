using Entities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Abstract
{
    public interface IObjectDataService
    {
        Task<bool> AddAsync(ObjectData objectData);
        Task<bool> AddRangeAsync(List<ObjectData> objectData);
        bool Update(ObjectData objectData);
        bool Delete(int id);
        Task<ObjectData> GetById(int id);
        List<ObjectData> GetAll();
        Task<List<ObjectData>> GetFilteredDataAsync(string objectType, dynamic filters);
    }
}
