using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eyesolaris.Logging.Bases;

namespace Eyesolaris.Logging
{
    public class LoggerProxy : LoggerProxyBase
    {
        public LoggerProxy(Func<IEyeLogger> loggerGetter)
        {
            _loggerGetter = loggerGetter;
        }

        protected override IEyeLogger GetLogger() => _loggerGetter();

        private readonly Func<IEyeLogger> _loggerGetter;
    }
}
