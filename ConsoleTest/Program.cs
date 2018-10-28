using PlanGenerator.Properties;
using System;
using System.Configuration;
using System.Data;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;

namespace PlanGenerator
{
    class Program
	{

		static void Main(string[] args)
		{
            SqlServerTypes.Utilities.LoadNativeAssemblies(AppDomain.CurrentDomain.BaseDirectory);
            System.Data.Entity.SqlServer.SqlProviderServices.SqlServerTypesAssemblyName = "Microsoft.SqlServer.Types, Version=14.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            //run other tests with different params:
            Go();
		}

		static void Go()
		{
            do
            {
                Console.WriteLine("RUNNING QUERIES");
                using (var ctx = new AdventureWorksEntities(ConfigurationManager.ConnectionStrings["AdventureWorksEntities"].ConnectionString))
                {
                    var address = (from a in ctx.Addresses
                                   select a).First();

                    address.AddressLine2 = "foo"; //mark it as changed, so the null below will take effect
                    ctx.SaveChanges();

                    address.AddressLine1 = $"a{(new Random().Next()).ToString()}";
                    address.AddressLine2 = null;
                    address.ModifiedDate = DateTime.Now;
                    ctx.SaveChanges();

                    var stores = ctx.Database.SqlQuery<Store>("SELECT * FROM Sales.Store s");
                    //the execution for the above query will not occur until the below line where stores is exercised. 
                    foreach (var store in stores)
                    {
                        Debug.WriteLine(store.Name);
                    }

                    var photo = ctx.ProductPhotoes.Take(1).First();
                    photo.LargePhoto = Resources.Koala.ToByteArray(ImageFormat.Bmp);
                    ctx.SaveChanges();


                    var poDetail = ctx.PurchaseOrderDetails.Take(1).First();
                    poDetail.ReceivedQty += 0.1M;
                    ctx.SaveChanges();


                }
                Console.WriteLine("Press x to exit, any other key to run again.");
            } while (Console.ReadKey(true).Key != ConsoleKey.X);
		}

        static string GetConnectionString()
        {
            const string efConnectionString = @"metadata=res://*/AdventurWorks.csdl|res://*/AdventurWorks.ssdl|res://*/AdventurWorks.msl;provider=System.Data.SqlClient;provider connection string=";
            var builder = new EntityConnectionStringBuilder(efConnectionString);
            var connectionString = @"Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=AdventureWorks2012;Data Source=KWHDBSQLD13\Instance1;Application Name=PlanGenerator";
            var sqlBuilder = new SqlConnectionStringBuilder(connectionString);
            builder.ProviderConnectionString = sqlBuilder.ConnectionString;
            return builder.ConnectionString;

        }
    }
}
