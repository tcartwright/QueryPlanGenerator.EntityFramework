using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Interception;
using System.Diagnostics;

namespace PlanGenerator
{
    public partial class AdventureWorksEntities : DbContext
    {
       public AdventureWorksEntities(string connectionString) : base(connectionString)
        {
            Database.SetInitializer<AdventureWorksEntities>(null);
        }      
    }  
}
