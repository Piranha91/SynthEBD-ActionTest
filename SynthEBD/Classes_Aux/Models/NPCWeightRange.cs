﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthEBD
{
    public class NPCWeightRange
    {
        public NPCWeightRange()
        {
            this.Lower = 0;
            this.Upper = 100;
        }
        public int Lower { get; set; }
        public int Upper { get; set; }
    }
}