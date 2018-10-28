using QueryPlanGenerator.EntityFramework.Persisters;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace QueryPlanGenerator.EntityFramework
{
    public class QueryPlanInterceptor : IDbCommandInterceptor
    {
        StringComparer _comparer = StringComparer.InvariantCultureIgnoreCase;
        #region fields 
        //USE SHOWPLAN_XML so that the query does not actually execute, but lets us capture the plans
        const string PLAN_ON = "SET SHOWPLAN_XML ON;";
        const string PLAN_OFF = "SET SHOWPLAN_XML OFF;";
        const string REGEX_PLAN_FIELD_NAME = "^Microsoft SQL Server .* XML Showplan$";

        private static readonly bool GENERATE_PLAN;
        private static readonly Regex _planRegex = null;

        private readonly IEnumerable<IPlanPersister> _persisters;
        #endregion fields 

        #region Constructors
        static QueryPlanInterceptor()
        {
            try
            {
                GENERATE_PLAN = bool.Parse(ConfigurationManager.AppSettings["EF.GenerateExecutionPlans"] ?? "false");
                _planRegex = new Regex(REGEX_PLAN_FIELD_NAME, RegexOptions.IgnoreCase);
            }
            catch
            {
                //if there are problems reading the .config for some reason, just shut off this interceptor
                GENERATE_PLAN = false;
            }
        }

        public QueryPlanInterceptor() : this(new DefaultFileSystemPersister()) { }

        public QueryPlanInterceptor(params IPlanPersister[] persistors)
        {
            if (persistors == null || persistors.Length == 0)
                throw new ArgumentNullException(nameof(persistors), "No IPlanPersistor/s provided");

            _persisters = persistors;
        }
        #endregion

        #region Interceptors
        public void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            GeneratePlan(command);
        }
        public void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            GeneratePlan(command);
        }
        public void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            GeneratePlan(command);
        }
        #endregion

        #region private methods
        private void GeneratePlan(DbCommand command)
        {
            DbDataReader reader = null;
            try
            {
                if (GENERATE_PLAN && !_comparer.Equals(command.CommandText, "select cast(serverproperty('EngineEdition') as int)"))
                {
                    using (var planCmd = command.Connection.CreateCommand())
                    {
                        //we HAVE to enlist in the transaction if there is one
                        if (command.Transaction != null) { planCmd.Transaction = command.Transaction; }
                        planCmd.CommandText = PLAN_ON;
                        planCmd.ExecuteNonQuery();

                        planCmd.SetupCommand(command);

                        using (reader = planCmd.ExecuteReader())
                        {
                            do
                            {
                                //loop until we find the Reader with an execution plan
                                while (reader.Read())
                                {
                                    if (reader.FieldCount == 1 && _planRegex.IsMatch(reader.GetName(0)))
                                    {
                                        var callingMethod = GetFQCallingMethodName();
                                        var planXml = reader.GetString(0);

                                        //send plan to all registered persisters
                                        foreach (var p in _persisters)
                                        {
                                            try
                                            {
                                                p.Persist(callingMethod, planXml);
                                            }
                                            catch (Exception ex)
                                            {
                                                //TODO: figure out what to do here? if the persister throws an exception
                                                Debug.WriteLine("QueryPlanInterceptor: " + ex.ToString());
                                            }
                                        }
                                    }
                                }
                            } while (reader.NextResult());
                        }
                        planCmd.CommandText = PLAN_OFF;
                        planCmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("QueryPlanInterceptor: " + ex.ToString());
            }
        }

        private string GetFQCallingMethodName()
        {
            //disregard all frames from System.Linq or System.Data
            //skip the 3 top stack frames, which are local to here
            //next frame should be the EF/Linq method which caused this interception to happen
            var frames = new StackTrace(true).GetFrames();
            var nonMSFrames = from f in new StackTrace(true).GetFrames()
                          let m = f.GetMethod()
                          where !m.DeclaringType.FullName.StartsWith("System.")  &&
                                !m.DeclaringType.FullName.StartsWith("Microsoft.")
                          select $"{m.DeclaringType.FullName}.{m.Name}.{f.GetFileLineNumber()}";

            return nonMSFrames
                .Skip(3)
                .Take(1)
                .Single();
        }
        #endregion private methods

        #region Unused
        public void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
        }
        public void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
        }
        public void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
        }
        #endregion
    }
}
