using Entities;
using Entities.DTOs;
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
        Task<bool> AddAsync(AddObjectDataDTO addObjectDataDTO);
        bool Update(UpdateObjectDataDTO updateObjectDataDTO);
        bool Delete(int id);
        Task<ObjectData> GetById(int id);
        List<ObjectData> GetAll();
        Task<List<ObjectData>> GetFilteredDataAsync(string objectType, dynamic filters);
    }
}
