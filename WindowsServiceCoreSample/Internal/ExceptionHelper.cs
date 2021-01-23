using System;

namespace WindowsServiceCoreSample.Internal
{
    internal static class ExceptionHelper
    {
        #region action methods
        public static string FormatException(Exception ex, bool ignoreStackTrace = false)
        {
            Check.NotNull(ex, nameof(ex));

            var sb = new System.Text.StringBuilder();
            sb.Append(string.IsNullOrEmpty(ex.Message) ? ex.GetType().ToString() : (ex.GetType().ToString() + ": " + ex.Message));

            if (ex.InnerException != null)
            {
                if (sb.Length != 0)
                {
                    sb.AppendLine();
                }
                sb.Append("---> " + FormatException(ex.InnerException, ignoreStackTrace));

                if (!ignoreStackTrace)
                {
                    sb.Append(Environment.NewLine);
                    sb.Append("   ");
                    sb.Append("--- End of inner exception stack trace ---");
                }
            }

            if (!ignoreStackTrace && ex.StackTrace != null)
            {
                if (sb.Length != 0)
                {
                    sb.AppendLine();
                }
                sb.Append(ex.StackTrace);
            }

            return sb.ToString();
        }
        #endregion
    }
}