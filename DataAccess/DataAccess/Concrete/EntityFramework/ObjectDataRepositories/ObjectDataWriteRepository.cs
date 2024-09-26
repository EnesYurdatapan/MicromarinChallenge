using DataAccess.Abstract;
using DataAccess.Abstract.EntityFramework.ObjectDataRepositories;
using Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Concrete.EntityFramework.ObjectDataRepositories
{
    public class ObjectDataWriteRepository : WriteRepository<ObjectData>, IObjectDataWriteRepository
    {
        public ObjectDataWriteRepository(Context context) : base(context)
        {
        }
    }
}
