using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using System;

namespace AspNetCoreWebApp.Helpers
{
    public static class ApiResponse
    {
        private static readonly string _Char = ('\u26A0').ToString();

        public static string GetError(Exception ex)
        {
            if (ex.InnerException == null)
                return string.Format("{0} {1}", _Char, ex.Message.ToString());
            else
                return string.Format("{0} {1}", _Char, ex.InnerException.Message.ToString());
        }

        public static string GetError(string message)
        {
            return string.Format("{0} {1}", _Char, message);
        }

        public static string GetError(ModelStateDictionary modelState)
        {
            return GetModelStateErrors(modelState);
        }

        public static string GetError(IdentityResult result)
        {
            return GetIdentityResultErrors(result);
        }

        private static string GetModelStateErrors(ModelStateDictionary modelState)
        {
            IEnumerable<string> Errors = modelState.SelectMany(x => x.Value.Errors)
                                                    .Select(x => x.ErrorMessage).ToArray();

            StringBuilder _Response = new StringBuilder();
            foreach (var _Error in Errors)
            {
                if (_Response.Length != 0)
                    _Response.AppendLine();

                _Response.Append(_Char);
                _Response.Append(" ");
                _Response.Append(_Error);
            }
            return _Response.ToString();
        }

        private static string GetIdentityResultErrors(IdentityResult result)
        {
            StringBuilder _Response = new StringBuilder();
            foreach (var _Error in result.Errors)
            {
                if (_Response.Length != 0)
                    _Response.AppendLine();

                _Response.Append(_Char);
                _Response.Append(" ");
                _Response.Append(_Error.Description);
            }
            return _Response.ToString();
        }
    }
}
