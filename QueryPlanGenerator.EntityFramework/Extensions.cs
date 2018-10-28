using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryPlanGenerator.EntityFramework
{
    internal static class Extensions
    {
        public static void SetupCommand(this DbCommand newCmd, DbCommand command)
        {
            newCmd.CommandText = command.CommandText;
            newCmd.CommandType = command.CommandType;
            newCmd.CommandTimeout = command.CommandTimeout;

            if (command.CommandType == CommandType.StoredProcedure)
            {
                //clone the parameters into the planCmd
                var parameters = command.Parameters.Cast<ICloneable>()
                    .Select(x => x.Clone() as SqlParameter)
                    .Where(x => x != null).ToArray();
                newCmd.Parameters.AddRange(parameters);
            }
            else if (command.Parameters.Count > 0)
            {
                //we must use a command W/O any parameters, otherwise sp_executesql kicks in, and we cannot capture the plan
                var paramDeclares = command.Parameters.GetParameterDeclares();
                newCmd.CommandText = $"{paramDeclares}\r\n{command.CommandText}";
            }
        }

        public static string GetParameterDeclares(this DbParameterCollection parameters)
        {
            var sb = new StringBuilder();
            foreach (SqlParameter parameter in parameters)
            {
                sb.AppendLine(parameter.GetParameterDeclare());
            }
            return sb.ToString();
        }

        public static string GetParameterDeclare(this SqlParameter parameter)
        {
            //most of these NEED testing
            var paramName = $"@{parameter.ParameterName.TrimStart('@')}"; 
            var sb = new StringBuilder($"DECLARE {paramName} ");
            var realValue = GetRealValue(parameter.Value, parameter.SqlDbType);
            var paramType = parameter.SqlDbType.ToString().ToUpper();
            var size = parameter.Size == -1 ? "MAX" : $"{parameter.Size}";

            switch (parameter.SqlDbType)
            {
                case SqlDbType.Bit:
                case SqlDbType.SmallInt:
                case SqlDbType.Int:
                case SqlDbType.BigInt:
                case SqlDbType.Money:
                case SqlDbType.TinyInt:
                case SqlDbType.Udt:
                    sb.Append($"{paramType} = {realValue}");
                    break;
                case SqlDbType.NChar:
                case SqlDbType.NVarChar:
                    sb.Append($"{paramType}({size}) = {realValue}");
                    break;
                case SqlDbType.Char:
                case SqlDbType.VarChar:
                    sb.Append($"{paramType}({size}) = {realValue}");
                    break;
                case SqlDbType.Date:
                case SqlDbType.DateTime:
                case SqlDbType.SmallDateTime:
                case SqlDbType.DateTimeOffset:
                case SqlDbType.Time:
                    sb.Append($"{paramType} = {realValue}");
                    break;
                case SqlDbType.DateTime2:
                    sb.Append($"{paramType}({size}) = {realValue}");
                    break;
                case SqlDbType.Real:
                case SqlDbType.Float:
                    sb.Append($"{paramType}({size}) = {realValue}");
                    break;
                case SqlDbType.Decimal:
                    sb.Append($"{paramType}({parameter.Precision},{parameter.Scale}) = {realValue}");
                    break;
                case SqlDbType.Image:
                case SqlDbType.Binary:
                case SqlDbType.VarBinary:
                    sb.Append($"{paramType}({size}) = 0x{String.Concat(Array.ConvertAll((byte[])parameter.Value, x => x.ToString("X2")))}");
                    break;
                case SqlDbType.NText:
                case SqlDbType.Text:
                    sb.Append($"/* QueryPlanInterceptor: TYPE {paramType} is unsupported */");
                    break;
                default:
                    Debug.WriteLine($"QueryPlanInterceptor: UNABLE TO DETERMINE SQL TYPE FOR {paramType}");
                    sb.Append($"{paramType} = {realValue} /* un-matched param type */");
                    break;

            }

            return sb.ToString();
        }

        private static string GetRealValue(object value, SqlDbType type)
        {
            if (value == DBNull.Value)
            {
                return "NULL";
            }
            else
            {
                switch (type)
                {
                    case SqlDbType.NChar:
                    case SqlDbType.NVarChar:
                        return $"N'{value}'";
                    case SqlDbType.Bit:
                    case SqlDbType.SmallInt:
                    case SqlDbType.Int:
                    case SqlDbType.BigInt:
                    case SqlDbType.Money:
                    case SqlDbType.TinyInt:
                    case SqlDbType.Real:
                    case SqlDbType.Float:
                    case SqlDbType.Decimal:
                        return $"{value}";
                    case SqlDbType.Udt:
                    case SqlDbType.Char:
                    case SqlDbType.VarChar:
                    case SqlDbType.Date:
                    case SqlDbType.DateTime:
                    case SqlDbType.SmallDateTime:
                    case SqlDbType.DateTimeOffset:
                    case SqlDbType.Time:
                    case SqlDbType.DateTime2:
                    default:
                         return $"'{value}'";
                }
            }
        }
    }
}
