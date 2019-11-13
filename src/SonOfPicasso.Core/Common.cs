using System;

namespace SonOfPicasso.Core
{
    public static class Common
    {
        public static bool IsDebug
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }

        public static bool IsTrace
        {
            get
            {
#if TRACE
                return true;
#else
                return false;
#endif
            }
        }

        public static bool IsVerboseLoggingEnabled
        {
            get
            {
                var environmentVariable = Environment.GetEnvironmentVariable("SonOfPicasso_Verbose");
                if (string.IsNullOrWhiteSpace(environmentVariable))
                    return false;

                environmentVariable = environmentVariable.ToLower();
                if (environmentVariable == "false" || environmentVariable == "0") return false;

                return true;
            }
        }
    }
}