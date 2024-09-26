using DataAccess.Abstract;
using DataAccess.Abstract.EntityFramework.ObjectSchemaRepositories;
using Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Concrete.EntityFramework.ObjectSchemaRepositories
{
    public class ObjectSchemaReadRepository : ReadRepository<ObjectSchema>, IObjectSchemaReadRepository
    {
        public ObjectSchemaReadRepository(Context context) : base(context)
        {
        }
    }
}
