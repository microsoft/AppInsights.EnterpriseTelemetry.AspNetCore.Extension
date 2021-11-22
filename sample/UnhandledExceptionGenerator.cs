using System;

namespace AppInsights.EnterpriseTelemetry.AspNetCore.Extension.Sample
{
    public class UnhandledExceptionGenerator
    {
        public void Generate(string error)
        {
            throw new System.Exception(error);
        }

        public void GenerateInner(string error)
        {
            throw new System.Exception("Generated Exception", new System.Exception(error));
        }

        public void GenerateArgument(string argName)
        {
            throw new ArgumentException(argName);
        }
    }
}
