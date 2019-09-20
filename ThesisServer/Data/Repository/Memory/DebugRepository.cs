using System;
using System.Collections.Generic;

namespace ThesisServer.Data.Repository.Memory
{
    public class DebugRepository
    {
        public List<(string, DateTime)> Errors { get; set; }

        public DebugRepository()
        {
            Errors = new List<(string, DateTime)>();
        }
    }
}
