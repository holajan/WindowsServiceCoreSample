using System;
using Microsoft.Extensions.Logging;

namespace WindowsServiceCoreSample.Logging
{
    internal sealed class NullExternalScopeProvider : IExternalScopeProvider
    {
        #region member varible and default property initialization
        public static IExternalScopeProvider Instance { get; } = new NullExternalScopeProvider();
        #endregion

        #region constructors and destructors
        private NullExternalScopeProvider() { }
        #endregion

        #region action methods
        void IExternalScopeProvider.ForEachScope<TState>(Action<object, TState> callback, TState state) { }

        IDisposable IExternalScopeProvider.Push(object state)
        {
            return NullScope.Instance;
        }
        #endregion
    }
}