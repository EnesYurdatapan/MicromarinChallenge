using DataAccess.Abstract;
using DataAccess.Abstract.EntityFramework.ObjectDataRepositories;
using Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Concrete.EntityFramework.ObjectDataRepositories
{
    public class ObjectDataReadRepository : ReadRepository<ObjectData>, IObjectDataReadRepository
    {
        public ObjectDataReadRepository(Context context) : base(context)
        {
        }
    }
}
