using System;
using System.Collections.Generic;
using System.Text;

namespace BlackOfWorld.Webkit.Models
{
    public enum CurrentServerStatus : int
    {
        NotListening,
        Listening,
        InternalExceptionHappened
    }
    public enum WebkitErrors : int
    {
        Ok,
        NotRunning,
        ConfigEmpty,
        PortUsedByExternalProgram,
        UnknownError
    }
}
