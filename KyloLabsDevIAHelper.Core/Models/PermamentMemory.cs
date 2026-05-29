using System;
using System.Collections.Generic;
using System.Text;
using KyloLabs.DevIAHelper.Core.Models.Brain;

namespace KyloLabs.DevIAHelper.Core.Models
{
    public class PermanentMemory
    {
        public CreatorInfo[] Creator { get; set; } = Array.Empty<CreatorInfo>();
        public Personality Personality { get; set; } = new();
    }
}
