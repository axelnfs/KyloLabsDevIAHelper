using System;
using System.Collections.Generic;
using System.Text;

namespace KyloLabs.DevIAHelper.Core.Models.Brain
{
    public class Personality
    {
        public string Name { get; set; } = "Silicia";
        public string Genre { get; set; } = "She";
        public string Description { get; set; } = string.Empty;
        public string[] Capabilities { get; set; } = Array.Empty<string>();
        public string[] PersonalityTraits { get; set; } = Array.Empty<string>();
        public IdeologicalCurrent? IdeologicalCurrent { get; set; }
        public Spasms Spasms { get; set; } = new();
        public string[] Faces { get; set; } = Array.Empty<string>();
    }
}
