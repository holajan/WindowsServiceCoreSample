using System;

namespace WindowsServiceCoreSample.Logging
{
    internal sealed class NullScope : IDisposable
    {
        #region member varible and default property initialization
        public static NullScope Instance { get; } = new NullScope();
        #endregion

        #region constructors and destructors
        private NullScope() { }
        #endregion

        #region action methods
        public void Dispose() { }
        #endregion
    }
}