using System.Collections.Concurrent;

namespace ThesisServer.BL.Services
{
    public class LockService
    {
        private ConcurrentDictionary<string, object> _lockObjects;

        private object _lockObject = new object();

        public LockService()
        { 
            _lockObjects = new ConcurrentDictionary<string, object>();
        }

        public object GetLockObjectForString(string str)
        {
            if (!_lockObjects.TryGetValue(str, out var value))
            {
                _lockObjects.AddOrUpdate(str, new object(), (s, o) => o);
            }

            _lockObjects.TryGetValue(str, out value);

            return value;
        }
    }
}