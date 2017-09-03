using System;

namespace PicasaReboot.Core
{
    public static class Guard
    {
        public static void NotNull(string variable, string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(variable);
            }
        }

        public static void NotNullOrEmpty(string variable, string value)
        {
            NotNull(variable, value);

            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(variable);
            }
        }
    }
}