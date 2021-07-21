using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Text;
using Journals.Api;

namespace Watch.API
{
    public static class SessionLoader
    {
        public static Session LoadSession(Stream loadFrom) => LoadSessions(loadFrom).FirstOrDefault();

        public static Session[] LoadSessions(Stream loadFrom)
        {
            try
            {
                loadFrom.Seek(0, SeekOrigin.Begin);

                var asm = AssemblyLoadContext.Default.LoadFromStream(loadFrom);

                var sessionTypes = asm.GetTypes().Where(x => typeof(Session).IsAssignableFrom(x) && !x.IsAbstract).ToArray();

                var sessions = sessionTypes.OrderBy(x => $"{x.Namespace}.{x.Name}").Select(t => (Session) Activator.CreateInstance(t)).ToArray();
                return sessions;
            }
            catch
            {
                return new Session[0];
            }
        }
    }
}
